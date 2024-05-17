using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{

[CanEditMultipleObjects]
[CustomEditor(typeof(GuiComponent))]
public class GuiComponentEditor : Editor 
{	
	
	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		DrawDefaultInspector();
		GuiComponent component = (GuiComponent)target;
		
		if ( component == null ) return;

		GUILayout.Space(5);
		GUILayout.Label("Script Functions",EditorStyles.boldLabel);
		if (  GUILayout.Button("On Show") )
		{
			QuestScriptEditor.Open( component, "OnShow","",false ); 
		}
		if ( GUILayout.Button("On Any Click") )
		{
			QuestScriptEditor.Open( component, PowerQuest.SCRIPT_FUNCTION_ONANYCLICK, PowerQuestEditor.SCRIPT_PARAMS_ONANYCLICK_GUI ); 
		}
		if ( GUILayout.Button("Update") )
		{
			QuestScriptEditor.Open( component, "Update","",false ); 
		}
		if ( GUILayout.Button("OnPostRestore") )
		{
			QuestScriptEditor.Open( component, "OnPostRestore", " int version ", false);
		}

		GUILayout.Space(5);
		GUILayout.Label("Utils",EditorStyles.boldLabel);
		if ( GUILayout.Button("Rename") )
		{			
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Gui, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}
	
	GameObject m_guiCamera = null;

	public void OnSceneGUI()
	{	
		GuiComponent component = (GuiComponent)target;					
		
		float baselineOffset = 0; // NB: 

		if ( m_guiCamera == null )
			m_guiCamera = GameObject.Find("QuestGuiCamera");
		if ( m_guiCamera != null )
			baselineOffset += m_guiCamera.transform.position.y;

		// offset baseline by the guicamera		
		if ( QuestClickableEditorUtils.OnSceneGUIBaseline( component, component.GetData(), new Vector2(0,baselineOffset) ) ) // show baselines relative to gui
		{
			// Gui baselines are multiplied by 100, and added to control baselines. So each control should sort inside the gui
			// Find all child gui controls
			GuiControl[] controls = component.GetComponentsInChildren<GuiControl>();
			foreach (GuiControl control in controls)
				control.UpdateBaseline();

		}



	/*
		CharacterComponent component = (CharacterComponent)target;
		QuestClickableEditorUtils.OnSceneGUI( component, component.GetData() );
		
		if ( component.GetData().EditorGetSolid() )
		{
			Handles.color = Color.yellow;
			Vector2[] points = component.CalcSolidPoly();		
			Vector2 pos = component.transform.position;
			for (int i = 0; i < points.Length; i++)
				Handles.DrawLine( pos+points[i], pos+(points[(i + 1) % points.Length]) );
		}
		*/
	}
}

}
