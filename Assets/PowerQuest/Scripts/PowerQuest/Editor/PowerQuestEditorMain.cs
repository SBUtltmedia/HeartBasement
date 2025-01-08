using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PowerTools.Quest;
using PowerTools;
using UnityEngine.Assertions;

namespace PowerTools.Quest
{

public partial class PowerQuestEditor
{

	#region Variables: Static definitions
	
	enum eMainTab { All, Rooms, Chars, Items, Dialogs, Guis, Count }

	string m_searchString = string.Empty;

	#endregion
	#region Variables: Serialized

	// Reference to the prefab list, or a filtered list of the prefabs, used by Reorderable lists
	readonly List<RoomComponent> m_listRoomPrefabs = new List<RoomComponent>(); // NB: removed out the 'serializefield' bit to see if that's what was causing invisible fields
	readonly GroupedPrefabContext m_listRoomGroups = new GroupedPrefabContext();
	readonly List<CharacterComponent> m_listCharacterPrefabs = new List<CharacterComponent>();
	readonly GroupedPrefabContext m_listCharacterGroups = new GroupedPrefabContext();
	readonly List<InventoryComponent> m_listInventoryPrefabs = new List<InventoryComponent>();
	readonly GroupedPrefabContext m_listInventoryGroups = new GroupedPrefabContext();
	readonly List<DialogTreeComponent> m_listDialogTreePrefabs = new List<DialogTreeComponent>();	
	readonly GroupedPrefabContext m_listDialogTreeGroups = new GroupedPrefabContext();
	readonly List<GuiComponent> m_listGuiPrefabs = new List<GuiComponent>();
	readonly GroupedPrefabContext m_listGuiGroups = new GroupedPrefabContext();

	[SerializeField] List<PrefabGroupListItem<RoomComponent>> m_roomUiState =
		new List<PrefabGroupListItem<RoomComponent>>();
	[SerializeField] List<PrefabGroupListItem<CharacterComponent>> m_characterUiState =
		new List<PrefabGroupListItem<CharacterComponent>>();
	[SerializeField] List<PrefabGroupListItem<InventoryComponent>> m_inventoryUiState =
		new List<PrefabGroupListItem<InventoryComponent>>();
	[SerializeField] List<PrefabGroupListItem<DialogTreeComponent>> m_dialogTreeUiState =
		new List<PrefabGroupListItem<DialogTreeComponent>>();
	[SerializeField] List<PrefabGroupListItem<GuiComponent>> m_guiUiState =
		new List<PrefabGroupListItem<GuiComponent>>();
	
	bool m_focusSearch = false;
	//! [Room search accelerator]
	bool m_canQuickSelectRoom = false;
	bool m_doQuickSelectRoom = false;
	int m_quickSelectRoomIndex = -1;

	#endregion
	#region Variables: Private	

	[SerializeField] eMainTab m_selectedMainTab = eMainTab.All;
	bool m_dirty = false;

	#endregion
	#region Funcs: Init

	public void RefreshMainGuiLists() { CreateMainGuiLists(); }

	void CreateMainGuiLists()
	{
		//
		// Create reorderable lists
		//
			
		Assert.IsTrue(m_gamePath.EndsWith("/"), "GamePath MUST end with '/', all code here expects it");

		m_listRooms = FilterAndCreateReorderable("Rooms",
			m_gamePath + "Rooms",
			m_listRoomGroups,
			ref m_roomUiState,
			m_powerQuest.GetRoomPrefabs(), m_listRoomPrefabs, m_filterRooms,
			LayoutRoomGUI, SelectRoom,
			(path, list) => {
				CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Room,
					"Room", "'Bathroom' or 'CastleGarden'",  CreateRoom,
					path);
			},
			(prefabs, list) => DeleteQuestObject(list.index, "Room", prefabs)
		);

		m_listCharacters = FilterAndCreateReorderable("Characters",
			m_gamePath + "Characters",
			m_listCharacterGroups,
			ref m_characterUiState,
			m_powerQuest.GetCharacterPrefabs(), m_listCharacterPrefabs, m_filterCharacters,
			LayoutCharacterGUI,
			SelectGameObjectFromList,
			(path, list) => {
				CreateCharacterWindow window = CreateInstance<CreateCharacterWindow>();
				window.SetPath(path);
				window.ShowUtility();
			},
			(prefabs, list) => DeleteQuestObject(list.index, PowerQuest.STR_CHARACTER, prefabs)
		);

		m_listInventory = FilterAndCreateReorderable(PowerQuest.STR_INVENTORY,
			m_gamePath + PowerQuest.STR_INVENTORY,
			m_listInventoryGroups,
			ref m_inventoryUiState,
			m_powerQuest.GetInventoryPrefabs(), m_listInventoryPrefabs, m_filterInventory,
			LayoutInventoryGUI,
			SelectGameObjectFromList,
			(path, list) => {
				CreateInstance<CreateQuestObjectWindow>().ShowQuestWindow(
					eQuestObjectType.Inventory, PowerQuest.STR_INVENTORY, "'Crowbar' or 'RubberChicken'", CreateInventory,
					path);
			},
			(prefabs, list) => DeleteQuestObject(list.index, PowerQuest.STR_INVENTORY, prefabs)
		);

		m_listDialogTrees = FilterAndCreateReorderable("Dialog Trees",
			m_gamePath + "DialogTree",
			m_listDialogTreeGroups,
			ref m_dialogTreeUiState,
			m_powerQuest.GetDialogTreePrefabs(), m_listDialogTreePrefabs, m_filterDialogTrees,
			LayoutGuiDialogTree,
			SelectGameObjectFromList,
			(path, list) => {
				CreateInstance<CreateQuestObjectWindow>().ShowQuestWindow(
					eQuestObjectType.Dialog, "DialogTree", "'MeetSarah' or 'Policeman2'", CreateDialogTree,
					path);
			},
			(prefabs, list) => DeleteQuestObject(list.index, "DialogTree", prefabs)
		);

		m_listGuis = FilterAndCreateReorderable("Guis",
			m_gamePath + "Gui",
			m_listGuiGroups,
			ref m_guiUiState,
			m_powerQuest.GetGuiPrefabs(), m_listGuiPrefabs, m_filterGuis,
			LayoutGuiGUI,
			SelectGameObjectFromList,
			(path, list) => {
				CreateInstance<CreateQuestObjectWindow>().ShowQuestWindow(
					eQuestObjectType.Gui, "Gui", "'Toolbar' or 'InventoryBox'", CreateGui,
					path);
			},
			(prefabs, list) => DeleteQuestObject(list.index, "Gui", prefabs)
		);
		
		//! [Room search accelerator]
		// Update whether can select the room - Could just rely on the m_quickSelectRoomIndex, but this is a minor optimisation.
		m_canQuickSelectRoom = m_listRooms.count > 0 && IsString.Valid(m_searchString);
	
	}

	#endregion
	#region Gui Layout: Main


	void OnGuiMain()
	{
		m_selectedMainTab = LayoutMainTabs(m_selectedMainTab);

		LayoutQuickSearch();

		m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

		GUILayout.Space(5);
		GUILayout.BeginHorizontal();

		if ( GUILayout.Button("Global Script", EditorStyles.miniButtonLeft ) )
			QuestScriptEditor.Open(PATH_GLOBAL_SCRIPT, PowerQuest.GLOBAL_SCRIPT_NAME, QuestScriptEditor.eType.Global );
		else if ( GUILayout.Button("...", new GUIStyle(EditorStyles.miniButtonRight){fixedWidth=30} ) )
		{
			GenericMenu menu = new GenericMenu();
			OnGuiGlobalScriptContext(menu);
			menu.ShowAsContext();			
			//Event.current.Use();
		}

		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		// Layout rooms
		LayoutMainObjectList(eMainTab.Rooms, m_filterRooms, m_listRooms, "Rooms");
		
		// Layout characters
		LayoutMainObjectList(eMainTab.Chars, m_filterCharacters, m_listCharacters, "Characters");				

		// Layout Inventory
		LayoutMainObjectList(eMainTab.Items, m_filterInventory, m_listInventory, "Inventory Items");	

		// Layout Dialogs		
		LayoutMainObjectList(eMainTab.Dialogs, m_filterDialogTrees, m_listDialogTrees, "Dialog Trees");

		// Layout Guis		
		LayoutMainObjectList(eMainTab.Guis, m_filterGuis, m_listGuis, "Guis");

		LayoutManual();

		EditorGUILayout.EndScrollView();
	}

	void OnGuiGlobalScriptContext(GenericMenu menu)
	{ 
		menu.AddItem(
			"Header", true,()=> QuestScriptEditor.Open(PATH_GLOBAL_SCRIPT, PowerQuest.GLOBAL_SCRIPT_NAME, QuestScriptEditor.eType.Global ));

		menu.AddSeparator("");

		menu.AddItem(
			"On Game Start  (BG)",true, () => QuestScriptEditor.Open( 
			"OnGameStart","", false) );
			
		menu.AddItem(
			"Post-Restore Game (BG)",true, () => QuestScriptEditor.Open(
			"OnPostRestore", " int version ", false) );

		menu.AddItem(
			"On Enter Room (BG)",true, () => QuestScriptEditor.Open(
			"OnEnterRoom","", false) );

		menu.AddItem(
			"On Enter Room After Fade",true, () => QuestScriptEditor.Open(
			"OnEnterRoomAfterFade") );

		menu.AddItem(
			"On Exit Room",true, () => QuestScriptEditor.Open( 
			"OnExitRoom") );

		menu.AddSeparator("");

		menu.AddItem(
			"Update Blocking",true, () => QuestScriptEditor.Open(
			"UpdateBlocking") );

		menu.AddItem(
			"Update (BG)",true, () => QuestScriptEditor.Open(
			"Update","", false) );
			
		menu.AddItem(
			"UpdateNoPause (BG)",true, () => QuestScriptEditor.Open(
			"UpdateNoPause","", false) );
			
			
		menu.AddSeparator("");
			
		menu.AddItem(
			"On Mouse Click (BG)",true, () => QuestScriptEditor.Open(
			"OnMouseClick", " bool leftClick, bool rightClick ", false) );

		menu.AddItem(
			"Update Input (BG)",true, () => QuestScriptEditor.Open(
			"UpdateInput", "", false) );
				
		menu.AddSeparator("");

		menu.AddItem(
			"On Any Click",true, () => QuestScriptEditor.Open(
			"OnAnyClick") );

		menu.AddItem(
			"On Walk To",true, () => QuestScriptEditor.Open(
			"OnWalkTo") );

			
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Parser) )
		{ 
			menu.AddSeparator("");
			menu.AddItem(
				"On Parser",true, () => QuestScriptEditor.Open(
				"OnParser") );
		}
			
		menu.AddSeparator("");
		
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
			menu.AddItem(
				"Unhandled Interact",true, () => QuestScriptEditor.Open(
				"UnhandledInteract", " IQuestClickable mouseOver ", true) );
				
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
			menu.AddItem(
				"Unhandled Look At",true, () => QuestScriptEditor.Open(
				"UnhandledLookAt", " IQuestClickable mouseOver ", true) );
		
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )		
			menu.AddItem(
				"Unhandled Inventory",true, () => QuestScriptEditor.Open(
				"UnhandledUseInv", " IQuestClickable mouseOver, Inventory item ", true) );

		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
			menu.AddItem(
				"Unhandled Inventory on Inventory",true, () => QuestScriptEditor.Open(
				"UnhandledUseInvInv", " Inventory invA, Inventory invB ", true) );
	}

	RoomComponent GetQuickSelectedRoom()
	{ 
		if ( m_canQuickSelectRoom )
		{ 
			int index = 0;
			for (int i = 0; i < m_listRooms.count; ++i)
			{
				if (m_listRooms.list[i] is PrefabGroupListItem<RoomComponent> group)
				{
					for (int j = 0; j < group.Members.Count; ++j)
					{
						if (index == m_quickSelectRoomIndex)
							return group.Members[j];
						index++;
					}
				}
				else if ( m_listRooms.list[i] is RoomComponent ) 
				{ 
					// ungrouped
					if ( index == m_quickSelectRoomIndex )
						return m_listRooms.list[i] as RoomComponent;
					index++;
				}
			}				
		}

		// if not found, reset room selected to zero
		m_quickSelectRoomIndex = 0;
		return null;
	}

	void LayoutQuickSearch()
	{
		//EditorGUIUtility.LookLikeInspector();
		GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));

		#if UNITY_2022_2_OR_NEWER
		GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField");
		GUIStyle searchCancelStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton");
		#else
		GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSeachTextField");
		GUIStyle searchCancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");
		#endif
		GUI.SetNextControlName(STR_PQ_SEARCH);	
		
		//! [Room search accelerator]
		if (m_canQuickSelectRoom && GUI.GetNameOfFocusedControl() == STR_PQ_SEARCH && 
		    Event.current.type == EventType.KeyDown)
		{
			if ( Event.current.keyCode == KeyCode.Return )
			{ 
				m_doQuickSelectRoom = true;
			}
			else if ( Event.current.keyCode == KeyCode.DownArrow )
			{ 
				m_quickSelectRoomIndex++;
				Event.current.Use();
			}
			else if	( Event.current.keyCode == KeyCode.UpArrow )
			{ 
				m_quickSelectRoomIndex--;
				Event.current.Use();
			}
		}
		if ( m_canQuickSelectRoom == false )
			m_quickSelectRoomIndex = 0;
		//!
		
		string newSearchString = m_searchString;
		string test = GUI.GetNameOfFocusedControl();
		if ( IsString.Empty(m_searchString) && GUI.GetNameOfFocusedControl() != STR_PQ_SEARCH && m_focusSearch == false && Event.current.type == EventType.Repaint)
		{
			GUI.contentColor = Color.grey;
			EditorGUILayout.TextField("Quick Search (Ctrl+Q)", new GUIStyle(searchStyle) { });
			GUI.contentColor = Color.white;
		}
		else
			newSearchString = EditorGUILayout.TextField(m_searchString, searchStyle);

		if ( m_focusSearch )
		{
			m_focusSearch=false;			
			GUI.FocusControl(null);
			EditorGUI.FocusTextInControl(STR_PQ_SEARCH);
		}

		if (GUILayout.Button("", searchCancelStyle))
		{
			// Remove focus if cleared
			newSearchString = "";			
			EditorGUI.FocusTextInControl(null);
			GUI.FocusControl(null);			
		}

		Event ev = Event.current;
		if (  ev.keyCode == KeyCode.Escape && IsString.Valid(m_searchString) )
		{
			// Remove focus on esc too
			newSearchString = "";
			
			if ( GUI.GetNameOfFocusedControl() == STR_PQ_SEARCH )
				EditorGUI.FocusTextInControl(null);
			GUI.FocusControl(null);
			Repaint();
			//ev.Use(); // kinda hacky- we're checking for 'escape' pressed during the 'layout' event so can't use the event like we really should
		} 
		GUILayout.EndHorizontal();
		if ( newSearchString != m_searchString || m_dirty)
		{
			m_searchString = newSearchString;
			m_dirty = false;
			CreateMainGuiLists();
		}
	}

	void LayoutMainObjectList( eMainTab tab, FilterContext filter, ReorderableList list, string name )
	{		
		if ( m_selectedMainTab == eMainTab.All || m_selectedMainTab == tab || string.IsNullOrEmpty(m_searchString) == false )
		{
			bool show = filter.Show || m_selectedMainTab == tab || IsString.Valid(m_searchString);

			if ( show && list != null )
				list.DoLayoutList();
			else 
				filter.Show = EditorGUILayout.Foldout(filter.Show, name, true);
			GUILayout.Space(5);
		}
		
		//! [Room search accelerator]
		if ( Event.current.keyCode != KeyCode.Return )
			m_doQuickSelectRoom = false;
	}
	
	//
	// Creates tab style layout
	//
	eMainTab LayoutMainTabs(eMainTab selected)
	{
		const float DarkGray = 0.6f;
		const float LightGray = 0.9f;
		//const float StartSpace = 5;
	 
		//GUILayout.Space(StartSpace);
		Color storeColor = GUI.backgroundColor;
		Color highlightCol = new Color(LightGray, LightGray, LightGray);
		Color bgCol = new Color(DarkGray, DarkGray, DarkGray);
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.padding.bottom = 8;
		buttonStyle.margin.left = 0;
		buttonStyle.margin.right = 0;
	 
		GUILayout.BeginHorizontal();
		{   //Create a row of buttons
			for (eMainTab i = 0; i < eMainTab.Count; ++i)
			{
				GUI.backgroundColor = i == selected ? highlightCol : bgCol;
				if (GUILayout.Button(((eMainTab)i).ToString(), buttonStyle))
				{
					selected = i; //Tab click
				}
			}
		} GUILayout.EndHorizontal();
		//Restore color
		GUI.backgroundColor = storeColor;	 
		return selected;
	}

	void LayoutManual()
	{
		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Open Editor Manual") )
			Application.OpenURL("https://powerquest.powerhoof.com/manual_projectsetup.html"); //Path.GetFullPath("Assets/PowerQuest/PowerQuest-Manual.pdf"));
		if ( GUILayout.Button("Open Scripting API") )
			Application.OpenURL("https://powerquest.powerhoof.com/apipage.html");
		GUILayout.EndHorizontal();
		GUILayout.Space(5);	

		LayoutVersion();

	}

	void LayoutVersion()
	{
		GUILayout.Space(15);	
		if ( m_powerQuest != null )
		{
			//
			// Update
			// 
			System.DateTime nextCheck = System.DateTime.FromFileTimeUtc( m_newVersionCheckTime );
			nextCheck = nextCheck.AddDays(1);
			//nextCheck = nextCheck.AddSeconds(20);
			if ( System.DateTime.Compare(nextCheck,System.DateTime.UtcNow) < 0 )
			{
				// Time for another check
				//Debug.Log("Checking for Powerquest Update");
				m_newVersionCheckTime = System.DateTime.UtcNow.ToFileTimeUtc();

				UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get("http://powerquest.powerhoof.com/version.txt");
				request.SendWebRequest().completed += ((item)=>
				{ 
					#if UNITY_2020_1_OR_NEWER
					if ( request.isDone && request.result != UnityEngine.Networking.UnityWebRequest.Result.ProtocolError )
					#else
					if ( request.isDone && request.isHttpError == false )
					#endif
					{
						string text = request.downloadHandler.text;
						int newVersion = Version(text);
						if ( newVersion > 0 && newVersion != m_powerQuest.EditorNewVersion )
						{
							m_powerQuest.EditorNewVersion = newVersion;
							EditorUtility.SetDirty(m_powerQuest);	
							if ( newVersion > m_powerQuest.EditorGetVersion() )
							{
								EditorUtility.DisplayDialog("PowerQuest Update Available!",
										"A new version of PowerQuest is available. (v"+Version(m_powerQuest.EditorNewVersion)+")\n\nTo update, click the Open Scripting API button, or go to http://powerquest.powerhoof.com",
										"Ok");				
							}
						}
					} 
				});

			}

			if ( m_powerQuest.EditorNewVersion > m_powerQuest.EditorGetVersion() )
			{	
				Rect rect = EditorGUILayout.GetControlRect(false, 20 );
				EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );			
				string text = "PowerQuest v"+Version( m_powerQuest.EditorGetVersion() ) + " <color=#4444c1>(v"+Version( m_powerQuest.EditorNewVersion )+" available)</color>";
				if (  GUI.Button(rect,text, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { richText = true, hover={ textColor=Color.white} } ) )
					Application.OpenURL("http://powerquest.powerhoof.com/version_history.html");					

			}
			else 
			{
				GUILayout.Label("PowerQuest v"+Version( m_powerQuest.EditorGetVersion() ), EditorStyles.centeredGreyMiniLabel );	
			}


		}
	}

	#endregion
	#region Gui Layout: Rooms

	void LayoutRoomGUI(List<RoomComponent> rooms, Rect rect, int index, bool isActive, bool isFocused)
	{
		RoomComponent doLoadScene = null;
		if ( m_powerQuest != null && rooms.IsIndexValid(index))
		{
			RoomComponent itemComponent = rooms[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				Rect contextRect = rect; // Cache rect for right click context menu
				float totalFixedWidth = 60+50+22;
				rect.width -= totalFixedWidth;
				
				bool isQuickSelectedRoom = GetQuickSelectedRoom() == itemComponent;
				
				EditorGUI.LabelField(rect, itemComponent.GetData().ScriptName, (((m_filterRooms.State == FilterState.All && IsHighlighted(itemComponent)) || isQuickSelectedRoom)?EditorStyles.whiteLabel:EditorStyles.label) );
				rect.y += 2;
				rect = rect.SetNextWidth(60);	
				
				//! [Room search accelerator]
				if ( GUI.Button(rect, Application.isPlaying ? "Teleport"  : "Scene", EditorStyles.miniButtonLeft ) 
				     || (m_doQuickSelectRoom && isQuickSelectedRoom) )
				{					
					if ( IsString.Valid(m_searchString) )
					{
						// if quicksorting, unselect quicksort and auto-change tabs						
						EditorGUI.FocusTextInControl(null);
						GUI.FocusControl(null);
						m_searchString = string.Empty;
						m_dirty=true;
						m_selectedTab = eTab.Room;
						m_doQuickSelectRoom=false;
						m_quickSelectRoomIndex = 0;
					}
					// Load the scene					
					doLoadScene = itemComponent; // setting this to do after layout to avoid error					
				}
				rect = rect.SetNextWidth(50);		
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonMid ) )
				{
					// Open the script
					QuestScriptEditor.Open(itemComponent);
				}
				
				rect = rect.SetNextWidth(22);		
				bool contextButtonPressed = GUI.Button(rect, "...", EditorStyles.miniButtonRight );
				
				// Passing in context menu. NB: there's error here because m_listRooms might not be the actual reorderable list we're in (because of all the folders stuff). and the reorderable list might not have the callback functions for add/delete...
				ReorderableList reordList = FindReorderableList(itemComponent); // maybe need this? // m_listRooms
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Room, reordList, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, contextRect, index, contextButtonPressed==false, (menu,prefab) =>
				{
					// If this is useful, add context menus for other scripts too
					menu.AddSeparator(string.Empty);										
					LayoutRoomScriptsContextMenu(menu, prefab.GetComponent<RoomComponent>(), "Scripts/");
				});
			}
		}
		
		if ( doLoadScene != null )
			LoadRoomScene(doLoadScene);
	}




	#endregion
	#region Gui Layout: Inventory


	void LayoutInventoryGUI(List<InventoryComponent> inventory, Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && inventory.IsIndexValid(index))
		{
			InventoryComponent itemComponent = inventory[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{
			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Inventory, m_listInventory, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0)
					+ (Application.isPlaying ? 1 : 0);
				float fixedWidth = 36;
				float totalFixedWidth = 50+(fixedWidth*actionCount)+22;
				actionCount += 2;

				rect.width -= totalFixedWidth;
				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName(), ((m_filterInventory.State == FilterState.All && IsHighlighted(itemComponent))?EditorStyles.whiteLabel:EditorStyles.label) );

				rect.y += 2;
				rect = rect.SetNextWidth(50);
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		
				}

				//!
				BeginHighlightingMethodButtons(itemComponent.GetData());
				
				int actionNum = 1; // Start at 1 since there's already a left item
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_LOOKAT_INVENTORY);
					rect = rect.SetNextWidth(fixedWidth);
					if (  GUI.Button(rect, "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// Lookat
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_LOOKAT_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_INVENTORY);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_INTERACT_INVENTORY);
					rect = rect.SetNextWidth(fixedWidth);
					if ( GUI.Button(rect, "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// Interact
						QuestScriptEditor.Open(itemComponent, PowerQuest.SCRIPT_FUNCTION_INTERACT_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_INVENTORY);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY);
					rect = rect.SetNextWidth(fixedWidth);
					if ( GUI.Button(rect, "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// UseItem
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_USEINV_INVENTORY);
					}
				}

				//!
				EndHighlightingMethodButtons();

				if ( Application.isPlaying )
				{
					rect = rect.SetNextWidth(37);
					if ( GUI.Button(rect, "Give", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// Debug give item to player
						itemComponent.GetData().Add();
					}
				}
				
				rect = rect.SetNextWidth(22);
				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Inventory, m_listInventory, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}

	#endregion
	#region Gui Layout: DialogTree

	void LayoutGuiDialogTree(List<DialogTreeComponent> trees, Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && trees.IsIndexValid(index))
		{
			DialogTreeComponent itemComponent = trees[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Dialog, m_listDialogTrees, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );
								
				int actionCount = (Application.isPlaying ? 1 : 0);
				float totalFixedWidth = 50+(34*actionCount)+22;
				actionCount+=1;

				rect.width -= totalFixedWidth;

				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName(), ((m_filterDialogTrees.State == FilterState.All && IsHighlighted(itemComponent))?EditorStyles.whiteLabel:EditorStyles.label) );
				
				rect.y += 2;
				rect = rect.SetNextWidth(50);
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		

				}

				if ( Application.isPlaying )
				{
					rect = rect.SetNextWidth(37);
					if ( GUI.Button(rect, "Test", EditorStyles.miniButtonMid ) )
					{
						// Debug give item to player
						PowerQuest.Get.StartDialog(itemComponent.GetData().GetScriptName());
					}
				}
				
				rect = rect.SetNextWidth(22);
				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Dialog, m_listDialogTrees, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}

	#endregion
	#region Gui Layout: Gui

	void LayoutGuiGUI(List<GuiComponent> guiComponents, Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && guiComponents.IsIndexValid(index))
		{
			GuiComponent itemComponent = guiComponents[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Gui, m_listGuis, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				float totalFixedWidth = 50+50+22;//+30;
				
				rect.width -= totalFixedWidth;
				rect.height = EditorGUIUtility.singleLineHeight;
				GUIStyle labelStyle = (m_filterGuis.State == FilterState.All && IsHighlighted(itemComponent)) ? EditorStyles.whiteLabel : EditorStyles.label;
				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName(), labelStyle);

				rect.y = rect.y+2;

				rect = rect.SetNextWidth(50);
				
				if ( GUI.Button(rect, "Edit", EditorStyles.miniButtonLeft ) )
				{
					// Stage the prefab, and switch to Gui tab	
					Selection.activeObject = itemComponent.gameObject;
					AssetDatabase.OpenAsset(itemComponent.gameObject);									
					m_selectedTab = eTab.Gui;
					GUIUtility.ExitGUI();

				}
				
				rect = rect.SetNextWidth(50);
				
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonMid) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		

				}
				
				rect = rect.SetNextWidth(22);

				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Gui, m_listGuis, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}

	#endregion
	#region Gui Layout: Character

	void LayoutCharacterGUI(List<CharacterComponent> characters, Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && characters.IsIndexValid(index))
		{
			CharacterComponent itemComponent = characters[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Character, m_listCharacters, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0);
				float totalFixedWidth = 50 + (34 *actionCount)+22;
				actionCount+=2;
				
				rect.width -= totalFixedWidth;
				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName(), ((m_filterCharacters.State == FilterState.All && IsHighlighted(itemComponent))?EditorStyles.whiteLabel:EditorStyles.label) );
				
				rect.y = rect.y+2;
				rect = rect.SetNextWidth(50);
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		
				}
				
				//! Highlight for existing functions
				BeginHighlightingMethodButtons(itemComponent.GetData());
				
				//GUIStyle nextStyle = EditorStyles.miniButtonLeft;
				int actionNum = 1; // start at one since there's already a left item
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_LOOKAT);
					rect = rect.SetNextWidth(34);
					if ( GUI.Button(rect, "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// Lookat
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_LOOKAT, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_CHARACTER);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_INTERACT);
					rect = rect.SetNextWidth(34);
					if ( GUI.Button(rect, "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// Interact
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_INTERACT, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_CHARACTER);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
				{			
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_USEINV);
					rect = rect.SetNextWidth(34);	
					if ( GUI.Button(rect, "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// UseItem
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_USEINV, PowerQuestEditor.SCRIPT_PARAMS_USEINV_CHARACTER);
					}
				}
				
				//!
				EndHighlightingMethodButtons();
				
				rect = rect.SetNextWidth(22);
				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Character, m_listCharacters, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}


	#endregion
	#region Functions: Private
	
	// Selects the game object in the project view from the passed in list of prefabs
	void SelectGameObjectFromList(ReorderableList list)
	{
		if ( list.index >= 0 && list.index < list.list.Count )
		{
			MonoBehaviour component = list.list[list.index] as MonoBehaviour;
			if ( component != null )
			{
				// Was trying 'auto' focuseing project window so you didn't need it open always... it's kinda annoying though
				//if ( PrefabUtility.GetPrefabInstanceStatus(component) == PrefabInstanceStatus.NotAPrefab )  // This confusing statement checks that the it's not an instance of a prefab (therefore is found in the project)
				//	EditorUtility.FocusProjectWindow();

				Selection.activeObject = component.gameObject;
				GUIUtility.ExitGUI();
			}
		}
	}

	#endregion
	
}

}
