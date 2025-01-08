using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.Quest
{


//
// Room Editor
//
[CanEditMultipleObjects]
[CustomEditor(typeof(RoomComponent))]
public class RoomComponentEditor : Editor 
{	

	public void OnEnable()
	{
		RoomComponent component = (RoomComponent)target;
		component.EditorUpdateChildComponents();


	}

	void ScriptButton(string description, string functionName, string parameters = "", bool isCoroutine = true )
	{ 
		bool bold = PowerQuestEditor.Get.HighlightMethodButton(functionName);
		if ( GUILayout.Button(description, QuestEditorUtils.EditorStylesBold.Button(bold)) )
			QuestScriptEditor.Open( (RoomComponent)target, QuestScriptEditor.eType.Room, functionName, parameters, isCoroutine);
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		DrawDefaultInspector();
		RoomComponent component = (RoomComponent)target;

		//
		// Script functions
		//	

		GUILayout.Space(5);
		//GUILayout.Label("Script Functions",EditorStyles.centeredGreyMiniLabel);
		EditorGUILayout.LabelField("Script Functions", EditorStyles.boldLabel);

		PowerQuestEditor.Get.BeginHighlightingMethodButtons(component.GetData());
		
		ScriptButton("On Enter Room (BG)", "OnEnterRoom","", false);
		ScriptButton("On Enter Room After Fade", "OnEnterRoomAfterFade");		
		ScriptButton("On Exit Room", "OnExitRoom", " IRoom oldRoom, IRoom newRoom ");
		ScriptButton("Update Blocking", "UpdateBlocking");
		ScriptButton("Update (BG)", "Update","", false);		
		ScriptButton("On Parser", "OnParser");
		ScriptButton("On Any Click", "OnAnyClick");
		ScriptButton("After Any Click", "AfterAnyClick");
		ScriptButton("On Walk To", "OnWalkTo");
		ScriptButton("Post-Restore Game (BG)", "OnPostRestore", " int version ", false);			
		ScriptButton("Unhandled Interact", "UnhandledInteract", " IQuestClickable mouseOver ");
		ScriptButton("Unhandled Look At", "UnhandledLookAt", " IQuestClickable mouseOver ");
		ScriptButton("Unhandled Use Inv", "UnhandledUseInv", " IQuestClickable mouseOver, IInventory item ");
		
		PowerQuestEditor.Get.EndHighlightingMethodButtons();

		GUILayout.Space(5);
		EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);
		if ( GUILayout.Button("Rename") )
		{
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Room, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}


	public void OnSceneGUI()
	{
		GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel);
		
		float scale = QuestEditorUtils.GameResScale;

		RoomComponent component = (RoomComponent)target;
		RectCentered roomBounds = component.GetData().Bounds;
		RectCentered roomScrollBounds = component.GetData().ScrollBounds;
		{

			// Draw room camera bounds
			if ( roomBounds.Width > 0 || roomBounds.Height > 0 )
			{
				// Show scroll stuff

				Handles.color = new Color(1,0.6f,0);
				GUI.color = new Color(1,0.6f,0);
				textStyle.normal.textColor = GUI.color;
				{
					Vector3 position =  new Vector3( roomScrollBounds.MinX, roomScrollBounds.MinY, 0);
					position = Handles.FreeMoveHandle( position+Vector3.one*scale, Quaternion.identity,1.0f*scale,new Vector3(1,1,0),Handles.DotHandleCap)-Vector3.one*scale;
					Handles.Label(position + new Vector3(5*scale,0,0), "Scroll", textStyle);
					position.x = Mathf.Min(position.x,roomScrollBounds.MaxX);
					position.y = Mathf.Min(position.y,roomScrollBounds.MaxY);
					position.x = Mathf.Clamp(position.x,roomBounds.MinX, roomBounds.MaxX);
					position.y = Mathf.Clamp(position.y,roomBounds.MinY, roomBounds.MaxY);
					roomScrollBounds.Min = Utils.SnapRound(position,PowerQuestEditor.SnapAmount*0.5f);
				}
				{
					Vector3 position =  new Vector3( roomScrollBounds.MaxX, roomScrollBounds.MaxY, 0);
					position = Handles.FreeMoveHandle( position-Vector3.one*scale, Quaternion.identity,1.0f*scale,new Vector3(1,1,0),Handles.DotHandleCap)+Vector3.one*scale;

					position.x = Mathf.Max(position.x,roomScrollBounds.MinX);
					position.y = Mathf.Max(position.y,roomScrollBounds.MinY);
					position.x = Mathf.Clamp(position.x,roomBounds.MinX, roomBounds.MaxX);
					position.y = Mathf.Clamp(position.y,roomBounds.MinY, roomBounds.MaxY);
					roomScrollBounds.Max = Utils.SnapRound(position,PowerQuestEditor.SnapAmount*0.5f);
				}

				Handles.DrawLine( roomScrollBounds.Min, new Vector2(roomScrollBounds.Min.x,roomScrollBounds.Max.y) );
				Handles.DrawLine( roomScrollBounds.Min, new Vector2(roomScrollBounds.Max.x,roomScrollBounds.Min.y) );
				Handles.DrawLine( roomScrollBounds.Max, new Vector2(roomScrollBounds.Min.x,roomScrollBounds.Max.y) );
				Handles.DrawLine( roomScrollBounds.Max, new Vector2(roomScrollBounds.Max.x,roomScrollBounds.Min.y) );
			}

			// Draw room camera bounds
			Handles.color = Color.yellow;
			GUI.color = Color.yellow;
			textStyle.normal.textColor = GUI.color;
			{
				Vector3 position =  new Vector3( roomBounds.MinX, roomBounds.MinY, 0);
				position = Handles.FreeMoveHandle( position+Vector3.one*scale, Quaternion.identity,1.0f*scale,new Vector3(0,1,0),Handles.DotHandleCap)-Vector3.one*scale;
				Handles.Label(position + new Vector3(5*scale,0,0), "Bounds", textStyle );
				//Handles.color = Color.yellow.WithAlpha(0.5f);
				position.x = Mathf.Min(position.x,roomBounds.MaxX);
				position.y = Mathf.Min(position.y,roomBounds.MaxY);
				roomBounds.Min = Utils.SnapRound(position,PowerQuestEditor.SnapAmount);
			}
			{
				Vector3 position =  new Vector3( roomBounds.MaxX, roomBounds.MaxY, 0);
				position = Handles.FreeMoveHandle( position-Vector3.one*scale, Quaternion.identity,1.0f*scale,new Vector3(0,1,0),Handles.DotHandleCap)+Vector3.one*scale;

				position.x = Mathf.Max(position.x,roomBounds.MinX);
				position.y = Mathf.Max(position.y,roomBounds.MinY);
				roomBounds.Max = Utils.SnapRound(position,PowerQuestEditor.SnapAmount);
			}

			Handles.DrawLine( roomBounds.Min, new Vector2(roomBounds.Min.x,roomBounds.Max.y) );
			Handles.DrawLine( roomBounds.Min, new Vector2(roomBounds.Max.x,roomBounds.Min.y) );
			Handles.DrawLine( roomBounds.Max, new Vector2(roomBounds.Min.x,roomBounds.Max.y) );
			Handles.DrawLine( roomBounds.Max, new Vector2(roomBounds.Max.x,roomBounds.Min.y) );
		}


		if ( roomScrollBounds != component.GetData().ScrollBounds )
		{
			component.GetData().SetScrollSize(roomScrollBounds);
			EditorUtility.SetDirty(target);
		}

		if ( roomBounds != component.GetData().Bounds )
		{
			component.GetData().SetSize(roomBounds);
			EditorUtility.SetDirty(target);
		}


	}


	public static void ApplyInstancePrefab(GameObject gameobj, bool force = true)
	{
		#if UNITY_2018_3_OR_NEWER
			PrefabInstanceStatus prefabType = PrefabUtility.GetPrefabInstanceStatus(gameobj);
			if ( (prefabType == PrefabInstanceStatus.Connected || prefabType == PrefabInstanceStatus.Disconnected )
				&& ((PrefabUtility.GetPropertyModifications(gameobj) != null && PrefabUtility.GetPropertyModifications(gameobj).Length > 0) || force) )
			{
				PrefabUtility.SaveAsPrefabAssetAndConnect( PrefabUtility.GetOutermostPrefabInstanceRoot(gameobj), QuestEditorUtils.GetPrefabPath(gameobj), InteractionMode.AutomatedAction);
			}			
		#else
			PrefabType prefabType = PrefabUtility.GetPrefabType(gameobj);
			if ( (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance )
				&& ((PrefabUtility.GetPropertyModifications(gameobj) != null && PrefabUtility.GetPropertyModifications(gameobj).Length > 0) || force) )
			{
				PrefabUtility.ReplacePrefab(PrefabUtility.FindValidUploadPrefabInstanceRoot(gameobj), PrefabUtility.GetPrefabParent(gameobj),ReplacePrefabOptions.ConnectToPrefab);
			}
		#endif
	}

}


//
// Prop Editor
//
[CanEditMultipleObjects]
[CustomEditor(typeof(PropComponent))]
public class PropComponentEditor : Editor 
{	
	float m_oldYPos = float.MaxValue;
	PolygonCollider2D m_collider = null;
	
	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		PropComponent component = (PropComponent)target;
		if ( component == null ) 
			return;

		if (m_collider == null )
			m_collider = component.GetComponent<PolygonCollider2D>();
					
		if (m_collider != null )
		{ 		
			EditorGUI.BeginChangeCheck();
			GUILayout.Toolbar(QuestPolyTool.Active(m_collider.gameObject)?0:-1, new string[]{"Edit Hotspot Shape"}, EditorStyles.miniButton);
			if ( EditorGUI.EndChangeCheck())
				QuestPolyTool.Toggle(m_collider.gameObject);	
		}

		Prop data = component.GetData();
		float oldBaseline = data.Baseline;
		bool oldBaselineFixed = data.BaselineFixed;
		
		DrawDefaultInspector();
		
		// Update baseline on renderers if it changed
		if ( oldBaseline != data.Baseline || oldBaselineFixed != data.BaselineFixed || m_oldYPos != component.transform.position.y )
			QuestClickableEditorUtils.UpdateBaseline(component.transform, data, data.BaselineFixed);
		m_oldYPos = component.transform.position.y;
		
		if (m_collider != null )
		{ 			

			GUILayout.Space(5);
			GUILayout.Label("Script Functions",EditorStyles.boldLabel);
			if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) && GUILayout.Button("On Interact") )
			{
				RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

				QuestScriptEditor.Open( room, QuestScriptEditor.eType.Prop,
					PowerQuest.SCRIPT_FUNCTION_INTERACT_PROP+ component.GetData().ScriptName,
					PowerQuestEditor.SCRIPT_PARAMS_INTERACT_PROP);
			}
			if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) && GUILayout.Button("On Look") )
			{
				RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

				QuestScriptEditor.Open( room, QuestScriptEditor.eType.Prop,
					PowerQuest.SCRIPT_FUNCTION_LOOKAT_PROP+ component.GetData().ScriptName,
					PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_PROP);
			}
			if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) && GUILayout.Button("On Use Item") )
			{
				RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

				QuestScriptEditor.Open( room, QuestScriptEditor.eType.Prop,
					PowerQuest.SCRIPT_FUNCTION_USEINV_PROP+ component.GetData().ScriptName,
					PowerQuestEditor.SCRIPT_PARAMS_USEINV_PROP);
			}
		}

		GUILayout.Space(5);
		EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);
		
		if (m_collider != null ) 
		{
			if ( GUILayout.Button("Create Polygon from Sprite") )
			{
				Undo.RecordObject(target, "Polygon from sprite");
				EditorUtils.UpdateClickableCollider(component.gameObject);
				EditorUtility.SetDirty(component.gameObject);
			}
		}
		else 
		{ 
			if ( GUILayout.Button("Make Clickable") )
			{
				Undo.RecordObject(component,"Made prop clickable");
				data.Clickable=true;
				m_collider = component.gameObject.AddComponent<PolygonCollider2D>();
				m_collider.isTrigger = true;
				EditorUtility.SetDirty(m_collider.gameObject);
			}
		}
		if ( GUILayout.Button("Rename") )
		{
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Prop, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}

	public void OnSceneGUI()
	{		
		PropComponent component = (PropComponent)target;
		QuestClickableEditorUtils.OnSceneGUI( component, component.GetData(), component.GetData().BaselineFixed );
	}
}


//
// Hotspot Editor
//
[CanEditMultipleObjects]
[CustomEditor(typeof(HotspotComponent))]
public class HotspotComponentEditor : Editor 
{	
	PolygonCollider2D m_collider = null;

	public override void OnInspectorGUI()
	{
	
		HotspotComponent component = (HotspotComponent)target;

		if (m_collider == null )
			m_collider = component.GetComponent<PolygonCollider2D>();
					
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);

		if (m_collider != null )
		{ 		
			EditorGUI.BeginChangeCheck();
			GUILayout.Toolbar(QuestPolyTool.Active(m_collider.gameObject)?0:-1, new string[]{"Edit Hotspot Shape"}, EditorStyles.miniButton);
			if ( EditorGUI.EndChangeCheck())
				QuestPolyTool.Toggle(m_collider.gameObject);	
		}

		DrawDefaultInspector();

		GUILayout.Space(5);
		GUILayout.Label("Script Functions",EditorStyles.boldLabel);
		if (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) &&  GUILayout.Button("On Interact") )
		{
			RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

			QuestScriptEditor.Open( room, QuestScriptEditor.eType.Hotspot,
				PowerQuest.SCRIPT_FUNCTION_INTERACT_HOTSPOT+ component.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_INTERACT_HOTSPOT);
		}
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) && GUILayout.Button("On Look") )
		{
			RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

			QuestScriptEditor.Open( room, QuestScriptEditor.eType.Hotspot,
				PowerQuest.SCRIPT_FUNCTION_LOOKAT_HOTSPOT+ component.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_HOTSPOT);
		}
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) && GUILayout.Button("On Use Inventory Item") )
		{
			RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

			QuestScriptEditor.Open( room, QuestScriptEditor.eType.Hotspot,
				PowerQuest.SCRIPT_FUNCTION_USEINV_HOTSPOT+ component.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_USEINV_HOTSPOT);
		}

		GUILayout.Space(5);
		EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);
		if ( GUILayout.Button("Rename") )
		{
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Hotspot, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}

	public void OnSceneGUI()
	{		
		HotspotComponent component = (HotspotComponent)target;
		QuestClickableEditorUtils.OnSceneGUI( component, component.GetData(), true );
	}
}


//
// Region Editor
//
[CanEditMultipleObjects]
[CustomEditor(typeof(RegionComponent))]
public class RegionComponentEditor : Editor 
{
	
	PolygonCollider2D m_collider = null;
	
	/* Don't show automatically any more, its annoying /
	bool m_first = false;
	public void OnEnable()
	{	
		m_first = true;
	}
	/**/



	public override void OnInspectorGUI()
	{
	
		RegionComponent component = (RegionComponent)target;
		if ( component == null )
			return;
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
			GUILayout.Toolbar(QuestPolyTool.Active(m_collider.gameObject)?0:-1, new string[]{"Edit Hotspot Shape"}, EditorStyles.miniButton);
			if ( EditorGUI.EndChangeCheck())
				QuestPolyTool.Toggle(m_collider.gameObject);	
		}

		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		DrawDefaultInspector();

		GUILayout.Space(5);
		GUILayout.Label("Script Functions",EditorStyles.boldLabel);
		GUILayout.Label("Blocking functions");

		if ( GUILayout.Button("On Character Enter") )
		{
			RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

			QuestScriptEditor.Open( room, QuestScriptEditor.eType.Region,
				PowerQuest.SCRIPT_FUNCTION_ENTER_REGION+ component.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_ENTER_REGION);
		}

		if ( GUILayout.Button("On Character Exit") )
		{
			RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

			QuestScriptEditor.Open( room, QuestScriptEditor.eType.Region,
				PowerQuest.SCRIPT_FUNCTION_EXIT_REGION+ component.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_EXIT_REGION);
		}
		GUILayout.Label("Background functions");
		GUILayout.Label("  (always trigger, even in sequences)");

		if (GUILayout.Button("On Character Enter BG"))
		{
			RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

			QuestScriptEditor.Open(room, QuestScriptEditor.eType.Region,
				PowerQuest.SCRIPT_FUNCTION_ENTER_REGION_BG + component.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_ENTER_REGION, false);
		}

		if ( GUILayout.Button("On Character Exit BG") )
		{
			RoomComponent room = component.transform.parent.GetComponent<RoomComponent>();

			QuestScriptEditor.Open( room, QuestScriptEditor.eType.Region,
				PowerQuest.SCRIPT_FUNCTION_EXIT_REGION_BG+ component.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_EXIT_REGION, false);
		}


		GUILayout.Space(5);
		EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);
		if ( GUILayout.Button("Rename") )
		{
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Region, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}

	public void OnSceneGUI()
	{	
		// REgion doesn't have clickable utils (baseline, walkto, etc)	
		//RegionComponent component = (RegionComponent)target;
		//QuestClickableEditorUtils.OnSceneGUI( component.transform, component.GetData() );

		// Draw walkable area (maybe just rely on polygon collider for that...)
		QuestPolyTool.DrawCollider((target as MonoBehaviour).gameObject);

	}

}

}
