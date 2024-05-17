using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.QuestGui
{

[CanEditMultipleObjects]
[CustomEditor(typeof(Image), true)]
public class ImageEditor : ControlEditorBase 
{	
	SpriteRenderer m_spriteComponent = null;
	FitToObject m_stretchComponent = null;

	public override void OnInspectorGUI()
	{
		Image component = (Image)target;			
		if ( component == null ) return;			

		if ( m_spriteComponent == null )
			m_spriteComponent = component.GetComponentInChildren<SpriteRenderer>();		
		QuestText textComponent = component.GetComponentInChildren<QuestText>();
		m_stretchComponent = component.GetComponentInChildren<FitToObject>(true);
		GuiComponent guiComponent = component.GetComponentInParent<GuiComponent>();
				
		//
		// Manual size
		//		
		if ( (m_stretchComponent == null || m_stretchComponent.enabled==false) && m_spriteComponent != null && m_spriteComponent.drawMode != SpriteDrawMode.Simple )
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

				if ( m_spriteComponent != null )
				{
					m_spriteComponent.size = component.CustomSize.Size;
					m_spriteComponent.transform.localPosition = component.CustomSize.Center;
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
			UpdateSprite(component, component.Anim, m_spriteComponent);
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

		// temp test
		//return;

		// Custom size control (if image is split)
		Image component = (Image)target;
		OnSceneDrawPivot(component.transform);
		if ( (m_stretchComponent == null || m_stretchComponent.enabled==false) && m_spriteComponent != null && m_spriteComponent.drawMode != SpriteDrawMode.Simple )
		{
			RectCentered oldBounds = RectCentered.zero;
			oldBounds.Size = m_spriteComponent.size;
			oldBounds.Center = m_spriteComponent.transform.localPosition;
			RectCentered bounds = OnSceneGuiRectCenter( oldBounds,true, component.transform );
			if ( bounds != oldBounds )
			{
				m_spriteComponent.size = bounds.Size;
				m_spriteComponent.transform.localPosition = bounds.Center;
				EditorUtility.SetDirty(target);
			}

			// update bounds displayed in inspector
			if ( bounds != component.CustomSize )
				component.CustomSize = bounds;
		}
		else if ( m_spriteComponent != null )
		{	
			OnSceneGuiRectCenter( GuiUtils.CalculateGuiRectFromSprite(component.transform,false,m_spriteComponent), false, component.transform);
		}
	}
}
}
