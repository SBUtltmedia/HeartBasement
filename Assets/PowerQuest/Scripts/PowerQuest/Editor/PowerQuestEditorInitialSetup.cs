using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.IO;
using System.Text.RegularExpressions;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.Quest
{


// For Initial Setup gui stuff
public partial class PowerQuestEditor
{
	#region Variables: Static definitions

	enum eGameTemplate {Default, NineVerb, HD, Parser};

	static readonly string[] GAME_TEMPLATE_NAMES = {"Default","9-Verb", "HD", "Text Parser"};
	static readonly string[] GAME_TEMPLATE_PATHS = 
	{
		"Assets/PowerQuest/Templates/DefaultGameTemplate.unitypackage",
		"Assets/PowerQuest/Templates/9VerbGameTemplate.unitypackage",
		"Assets/PowerQuest/Templates/HdGameTemplate.unitypackage",
		"Assets/PowerQuest/Templates/ParserGameTemplate.unitypackage",
	};
	static readonly string[] GAME_TEMPLATE_DESCRIPTIONS = 
	{
		"The Default Template is a modern 1 or 2 click interface, with drop-down inventory (Similar to most Wadjet Eye games, or Beneath a Steel Sky).",
		"The 9-Verb interface is the classic Lucasarts style. It's clunky by modern standards, but fun for that retro style. Just be prepared to write lots of responses for interactions! \n\nIt's a bit more complicated than the default, so read \"Assets\\Game\\9-Verb-ReadMe.txt\" to get started.",
		"A High Def (1080p) version of the default template. Good starting place for non-pixel art games.",
		"A Text Parser interface similar to early Sierra games. Type commands to perform actions! \n\nYou can optionally also left click to walk, and right click to look-at."
	};
	
	// Function to export the powerQuest package automatically
	public static void ExportTemplatePackage()
	{		
		AssetDatabase.ExportPackage( new string[] {@"Assets\Audio", @"Assets\Fonts", @"Assets\Game"}, @"Assets\PowerQuest\Templates\DefaultGameTemplate.unitypackage", ExportPackageOptions.Recurse);
	}
	public static void ExportTemplatePackage9Verb()
	{		
		AssetDatabase.ExportPackage( new string[] {@"Assets\Audio", @"Assets\Fonts", @"Assets\Game"}, @"Assets\PowerQuest\Templates\9VerbGameTemplate.unitypackage", ExportPackageOptions.Recurse);
	}
	public static void ExportTemplatePackageHD()
	{		
		AssetDatabase.ExportPackage( new string[] {@"Assets\Audio", @"Assets\Fonts", @"Assets\Game"}, @"Assets\PowerQuest\Templates\HdGameTemplate.unitypackage", ExportPackageOptions.Recurse);
	}
	public static void ExportTemplatePackageParser()
	{		
		AssetDatabase.ExportPackage( new string[] {@"Assets\Audio", @"Assets\Fonts", @"Assets\Game"}, @"Assets\PowerQuest\Templates\ParserGameTemplate.unitypackage", ExportPackageOptions.Recurse);
	}

	#endregion
	#region Variables: Serialized

	[SerializeField] bool m_initialSetupImportInProgress = false;

	#endregion
	#region Gui Layout: Initial Setup Required

	eGameTemplate m_gameTemplate = eGameTemplate.Default;

	void OnGuiInitialSetup()
	{
		GUILayout.Space(20);
		//GUILayout.BeginHorizontal();
		GUILayout.Label("Initial set up required!", EditorStyles.boldLabel );

		EditorGUILayout.HelpBox("This will create template game files to get you started."+"\n\n"+GAME_TEMPLATE_DESCRIPTIONS[(int)m_gameTemplate],MessageType.Info);

		
		m_gameTemplate = (eGameTemplate)EditorGUILayout.Popup("Game Template:",(int)m_gameTemplate,GAME_TEMPLATE_NAMES);
		string templateText = string.Empty;

		if ( GUILayout.Button("Set it up!") )
		{
			m_initialSetupImportInProgress = true;
			PowerQuestProjectSetupUtil.AddAlwaysIncludedShaders();
			AssetDatabase.ImportPackage( GAME_TEMPLATE_PATHS[(int)m_gameTemplate], false );
		}
		
		EditorGUILayout.Separator();
		LayoutManual();
	}


    #endregion
	#region Functions: Private


	// Checks setup of powerquest
	void UpdateCheckInitialSetup()
	{

		if ( m_powerQuest == null )
		{
			GameObject obj = AssetDatabase.LoadAssetAtPath(m_powerQuestPath, typeof(GameObject)) as GameObject;
			if ( obj != null ) // we check for layout event type so we don't change the gui while between layout and refresh
				m_powerQuest = obj.GetComponent<PowerQuest>();
			if ( m_powerQuest != null )
				OnFoundPowerQuest();
		}

		// Find systemText in same folder as PowerQuest
		if ( m_systemText == null)
		{
			string systemPath = Path.GetDirectoryName(m_powerQuestPath)+"/SystemText.prefab";
			GameObject obj = AssetDatabase.LoadAssetAtPath(systemPath, typeof(GameObject)) as GameObject;
			if ( obj != null )
				m_systemText = obj.GetComponent<SystemText>();
		}
		
		// Find parser in same folder as PowerQuest
		if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Parser) && m_systemParser == null)
		{
			string systemPath = Path.GetDirectoryName(m_powerQuestPath)+"/SystemParser.prefab";
			GameObject obj = AssetDatabase.LoadAssetAtPath(systemPath, typeof(GameObject)) as GameObject;
			if ( obj != null )
				m_systemParser = obj.GetComponent<SystemParser>();
		}

		// Find systemAudio in same folder as PowerQuest
		if ( m_systemAudio == null)
		{
			string systemPath = Path.GetDirectoryName(m_systemAudioPath)+"/SystemAudio.prefab";
			GameObject obj = AssetDatabase.LoadAssetAtPath(systemPath, typeof(GameObject)) as GameObject;
			if ( obj != null )
				m_systemAudio = obj.GetComponent<SystemAudio>();
		}
	}

	void OnFoundPowerQuest()
	{
		CreateMainGuiLists();

		if ( m_initialSetupImportInProgress )
		{
			m_initialSetupImportInProgress = false;
			OnInitialSetupComplete();
		}
	}

	// Called when first imported game data
	void OnInitialSetupComplete()
	{
		AssetDatabase.Refresh(); // In case there were changes while play mode.

		// Add scenes to editor build settings
		foreach ( RoomComponent roomPrefab in GetPowerQuest().GetRoomPrefabs() )
		{	
			string path	= Path.GetDirectoryName( AssetDatabase.GetAssetPath(roomPrefab) ) + "/" + roomPrefab.GetData().GetSceneName()+".unity";
			AddSceneToBuildSettings(path);
		}

		// Load the first scene- May throw exception, safe to ignore
		if ( GetPowerQuest().GetRoomPrefabs().Count > 0 )
		{
			try 
			{
				LoadRoomScene(GetPowerQuest().GetRoomPrefabs()[0]);
			}
			catch
			{
			}
		}
		
		// NB: This file exists for upgrades, but must be removed
		AssetDatabase.DeleteAsset(@"Assets\Plugins\PowerSpriteImport\Editor\PowerSpriteImportEditor.cs");		

		Repaint();

	}

	#endregion
	#region Version Upgradeing

	TextAsset m_versionFile = null;

	// Checks if version has changed, and if it has, upgrades
	void CheckVersion()
	{
		if ( m_powerQuest == null )
			return;

		// load version file 
		if ( m_versionFile == null )
			m_versionFile =  AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/PowerQuest/Version.txt");				
		int newVersion = Version(m_versionFile.text);

		// Check version, upgrade stuff if necessary		
		int version = m_powerQuest.EditorGetVersion();
		if ( version < newVersion )
		{
			if ( UpgradeVersion(version, newVersion) )
			{
				m_powerQuest.EditorSetVersion(newVersion);
				EditorUtility.SetDirty(m_powerQuest);
			}
		}
	}

	// Called when updating to new version of powerquest. 
	// I always forget what to search for when editng this, so... UpdateVersion VersionUpdate VersionUpgrade UpdatePowerQuest PowerQuestUpdate PowerQuestUpgrade
	bool UpgradeVersion(int oldVersion, int newVersion)
	{
		try 
		{

			// Version 0.4.3 changed what GlobalScript inherits from (slightly)
			if ( oldVersion < Version(0,4,3) )
			{
				// Need to update GlobalScript to inherit from GlobalScriptBase

				string globalSource = File.ReadAllText(	PowerQuestEditor.PATH_GLOBAL_SCRIPT );
				globalSource = Regex.Replace(globalSource, "class GlobalScript.*","class GlobalScript : GlobalScriptBase<GlobalScript>", RegexOptions.Multiline);
				File.WriteAllText( PowerQuestEditor.PATH_GLOBAL_SCRIPT, globalSource );
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			}

			// 0.5.0: Changes to DialogText, AudioCues, QuestCamera
			if ( oldVersion < Version(0,5,0) )
			{		
				// Dialog text set to Gui layer with -10 sort order
				m_powerQuest.GetDialogTextPrefab().SortingLayer = "Gui";
				m_powerQuest.GetDialogTextPrefab().OrderInLayer = -10;
				m_powerQuest.GetDialogTextPrefab().gameObject.layer = LayerMask.NameToLayer("UI");
				EditorUtility.SetDirty(m_powerQuest.GetDialogTextPrefab());

				// AudioCues that have a clip with "loop" should set "loop" in base
				foreach( AudioCue cue in m_systemAudio.EditorGetAudioCues() )
				{
					if ( cue.GetClipCount() > 0 && cue.GetClipData(0).m_loop )
					{
						cue.m_loop = true;
						EditorUtility.SetDirty(cue.gameObject);
					}					
				}
				/* Actually... leave it as it is
				// QuestCamera layer should be set to NoPause
				GameObject questCam = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Camera/QuestCamera.prefab");
				if ( questCam != null )
				{
					questCam.layer = LayerMask.NameToLayer("NoPause");
					EditorUtility.SetDirty(questCam);
				}*/

			}

			if ( oldVersion < Version(0,5,2) )
			{
				// Add speechBox package
				AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/SpeechBox.unitypackage", false );	

			}

			if ( oldVersion < Version(0,8,1) )
			{
				// Add PowerQuestExtensions
				AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-8.unitypackage", false );

				// Also really should add "OnMouseClick" to global script here... but too fiddly, so people are on there own with that one :O

				// Also, gui toolbar curves changed, copy them from the inventory one
				GuiComponent guiToolbar = m_powerQuest.GetGuiPrefabs().Find(item=>item.GetData().ScriptName == "Toolbar");
				GuiComponent guiInventory = m_powerQuest.GetGuiPrefabs().Find(item=>item.GetData().ScriptName == PowerQuest.STR_INVENTORY);
				if ( guiToolbar != null && guiInventory != null && guiToolbar.GetComponent<GuiDropDownBar>() != null && guiInventory.GetComponent<GuiDropDownBar>() != null)
				{
					SerializedObject objToolbar = new SerializedObject(guiToolbar.GetComponent<GuiDropDownBar>());
					SerializedObject invToolbar = new SerializedObject(guiInventory.GetComponent<GuiDropDownBar>());
					if ( Mathf.Approximately( objToolbar.FindProperty("m_dropDownDistance").floatValue,1.0f) )
					{
						objToolbar.FindProperty("m_dropDownDistance").floatValue = invToolbar.FindProperty("m_dropDownDistance").floatValue;
						objToolbar.FindProperty("m_curveIn").animationCurveValue = invToolbar.FindProperty("m_curveIn").animationCurveValue;
						objToolbar.FindProperty("m_curveOut").animationCurveValue = invToolbar.FindProperty("m_curveOut").animationCurveValue;

						objToolbar.ApplyModifiedProperties();
						EditorUtility.SetDirty(guiToolbar);
					}
				}


				// Also remove PlayAmbientSound call from globalscript
				string globalSource = File.ReadAllText(	PowerQuestEditor.PATH_GLOBAL_SCRIPT );
				globalSource = Regex.Replace(globalSource, @"// Restart ambient sound","", RegexOptions.Multiline);
				globalSource = Regex.Replace(globalSource, @"PlayAmbientSound\(m_ambientSoundName\);","", RegexOptions.Multiline);
				File.WriteAllText( PowerQuestEditor.PATH_GLOBAL_SCRIPT, globalSource );
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

				// And delete the QuestGuiButton which has been removed
				AssetDatabase.DeleteAsset("Assets/PowerQuest/Scripts/Gui/QuestGuiButton.cs");
			}

			if ( oldVersion < Version(0,9,0) )
			{
				// update dialog text offset (down to 2, now it takes text  size into account)
				GuiComponent guiDialog = m_powerQuest.GetGuiPrefabs().Find(item=>item.GetData().ScriptName == "DialogTree");
				if ( guiDialog != null && guiDialog.GetComponent<GuiDialogTreeComponent>() != null )
				{
					SerializedObject serializedObj = new SerializedObject(guiDialog.GetComponent<GuiDialogTreeComponent>());
					if ( serializedObj.FindProperty("m_itemSpacing").floatValue > 5 )
					{
						
						serializedObj.FindProperty("m_itemSpacing").floatValue = 2;
						serializedObj.ApplyModifiedProperties();
						EditorUtility.SetDirty(guiDialog);
					}
				}

				// Update "Enable" and "Show" functions to remove the first boolean (if it's false, should error). Show(false) will give wierd errors), 

				// Actually- just show a prompt
				EditorUtility.DisplayDialog(
					"PowerQuest Updated!",
					"Important note! The following functions changed-\n\n   Character::Show(bool visible, bool clickable)\n   Prop::Enable(bool visible, bool clickable)\n\nChanged to \n\n   Character::Show(bool clickable)\n   Prop::Enable(bool clickable)\n\nSo where you had C.Player.Show(true,true); Change it to 'C.Player.Show();'\n\nWhere you had 'C.Player.Show(false,true);'. Change it to 'C.Player.Visible = true;'\n\n---\n\nLook at the online documentation for a full list of changes!\n",
					"Got it!");

			}

			if ( oldVersion < Version(0,11,0) )
			{
				// Set default inventory click mode to zero when updating				
				SerializedObject serializedObj = new SerializedObject(m_powerQuest);
				if ( serializedObj.FindProperty("m_inventoryClickStyle") != null)
				{
					// Found inv click property					
					serializedObj.FindProperty("m_inventoryClickStyle").intValue = 0;
					serializedObj.ApplyModifiedProperties();
					EditorUtility.SetDirty(m_powerQuest);
				}
			}

			if ( oldVersion < Version(0,12,5) )
			{
				// update project settings to set webgl setting
				PowerQuestProjectSetupUtil.SetProjectSettings();
			}
			if ( oldVersion < Version(0,13,6) )
			{
			
				// Actually- just show a prompt
				EditorUtility.DisplayDialog(
					"PowerQuest Updated!",
					"Important note! The character Show() and Enable() functions have changed subtly. They no longer override the 'Clickable' property you have set. \n\nEg. If you've specifically set 'Clickable = false;' then call 'Show();' the character will stay non-clickable.\n\n---\n\nLook at the online documentation for more info and a full list of changes!\n",
					"Got it!");
			}

			//if (oldVersion < Version(0,14,5)) // do this for all future versions (until we can be sure no-one's updating from pre 14.5)
			{
				// Delete old version of PowerSpriteImportEditor				
				AssetDatabase.DeleteAsset(@"Assets\Plugins\PowerSpriteImport\Editor\PowerSpriteImportEditor.cs");				
			}
			
			if ( oldVersion < Version(0,14,10) )
			{
				// Pre-empting 0.15 change to 9-verb. Otherwise when they install 0.15 it'll have compile error and won't get to this update code.
				if ( File.Exists("Assets/Game/PowerQuestExtensions9Verb.cs") )
				{
					AssetDatabase.ImportPackage("Assets/PowerQuest/Templates/Update-0-14-10-9Verb.unitypackage", false );
				}
			}

			if ( oldVersion < Version(0,15,0) )
			{
				// Rename Gui/Sprites to Gui/GuiSprites
				Debug.Log(AssetDatabase.RenameAsset("Assets/Game/Gui/Sprites", "GuiSprites"));

				// Create sprite atlases
				#if !UNITY_2020_3_OR_NEWER
				EditorUtility.DisplayDialog(
					"Older Unity Detectved",
					"Unity 2020.3 or greater is now the minimum unity version PowerQuest supports. Sorry!","Ok");
				#endif
				if ( EditorUtility.DisplayDialog("Create Sprite Atlases",
					"PowerQuest v0.15 now creates sprite atlases automatically for new rooms, characteres, etc.\n\nCreate atlases for existing sprites?","Yes (Recommended)","No, I'll do it myself") )
				{
					CreateSpriteAtlases();
				}

				// update cursor sprite sort order to 32000
				if ( m_powerQuest != null && m_powerQuest.GetCursorPrefab() != null && m_powerQuest.GetCursorPrefab().GetComponentInChildren<SpriteRenderer>() != null )
				{
					m_powerQuest.GetCursorPrefab().GetComponentInChildren<SpriteRenderer>().sortingOrder = 32000;
					EditorUtility.SetDirty(m_powerQuest.GetCursorPrefab());
				}
				
				
				// Import controls, gui sprites/anims, and prompt gui
				//AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-15.unitypackage", false );
				
				EditorUtility.SetDirty(m_powerQuest);

				// Add prompt gui
				{
					string name = "Prompt";
					QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#G", "\n\t\tpublic static IGui "+name.PadRight(14)+" { get { return PowerQuest.Get.GetGui(\""+name+"\"); } }");
				}

			}
			if ( oldVersion < Version(0,15,4) )
			{
				// Add options gui
				{
					string name = "Options";
					QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#G", "\n\t\tpublic static IGui "+name.PadRight(14)+" { get { return PowerQuest.Get.GetGui(\""+name+"\"); } }");
				}				
								
				// Import controls, gui sprites/anims, and prompt gui
				if ( File.Exists("Assets/Game/PowerQuestExtensions9Verb.cs") )
				{
					AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-15-9Verb.unitypackage", false );
				}
				else 
				{
					AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-15.unitypackage", false );
				}
			}
			
			if ( oldVersion < Version(0,15,8) )
			{
				// Add save gui & PQ icon
				{
					string name = "Save";
					QuestEditorUtils.InsertTextIntoFile(PATH_GAME_GLOBALS, "#G", "\n\t\tpublic static IGui "+name.PadRight(14)+" { get { return PowerQuest.Get.GetGui(\""+name+"\"); } }");
				}
				
				if ( File.Exists("Assets/Game/PowerQuestExtensions9Verb.cs") )
				{
					AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-15-8-9Verb.unitypackage", false );
				}
				else 
				{
					AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-15-8.unitypackage", false );
				}
			}

			if ( oldVersion < Version(0,15,11) )
			{			
				// Renamed mispelled Occurance functions
				ReplaceInAllScripts("Occurr?ance","Occurrence");		
				
			}

			if ( oldVersion < Version(0,16,1) )
			{	
				if ( File.Exists("Assets/Game/PowerQuestExtensions9Verb.cs") )
				{
					AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-16-1-9Verb.unitypackage", false );
				}
				else 
				{
					AssetDatabase.ImportPackage( "Assets/PowerQuest/Templates/Update-0-16-1.unitypackage", false );
				}
			}
			
			if ( oldVersion < Version(0,16,2) )
			{
				// Add shaders to required thingy
				PowerQuestProjectSetupUtil.AddAlwaysIncludedShaders();
			
				// Scale vertical resolution to horizontal resolutions supported. (Now regretting setting it as a width instead of aspect dropdown lol)
				m_powerQuest.EditorSetHorizontalResolution(new MinMaxRange((16f/9f)*m_powerQuest.DefaultVerticalResolution) );
				EditorUtility.SetDirty(m_powerQuest);
			}

			
			if ( oldVersion < Version(0,18,6) )
			{
				// Replace GetInventoryItems with _SaveFlagNotDirtied version in GlobalScript
				string globalSource = File.ReadAllText(	PowerQuestEditor.PATH_GLOBAL_SCRIPT );
				globalSource = Regex.Replace(globalSource, @"GetInventoryItems\(","GetInventoryItems_SaveFlagNotDirtied(", RegexOptions.Multiline);
				File.WriteAllText( PowerQuestEditor.PATH_GLOBAL_SCRIPT, globalSource );
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			}

			// Remove PowerQuestObsolete after upgrading for all future versions
			{
				AssetDatabase.DeleteAsset(@"Assets\PowerQuest\Scripts\PowerQuest\PowerQuestObsolete.cs");
				//AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);		
			}

		}
		catch ( System.Exception e )
		{
			Debug.LogError("Failed to upgrade PowerQuest, close unity and try again\n\n"+e.Message);
			return false;
		}

		return true;
	}

	void ReplaceInAllScripts(string pattern, string replacement)
	{	
		// Find all the paths we want to hot-load	
		List<string> paths = new List<string>();

		string[] assets = AssetDatabase.FindAssets(STR_SCRIPT_TYPE,STR_SCRIPT_FOLDERS);			
		for ( int i = 0; i < assets.Length; ++i )
		{
			string path = AssetDatabase.GUIDToAssetPath(assets[i]);			
			if ( paths.Contains(path) == false )
				paths.Add(path);		
		}

		foreach( string path in paths )
		{
			string globalSource = File.ReadAllText(	path );
			globalSource = Regex.Replace(globalSource, pattern,replacement, RegexOptions.Multiline);
			File.WriteAllText( path, globalSource );
		}
	}
	
	[MenuItem("Edit/PowerQuest/Create sprite atlases")]
	static void CreateSpriteAtlases()
	{
		bool pixel = PowerQuestEditor.GetPowerQuest().GetSnapToPixel();

		string result = "Created Sprite Atlases:\n";
		
		// Inventory
		string path = "Assets/Game/Inventory";
		if ( QuestEditorUtils.CreateSpriteAtlas($"{path}/InventoryAtlas.spriteatlas",$"{path}/Sprites",pixel,false) )
			result += $"{path}/InventoryAtlas.spriteatlas\n";
		// Gui
		path = "Assets/Game/Gui";
		if ( QuestEditorUtils.CreateSpriteAtlas($"{path}/GuiAtlas.spriteatlas",$"{path}/GuiSprites",pixel,true) )
			result += $"{path}/GuiAtlas.spriteatlas\n";
		
		// Rooms
		foreach ( RoomComponent prefab in PowerQuestEditor.GetPowerQuest().GetRoomPrefabs() )
		{
			path = Path.GetDirectoryName( AssetDatabase.GetAssetPath(prefab));
			if ( QuestEditorUtils.CreateSpriteAtlas($"{path}/Room{prefab.GetData().ScriptName}Atlas.spriteatlas", $"{path}/Sprites",pixel,false) )
				result += $"{path}/Room{prefab.GetData().ScriptName}.spriteatlas\n";
		}
		
		// Characters
		foreach ( CharacterComponent prefab in PowerQuestEditor.GetPowerQuest().GetCharacterPrefabs() )
		{
			path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefab));
			if ( QuestEditorUtils.CreateSpriteAtlas($"{path}/Character{prefab.GetData().ScriptName}Atlas.spriteAtlas", $"{path}/Sprites",pixel,false) )
				result += $"{path}/Character{prefab.GetData().ScriptName}.spriteatlas\n";
		}
		result=result.Replace('\\','/');
		EditorUtility.DisplayDialog( "Created Sprite Atlases",	result, "Ok!");

		AssetDatabase.Refresh();
	}
}

#endregion
#region PowerQuest Layer Installer

//  class that just adds necessary layers to the project
[InitializeOnLoad]
public class PowerQuestProjectSetupUtil
{
	static readonly string[] LAYERS_REQUIRED = {"NoPause","HighRes"};
	static readonly string[] SORTINGLAYERS_NAME = {"Gui","GameText"};
	static readonly long[] SORTINGLAYERS_HASH = {3130941793,1935310555}; // hack but it was breaking existing stuff without it.
	
	static PowerQuestProjectSetupUtil()
	{
		if ( HasLayers() == false )
		{
			AddLayers();
			SetProjectSettings();
			
			// This is first time package has loaded, so open the powerquest window
			EditorWindow.GetWindow<PowerQuestEditor>("PowerQuest");
		}
	}

	static bool HasLayers()
	{	
		return System.Array.Exists(LAYERS_REQUIRED, item=>LayerMask.NameToLayer(item) <= 0 ) == false
			&& System.Array.Exists(SORTINGLAYERS_NAME, item=>SortingLayer.GetLayerValueFromName(item) <= 0 ) == false;
	}

	// Adapted From http://forum.unity3d.com/threads/adding-layer-by-script.41970/ via AC ;)
	static void AddLayers()
	{
		if ( HasLayers() )
			return;

		// Doesn't exist, so create it
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/TagManager.asset")[0]);

		SerializedProperty allLayers = tagManager.FindProperty ("layers");
		if (allLayers == null || !allLayers.isArray)
			return;	

		foreach ( string layerName in LAYERS_REQUIRED )
		{
			if ( LayerMask.NameToLayer(layerName) >= 0 )
				continue;
			
			// Create layer
			SerializedProperty slot = null;
			for (int i = 8; i <= 31; i++)
			{			
				SerializedProperty sp = allLayers.GetArrayElementAtIndex (i);
				if (sp != null && string.IsNullOrEmpty (sp.stringValue))
				{
					slot = sp;
					break;
				}
			}

			if (slot != null)
			{
				slot.stringValue = layerName;
			}
			else
			{
				Debug.LogError("Failed to install PowerQuest- Could not find an open Layer Slot for: " + layerName);
				return;
			}

			tagManager.ApplyModifiedProperties ();

			Debug.Log("Created layer: " + layerName);
		}

		SerializedProperty allSortingLayers = tagManager.FindProperty ("m_SortingLayers");
		if (allSortingLayers == null || !allSortingLayers.isArray)
			return;	
		for ( int i = 0; i < SORTINGLAYERS_NAME.Length; ++i )
		{
			string layerName = SORTINGLAYERS_NAME[i];
			long layerHash = SORTINGLAYERS_HASH[i];

			//Debug.Log("layer: " + layerName + "Id: "+SortingLayer.GetLayerValueFromName(layerName));

			if ( SortingLayer.GetLayerValueFromName(layerName) > 0 ) 
				continue;
			
			// Create layer
			allSortingLayers.InsertArrayElementAtIndex(allSortingLayers.arraySize);
			SerializedProperty sp = allSortingLayers.GetArrayElementAtIndex(allSortingLayers.arraySize-1);
			sp.FindPropertyRelative("name").stringValue = layerName;
			sp.FindPropertyRelative("uniqueID").longValue = layerHash;

			tagManager.ApplyModifiedProperties ();

			Debug.Log("Created sorting layer: " + layerName); 
		}
	}

	public static void AddAlwaysIncludedShaders()
	{
		AddAlwaysIncludedShader("Powerhoof/Pixel Text Shader");
		AddAlwaysIncludedShader("Powerhoof/Pixel Text Shader AA");
		AddAlwaysIncludedShader("Powerhoof/Sharp Text Shader");
		AddAlwaysIncludedShader("Sprites/PowerSprite");
		AddAlwaysIncludedShader("Sprites/PowerSpriteAdditive");
		AddAlwaysIncludedShader("Sprites/PowerSpriteOutline");
		AddAlwaysIncludedShader("Sprites/PowerSpriteAA");
		AddAlwaysIncludedShader("Unlit/TransparentAntiAliased");
	}
				
	static void AddAlwaysIncludedShader(string shaderName)
	{
		var shader = Shader.Find(shaderName);
		if (shader == null)
		{
			Debug.Log($"Unable to find shader {shaderName} while adding to 'Always Included Shader' list. Skipping...");
			return;
		}
 
		var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
		if ( graphicsSettingsObj  == null )
		{
			Debug.Log("Graphic settings unavailable while adding to 'Always Included Shader' list. Skipping...");
			return;
		}
		var serializedObject = new SerializedObject(graphicsSettingsObj);
		var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
		bool hasShader = false;
		for (int i = 0; i < arrayProp.arraySize; ++i)
		{
			var arrayElem = arrayProp.GetArrayElementAtIndex(i);
			if (shader == arrayElem.objectReferenceValue)
			{
				hasShader = true;
				break;
			}
		}
 
		if (!hasShader)
		{
			int arrayIndex = arrayProp.arraySize;
			arrayProp.InsertArrayElementAtIndex(arrayIndex);
			var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
			arrayElem.objectReferenceValue = shader;
 
			serializedObject.ApplyModifiedProperties();
 
			AssetDatabase.SaveAssets();
		}
	}

	public static void SetProjectSettings()
	{
		SerializedObject editorSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/EditorSettings.asset")[0]);
		if (  editorSettings != null )
		{
			// Sprite packer setting
			SerializedProperty prop = editorSettings.FindProperty ("m_SpritePackerMode");			
			if ( prop != null )
			{
				#if UNITY_2020_3_OR_NEWER
					prop.intValue = (int)SpritePackerMode.AlwaysOnAtlas;
					Debug.Log("PowerQuest setup - Set sprite packer to build time atlas mode"); 
				#else				
					prop.intValue = (int)SpritePackerMode.BuildTimeOnly;
					Debug.Log("PowerQuest setup - Set sprite packer to legacy mode"); 
				#endif
			}

			// Line endings settings
			prop = editorSettings.FindProperty ("m_LineEndingsForNewScripts");
			if ( prop != null )
			{
				prop.intValue = (int)LineEndingsMode.Unix;
				Debug.Log("PowerQuest setup - Set line endings to unix"); 
			}

			editorSettings.ApplyModifiedProperties();
			
		}

		SerializedObject projectSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/ProjectSettings.asset")[0]);
		if (  projectSettings != null )
		{
			// WebGL exception settings
			// webGLExceptionSupport: 2
			SerializedProperty prop = projectSettings.FindProperty("webGLExceptionSupport");
			if ( prop != null )
			{
				prop.intValue = (int)WebGLExceptionSupport.FullWithoutStacktrace;				
				Debug.Log("PowerQuest setup - WebGL Exception Support to full"); 
			}
			projectSettings.ApplyModifiedProperties();
		}
	}
	
	// TODO: Remove this? Not sure it's used...
	public static void SetEditorSettings()
	{
		SerializedObject editorSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/EditorSettings.asset")[0]);
		if (  editorSettings == null )
			return;
		/*
		{
			SerializedProperty prop = editorSettings.FindProperty ("m_SpritePackerMode");
			if ( prop != null )
			{
				prop.intValue = (int)SpritePackerMode.BuildTimeOnly;
			}
			Debug.Log("PowerQuest setup - Set sprite packer to legacy mode"); 
		}*/

		SerializedProperty prop = editorSettings.FindProperty ("m_LineEndingsForNewScripts");
		if ( prop != null )
		{
			prop.intValue = (int)LineEndingsMode.Unix;
		}
		editorSettings.ApplyModifiedProperties();

	}

}

#endregion

}
