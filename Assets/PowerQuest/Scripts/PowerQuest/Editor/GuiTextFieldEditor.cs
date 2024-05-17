using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.QuestGui
{

[CanEditMultipleObjects]
[CustomEditor(typeof(TextField), true)]
public class TextFieldEditor : ControlEditorBase 
{	
	//SpriteRenderer m_spriteComponent = null;
	//SpriteRenderer m_spriteComponentHandle = null;
	//FitToObject m_stretchComponent = null;	

	public override void OnInspectorGUI()
	{
		TextField component = (TextField)target;			
		if ( component == null ) return;			
		
		SpriteRenderer spriteComponent = component.GetComponentInChildren<SpriteRenderer>();		
		QuestText textComponent = component.GetComponentInChildren<QuestText>();
		//m_stretchComponent = m_spriteComponent.GetComponentInChildren<FitToObject>(true);
		GuiComponent guiComponent = component.GetComponentInParent<GuiComponent>();
			
		
		/////////////////////////////////////////////////////////////////////////////////////
		// Script functions

		//GUILayout.Space(5);
		GUILayout.Label("Script Functions",EditorStyles.boldLabel);
		if ( GUILayout.Button("On Click") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_CLICKGUI+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI ); 
		}
		if ( GUILayout.Button("On Keyboard Focus") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_ONKBFOCUS+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI ); 
		}
		if ( GUILayout.Button("On Keyboard Defocus") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_ONKBDEFOCUS+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI ); 
		}
		if ( GUILayout.Button("On Edit Text") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_ONTEXTEDIT+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI ); 
		}
		if ( GUILayout.Button("On Confirm Text") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_ONTEXTCONFIRM+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI ); 
		}
		

		GUILayout.Space(5);

		/////////////////////////////////////////////////////////////////////////////////////
		// Setup


		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
				
		/////////////////////////////////////////////////////////////////////////////////////
		// Text field
		//
		if( textComponent != null )
		{
			string text = textComponent.text;
			EditorGUILayout.LabelField("Text");
			text = EditorGUILayout.TextArea(textComponent.text);
			if ( string.Equals(text,textComponent.text) == false )
			{
				textComponent.text = text;
				//UpdateSize(component, stretchComponent,collider2D);
				EditorUtility.SetDirty(target);
			}
		}
			
		//
		// Text padding
		//
		SerializedObject serializedObj = new SerializedObject(component);		
		if ( spriteComponent != null && spriteComponent.drawMode != SpriteDrawMode.Simple )
		{			
			// show text padding
			SerializedProperty prop = serializedObj.FindProperty("m_textPadding");
			EditorGUILayout.PropertyField(prop,new GUIContent("Text Padding"),true);
			if ( serializedObj.ApplyModifiedProperties() )
			{
				// On change, update text align offset
				if ( textComponent != null && textComponent.GetComponent<AlignToObject>())
					textComponent.GetComponent<AlignToObject>().m_offset = component.TextPadding;
				EditorUtility.SetDirty(target);
			}
		}

		//
		// Manual size
		//	
		if ( /*(m_stretchComponent == null || m_stretchComponent.enabled==false) &&*/ spriteComponent != null && spriteComponent.drawMode != SpriteDrawMode.Simple )
		{			
			// show manual image sizer
			SerializedProperty prop = serializedObj.FindProperty("m_customSize");
			if ( prop == null )
				Debug.LogError("Didn't find property");
			EditorGUILayout.PropertyField(prop,new GUIContent("Size"),true);
			if ( serializedObj.ApplyModifiedProperties() )
			{
				// On change, update collider
				RectCentered customSizeRect = component.CustomSize;

				if ( spriteComponent != null )
				{
					spriteComponent.size = component.CustomSize.Size;
					spriteComponent.transform.localPosition = component.CustomSize.Center;
					component.UpdateHotspot();
				}
				EditorUtility.SetDirty(target);
			}
		}
				
		// Alignment buttons
		LayoutAlignment("Align Image");

		string anim = component.Anim;
		DrawDefaultInspector();		
		

		if ( anim != component.Anim )
		{
			// update default sprite
			UpdateSprite(component, component.Anim, spriteComponent);
		}
		
		GUILayout.Label("Utils",EditorStyles.boldLabel);		
		if ( GUILayout.Button("Rename") )
		{			
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Gui, component.ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}
	

	public override void OnSceneGUI()
	{		
		// Call up to parent for baseline
		base.OnSceneGUI();

		
		// Custom size control (if image is split)
		TextField component = (TextField)target;
		SpriteRenderer spriteComponent = component.GetComponentInChildren<SpriteRenderer>();	
		OnSceneDrawPivot(component.transform);
		if ( /*(m_stretchComponent == null || m_stretchComponent.enabled==false) &&*/ spriteComponent != null && spriteComponent.drawMode != SpriteDrawMode.Simple )
		{
			RectCentered oldBounds = RectCentered.zero;
			oldBounds.Size = spriteComponent.size;
			oldBounds.Center = spriteComponent.transform.localPosition;
			RectCentered bounds = OnSceneGuiRectCenter( oldBounds,true, component.transform );
			if ( bounds != oldBounds )
			{
				spriteComponent.size = bounds.Size;
				spriteComponent.transform.localPosition = bounds.Center;
				component.UpdateHotspot();
				EditorUtility.SetDirty(target);
			}

			// update bounds displayed in inspector
			if ( bounds != component.CustomSize )
				component.CustomSize = bounds;
		}
		else if ( spriteComponent != null )
		{	
			OnSceneGuiRectCenter( GuiUtils.CalculateGuiRectFromSprite(component.transform,false,spriteComponent), false, component.transform);
		}
	}
}
}
