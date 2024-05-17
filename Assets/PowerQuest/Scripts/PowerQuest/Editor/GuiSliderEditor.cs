using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.QuestGui
{

[CanEditMultipleObjects]
[CustomEditor(typeof(Slider), true)]
public class SliderEditor : ControlEditorBase 
{	
	SpriteRenderer m_spriteComponentBar = null;
	//SpriteRenderer m_spriteComponentHandle = null;
	FitToObject m_stretchComponent = null;	

	public override void OnInspectorGUI()
	{
		Slider component = (Slider)target;			
		if ( component == null ) return;			
		
		if ( m_spriteComponentBar == null )
			m_spriteComponentBar = component.GetSprite();
		QuestText textComponent = component.GetQuestText();
		m_stretchComponent = m_spriteComponentBar.GetComponentInChildren<FitToObject>(true);
		GuiComponent guiComponent = component.GetComponentInParent<GuiComponent>();
			
		
		/////////////////////////////////////////////////////////////////////////////////////
		// Script functions

		//GUILayout.Space(5);
		GUILayout.Label("Script Functions",EditorStyles.boldLabel);
		if ( GUILayout.Button("On Drag") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_DRAGGUI+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONDRAG_GUI ); 
		}
		if ( GUILayout.Button("On Click") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_CLICKGUI+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI ); 
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
		// Manual size
		//		
		if ( (m_stretchComponent == null || m_stretchComponent.enabled==false) && m_spriteComponentBar != null && m_spriteComponentBar.drawMode != SpriteDrawMode.Simple )
		{			
			// show manual image sizer
			SerializedObject serializedObj = new SerializedObject(component);
			SerializedProperty prop = serializedObj.FindProperty("m_customSize");
			if ( prop == null )
				Debug.LogError("Didn't find property");
			EditorGUILayout.PropertyField(prop,new GUIContent("Size"),true);
			if ( serializedObj.ApplyModifiedProperties() )
			{
				// On change, update collider
				RectCentered customSizeRect = component.CustomSize;

				if ( m_spriteComponentBar != null )
				{
					m_spriteComponentBar.size = component.CustomSize.Size;
					m_spriteComponentBar.transform.localPosition = component.CustomSize.Center;
					component.UpdateHotspot();
				}
				EditorUtility.SetDirty(target);
			}
		}
				
		// Alignment buttons
		LayoutAlignment("Align Image");

		string anim = component.AnimBar;
		DrawDefaultInspector();		
		

		if ( anim != component.AnimBar )
		{
			// update default sprite
			UpdateSprite(component, component.AnimBar, m_spriteComponentBar);
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
		Slider component = (Slider)target;
		OnSceneDrawPivot(component.transform);
		if ( (m_stretchComponent == null || m_stretchComponent.enabled==false) && m_spriteComponentBar != null && m_spriteComponentBar.drawMode != SpriteDrawMode.Simple )
		{
			RectCentered oldBounds = RectCentered.zero;
			oldBounds.Size = m_spriteComponentBar.size;
			oldBounds.Center = m_spriteComponentBar.transform.localPosition;
			RectCentered bounds = OnSceneGuiRectCenter( oldBounds,true, component.transform );
			if ( bounds != oldBounds )
			{
				m_spriteComponentBar.size = bounds.Size;
				m_spriteComponentBar.transform.localPosition = bounds.Center;
				component.UpdateHotspot();
				EditorUtility.SetDirty(target);
			}

			// update bounds displayed in inspector
			if ( bounds != component.CustomSize )
				component.CustomSize = bounds;
		}
		else if ( m_spriteComponentBar != null )
		{	
			OnSceneGuiRectCenter( GuiUtils.CalculateGuiRectFromSprite(component.transform,false,m_spriteComponentBar), false, component.transform);
		}
	}
}
}
