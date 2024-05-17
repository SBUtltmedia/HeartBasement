using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.QuestGui
{

[CanEditMultipleObjects]
[CustomEditor(typeof(InventoryPanel), true)]
public class InventoryPanelEditor : ControlEditorBase 
{	
	GridContainer m_gridContainer = null;
	Vector2 m_posCached = Vector2.zero;
	RectCentered m_bounds = RectCentered.zero;

	public override void OnInspectorGUI()
	{
		InventoryPanel component = (InventoryPanel)target;			
		if ( component == null ) return;			

		GuiComponent guiComponent = component.GetComponentInParent<GuiComponent>();
		m_gridContainer = component.GetComponent<GridContainer>();
								
		// Alignment buttons
		//LayoutAlignment("Align Image");
	
		/* Moved into the grid container itself

		//
		// Manual size
		//		
		if ( m_gridContainer != null )
		{			
			// show manual image sizer
			SerializedObject serializedObj = new SerializedObject(component);
			EditorGUI.BeginChangeCheck();
			
			m_bounds.Size = Utils.SnapRound(EditorGUILayout.Vector2Field("Size:", m_bounds.Size));

			if ( EditorGUI.EndChangeCheck() )
			{
				component.CustomSize = m_bounds;
				m_posCached = component.transform.position;
				EditorUtility.SetDirty(target);
			}
		}
		*/
		DrawDefaultInspector();				
		
		GUILayout.Label("Utils",EditorStyles.boldLabel);		
		if ( GUILayout.Button("Rename") )
		{			
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Gui, component.ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}
	}
	
	
		/* Moved to the grid container itself
	public override void OnSceneGUI()
	{		
		// Call up to parent for baseline
		base.OnSceneGUI();
		
		// Custom size control (if image is split)
		InventoryPanel component = (InventoryPanel)target;
		

		OnSceneDrawPivot(component.transform);


		if ( m_gridContainer != null && component.transform.parent != null )
		{
			if ( m_bounds == RectCentered.zero || m_posCached != (Vector2)component.transform.position )
			{
				m_bounds = component.CustomSize;		
				m_posCached = component.transform.position;
			}			

			RectCentered oldBounds = m_bounds;			
			RectCentered bounds = OnSceneGuiRectCenter( oldBounds, true );
			if ( bounds != oldBounds )
			{
				m_bounds = bounds;
				component.CustomSize = bounds;
				m_posCached = component.transform.position;
				EditorUtility.SetDirty(component.gameObject);// set whole thing dirty
			}
		}
	}
		*/
}
}
