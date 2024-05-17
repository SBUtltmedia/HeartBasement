using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using ClipperLib;

namespace PowerTools.Quest
{

// For clipper library
using Path = List<IntPoint>;
using Paths = List<List<IntPoint>>;


public partial class RoomComponent
{

	static readonly float TO_CLIPPER_MULT = 1000;
	static readonly float FROM_CLIPPER_MULT = 0.001f;

	// Uses clipper to build walkable area from multiple polygons, subtracting holes. 
	// This will allow walkables to be made up of more than one poly, so you can turn parts of the are on or off
	// Also will allow regions to block off whole areas, rather than having to build 2 walkable areas for that.
	// In short- it's better, but needs some refactoring and lots of testing to work.
	// This would make more sense living in Pathfinder rather than room. too...
	public bool BuildWalkableArea()
	{

		// Try clipper!
		Clipper clipper = new Clipper();
		clipper.ReverseSolution = true;
		
		Paths result;

		if ( BuildClipperWalkableArea(clipper, out result) == false  )
			return false;
						
		
		bool setMain = false;
		m_pathfinder = new Pathfinder();
		foreach ( Path path in result )
		{
			Vector2[] points = path.ConvertAll( item => new Vector2((float)item.X*FROM_CLIPPER_MULT,(float)item.Y*FROM_CLIPPER_MULT) ).ToArray();
			if ( Clipper.Orientation(path) == false )
			{
				if ( setMain == false )
					m_pathfinder.SetMainPolygon( m_walkableAreas[0].PolygonCollider, points );
				setMain = true;
			}
			else 
			{
				Pathfinder.ReversePoly(points);	
				m_pathfinder.AddObstacle( m_walkableAreas[0].PolygonCollider, points );
			}
		}

		return true;
	}

	public bool BuildClipperWalkableArea(Clipper clipper, out Paths result)
	{		
		// Create path from firstPoly
		bool first = true;
		for ( int i = 0; i < m_walkableAreas.Count; ++i )
		{		
			// TODO: check if walkable is "enabled", rather than only allowing one at a time.
			if ( m_data.ActiveWalkableArea != i )
				continue;

			WalkableComponent walkable = m_walkableAreas[i];
				
			Vector2[] points = walkable.PolygonCollider.points;

			//Pathfinder.ReversePoly(points);
			Path poly = new Path( System.Array.ConvertAll<Vector2,IntPoint>(points, item => {return new IntPoint(item.x*TO_CLIPPER_MULT,item.y*TO_CLIPPER_MULT);} )  );
			clipper.AddPath(poly, first ? PolyType.ptSubject : PolyType.ptClip, true);
			first = false;
			
		}

		result = new Paths();
		bool success = clipper.Execute(ClipType.ctUnion, result, PolyFillType.pftEvenOdd);
		//Debug.Log("Clipper main: "+ (success ? "succeed":"fail")+", results: "+result.Count);

		if ( success == false || result.Count < 0 )
			return false;

		// Now subtract holes
		int holes = 0;
		clipper.Clear();
		clipper.AddPath(result[0], PolyType.ptSubject, true);
		
		for ( int i = 0; i < m_walkableAreas.Count; ++i )
		{		
			// TODO: check if walkable is "enabled", rather than only allowing one at a time.
			if ( m_data.ActiveWalkableArea != i )
				continue;
			
			WalkableComponent walkable = m_walkableAreas[i];

			PolygonCollider2D[] childColliders = walkable.transform.GetComponentsInChildren<PolygonCollider2D>();
			foreach( PolygonCollider2D hole in childColliders )
			{
				// TODO: check if walkable is "enabled"
				if( hole.transform == walkable.PolygonCollider.transform )
					continue;			

				Vector2[] points = hole.points;
				Path poly = new Path( System.Array.ConvertAll<Vector2,IntPoint>(points, item => {return new IntPoint(item.x*TO_CLIPPER_MULT,item.y*TO_CLIPPER_MULT);} )  );
				clipper.AddPath(poly, PolyType.ptClip, true);
				holes++;
			}			
		}

		foreach ( RegionComponent region in m_regionComponents )
		{
			if ( /*region.GetData().Enabled == false ||*/ region.GetData().Walkable )
				continue;

			Vector2[] points = region.GetPolygonCollider().points;
			Path poly = new Path( System.Array.ConvertAll<Vector2,IntPoint>(points, item => {return new IntPoint(item.x*TO_CLIPPER_MULT,item.y*TO_CLIPPER_MULT);} )  );
			clipper.AddPath(poly, PolyType.ptClip, true);
			holes++;
		}

		if ( holes > 0 )
		{
			result.Clear();
			success = clipper.Execute(ClipType.ctDifference, result, PolyFillType.pftEvenOdd);
			//Debug.Log("Clipper holes: "+ (success ? "succeed":"fail")+", results: "+result.Count);
		}

		return true;
		
	}


	public Vector2 GetClosestPoint(Vector2 from, Vector2 to)
	{	
		/* 
			Using clipper to find the closest point on the polygon a character is standing on.
			This first builds the polygons, then finds the one the 'from' point is on, and checks to find closest 'to' pos that's also on the polygon.

			For now we're only using this as a fallback, since it's kinda broken in edge cases, and I don't wanna spend time on it now.

			NB: Due to rounding errors (Clipper is dealing with ints, and we're scaling by 1000 to help with accuracy), the player may path incorrectly when standing on, or clicking on edge of an obstacle. 
			NBB: Also, could pre-build the result rather than rebuilding constantly... 
			NBBB: Should also add other player's colliders before doing this... for now we fall back on using the old function for getting closest point.
			
			TODO: Debug with lines so can check how its working properly
		*/

		// Try clipper!
		Clipper clipper = new Clipper();
		clipper.ReverseSolution = true;
		
		Paths clipperResult;		
		


		if ( BuildClipperWalkableArea(clipper, out clipperResult) == false )
		{
			Debug.Log("Clipper FAIL: couldn't build walkable");
			return to;
		}	
		
		// Multiplying by 1000 to get some more accuracy from integer points. This should be ok for walkable areas up to 2147x2147, then we're in trouble.
		IntPoint fromInt = new IntPoint(from.x*TO_CLIPPER_MULT,from.y*TO_CLIPPER_MULT);
		IntPoint toInt = new IntPoint(to.x*TO_CLIPPER_MULT,to.y*TO_CLIPPER_MULT);
		
		// Find poly we're on. PointInPolygon returns 0 if false, -1 if pt is on poly and +1 if pt is in poly.
		Path mainPath = clipperResult.Find( path => Clipper.Orientation(path) == false && Clipper.PointInPolygon(fromInt,path) != 0 );
		if ( mainPath == null || mainPath.Count == 0 )
		{			
			Debug.Log("Clipper FAIL: Plr not on walkable");
			return to;
		}
				
		// Offset by 0.01 * 1000, to account for innacuracy scaling from floats to the ints that clipper uses.
		ClipperOffset offset = new ClipperOffset();
		offset.AddPath(mainPath, JoinType.jtMiter, EndType.etClosedPolygon);
		offset.Execute(ref clipperResult, -10); // offsetting by 0.01, the accuracy should be 0.001, so i think this i safe..?
		mainPath = clipperResult[0];
		
		if ( clipperResult.Count > 1 )
			Debug.LogWarning("Multiple polygons created when offsetting walkable area. Move your area points further apart");
			
		if ( Clipper.PointInPolygon(toInt,mainPath) != 0 )
		{
			//Debug.Log("Clipper: point is in poly");
			return to; // If so, return that point	
		}		

		// If it's not on the same poly, return the closest poly on the current polygon... another expensive operation			
		Vector2[] poly = mainPath.ConvertAll( item => new Vector2((float)item.X*FROM_CLIPPER_MULT,(float)item.Y*FROM_CLIPPER_MULT) ).ToArray();	
	
		Vector2 result = GetClosestPointToPoly(to,poly);	
		
		//Systems.Debug.DrawPoly(poly, Color.yellow);
		//Systems.Debug.DrawPoint(to, Color.green);
		//Systems.Debug.DrawPoint(result, Color.red);		
		//Debug.Log($"Clipper: Closest Point to ({to}), is ({result})");
		return result;

	}
	
	// Assuming point isn't inside poly, this finds the closest point to the poly.
	Vector2 GetClosestPointToPoly( Vector2 x, Vector2[] poly )
	{
		float closestDistSqr = float.MaxValue;
		Vector2 closestPosition = Vector2.zero;

		for ( int j = 0; j < poly.Length; ++j )
		{
			int i = j == 0 ? poly.Length-1 : j-1;
			Vector2 p1 = poly[i];
			Vector2 p2 = poly[j];
			Vector2 p12 = p2-p1; // From 1st to 2nd point
			Vector2 pP1 = x-p1; // From 1st to target point

			if ( p1==p2 )
				continue;

			// Now R, as the ratio between the line p1 to p2 (from 0 to 1)
			float r = Vector2.Dot(p12, pP1);
			r = r / p12.sqrMagnitude;			

			float distSqr = 0;
			Vector2 newPoint = Vector2.zero;
			if ( r < 0 )
				distSqr = (pP1).sqrMagnitude; // at point 1
			else if ( r > 1 )
				distSqr = (p2-x).sqrMagnitude; // at point 2
			else
				distSqr = pP1.sqrMagnitude - Mathf.Pow(r*p12.magnitude,2); // between

			if ( distSqr < closestDistSqr )
			{
				closestDistSqr = distSqr;
				// Find the point
				if ( r < 0 )
					closestPosition = p1;
				else if ( r > 1 )
					closestPosition = p2;
				else
					closestPosition = p1 + p12*r;
			}

		}
		//Debug.Log($"closestDistSqr: {closestDistSqr}");
		return closestPosition;
	}
	

}

}
