using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.QuestGui
{

[CanEditMultipleObjects]
[CustomEditor(typeof(GridContainer), true)]
public class GridContainerEditor : ControlEditorBase 
{	

	Vector2 m_posCached = Vector2.zero;
	RectCentered m_rect = RectCentered.zero;

	public override void OnInspectorGUI()
	{
		GridContainer grid = (GridContainer)target;			
		
		// Alignment buttons
		//LayoutAlignment("Align Image");
	
		//
		// Manual size
		//		
		
		// show manual image sizer
		SerializedObject serializedObj = new SerializedObject(grid);
		EditorGUI.BeginChangeCheck();

		m_rect.Size = Utils.Snap(EditorGUILayout.Vector2Field("Size:", m_rect.Size));

		if (EditorGUI.EndChangeCheck() )
		{
			grid.Rect = m_rect;
			m_posCached = grid.transform.position;
			EditorUtility.SetDirty(target);
		}
		
		EditorGUI.BeginChangeCheck();
		DrawDefaultInspector();
		
		if (EditorGUI.EndChangeCheck() )
		{
			if ( grid.ItemSpacing.x <= 0 || grid.ItemSpacing.y <= 0 )
				grid.ItemSpacing = grid.ItemSpacing; // Setting this property
		}
	}
	

	public override void OnSceneGUI()
	{	
		// Custom size control (if image is split)
		GridContainer grid = (GridContainer)target;

		OnSceneDrawPivot(grid.transform);

		if ( grid.transform.parent != null )
		{
			if ( m_rect == RectCentered.zero || m_posCached != (Vector2)grid.transform.position )
			{
				m_rect = grid.Rect;		
				m_posCached = grid.transform.position;
			}			

			RectCentered oldBounds = m_rect;			
			RectCentered bounds = OnSceneGuiRectCenter( oldBounds, true );
			if ( bounds != oldBounds )
			{
				m_rect = bounds;
				grid.Rect = bounds;
				m_posCached = grid.transform.position;
				EditorUtility.SetDirty(grid.gameObject);// set whole thing dirty
			}

			// Show the grid of items
			
			Vector2 gridActualSize = grid.Rect.Size;
			
			// vertical lines
			for ( float x = 0; x <= gridActualSize.x; x+=grid.ItemSpacing.x )
				Handles.DrawLine( new Vector2(x+bounds.MinX,bounds.MaxY), new Vector2(x+bounds.MinX,bounds.MaxY-gridActualSize.y));
			// horizontal lines
			for ( float y = 0; y <= gridActualSize.y; y+=grid.ItemSpacing.y )
				Handles.DrawLine( new Vector2(bounds.MinX,bounds.MaxY-y), new Vector2(bounds.MinX+gridActualSize.x,bounds.MaxY-y));

		}	
	}
}
}
