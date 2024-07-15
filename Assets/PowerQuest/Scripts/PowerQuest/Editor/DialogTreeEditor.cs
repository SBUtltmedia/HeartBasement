using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.Quest
{


[CanEditMultipleObjects]
[CustomEditor(typeof(DialogTreeComponent))]
public class DialogTreeComponentEditor : Editor 
{
	ReorderableList m_listOptions = null;

	public void OnEnable()
	{
		DialogTreeComponent component = (DialogTreeComponent)target;

		m_listOptions = new ReorderableList(component.GetData().Options, typeof(DialogOption),true, true, true, true);
		m_listOptions.drawHeaderCallback = DrawOptionHeader;
		m_listOptions.drawElementCallback = DrawOption;
		m_listOptions.onAddCallback = AddOption;
		//m_listOptions.onRemoveCallback = DeleteOption;
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		DialogTreeComponent component = (DialogTreeComponent)target;
		if ( component == null ) return;
		
		GUILayout.Label( "Script Functions", EditorStyles.boldLabel);

		if ( GUILayout.Button("Edit Script") )
		{
			// Open the script
			QuestScriptEditor.Open(  component );	
		}

		if ( GUILayout.Button("On Start") )
		{
			QuestScriptEditor.Open( component, PowerQuest.SCRIPT_FUNCTION_DIALOG_START,"", true);
		}

		if ( GUILayout.Button("On Stop") )
		{
			QuestScriptEditor.Open( component, PowerQuest.SCRIPT_FUNCTION_DIALOG_STOP,"", true);
		}

		GUILayout.Space(10);
		EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
		serializedObject.Update();
		if ( m_listOptions != null )
		{
			m_listOptions.list = component.GetData().Options; // Had to add this, list was getting detatched from the component somehow.
			m_listOptions.DoLayoutList();
		}
				
		GUILayout.Label( "Debug", EditorStyles.boldLabel);				
		EditorGUI.BeginDisabledGroup( Application.isPlaying == false || PowerQuest.Exists == false );
		if ( GUILayout.Button("Test Dialog") )
		{
			PowerQuest.Get.StartDialog(component.GetData().ScriptName);
		}

		EditorGUI.EndDisabledGroup();

		EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);
		if ( GUILayout.Button("Rename") )
		{
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Dialog, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}

		if (GUI.changed)
			EditorUtility.SetDirty(target);
		
	}


	void AddOption(ReorderableList list)
	{
		int index = list.index+1;
		if ( index <= 0 )
			index = list.count;
		
		DialogTreeComponent component = (DialogTreeComponent)target;
		DialogOption option = new DialogOption();
		option.Name = (index+1).ToString();
		option.Visible = true;
		option.Text = "Option "+(index+1);
		component.GetData().Options.Insert(index,option);

		EditorUtility.SetDirty(target);
		
		QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.DialogOptions);
	}

	void DrawOptionHeader(Rect rect)
	{

		float offset = rect.x+12;
		EditorGUI.LabelField(new Rect(offset, rect.y, 80, EditorGUIUtility.singleLineHeight), "Id" );
		offset += 65;
		EditorGUI.LabelField(new Rect(offset, rect.y, 50, EditorGUIUtility.singleLineHeight), "Show" );
		offset += 40;
		EditorGUI.LabelField(new Rect(offset, rect.y, 50, EditorGUIUtility.singleLineHeight), "Text" );
	}

	void DrawOption(Rect rect, int index, bool isActive, bool isFocused)
	{
		DialogTreeComponent component = (DialogTreeComponent)target;
		if ( component == null )
			return;
		DialogOption option = component.GetData().Options[index];
		if ( option == null )
			return;
		
		rect.y += 2;

		float totalFixedWidth = 82+20+50;//+34+34+20;
		float offset = rect.x;		
		string newName = option.Name;
		newName = EditorGUI.DelayedTextField(new Rect(offset, rect.y,80, EditorGUIUtility.singleLineHeight), option.Name ).Trim();
		// Check newName != option.Name
		if ( newName != option.Name )
		{
			// Ensure name is valid
			if (newName.Length == 1 )
				newName = newName.ToUpperInvariant();
			if ( newName.Length > 1 )
				newName = (newName[0].ToString().ToUpperInvariant() + newName.Substring(1));
			
			// Remove spaces
			int spaceInd = newName.IndexOf(' ');
			while ( spaceInd >= 0 && spaceInd < newName.Length-1 )
			{
				newName = newName.Remove(spaceInd,1);
				newName = newName.Substring(0,spaceInd) + newName[spaceInd].ToString().ToUpperInvariant() + newName.Substring(spaceInd+1);
				spaceInd = newName.IndexOf(' ');
			}

			// remove non word characters		
			newName = Regex.Replace(newName, @"(\W)", "");

			// Finally, check for duplicates
			DialogOption duplicate = component.GetData().Options.Find(item=>item.Name == newName);
			if ( duplicate != null && duplicate != option )
			{
				EditorUtility.DisplayDialog("Invalid Id", "Dialog option ids must be unique", "Ok");
			}
			else if ( string.IsNullOrEmpty(newName) )
			{
				EditorUtility.DisplayDialog("Invalid Id", "Dialog option ids cannot be empty", "Ok");
			}
			else 
			{
				option.Name = newName;
				QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.DialogOptions);
			}			
		}

		offset += 82;
		option.Visible = GUI.Toggle(new Rect(offset, rect.y, 20, EditorGUIUtility.singleLineHeight), option.Visible,"" );
		offset += 20;
		option.Text = EditorGUI.TextField(new Rect(offset, rect.y, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), option.Text );
		offset += rect.width - totalFixedWidth;
		if ( GUI.Button(new Rect(offset, rect.y, 50, EditorGUIUtility.singleLineHeight), "Script", EditorStyles.miniButton ) )
		{
			QuestScriptEditor.Open( component, PowerQuest.SCRIPT_FUNCTION_DIALOG_OPTION+option.Name, " IDialogOption option " );
		}
		offset += 50;
		/*
		if ( GUI.Button(new Rect(offset, rect.y, 20, EditorGUIUtility.singleLineHeight), "x", EditorStyles.miniButtonRight ) )
		{
			// Delete
			DeleteOption(index);
		}
		offset += 20;
		*/
	}


}

}
