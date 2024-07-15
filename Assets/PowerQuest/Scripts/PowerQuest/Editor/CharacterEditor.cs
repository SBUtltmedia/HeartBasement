using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;


namespace PowerTools.Quest
{


[CanEditMultipleObjects]
[CustomEditor(typeof(CharacterComponent))]
public class CharacterComponentEditor : Editor 
{
	float m_oldYPos = float.MaxValue;
	
	[SerializeField]PowerSprite m_spriteComponent = null;

	public override void OnInspectorGUI()
	{
		CharacterComponent component = (CharacterComponent)target;
		if ( component == null ) 
			return;
		if ( m_spriteComponent == null || m_spriteComponent.gameObject != component.gameObject )
			m_spriteComponent = component.transform.GetComponent<PowerSprite>();

		Character data = component.GetData();
		float oldBaseline = data.Baseline;

		bool editingPrefab = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;
		
		if ( editingPrefab == false && component.gameObject.activeInHierarchy )
		{ 
			EditorGUILayout.HelpBox("This is a preview instance of the character. \n\nClick Edit Prefab to make permanent changes. (or hit the Overrides -> Apply All to apply changes you make here)", MessageType.Warning);
		}
		if ( editingPrefab == false && GUILayout.Button("Edit Prefab") ) // edit prefab if not already editing one
		{ 
			Selection.activeObject = target;
			EditorGUIUtility.PingObject(target);
			AssetDatabase.OpenAsset(QuestEditorUtils.GetPrefabParent(component.gameObject,true));
		}	
		
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);	


		if ( editingPrefab )
		{			
			EditorGUI.BeginChangeCheck();
			GUILayout.Toolbar(QuestPolyTool.Active(component.gameObject)?0:-1, new string[]{"Edit Hotspot Shape"}, EditorStyles.miniButton);
			if ( EditorGUI.EndChangeCheck())
				QuestPolyTool.Toggle(component.gameObject);	

			EditorGUI.BeginChangeCheck();
			m_spriteComponent.Offset = EditorGUILayout.Vector2Field(new GUIContent("Character Pivot"), m_spriteComponent.Offset );
			if ( EditorGUI.EndChangeCheck())
			{ 
				EditorUtility.SetDirty(m_spriteComponent);
			}
		}


		DrawDefaultInspector();
		
		// Update baseline on renderers if it changed
		if ( oldBaseline != data.Baseline || m_oldYPos != component.transform.position.y )
			QuestClickableEditorUtils.UpdateBaseline(component.transform, data, false);
		m_oldYPos = component.transform.position.y;

		// Sprite offset from PowerSprite component
		EditorGUI.BeginChangeCheck();
		m_spriteComponent.Offset = EditorGUILayout.Vector2Field(new GUIContent("Character Pivot"), m_spriteComponent.Offset );
		if ( EditorGUI.EndChangeCheck())
		{ 
			EditorUtility.SetDirty(m_spriteComponent);
		}
		
		EditorGUI.BeginChangeCheck();
		GUILayout.Toolbar(QuestPolyTool.Active(component.gameObject)?0:-1, new string[]{"Edit Hotspot Shape"}, EditorStyles.miniButton);
		if ( EditorGUI.EndChangeCheck())
			QuestPolyTool.Toggle(component.gameObject);	
		
		GUILayout.Space(5);
		GUILayout.Label("Script Functions",EditorStyles.boldLabel);
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) && GUILayout.Button("On Interact") )
		{
			QuestScriptEditor.Open( component, PowerQuest.SCRIPT_FUNCTION_INTERACT, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_CHARACTER ); 
		}

		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) && GUILayout.Button("On Look") )
		{
			QuestScriptEditor.Open( component, PowerQuest.SCRIPT_FUNCTION_LOOKAT, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_CHARACTER);
		}

		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) && GUILayout.Button("On Use Item") )
		{
			QuestScriptEditor.Open( component, PowerQuest.SCRIPT_FUNCTION_USEINV, PowerQuestEditor.SCRIPT_PARAMS_USEINV_CHARACTER);
		}

		GUILayout.Space(5);
		GUILayout.Label("Utils",EditorStyles.boldLabel);
		if ( GUILayout.Button("Rename") )
		{			
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Character, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}

	public void OnSceneGUI()
	{		
		CharacterComponent component = (CharacterComponent)target;
		QuestClickableEditorUtils.OnSceneGUI( component, component.GetData(),false );
		
		if ( component.GetData().EditorGetSolid() )
		{
			Handles.color = Color.yellow;
			Vector2[] points = component.CalcSolidPoly();		
			Vector2 pos = component.transform.position;
			for (int i = 0; i < points.Length; i++)
				Handles.DrawLine( pos+points[i], pos+(points[(i + 1) % points.Length]) );
		}
	}


}


// New Character Window
class CreateCharacterWindow : EditorWindow 
{
	string m_prefabName = "";
	string m_path = null;

	public void SetPath( string path )
	{
		m_path = path;
	}

	void OnGUI() 
	{
		if (  string.IsNullOrEmpty(m_path) )
			m_path = QuestEditorUtils.FindCurrentPath();

		m_prefabName = EditorGUILayout.TextField("Choose Name", m_prefabName).Trim();
		EditorGUILayout.HelpBox("Short and Unique- eg: 'Frank' or 'ManOfLowMoralFiber'",MessageType.None);
		EditorGUILayout.LabelField("Creates Prefab At", m_path+"/"+m_prefabName+"/Character"+m_prefabName+".prefab");

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Create")) 
		{
			PowerQuestEditor.CreateCharacter(m_path, m_prefabName);
			Close();
		}
		if (GUILayout.Button("Cancel")) 
		{
			Close();
		}
		GUILayout.EndHorizontal();
	}
	void OnDestroy()
	{
		m_path = null;
	}
}
}
