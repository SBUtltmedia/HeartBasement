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
using System.Text.RegularExpressions;
using PowerTools.Quest;
using PowerTools.QuestGui;
using PowerTools;
using Object = UnityEngine.Object;
#if UNITY_2018_3_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace PowerTools.Quest
{


public partial class PowerQuestEditor : EditorWindow 
{
	#region Variables: Static definitions
	
	//
	// Static definitions
	//
	static readonly string PATH_GAME = "Assets/Game/";
	static readonly string PATH_AUDIO = "Assets/Audio";
	public static readonly string PATH_GLOBAL_SCRIPT = PATH_GAME+"GlobalScript.cs";
	static readonly string PATH_GAME_GLOBALS = PATH_GAME+"QuestScriptAutos.cs";
	static readonly string PATH_POWERQUEST = PATH_GAME+"PowerQuest.prefab";
	static readonly string PATH_SYSTEM_AUDIO = PATH_GAME;


	public static readonly string SCRIPT_PARAMS_INTERACT_CHARACTER = "";
	public static readonly string SCRIPT_PARAMS_INTERACT_ROOM_CHARACTER = " ICharacter character ";
	public static readonly string SCRIPT_PARAMS_INTERACT_PROP = " IProp prop ";
	public static readonly string SCRIPT_PARAMS_INTERACT_HOTSPOT = " IHotspot hotspot ";
	public static readonly string SCRIPT_PARAMS_INTERACT_INVENTORY = " IInventory thisItem ";

	public static readonly string SCRIPT_PARAMS_LOOKAT_CHARACTER = "";
	public static readonly string SCRIPT_PARAMS_LOOKAT_ROOM_CHARACTER = " ICharacter character ";
	public static readonly string SCRIPT_PARAMS_LOOKAT_PROP = " IProp prop ";
	public static readonly string SCRIPT_PARAMS_LOOKAT_HOTSPOT = " IHotspot hotspot ";
	public static readonly string SCRIPT_PARAMS_LOOKAT_INVENTORY = " IInventory thisItem ";

	public static readonly string SCRIPT_PARAMS_USEINV_CHARACTER = " IInventory item ";
	public static readonly string SCRIPT_PARAMS_USEINV_ROOM_CHARACTER = " ICharacter character, IInventory item ";
	public static readonly string SCRIPT_PARAMS_USEINV_PROP = " IProp prop, IInventory item ";
	public static readonly string SCRIPT_PARAMS_USEINV_HOTSPOT = " IHotspot hotspot, IInventory item ";
	public static readonly string SCRIPT_PARAMS_USEINV_INVENTORY = " IInventory thisItem, IInventory item ";

	public static readonly string SCRIPT_PARAMS_ENTER_REGION = " IRegion region, ICharacter character ";
	public static readonly string SCRIPT_PARAMS_EXIT_REGION = " IRegion region, ICharacter character ";

	public static readonly string SCRIPT_PARAMS_ONCLICK_GUI = " IGuiControl control ";
	public static readonly string SCRIPT_PARAMS_ONANYCLICK_GUI = " IGuiControl control ";
	public static readonly string SCRIPT_PARAMS_ONDRAG_GUI = " IGuiControl control ";
	
	static readonly string STR_PQ_SEARCH = "PQSearch";

	//struct ScriptFuncParamPair {string func, string param};
	public static readonly Dictionary<string, string> SCRIPT_FUNC_PARAM_MAP = new Dictionary<string,string>() 
	{ 
		{PowerQuest.SCRIPT_FUNCTION_INTERACT_PROP,			SCRIPT_PARAMS_INTERACT_PROP},
		{PowerQuest.SCRIPT_FUNCTION_INTERACT_HOTSPOT,		SCRIPT_PARAMS_INTERACT_HOTSPOT},
		{PowerQuest.SCRIPT_FUNCTION_INTERACT_INVENTORY,		SCRIPT_PARAMS_INTERACT_INVENTORY},

		{PowerQuest.SCRIPT_FUNCTION_LOOKAT_PROP,			SCRIPT_PARAMS_LOOKAT_PROP},
		{PowerQuest.SCRIPT_FUNCTION_LOOKAT_HOTSPOT,			SCRIPT_PARAMS_LOOKAT_HOTSPOT},
		{PowerQuest.SCRIPT_FUNCTION_LOOKAT_INVENTORY,		SCRIPT_PARAMS_LOOKAT_INVENTORY},

		{PowerQuest.SCRIPT_FUNCTION_USEINV,					SCRIPT_PARAMS_USEINV_CHARACTER},
		{PowerQuest.SCRIPT_FUNCTION_USEINV_PROP,			SCRIPT_PARAMS_USEINV_PROP},
		{PowerQuest.SCRIPT_FUNCTION_USEINV_HOTSPOT,			SCRIPT_PARAMS_USEINV_HOTSPOT},
		{PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY,		SCRIPT_PARAMS_USEINV_INVENTORY},

		{PowerQuest.SCRIPT_FUNCTION_ENTER_REGION,			SCRIPT_PARAMS_ENTER_REGION},
		{PowerQuest.SCRIPT_FUNCTION_EXIT_REGION,			SCRIPT_PARAMS_EXIT_REGION},
		{PowerQuest.SCRIPT_FUNCTION_ENTER_REGION_BG,		SCRIPT_PARAMS_ENTER_REGION},
		{PowerQuest.SCRIPT_FUNCTION_EXIT_REGION_BG,			SCRIPT_PARAMS_EXIT_REGION},
	};


	static readonly string PROPERTYNAME_SPRITE = "m_Sprite";

	public static readonly string[] STR_SCRIPT_FOLDERS = {"Assets/Game"};
	public static readonly string STR_SCRIPT_TYPE = " t:script";

	static readonly Vector2[] DEFAULT_COLLIDER_POINTS = new Vector2[]
	{
		new Vector2(-10,-10),
		new Vector2(-10,10),
		new Vector2(10,10),
		new Vector2(10,-10)
	};

	// Returns default collider points multiplied by game scale
	public static Vector2[] DefaultColliderPoints { get {
		float scale = QuestEditorUtils.GameResScale;
		return System.Array.ConvertAll(DEFAULT_COLLIDER_POINTS, item=> item*scale);
	} }


	// Aligned with eQuestObjectType
	static readonly string[] RENAME_QO_SHORT =
	{
		"R",// Room,
		"C",// Character,
		"I",// Inventory,
		"D",// Dialog,
		"G",// Gui,
		"X",// Prop,
		"X",// Hotspot,
		"X",// Region,
	};
	static readonly string[] RENAME_QO_LONG =
	{
		"Room",		 
		PowerQuest.STR_CHARACTER,
		PowerQuest.STR_INVENTORY,
		"DialogTree",
		"Gui",
		PowerQuest.STR_PROP,
		PowerQuest.STR_HOTSPOT,
		PowerQuest.STR_REGION,
	};
	static readonly string RENAME_SCRIPT_REGEX = @"(?<=<SHORTTYPE>\.)(<SCRIPTNAME>)(?=\W)|(?<=<LONGTYPE>\("")(<SCRIPTNAME>)(?=""\))|(?<=<TYPE>)(<SCRIPTNAME>)(?=\W)|(?<=I<LONGTYPE>\s+)(<SCRIPTNAME>)(?=\W)";

	/// This is a way to extend functionality of the editor without inheriting from PowerQuestEditor. USe the [InitializeOnLoad] attribute and call this static function to register. Bit of an experiment tbh
	public abstract class QuestEditorExtension
	{	
		public virtual void OnScene(PowerQuestEditor editor, SceneView sceneView){}
	}
	static List<QuestEditorExtension> s_editorExtensions = new List<QuestEditorExtension>();

	#endregion
	#region Variables: Serialized

	//
	// Serialized values
	//

	[SerializeField] Vector2 m_scrollPosition = Vector2.zero;
	[SerializeField] PowerQuest m_powerQuest = null;
	[SerializeField] string m_powerQuestPath = PATH_POWERQUEST;
	[SerializeField] string m_systemAudioPath = PATH_SYSTEM_AUDIO;
	[SerializeField] string m_gamePath = PATH_GAME;
	[SerializeField] SystemText m_systemText = null;
	[SerializeField] SystemParser m_systemParser = null;
	[SerializeField] SystemAudio m_systemAudio = null;

	[SerializeField] GameObject m_questCamera = null;
	[SerializeField] GameObject m_questGuiCamera = null;
	enum eTab { Main,Room,Gui,Tools,Count }
	[SerializeField] eTab m_selectedTab = eTab.Main;
		
	[SerializeField] bool m_smartCompileRequired = false;
	
	
	[SerializeField] long m_newVersionCheckTime = 0;


	#endregion
	
	#region Private Classes

	private class GroupedPrefabContext
	{
		public Dictionary<string, ReorderableList> GroupedCollection { get; } = new Dictionary<string, ReorderableList>();

		public ReorderableList UngroupedList { get; set; } = null;
	}
	
	#endregion
	
	#region Variables: Private

	static PowerQuestEditor m_instance = null;

	static bool m_registered = false;


	//
	// Menu items
	//
	ReorderableList m_listRooms = null;
	ReorderableList m_listCharacters = null;
	ReorderableList m_listInventory = null;
	ReorderableList m_listDialogTrees = null;
	ReorderableList m_listGuis = null;

	System.DateTime? m_sourceModifiedTime = null;

	
	#endregion
	#region Callbacks

	// Callback when room created in editor, eg- OnCreateRoom(string path, string name)
	public System.Action<string,string> CallbackOnCreateRoom = null;
	// Callback when quest object created in editor, eg- OnCreateRoom(eQuestObjectType type, string path, string name). For room objects (hotspots/props/etc) path will be null.
	public System.Action<eQuestObjectType, string,string> CallbackOnCreateObject = null;

	#endregion
	#region Menu Items

	//
	// Menu items
	//

	// Add menu item named "Super Animation Editor" to the Window menu
	[MenuItem("Window/PowerQuest")]
	public static void ShowWindow() 
	{
		//Show existing window instance. If one doesn't exist, make one.
		EditorWindow.GetWindow(typeof(PowerQuestEditor));
	}	

	[MenuItem("Assets/Create/PowerQuest/Character")]
	private static void CreateCharacter()
	{
		ScriptableObject.CreateInstance<CreateCharacterWindow>().ShowUtility();
	}

	[MenuItem("Assets/Create/PowerQuest/Inventory Item")]
	private static void CreateInventoryItem()
	{
		ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
			eQuestObjectType.Inventory,	PowerQuest.STR_INVENTORY, 
			"'RubberChicken' or 'TreasureMap'",
			CreateInventory);//new CreateQuestObjectWindow<InventoryComponent>.DelegateCreateFunction(CreateInventory));
	}
	[MenuItem("Assets/Create/PowerQuest/Gui")]
	private static void CreateGuiItem()
	{
		ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
			eQuestObjectType.Gui, "Gui", 
			"'Toolbar' or 'InventoryBox'", 
			new CreateQuestObjectWindow.DelegateCreateFunction(CreateGui));
	}


	[MenuItem("Assets/Create/PowerQuest/Room")]
	private static void CreateRoom()
	{
		ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
			eQuestObjectType.Room, "Room",
			"'Bathroom' or 'CastleGarden'", 
			new CreateQuestObjectWindow.DelegateCreateFunction(CreateRoom));
		//ScriptableObject.CreateInstance<CreateRoomWindow>().ShowUtility();
	}

	#endregion
	#region Functions: Misc
	 
	/// This is a way to extend functionality of the editor without inheriting from PowerQuestEditor. USe the [InitializeOnLoad] attribute and call this static function to register. Bit of an experiment tbh
	public static void AddQuestEditorExtension(QuestEditorExtension extension) { s_editorExtensions.Add(extension); }

	// Function to export the powerQuest package automatically
	public static void ExportPackage()
	{
		AssetDatabase.ExportPackage( new string[] {@"Assets\PowerQuest",@"Assets\Plugins"}, @"..\Packages\PowerQuest.unitypackage", ExportPackageOptions.Recurse);
	}	
	

	// Get/Set objects as "favorites", they just get highlighted for now
	public static bool IsHighlighted(Object obj)
	{
		if ( obj == null )
			return false;
		const string fav = "HL";
		return System.Array.Exists(AssetDatabase.GetLabels(obj), item=>string.Equals(item,fav));
	}	
	public static void ToggleHighlight(Object obj)
	{
		if ( obj == null )
			return;
		List<string> labels = new List<string>(AssetDatabase.GetLabels(obj));
		if ( IsHighlighted(obj) )
			labels.Remove("HL");
		else
			labels.Add("HL");
		AssetDatabase.SetLabels(obj,labels.ToArray());		

		// Refresh lists
		if ( m_instance )
			m_instance.CreateMainGuiLists();
	}

	#endregion
	#region Functions: Quest Inventory

	//
	// Room/Character/Hotspot/Prop manipulation functions
	//

	public static void CreateInventory( string path, string name )
	{
		// Make sure we can find powerQuest
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null ) 
			return;

		// create directory
		if ( Directory.Exists($"{path}/{name}") == false )
			AssetDatabase.CreateFolder(path,name);		
		path += "/" + name;

		// Create game object
		GameObject gameObject = new GameObject("Inventory"+name, typeof(InventoryComponent)) as GameObject; 

		InventoryComponent component = gameObject.GetComponent<InventoryComponent>();
		component.GetData().EditorInitialise(name);

		// turn game object into prefab
		#if UNITY_2018_3_OR_NEWER
		Object prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path + "/Inventory"+name+".prefab", InteractionMode.AutomatedAction);		
		#else
		Object prefab = PrefabUtility.CreatePrefab(path + "/Inventory"+name+".prefab", gameObject, ReplacePrefabOptions.ConnectToPrefab);
		#endif

		// Select the prefab for editing
		Selection.activeObject = prefab;

		// Delete the instance
		DestroyImmediate(gameObject);

		// Add item to list in PowerQuest and repaint the quest editor
		powerQuestEditor.m_powerQuest.GetInventoryPrefabs().Add(((GameObject)prefab).GetComponent<InventoryComponent>());
		EditorUtility.SetDirty(powerQuestEditor.m_powerQuest);
		powerQuestEditor.Repaint();

		// Add line to GameGlobals.cs for easy scripting
		QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#I", "\n\t\tpublic static IInventory "+name.PadRight(14)+" { get { return PowerQuest.Get.GetInventory(\""+name+"\"); } }");
		
		// Check that inventory atlas matches pixel settings
		QuestEditorUtils.UpdateAtlasSettings("Assets/Game/Inventory/InventoryAtlas.spriteatlas", powerQuestEditor.m_powerQuest.GetSnapToPixel() );
		
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Inventory, path, name);
		powerQuestEditor.RequestAssetRefresh();
		powerQuestEditor.RefreshMainGuiLists();
	}

	#endregion
	#region Functions: Quest Dialog Tree
	
	public static void CreateDialogTree( string path, string name )
	{
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null )
			return;

		// create directory
		if ( Directory.Exists($"{path}/{name}") == false )
			AssetDatabase.CreateFolder(path,name);		
		path += "/" + name;

		// Create game object
		GameObject gameObject = new GameObject("Dialog"+name, typeof(DialogTreeComponent)) as GameObject; 

		DialogTreeComponent component = gameObject.GetComponent<DialogTreeComponent>();
		component.GetData().EditorInitialise(name);

		// turn game object into prefab
		#if UNITY_2018_3_OR_NEWER
		Object prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path + "/Dialog"+name+".prefab", InteractionMode.AutomatedAction);		
		#else
		Object prefab = PrefabUtility.CreatePrefab(path + "/Dialog"+name+".prefab", gameObject, ReplacePrefabOptions.ConnectToPrefab);
		#endif

		// Select the prefab for editing
		Selection.activeObject = prefab;

		// Delete the instance
		DestroyImmediate(gameObject);

		// Add item to list in PowerQuest and repaint the quest editor
		powerQuestEditor.m_powerQuest.GetDialogTreePrefabs().Add(((GameObject)prefab).GetComponent<DialogTreeComponent>());
		EditorUtility.SetDirty(powerQuestEditor.m_powerQuest);
		powerQuestEditor.Repaint();

		// Add line to GameGlobals.cs for easy scripting		
		QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#D", "\n\t\tpublic static IDialogTree "+name.PadRight(20)+" { get { return PowerQuest.Get.GetDialogTree(\""+name+"\"); } }");
		
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Dialog, path, name);
		powerQuestEditor.RequestAssetRefresh();
		powerQuestEditor.RefreshMainGuiLists();

		
		QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Dialogs);
	}

	#endregion
	#region Functions: Quest Gui
	
	public static void CreateGui( string path, string name )
	{
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null )
			return;

		// create directory
		if ( Directory.Exists($"{path}/{name}") == false )
			AssetDatabase.CreateFolder(path,name);		
		path += "/" + name;
		
		// Create Sprite folder
		if ( Directory.Exists(path+"/Sprites") == false )
			AssetDatabase.CreateFolder(path,"Sprites");

		// Create importer
		PowerSpriteImport importer = PowerSpriteImportEditor.CreateImporter(path+"/_Import"+name+".asset", false);
		importer.m_createSingleSpriteAnims = true; // Guis can work with single sprite anims

		// Create atlas
		QuestEditorUtils.CreateSpriteAtlas($"{path}/Gui{name}Atlas.spriteatlas",$"{path}/Sprites",GetPowerQuest().GetSnapToPixel(),false,false);

		// Check that gui atlas matches pixel settings. Incase user has changed from pixel to non-pixel
		QuestEditorUtils.UpdateAtlasSettings("Assets/Game/Gui/GuiAtlas.spriteatlas", powerQuestEditor.m_powerQuest.GetSnapToPixel() );

		// Create game object
		GameObject gameObject = new GameObject("Gui"+name, typeof(GuiComponent)) as GameObject; 
		//gameObject.transform.position = new Vector3(0,1000,0);
		gameObject.layer = LayerMask.NameToLayer("UI");
		BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
		box.isTrigger = true;
		box.size = new Vector2(200,100);
		box.enabled = false;

		GuiComponent gui = gameObject.GetComponent<GuiComponent>();
		gui.GetData().EditorInitialise(name);

		// turn game object into prefab		
		#if UNITY_2018_3_OR_NEWER
		Object prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path + "/Gui"+name+".prefab", InteractionMode.AutomatedAction);		
		#else
		Object prefab = PrefabUtility.CreatePrefab(path + "/Gui"+name+".prefab", gameObject, ReplacePrefabOptions.ConnectToPrefab);
		#endif				

		// Select the prefab for editing
		Selection.activeObject = prefab;

		// Delete the instance
		DestroyImmediate(gameObject);

		// Add item to list in PowerQuest and repaint the quest editor 
		// NB: No longer need to 3 add to list,  they get added post-import
		//powerQuestEditor.m_powerQuest.GetGuiPrefabs().Add(((GameObject)prefab).GetComponent<GuiComponent>());
		EditorUtility.SetDirty(powerQuestEditor.m_powerQuest);
		powerQuestEditor.Repaint();

		// Add line to GameGlobals.cs for easy scripting
		QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#G", "\n\t\tpublic static IGui "+name.PadRight(14)+" { get { return PowerQuest.Get.GetGui(\""+name+"\"); } }");
		 
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Gui, path, name);
		powerQuestEditor.RequestAssetRefresh();
		powerQuestEditor.RefreshMainGuiLists();
		
		QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Guis);
	}


	#endregion
	#region Functions: Quest Character


	public static void CreateCharacter( string path, string name )
	{
		PowerQuestEditor powerQuestEditor = OpenPowerQuestEditor();
		if ( powerQuestEditor == null )
			return;

		// This will mark as needing smartcompile
		powerQuestEditor.RequestAssetRefresh();

		// create directory		
		if ( Directory.Exists($"{path}/{name}") == false )
			AssetDatabase.CreateFolder(path,name);		
		path += "/" + name;
		
		// Create Sprite folder
		if ( Directory.Exists(path+"/Sprites") == false )
			AssetDatabase.CreateFolder(path,"Sprites");

		// Create importer
		PowerSpriteImportEditor.CreateImporter(path+"/_Import"+name+".asset", false);

		// Create atlas
		QuestEditorUtils.CreateSpriteAtlas($"{path}/Character{name}Atlas.spriteatlas",$"{path}/Sprites",GetPowerQuest().GetSnapToPixel(),false,false);

		// Create game object
		GameObject gameObject = new GameObject(PowerQuest.STR_CHARACTER+name, typeof(CharacterComponent), typeof(PolygonCollider2D), typeof(PowerSprite), typeof(SpriteAnim)) as GameObject; 

		CharacterComponent component = gameObject.GetComponent<CharacterComponent>();
		component.GetData().EditorInitialise(name);

		PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();
		collider.isTrigger = true;
		collider.points = DefaultColliderPoints;
		/* Sprites now use PowerSprite tool
		GameObject spriteObj = new GameObject("Sprite", typeof(SpriteAnim)) as GameObject;
		spriteObj.transform.parent = gameObject.transform;
		spriteObj.GetComponent<SpriteRenderer>().sortingOrder = 10; // start sorting infront of most stuff
		*/
		component.GetComponent<SpriteRenderer>().sortingOrder = 100; // start sorting infront of most stuff

		// turn game object into prefab
		#if UNITY_2018_3_OR_NEWER
		Object prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path + "/Character"+name+".prefab", InteractionMode.AutomatedAction);		
		#else
		Object prefab = PrefabUtility.CreatePrefab(path + "/Character"+name+".prefab", gameObject, ReplacePrefabOptions.ConnectToPrefab);
		#endif

		// Select the prefab for editing
		Selection.activeObject = prefab;
		
		// Delete the instance
		DestroyImmediate(gameObject);

		// Add character to list in PowerQuest and repaint the quest editor
		powerQuestEditor.m_powerQuest.GetCharacterPrefabs().Add(((GameObject)prefab).GetComponent<CharacterComponent>());
		EditorUtility.SetDirty(powerQuestEditor.m_powerQuest);

		// Add line to GameGlobals.cs for easy scripting
		QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#C", "\n\t\tpublic static ICharacter "+name.PadRight(14)+" { get { return PowerQuest.Get.GetCharacter(\""+name+"\"); } }");
		
		powerQuestEditor.CallbackOnCreateObject?.Invoke(eQuestObjectType.Character, path, name);
		powerQuestEditor.RefreshMainGuiLists();
		powerQuestEditor.Repaint();
		
		powerQuestEditor.RequestAssetRefresh();
		
		QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Characters);

		// Stage the character 		
		EditorGUIUtility.PingObject(prefab);
		AssetDatabase.OpenAsset(prefab);
	}


	#endregion
	#region Functions: Rename Quest Objects

	public void RenameQuestObject( GameObject prefab, eQuestObjectType questType, string newName )
	{	
		if ( m_powerQuest == null || prefab == null || Application.isPlaying )			
			return;	

		bool isRoomChild = questType == eQuestObjectType.Hotspot || questType == eQuestObjectType.Prop || questType == eQuestObjectType.Region;

		// NB: Actually not always the prefab that's passed in, at least in the case of characters
		if ( isRoomChild == false )
		{
			// Ensure the prefab is selected (can't do this in play-mode)
			prefab = QuestEditorUtils.GetPrefabParent(prefab);
		}		

		IQuestScriptable scriptable = null;
		switch ( questType )
		{
		case eQuestObjectType.Room: scriptable = prefab.GetComponent<RoomComponent>().GetData(); break;
		case eQuestObjectType.Character: scriptable = prefab.GetComponent<CharacterComponent>().GetData(); break;
		case eQuestObjectType.Inventory: scriptable = prefab.GetComponent<InventoryComponent>().GetData(); break;
		case eQuestObjectType.Dialog: scriptable = prefab.GetComponent<DialogTreeComponent>().GetData(); break;
		case eQuestObjectType.Gui: scriptable = prefab.GetComponent<GuiComponent>().GetData(); break;
		case eQuestObjectType.Prop: scriptable = prefab.GetComponent<PropComponent>().GetData(); break;
		case eQuestObjectType.Hotspot: scriptable = prefab.GetComponent<HotspotComponent>().GetData(); break;
		case eQuestObjectType.Region: scriptable = prefab.GetComponent<RegionComponent>().GetData(); break;
		} 
		
		if ( questType == eQuestObjectType.Room || isRoomChild )
		{			
			// Save scene first incase
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		}
		
		string oldName = scriptable.GetScriptName();
		string oldFileName = scriptable.GetScriptClassName();
		
		// Rename component internal names
		scriptable.EditorRename(newName);		
		EditorUtility.SetDirty(prefab);
		
		// Get new file name from the scritable (ready now ti's been renamed internally)
		string newFileName = scriptable.GetScriptClassName();

		prefab.name = newFileName;
		
		// Rename Prefab (if it's not a hotspot/prop/region)
		if ( isRoomChild == false )
		{
		
			#if UNITY_2018_3_OR_NEWER		
		
			PrefabStage stage = PrefabStageUtility.GetPrefabStage(prefab);
			if ( stage != null)
			{
				#if UNITY_2020_1_OR_NEWER
					prefab = PrefabUtility.SaveAsPrefabAsset(prefab, stage.assetPath);
				#else
					prefab = PrefabUtility.SaveAsPrefabAsset(prefab, stage.prefabAssetPath);
				#endif				
			}
			else 
			{
				PrefabUtility.SavePrefabAsset(prefab);
			}
			#endif


			// Find in directory
			string path = QuestEditorUtils.GetPrefabPath(prefab);
			

			// Rename Files
			//	Rename Folder
			//Debug.Log(Path.GetDirectoryName(path)+'/' + " -> " +   Regex.Replace(Path.GetDirectoryName(path)+'/', oldName+@"/$", newName+'/'));
			Debug.Log("Renaming: "+Path.GetDirectoryName(path) + " -> " +   newName );
			AssetDatabase.RenameAsset( Path.GetDirectoryName(path), newName );
			// Update directory
			path = QuestEditorUtils.GetPrefabPath(prefab);


			if ( questType == eQuestObjectType.Room )
			{			
				// Check any characters that started in that room
				m_powerQuest.GetCharacterPrefabs().ForEach(item=>
					{
						if ( item.GetData().EditorGetRoom() == oldName )
							item.GetData().EditorSetRoom(newName);
					});
			}

			//	Rename file names included in folder, including the prefab itself, and other files (eg: CharacterXXXImporter)
			System.Array.ForEach(Directory.GetFiles( Path.GetDirectoryName(path) ), item =>
				{
					if ( item.Contains(oldFileName) )
						AssetDatabase.RenameAsset( item, Path.GetFileNameWithoutExtension(item).Replace(oldFileName,newFileName) );
				});	
		}

		
		// Find/Replace name in scripts.
		{
			

			string regex = RENAME_SCRIPT_REGEX;
			regex = regex.Replace("<SHORTTYPE>",RENAME_QO_SHORT[(int)questType]);
			regex = regex.Replace("<TYPE>",questType.ToString());
			regex = regex.Replace("<LONGTYPE>",RENAME_QO_LONG[(int)questType]);
			regex = regex.Replace("<SCRIPTNAME>",oldName);
			// Debug.Log(regex);
			Regex regexCompiled = new Regex(regex,RegexOptions.Compiled); // Compile the regex

			// For Props/Hotspots/regions, only do it for their own room script
			if ( isRoomChild )
			{
				// Process that object's room
				RoomComponent component =  null;
				if ( prefab.transform.parent != null )
				{
					GameObject roomPrefab = QuestEditorUtils.GetPrefabParent(prefab.transform.parent.gameObject);
					if ( roomPrefab != null )
						component = roomPrefab.GetComponent<RoomComponent>();
				}
				if ( component != null )
				{
					
					Debug.Log("Replacing in "+component.GetData().GetScriptClassName() +".cs: " + oldName + " -> " + newName);
					RenameQuestObjectInScript( QuestEditorUtils.GetFullPath(component.gameObject, component.GetData().GetScriptClassName() +".cs"), regexCompiled, newName ); 
				}
			}
			else 
			{
				Debug.Log("Replacing in scripts: " + oldName + " -> " + newName);

				// Process ScriptGlobals
				RenameQuestObjectInScript(PowerQuestEditor.PATH_GAME_GLOBALS, regexCompiled, newName);
				// Process Global script
				RenameQuestObjectInScript(PowerQuestEditor.PATH_GLOBAL_SCRIPT, regexCompiled, newName);

				// Process Room scripts
				foreach ( RoomComponent component in m_powerQuest.GetRoomPrefabs() )
				{
					RenameQuestObjectInScript( QuestEditorUtils.GetFullPath(component.gameObject, component.GetData().GetScriptClassName() +".cs"), regexCompiled, newName ); 
				}

				// Process Character scripts
				foreach ( CharacterComponent component in m_powerQuest.GetCharacterPrefabs() )
				{
					RenameQuestObjectInScript( QuestEditorUtils.GetFullPath(component.gameObject, component.GetData().GetScriptClassName() +".cs"), regexCompiled, newName ); 
				}

				// Process Inventory scripts
				foreach ( InventoryComponent component in m_powerQuest.GetInventoryPrefabs() )
				{
					RenameQuestObjectInScript( QuestEditorUtils.GetFullPath(component.gameObject, component.GetData().GetScriptClassName() +".cs"), regexCompiled, newName ); 
				}

				// Process Dialog scripts
				foreach ( DialogTreeComponent component in m_powerQuest.GetDialogTreePrefabs() )
				{
					RenameQuestObjectInScript( QuestEditorUtils.GetFullPath(component.gameObject, component.GetData().GetScriptClassName() +".cs"), regexCompiled, newName ); 
				}
				
				// Process Gui Scripts
				foreach ( GuiComponent component in m_powerQuest.GetGuiPrefabs() )
				{
					RenameQuestObjectInScript( QuestEditorUtils.GetFullPath(component.gameObject, component.GetData().GetScriptClassName() +".cs"), regexCompiled, newName ); 
				}
			}		
		}
		
		Debug.Log("Rename complete");

		AssetDatabase.Refresh();

		PowerQuestEditor powerQuestEditor = EditorWindow.GetWindow<PowerQuestEditor>();
		if ( powerQuestEditor != null ) powerQuestEditor.Repaint();
		
		QuestScriptEditor.UpdateAutoComplete();
	}

	void RenameQuestObjectInScript( string filePath, Regex regex, string replace )
	{
		try 
		{
			string content = File.ReadAllText(filePath);
			content = regex.Replace(content, replace);
			File.WriteAllText( filePath,  content );
		} 
		catch (System.Exception ex) 
		{
			if ( ex is System.IO.FileNotFoundException )
			{
			}
			else 
			{
				Debug.LogWarningFormat("Failed to process text in {0}: {1}",filePath, ex.ToString());
			}
		}
	}

	#endregion
	#region Functions: Delete Quest Objects
	
	bool DeleteQuestObject( int index, string typeName, List<CharacterComponent> prefabs )
	{
		string scriptName = GetQuestObjectToDelete( prefabs, ref index )?.GetData()?.ScriptName ?? null;		
		return DeleteQuestObject( index, prefabs, typeName, scriptName );
	}
	
	bool DeleteQuestObject( int index, string typeName, List<InventoryComponent> prefabs )
	{
		string scriptName = GetQuestObjectToDelete( prefabs, ref index )?.GetData()?.ScriptName ?? null;
		return DeleteQuestObject( index, prefabs, typeName, scriptName );
	}
	
	bool DeleteQuestObject( int index, string typeName, List<RoomComponent> prefabs)
	{		
		string scriptName = GetQuestObjectToDelete( prefabs, ref index )?.GetData()?.ScriptName ?? null;
		string sceneName = GetQuestObjectToDelete( prefabs, ref index )?.GetData()?.GetSceneName() ?? null;
		if ( DeleteQuestObject( index, prefabs, typeName, scriptName ) )
		{				
			// Also unload the scene if it's the active one			
			if ( EditorSceneManager.GetActiveScene().name == sceneName && prefabs.Count > 0 )
				LoadRoomScene(prefabs[0],false);
				
			// Also delete scene from build settings			
			string sceneFileEnd = sceneName+".unity";
			List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
			buildScenes.RemoveAll( buildScene=>string.IsNullOrEmpty(buildScene.path) || buildScene.path.EndsWith(sceneFileEnd));
			EditorBuildSettings.scenes = buildScenes.ToArray();

			return true;
		}

		return false;
	}
	
	bool DeleteQuestObject( int index, string typeName, List<DialogTreeComponent> prefabs )
	{
		string scriptName = GetQuestObjectToDelete( prefabs, ref index )?.GetData()?.ScriptName ?? null;
		return DeleteQuestObject( index, prefabs, typeName, scriptName );
	}
	
	bool DeleteQuestObject( int index, string typeName, List<GuiComponent> prefabs )
	{
		string scriptName = GetQuestObjectToDelete( prefabs, ref index )?.GetData()?.ScriptName ?? null;
		return DeleteQuestObject( index, prefabs, typeName, scriptName );
	}

	// Helpers for deleting items
	T GetQuestObjectToDelete<T>( List<T> components, ref int index ) where T  : class
	{
		// If index < 0, then use the last index
		if ( index < 0 ) index = components.Count-1;
		if ( index < 0 || index >= components.Count )
			return null;
		return components[index];
	}

	bool DeleteTemplateGameQuestObject(string name)
	{
		return EditorUtility.DisplayDialog("Really remove example object?", 
		$"{name} is part of the template game.\n\nIt's recommended to keep these around for reference, or rename them and re-use them.\n\nIf you really want to delete them, make sure you remove all of them (Rooms, Dialogs, and Characters), or you'll get compile errors you'll have to fix yourself ;)",
		"Yeah yeah, I know...", "Cancel");
	}

	bool DeleteQuestObject<T>( int index, List<T> components, string typename, string name) where T : Component
	{
		T component = components[index];
		if ( component == null )
		{
			components.RemoveAt(index);							
			EditorUtility.SetDirty(m_powerQuest);
			return false;
		}
		
		if ( component is CharacterComponent && (name == "Dave" || name == "Barney") )
			if ( DeleteTemplateGameQuestObject(name) == false )
				return false;
		if ( component is RoomComponent && (name == "Title" || name == "Forest") )
			if ( DeleteTemplateGameQuestObject(name) == false )
				return false;
		if ( component is DialogTreeComponent && (name == "ChatWithBarney") )
			if ( DeleteTemplateGameQuestObject(name) == false )
				return false;
		if ( component is InventoryComponent && (name == "Bucket") )
			if ( DeleteTemplateGameQuestObject(name) == false )
				return false;

		if ( EditorUtility.DisplayDialog("Really Remove?", "Yo, you sure you wanna remove "+name+"?\n\nThis can't be undone.", "Yeah yeah", "Hmm, Nah") == false )
			return false;
		
		// Undo.RecordObject(m_powerQuest, "Remove "+name); // Can't remove script changes, so don't undo

		// Remove line from script
		QuestEditorUtils.RemoveLineFromFile( PATH_GAME_GLOBALS, typename, name);
		
		components.RemoveAt(index);		
					
		EditorUtility.SetDirty(m_powerQuest);
		

		// Delete from script file
		if ( EditorUtility.DisplayDialog("Delete files as well?", string.Format("Also delete all files for {0}?\n\nThis can't be undone either.",name), "Yes, delete them", "NO!") == false )
			return false;

		EditorUtility.DisplayProgressBar("Deleting...","Removing asset",0.1f);

		// So be it - delete directory containing the file
		// Find in room directory
		string path = AssetDatabase.GetAssetPath(component.gameObject);
		AssetDatabase.MoveAssetToTrash(Path.GetDirectoryName(path));
		
		EditorUtility.DisplayProgressBar("Deleting...","Refreshing Asset Database",0.5f);
				
		PowerQuestEditor powerQuestEditor = EditorWindow.GetWindow<PowerQuestEditor>();
		if ( powerQuestEditor != null ) powerQuestEditor.Repaint();
		
		// Fix: If we don't force a refresh here, if you add another questobject after deleting, when the asset database refreshes it deletes the new one from the list! So we have to force a refresh intead			
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); //RequestAssetRefresh();	

		EditorUtility.ClearProgressBar();	
		return true;
	}

	#endregion
	#region Functions: Public Utilities

	//
	// Public utility functions (NB: Look in QuestEditorUtils if it's not here
	//


	public static bool IsOpen() { return PowerQuestEditor.m_instance != null; }
	public static bool IsReady() { return PowerQuestEditor.m_instance != null && PowerQuestEditor.m_instance.m_powerQuest != null; }

	// Gets the instance of the PowerQuestEditor if ready
	public static PowerQuestEditor Get { get{ return m_instance; } }
	public static PowerQuestEditor GetPowerQuestEditor() { return m_instance; }
	public SystemAudio GetSystemAudio() { return m_instance?.m_systemAudio; }
	
	// Gets the powerquest prefab object if ready
	public static PowerQuest GetPowerQuest() { return IsReady() ? m_instance.m_powerQuest : null; }

	public static QuestEditorSettings EditorSettings => QuestEditorSettings.Get;

	// Returns selected room
	public RoomComponent GetSelectedRoom() { return m_selectedRoom; }

	// Returns PowerQuestEditor and prints errors if it can't access it
	public static PowerQuestEditor OpenPowerQuestEditor()
	{
		if ( IsOpen() )
			return GetPowerQuestEditor();

		// Make sure we can find PowerQuest
		PowerQuestEditor powerQuestEditor = EditorWindow.GetWindow<PowerQuestEditor>();
		return powerQuestEditor;
	}

	/* // this was too dangerous, kept accidentally opening new copies of the eidtor, or at very least, focusing it when  not intended
	// Returns PowerQuest from the PowerQuest window, and prints errors if it can't access it
	public static PowerQuest FindPowerQuest()
	{
		// Make sure we can find PowerQuest
		if ( FindPowerQuestEditor() )
		{
			if ( PowerQuestEditor.GetPowerQuest() == null )
			{				
				Debug.LogError("Ensure PowerQuest exists in PowerQuestEditor first");
				return null;			
			}
			return PowerQuestEditor.GetPowerQuest();
		}
		return null;
	}*/

	public static bool GetActionEnabled(eQuestVerb action)
	{
		return IsReady() ? GetPowerQuest().GetActionEnabled(action) : false;
	}

	public RoomComponent GetRoom(string roomName) 
	{ 
		if ( IsReady() )
			return GetPowerQuest().GetRoomPrefabs().Find(item=>item.GetData().ScriptName == roomName);
		return null;
	}
	public GuiComponent GetGui(string roomName) 
	{ 
		if ( IsReady() )
			return GetPowerQuest().GetGuiPrefabs().Find(item=>item.GetData().ScriptName == roomName);
		return null;
	}

	public DialogTreeComponent GetDialogTree(string name) 
	{ 
		if ( IsReady() )
			return GetPowerQuest().GetDialogTreePrefabs().Find(item=>item.GetData().ScriptName == name);
		return null;
	}

	// Return snap amount if able to access powerquest, otherwise 0
	public static float SnapAmount { get {
			if ( IsReady() )
				return GetPowerQuest().SnapAmount;
			return 0.0f;
	} }

	// Return snap if able to access powerquest, otherwise true
	public static bool Snap { get {
			if ( IsReady() )
				return GetPowerQuest().SnapAmount > 0;
			return true;
	} }


	public static void LoadRoomScene(RoomComponent room, bool askToSave = true)
	{
		// Load the scene, or if the game's being played, change rooms
		if ( Application.isPlaying )
		{
			// Get powerquest in the scene and call change room
			if ( PowerQuest.Exists )
				PowerQuest.Get.StartRoomTransition( room.GetData(), true );			
			//if ( PowerQuest.Exists )
			// PowerQuest.Get.ChangeRoomBG(room.GetData());
		}
		else if ( askToSave == false || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() )
		{			
			string scenePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(room.gameObject)) + "/" + room.GetData().GetSceneName()+".unity";
			EditorSceneManager.OpenScene(scenePath);
		}
	}

	public static void AddSceneToBuildSettings(string path)
	{
		if ( System.Array.Exists(EditorBuildSettings.scenes, item=>item.path == path ) == false )
		{			
			EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[EditorBuildSettings.scenes.Length+1];
			if (  EditorBuildSettings.scenes.Length > 0 )
			{
				System.Array.Copy(EditorBuildSettings.scenes,scenes,EditorBuildSettings.scenes.Length);
			}
			scenes[scenes.Length-1] = new EditorBuildSettingsScene(path,true);
			EditorBuildSettings.scenes = scenes;
		}
	}

	/// Sanitizes the passed in object name, checks it against existing types. Returns FALSE if name can't be sanitized.
	public bool SanitizeQuestObjectName(eQuestObjectType questType, ref string name, out string error )
	{

		error = string.Empty;
		
		if ( name.Length == 0 )
		{
			error = "Type a name";
			return false;
		}
		name = name.Trim();	
		// First letter to upper		
		name = name[0].ToString().ToUpper() + name.Substring(1);
		// Check it's valid
		if ( Regex.IsMatch(name,@"^[A-Z][\w]*$") == false )
		{
			error = questType.ToString()+" names must be single words, with no special characters";
			return false;
		}
		if ( questType == eQuestObjectType.Character && Regex.IsMatch(name,@"\d$") )
		{
			error = "Character names must not end in a number.";
			return false;			
		}
		
		// Check it's not already in use
		List<IQuestScriptable> scriptables = GetQuestScriptables(questType);
		string checkName = name;
		if ( scriptables != null && scriptables.Exists(item=>item.GetScriptName().Equals(checkName,System.StringComparison.OrdinalIgnoreCase)) )
		{
			error = "The name "+name+" is already in use!";
			return false;
		}

		return true;
	}
	/// Sanitizes the passed in object name, checks it against existing types. Returns FALSE if name can't be sanitized, and shows a popup-box
	public bool SanitizeQuestObjectNameWithError(eQuestObjectType questType, ref string name )
	{
		string error;
		bool result = SanitizeQuestObjectName(questType, ref name, out error);
		if ( result == false)
		{
			EditorUtility.DisplayDialog("Invalid name", error, "Ok");
		}
		return result;
	}

	/// Creates list of all clickables of certain type. Returns room objects from the passed inRoom, or the current room if not set
	public List<IQuestScriptable> GetQuestScriptables( eQuestObjectType type, Room inRoom = null )
	{		
		if ( inRoom == null )
			inRoom = m_selectedRoom?.GetData();
		switch (type)
		{
		case eQuestObjectType.Room: return m_powerQuest?.GetRoomPrefabs().ConvertAll<IQuestScriptable>(item=>item.GetData());
		case eQuestObjectType.Character: return m_powerQuest?.GetCharacterPrefabs().ConvertAll<IQuestScriptable>(item=>item.GetData());
		case eQuestObjectType.Inventory: return m_powerQuest?.GetInventoryPrefabs().ConvertAll<IQuestScriptable>(item=>item.GetData());
		case eQuestObjectType.Dialog: return m_powerQuest?.GetDialogTreePrefabs().ConvertAll<IQuestScriptable>(item=>item.GetData());
		case eQuestObjectType.Gui: return m_powerQuest?.GetGuiPrefabs().ConvertAll<IQuestScriptable>(item=>item.GetData());
		case eQuestObjectType.Prop: return inRoom?.GetProps_SaveFlagNotDirtied().ConvertAll<IQuestScriptable>(item=>item);
		case eQuestObjectType.Hotspot: return inRoom?.GetHotspots_SaveFlagNotDirtied().ConvertAll<IQuestScriptable>(item=>item);
		case eQuestObjectType.Region: return inRoom?.GetRegions_SaveFlagNotDirtied().ConvertAll<IQuestScriptable>(item=>item);
		}
		return null;
	}


	#endregion
	#region Functions: Private

	// Call when something is selected in the editor to reset any things being edited in the background, like walkable area polys, or points
	void UnselectSceneTools()
	{
		// Reset any walkable polygon being edited
		if ( QuestPolyTool.Active() && QuestPolyTool.Active(Selection.activeGameObject) == false )
			QuestPolyTool.Hide();
		
		// Reset any point being editied
		m_selectedRoomPoint = -1;
	}

	// Quick version converter function
	static int Version(int full, int major, int minor) { return full*1000000 + major*1000 + minor; }
	static string Version(int version) { return string.Format("{0}.{1}.{2}", (version/1000000), ((version%1000000)/1000), (version%1000)); }
	static int Version(string versionString)
	{
		Match match = Regex.Match(versionString, @"^(\d*)\.(\d*)\.(\d*)");
		if ( match.Success && match.Groups.Count == 4)
			return Version(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value) );
		return -1;
	}
	
	// Does an AssetDatabase.Refresh(), unless set to smart in which case sets Smart Refresh Required flag
	public void RequestAssetRefresh()
	{
		if ( EditorSettings.m_smartCompile )
		{
			m_smartCompileRequired = true;
			EditorPrefs.SetBool("kAutoRefresh", false);
		}
		else
			AssetDatabase.Refresh();
	}
	
	public bool GetSmartCompileRequired() { return EditorSettings.m_smartCompile && m_smartCompileRequired; }
	public bool GetSmartCompileEnabled() { return EditorSettings.m_smartCompile; }
	public void SetSmartCompileEnabled(bool enabled)
	{
		if ( enabled == false ) 
		{	
			EditorPrefs.SetBool("kAutoRefresh", true);
			AssetDatabase.Refresh();
		}
		EditorSettings.m_smartCompile = enabled;
		m_smartCompileRequired = false;
	}
	public void PerformSmartCompile()
	{
		if ( EditorSettings.m_smartCompile == false )
			return;
			
		EditorPrefs.SetBool("kAutoRefresh", true);
		AssetDatabase.Refresh(); 
		m_smartCompileRequired = false;
	}


	// Get/Set spell check settings	
	public bool SpellCheckEnabled 
	{ 
		get {return EditorSettings.m_spellCheckEnabled;} 
		set 
		{ 
			if ( EditorSettings.m_spellCheckEnabled == value) 
				return;
			EditorSettings.m_spellCheckEnabled = value;  
			QuestScriptEditor.InitSpellCheck(true); 
		} }
	public List<string> SpellCheckIgnoredWords { get {return EditorSettings.m_spellCheckIgnoredWords;} }
	public string SpellCheckDictionaryPath 
	{ 
		get {return EditorSettings.m_spellCheckDictionaryPath;} 
		set 
		{			
			if ( EditorSettings.m_spellCheckDictionaryPath.Equals(value) ) 
				return;
			EditorSettings.m_spellCheckDictionaryPath = value; 
			QuestScriptEditor.InitSpellCheck(true); 
		} }

	
	void OnPlaymodeStateChanged(PlayModeStateChange stateChange)
	{
		bool startedPlaying = false;
		bool stoppedPlaying = false;

		m_sourceModifiedTime = System.DateTime.Now; 
		
		startedPlaying = (stateChange == PlayModeStateChange.ExitingEditMode);
		stoppedPlaying = (stateChange == PlayModeStateChange.EnteredEditMode);

		if ( startedPlaying )
		{
			// About to start playing - compile before play
			PerformSmartCompile();				

			// Apply room changes
			ApplySelectedRoomInstance();

			// Turn off auto-import while playing
			EditorPrefs.SetBool("kAutoRefresh", false);
			//Application.logMessageReceived += OnLogMessageReceived; // now always receiving these
		}

		if ( stoppedPlaying )
		{
			// Stopped playing

			// Clear and update selected room
			m_selectedRoom = null;
			UpdateRoomSelection(GameObject.FindObjectOfType<RoomComponent>(), true); 				

			// Re-enable auto-import while playing
			EditorPrefs.SetBool("kAutoRefresh", true);

			PerformSmartCompile(); // In case there were changes while play mode.
		}
	}


	static void OnLogMessageReceived(string message, string stackTrace, LogType logType)
	{
		
		// Fix for pressing play when there's a compile error causing "auto refresh" to remain disabled.
		// This event is registered when user clicks "play" and if there's an error and we're not playing, we ensure autorefresh is enabled again
		if ( logType == LogType.Error && EditorApplication.isPlaying == false )
		{
			EditorPrefs.SetBool("kAutoRefresh", true); // turn on auto-refresh when there's an error in console
			// PowerQuestEditor.Get.RequestSmartAssetRefresh(); // don't 
			//Application.logMessageReceived -= OnLogMessageReceived;			
		}

		// TODO: If in open quest script, open it?		
	}

	PowerTools.SpriteAnimator m_animatorWindow = null;

	void Update()
	{
		UpdateCheckInitialSetup();

		if ( Application.isPlaying && PowerQuest.GetValid() && PowerQuest.Get.GetRegainedFocus() )
		{
			// Must have focus again i guess...?
			HotloadScripts();
			PowerQuest.Get.GetRegainedFocus(); // to reset again
		}

		UpdateQuestEditorTools();
		CheckVersion();

		// Register with animator window for anim tag callbacks
		if ( m_animatorWindow == null && EditorWindow.HasOpenInstances<SpriteAnimator>() )
		{
			m_animatorWindow = (SpriteAnimator)EditorWindow.GetWindow(typeof(SpriteAnimator),false,null,false); //EditorWindow.GetWindow<SpriteAnimator>();
		}		
		else if ( m_animatorWindow != null && EditorWindow.HasOpenInstances<SpriteAnimator>() == false )
		{
			//Debug.Log("Removed editor");
			//m_animatorWindow.CallbackOnAnimEvent -= OnAnimatorWindowEvent;
			m_animatorWindow = null;
		}

		// Register callback with animator window once found
		if ( m_animatorWindow != null && m_animatorWindow.CallbackOnAnimEvent == null ) // add event if window crea
		{
			m_animatorWindow.CallbackOnAnimEvent += OnAnimatorWindowEvent;
		}
				
		if ( m_hotloadAssemblyOnUnblock != null )
		{ 
			HotloadAssemblyOnUnblock();
		}
	}

	#endregion
	#region Functions: Messages

	//
	// Messages
	//


	void OnEnable()  
	{
		if ( m_registered == false )
		{
			
			#if UNITY_2019_1_OR_NEWER
				SceneView.duringSceneGui += OnScene;
			#else
				SceneView.onSceneGUIDelegate += OnScene;
			#endif

			EditorApplication.update += OnUpdate;
			PrefabStage.prefabStageOpened += OnPrefabStaged;
			PrefabStage.prefabStageClosing += OnPrefabUnstaged;
			
			#if UNITY_2017_1_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
			#else
			EditorApplication.playmodeStateChanged +=  OnPlaymodeStateChanged;
			#endif
			EditorSceneManager.sceneSaving += OnSceneSaving;

			Application.logMessageReceived += OnLogMessageReceived;
		
		}
		m_registered = true;
		m_instance = this;

		if ( m_powerQuest == null )
		{
			GameObject obj = AssetDatabase.LoadAssetAtPath(m_powerQuestPath, typeof(GameObject)) as GameObject;
			if ( obj != null )
				m_powerQuest = obj.GetComponent<PowerQuest>();
		}
		if ( m_powerQuest != null )
		{
			OnFoundPowerQuest();

			m_selectedRoom = null;
			UpdateRoomSelection(GameObject.FindObjectOfType<RoomComponent>(), true); 
			UpdateGuiSelectionFromStage();
		}

	}

	void OnSceneSaving(Scene scene, string path)
	{
		// Before scene is saved, apply room changes to the prefab
		ApplySelectedRoomInstance();
	}

	void OnSelectionChange()
	{
		if ( Selection.activeGameObject != null )
		{			
			UnselectSceneTools();
		}

		if ( Selection.activeGameObject == null )
			return;

		/*
		if ( Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<GuiComponent>() != null )
			UpdateGuiSelection( Selection.activeGameObject.GetComponent<GuiComponent>() );
		*/
		/*			
		if ( Selection.activeGameObject != null && PrefabUtility.GetPrefabType(Selection.activeGameObject) != PrefabType.PrefabInstance && Selection.activeGameObject.GetComponent<RoomComponent>() != null )
		{	
			UpdateRoomSelection( Selection.activeGameObject.GetComponent<RoomComponent>() );
		}*/

	}

	void OnPrefabStaged(PrefabStage stage)
	{		
		GuiComponent comp =  stage.prefabContentsRoot.GetComponent<GuiComponent>();
		if ( comp != null )
		{
			UpdateGuiSelection( comp, true );
			m_selectedTab = eTab.Gui;
		}
	}
	void OnPrefabUnstaged(PrefabStage stage)
	{
		if ( m_selectedTab == eTab.Gui && (m_selectedGui == null || stage.prefabContentsRoot == m_selectedGui.gameObject) )
		{
			m_selectedTab = eTab.Main;
			m_selectedGui = null;
			Repaint(); 
		}
	}
	
	void OnUpdate()
	{		
		// Check mouse position when game is running. Unfortunately, this doesn't work unless game is running.
		if ( Application.isPlaying && Camera.main != null )
		{
			Vector2 mousePos = Input.mousePosition;
			mousePos = Camera.main.ScreenToViewportPoint(mousePos);
			if ( mousePos.x >= 0 && mousePos.x <= 1 && mousePos.y >= 0 && mousePos.y <= 1 )
			{
				mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				if ( m_mousePos != mousePos )
				{
					m_mousePos = mousePos;				
					if ( m_selectedTab == eTab.Room )
						PowerQuestEditor.Get.Repaint();
				}
			}
		}
	}

	protected virtual void OnScene(SceneView sceneView)
	{
		if ( Event.current != null && Event.current.type == EventType.MouseMove )
			m_mousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;

		OnSceneRoom(sceneView);
		OnSceneGui(sceneView);

		if ( s_editorExtensions != null )
		{
			foreach(  QuestEditorExtension extension in s_editorExtensions )
			{
				if ( extension != null )
					extension.OnScene(this,sceneView);
			}
		}

	} 

	void OnDestroy()
	{
		if ( m_registered )
		{		    
		
			#if UNITY_2019_1_OR_NEWER
				SceneView.duringSceneGui -= OnScene;
			#else
				SceneView.onSceneGUIDelegate -= OnScene;
			#endif
			
			EditorApplication.update -= OnUpdate;			
			PrefabStage.prefabStageOpened -= OnPrefabStaged;
			PrefabStage.prefabStageClosing -= OnPrefabUnstaged;
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged; // TODO: check this isn't supposed to be '-='
			
			EditorSceneManager.sceneSaving -= OnSceneSaving;
			
			Application.logMessageReceived -= OnLogMessageReceived;

			m_registered = false;
			//Debug.Log("PowerQuest OnDestroy"); // Probably because playing game fullscreen
			EditorPrefs.SetBool("kAutoRefresh", true);
		}
	}

	
	public void CreateTempAudioSystem()
	{
		if ( SystemAudio.HasInstance() && SystemAudio.Get.EditorGetAudioCues().Count > 0 )
			return;
		if ( SystemAudio.HasInstance() )
			GameObject.DestroyImmediate(SystemAudio.Get.gameObject); // Remove non-PQ audio cue
		
		GameObject systemAudio = GameObject.Instantiate(m_systemAudio.gameObject) as GameObject;
		systemAudio.hideFlags = HideFlags.HideAndDontSave;
	
	}

	partial void exOnAnimatorWindowEvent( SpriteAnimator.AnimEventData ev );
	void OnAnimatorWindowEvent( SpriteAnimator.AnimEventData ev )
	{
		if ( m_powerQuest == null )
			return;

		//Debug.Log(ev.m_functionName);
		if (string.Equals(ev.m_functionName, "Sound", System.StringComparison.OrdinalIgnoreCase) )
		{
			if ( ev.m_paramType == SpriteAnimator.eAnimEventParameter.String )
			{
				CreateTempAudioSystem();
				SystemAudio.Play(ev.m_paramString);
			}
			else if ( ev.m_paramType == SpriteAnimator.eAnimEventParameter.Object )
			{
				if ( ev.m_paramObjectReference == null )
					return;				
				AudioCue cue =  (ev.m_paramObjectReference as GameObject).GetComponent<AudioCue>();
				if ( cue )
				{
					CreateTempAudioSystem();
					SystemAudio.Play(cue);
				}

			}
		}
		else if (string.Equals(ev.m_functionName, "SoundStop", System.StringComparison.OrdinalIgnoreCase) )
		{
			if ( ev.m_paramType == SpriteAnimator.eAnimEventParameter.String )
			{
				CreateTempAudioSystem();
				SystemAudio.Stop(ev.m_paramString);
			}
			else if ( ev.m_paramType == SpriteAnimator.eAnimEventParameter.Object )
			{
				if ( ev.m_paramObjectReference == null )
					return;				
				GameObject cue =  ev.m_paramObjectReference as GameObject;
				if ( cue )
				{
					CreateTempAudioSystem();
					SystemAudio.Stop(cue.name);
				}

			}
		}
		else if ( string.Equals(ev.m_functionName, "Footstep", System.StringComparison.OrdinalIgnoreCase) && m_listCharacterPrefabs.Count > 0)
		{	
			CreateTempAudioSystem();			
			SystemAudio.Play(m_listCharacterPrefabs[0].GetData().FootstepSound);
		}
		//Debug.Log(ev.m_functionName);

		// For extentions
		exOnAnimatorWindowEvent(ev);
	}	

	//
	// GUI Layout
	//
	#endregion
	#region Gui Layout: Main

	void OnGUI()
	{
		titleContent.text = "PowerQuest";
		/*
		#if UNITY_2018_3_OR_NEWER
			GUILayout.Space(20);
			GUILayout.Label("Unity Version Not Supported!", EditorStyles.boldLabel );
			EditorGUILayout.HelpBox("Unity versions 2018.3 and later are not supported yet, sorry!", MessageType.Error);
			EditorGUILayout.HelpBox("Unity 5.6 is the most stable. Up to 2018.2 has been tested.\n\nI recommend using LTS releases for stability - https://unity3d.com/unity/qa/lts-releases.", MessageType.Info);
			LayoutManual();
			return;			
		#endif
		*/
		if ( m_powerQuest == null )
		{
			OnGuiInitialSetup();
			return;
		} 
		
		string[] tabstrings = new string[] { "Main", "Room", "Gui", "Tools" };
		eTab prevTab = m_selectedTab;
		if ( m_selectedGui == null && m_selectedTab == eTab.Gui )
			m_selectedTab = eTab.Main;
		m_selectedTab = Tabs( tabstrings, m_selectedTab );
		bool tabChanged = prevTab != m_selectedTab;

		// Update room selection.
		if ( m_selectedRoom == null || tabChanged || m_listHotspots == null || m_listProps == null || m_listRegions == null || m_listWalkableAreas == null )
			UpdateRoomSelection( GameObject.FindObjectOfType<RoomComponent>() );
				
		if ( m_selectedTab == eTab.Main )
		{
			OnGuiMain();
		}
		else if ( m_selectedTab == eTab.Room )
		{
			OnGuiRoom( tabChanged );
		}
		else if ( m_selectedTab == eTab.Gui)
		{
			OnGuiGui( tabChanged );
		}
		else if ( m_selectedTab == eTab.Tools )
		{
			OnGuiTools();
		}
	}




	#endregion
	#region Tools



	//
	// Tool for instantly searching PQ window
	//
	[MenuItem("Edit/PowerQuest/Quick Search %Q")] // %ctrl #shift &alt
	//[MenuItem("Edit/PowerQuest/Jump to (Search ")]
	public static void FocusSearchBox()
	{		
		PowerQuestEditor.Get.m_selectedTab = eTab.Main;		
		PowerQuestEditor.Get.m_focusSearch=true;
		PowerQuestEditor.Get.Focus();				
	}

	//
	// Tool for hot-loading scripts while running
	//
	
	[MenuItem("Edit/PowerQuest/Hotload scripts %F7")]
	public static void HotloadScriptsCmd()
	{
		PowerQuestEditor window = GetWindow<PowerQuestEditor>();
		if ( window != null ) 
		{
			window.HotloadScripts();
		}
	}

	Assembly m_hotloadAssemblyOnUnblock = null;

	// If recompiled while game was in blocking state, it waits until no longer blocking until now to swap stuff out
	void HotloadAssemblyOnUnblock()
	{ 		
		if ( Application.isPlaying == false || PowerQuest.GetValid() == false || m_hotloadAssemblyOnUnblock == null || PowerQuest.Get.GetBlocked())
			return;
			
		List<IQuestScriptable> scriptables = PowerQuest.Get.GetAllScriptables();
		List<IQuestScriptable> hotloadedScriptables = scriptables;
		
		foreach ( IQuestScriptable scriptable in hotloadedScriptables )
		{
			// Call through to scriptables to set them 
			scriptable.HotLoadScript(m_hotloadAssemblyOnUnblock);
		}
		PowerQuest.Get.EditorSetHotLoadAssembly(m_hotloadAssemblyOnUnblock);

		m_hotloadAssemblyOnUnblock = null;
	}

	// Recompiles any scripts that have changed and hot-loads them. Returns true if anything changed.
	bool HotloadScripts()
	{
		bool hotloadedSomething = false;
		if ( Application.isPlaying == false || m_sourceModifiedTime == null || PowerQuest.GetValid() == false/* || PowerQuest.Get.GetBlocked() */)
			return hotloadedSomething;
	
		System.DateTime sourceModifiedTime = m_sourceModifiedTime.Value;

		List<IQuestScriptable> scriptables = PowerQuest.Get.GetAllScriptables(); //  Dont' use internal version of m_powerQuest- since we're hot swapping in the actual running game

		/* // We're now just compiling ALL scriptables */
		List<IQuestScriptable> hotloadedScriptables = scriptables;
		/*/ // no longer only trying to recompile changed scripts, since they need to reference each other. (unless can find way to compile new object in existing assembly)
		List<IQuestScriptable> hotloadedScriptables = PowerQuest.Get.EditorGetHotloadedScriptables();
		if ( hotloadedScriptables == null )
			hotloadedScriptables = new List<IQuestScriptable>();
		/**/

		bool needsCompile = false;
		// Get all assets in script folder. NB: We used to call FindAssets for each scriptable in turn, but it's mega slow once you get lots of scriptables (was taking 1.5 seconds in drifter)
		string[] assets = AssetDatabase.FindAssets(STR_SCRIPT_TYPE,STR_SCRIPT_FOLDERS);			
		// loop through and check if any changed
		for ( int i = 0; i < assets.Length; ++i )
		{
			string path = AssetDatabase.GUIDToAssetPath(assets[i]);
			if ( File.Exists(path) && File.GetLastWriteTime(path) > sourceModifiedTime )
			{
				//Debug.Log($"Rebuilding because of {path}. Changed at {sourceModifiedTime}");
				needsCompile = true;
				/* // We're now just compiling ALL scriptables */
				break;
				/*/
				hotloadedScriptables.Add(scriptable);
				/**/
			}
		}
		
		
		// Check we actually have anything that needs compiling
		if ( needsCompile == false )
			return false;
				
		EditorUtility.DisplayProgressBar("Compiling","Finding Scripts",0.0f);

		// Find all the paths we want to hot-load	
		List<string> hotLoadPaths = new List<string>();
		
		for ( int i = 0; i < assets.Length; ++i )
		{
			string hotloadPath = AssetDatabase.GUIDToAssetPath(assets[i]);			
			if ( hotLoadPaths.Contains(hotloadPath) == false && hotloadedScriptables.Exists(item=>hotloadPath.Contains(item.GetScriptClassName())) )
				hotLoadPaths.Add(hotloadPath);
		}

		// Hack- Also add the GlobalScriptBase file. Later I should really move features from GlobalScriptBase to the PowerQuest system
		hotLoadPaths.Add("Assets/PowerQuest/Scripts/PowerQuest/GlobalScriptBase.cs");

		// Compile
		Assembly assembly = null;
		try
		{
			EditorUtility.DisplayProgressBar("Compiling","Compiling Scripts",0.2f);//, (float)i/(float)scriptables.Count);
			assembly = QuestEditorUtils.CompileFiles(hotLoadPaths.ToArray());

			if ( PowerQuest.Get.GetBlocked() )
			{ 
				m_hotloadAssemblyOnUnblock = assembly;
			}
			else 
			{ 
				EditorUtility.DisplayProgressBar("Compiling","Hotloading Scripts",0.9f);//, (float)i/(float)scriptables.Count);
				foreach ( IQuestScriptable scriptable in hotloadedScriptables )
				{
					// Call through to scriptables to set them 
					scriptable.HotLoadScript(assembly);
				}

				PowerQuest.Get.EditorSetHotLoadAssembly(assembly/*, hotloadedScriptables*/);
			}
			m_sourceModifiedTime = System.DateTime.Now; 
			hotloadedSomething = true;
		}
		catch //(System.Exception ex)
		{			
			// Errors now handled in the compile function so can double click to find line
			// Debug.LogError(ex.Message);
		}
		finally
		{
			EditorUtility.ClearProgressBar();
		}

		return hotloadedSomething;
	}


	//
	// Tool for copying position to clipboard
	//

	[MenuItem("Edit/PowerQuest/Copy Cursor Position To Clipboard %m")]
	static void CopyPositionToClipboard()
	{
		Vector2 diff = m_mousePosPrev - m_mousePos;
		float dist = diff.magnitude;
		m_mousePosPrev = m_mousePos;
		m_mousePosCopied = Mathf.RoundToInt(m_mousePos.x).ToString() +", " + Mathf.RoundToInt(m_mousePos.y).ToString();
		EditorGUIUtility.systemCopyBuffer = m_mousePosCopied;
		//System.Windows.Clipboard.SetText( Mathf.RoundToInt(m_mousePos.x).ToString() +", " + Mathf.RoundToInt(m_mousePos.y).ToString())
		Debug.Log($"Copied coords to clipboard: {m_mousePosCopied} - Offset from previous: {diff}, distance: {dist}");
		PowerQuestEditor window = GetWindow<PowerQuestEditor>();
		if ( window != null ) 
		{
			window.Repaint();
		}
	}
	
	//! Highlight for existing functions
	
	//
	// Highlighting buttons for existing methods
	//	
	Dictionary<string, HashSet<string>> m_buttonHighlightClassMethodsCache = new Dictionary<string, HashSet<string>>();
	Dictionary<string, bool> m_buttonHighlightCheckResultsCache = new Dictionary<string, bool>();
	Color m_guiColorBeforeButtonHighlight = Color.white;
	readonly Color m_buttonHighlightGuiColor = Color.white;//new Color(0.67f, 1f, 0.72f);
	HashSet<string> m_buttonHighlightClassMethods = null;
	string m_buttonHighlightClassName = string.Empty;

	public void BeginHighlightingMethodButtons(IQuestScriptable methodsOwner)
	{
		m_guiColorBeforeButtonHighlight = GUI.color;
		
		if ( methodsOwner == null )
			return;

		string className = methodsOwner.GetScriptClassName();
		m_buttonHighlightClassName = className;
		// For resetting after
		
		if (!m_buttonHighlightClassMethodsCache.TryGetValue(className, out m_buttonHighlightClassMethods))
		{
			Type buttonHighlightClassType = Type.GetType($"{className}, Assembly-CSharp");
			MethodInfo[] methods = 
				buttonHighlightClassType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (methods != null)
			{
				m_buttonHighlightClassMethods = new HashSet<string>(); // removed linq here 				
				foreach ( MethodInfo item in methods )
					m_buttonHighlightClassMethods.Add(item.Name);
			}
			m_buttonHighlightClassMethodsCache[className] = m_buttonHighlightClassMethods;
		}
	}

	public bool HighlightMethodButton(string methodName)
	{
		bool methodExists;

		string methodUniqueName = $"{m_buttonHighlightClassName}.{methodName}";
		if (!m_buttonHighlightCheckResultsCache.TryGetValue(methodUniqueName, out methodExists))
		{
			// method hasn't been cached, so check if it exists
			methodExists = m_buttonHighlightClassMethods != null && 
			               m_buttonHighlightClassMethods.Contains(methodName);
			m_buttonHighlightCheckResultsCache[methodUniqueName] = methodExists;
		}
 
		GUI.color = methodExists
			? EditorSettings.m_scriptBtnUsedCol 
			: EditorSettings.m_scriptBtnUnusedCol ;//m_guiColorBeforeButtonHighlight;

		return methodExists;
	}

	public void EndHighlightingMethodButtons()
	{
		GUI.color = m_guiColorBeforeButtonHighlight;
	}

	//
	// Creates tab style layout
	//
	eTab Tabs(string[] options, eTab selected)
	{
		const float DarkGray = 0.6f;
		const float LightGray = 0.9f;
		const float StartSpace = 5;
	 
		GUILayout.Space(StartSpace);
		Color storeColor = GUI.backgroundColor;
		Color highlightCol = new Color(LightGray, LightGray, LightGray);
		Color bgCol = new Color(DarkGray, DarkGray, DarkGray);
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.padding.bottom = 8;
		buttonStyle.margin.left = 0;
		buttonStyle.margin.right = 0;
	 
		GUILayout.BeginHorizontal();
		{   //Create a row of buttons
			for (eTab i = 0; i < eTab.Count; ++i)
			{
				if ( m_selectedGui == null && i == eTab.Gui )
					continue; // Don't show gui tab if no gui's selected

				GUI.backgroundColor = i == selected ? highlightCol : bgCol;
				if (GUILayout.Button(options[(int)i], buttonStyle))
				{
					selected = i; //Tab click
				}
			}
		} GUILayout.EndHorizontal();
		//Restore color
		GUI.backgroundColor = storeColor;	 
		return selected;
	}
	

	public void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		
		if ( m_powerQuest == null )
			return;

		//QuestUtils.StopwatchStart();
				
		/*/ Update PowerQuest prefab to stop weird errors where list items go missing when the prefab has changed outside unity while unity's open. But don't think we want to do this all the time. Needs looking at more closely.
		if ( PowerQuestAssetPostProcessor.HasPostProcessed("PowerQuest.prefab")  )
		{
			Debug.Log("Refreshing PowerQuest prefab on post process all assets...");
			GameObject obj = AssetDatabase.LoadAssetAtPath(m_powerQuestPath, typeof(GameObject)) as GameObject;
			if ( obj != null )
				m_powerQuest = obj.GetComponent<PowerQuest>();
		}
		/**/
		
			
		/* Tried removing this since not sure what it actually does anymore. maybe fix for when the "room" would get deselected sometimes?? not sure...
		// Update room selection. TODO: Work out when this is actually necessary. Only if current room changed?
		if ( m_selectedRoom == null || m_selectedRoom.gameObject.activeInHierarchy == false )
		{
	  		m_selectedRoom = null;
			if ( PowerQuestAssetPostProcessor.HasPostProcessed("PowerQuest.prefab") || PowerQuestAssetPostProcessor.HasPostProcessed("Room") )
			{
				if ( m_selectedRoom.gameObject.activeInHierarchy == false )
					UpdateRoomSelection(GameObject.FindObjectOfType<RoomComponent>(), true);  // NB: this find is expensive when importing lots of sprites
			}
		}
		*/
		
		UpdateGuiSelectionFromStage();
		// UpdateGuiSelection(GameObject.FindObjectOfType<GuiComponent>(), true );
		PostProcessAnimationLists(); 		
		PostProcessQuestObjectLists();		
		PostProcessAudioCueLists();

		//QuestUtils.StopwatchStop("OnPostProcessAllAssets: ");
		//Debug.Log("OnPostProcessAllAssets");

		// Clear out any "null" quest objects from the lists.
		// This seemed to be erroneously be deleting things when I revert a character, etc. So I moved it here from UpdateCheckInitialSetup		
		/*// It was still erroneously deleting things, and it doestn' seem necessary anyway?
		if ( m_powerQuest != null && m_powerQuest.GetSerializationComplete()  )
		{			
			
			// Clear deleted rooms/inventory/gui/characters
			for (int i = m_powerQuest.GetCharacterPrefabs().Count - 1; i >= 0; i--) 
			{
				if ( m_powerQuest.GetCharacterPrefabs()[i] == null )
				{
					m_powerQuest.GetCharacterPrefabs().RemoveAt(i);				
					EditorUtility.SetDirty(m_powerQuest);
				}
			}
			for (int i = m_powerQuest.GetInventoryPrefabs().Count - 1; i >= 0; i--) 
			{
				if ( m_powerQuest.GetInventoryPrefabs()[i] == null )
				{
					m_powerQuest.GetInventoryPrefabs().RemoveAt(i);				
					EditorUtility.SetDirty(m_powerQuest);
				}
			}
			for (int i = m_powerQuest.GetGuiPrefabs().Count - 1; i >= 0; i--) 
			{
				if ( m_powerQuest.GetGuiPrefabs()[i] == null )
				{
					m_powerQuest.GetGuiPrefabs().RemoveAt(i);				
					EditorUtility.SetDirty(m_powerQuest);
				}
			}
			for (int i = m_powerQuest.GetRoomPrefabs().Count - 1; i >= 0; i--) 
			{
				if ( m_powerQuest.GetRoomPrefabs()[i] == null )
				{
					m_powerQuest.GetRoomPrefabs().RemoveAt(i);			
					EditorUtility.SetDirty(m_powerQuest);
				}
			}
		}
		/**/ 
	}

	// Note: when powerquest is set dirty, it'll cause an asset refresh & PostProcessAllAssets
	// NB: only call from OnPostProcessAllAssets
	void PostProcessQuestObjectLists()
	{		
		try
		{
			// Note: when powerquest is set dirty, it'll cause an asset refresh & PostProcessAllAssets. Need to make sure it can't get stuck in a loop doing this.

			// GUIs are automatically added to PowerQuest when found in the folder, so it's easy to share them in packages. 
			// Alternative could be to have some flag thats checked, and only add ones with it
			if ( PowerQuestAssetPostProcessor.HasPostProcessed("Gui") )
			{
				if ( RefreshObjectList( m_powerQuest.GetGuiPrefabs(), m_gamePath+"Gui" ) )
						EditorUtility.SetDirty(m_powerQuest);
				QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Guis); // update even if it's just a name change
			}
		}
		catch{}
	}

	// NB: only call from OnPostProcessAllAssets
	void PostProcessAudioCueLists() 
	{
		try
		{
			if ( m_systemAudio.EditorGetAutoAddCues()
			  && PowerQuestAssetPostProcessor.HasPostProcessed("Audio"))
			{
				if ( RefreshObjectList( m_systemAudio.EditorGetAudioCues(), PATH_AUDIO ) )
					EditorUtility.SetDirty(m_systemAudio);
				QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.Sound); // update even if it's just a name change
			}
		}
		catch{}
	}


	// Updates list with objects of specified type in the path, returns true if it changed.
	bool RefreshObjectList<T>( List<T> list, string path ) where T : MonoBehaviour
	{
		
		string[] assets = AssetDatabase.FindAssets("t:prefab", new string[]{path});

		// Create list of all items, with check to see they're the same type (check's prefab)
		List<T> newList = new List<T>();
		for (int i = 0; i < assets.Length; ++i)
		{		    
			T prefab = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(assets[i]));
			if ( prefab != null )
				newList.Add( prefab );
		}

		bool changed = list.Count != newList.Count;
		if ( changed == false )
		{  
			// same number of assets- check if any have changed. Ignore order changes.
			for (int i = 0; i < list.Count; ++i) 
			{
				if ( newList.Contains(list[i]) == false )
				{
					changed = true;
					break;
				}
			}
		}

		if ( changed )
		{ 
			//  Something's changed, use the new list
			list.Clear();
			list.AddRange(newList);
		}
		return changed;
	}

	// NB: Only call from  OnPostProcessAllAssets
	public void PostProcessAnimationLists()
	{
		try
		{

			bool anim = PowerQuestAssetPostProcessor.HasPostProcessed(".anim");
			bool png = PowerQuestAssetPostProcessor.HasPostProcessed(".png");
			if ( anim == false && png == false )
				return;

			if ( anim )
			{			
				if ( PowerQuestAssetPostProcessor.HasPostProcessed(PowerQuest.STR_INVENTORY) )
					if ( PostProcessRefreshAnimationList( m_powerQuest.GetInventoryAnimations(), m_gamePath+PowerQuest.STR_INVENTORY ) )
						EditorUtility.SetDirty(m_powerQuest);
					
				if ( PowerQuestAssetPostProcessor.HasPostProcessed("Gui/GuiAnims") )
					if ( PostProcessRefreshAnimationList( m_powerQuest.GetGuiAnimations(), m_gamePath+"Gui/GuiAnims" ) )
						EditorUtility.SetDirty(m_powerQuest);
	
				if ( PowerQuestAssetPostProcessor.HasPostProcessed("Cursor") )
				{
					if ( PostProcessRefreshAnimationList( m_powerQuest.GetCursorPrefab().GetAnimations(), m_gamePath+"Cursor") )
					{
						EditorUtility.SetDirty(m_powerQuest.GetCursorPrefab());			
						UpdateDefaultSprite( m_powerQuest.GetCursorPrefab(), m_powerQuest.GetCursorPrefab().GetData().AnimationClickable, m_powerQuest.GetCursorPrefab().GetAnimations() );
					}
				}
				
			}
			else if ( png )
			{			
				if ( PowerQuestAssetPostProcessor.HasPostProcessed("Inventory/Sprites") )
					if ( PostProcessRefreshSpriteList( m_powerQuest.GetInventorySprites(), m_gamePath+"Inventory/Sprites" ) )
						EditorUtility.SetDirty(m_powerQuest);
						
				if ( PowerQuestAssetPostProcessor.HasPostProcessed("Gui/GuiSprites") )
					if ( PostProcessRefreshSpriteList( m_powerQuest.GetGuiSprites(), m_gamePath+"Gui/GuiSprites" ) )
						EditorUtility.SetDirty(m_powerQuest);	
						
				if ( PowerQuestAssetPostProcessor.HasPostProcessed("Cursor/Sprites") )
				{
					if ( PostProcessRefreshSpriteList( m_powerQuest.GetCursorPrefab().GetSprites(), m_gamePath+"Cursor/Sprites" ) )
					{
						EditorUtility.SetDirty(m_powerQuest.GetCursorPrefab());			
						UpdateDefaultSprite( m_powerQuest.GetCursorPrefab(), m_powerQuest.GetCursorPrefab().GetData().AnimationClickable, m_powerQuest.GetCursorPrefab().GetAnimations() );
					}
				}
			}
						
			if ( PowerQuestAssetPostProcessor.HasPostProcessed("Rooms") )
			{
				foreach ( RoomComponent room in m_powerQuest.GetRoomPrefabs() )
				{	
					if ( PrefabUtility.GetCorrespondingObjectFromSource(room.gameObject) != null && room.EditorVariantShouldOverrideArt == false ) // Don't change animations for prefab variants, use their owner's
						continue;

					bool dirty = false;
					if ( anim )
						dirty = PostProcessRefreshAnimationList( room.GetAnimations(), Path.GetDirectoryName(AssetDatabase.GetAssetPath(room.gameObject)) );
					else if ( png )
						dirty =  PostProcessRefreshSpriteList( room.GetSprites(), Path.GetDirectoryName(AssetDatabase.GetAssetPath(room.gameObject)) );
				
					if ( dirty )
					{
						PrefabUtility.SavePrefabAsset(room.gameObject);						
						EditorUtility.SetDirty(room.gameObject);
						dirty = false;
						foreach ( PropComponent component in room.GetPropComponents() )
						{		
							dirty |= UpdateDefaultSprite( component, component.GetData().Animation, room.GetAnimations(), room.GetSprites() ); // Now only updating sprites if anim was changed.
						}
						
						if ( dirty && EditorSceneManager.GetActiveScene().name == room.GetData().GetSceneName() )
						{
							// have to do all these things in order for sprite toshow up immediately in scene.		
							PrefabUtility.SavePrefabAsset(room.gameObject);						
							EditorUtility.SetDirty(room.gameObject);
							PrefabUtility.RecordPrefabInstancePropertyModifications(room.gameObject);
							Selection.activeGameObject = room.gameObject;
						}

						QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.AnimProp); // Note: this won't update if an anim's been renamed, only if added/removed...
					}
				}
			}
			if ( PowerQuestAssetPostProcessor.HasPostProcessed("Characters") )
			{
				foreach ( CharacterComponent character in m_powerQuest.GetCharacterPrefabs() )
				{
					bool dirty = false;
					if ( anim )
						dirty = PostProcessRefreshAnimationList( character.GetAnimations(), Path.GetDirectoryName(AssetDatabase.GetAssetPath(character.gameObject)) );
				
					if ( dirty )
					{
						UpdateDefaultSprite( character, character.GetData().AnimIdle, character.GetAnimations() );
						UpdateDefaultSprite( character, character.GetData().AnimIdle+'R', character.GetAnimations() );
								
						EditorUtility.SetDirty(character.gameObject);

						QuestScriptEditor.UpdateAutoComplete(QuestScriptEditor.eAutoCompleteContext.AnimChar); // Note: this won't update if an anim's been renamed, only if added/removed...
					}					
				}
			}
			if ( PowerQuestAssetPostProcessor.HasPostProcessed("/Gui/") )
			{
				foreach ( GuiComponent gui in m_powerQuest.GetGuiPrefabs() )
				{
					if ( gui == null ) 
						continue; // Fix for exception when upgrading

					bool dirty = false;
					if ( anim )
						dirty = PostProcessRefreshAnimationList( gui.GetAnimations(), Path.GetDirectoryName(AssetDatabase.GetAssetPath(gui.gameObject)) );
					else if ( png )
						dirty =  PostProcessRefreshSpriteList( gui.GetSprites(), Path.GetDirectoryName(AssetDatabase.GetAssetPath(gui.gameObject)) );
			
					if ( dirty )
					{
						// TODO: Update controls, if they have no sprite set
						//UpdateDefaultSprite( character, character.GetData().AnimIdle, character.GetAnimations() );
				
						EditorUtility.SetDirty(gui.gameObject);

						// NB: When the object's staged, it doesn't get changes immediately.
						// Doesn't seem to be a fix for it. Maybe in unity 2021??.. For now just repeat the changes on the prefab contents	
						PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
						if ( stage != null && stage.prefabContentsRoot.name == gui.gameObject.name )
						{
							//Debug.Log("Gui sprites updated, close and re-open the prefab to update");						
							GuiComponent stagedGui = stage.prefabContentsRoot.GetComponent<GuiComponent>();
							if ( stagedGui != null )
							{
								if ( anim )
									PostProcessRefreshAnimationList( stagedGui.GetAnimations(), Path.GetDirectoryName(AssetDatabase.GetAssetPath(gui.gameObject)) );
								else if ( png )
									 PostProcessRefreshSpriteList( stagedGui.GetSprites(), Path.GetDirectoryName(AssetDatabase.GetAssetPath(gui.gameObject)) );
								EditorUtility.SetDirty(stagedGui);					
							}
						}
					}	
				}
			}
		}
		catch (System.Exception ex)
		{
			// Do nothing, just don't continue if there's a file error
			Debug.Log("PostProcess exception: "+ex.ToString());
		}
	}


	// Updates list with animations in the path, returns true if it changed. Must be called from PostProcess
	bool PostProcessRefreshAnimationList( List<AnimationClip> list, string path )
	{		
		if ( PowerQuestAssetPostProcessor.HasPostProcessed('/'+Path.GetFileName(path)+'/') == false )
			return false;
		if ( AssetDatabase.IsValidFolder(path) == false  )
			return false;

		string[] assets = AssetDatabase.FindAssets("t:animation", new string[]{path});
		bool changed = list.Count != assets.Length;
		if ( changed == false )
		{  
			// same number of assets- check if any have changed
			string[] paths = new string[assets.Length];
			for (int i = 0; i < list.Count; ++i) 
				paths[i] = AssetDatabase.GetAssetPath(list[i]);
			for (int i = 0; i < paths.Length; ++i) 
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assets[i]);
				if ( System.Array.Exists(paths, item=>item == assetPath) == false )
				{
					changed = true;
					break;
				}					
			}	
		}

		if ( changed )
		{
			list.Clear();
			for (int i = 0; i < assets.Length; ++i)
			{		    
				AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(assets[i]));
				if ( clip != null )
					list.Add( clip );
				else 
					Debug.LogWarning("RefreshAnimationList() found null clip"); // if this is hit, it means we're refreshing anim lists repeatedly (slowing down post-process)
			}
		}
		return changed;
	}

	// Updates list with animations in the path, returns true if it changed. Must be called from PostProcess
	bool PostProcessRefreshSpriteList( List<Sprite> list, string path )
	{	
		// Check the directory name (which will be the directory the sprite's in). Eg: 'Kitchen'
		if ( PowerQuestAssetPostProcessor.HasPostProcessed('/'+Path.GetFileName(path)+'/') == false )
			return false;
		if ( AssetDatabase.IsValidFolder(path) == false  )
			return false;

		string[] assets = AssetDatabase.FindAssets("t:sprite", new string[]{path});
		bool changed = list.Count != assets.Length;
		if ( changed == false )
		{  
			// same number of assets- check if any have changed
			string[] paths = new string[assets.Length];
			for (int i = 0; i < list.Count; ++i) 
				paths[i] = AssetDatabase.GetAssetPath(list[i]);
			for (int i = 0; i < paths.Length; ++i) 
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assets[i]);
				if ( System.Array.Exists(paths, item=>item == assetPath) == false )
				{
					changed = true;
					break;
				}
			}
		}

		if ( changed )
		{
			list.Clear();
			for (int i = 0; i < assets.Length; ++i)
			{		    
				Sprite clip = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(assets[i]));
				if ( clip != null )
					list.Add( clip );
				else 
					Debug.LogWarning("RefreshSpriteList() found null clip"); // if this is hit, it means we're refreshing anim lists repeatedly (slowing down post-process)
			}
		}
		return changed;
	}

	public static Sprite GetFirstSpriteInAnim( AnimationClip clip )
	{
		if ( clip == null )
			return null;
	
		// There's no sprite set, but there is an animation set, so set the sprite as the first frame of the animation				        						
		EditorCurveBinding m_curveBinding = new EditorCurveBinding();
		m_curveBinding = System.Array.Find( AnimationUtility.GetObjectReferenceCurveBindings(clip), item=>item.propertyName == PROPERTYNAME_SPRITE ); 
		if ( m_curveBinding.isPPtrCurve )
		{
			// Convert frames from ObjectReferenceKeyframe (struct with time & sprite) to our easier to use list of AnimFrame
			ObjectReferenceKeyframe[] objRefKeyframes = AnimationUtility.GetObjectReferenceCurve(clip, m_curveBinding );
			if ( objRefKeyframes.Length > 0 && objRefKeyframes[0].value != null )
			{
				return objRefKeyframes[0].value as Sprite;
			}
		}
		return null;
	}

	// NB: Can either set anim name and animation list, sprites list, or just the clip. Returns true if set
	public static bool UpdateDefaultSprite( MonoBehaviour owner, AnimationClip clip ) 
	{
		if ( owner == null || clip == null )
			return false;
		SpriteRenderer sprite = owner.GetComponentInChildren<SpriteRenderer>(true);
		if ( sprite == null || sprite.sprite != null )
			return false;
		
		Sprite newSprite = GetFirstSpriteInAnim(clip);
		if ( newSprite != null )
		{
			sprite.sprite = newSprite;
			EditorUtility.SetDirty(sprite);
			return true;		
		}
		return false;
		
	}
	public static bool UpdateDefaultSprite( MonoBehaviour owner, string animName, List<AnimationClip> animations,List<Sprite> spriteList = null )
	{
		if ( owner == null || string.IsNullOrEmpty(animName) || (animations == null && spriteList == null ))
			return false;
		SpriteRenderer sprite = owner.GetComponentInChildren<SpriteRenderer>(true);
		if ( sprite == null || sprite.sprite != null )
			return false;

		if ( spriteList != null )
		{		
			Sprite foundSprite = PowerQuest.FindSpriteInList(spriteList, animName);
			if ( foundSprite != null )
			{
				sprite.sprite=foundSprite;
				EditorUtility.SetDirty(sprite);
				return true;
			}
		}
		
		AnimationClip clip = null;
		
		//
		// Try animation list
		//
		if ( animations != null )
		{
			if ( clip == null )
				clip = animations.Find(item=> item != null && string.Equals(item.name, animName, System.StringComparison.OrdinalIgnoreCase));
		
			// Check if sprite has animation now
			Sprite newSprite = GetFirstSpriteInAnim(clip);
			if ( newSprite != null )
			{
				sprite.sprite = newSprite;
						EditorUtility.SetDirty(sprite);
						return true;
					}
				}
		
		//
		// Try sprites folder		
		//

		// If didn't find sprite in animation, check sprite folder for sprite
		if ( sprite.enabled && string.IsNullOrEmpty(animName) == false )
		{
			// find prefab folder
			//#if UNITY_2018_3_OR_NEWER
			//Object prefab = PrefabUtility.GetPrefabInstanceHandle(owner.gameObject);
			//#else
			#pragma warning disable 
			Object prefab = PrefabUtility.GetPrefabObject(owner.gameObject);
			#pragma restore
			//#endif
			if ( prefab == null )
				return false;
			string path = AssetDatabase.GetAssetPath( prefab );
			if ( string.IsNullOrEmpty(path) == false )
			{
				path = Path.GetDirectoryName(path);
				path += "/Sprites";

				string[] sprites = AssetDatabase.FindAssets("t:sprite", new string[]{path} ); // NB: this is VERY slow
				if ( sprites != null && sprites.Length > 0 )
				{
					//Debug.Log("Anim: "+animName+", Path: "+path+", spriteeg: "+Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(sprites[0])));
					string animNameUnderscore = animName+'_';

					string result = null;
					for (int i = 0; i < sprites.Length && result == null; ++i )
					{
						string spriteName = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(sprites[i]));
						if ( string.Equals(spriteName, animName, System.StringComparison.OrdinalIgnoreCase)
								|| spriteName.StartsWith(animNameUnderscore, System.StringComparison.OrdinalIgnoreCase) )
						{
							result = AssetDatabase.GUIDToAssetPath(sprites[i]);
						}
					}

					if ( result != null )
					{									
						//Debug.Log("Path: "+path+", Anim: "+animName+", Result: "+result);
						Debug.Log("Automatically assigned sprite: "+result);
						sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(result);
						EditorUtility.SetDirty(sprite);
						return true;
					}

				}
			}
				
		}
		return false;
	}


	void ApplySelectedRoomInstance()
	{
		// When scene's saved, apply changes to the selected room (if the selected room is a prefab)
		if ( m_selectedRoom == null )
			return;
		GameObject roomobj = m_selectedRoom.gameObject;
				
		#if UNITY_2018_3_OR_NEWER
		PrefabInstanceStatus prefabType = PrefabUtility.GetPrefabInstanceStatus(roomobj);
		if ( prefabType == PrefabInstanceStatus.Connected || prefabType == PrefabInstanceStatus.Disconnected  )
		{
			PrefabUtility.SaveAsPrefabAssetAndConnect( PrefabUtility.GetOutermostPrefabInstanceRoot(roomobj), QuestEditorUtils.GetPrefabPath(roomobj), InteractionMode.AutomatedAction);
		}				
		#else
		PrefabType prefabType = PrefabUtility.GetPrefabType(roomobj);
		if ( prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance )		
		{
			PrefabUtility.ReplacePrefab(roomobj, PrefabUtility.GetPrefabParent(roomobj),ReplacePrefabOptions.ConnectToPrefab);
		}
		#endif
	}
}


#endregion

}
