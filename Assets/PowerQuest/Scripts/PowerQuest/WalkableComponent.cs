using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// The walkable component doesn't do much- just lets you define holes for the pathfinder, and other editor stuff
[RequireComponent(typeof(PolygonCollider2D))]
public class WalkableComponent : MonoBehaviour 
{
	//[SerializeField] List<PolygonCollider2D> m_holes = new List<PolygonCollider2D>();
	PolygonCollider2D m_polygonCollider = null;

	//public List<PolygonCollider2D> Holes { get { return m_holes; } set{ m_holes = value; } }

	public PolygonCollider2D PolygonCollider 
	{ 
		get
		{
			if ( m_polygonCollider == null )
				m_polygonCollider = GetComponent<PolygonCollider2D>();
			 return m_polygonCollider; 
		} 
	}

	public Vector2[] Points { get { return PolygonCollider.points; } }
}
