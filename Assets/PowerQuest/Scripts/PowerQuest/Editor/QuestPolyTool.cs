using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.Rendering;
using PowerTools;
using PowerTools.Quest;
using ClipperLib;
using System.Linq; // for simplifier thing


namespace PowerTools.Quest
{

using Path = List<IntPoint>;
using Paths = List<List<IntPoint>>;

// Tagging a class with the EditorTool attribute and no target type registers a global tool. Global tools are valid for any selection, and are accessible through the top left toolbar in the editor.
[EditorTool("Quest Poly", typeof(PolygonCollider2D))]
class QuestPolyTool : EditorTool, IDrawSelectedHandles
{
	static readonly float TO_CLIPPER_MULT = 1000;
	static readonly float FROM_CLIPPER_MULT = 0.001f;
	static readonly float BRUSH_MIN = 2;
	static readonly float BRUSH_MAX = 30;

	enum eTool { Point, Square, Circle, Count }
	eTool m_tool = eTool.Circle;
	float m_brushSize = 8;
	bool m_hasSpriteRenderer = false;
	bool m_useSpriteConfirm = false; // So you have to double click to "confirm" overriding polygon from a sprite

	PolygonCollider2D m_collider = null;
	Vector2 m_offset = Vector2.zero;
	List<Vector2[]> m_paths = null;
	Vector3 m_offsetCached = Vector2.one; // USed to detect whether transform or offset has changed

	GUIContent m_ToolbarIcon;	
	
	// for brush
	Vector2 m_dragStart = Vector2.zero;

	// For points
	Vector2 m_mouseDelta = Vector2.zero;
	Vector2 m_mousePosPrev = Vector2.zero;	
	bool m_dragging = false;
	int m_draggingPath = -1;
	int m_draggingPoint = -1;
	
		
	///////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Static show/hide tool functions

	static readonly System.Type TYPE_QUESTPOLYTOOL = typeof(QuestPolyTool);
	public static GameObject m_polyEditorObject = null;	
	public static bool Active() => Tools.current == Tool.Custom && ToolManager.activeToolType == TYPE_QUESTPOLYTOOL;
	public static bool Active(GameObject col) { return Tools.current == Tool.Custom && ToolManager.activeToolType == TYPE_QUESTPOLYTOOL && (col == null || m_polyEditorObject == col.gameObject); }
	public static void Toggle(GameObject col)
	{ 
		if ( Active(col) )
		{ 		
			Hide();
		}
		else 
		{ 
			Show(col);
		}
	}

	// Set col to null to hide again
	public static void Hide()
	{
		if ( Active() )
		{
			//EditMode.ChangeEditMode(UnityEditorInternal.EditMode.SceneViewEditMode.None, new Bounds(), null);
			ToolManager.SetActiveTool((UnityEditor.EditorTools.EditorTool)null);
		}
		m_polyEditorObject = null;
	}
	
	public static void Show( MonoBehaviour col ) => Show(col.GetComponent<PolygonCollider2D>());
	public static void Show( GameObject col ) => Show(col.GetComponent<PolygonCollider2D>());
	public static void Show( Collider2D col )
	{
		if ( col == null )		
		{
			Hide();
			return;
		}	
		
		m_polyEditorObject = col.gameObject;
		
		Selection.activeGameObject = col.gameObject;
		if ( Active() == false && col )
			ToolManager.SetActiveTool<QuestPolyTool>();
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Static draw collider functions

	public static void DrawCollider(GameObject gameObject, float alpha = 1) => DrawCollider( gameObject.GetComponent<PolygonCollider2D>(), alpha );
	
	public static void DrawCollider(PolygonCollider2D collider, float alpha = 1)
	{ 
		if ( collider == null )
			return;
		Transform t = collider.transform;
		for (int i = 0; i < collider.pathCount; ++i )		
		{ 
			Vector2[] path = collider.GetPath(i);

			var zTest = Handles.zTest;
			Handles.zTest = CompareFunction.LessEqual;

			float scale = 1;

			if ( Pathfinder.CheckWindingClockwise(path))
				Handles.color = Color.yellow.WithAlpha(alpha);
			else 
				Handles.color = Color.green.WithAlpha(alpha);
				
			//Handles.DrawAAConvexPolygon(System.Array.ConvertAll<Vector2,Vector3>(path, item=>item));
			
			Handles.DrawAAPolyLine(2*scale,System.Array.ConvertAll<Vector2,Vector3>(path, item=>t.TransformPoint(item+collider.offset)));	
			if ( path.Length > 2 )
				Handles.DrawAAPolyLine(2*scale,t.TransformPoint(path[path.Length-1]+collider.offset), t.TransformPoint(path[0]+collider.offset));	
								
			Handles.zTest = zTest;
		}
	}	

	
	///////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Tool functcions

	public override void OnToolGUI(EditorWindow window)
	{
		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
	
		if (!(window is SceneView sceneView))
			return;
			
		Event e = Event.current;

		// Setup collider if necessary
		PolygonCollider2D col = ((PolygonCollider2D)target);
		if ( col != m_collider || m_paths == null )
		{ 
			m_collider = col;			
			SetupPathsFromCollider();
			PassThroughClipper(); // pass through clipper first time to correct windings
			
			m_hasSpriteRenderer = col.GetComponentInChildren<SpriteRenderer>() != null;
		}		

		// Check if transform or offset has changed, reapply to cached poitns
		if ( e.type == EventType.Repaint )
		{
			if ( m_offsetCached != m_collider.transform.TransformPoint(Vector2.one+m_offset) )
				SetupPathsFromCollider();
		}
		
		Vector2 mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
		
		if ( e.type == EventType.MouseDown || e.type == EventType.MouseDrag )
		{ 
			m_mouseDelta = mousePos - m_mousePosPrev;
			m_mousePosPrev = mousePos;
		}

		////////////////////////////////////////////////////////////////////////
		/// Test gui box
		
		Handles.BeginGUI();
		
		using (new GUILayout.HorizontalScope(GUILayout.Width(120)))
		{
			GUILayout.Space(20);
			float width = 220;
			using (new GUILayout.VerticalScope(EditorStyles.helpBox,GUILayout.MaxWidth(width)))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(new GUIContent("Edit Polygon"), EditorStyles.boldLabel, GUILayout.MaxWidth(width-100));

				if ( m_hasSpriteRenderer )
				{ 
					if ( m_useSpriteConfirm == false )
					{ 
						if ( GUILayout.Button("From Sprite", EditorStyles.miniButton, GUILayout.MaxWidth(100) ) )
							m_useSpriteConfirm = true;
					}
					else if ( GUILayout.Button("Confirm", EditorStyles.miniButton, GUILayout.MaxWidth(100) ) )
					{					
						Undo.RecordObject(target, "Polygon from sprite");
						EditorUtils.UpdateClickableCollider(col.gameObject);					
						EditorUtility.SetDirty(col);
						SetupPathsFromCollider();
						window.Repaint();
						m_useSpriteConfirm = false;
					}
				}
				EditorGUILayout.EndHorizontal();

				m_tool = LayoutToolButtons(m_tool);

				m_brushSize = EditorGUILayout.Slider(m_brushSize,BRUSH_MIN*QuestEditorUtils.GameResScale,BRUSH_MAX*QuestEditorUtils.GameResScale, GUILayout.MaxWidth(width));
				if ( m_tool == eTool.Point || e.shift )
					EditorGUILayout.LabelField( new GUIContent("Left click: Add/Edit Point\nRight click: Remove Point"), EditorStyles.miniLabel, GUILayout.MaxWidth(width), GUILayout.MaxHeight(20));
				else 
					EditorGUILayout.LabelField( new GUIContent("Left/Right Click: Draw/Erase\nCtrl+Scroll: Brush Size\nShift: Edit Points"), EditorStyles.miniLabel,GUILayout.MaxWidth(width), GUILayout.MaxHeight(30) );
			}

			GUILayout.FlexibleSpace();
		}
		Handles.EndGUI();
				
		if ( m_tool == eTool.Point || e.shift ) // hold shift to edit points too
			OnToolGuiPoint(window,mousePos);
		else 
			OnToolGuiBrush(window, mousePos);
		
		if ( e.type == EventType.ScrollWheel && e.control )
		{			
			if ( e.delta.y < 0 )
				m_brushSize ++;
			else 
				m_brushSize --;
			m_brushSize = Mathf.Clamp(m_brushSize,BRUSH_MIN*QuestEditorUtils.GameResScale,BRUSH_MAX*2*QuestEditorUtils.GameResScale);
			e.Use();
		}
			
		if ( e.isMouse && e.type == EventType.MouseMove ) // should also check its in right place
		{ 
			window.Repaint(); //(window as SceneView).sceneViewState.alwaysRefresh = true;
		}
	}

	bool m_edited = false;
	

	public void OnToolGuiBrush(EditorWindow window, Vector2 mousePos)
	{ 
	
		List<Vector2> points;
		Event e = Event.current;	
		float handleSize = HandleUtility.GetHandleSize(Vector3.zero) * 0.025f;
				
		////////////////////////////////////////////////////////////////////////
		/// Draw brush outline
		/// 

		if  (m_tool == eTool.Square )
			Handles.RectangleHandleCap(0, mousePos, Quaternion.identity, m_brushSize, e.type);
		else 
			Handles.CircleHandleCap(0, mousePos, Quaternion.identity, m_brushSize, e.type);

		////////////////////////////////////////////////////////////////////////
		/// Clipper stuff

		// construct it
		Clipper clipper = new Clipper();
		clipper.ReverseSolution = true;
		clipper.StrictlySimple = true;		
		
		// Add polys		
		foreach( Vector2[] path in m_paths )
			clipper.AddPath(ToPath(path), PolyType.ptSubject, true);

		bool delete = e.control;

		bool success = false;
		Paths clipperResult = new Paths();
		if ( (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && (e.button == 1 || e.button == 0) )
		{ 
			List<Vector2> brush = new List<Vector2>();
			
			if ( m_tool == eTool.Square )
			{
				for ( int i = 0; i < 4; ++i )
					brush.Add((Vector2.one.Rotate(i*90)*m_brushSize) + mousePos);
			}
			else
			{
				int segments = 8;//m_brushSize < 8 ? 8 : 12;
				float segAngle = 360f/(float)segments;
				for ( int i = 0; i < segments; ++i )
					brush.Add((Vector2.right.Rotate((i*segAngle)+(segAngle*0.5f))*m_brushSize) + mousePos); // adding 0.5 so sides are flat
			}			

			// if dragging, add pref point 
			if ( e.type == EventType.MouseDrag && m_dragStart != mousePos )
			{
				// Dragging- add extra polygon to smooth edges from last pos to this one
				clipper.AddPath(ToPath(GetDraggedPoly(m_dragStart, mousePos, brush.ToArray())), PolyType.ptClip, true);
				
				// do in 2 stages
				if ( e.button == 1 || (e.button == 0 && delete) ) // rightclick or hold control deletes
					success = clipper.Execute(ClipType.ctDifference, clipperResult, PolyFillType.pftEvenOdd);				
				if ( e.button == 0 )
					success = clipper.Execute(ClipType.ctUnion, clipperResult, PolyFillType.pftEvenOdd);	
				clipper.Clear();
				clipper.AddPaths(clipperResult, PolyType.ptSubject,true);
				clipperResult = new Paths();
			}

			clipper.AddPath(ToPath(brush.ToArray()), PolyType.ptClip, true);

			m_dragStart = mousePos;

			if ( e.button == 1 || (e.button == 0 && delete) ) // rightclick or hold control deletes
				success = clipper.Execute(ClipType.ctDifference, clipperResult, PolyFillType.pftEvenOdd);				
			if ( e.button == 0 )
				success = clipper.Execute(ClipType.ctUnion, clipperResult, PolyFillType.pftEvenOdd);		
			
			e.Use();
		}
		
		if ( success && clipperResult.Count > 0 )
		{ 
			m_edited = true;

			m_paths.Clear();
			
			foreach ( Path path in clipperResult )
			{ 
				// From clipper
				points = FromPath(path);
				// Simplify poly
				points = DouglasPeuckerInterpolation.Interpolate(points,points.Count,0.5f*QuestEditorUtils.GameResScale).ToList<Vector2>();
				m_paths.Add(points.ToArray());
			}	
					

		}
		

		if ( m_edited && (e.type == EventType.MouseUp || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null) ) // if staged prefab- wait for mouse up
		{ 
			////////////////////////////////////////////////////////////////////////
			/// Apply changes
			/// 
			CommitChanges();
		}

		////////////////////////////////////////////////////////////////////////
		/// Draw poly
		
		if (e.type == EventType.Repaint)
		{				
			foreach ( Vector2[] path in m_paths )
			{ 
				var zTest = Handles.zTest;
				Handles.zTest = CompareFunction.LessEqual;

				float scale = 1;

				if ( Pathfinder.CheckWindingClockwise(path)) // note this is sometimes wrong he he
					Handles.color = Color.yellow;
				else 
					Handles.color = Color.green;

				Handles.DrawAAPolyLine(4*scale,System.Array.ConvertAll<Vector2,Vector3>(path, item=>item));	
				if ( path.Length > 2 )
					Handles.DrawAAPolyLine(4*scale,path[path.Length-1], path[0]);	
						
				foreach ( Vector2 point in path )
					Handles.DotHandleCap(0, point, Quaternion.identity, handleSize, e.type);
			
				Handles.zTest = zTest;
			}
		}

	}
	
	Path ToPath(Vector2[] points)
	{
		return new Path( System.Array.ConvertAll<Vector2,IntPoint>(points, item => {return new IntPoint(item.x*TO_CLIPPER_MULT,item.y*TO_CLIPPER_MULT);} )  );
	}

	List<Vector2> FromPath(Path path)
	{ 
		return path.ConvertAll( item => new Vector2((float)item.X*FROM_CLIPPER_MULT,(float)item.Y*FROM_CLIPPER_MULT) );	
	}
	
	// Creat polygon from old mouse pos shape to new one to fill in gaps when dragging mouse fast
	Vector2[] GetDraggedPoly( Vector2 oldPos, Vector2 newPos, Vector2[] poly ) 
	{ 
		Vector2[] result = new Vector2[4];
		Vector2 tangent = (newPos-oldPos).normalized.GetTangent();
		Vector2 oldOffset = oldPos - newPos;
		
		// order: +new, +old, -old, -new // reverse wind 
		result[0] = FindFarthestTangentPoint(newPos, poly, tangent  );
		result[1] = FindFarthestTangentPoint(newPos, poly, tangent  ) + oldOffset;
		result[2] = FindFarthestTangentPoint(newPos, poly, -tangent ) + oldOffset;
		result[3] = FindFarthestTangentPoint(newPos, poly, -tangent );	

		// local function to find furthest point in tangent
		static Vector2 FindFarthestTangentPoint(Vector2 center, Vector2[] poly, Vector2 tangent )
		{ 
			float distFarthest = 0;
			Vector2 result = Vector2.zero;
			foreach ( Vector2 point in poly )
			{ 
				float dot = Vector2.Dot(point-center,tangent);
				if ( dot > distFarthest )
				{ 
					distFarthest=dot;
					result=point;
				}
			}
			return result;
		}
		
		return result;
	}

	public void OnToolGuiPoint(EditorWindow window, Vector2 mousePos)
	{ 
		// this one needs to change from using handles to manually doing dragging/etc,
		// - So can start dragging at same time as create new point
		// - So can highlight point mouse is over in yellow		
		// - So can offset current drag pos from mouse like collider one does.

		////////////////////////////////////////////////////////////////////////

		var e = Event.current;		
		float handleSize = HandleUtility.GetHandleSize(Vector3.zero) * 0.05f;
		
		bool changed = false;
		bool delete = e.control;		
		int closestPath,closestPoint;

		// closest line for adding point to
		int closestLinePath = -1;
		int closestLinePoint = -1;
		float closestLineDist = float.MaxValue;
		Vector2 closestPointOnLine = Vector2.zero;
		
		////////////////////////////////////////////////////////////////////////
		// Calc closest point or point on closest line
		
		if ( m_dragging )
		{ 
			closestPath = m_draggingPath;
			closestPoint = m_draggingPoint;
		}
		else 
		{ 
			float handleOverPointSize = handleSize*4;
			GetClosestPoint(mousePos, handleOverPointSize,out closestPath, out closestPoint);

			if ( closestPoint < 0 )
			{ 
				for ( int i = 0; i < m_paths.Count; ++i )
				{ 
					Vector2[] points = m_paths[i];
					for ( int j = 0; j < points.Length; ++j )
					{ 
						Vector3 point = points[j];			
			
						int nextPoint = j == points.Length-1 ? 0 : j+1;
						float lineDist = HandleUtility.DistancePointLine(mousePos, points[j], points[nextPoint]);
						if ( lineDist < handleSize*10 &&  lineDist < closestLineDist )
						{ 
							closestLineDist = lineDist;
							closestLinePath = i;
							closestLinePoint = nextPoint;
							closestPointOnLine = HandleUtility.ProjectPointLine(mousePos, points[j], points[nextPoint]);
						}
					}
				}
				
				// if closest point on line is over an existing point, set that as selected point
				if ( closestLinePoint >= 0 )
				{ 
					Vector2[] points = m_paths[closestLinePath];
					for ( int j = 0; j < points.Length; ++j )
					{ 	
						if ( (points[j] - closestPointOnLine).sqrMagnitude < handleOverPointSize*handleOverPointSize )
						{ 
							closestPath = closestLinePath;
							closestPoint = j;
						}
					}
				}
			}
		}

		////////////////////////////////////////////////////////////////////////
		// Draw the polygons (behind editable nodes)

		if (e.type == EventType.Repaint)
		{				
			foreach ( Vector2[] path in m_paths )
			{ 
				var zTest = Handles.zTest;
				Handles.zTest = CompareFunction.LessEqual;

				float scale = 1;

				if ( Pathfinder.CheckWindingClockwise(path))
					Handles.color = Color.yellow;
				else 
					Handles.color = Color.green;
							
				Handles.DrawAAPolyLine(4*scale,System.Array.ConvertAll<Vector2,Vector3>(path, item=>item));	
				if ( path.Length > 2 )
					Handles.DrawAAPolyLine(4*scale,path[path.Length-1], path[0]);	
						
				foreach ( Vector2 point in path )
					Handles.DotHandleCap(0, point, Quaternion.identity, handleSize*.5f, e.type);
			
				Handles.zTest = zTest;
			}

			// Draw highlighted point		
			if ( closestPoint >= 0 )
			{ 			
				if ( delete )
					Handles.color = Color.red;
				else 
					Handles.color = Color.green;
				Handles.DotHandleCap(0, m_paths[closestPath][closestPoint], Quaternion.identity, handleSize, e.type);				
			}
			else if ( closestLinePoint >= 0 )
			{ 		
				Handles.color = Color.yellow;		
				Handles.DotHandleCap(0, closestPointOnLine, Quaternion.identity, handleSize, e.type);				
			}
		}		

		/////////////////////////////////////////////////////////////////////////////
		// Handle delete
		
		if ( e.type == EventType.MouseDown 
			&& ((e.button == 0 && delete) || e.button == 1) // hold ctrl to delete, or right click
			&& closestPoint >= 0 && m_paths[closestPath].Length > 3 )
		{ 			
			List<Vector2> pointsList = new List<Vector2>(m_paths[closestPath]);
			pointsList.RemoveAt(closestPoint);
			m_paths[closestPath] = pointsList.ToArray();
			changed=true;
			e.Use();
		}

		/////////////////////////////////////////////////////////////////////////////
		// Find if over line to add point
		
		if ( e.type == EventType.MouseUp )
		{ 
			m_dragging=false;
		}

		if ( e.button == 0 && delete == false )
		{	
		
			/////////////////////////////////////////////////////////////////////////////
			// Move points (click drag existing point)

			if ( closestPoint != -1 )
			{ 
				if ( e.type == EventType.MouseDown )
				{ 
					m_dragging=true;
					m_draggingPath=closestPath;
					m_draggingPoint=closestPoint;
					e.Use();
				}
				else if ( e.type == EventType.MouseDrag && m_dragging && m_draggingPoint != -1 )
				{ 
					// Move closest point
					m_paths[m_draggingPath][m_draggingPoint] += m_mouseDelta;
					changed=true;
					e.Use();
				}
			}
			
			/////////////////////////////////////////////////////////////////////////////
			// Add point on click

			else if (closestLinePath != -1 && closestLinePoint != -1 && closestPoint == -1 )
			{ 
				
				if ( e.type == EventType.MouseDown )
				{ 
					// Add point
					changed=true;										
					List<Vector2> pointsList = new List<Vector2>(m_paths[closestLinePath]);
					pointsList.Insert(closestLinePoint, closestPointOnLine);
					m_paths[closestLinePath] = pointsList.ToArray();

					m_dragging=true;
					m_draggingPath=closestLinePath;
					m_draggingPoint=closestLinePoint;

					e.Use();
				}
			}
		}
		if ( changed )
			m_edited = true;
		
		if ( m_edited && (e.type == EventType.MouseUp || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null) )  // if staged prefab- wait for mouse up
		{ 
			if ( e.type == EventType.MouseUp )				
				PassThroughClipper(); // pass into clipper to clean it up

			CommitChanges();
		}
	}

	// Apply changes from m_paths to the collider
	void CommitChanges()
	{ 
		Undo.RecordObject(m_collider, "Edit Polygon");			
			
		// Transform back to local coords and set points in collider
		Transform t = m_collider.transform;		
		m_collider.pathCount = m_paths.Count;	
		for (int i = 0; i < m_paths.Count; ++i)
			m_collider.SetPath(i, 
				System.Array.ConvertAll<Vector2,Vector2>(m_paths[i], item=>
					(Vector2)t.InverseTransformPoint(item)-m_offset));
			
		EditorUtility.SetDirty(m_collider.gameObject);
		m_edited = false;
	}

	void GetClosestPoint( Vector2 mousePos, float maxDist, out int path, out int point )
	{ 		
		float maxDistSqr = maxDist*maxDist;
		float closest = float.MaxValue;
		path = -1;
		point = -1;
			
		for ( int i = 0; i < m_paths.Count; ++i )
		{ 		
			for ( int j = 0; j < m_paths[i].Length; ++j )
			{ 
				Vector2 pos = m_paths[i][j];
				float dist = (mousePos- pos).sqrMagnitude;
				if ( dist <= maxDistSqr && dist < closest )
				{ 
					path = i;
					point = j;
					closest = dist;
				}
			}
		}		
	}
	
	
	// Grabs polygon data from the collider into m_paths and m_offset
	void SetupPathsFromCollider()
	{
		if ( m_collider == null )
			m_collider = ((PolygonCollider2D)target);
		
		m_useSpriteConfirm = false;
		m_offset = m_collider.offset;
		if ( m_paths == null )
			m_paths = new List<Vector2[]>();
		m_paths.Clear();

		Transform t = m_collider.transform;
		for ( int i = 0; i < m_collider.pathCount; ++i )
			m_paths.Add(System.Array.ConvertAll<Vector2,Vector2>(m_collider.GetPath(i), item=>t.TransformPoint(item+m_offset)) );
		m_offsetCached = t.TransformPoint(Vector2.one+m_offset);

		PassThroughClipper();
	}	

	void PassThroughClipper()
	{ 
		List<Vector2> points;
		Paths clipperResult = new Paths();
		
		// construct it
		Clipper clipper = new Clipper();
		clipper.ReverseSolution = true;
		clipper.StrictlySimple = true;		
		
		// Add polys		
		foreach( Vector2[] path in m_paths )
			clipper.AddPath(ToPath(path), PolyType.ptSubject, true);
			
		if ( clipper.Execute(ClipType.ctUnion, clipperResult, PolyFillType.pftEvenOdd) )
		{ 
			m_paths.Clear();			
			foreach ( Path path in clipperResult )
			{ 
				// From clipper
				points = FromPath(path);
				m_paths.Add(points.ToArray());
			}	
		}
	}

	// Layout tabs
	eTool LayoutToolButtons(eTool selected)
	{
		const float DarkGray = 0.6f;
		const float LightGray = 0.9f;
		//const float StartSpace = 5;
	 
		//GUILayout.Space(StartSpace);
		Color storeColor = GUI.backgroundColor;
		Color highlightCol = new Color(LightGray, LightGray, LightGray);
		Color bgCol = new Color(DarkGray, DarkGray, DarkGray);
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.padding.top = 10;
		buttonStyle.padding.bottom = 10;
		buttonStyle.margin.left = 0;
		buttonStyle.margin.right = 0;
	 
		GUILayout.BeginHorizontal(/*GUILayout.MaxWidth(50)*/);
		{   //Create a row of buttons
			for (eTool i = 0; i < eTool.Count; ++i)
			{
				GUI.backgroundColor = i == selected ? highlightCol : bgCol;
				if (GUILayout.Button(((eTool)i).ToString(), buttonStyle))
				{
					selected = i; //Tab click
				}
			}
		} GUILayout.EndHorizontal();
		//Restore color
		GUI.backgroundColor = storeColor;	 
		return selected;
	}

	
	//////////////////////////////////////////////////////////////////////////////////////
	// Unity tool stuff

	/*
	public override GUIContent toolbarIcon
	{
		get
		{
			if (m_ToolbarIcon == null)
				m_ToolbarIcon = new GUIContent(
					AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Examples/Icons/VertexTool.png"),
					"Vertex Visualization Tool");
			return m_ToolbarIcon;
		}
	}
	*/

	void OnEnable()
	{		
		//ToolManager.activeToolChanged += ActiveToolDidChange;
		Undo.undoRedoPerformed += SetupPathsFromCollider;
	}

	void OnDisable()
	{
		//ToolManager.activeToolChanged -= ActiveToolDidChange;
		Undo.undoRedoPerformed -= SetupPathsFromCollider;
	}
	
	// Called when the active tool is set to this tool instance. Global tools are persisted by the ToolManager,
	// so usually you would use OnEnable and OnDisable to manage native resources, and OnActivated/OnWillBeDeactivated
	// to set up state. See also `EditorTools.{ activeToolChanged, activeToolChanged }` events.
	public override void OnActivated()
	{
		//SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Entering Platform Tool"), .1f);		
		//UnityEditor.SceneView.lastActiveSceneView.sceneViewState.alwaysRefresh=true;
	}

	// Called before the active tool is changed, or destroyed. The exception to this rule is if you have manually
	// destroyed this tool (ex, calling `Destroy(this)` will skip the OnWillBeDeactivated invocation).
	public override void OnWillBeDeactivated()
	{
		//SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Exiting Platform Tool"), .1f);
		//UnityEditor.SceneView.lastActiveSceneView.sceneViewState.alwaysRefresh=false;
	}

	public void OnDrawHandles()
	{ 
	}

	//void ActiveToolDidChange()
	//{
	//	if (!ToolManager.IsActiveTool(this))
	//		return;
	//}

}







/// <summary>
///     Douglas Peucker Reduction algorithm.
/// </summary>
/// <remarks>
///     Ramer Douglas Peucker algorithm is a line simplification
///     algorithm for reducing the number of points used to define its
///     shape.
/// </remarks>
public class DouglasPeuckerInterpolation
{
	/// <summary>
	///     Minimum number of points required to run the algorithm.
	/// </summary>
	private const int MinPoints = 3;

	/// <summary>
	///     Class representing a Douglas Peucker
	///     segment. Contains the start and end index of the line,
	///     the biggest distance of a point from the line and the
	///     points index.
	/// </summary>
	private class Segment
	{
		/// <summary>
		///     The start index of the line.
		/// </summary>
		public int Start { get; set; }

		/// <summary>
		///     The end index of the line.
		/// </summary>
		public int End { get; set; }

		/// <summary>
		///     The biggest perpendicular distance index of a point.
		/// </summary>
		public int Perpendicular { get; set; }

		/// <summary>
		///     The max perpendicular distance of a point along the
		///     line.
		/// </summary>
		public double Distance { get; set; }
	}

	/// <summary>
	///     Gets the perpendicular distance of a point to the line between start
	///     and end.
	/// </summary>
	/// <param name="start">The start point of the line.</param>
	/// <param name="end">The end point of the line.</param>
	/// <param name="point">
	///     The point to calculate the perpendicular distance of.
	/// </param>
	/// <returns>The perpendicular distance.</returns>
	private static double GetDistance(Vector2 start, Vector2 end, Vector2 point)
	{
		var x = end.x - start.x;
		var y = end.y - start.y;

		var m = x * x + y * y;

		var u = ((point.x - start.x) * x + (point.y - start.y) * y) / m;

		if (u < 0)
		{
			x = start.x;
			y = start.y;
		}
		else if (u > 1)
		{
			x = end.x;
			y = end.y;
		}
		else
		{
			x = start.x + u * x;
			y = start.y + u * y;
		}

		x = point.x - x;
		y = point.y - y;

		return Math.Sqrt(x * x + y * y);
	}

	/// <summary>
	///     Creates a new <see cref="Segment"/> with the start and end indices.
	///     Calculates the max perpendicular distance for each specified point
	///     against the line between start and end.
	/// </summary>
	/// <param name="start">The start index of the line.</param>
	/// <param name="end">The end index of the line.</param>
	/// <param name="points">The points.</param>
	/// <returns>The Segment</returns>
	/// <remarks>
	///     If the segment doesnt contain enough values to be split again the
	///     segment distance property is left as 0. This ensures that the segment
	///     wont be selected again from the <see cref="Reduce(ref List{Segment},
	///     List{Point}, int, double)"/> part of the algorithm.
	/// </remarks>
	private static Segment CreateSegment(int start, int end, List<Vector2> points)
	{
		var count = end - start;

		if (count >= MinPoints - 1)
		{
			var first = points[start];
			var last = points[end];

			var max = points.GetRange(start + 1, count - 1)
				.Select((point, index) => new
				{
					Index = start + 1 + index,
					Distance = GetDistance(first, last, point)
				}).OrderByDescending(p => p.Distance).First();

			return new Segment
			{
				Start = start,
				End = end,
				Perpendicular = max.Index,
				Distance = max.Distance
			};
		}

		return new Segment
		{
			Start = start,
			End = end,
			Perpendicular = -1
		};
	}

	/// <summary>
	///     Splits the specified segment about the perpendicular index and return
	///     the segment before and after with calculated values.
	/// </summary>
	/// <param name="segment">The segment to split.</param>
	/// <param name="points">The points.</param>
	/// <returns>The two segments.</returns>
	private static IEnumerable<Segment> SplitSegment(Segment segment,
		List<Vector2> points)
	{
		return new[]
		{
			CreateSegment(segment.Start, segment.Perpendicular, points),
			CreateSegment(segment.Perpendicular, segment.End, points)
		};
	}

	/// <summary>
	///     Check to see if the point has valid values and returns false if not.
	/// </summary>
	/// <param name="point">The point to check.</param>
	/// <returns>True if the points values are valid.</returns>
	private static bool IsValid(Vector2 point)
	{
		return !double.IsNaN(point.x) && !double.IsNaN(point.y);
	}

	/// <summary>
	///     Interpolates the sepcified points by reducing until the sepcified
	///     tolerance is met or the specified max number of points is met.
	/// </summary>
	/// <param name="points">The points to reduce.</param>
	/// <param name="max">The max number of points to return.</param>
	/// <param name="tolerance">
	///     The min distance tolerance of points to return.
	/// </param>
	/// <returns>The interpolated reduced points.</returns>
	public static IEnumerable<Vector2> Interpolate(List<Vector2> points, int max,
		double tolerance = 0d)
	{
		if (max < MinPoints || points.Count < max)
		{
			return points;
		}

		var segments = GetSegments(points).ToList();

		Reduce(ref segments, points, max, tolerance);

		return segments
			.OrderBy(p => p.Start)
			.SelectMany((s, i) => GetPoints(s, segments.Count, i, points));
	}

	/// <summary>
	///     Gets the reduced points from the <see cref="Segment"/>. Invalid values
	///     are included in the result as well as last point of the last segment.
	/// </summary>
	/// <param name="segment">The segment to get the indices from.</param>
	/// <param name="count">The total number of segments in the algorithm.</param>
	/// <param name="index">The index of the current segment.</param>
	/// <param name="points">The points.</param>
	/// <returns>The valid points from the segment.</returns>
	private static IEnumerable<Vector2> GetPoints(Segment segment, int count,
		int index, List<Vector2> points)
	{
		yield return points[segment.Start];

		var next = segment.End + 1;

		var isGap = next < points.Count && !IsValid(points[next]);

		if (index == count - 1 || isGap)
		{
			yield return points[segment.End];

			if (isGap)
			{
				yield return points[next];
			}
		}
	}

	/// <summary>
	///     Gets the initial <see cref="Segment"/> for the algorithm. If points
	///     contains invalid values then multiple segments are returned for each
	///     side of the invalid value.
	/// </summary>
	/// <param name="points">The points.</param>
	/// <returns>The segments.</returns>
	private static IEnumerable<Segment> GetSegments(List<Vector2> points)
	{
		var previous = 0;

		foreach (var p in points.Select((p, i) => new
		{
			Vector2 = p,
			Index = i
		})
		.Where(p => !IsValid(p.Vector2)))
		{
			yield return CreateSegment(previous, p.Index - 1, points);

			previous = p.Index + 1;
		}

		yield return CreateSegment(previous, points.Count - 1, points);
	}

	/// <summary>
	///     Reduces the segments until the specified max or tolerance has been met
	///     or the points can no longer be reduced.
	/// </summary>
	/// <param name="segments">The segements to reduce.</param>
	/// <param name="points">The points.</param>
	/// <param name="max">The max number of points to return.</param>
	/// <param name="tolerance">The min distance tolerance for the points.</param>
	private static void Reduce(ref List<Segment> segments, List<Vector2> points,
		int max,
		double tolerance)
	{
		var gaps = points.Count(p => !IsValid(p));

		// Check to see if max numbers has been reached.
		while (segments.Count + gaps < max - 1)
		{
			// Get the largest perpendicular distance segment.
			var current = segments.OrderByDescending(s => s.Distance).First();

			// Check if tolerance has been met yet or can no longer reduce.
			if (current.Distance <= tolerance)
			{
				break;
			}

			segments.Remove(current);

			var split = SplitSegment(current, points);

			segments.AddRange(split);
		}
	}
}
}

