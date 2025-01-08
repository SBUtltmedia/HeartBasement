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
	
	PolygonCollider2D m_collider = null;
	
	/* Don't show automatically any more, its annoying /
	bool m_first = false;
	/**/

	public void OnEnable()
	{
		UpdateHoleList();
		m_listHoles = new ReorderableList(m_holes, typeof(PolygonCollider2D),false, true, true, true);
		m_listHoles.drawHeaderCallback = DrawHoleHeader;
		m_listHoles.drawElementCallback = DrawHole;
		m_listHoles.onAddCallback = AddHole;
		//m_listHoles.onSelectCallback = SelectHole;
		m_listHoles.onRemoveCallback = DeleteHole;
		
		/* Don't show automatically any more, its annoying /
		m_first=true;		
		/**/
	}

	void OnDestroy()
	{
	}


	public override void OnInspectorGUI()
	{
		//DrawDefaultInspector();
		WalkableComponent component = (WalkableComponent)target;
		if ( component == null ) return;
		/* Don't show automatically any more, its annoying /
		if ( m_first )
			QuestPolyTool.Show(component);
		m_first = false;
		/**/
		
		if (m_collider == null )
			m_collider = component.GetComponent<PolygonCollider2D>();
					
		if (m_collider != null )
		{ 		
			EditorGUI.BeginChangeCheck();
			GUILayout.Toolbar(QuestPolyTool.Active(m_collider.gameObject)?0:-1, new string[]{"Edit Walkable Area"}, EditorStyles.miniButton);
			if ( EditorGUI.EndChangeCheck())
				QuestPolyTool.Toggle(m_collider.gameObject);

			// Check 
			if ( m_collider.pathCount > 1 )
				EditorGUILayout.HelpBox("NB: Walkable areas currently only support a single polygon", MessageType.Warning);
		}

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
		
		EditorLayouter layout = new EditorLayouter(rect).Stretched.Fixed(100);

		EditorGUI.LabelField(layout,"Index: "+index);

		if ( GUI.Button(layout, "Edit Polygon", EditorStyles.miniButton) )
			QuestPolyTool.Show(hole);
	}

	public void OnSceneGUI()
	{	
		// Draw walkable area (maybe just rely on polygon collider for that...)
		QuestPolyTool.DrawCollider((target as MonoBehaviour).gameObject);

		// Draw holes
		Handles.color = Color.red;
		foreach( PolygonCollider2D hole in m_holes )
		{
			Handles.DrawAAPolyLine(4f,System.Array.ConvertAll<Vector2,Vector3>(hole.points, item=>item));	
			if (  hole.points.Length > 2 )
				Handles.DrawAAPolyLine(4f,hole.points[hole.points.Length-1], hole.points[0]);	
		}
	}

}

}
