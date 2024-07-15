using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.Quest
{
 
public partial class PowerQuestEditor
{

	#region Variables: Static definitions

	#endregion
	#region Variables: Serialized

	[SerializeField] int m_selectedRoomPoint = -1;
	[SerializeField] bool m_showRoomCharacters = false;

	#endregion
	#region Variables: Private
	
	RoomComponent m_selectedRoom = null;

	ReorderableList m_listHotspots = null;
	ReorderableList m_listProps = null;
	ReorderableList m_listRegions = null;
	ReorderableList m_listPoints = null;
	ReorderableList m_listWalkableAreas = null; 
	ReorderableList m_listRoomCharacters = null; 
	ReorderableList m_listRoomCharactersUsed = null; 



	// list of functions you can start he game with
	List<string> m_playFromFuncs = new List<string>();
	List<string> m_playFromFuncsNicified = new List<string>();
	
	bool m_editingPointMouseDown = false;

	#endregion
	#region Functions: Quest Room

	// Called when selecting a room from the main list
	void SelectRoom(ReorderableList list)
	{
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null )
			return;

		if ( list.index >= 0 && list.index < list.list.Count)
		{
			// Find if there's an instance in the scene, if so select that.
			if ( list.list[list.index] is RoomComponent component && component != null )
			{
				// If there's an instance inthe scene, select that, otherwise select the prefab
				GameObject instance = GameObject.Find( "Room"+component.GetData().ScriptName );
				Selection.activeObject = component.gameObject;
				if ( instance != null && instance.GetComponent<RoomComponent>() != null )
				{
					powerQuestEditor.Repaint();
					Selection.activeObject = instance;


					// Also ping the prefab
					EditorGUIUtility.PingObject(component.gameObject);
				}
				
				// Was trying 'auto' focuseing project window so you didn't need it open always... it's kinda annoying though
				//if ( PrefabUtility.GetPrefabInstanceStatus(component) == PrefabInstanceStatus.NotAPrefab )  // This confusing statement checks that the it's not an instance of a prefab (therefore is found in the project)
				//	EditorUtility.FocusProjectWindow();

				//Selection.activeObject = (instance != null && instance.GetComponent<RoomComponent>() != null ) ? instance : component.gameObject;
				
				GUIUtility.ExitGUI();
			}
		}
	}


	public static void CreateRoom( string path, string name )
	{
		// Make sure we can find powerQuest
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null )
			return;

		// Check quest camera + guicamera are set, or if not, try grabbing them from the current scene
		if ( powerQuestEditor.m_questCamera == null )
		{
			QuestCameraComponent instance = GameObject.FindObjectOfType<QuestCameraComponent>();
			if ( instance != null )
				powerQuestEditor.m_questCamera = QuestEditorUtils.GetPrefabParent(instance.gameObject) as GameObject;
		}
		if ( powerQuestEditor.m_questGuiCamera == null )
		{
			#if UNITY_2017_1_OR_NEWER
			Canvas instance = GameObject.FindObjectOfType<Canvas>();
			#else
			GUILayer instance = GameObject.FindObjectOfType<GUILayer>();
			#endif
			if ( instance != null )
				powerQuestEditor.m_questGuiCamera = QuestEditorUtils.GetPrefabParent(instance.gameObject) as GameObject;
		}
		if ( powerQuestEditor.m_questCamera == null || powerQuestEditor.m_questGuiCamera == null )
		{
			Debug.LogError("Add a QuestCamera and QuestGuiCamera to the scene first");
			return;		
		}

		// Give user opportunity to save scene, and cancel if they hit cancel
		if ( EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false )
			return;

		// create directory		
		if ( Directory.Exists($"{path}/{name}") == false )
			AssetDatabase.CreateFolder(path,name);		
		path += "/" + name;

		// Create new scene
		Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

		// Add Camera and SystemMain
		PrefabUtility.InstantiatePrefab(powerQuestEditor.m_powerQuest);
		PrefabUtility.InstantiatePrefab(powerQuestEditor.m_questCamera);
		PrefabUtility.InstantiatePrefab(powerQuestEditor.m_questGuiCamera);

		// Create SpriteCollection
		if ( Directory.Exists(path+"/Sprites") == false )
			AssetDatabase.CreateFolder(path,"Sprites");

		// Create importer
		PowerSpriteImport importer = PowerSpriteImportEditor.CreateImporter(path+"/_Import"+name+".asset");
		importer.m_createSingleSpriteAnims = true; // Rooms can work with single sprite anims

		// Create atlas
		QuestEditorUtils.CreateSpriteAtlas($"{path}/Room{name}Atlas.spriteatlas",$"{path}/Sprites",GetPowerQuest().GetSnapToPixel(),false);

		// Create game object
		GameObject gameObject = new GameObject("Room"+name, typeof(RoomComponent)) as GameObject; 

		RoomComponent room = gameObject.GetComponent<RoomComponent>();
		room.GetData().EditorInitialise(name);

		GameObject walkableAreaObj = new GameObject("WalkableArea",typeof(WalkableComponent)) as GameObject;
		walkableAreaObj.transform.parent = gameObject.transform;
		walkableAreaObj.name = "WalkableArea";
		walkableAreaObj.GetComponent<PolygonCollider2D>().points = DefaultColliderPoints;
		walkableAreaObj.GetComponent<PolygonCollider2D>().isTrigger = true;

		// turn game object into prefab
		Object prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path + "/Room"+name+".prefab", InteractionMode.AutomatedAction);		

		// Select the prefab for editing
		Selection.activeObject = prefab;

		// Add item to list in PowerQuest and repaint the quest editor
		powerQuestEditor.m_powerQuest.GetRoomPrefabs().Add(((GameObject)prefab).GetComponent<RoomComponent>());
		EditorUtility.SetDirty(powerQuestEditor.m_powerQuest);
		powerQuestEditor.Repaint();

		// Add line to GameGlobals.cs for easy scripting
		// public static Room Village { get { return E.GetRoom("RoomVillage"); } }
		QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#R", "\n\t\tpublic static IRoom "+name.PadRight(14)+" { get { return PowerQuest.Get.GetRoom(\""+name+"\"); } }");

		// Save scene
		string scenePath = path+"/SceneRoom"+name+".unity";
		EditorSceneManager.SaveScene(newScene,scenePath);

		// Add scene to editor build settings
		PowerQuestEditor.AddSceneToBuildSettings(scenePath);

		powerQuestEditor.CallbackOnCreateRoom?.Invoke(path, name);
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Room, path, name);
		
		powerQuestEditor.RequestAssetRefresh();
		powerQuestEditor.RefreshMainGuiLists();

	}


	#endregion
	#region Functions: Quest Hotspot


	static public void CreateHotspot(string name = "New")
	{
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null || powerQuestEditor.m_selectedRoom == null)
			return;

		// Create game object
		GameObject gameObject = new GameObject(PowerQuest.STR_HOTSPOT+name, typeof(HotspotComponent), typeof(PolygonCollider2D)) as GameObject; 

		//CharacterComponent character = gameObject.GetComponent<CharacterComponent>();
		//character.GetData().EditorInitialise(name);

		PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();
		collider.isTrigger = true;		 
		collider.points = PowerQuestEditor.DefaultColliderPoints;


		HotspotComponent hotspotComponent = gameObject.GetComponent<HotspotComponent>();
		hotspotComponent.GetData().EditorInitialise(name);

		// Add to the selected room 
		gameObject.transform.parent = powerQuestEditor.m_selectedRoom.transform;

		Selection.activeObject = gameObject;		

		powerQuestEditor.m_selectedRoom.EditorUpdateChildComponents();
		powerQuestEditor.UpdateRoomObjectOrder(false);
		QuestEditorUtils.ReplacePrefab(powerQuestEditor.m_selectedRoom.gameObject);
		
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Hotspot, null, name);

		powerQuestEditor.Repaint();
		
		QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Hotspots);

	}

	void DeleteHotspot(int index = -1) 
	{
		// if index is -1, deletes the end
		List<HotspotComponent> components = m_selectedRoom.GetHotspotComponents();
		HotspotComponent component = null;
		if (components.Count <= 0)
			return;

		if ( index == -1)
			index = components.Count - 1;
		if ( components.IsIndexValid(index) )
			component = components[index];
		
		if ( EditorUtility.DisplayDialog("Really Delete?", "Dude, Sure you wanna delete "+component.GetData().ScriptName+"?", "Yeah Man", "Hmm, Nah") )
		{
			if ( component != null )
			{
				#if UNITY_2018_3_OR_NEWER

					string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_selectedRoom.gameObject);
					GameObject instancedObject = PrefabUtility.LoadPrefabContents(assetPath);
					RoomComponent instancedRoom = instancedObject.GetComponent<RoomComponent>();
					HotspotComponent instancedComponent = instancedRoom.GetHotspotComponents()[index];
					instancedRoom.GetHotspotComponents().Remove(instancedComponent);
					GameObject.DestroyImmediate(instancedComponent.gameObject);
					PrefabUtility.SaveAsPrefabAsset(instancedObject, assetPath);
					PrefabUtility.UnloadPrefabContents(instancedObject);

				#else
					components.Remove(component);
					GameObject.DestroyImmediate(component.gameObject);
				#endif
			}	
		}

		m_selectedRoom.EditorUpdateChildComponents();
		#if !UNITY_2018_3_OR_NEWER
			PrefabUtility.ReplacePrefab( m_selectedRoom.gameObject, PrefabUtility.GetPrefabParent(m_selectedRoom.gameObject), ReplacePrefabOptions.ConnectToPrefab );
		#endif
		PowerQuestEditor powerQuestEditor = EditorWindow.GetWindow<PowerQuestEditor>();
		if ( powerQuestEditor != null ) powerQuestEditor.Repaint();

	}


	#endregion
	#region Functions: Quest Prop

	public static void CreateProp( string name = "New", bool addCollider = true )
	{

		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null || powerQuestEditor.m_selectedRoom == null)
			return;

		// Create game object
		GameObject gameObject = new GameObject(PowerQuest.STR_PROP+name, typeof(PropComponent) ) as GameObject; 


		PropComponent propComponent = gameObject.GetComponent<PropComponent>();
		propComponent.GetData().EditorInitialise(name);

		propComponent.GetData().Clickable = addCollider;
		if ( addCollider )
		{
			PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();
			collider.isTrigger = true;
			collider.points = DefaultColliderPoints;
		}
		gameObject.AddComponent<SpriteAnim>();

		gameObject.GetComponent<SpriteRenderer>().sortingOrder = powerQuestEditor.m_selectedRoom.GetPropComponents().Count;

		// Set sprite if one exists already
		UpdateDefaultSprite( propComponent, propComponent.GetData().Animation, powerQuestEditor.m_selectedRoom.GetAnimations(), powerQuestEditor.m_selectedRoom.GetSprites() );

		Selection.activeObject = gameObject;

		// Add to the selected room
		gameObject.transform.parent = powerQuestEditor.m_selectedRoom.transform;
		powerQuestEditor.m_selectedRoom.EditorUpdateChildComponents();
		powerQuestEditor.UpdateRoomObjectOrder(false);
		QuestEditorUtils.ReplacePrefab(powerQuestEditor.m_selectedRoom.gameObject);
				
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Prop, null, name);

		powerQuestEditor.Repaint(); 
		
		QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Props);

	}

	void DeleteProp(int index = -1) 
	{
		// if index is -1, deletes the end
		List<PropComponent> components = m_selectedRoom.GetPropComponents();
		PropComponent component = null;
		if (components.Count <= 0)
			return;

		if (index == -1)
			index = components.Count - 1;
		if ( components.IsIndexValid(index) )
			component = components[index];
		
		if ( EditorUtility.DisplayDialog("Really Delete?", "Dude, Sure you wanna delete "+component.GetData().ScriptName+"?", "Yeah Man", "Hmm, Nah") )
		{
			if ( component != null )
			{
				#if UNITY_2018_3_OR_NEWER

					string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_selectedRoom.gameObject);
					GameObject instancedObject = PrefabUtility.LoadPrefabContents(assetPath);
					RoomComponent instancedRoom = instancedObject.GetComponent<RoomComponent>();
					PropComponent instancedComponent = instancedRoom.GetPropComponents()[index];
					instancedRoom.GetPropComponents().Remove(instancedComponent);
					GameObject.DestroyImmediate(instancedComponent.gameObject);
					PrefabUtility.SaveAsPrefabAsset(instancedObject, assetPath);
					PrefabUtility.UnloadPrefabContents(instancedObject);

				#else
					components.Remove(component);
					GameObject.DestroyImmediate(component.gameObject);
				#endif
			}	
		}	

		m_selectedRoom.EditorUpdateChildComponents();
		#if !UNITY_2018_3_OR_NEWER
			PrefabUtility.ReplacePrefab( m_selectedRoom.gameObject, PrefabUtility.GetPrefabParent(m_selectedRoom.gameObject), ReplacePrefabOptions.ConnectToPrefab );
		#endif
			PowerQuestEditor powerQuestEditor = EditorWindow.GetWindow<PowerQuestEditor>();
		if ( powerQuestEditor != null ) powerQuestEditor.Repaint();
	}

	#endregion
	#region Functions: Quest Region

	static public void CreateRegion(string name = "New")
	{
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null || powerQuestEditor.m_selectedRoom == null)
			return;

		// Create game object
		GameObject gameObject = new GameObject(PowerQuest.STR_REGION+name, typeof(RegionComponent), typeof(PolygonCollider2D)) as GameObject; 

		//CharacterComponent character = gameObject.GetComponent<CharacterComponent>();
		//character.GetData().EditorInitialise(name);

		PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();
		collider.isTrigger = true;
		collider.points = DefaultColliderPoints;

		RegionComponent regionComponent = gameObject.GetComponent<RegionComponent>();
		regionComponent.GetData().EditorInitialise(name);

		// Add to the selected room 
		gameObject.transform.parent = powerQuestEditor.m_selectedRoom.transform;

		Selection.activeObject = gameObject;

		powerQuestEditor.m_selectedRoom.EditorUpdateChildComponents();
		powerQuestEditor.UpdateRoomObjectOrder(false);
		QuestEditorUtils.ReplacePrefab(powerQuestEditor.m_selectedRoom.gameObject);
		
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Region, null, name);
		powerQuestEditor.Repaint();
		
						
		QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Regions);

	}
	void DeleteRegion(int index = -1) 
	{
		// if index is -1, deletes the end
		List<RegionComponent> components = m_selectedRoom.GetRegionComponents();
		RegionComponent component = null;
		if (components.Count <= 0)
			return;

		if (index == -1)
			index = components.Count - 1;
		if ( components.IsIndexValid(index) )
			component = components[index];
		
		if ( EditorUtility.DisplayDialog("Really Delete?", "Dude, Sure you wanna delete "+component.GetData().ScriptName+"?", "Yeah Man", "Hmm, Nah") )
		{
			if ( component != null )
			{
				#if UNITY_2018_3_OR_NEWER
					string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_selectedRoom.gameObject);
					GameObject instancedObject = PrefabUtility.LoadPrefabContents(assetPath);
					RoomComponent instancedRoom = instancedObject.GetComponent<RoomComponent>();
					RegionComponent instancedComponent = instancedRoom.GetRegionComponents()[index];
					instancedRoom.GetRegionComponents().Remove(instancedComponent);
					GameObject.DestroyImmediate(instancedComponent.gameObject);
					PrefabUtility.SaveAsPrefabAsset(instancedObject, assetPath);
					PrefabUtility.UnloadPrefabContents(instancedObject);

				#else
					components.Remove(component);
					GameObject.DestroyImmediate(component.gameObject);
				#endif
			}
		}

		m_selectedRoom.EditorUpdateChildComponents();
		#if !UNITY_2018_3_OR_NEWER
			PrefabUtility.ReplacePrefab( m_selectedRoom.gameObject, PrefabUtility.GetPrefabParent(m_selectedRoom.gameObject), ReplacePrefabOptions.ConnectToPrefab );
		#endif
		PowerQuestEditor powerQuestEditor = EditorWindow.GetWindow<PowerQuestEditor>();
		if ( powerQuestEditor != null ) powerQuestEditor.Repaint();

	}


	#endregion
	#region Functions: Quest Walkable Area
	
	public static void CreateWalkableArea()
	{

		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null || powerQuestEditor.m_selectedRoom == null)
			return;

		GameObject walkableAreaObj = new GameObject("WalkableArea",typeof(WalkableComponent)) as GameObject;
		walkableAreaObj.transform.parent = powerQuestEditor.m_selectedRoom.transform;
		walkableAreaObj.name = "WalkableArea";
		walkableAreaObj.GetComponent<PolygonCollider2D>().points = DefaultColliderPoints;
		walkableAreaObj.GetComponent<PolygonCollider2D>().isTrigger = true;

		Selection.activeObject = walkableAreaObj;

		// Add to the selected room
		powerQuestEditor.m_selectedRoom.EditorUpdateChildComponents();
		powerQuestEditor.UpdateRoomObjectOrder(false);
		QuestEditorUtils.ReplacePrefab(powerQuestEditor.m_selectedRoom.gameObject);		
		powerQuestEditor.Repaint();

	}

	void DeleteWalkableArea(int index = -1) 
	{
		// if index is -1, deletes the end
		List<WalkableComponent> components = m_selectedRoom.GetWalkableAreas();
		WalkableComponent component = null;
		if ( components.Count <= 0 )
			return;
		
		// Remove gameobject
		if ( index == -1 )
			index = components.Count-1;
		if ( components.IsIndexValid(index) )
			component = components[index];
		
		if ( EditorUtility.DisplayDialog("Really Delete?", "Dude, Sure you wanna delete Walkable Area "+index.ToString()+" ?", "Yeah Man", "Hmm, Nah") )
		{
			if ( component != null )
			{                
				#if UNITY_2018_3_OR_NEWER
					string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_selectedRoom.gameObject);
					GameObject instancedObject = PrefabUtility.LoadPrefabContents(assetPath);
					RoomComponent instancedRoom = instancedObject.GetComponent<RoomComponent>();
					WalkableComponent instancedComponent = instancedRoom.GetWalkableAreas()[index];
					instancedRoom.GetWalkableAreas().Remove(instancedComponent);
					GameObject.DestroyImmediate(instancedComponent.gameObject);
					PrefabUtility.SaveAsPrefabAsset(instancedObject, assetPath);
					PrefabUtility.UnloadPrefabContents(instancedObject);
				#else
					components.Remove(component);
					GameObject.DestroyImmediate(component.gameObject);
				#endif

			}
		}

		m_selectedRoom.EditorUpdateChildComponents();
		#if !UNITY_2018_3_OR_NEWER
			PrefabUtility.ReplacePrefab( m_selectedRoom.gameObject, PrefabUtility.GetPrefabParent(m_selectedRoom.gameObject), ReplacePrefabOptions.ConnectToPrefab );
		#endif
		PowerQuestEditor powerQuestEditor = EditorWindow.GetWindow<PowerQuestEditor>();
		if ( powerQuestEditor != null ) powerQuestEditor.Repaint();

	}

	#endregion
	#region Functions: Create GUI Lists

	void UpdateRoomSelection( RoomComponent newRoom, bool repaint = false )
	{
		
		if ( m_selectedRoom == null )
			m_selectedRoom = null; // Fix for wierd bug: In case of missing reference, clear the room reference so that it will never match the new room. I didn't know that could happen!

		RoomComponent oldRoom = m_selectedRoom;
		if ( newRoom == null )
			m_selectedRoom = null;

		if ( (newRoom != null && oldRoom != newRoom)
			|| (newRoom == null) != (m_listProps == null && m_listHotspots == null && m_listRegions == null) ) // if changed, or lists are obviously out of date
		{
			m_selectedRoom = newRoom;
			m_listHotspots = null;
			m_listProps = null;
			m_listRegions = null;
			m_listPoints = null;
			m_listWalkableAreas = null;
			m_listRoomCharacters = null;
			m_listRoomCharactersUsed = null;
			m_selectedRoomPoint = -1;

			if ( m_selectedRoom != null )
			{
				m_selectedRoom.EditorUpdateChildComponents();

				// The selected room will be an instance unless the game is running
				bool isInstance = PrefabUtility.GetPrefabInstanceStatus(m_selectedRoom.gameObject) == PrefabInstanceStatus.Connected;

				if ( isInstance )
				{
					// If it's the room instance that's being edited, allow to add/remove hotspots
					m_listHotspots = new ReorderableList(m_selectedRoom.GetHotspotComponents(),typeof(HotspotComponent)) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Hotspots"); },
						drawElementCallback = 	LayoutHotspotGUI,
						onSelectCallback = 		SelectGameObjectFromList,
						onReorderCallback =     (ReorderableList list)=>UpdateRoomObjectOrder(),
						onAddCallback = 		(ReorderableList list) => 	
						{ 
							ScriptableObject.CreateInstance< CreateRoomObjectWindow >().ShowQuestWindow( eQuestObjectType.Hotspot, PowerQuest.STR_HOTSPOT, "'Shrubbery' or 'DistantCity'",  PowerQuestEditor.CreateHotspot);
						},
						onRemoveCallback = 		(ReorderableList list) => { DeleteHotspot(list.index); }
					};

					m_listProps = new ReorderableList(m_selectedRoom.GetPropComponents(),typeof(PropComponent)) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Props"); },
						drawElementCallback = 	LayoutPropGUI,
						onSelectCallback = 		SelectGameObjectFromList,
						onReorderCallback =     (ReorderableList list)=>UpdateRoomObjectOrder(),
						onAddCallback = 		(ReorderableList list) => 
						{ 
							ScriptableObject.CreateInstance< CreatePropWindow >().ShowUtility();
						},
						onRemoveCallback = 		(ReorderableList list) => { DeleteProp(list.index); }
					};

					// If it's the room instance that's being edited, allow to add/remove regions
					m_listRegions = new ReorderableList(m_selectedRoom.GetRegionComponents(),typeof(RegionComponent)) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Regions"); },
						drawElementCallback = 	LayoutRegionGUI,
						onSelectCallback = 		SelectGameObjectFromList,
						onReorderCallback =     (ReorderableList list)=>UpdateRoomObjectOrder(),
						onAddCallback = 		(ReorderableList list) => 	
						{ 
							ScriptableObject.CreateInstance< CreateRoomObjectWindow >().ShowQuestWindow( eQuestObjectType.Region, PowerQuest.STR_REGION, "'Puddle' or 'Quicksand'",  PowerQuestEditor.CreateRegion);
						},
						onRemoveCallback = 		(ReorderableList list) => { DeleteRegion(list.index); }
					};

					m_listPoints = new ReorderableList(m_selectedRoom.GetData().GetPoints(),typeof(Room.RoomPoint)) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Points"); },
						drawElementCallback = 	LayoutRoomPointGUI,
						onSelectCallback = 		(ReorderableList list) => 
						{ 
							Selection.activeObject = null; 
							UnselectSceneTools(); 
							m_selectedRoomPoint = list.index; 
							SceneView.RepaintAll(); 
						},
						onAddCallback = 		(ReorderableList list) => 
						{ 
							Undo.RecordObject(m_selectedRoom, "Added Point");
							m_selectedRoom.GetData().GetPoints().Add(new Room.RoomPoint() { m_name = "Point"+m_selectedRoom.GetData().GetPoints().Count } ); 
							EditorUtility.SetDirty(m_selectedRoom);
						},
						//onRemoveCallback = 	(ReorderableList list) => { DeletePosition(list.index); }
					};

					m_listWalkableAreas = new ReorderableList(m_selectedRoom.GetWalkableAreas(),typeof(WalkableComponent)) 
					{ 
						drawHeaderCallback =    (Rect rect) => { EditorGUI.LabelField(rect, "Walkable Areas"); },
						drawElementCallback =   LayoutWalkableAreaGUI,
						onSelectCallback =      SelectGameObjectFromList,
						onReorderCallback =     (ReorderableList list)=>UpdateRoomObjectOrder(),
						onAddCallback =         (ReorderableList list) =>   { CreateWalkableArea(); },
						onRemoveCallback =      (ReorderableList list) => { DeleteWalkableArea(list.index); }
					};
				}
				else 
				{
					// This should only happen when the game is running now.
					// If it's the room prefab that's being edited, DON'T allow to add/remove hotspots. Couldn't find a way to add/remove children of the prefab without errors
					m_listHotspots = new ReorderableList(m_selectedRoom.GetHotspotComponents(),typeof(HotspotComponent), true, true, false, false) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Hotspots"); },
						drawElementCallback = 	LayoutHotspotGUI,
						onSelectCallback = 		SelectGameObjectFromList
					};

					m_listProps = new ReorderableList(m_selectedRoom.GetPropComponents(),typeof(PropComponent), true, true, false, false) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Props"); },
						drawElementCallback = 	LayoutPropGUI,
						onSelectCallback = 		SelectGameObjectFromList
					};

					m_listRegions = new ReorderableList(m_selectedRoom.GetRegionComponents(),typeof(RegionComponent), true, true, false, false) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Regions"); },
						drawElementCallback = 	LayoutRegionGUI,
						onSelectCallback = 		SelectGameObjectFromList
					};

					m_listPoints = new ReorderableList(m_selectedRoom.GetData().GetPoints(),typeof(Room.RoomPoint), true, true, false, false) 
					{ 
						drawHeaderCallback = 	(Rect rect) => { EditorGUI.LabelField(rect, "Points"); },
						drawElementCallback = 	LayoutRoomPointGUI,
						onSelectCallback = 		(ReorderableList list) => { m_selectedRoomPoint = list.index; },
					};

					m_listWalkableAreas = new ReorderableList(m_selectedRoom.GetWalkableAreas(),typeof(WalkableComponent), true, true, false, false) 
					{ 
						drawHeaderCallback =    (Rect rect) => { EditorGUI.LabelField(rect, "Walkable Areas"); },
						drawElementCallback =   LayoutWalkableAreaGUI,
						onSelectCallback =      SelectGameObjectFromList
					};
				}
			}
			
			m_listRoomCharacters =     new ReorderableList( m_powerQuest.GetCharacterPrefabs(), typeof(CharacterComponent),false,true,false,false) 
			{ 			
				drawHeaderCallback = 	(Rect rect) => FoldoutRoomCharacters(rect),
				drawElementCallback = 	LayoutRoomCharacterGUI,
				onSelectCallback = 		SelectGameObjectFromList,
			};
			
			RefreshCharactersWithFunctionsList(newRoom);
			m_listRoomCharactersUsed = new ReorderableList( m_charactersWithFunctions, typeof(CharacterComponent),false,true,false,false) 
			{ 			
				drawHeaderCallback = 	(Rect rect) => FoldoutRoomCharacters(rect),
				drawElementCallback = 	LayoutRoomCharacterGUI,
				onSelectCallback = 		SelectGameObjectFromList,
			};

			UpdatePlayFromFuncs();	

			if ( repaint ) 
				Repaint();
		}
	}

	//! List of only characters with functions...
	List<CharacterComponent> m_charactersWithFunctions = new List<CharacterComponent>();

	void FoldoutRoomCharacters(Rect rect)
	{
		m_showRoomCharacters = EditorGUI.Foldout(rect, m_showRoomCharacters,"Characters (Room Specific Interactions)", true);
		if ( m_showRoomCharacters == false )
			RefreshCharactersWithFunctionsList();
	}

	void RefreshCharactersWithFunctionsList(RoomComponent room = null)
	{
		if ( room == null )
			room = m_selectedRoom;
		if ( room == null )
			return;
		BeginHighlightingMethodButtons(room.GetData());

		// If false, only show characters with functions... so we need to create this list
		m_charactersWithFunctions.Clear();
		foreach (CharacterComponent charComp in m_powerQuest.GetCharacterPrefabs() )
		{
			string name = charComp.GetData().ScriptName;
			if (    HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_LOOKAT_CHARACTER+ name)
			     || HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_INTERACT_CHARACTER+ name)
			     || HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_USEINV_CHARACTER+ name) )
			{
				m_charactersWithFunctions.Add(charComp);
			}		
		}	  
		EndHighlightingMethodButtons();
	}

	

	// Character on room panel
	bool ShouldShowRoomCharacter(Character character)
	{
		bool result = false;
		return result;
	}

	void UpdateRoomObjectOrder(bool applyPrefab = true)
	{
		
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null || powerQuestEditor.m_selectedRoom == null)
			return;

		int index = 0;					
		powerQuestEditor.m_selectedRoom.GetWalkableAreas().ForEach(item=> item.transform.SetSiblingIndex(index++));
		powerQuestEditor.m_selectedRoom.GetHotspotComponents().ForEach(item=> item.transform.SetSiblingIndex(index++));
		powerQuestEditor.m_selectedRoom.GetPropComponents().ForEach(item=> item.transform.SetSiblingIndex(index++));
		powerQuestEditor.m_selectedRoom.GetRegionComponents().ForEach(item=> item.transform.SetSiblingIndex(index++));
	
		if ( applyPrefab )
		{
			QuestEditorUtils.ReplacePrefab(powerQuestEditor.m_selectedRoom.gameObject);
			powerQuestEditor.Repaint(); 

		}	
	}

	#endregion
	#region Gui Layout: Room and contents

	void OnGuiRoom( bool tabChanged )
	{	    
		//
		// Layout room contents
		//

		if ( m_selectedRoom == null )
		{
			GUILayout.Label("Select a room's scene in the Main Panel", EditorStyles.centeredGreyMiniLabel);			
			return;		 
		}

		//GUILayout.Space(2);
					
		#if UNITY_2018_3_OR_NEWER	
		bool isPrefab = PrefabUtility.GetPrefabInstanceStatus(m_selectedRoom.gameObject) != PrefabInstanceStatus.Connected;
		#else 
		bool isPrefab =  PrefabUtility.GetPrefabType(m_selectedRoom.gameObject) == PrefabType.Prefab;
		#endif
		
		//! Play From Func Selector
		var headerStyle = new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter };
		var roomHeader = m_selectedRoom.GetData().ScriptName + (isPrefab ? " (Prefab)" : "");
		var isPlayFromSetAndHidden = 
			m_selectedRoom != null && 
			!string.IsNullOrEmpty(m_selectedRoom.m_debugStartFunction) &&
			m_scrollPosition.y > 23;
		if (isPlayFromSetAndHidden)
		{
			var selectedFuncIndex = m_playFromFuncs.IndexOf(m_selectedRoom.m_debugStartFunction);
			if (selectedFuncIndex >= 0)
			{
				headerStyle.richText = true;
				roomHeader += $" from <color=orange>{m_playFromFuncsNicified[selectedFuncIndex]}</color>";
			}
		}
		if (GUILayout.Button(roomHeader, headerStyle) && isPlayFromSetAndHidden)
		{
			m_scrollPosition.y = 0;
			m_showPlayFromFunctions = true;
		}
		//GUILayout.Label(roomHeader, headerStyle);
		//GUILayout.Label( m_selectedRoom.GetData().ScriptName + ( isPrefab ? " (Prefab)" : "" ), new GUIStyle(EditorStyles.largeLabel){alignment=TextAnchor.MiddleCenter});
		
				
		GUILayout.BeginHorizontal();

		if ( GUILayout.Button( "Select", EditorStyles.miniButtonLeft ) ) 
		{ 
			Selection.activeObject = m_selectedRoom.gameObject; 
			GameObject room = QuestEditorUtils.GetPrefabParent(m_selectedRoom.gameObject, true);
			if ( room == null && Application.isPlaying ) // in play mode, GetPrefabParent doesn't work :'(
			{
				RoomComponent roomC = GetPowerQuestEditor().GetRoom(m_selectedRoom.GetData().ScriptName);
				room = roomC != null ? roomC.gameObject : null;				
			}
			EditorGUIUtility.PingObject( room );
		}


		if ( GUILayout.Button( "Script", EditorStyles.miniButtonMid ) )
		{ 
			// Open the script
			QuestScriptEditor.Open( m_selectedRoom );	
		}
		else if ( GUILayout.Button("...", new GUIStyle(EditorStyles.miniButtonRight){fixedWidth=30} ) )
		{ 		
			GenericMenu menu = new GenericMenu();
			LayoutRoomScriptsContextMenu(menu,m_selectedRoom);
			menu.ShowAsContext();
		}
		GUILayout.EndHorizontal();
		
		m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
		
		//! Play From Func Selector
		LayoutPlayFromGUI();

		if ( m_listHotspots != null ) m_listHotspots.DoLayoutList();

		GUILayout.Space(5);

		if ( m_listProps != null ) m_listProps.DoLayoutList();

		GUILayout.Space(5);

		if ( m_listRegions != null ) m_listRegions.DoLayoutList();

		GUILayout.Space(5);

		if ( m_listPoints != null ) m_listPoints.DoLayoutList();

		GUILayout.Space(5);

		if ( m_listWalkableAreas != null ) m_listWalkableAreas.DoLayoutList();

		GUILayout.Space(5);

		// Layout characters
		if ( m_showRoomCharacters && m_listRoomCharacters != null) 
			m_listRoomCharacters.DoLayoutList();
		else if ( m_showRoomCharacters == false && m_listRoomCharactersUsed != null )
			m_listRoomCharactersUsed.DoLayoutList();
		//else  
		//	m_showRoomCharacters = EditorGUILayout.Foldout(m_showRoomCharacters,"Characters (Room Specific Interactions)", true); 

		EditorGUILayout.EndScrollView();

		GUILayout.Label($"Mouse Pos (Ctrl+M to copy): {Mathf.RoundToInt(m_mousePos.x)}, {Mathf.RoundToInt(m_mousePos.y)}".PadRight(38,' '), EditorStyles.centeredGreyMiniLabel);
		GUILayout.Space(3);
	}

	//! Play From Func Selector
	private bool m_showPlayFromFunctions = false;
	private string m_newPlayFromName = "";
	private void LayoutPlayFromGUI()
	{
		// if ( m_playFromFuncs.Count > 1 )
		// {
		// 	debugFuncId = m_playFromFuncs.FindIndex(item => string.Compare(item, m_selectedRoom.m_debugStartFunction, true) == 0);
		// 	debugFuncId = EditorGUILayout.Popup("Play-from function: ", debugFuncId, m_playFromFuncs.ToArray(), new GUIStyle(EditorStyles.toolbarPopup));
		// 	if (debugFuncId <= 0)
		// 		m_selectedRoom.m_debugStartFunction = null;
		// 	else
		// 		m_selectedRoom.m_debugStartFunction = m_playFromFuncs[debugFuncId];
		//
		// 	GUILayout.Space(8);
		// }
		
		var gridStyle = new GUIStyle("ObjectField")
		{
			margin = new RectOffset(4, 4, 5, 5),
			padding = new RectOffset(3, 3, 3, 3),
			stretchHeight = false
		};
		var oldEnabled = GUI.enabled;
		GUI.enabled = false;
		GUILayout.BeginVertical(gridStyle);
		GUI.enabled = oldEnabled;
		
		var headerStyle = new GUIStyle("DropDown") {
			margin = new RectOffset(0, 0, 0, 0),
			padding = new RectOffset(8, 0, 0, 0),
			alignment = TextAnchor.MiddleLeft,
			fixedHeight = 25,
			richText = true
		};

		var selectedFuncIndex = m_playFromFuncs.IndexOf(m_selectedRoom.m_debugStartFunction);
		var dropdownLabel = selectedFuncIndex >= 0
			? $"Play from <color=orange><b>{m_playFromFuncsNicified[selectedFuncIndex]}</b></color>"
			: "Play from...";
		if (GUILayout.Button(dropdownLabel, headerStyle))
		{
			m_showPlayFromFunctions = !m_showPlayFromFunctions;
		}
		
		if (m_showPlayFromFunctions)
		{
			var playFromFuncStyle = new GUIStyle("TE toolbarbutton") {
				margin = new RectOffset(15, 0, 0, 0),
				padding = new RectOffset(10, 0, 0, 0),
				alignment = TextAnchor.MiddleLeft,
				fontSize = 12,
				fixedHeight = 22,
				stretchWidth = true,
				normal = { textColor = Color.white },
				hover = { textColor = Color.white }
			};
			var btnStyle = new GUIStyle(playFromFuncStyle) {
				margin = new RectOffset(-1, -2, 0, 0),
				padding = new RectOffset(0,0,0,0),
				alignment = TextAnchor.MiddleCenter
			};

			// Function buttons
			var oldContentColor = GUI.contentColor;
			var funcToDeleteIndex = -1;
			for (int i = 0; i < m_playFromFuncs.Count; i++)
			{
				if (i == 0 && string.IsNullOrEmpty(m_selectedRoom.m_debugStartFunction)) continue;
				
				var funcName = m_playFromFuncs[i];
				var funcTitle = m_playFromFuncsNicified[i];
				
				GUILayout.BeginHorizontal();
				
				// Select
				if (m_selectedRoom.m_debugStartFunction.Equals(funcName)) GUI.contentColor = new Color(1f, 0.67f, 0.19f);
				if (i == 0)
				{
					GUI.contentColor = new Color(1f, 1f, 1f, 0.65f);
				}
				if (GUILayout.Button(funcTitle, playFromFuncStyle))
				{
					m_selectedRoom.m_debugStartFunction = i == 0  
						? ""
						: funcName;
					m_showPlayFromFunctions = false;
				}
				GUI.contentColor = oldContentColor;
				
				if (i > 0)
				{
					// Edit
					if (GUILayout.Button(
						    new GUIContent(EditorGUIUtility.IconContent("align_horizontally_left_active").image), 
						    btnStyle, 
						    GUILayout.Width(25)))
					{
						QuestScriptEditor.Open( m_selectedRoom, QuestScriptEditor.eType.Other,
							funcName, isCoroutine: false);
					}

					// Delete
					if (GUILayout.Button(
						    new GUIContent(EditorGUIUtility.IconContent("d_winbtn_win_close").image), 
						    btnStyle, 
						    GUILayout.Width(25)))
					{
						string fileName = m_selectedRoom.GetData().GetScriptClassName() +".cs";
						string path = QuestEditorUtils.GetScriptPath(m_selectedRoom.GetPrefab(), fileName);
						var erased = QuestEditorUtils.ErasePlayFromFunction(
							path,
							funcName
						);
						if (erased >= 0)
						{
							if (m_selectedRoom.m_debugStartFunction == funcName)
							{
								m_selectedRoom.m_debugStartFunction = string.Empty;
							}

							QuestScriptEditor editor = EditorWindow.GetWindow<QuestScriptEditor>();
							if (editor != null)
							{
								if (editor.IsFunctionLoaded(path, funcName))
								{
									editor.LoadPreviousHistoryEntry();
								}
							}
							
							funcToDeleteIndex = i;
						}
					}
				}

				GUILayout.EndHorizontal();

				if (i == 0)
				{
					GUILayout.Space(1);
				}
			}
			
			// Deleting cached elements for function erased with ×
			if (funcToDeleteIndex > 0)
			{
				m_playFromFuncs.RemoveAt(funcToDeleteIndex);
				m_playFromFuncsNicified.RemoveAt(funcToDeleteIndex);
			}
			
			GUILayout.Space(3);
			
			// Add new function
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			m_newPlayFromName = GUILayout.TextField(m_newPlayFromName, GUILayout.ExpandWidth(true), GUILayout.Height(22));
			if (GUILayout.Button("Add", GUILayout.Width(47), GUILayout.Height(22)))
			{
				string validationPattern = @"^[a-zA-Z][a-zA-Z0-9_ ]*$";
				if (Regex.IsMatch(m_newPlayFromName, validationPattern))
				{
					var finalFunctionName = "";
					
					finalFunctionName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m_newPlayFromName);
					finalFunctionName = "PlayFrom" + finalFunctionName.Replace(" ", "");
					
					string fileName = m_selectedRoom.GetData().GetScriptClassName() +".cs";
					string path = QuestEditorUtils.GetScriptPath(m_selectedRoom.GetPrefab(), fileName);
					var created = QuestEditorUtils.CreatePlayFromFunction(
						path,
						finalFunctionName
						);
					
					if (created >= 0)
					{
						m_playFromFuncs.Add(finalFunctionName);
						m_playFromFuncsNicified.Add(
							ObjectNames.NicifyVariableName(finalFunctionName
								.Replace("PlayFrom", "")));

						m_selectedRoom.m_debugStartFunction = finalFunctionName;
						
						QuestScriptEditor.Open(m_selectedRoom, QuestScriptEditor.eType.Other,
							finalFunctionName, isCoroutine: false);

						m_newPlayFromName = "";
						GUI.FocusControl(null);
					}
				}
			}
			GUILayout.Space(3);
			GUILayout.EndHorizontal();
			GUILayout.Space(3);
		}
		
		GUILayout.EndVertical();
	}

	void LayoutRoomScriptsContextMenu(GenericMenu menu, RoomComponent component, string path = "" )
	{ 				
		menu.AddItem(
			"Header", true,()=> QuestScriptEditor.Open( m_selectedRoom ));

		menu.AddSeparator("");
		menu.AddItem(path+"On Enter Room (BG)",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnEnterRoom","", false) );
		menu.AddItem(path+"On Enter Room After Fade",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnEnterRoomAfterFade") );
		menu.AddItem(path+"On Exit Room",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnExitRoom", " IRoom oldRoom, IRoom newRoom ") );
		menu.AddItem(path+"Update Blocking",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "UpdateBlocking") );
		menu.AddItem(path+"Update (BG)",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "Update","", false) );
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Parser) )
			menu.AddItem(path+"On Parser",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnParser") );
		menu.AddItem(path+"On Any Click",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnAnyClick") );
		menu.AddItem(path+"After Any Click",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "AfterAnyClick") );
		menu.AddItem(path+"On Walk To",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnWalkTo") );
		menu.AddItem(path+"Post-Restore Game (BG)",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnPostRestore", " int version ", false) );
		menu.AddItem(path+"Unhandled Interact",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "UnhandledInteract", " IQuestClickable mouseOver ", true) );
		menu.AddItem(path+"Unhandled Use Inv",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "UnhandledUseInv", " IQuestClickable mouseOver, IInventory item ", true) );
	}

	void LayoutHotspotGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_selectedRoom != null && m_selectedRoom.GetHotspotComponents().IsIndexValid(index))
		{

			HotspotComponent itemComponent = m_selectedRoom.GetHotspotComponents()[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{		
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Hotspot, m_listHotspots, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0);
					
				EditorLayouter layout = new EditorLayouter(new Rect(rect){y= rect.y+2,height=EditorGUIUtility.singleLineHeight});
				layout.Variable(1);
				for (int i = 0; i< actionCount; ++i)
					layout.Fixed(36);
				// layout.Fixed(22); // for '...'

				EditorGUI.LabelField(layout, itemComponent.GetData().ScriptName );
				
				//!
				BeginHighlightingMethodButtons(m_selectedRoom.GetData());

				int actionNum = 0;
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_LOOKAT_HOTSPOT+ itemComponent.GetData().ScriptName);
					if ( GUI.Button(layout, "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// Lookat
						QuestScriptEditor.Open( m_selectedRoom, QuestScriptEditor.eType.Hotspot,
							PowerQuest.SCRIPT_FUNCTION_LOOKAT_HOTSPOT+ itemComponent.GetData().ScriptName,
							PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_HOTSPOT);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_INTERACT_HOTSPOT+ itemComponent.GetData().ScriptName);
					if ( GUI.Button(layout, "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// Interact
						QuestScriptEditor.Open( m_selectedRoom, QuestScriptEditor.eType.Hotspot,
							PowerQuest.SCRIPT_FUNCTION_INTERACT_HOTSPOT+ itemComponent.GetData().ScriptName,
							PowerQuestEditor.SCRIPT_PARAMS_INTERACT_HOTSPOT);
					}
				}

				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
				{
					//!
					bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_USEINV_HOTSPOT+ itemComponent.GetData().ScriptName);
					if ( GUI.Button(layout, "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
					{
						// UseItem
						QuestScriptEditor.Open( m_selectedRoom, QuestScriptEditor.eType.Hotspot,
							PowerQuest.SCRIPT_FUNCTION_USEINV_HOTSPOT+ itemComponent.GetData().ScriptName,
							PowerQuestEditor.SCRIPT_PARAMS_USEINV_HOTSPOT);
					}
				}
				
				//!
				EndHighlightingMethodButtons();
				
				/* Not sure if want this for hotspots/props yet				
				if ( GUI.Button(layout, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Hotspot, m_listHotspots, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );				
				*/

			}
		}
	}

	void LayoutPropGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_selectedRoom != null && m_selectedRoom.GetPropComponents().IsIndexValid(index))
		{             

			PropComponent itemComponent = m_selectedRoom.GetPropComponents()[index];
			bool hasCollider = itemComponent.GetComponent<Collider2D>() != null;
			if ( itemComponent != null && itemComponent.GetData() != null )
			{         
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Prop, m_listProps, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0);
					
				EditorLayouter layout = new EditorLayouter(new Rect(rect){y= rect.y+2,height=EditorGUIUtility.singleLineHeight});
				layout.Variable(1);
				for (int i = 0; i< actionCount; ++i)
					layout.Fixed(36);

				EditorGUI.LabelField(layout, itemComponent.GetData().ScriptName);

				//!
				BeginHighlightingMethodButtons(m_selectedRoom.GetData());
				
				int actionNum = 0;
				if ( hasCollider )
				{
					if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
					{
						//!
						bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_LOOKAT_PROP+ itemComponent.GetData().ScriptName);
						if ( GUI.Button(layout, "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
						{
							// Lookat
							QuestScriptEditor.Open( 
								m_selectedRoom, QuestScriptEditor.eType.Prop,
								PowerQuest.SCRIPT_FUNCTION_LOOKAT_PROP+ itemComponent.GetData().ScriptName,
								PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_PROP);
						}
					}
					if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
					{
						//!
						bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_INTERACT_PROP+ itemComponent.GetData().ScriptName);
						if ( GUI.Button(layout, "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
						{
							// Interact
							QuestScriptEditor.Open( 
								m_selectedRoom, QuestScriptEditor.eType.Prop,
								PowerQuest.SCRIPT_FUNCTION_INTERACT_PROP+ itemComponent.GetData().ScriptName,
								PowerQuestEditor.SCRIPT_PARAMS_INTERACT_PROP);
						}
					}
					if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
					{
						//!
						bool bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_USEINV_PROP+ itemComponent.GetData().ScriptName);
						if ( GUI.Button(layout, "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
						{
							// UseItem
							QuestScriptEditor.Open( 
								m_selectedRoom, QuestScriptEditor.eType.Prop,
								PowerQuest.SCRIPT_FUNCTION_USEINV_PROP+ itemComponent.GetData().ScriptName,
								PowerQuestEditor.SCRIPT_PARAMS_USEINV_PROP);
						}
					}
				}
				
				//!
				EndHighlightingMethodButtons();
			}
		}
	}

	void LayoutRegionGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_selectedRoom != null && m_selectedRoom.GetRegionComponents().IsIndexValid(index))
		{   
			RegionComponent itemComponent = m_selectedRoom.GetRegionComponents()[index];
			
			if ( itemComponent != null && itemComponent.GetData() != null )
			{
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Region, m_listRegions, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				EditorLayouter layout = new EditorLayouter(new Rect(rect){y= rect.y+2,height=EditorGUIUtility.singleLineHeight});
				layout.Variable().Fixed(42).Fixed(25).Fixed(32).Fixed(25);

				int actionCount = 2;
				EditorGUI.LabelField(layout, itemComponent.GetData().ScriptName);

				//!
				BeginHighlightingMethodButtons(m_selectedRoom.GetData());
				
				int actionNum = 0;
				bool bold = false;
				//!
				bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_ENTER_REGION+ itemComponent.GetData().ScriptName);
				if ( GUI.Button(layout, "Enter", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
				{
				QuestScriptEditor.Open( m_selectedRoom, QuestScriptEditor.eType.Region,
					PowerQuest.SCRIPT_FUNCTION_ENTER_REGION+ itemComponent.GetData().ScriptName,
					PowerQuestEditor.SCRIPT_PARAMS_ENTER_REGION);
				}
				//!
				bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_ENTER_REGION_BG + itemComponent.GetData().ScriptName);
				if (GUI.Button(layout, "BG", QuestEditorUtils.GetMiniButtonStyle(actionNum++, actionCount, bold)))
				{
					QuestScriptEditor.Open(m_selectedRoom, QuestScriptEditor.eType.Region,
						PowerQuest.SCRIPT_FUNCTION_ENTER_REGION_BG + itemComponent.GetData().ScriptName,
						PowerQuestEditor.SCRIPT_PARAMS_ENTER_REGION,false);
				}
				//!
				bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_EXIT_REGION+ itemComponent.GetData().ScriptName);
				if ( GUI.Button(layout, "Exit", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount, bold) ) )
				{
					QuestScriptEditor.Open( m_selectedRoom, QuestScriptEditor.eType.Region,
						PowerQuest.SCRIPT_FUNCTION_EXIT_REGION+ itemComponent.GetData().ScriptName,
						PowerQuestEditor.SCRIPT_PARAMS_EXIT_REGION);
				}
				//!
				bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_EXIT_REGION_BG + itemComponent.GetData().ScriptName);
				if (GUI.Button(layout, "BG", QuestEditorUtils.GetMiniButtonStyle(actionNum++, actionCount, bold)))
				{
					QuestScriptEditor.Open(m_selectedRoom, QuestScriptEditor.eType.Region,
						PowerQuest.SCRIPT_FUNCTION_EXIT_REGION_BG + itemComponent.GetData().ScriptName,
						PowerQuestEditor.SCRIPT_PARAMS_EXIT_REGION,false);
				}
				
				//!
				EndHighlightingMethodButtons();

			}
		}
	}

	void LayoutRoomPointGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_selectedRoom != null && m_selectedRoom.GetData().GetPoints().IsIndexValid(index))
		{
			Room.RoomPoint point = m_selectedRoom.GetData().GetPoints()[index];
			if ( point != null )
			{
				Undo.RecordObject(m_selectedRoom,"Point Changed");
				EditorGUI.BeginChangeCheck();
				EditorLayouter layout = new EditorLayouter(new Rect(rect){y=rect.y+2,height=EditorGUIUtility.singleLineHeight});
				layout.Variable().Fixed(100);

				if ( index == m_selectedRoomPoint )
					point.m_name = EditorGUI.TextField(layout, point.m_name).Trim();
				else 
					EditorGUI.LabelField(layout, point.m_name);
				
				float x = point.m_position.x;
				float y = point.m_position.y;

				//position.m_name = EditorGUI.DelayedTextField(new Rect(rect.x, rect.y, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), position.m_name);
				float[] xy = new float[] {x,y};
				GUIContent[] xyLbl = new GUIContent[] {new GUIContent("x"),new GUIContent("y")};

				EditorGUI.MultiFloatField(layout,xyLbl,xy);
				if ( EditorGUI.EndChangeCheck() )
				{
					point.m_position = Utils.SnapRound(new Vector2(xy[0],xy[1]),PowerQuestEditor.SnapAmount);
					EditorUtility.SetDirty(m_selectedRoom);
					SceneView.RepaintAll();
				}
			}
		}
	}

	//public static readonly GUIStyle TOOLBAR_TOGGLE = new GUIStyle(EditorStyles.toggle) { font = EditorStyles.miniLabel.font, fontSize = EditorStyles.miniLabel.fontSize, padding = new RectOffset(15,0,3,0) };
	void LayoutWalkableAreaGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_selectedRoom != null && m_selectedRoom.GetWalkableAreas().IsIndexValid(index))
		{

			PolygonCollider2D itemComponent = m_selectedRoom.GetWalkableAreas()[index].PolygonCollider;
			if ( itemComponent != null )
			{
				EditorLayouter layout = new EditorLayouter(rect).Stretched.Fixed(60);
				
				float fixedWidth = 60;
				float totalFixedWidth = fixedWidth*1;
				float offset = rect.x;
				//EditorGUI.LabelField(new Rect(rect.x, rect.y+2, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), "Id: "+index.ToString());
				EditorGUI.LabelField(layout, "Id: "+index.ToString());

				offset += rect.width - totalFixedWidth;
		
				EditorGUI.BeginChangeCheck();
				GUI.Toolbar(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), QuestPolyTool.Active(itemComponent.gameObject)?0:-1, new string[]{"Edit"}, EditorStyles.miniButton);
				if ( EditorGUI.EndChangeCheck())
				{ 
					m_listWalkableAreas.index = index;
					QuestPolyTool.Toggle(itemComponent.gameObject);
				}
			}
		}
	}

	void LayoutRoomCharacterGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest == null )
			return;

		List<CharacterComponent> list = m_showRoomCharacters ? m_powerQuest.GetCharacterPrefabs() : m_charactersWithFunctions;
		if ( list.IsIndexValid(index) == false )
			return;

		CharacterComponent itemComponent = list[index];
		if ( itemComponent == null || itemComponent.GetData() == null )
			return;
			
		int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
			+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
			+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0);
		float totalFixedWidth = /*60+*/(36 *actionCount);
		float offset = rect.x;
		EditorGUI.LabelField(new Rect(rect.x, rect.y+2, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), itemComponent.GetData().GetScriptName(), (IsHighlighted(itemComponent)?EditorStyles.whiteLabel:EditorStyles.label) );
		offset += rect.width - totalFixedWidth;
		float fixedWidth = 36;
				
		//!
		BeginHighlightingMethodButtons(m_selectedRoom.GetData());
				
		int actionNum = 0; // start at one since there's already a left item
		bool bold = false;
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
		{
			//!
			bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_LOOKAT_CHARACTER+ itemComponent.GetData().ScriptName);
			if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1, bold) ) )
			{
				// Lookat
				QuestScriptEditor.Open( m_selectedRoom, PowerQuest.SCRIPT_FUNCTION_LOOKAT_CHARACTER+ itemComponent.GetData().ScriptName, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_ROOM_CHARACTER);
			}
			offset += fixedWidth;
			actionNum++;
		}
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
		{
			//!
			bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_INTERACT_CHARACTER+ itemComponent.GetData().ScriptName);
			if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1, bold) ) )
			{
				// Interact
				QuestScriptEditor.Open( m_selectedRoom, PowerQuest.SCRIPT_FUNCTION_INTERACT_CHARACTER+ itemComponent.GetData().ScriptName, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_ROOM_CHARACTER);
			}
			offset += fixedWidth;
			actionNum++;
		}
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
		{
			//!
			bold = HighlightMethodButton(PowerQuest.SCRIPT_FUNCTION_USEINV_CHARACTER+ itemComponent.GetData().ScriptName);
			if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) && GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1, bold) ) )
			{
				// UseItem
				QuestScriptEditor.Open( m_selectedRoom, PowerQuest.SCRIPT_FUNCTION_USEINV_CHARACTER+ itemComponent.GetData().ScriptName, PowerQuestEditor.SCRIPT_PARAMS_USEINV_ROOM_CHARACTER);
			}
			offset += fixedWidth;
			actionNum++;
		}
					
		//!
		EndHighlightingMethodButtons();
		
	}

	#endregion 
	#region Funcs: Layout Scene
	
	void OnSceneRoom(SceneView sceneView)
	{
		// Repaint if mouse moved in scene view
		if ( Event.current != null && Event.current.isMouse )
		{
			if ( m_selectedTab == eTab.Room )
				PowerQuestEditor.Get.Repaint();
		}

		float scale = QuestEditorUtils.GameResScale;

		/* Show the walkable area editor always? /
		if ( m_listWalkableAreas != null && m_selectedRoom.GetWalkableAreas().IsIndexValid(m_listWalkableAreas.index) )
			QuestPolyTool.DrawCollider(m_selectedRoom.GetWalkableAreas()[m_listWalkableAreas.index].gameObject);
		/**/

		// Update Room Points
		if ( m_selectedRoom != null && m_selectedTab == eTab.Room )
		{
			for ( int i = 0; i < m_selectedRoom.GetData().GetPoints().Count; ++i )
			{
				Room.RoomPoint point = m_selectedRoom.GetData().GetPoints()[i];

				Vector3 position = point.m_position.WithZ(0);
				if ( m_selectedRoomPoint == i )
				{						
					Vector2 newPos = Utils.SnapRound(Handles.PositionHandle( position, Quaternion.identity),PowerQuestEditor.SnapAmount );//,2.0f,new Vector3(0,1,0),Handles.DotHandleCap));										
					if ( point.m_position != newPos )
					{
						Undo.RecordObject(m_selectedRoom,"Point moved");
						point.m_position = newPos;
						Repaint();
					}

					if ( Event.current != null && Event.current.type == EventType.MouseDown && Event.current.button == 0  )
					{
						// Unselect when used
						Event.current.Use();
						m_selectedRoomPoint = -1;
					}
					else if ( m_editingPointMouseDown )
					{
						Selection.activeObject = null;
						if ( Event.current != null && Event.current.type == EventType.MouseLeaveWindow )
							m_editingPointMouseDown = false;						
					}
					if ( Selection.activeObject != null )
						m_selectedRoomPoint = -1;
						

				}
				else 
				{
					Handles.color = Color.yellow;
					Handles.DrawLine( position + (Vector3.left * 2*scale), position + (Vector3.right * 2*scale) );
					Handles.DrawLine( position + (Vector3.up * 2*scale), position + (Vector3.down * 2*scale) );
					
					if ( Event.current != null && Event.current.type == EventType.MouseDown && Event.current.button == 0 && Tools.current != Tool.Custom )
					{
						if ( (m_mousePos - (Vector2)position).sqrMagnitude < 6*scale*scale )
						{
							m_editingPointMouseDown = true;
							Selection.activeObject = null;
							UnselectSceneTools();
							Event.current.Use();
							m_selectedRoomPoint = i;
						}
						//Event.current.Use();
					}
				}
				GUI.color = Color.yellow;

				Handles.Label(position + new Vector3(1*scale,0,0), point.m_name, new GUIStyle(EditorStyles.boldLabel) {normal = { textColor = Color.yellow } } );

			}
		}
	}

	#endregion	
	#region Funcs: Util

	void UpdatePlayFromFuncs()
	{
		//! Play From Func Selector
		// Generate debug funcs list from attribtues in script
		if ( m_selectedRoom == null )
			return;
		m_playFromFuncs.Clear();
		m_playFromFuncsNicified.Clear();
		m_playFromFuncs.Add("None");
		m_playFromFuncsNicified.Add("...");
		System.Type type = System.Type.GetType( string.Format("{0}, {1}", m_selectedRoom.GetData().GetScriptClassName(),  typeof(PowerQuest).Assembly.FullName ));
		if (type == null)
			return;
		foreach( System.Reflection.MethodInfo method in type.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) )
		{						
			if ( method.GetCustomAttributes(typeof(QuestPlayFromFunctionAttribute),false).Length > 0 )
			{
				// Debug.Log( method.Name );		
				m_playFromFuncs.Add(method.Name);

				var niceName = ObjectNames
						.NicifyVariableName(method.Name)
						.Replace("Play From ", ""); 
				m_playFromFuncsNicified.Add(niceName);
			}
		}
	}

	#endregion
	
}

}
