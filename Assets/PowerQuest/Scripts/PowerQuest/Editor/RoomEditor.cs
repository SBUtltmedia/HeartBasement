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
		if ( GUILayout.Button("On Enter Room") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnEnterRoom","", false);
		}
		if ( GUILayout.Button("On Enter Room (After fading in)") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnEnterRoomAfterFade");
		}
		if ( GUILayout.Button("On Exit Room") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnExitRoom", " IRoom oldRoom, IRoom newRoom ");
		}
		if ( GUILayout.Button("Update (Blocking)") )
		{
			QuestScriptEditor.Open(  component, QuestScriptEditor.eType.Room, "UpdateBlocking");
		}
		if ( GUILayout.Button("Update") )
		{
			QuestScriptEditor.Open(  component, QuestScriptEditor.eType.Room, "Update","", false);
		}
		if ( GUILayout.Button("On Any Click") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnAnyClick");
		}
		if ( GUILayout.Button("After Any Click") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "AfterAnyClick");
		}
		if ( GUILayout.Button("On Walk To") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnWalkTo");
		}
		if ( GUILayout.Button("Post-Restore Game") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnPostRestore", " int version ", false);
		}
		if ( GUILayout.Button("Unhandled Interact") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "UnhandledInteract", " IQuestClickable mouseOver ");
		}
		if ( GUILayout.Button("Unhandled Use Inv") )
		{
			QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "UnhandledUseInv", " IQuestClickable mouseOver, IInventory item ");
		}

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
	
	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		PropComponent component = (PropComponent)target;
		if ( component == null ) 
			return;
					
		Prop data = component.GetData();
		float oldBaseline = data.Baseline;
		bool oldBaselineFixed = data.BaselineFixed;
		
		DrawDefaultInspector();
		
		// Update baseline on renderers if it changed
		if ( oldBaseline != data.Baseline || oldBaselineFixed != data.BaselineFixed || m_oldYPos != component.transform.position.y )
			QuestClickableEditorUtils.UpdateBaseline(component.transform, data, data.BaselineFixed);
		m_oldYPos = component.transform.position.y;
		
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

		GUILayout.Space(5);
		EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);
		
		if ( GUILayout.Button("Create Polygon from Sprite") )
		{
			Undo.RecordObject(target, "Polygon from sprite");
			EditorUtils.UpdateClickableCollider(component.gameObject);
			EditorUtility.SetDirty(component.gameObject);
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
	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		DrawDefaultInspector();
		HotspotComponent component = (HotspotComponent)target;

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
	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		DrawDefaultInspector();
		RegionComponent component = (RegionComponent)target;

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
	}

}

}
