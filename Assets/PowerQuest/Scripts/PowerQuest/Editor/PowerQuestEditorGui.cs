using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

using System.Reflection;
using PowerTools;
using PowerTools.Quest;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{

public partial class PowerQuestEditor
{

	#region Variables: Static definitions

	#endregion
	#region Variables: Serialized
	
	[SerializeField] Vector2 m_scrollPositionGui = Vector2.zero;

	#endregion
	#region Variables: Private

	GuiComponent m_selectedGui = null;

	//ReorderableList m_listButtons = null;
	//ReorderableList m_listImages = null;
	//ReorderableList m_listLabels = null;
	//ReorderableList m_listOther = null;
	ReorderableList m_listControls = null;

	Camera m_guiCamera = null;

	#endregion
	#region Functions: Create GUI Lists

	void UpdateGuiSelectionFromStage()
	{	
		GuiComponent gui = null;		
		UnityEditor.SceneManagement.PrefabStage stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
		if (stage != null) 
		{
			if ( stage.prefabContentsRoot != null )				
				gui = stage.prefabContentsRoot.GetComponent<GuiComponent>();
		}
		UpdateGuiSelection(gui, true); 
	}
	void UpdateGuiSelection( GuiComponent newGui, bool repaint = false )
	{
		
		if ( m_selectedGui == null )
			m_selectedGui = null; // Fix for wierd bug: In case of missing reference, clear the room reference so that it will never match the new room. I didn't know that could happen!

		GuiComponent oldRoom = m_selectedGui;
		if ( newGui == null )
			m_selectedGui = null;

		if ( (newGui != null && oldRoom != newGui)
			|| (newGui == null) != (m_listControls == null /*&& m_listHotspots == null && m_listRegions == null*/) ) // if changed, or lists are obviously out of date
		{
			m_selectedGui = newGui;
			m_listControls = null;

			if ( m_selectedGui != null )
			{
				m_selectedGui.EditorUpdateChildComponents();

				// The selected room will be an instance unless the game is running				
				//bool isInstance = PrefabUtility.GetPrefabInstanceStatus(m_selectedGui.gameObject) == PrefabInstanceStatus.Connected;
				
				m_listControls = new ReorderableList(m_selectedGui.GetControlComponents(),typeof(GuiControl), true, true, true, true) 
				{ 
					drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Controls"); },
					drawElementCallback = 	LayoutControlGUI,
					onSelectCallback = 		SelectGameObjectFromList,
					onAddCallback = 		(ReorderableList list) => ShowAddControlMenu(),
					onRemoveCallback = 		(ReorderableList list) => { DeleteControl(list.index); },
					onReorderCallback =     UpdateControlOrder
				};
			}

			if ( repaint ) 
				Repaint();
		}
	}

	#endregion
	#region Gui Layout

	void OnGuiGui( bool tabChanged )
	{	    
		//
		// Layout room contents
		//

		if ( m_selectedGui == null )
		{
			GUILayout.Label("Select a Gui to edit.", EditorStyles.centeredGreyMiniLabel);			
			return;		 
		}
									
		bool isPrefab = PrefabUtility.GetPrefabInstanceStatus(m_selectedGui.gameObject) != PrefabInstanceStatus.Connected;
		GUILayout.Label( m_selectedGui.GetData().ScriptName + ( isPrefab ? " (Prefab)" : "" ), new GUIStyle(EditorStyles.largeLabel){alignment=TextAnchor.MiddleCenter});
						
		GUILayout.BeginHorizontal();

		if ( GUILayout.Button( "Select", EditorStyles.miniButtonLeft ) ) 
		{ 
			Selection.activeObject = m_selectedGui.gameObject; 
			GameObject room = QuestEditorUtils.GetPrefabParent(m_selectedGui.gameObject, true);
			if ( room == null && Application.isPlaying ) // in play mode, GetPrefabParent doesn't work :'(
			{
				GuiComponent guiC = GetPowerQuestEditor().GetGui(m_selectedGui.GetData().ScriptName);
				room = guiC != null ? guiC.gameObject : null;				
			}
			EditorGUIUtility.PingObject( room );
		}

		// NB: Only show tab when editing 
		if ( GUILayout.Button("Edit", EditorStyles.miniButtonMid ) )
		{
			AssetDatabase.OpenAsset(m_selectedGui);
		}

		if ( GUILayout.Button( "Script", EditorStyles.miniButtonRight ) )
		{ 
			// Open the script
			QuestScriptEditor.Open( m_selectedGui );	
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(8);
			
		m_scrollPositionGui = EditorGUILayout.BeginScrollView(m_scrollPositionGui);

		//
		// Add gui Control button
		//
		if ( GUILayout.Button("Add Gui Control") )
		{
			ShowAddControlMenu();
		}

		//
		// List of controls
		//
		
		// Update list controls first... kinda dumb to do every frame, but whatever.		
		m_selectedGui.EditorUpdateChildComponents();
		if ( m_listControls != null ) m_listControls.DoLayoutList();


		/*
		GUILayout.Space(5);
		
		if ( m_listButtons != null ) m_listButtons.DoLayoutList();

		GUILayout.Space(5);

		if ( m_listImages != null ) m_listImages.DoLayoutList();

		GUILayout.Space(5);

		if ( m_listLabels != null ) m_listLabels.DoLayoutList();

		GUILayout.Space(5);

		if ( m_listOther != null ) m_listOther.DoLayoutList();
		*/
				
		//GUILayout.Label("Add Controls");

		// Find controls in the Assets/Gui directory
		//string[] assets = AssetDatabase.FindAssets("t:prefab",new string[]{"Assets/Gui"});
					
		/*
		if ( GUILayout.Button("Add Image") )
		{
			GameObject go = new GameObject("ImgNew");
			QuiControl ctrl = go.AddComponent<Image>() as QuiControl;
			go.transform.SetParent(m_selectedGui.transform);
			// add image too			
			GameObject img = new GameObject("Sprite",typeof(PowerSprite));
			img.transform.SetParent(go.transform);
			img.GetComponent<Renderer>().sortingLayerName="Gui";
			img.layer = LayerMask.NameToLayer("UI");
			OnNewControl(go, ctrl);
		}	
		*/
		EditorGUILayout.EndScrollView();	

	}

	void ShowAddControlMenu()
	{	
		try
		{
			
			GenericMenu menu = new GenericMenu();
			string[] files = Directory.GetFiles("Assets/Game/Gui/GuiControls","*.prefab",SearchOption.TopDirectoryOnly);
			foreach ( string file in files )
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);					
				if ( prefab != null && prefab.GetComponent<GuiControl>() != null )
				{					
					//GUILayout.Button($"Add new {prefab.name}");
					menu.AddItem(prefab.name,true, ()=>AddControl(prefab) );
				}
			}
			menu.ShowAsContext();
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}	
	}

	void AddControl(GameObject control)
	{		
		GameObject newGo = PrefabUtility.InstantiatePrefab(control, m_selectedGui.transform) as GameObject;
		OnNewControl(newGo, newGo.GetComponent<GuiControl>());
		newGo.name = "New"+newGo.name;
	}
	void OnNewControl(GameObject newObj, GuiControl ctrl)
	{
		Selection.activeObject = newObj;
		m_selectedGui.EditorUpdateChildComponents();
		newObj.layer = LayerMask.NameToLayer("UI");

		// Set the name to the "sanitized" script name (So they start out being the same)
		newObj.name = ctrl.ScriptName;

		// Find baseline in front of others
		float maxBaseline = 1000;
		foreach ( GuiControl item in m_selectedGui.GetControlComponents() )
		{
			maxBaseline = Mathf.Min(maxBaseline,item.Baseline);				
		}
		ctrl.Baseline=maxBaseline-1;	
		ctrl.UpdateBaseline();

		// Find all child sprites and update their renderer
		PowerSprite[] sprites = newObj.GetComponentsInChildren<PowerSprite>(false);
		foreach( PowerSprite spr in sprites )
		{
			spr.OnValidate();
		}

		// Select in list
		m_listControls.index = m_selectedGui.GetControlComponents().FindIndex(item=>item==ctrl);
		
		EditorUtility.SetDirty(m_selectedGui);
		Repaint();

	}
	
	void DeleteControl(int index = -1) 
	{
		// if index is -1, deletes the end
		Undo.RecordObject(m_selectedGui, "Delete control");

		List<GuiControl> components = m_selectedGui.GetControlComponents();
		GuiControl component = null;
		if (components.Count <= 0)
			return;

		if ( index == -1)
			index = components.Count - 1;
		if ( components.IsIndexValid(index) )
			component = components[index];
		
		if ( component != null && EditorUtility.DisplayDialog("Really Delete?", "Dude, Sure you wanna delete "+component.ScriptName+"?", "Yeah Man", "Hmm, Nah") )
		{
			// Since we're editing the staged prefab, we can just delete immediately. If want to delete from instance, have to do it the harder way (see hotspots)
			GameObject.DestroyImmediate(component.gameObject); 
			
			EditorUtility.SetDirty(m_selectedGui);
			Repaint();
		}

	}
	
	void LayoutControlGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_selectedGui == null || m_selectedGui.GetControlComponents().IsIndexValid(index) == false )
			return;			

		GuiControl itemComponent = m_selectedGui.GetControlComponents()[index];
		if ( itemComponent == null )
			return;
		
		//QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Gui, m_listControls, itemComponent.GetScriptName(), itemComponent.gameObject, rect, index, true );
								
		List<float> widths = new List<float>(); // Element widths, including spaces
		if ( itemComponent is Button ) widths.Add(65); // OnClick
		widths.Add(24); // ...
			
		int widthIndex = -1;
		widths.ForEach( elementWidth=> rect.width -= elementWidth ); // Get width minus right-aligned stuff
		rect.height = EditorGUIUtility.singleLineHeight;
		rect.y += 2;		
		
		// control type
		{
			//rect.xMin += rect.width;/* + widths[++widthIndex]; // no space*/
			//rect.width = widths[++widthIndex];			
			

			// I guess control should have virtual function for this
			string controlTypeName = "";
			if ( itemComponent is IButton )
				controlTypeName = "Btn";
			else if ( itemComponent is ILabel )
				controlTypeName = "Lbl";	
			else if ( itemComponent is IImage )
				controlTypeName = "Img";		
			else if ( itemComponent is IInventoryPanel )
				controlTypeName = "Inv";		
			else if ( itemComponent is ISlider )
				controlTypeName = "Slr";		
			else if ( itemComponent is ITextField )
				controlTypeName = "Txt";		
			EditorGUI.LabelField(rect, controlTypeName, EditorStyles.miniLabel );
			
			rect.x += 25;
			rect.width -= 25;
		}


		// Name		
		EditorGUI.LabelField(rect, itemComponent.ScriptName );
		

		if ( itemComponent is Button )
		{
			rect = rect.SetNextWidth(widths[++widthIndex]);
		
			if ( GUI.Button(rect, "On Click", QuestEditorUtils.GetMiniButtonStyle(widthIndex,widths.Count) ) )
			{
				// Lookat
				QuestScriptEditor.Open( m_selectedGui,
					PowerQuest.SCRIPT_FUNCTION_CLICKGUI+ itemComponent.ScriptName,
					PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI);
			}
		}		
		rect = rect.SetNextWidth(widths[++widthIndex]);
				
		if ( GUI.Button(rect, "...", QuestEditorUtils.GetMiniButtonStyle(widthIndex,widths.Count) ) )
		{
			// Layout ... stuff
			/*
			// Lookat
			QuestScriptEditor.Open( m_selectedRoom, QuestScriptEditor.eType.Hotspot,
				PowerQuest.SCRIPT_FUNCTION_LOOKAT_HOTSPOT+ itemComponent.GetData().ScriptName,
				PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_HOTSPOT);
			*/
		}
				
		/* Not sure if want this for hotspots/props yet
		fixedWidth = 22;
		if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "...", EditorStyles.miniButtonRight ) )
			QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Hotspot, m_listHotspots, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
		offset += fixedWidth;
		*/
	}

	
	
	void UpdateControlOrder(ReorderableList list)
	{
		if ( m_selectedGui == null)
			return;
		int index = 0;
		m_selectedGui.GetControlComponents().ForEach( item=> { if ( item.transform.parent == m_selectedGui.transform ) item.transform.SetSiblingIndex(index++); } );

		EditorUtility.SetDirty(m_selectedGui);
		Repaint();
	}
		
	void OnSceneGui(SceneView sceneView)
	{
		if ( m_selectedGui == null )
			return;
		
		// Get the gui camera
		if (m_guiCamera == null)
			m_guiCamera = GuiUtils.FindGuiCamera();

		if (m_guiCamera != null )
		{
			Rect cameraRect = m_guiCamera.pixelRect;
			RectCentered bounds = new RectCentered( cameraRect );
			bounds.Min = m_guiCamera.ScreenToWorldPoint(bounds.Min);
			bounds.Max = m_guiCamera.ScreenToWorldPoint(bounds.Max);
			
			Handles.DrawSolidRectangleWithOutline(bounds, new Color(0,0,0,0),Color.yellow);
		}

	}


	#endregion
}
}
