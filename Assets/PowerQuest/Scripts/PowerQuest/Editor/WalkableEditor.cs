using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;
using System.Reflection;

namespace PowerTools.Quest
{


[CanEditMultipleObjects]
[CustomEditor(typeof(WalkableComponent))]
public class WalkableComponentEditor : Editor 
{
	ReorderableList m_listHoles = null;
	List<PolygonCollider2D> m_holes = new List<PolygonCollider2D>();

	Editor m_holeEditor = null;
	int m_holeEditingId = -1;

	public void OnEnable()
	{
		UpdateHoleList();
		m_listHoles = new ReorderableList(m_holes, typeof(PolygonCollider2D),false, true, true, true);
		m_listHoles.drawHeaderCallback = DrawHoleHeader;
		m_listHoles.drawElementCallback = DrawHole;
		m_listHoles.onAddCallback = AddHole;
		//m_listHoles.onSelectCallback = SelectHole;
		m_listHoles.onRemoveCallback = DeleteHole;
	}

	void OnDestroy()
	{
		if ( m_holeEditor != null ) 
			Editor.DestroyImmediate(m_holeEditor);
		m_holeEditingId = -1;
	}

	public override void OnInspectorGUI()
	{
		//DrawDefaultInspector();
		WalkableComponent component = (WalkableComponent)target;
		if ( component == null ) return;

		GUILayout.Space(10);

		/*
		if ( GUILayout.Button("Edit Script") )
		{
			// Open the script
			QuestScriptEditor.Open(  component );	
		}
		GUILayout.Space(10);
		*/

		//EditorGUILayout.LabelField("Holes", EditorStyles.boldLabel);
		serializedObject.Update();
		if ( m_listHoles != null ) m_listHoles.DoLayoutList();

		if ( m_holeEditor != null && m_holeEditor.target != null )
		{
			GUILayout.Space(10);
			GUILayout.Label("Editing Hole "+m_holeEditingId.ToString()+":", EditorStyles.boldLabel);
			m_holeEditor.OnInspectorGUI();
		} 

		if (GUI.changed)
			EditorUtility.SetDirty(target);
		
	}

	void UpdateHoleList()
	{
		WalkableComponent component = (WalkableComponent)target;
		if ( component == null )
			return;
		m_holes.Clear();
		m_holes.AddRange( System.Array.FindAll(component.GetComponentsInChildren<PolygonCollider2D>(), item=>item.transform != component.transform) );		
	}

	void AddHole(ReorderableList list)
	{
		WalkableComponent component = (WalkableComponent)target;
		if ( component == null )
			return;

		// Create game object
		GameObject gameObject = new GameObject("Hole"+list.count, typeof(PolygonCollider2D)) as GameObject; 
		gameObject.transform.parent = component.transform;

		PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();
		collider.isTrigger = true;
		collider.points = PowerQuestEditor.DefaultColliderPoints;

		RoomComponentEditor.ApplyInstancePrefab(component.gameObject);

		EditorUtility.SetDirty(target);

		UpdateHoleList();

	}

	void DeleteHole(ReorderableList list)
	{
		WalkableComponent component = (WalkableComponent)target;
		if ( component == null ) 
			return;

		int index = list.index;
		// if index is -1, deletes the end
		if ( index < 0 ) 
			index = list.count-1;
		if ( index >= m_holes.Count )
			return;
		PolygonCollider2D hole = m_holes[index];
		if ( hole == null )
			return;
		//Selection.activeObject = hole;

		if ( m_holeEditor != null ) 
			Editor.DestroyImmediate(m_holeEditor);
		m_holeEditingId = -1;

		/**/

		// if index is -1, deletes the end
		if ( hole != null )
		{
			#if UNITY_2018_3_OR_NEWER
				// FUck me I want to die this is so horrrible damn you new unity prefab API!!

				// Load the prefab instance
				string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject);
				GameObject instancedObject = PrefabUtility.LoadPrefabContents(assetPath);

				// Find object to delete
				RoomComponent instancedRoom = instancedObject.GetComponent<RoomComponent>();
				RoomComponent prefabRoom = component.GetComponentInParent<RoomComponent>();
				int walkableIndex = prefabRoom.GetWalkableAreas().FindIndex(item=>item==component);				
				WalkableComponent instancedComponent = instancedRoom.GetWalkableAreas()[walkableIndex];				
				int holeindex = hole.transform.GetSiblingIndex();
				// Destroy the object
				GameObject.DestroyImmediate(instancedComponent.transform.GetChild(holeindex).gameObject);

				// Save the prefab instance
				PrefabUtility.SaveAsPrefabAsset(instancedObject, assetPath);
				PrefabUtility.UnloadPrefabContents(instancedObject);
			#else
				// Destroy the object
				GameObject.DestroyImmediate(hole.gameObject);				
			#endif
		}		


		RoomComponentEditor.ApplyInstancePrefab(component.gameObject);

		EditorUtility.SetDirty(target);
		UpdateHoleList();

	} 


	void DrawHoleHeader(Rect rect)
	{
		EditorGUI.LabelField(rect, "Holes" );
	}


	void DrawHole(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( index >= m_holes.Count )
			return;
		PolygonCollider2D hole = m_holes[index];
		if ( hole == null )
			return;
		
		//rect.y += 2;

		float totalFixedWidth = 50+120;
		float offset = rect.x;
		rect = new Rect(offset, rect.y, rect.width - totalFixedWidth, EditorGUIUtility.singleLineHeight);
		EditorGUI.LabelField(rect,"Index: "+index);

		//hole.Name = EditorGUI.TextField(new Rect(offset, rect.y,80, EditorGUIUtility.singleLineHeight), hole.Name );
		//if ( hole.Name.Length > 1 )
			//hole.Name = hole.Name[0].ToString().ToUpperInvariant() + hole.Name.Substring(1);

		rect.x = rect.x+rect.width;
		rect.width = 50;
		if ( GUI.Button(rect, "Select" ) )
		{
			//  Select the child 
			Selection.activeObject = hole;
		}

		rect.x = rect.x+rect.width;
		rect.width = 120;

		if ( m_holeEditingId == index && m_holeEditor != null )
		{
			if ( GUI.Button(rect, "Hide Polygon Editor", EditorStyles.miniButton) )
			{
				Editor.DestroyImmediate(m_holeEditor);
				m_holeEditingId = -1;
			}
		}
		else if ( GUI.Button(rect, "Show Polygon Editor", EditorStyles.miniButton) )
		{
			if ( m_holeEditor != null ) 
				Editor.DestroyImmediate(m_holeEditor);
			m_holeEditor = Editor.CreateEditor(hole);
			m_holeEditingId = index;
		}

		/*
		rect.x = rect.x+rect.width;
		rect.width = 20;

		if ( GUI.Button(rect, "x", EditorStyles.miniButtonRight ) )
		{
			// Delete
			m_listHoles.index = index;
			DeleteHole(m_listHoles);
		}
		offset += 20;
		*/

	}

	void SelectHole(ReorderableList list)
	{
		if ( list.index >= m_holes.Count )
			return;
		PolygonCollider2D hole = m_holes[list.index];
		if ( hole == null )
			return;
		//Selection.activeObject = hole;


		if ( m_holeEditor != null ) 
			Editor.DestroyImmediate(m_holeEditor);
		m_holeEditor = Editor.CreateEditor(hole);
		m_holeEditingId = list.index;

	}


	public void OnSceneGUI()
	{	
		// Draw walkable area (maybe just rely on polygon collider for that...

		// Draw holes
		Handles.color = Color.red;
		foreach( PolygonCollider2D hole in m_holes )
		{
			Handles.DrawAAPolyLine(4f,System.Array.ConvertAll<Vector2,Vector3>(hole.points, item=>item));	
			if (  hole.points.Length > 2 )
				Handles.DrawAAPolyLine(4f,hole.points[hole.points.Length-1], hole.points[0]);	
		}

				
		// Update walkable area editor
		if ( m_holeEditor != null && m_holeEditor.target != null )
		{
			//GUILayout.Label("Walkable Area "+m_walkableAreaEditingId.ToString()+":", EditorStyles.boldLabel);
			MethodInfo methodInfo = m_holeEditor.GetType().GetMethod("OnSceneGUI"); //.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
			if ( methodInfo != null )
			{
				methodInfo.Invoke(m_holeEditor,null);
			}
		}
	}

	/*
	static Vector2[] ReversedPoly(Vector2[] poly)
	{
		Vector2 result = new Vector2[poly.Length];
		for (int i = 0; i < poly.Length; ++i )
			result[i] = poly[poly.Length-i];
	}*/

}

}
