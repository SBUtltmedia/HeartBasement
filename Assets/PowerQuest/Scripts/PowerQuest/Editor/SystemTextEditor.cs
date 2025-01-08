using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Linq;
using System;

namespace PowerTools.Quest
{


[CustomEditor(typeof(SystemText))]
public class SystemTextEditor : Editor 
{
	public static readonly char[] LABEL_DELIMITER_WRITE=new char[]{',',' '};
	public static readonly string WHITESPACE = " ";
	public static readonly string LABEL_DELIMITER_READ = ", ";


	SystemText m_component = null;
	List<string> m_processedFiles = new List<string>();
	
	static bool m_skipExistingLipsyncData = true;
	[SerializeField] bool m_foldoutSettings = false;
	[SerializeField] bool m_foldoutProcess = false;
	[SerializeField] bool m_foldoutScript = false;
	[SerializeField] bool m_foldoutCSV = false;
	[SerializeField] bool m_foldoutLipSync = false;
		
	enum eProcessTextMode
	{
		ProcessFromScript,
		UpdateScript
	}
	eProcessTextMode m_processTextMode = eProcessTextMode.ProcessFromScript;	

	// Regex explanation:
	//		- Finds things of format
	//			C.<character>.Say( /* comment */ $"<text>", 45 /*comment*/ );
	//			.Display( /* comment */ $"<text>", <id> /*comment*/ );
	//			SystemText.Localize( /* comment */ $"<text>", <id> /*comment*/ );
	//			.Description = "<text>"
	//		- eg: C.Jon.Say("yo dudes");
	//			or .Display($"whoo!", 32);
	//			or SystemText.Localize("&<id> Whatever");
	//			or C.Man.Description = "It's a silly man"
	//		- Comments, and ", <id>" are optional, whitespace is optional
	//							
	//		Optional whitespace:	\s*
	//		Optional comment:		(/\*.*\*/\s*)* 
	//		Character name:			(?<character> \w+ )
	//		The text string:		"(?<text>.*)"
	//		The optional id:		(, \s* (?<id> \d* ) \s* )?
	//		The inline id:			(&(?<id>)\s)?
	//		<start> WAS everything before the "id" is added. But is now everything until start quote for the assigned string. (Effetively same as asignstart now I think)
	//		<assignStart> is everything until the start quote for the assigned string
	//	
	static readonly Regex REGEX_DIALOG_METHOD = new Regex(
		//@"(  (public\s+\w+\s+(?<function>\w+)\s*\()    " // Check for function (NB: Only works on public functions, too hard to tell what's a func declaration otherwise. Eg: else if (blah)
		@"(  (^\s*((public|private)\s+\w+|IEnumerator)\s+(?<function>\w+)\s*\()"
		+@"| ( (?<assignStart> \.\s*Description\s* = \s*) \$?""(?<text>.*)"" )" // ".Desciption = "hi" 
		+@"| ((?<start> (" // beggining of <start> section, and alternate function
				+@"(C \s* \. \s* (?<character> \w+ ) \s* \. \s* Say(BG|NoSkip|BGSkip)?) " // "C.Guy.SayBG"
				+@" |  (\.\s*(?<character>Section)) " // ".Section"
				+@" |  (\.\s*Display(BG)?)  "	// "DisplayBG"
				+@" |  (SystemText\.(?<character>Localize)) ) " // "SystemText.Localize"  and end of alternates
			+@"  \s*\( \s* (/\*.*\*/\s*)* \$?)" // Open bracket (" and comments, and end of <start> section
			+@" ""(?<text>.*)""" // The text inside "blah"
			+@" \s* (, \s* (?<id> \d* ) \s* )? " // Optional id parameter ", 123 "
			+@" (/\*.*\*/\s*)* \) )"  // Whitespace, comments and Function ending " )"
		+")",
		RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Multiline );
	
	
	static readonly Regex REGEX_NEWLINE = new Regex("(\r\n|\n|\r)", RegexOptions.Compiled);

	// Used for adding ids to text
	static readonly string REPLACE_DIALOG = "{0}\"{1}\", {2})"; // C.Jon.Say("hi", 123)
	static readonly string REPLACE_ASSIGN = "{0}\"&{1} {2}\""; // C.Jon.Description = "&123 hi"
	
	// Used for removing ids from text
	static readonly string REPLACE_DIALOG_NOID = "{0}\"{1}\")"; // C.Jon.Say("hi")
	static readonly string REPLACE_ASSIGN_NOID = "{0}\"{1}\""; // C.Jon.Description = "hi"

	static readonly string SCRIPT_DIALOG_FILE_START = @"<html><head><style>
		body{ text-align: center; }
		ul {
			margin-top: 20px;
			margin-bottom: 20px;
			margin-right: auto;
			margin-left: auto;

			list-style: none;
			max-width: 700px;
			background: #fff;
			padding: 5px 14px;
			text-align: left;
		}
		li { font: 12px/14px Courier, fixed; }
		.action {  font-style: italic; }
		.sceneheader, .action, .character { padding-top: 1.5ex; }
		.sceneheader  { font-weight: bold; text-transform:uppercase; }
		.action { padding-right: 5%; font- }
		.character {  margin-left: 40%; text-transform:uppercase;  }
		.dialogue { margin-left: 25%; min-width: 320px; padding-right: 5%; }
		.parenthetical { margin-left: 32%; padding-right: 10%; }
		/* special case: dialogue followed by a parenthetical; the extra line needs to be suppressed */
		.dialogue + .parenthetical { padding-bottom: 0; }
		.dialogue.recorded { color:#aaa; }
		.transition { padding-top: 3ex; margin-left: 65%; padding-bottom: 1.5ex; }
		.id { float:left; padding-right: 0; color:#aaa; text-align: right; width: 23%; }
		a:hover {text-decoration: underline; cursor: pointer;}
		</style>
		<script> function play(name) {(new Audio('./Voice/'+name+'.wav')).play(); } </script>"+"\n</head>\n<body><code><ul>";
	static readonly string SCRIPT_DIALOG_FILE_END = "</ul></code></body></html>";
	static readonly string SCRIPT_DIALOG_CHARACTER = "\t\t\t<li class=\"character\"><b>{0}</b></li>\n";
	static readonly string SCRIPT_DIALOG_CHARACTER_HIGHLIGHTED = "\t\t\t<li class=\"character\"><span style=\"background-color: #{0};\"><b>{1}</b></span></li>\n";
	static readonly string SCRIPT_DIALOG_LINE = "\t\t\t<li class=\"id\">(<a onclick=\"play('{0}{1}')\">{0}{1}</a>)</li><li class=\"dialogue\">{2}</li>\n";
	//static readonly string SCRIPT_DIALOG_LINE = "\t\t\t<li class=\"character\"><b>{0}</b> {1}</li><li class=\"dialogue\">{2}</li>\n";
	static readonly string SCRIPT_DIALOG_RECORDED_LINE = "\t\t\t<li class=\"id\">(<a onclick=\"play('{0}{1}')\">{0}{1}</a>)</li><li class=\"dialogue recorded\">{2}</li>\n";
	static readonly string SCRIPT_FILE_LINE = "\n\t<li class=\"sceneheader\">{0}</li>\n\n";
	static readonly string SCRIPT_FUNCTION_LINE = "\n\t\t<li class=\"action\">{0}</li>\n\n";

	private static readonly Color[] HIGHLIGHT_COLOURS =
	{
		// colors from stabilo-boss highligher color chart :P
		new Color(0.988f, 0.945f, 0.718f), //yellow
		new Color(0.737f, 0.922f, 0.914f), // blue-green
		new Color(0.969f, 0.843f, 0.886f),// pink-light
		new Color(0.922f, 0.969f, 0.737f), //green
		new Color(0.976f, 0.729f, 0.776f), // pink
		new Color(0.996f, 0.812f, 0.749f), // orange
		new Color(0.906f, 0.808f, 0.914f), // purple 
		new Color(0.996f, 0.659f, 0.584f), // red
		new Color(0.769f, 0.886f, 0.820f), // Green
		new Color(0.867f, 0.902f, 0.973f), // Blue
		// word highlight colors
		new Color(1.0f, 0.95f, 0.33f, 1.0f),
		new Color(1.0f, 0.26f, 0.26f, 1.0f),
		new Color(0.6f, 0.3f, 1.0f, 1.0f),
		new Color(1.0f, 0.3f, 1.0f, 1.0f),
		new Color(0.96f, 0.53f, 0.18f, 1.0f),
		new Color(0.3f, 0.5f, 0.9f, 1.0f),
		new Color(0.3f, 1.0f, 1.0f, 1.0f),
	};

	// NB: tooltipes moved to GuiContent where in the editor code
	//[Tooltip("Enable this when you'd already recorded some dialog lines/translated some text, so it doesn't get muddled up")]
	[SerializeField] bool m_preserveIds = true;
	//[Tooltip("If enabled, only spoken dialog lines are processed. Disable if you're planning on translating all game text")]
	[SerializeField] bool m_processDialogOnly = true;
	//[Tooltip("If enabled, when a section or function has been fully recorded, it will be omitted from the output script")]
	[SerializeField] bool m_skipFullyRecordedSections = false;
	//[Tooltip("Specify specific characters to export, comma seperated. Sections not including the character will be skipped. Specify 'Narr' for narrator.")]
	[SerializeField] string[] m_exportCharacters = null;
	//[Tooltip("Highlight specified characters names in the script to make them easier to see.")]
	[SerializeField] bool m_highlightCharacter = false;
	//[Tooltip("Specify specific quest object names to export, comma seperated. Others will be skipped. eg 'Forest, GlobalScript, Barney', also note special cases: 'characters, guis, items'")]
	[SerializeField] string[] m_exportScripts = null;	
	//[Tooltip("Comma separated list of language codes. Case sensitive, use the same case you put in Languages' codes")]
	[SerializeField] string[] m_exportLanguages = null;

	enum eCsvEncoding
	{
		Default,
		ASCII,
		UTF8,
		Unicode,
		Windows1252,
		Excel,
		GoogleSheets
	}
	[SerializeField] eCsvEncoding m_csvEncoding = eCsvEncoding.Excel;

	string m_currSourceFile = null;

	string m_sectionsOrder = null;

	public override void OnInspectorGUI()
	{
		m_component = (SystemText)target;
		
		//EditorGUILayout.LabelField("Settings",EditorStyles.boldLabel);		
		m_foldoutSettings = EditorGUILayout.Foldout(m_foldoutSettings,"Settings", true, m_foldoutSettings ? new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }: EditorStyles.foldout);
		if ( m_foldoutSettings )
		{				
			SerializedProperty defaultTextSourceProp =
				serializedObject.FindProperty("m_defaultTextSource");

			if (defaultTextSourceProp != null)
			{
				EditorGUILayout.PropertyField(defaultTextSourceProp);
			}

			SerializedProperty languagesProperty =
				serializedObject.FindProperty(
					"m_languages"
				);

			if (languagesProperty != null)
			{
				EditorGUILayout.PropertyField(languagesProperty);
			}
			GUILayout.Space(10);	
		}


		bool processText = false;
		bool processRemoveTextIds = false;

		//EditorGUILayout.LabelField("Process Game Text",EditorStyles.boldLabel);
		
		m_foldoutProcess = EditorGUILayout.Foldout(m_foldoutProcess,"Process Game Text", true, m_foldoutProcess ? new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }: EditorStyles.foldout);
		if ( m_foldoutProcess )
		{
			string helpText = string.Empty;
			if ( m_preserveIds )
				helpText = "Process Text reads through scripts and adds any text to this text system, assigning ids to each line of text.\n\nExisting dialog ids will not be changed.";
			else 
				helpText = "Process Text reads through scripts and adds any text to this text system, assigning ids to each line of text.\n\nDialog ids may be change.\nTick Preserve Ids if you've recorded dialog already.";

			if ( m_processDialogOnly )
				helpText += "\n\nOnly Spoken Dialog will be imported (for use in voice scripts)";
			else 
				helpText += "\n\nAll text will be imported (for voice scripts and translation)";
			EditorGUILayout.HelpBox(helpText, MessageType.Info);

			GUILayout.BeginHorizontal();
			m_preserveIds = GUILayout.Toggle( m_preserveIds, new GUIContent("Preserve Ids", "Enable this when you'd already recorded some dialog lines/translated some text, so it doesn't get muddled up") );
			m_processDialogOnly = GUILayout.Toggle( m_processDialogOnly, new GUIContent("Dialog Only","If enabled, only spoken dialog lines are processed. Disable if you're planning on translating all game text") );
			GUILayout.EndHorizontal();
		
		
			GUILayout.Space(5);
			processText = GUILayout.Button("Process Text", GUILayout.Height(25));
		
			if ( GUILayout.Button("Remove Unused Text Ids") )
			{
				if ( EditorUtility.DisplayDialog("Really remove ids from test system?","This will remove text from the text system and ids from the game scripts. If audio exists for voice lines, they will not be removed\n\n(Backup highly recommended!)","Removed unused ids","Cancel") )
					 processRemoveTextIds = true;
			}
			GUILayout.Space(10);
		}	

		// Script generation
		//EditorGUILayout.LabelField("Export Script",EditorStyles.boldLabel);
		
		m_foldoutScript = EditorGUILayout.Foldout(m_foldoutScript,"Script Export", true, m_foldoutScript ? new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }: EditorStyles.foldout);
		if ( m_foldoutScript )
		{
			EditorGUILayout.HelpBox("Export processed text to an html script. \nOptionally specify specific characters or scripts to export.", MessageType.Info);
			// Characters
			{
				EditorGUI.BeginChangeCheck();
				string inLabels = (m_exportCharacters != null && m_exportCharacters.Length > 0) ?  string.Join(LABEL_DELIMITER_READ, m_exportCharacters) : "";
				string outLabels = EditorGUILayout.TextField(
					new GUIContent("From Characters","Specify specific characters to export, comma seperated. Sections not including the character will be skipped. Specify 'Narr' for narrator."), 
					inLabels );

				if ( EditorGUI.EndChangeCheck() )
				{					
					//outLabels = outLabels.Replace(WHITESPACE,string.Empty);
					outLabels = outLabels.ToLower();
					m_exportCharacters = outLabels.Split(LABEL_DELIMITER_WRITE, System.StringSplitOptions.RemoveEmptyEntries);
				}

				m_highlightCharacter = EditorGUILayout.Toggle(
					new GUIContent("Highlight Character", "If enabled, it will highlight characters contained in the 'From Characters' set"),
					m_highlightCharacter
				);
			}

			// Scripts
			{
				EditorGUI.BeginChangeCheck();
				string inLabels = (m_exportScripts != null && m_exportScripts.Length > 0) ?  string.Join(LABEL_DELIMITER_READ, m_exportScripts) : "";
				string outLabels = EditorGUILayout.DelayedTextField(
					new GUIContent("From Scripts", "Specify specific quest object names to export, comma seperated. Others will be skipped. eg 'Forest, GlobalScript, Barney', also note special cases: 'characters, guis, items'" )
					, inLabels);
				if ( EditorGUI.EndChangeCheck() )
					m_exportScripts = outLabels.Split(LABEL_DELIMITER_WRITE, System.StringSplitOptions.RemoveEmptyEntries);
			
			}

			// Languages
			{
				EditorGUI.BeginChangeCheck();
				string inLabels = (m_exportLanguages != null && m_exportLanguages.Length > 0) ?  string.Join(LABEL_DELIMITER_READ, m_exportLanguages) : "";
				string outLabels = EditorGUILayout.TextField(
					new GUIContent("Languages", "Comma separated list of language codes. Case sensitive, use the same case you put in Languages' codes")
					, inLabels);
				if ( EditorGUI.EndChangeCheck() )
				{
					m_exportLanguages = outLabels.Split(LABEL_DELIMITER_WRITE, System.StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray<string>();
				}
			}
		
			m_skipFullyRecordedSections = EditorGUILayout.Toggle(
				new GUIContent("Skip Recorded Sections", "If this is enabled, sections where all the lines have been recorded will be omitted (recorded lines will not have their character names highlighted, if Highlight Character has been enabled), all lines except those that need to be recorded by the VA are greyed out"),
				m_skipFullyRecordedSections
			);
				
			
			if ( GUILayout.Button("Get Section Order"))
			{ 			
				List<TextData> list = m_component.EditorGetTextDataOrdered();
				DialogScript dialogScript = GatherDialog(list);				
				m_sectionsOrder = string.Empty;
				foreach ( DialogSection section in dialogScript.Sections )
				{
					string sectionName = section.Name + ": ";
					foreach ( DialogSubsection subsection in section.Subsections )
					{	
						m_sectionsOrder += sectionName + subsection.Name + '\n';
					}
				}

			}

			if ( string.IsNullOrWhiteSpace(m_sectionsOrder) == false  )				
				m_sectionsOrder = EditorGUILayout.TextArea(m_sectionsOrder);
		
			GUILayout.Space(5);

			if ( GUILayout.Button("Generate Script", GUILayout.Height(25)) )
			{
				GenerateScript( m_component );
			}
			GUILayout.Space(10);	
		}

		// Script generation
		//EditorGUILayout.LabelField("Export/Import to CSV",EditorStyles.boldLabel);
		
		bool processUpdateGameTextFromImport = false;
		m_foldoutCSV = EditorGUILayout.Foldout(m_foldoutCSV,"CSV Export/Import", true, m_foldoutCSV ? new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }: EditorStyles.foldout);
		if ( m_foldoutCSV )
		{
			EditorGUILayout.HelpBox("Export processed text to CSV for translation or external editing, then import again.", MessageType.Info);

			m_csvEncoding =  (eCsvEncoding) EditorGUILayout.EnumPopup("Csv Encoding", (Enum)m_csvEncoding);

			GUILayout.BeginHorizontal();
			if ( GUILayout.Button("Export to CSV", GUILayout.Height(25)) )
			{
				ExportToCSV( m_component );
			}
			if ( GUILayout.Button("Import from CSV", GUILayout.Height(25)) )
			{
				EditorUtility.DisplayProgressBar("Importing from CSV", "Importing text",0);
				// Import
				bool importSuccess = ImportFromCSV( m_component );
			
				EditorUtility.ClearProgressBar();
			
				// If import successful, ask about updating game text too.
				if ( importSuccess && m_component.EditorGetShouldImportDefaultStringFromCSV() )
				{
					if ( EditorUtility.DisplayDialog("CSV Import Succeeded","Success!\n\nDo you also want to update the text in your game code/scripts and prefabs?\n\n(Backup highly recommended!)","Update Scripts and Prefabs","No thanks") )
						processUpdateGameTextFromImport = true;
				}
			}	

			GUILayout.EndHorizontal();	
			GUILayout.Space(10);	
		}
		/*
		m_advancedFoldout = EditorGUILayout.Foldout(m_advancedFoldout,"Advanced", true);
		if ( m_advancedFoldout )
		{
			if ( GUILayout.Button("Update scripts from imported text") )
			{
				if ( EditorUtility.DisplayDialog("Update scripts?","Do you want to update the text in your game code/scripts and prefabs from imported text?\n\n(Backup highly recommended!)","Update Scripts and Prefabs","Cancel") )
					processUpdateGameTextFromImport = true;
			}
			
		}*/


		//EditorGUILayout.LabelField("Lip Syncing",EditorStyles.boldLabel);		
		m_foldoutLipSync = EditorGUILayout.Foldout(m_foldoutLipSync,"Lip Sync", true, m_foldoutLipSync ? new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }: EditorStyles.foldout);
		if ( m_foldoutLipSync )
		{
			EditorGUILayout.HelpBox("Generate lip sync data. Requires voice acting files to be present, and Rhubarb to be installed", MessageType.Info);
		
			SerializedProperty lipSyncingProperty =
				serializedObject.FindProperty("m_lipSyncExtendedShapes");

			if (lipSyncingProperty != null)
			{
				EditorGUILayout.PropertyField(lipSyncingProperty);
				//Tooltip("If ticked, lipsync data is re-created even for lines that already have it.")
				m_skipExistingLipsyncData = GUILayout.Toggle(m_skipExistingLipsyncData,"Skip existing lipsync data");
			}
		
			if ( GUILayout.Button("Process Lip Sync Data") )
			{
				PowerQuestEditor.GetPowerQuestEditor().RunRhubarb();
			}

			GUILayout.Space(10);
		}

		if ( processText )
		{
			EditorUtility.DisplayProgressBar("Processing text", "Processing text",0);
			m_processTextMode = eProcessTextMode.ProcessFromScript;
			ProcessAllText(m_component);
			
			EditorUtility.DisplayProgressBar("Processing text", "Refreshing asset database",0);

			EditorUtility.SetDirty(target);	
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			EditorUtility.ClearProgressBar();
		}
		else if ( processUpdateGameTextFromImport )
		{
			EditorUtility.DisplayProgressBar("Updating game text", "Updating game text",0);
			m_preserveIds=true;
			m_processDialogOnly=false;
			m_processTextMode = eProcessTextMode.UpdateScript;
			ProcessAllText(m_component);			
			EditorUtility.DisplayProgressBar("Updating game text", "Refreshing asset database",0);
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			EditorUtility.ClearProgressBar();
		}
		else if ( processRemoveTextIds )
		{
			EditorUtility.DisplayProgressBar("Removing ids", "Removing ids",0);
			m_preserveIds=true;
			m_processDialogOnly=false;
			m_processTextMode = eProcessTextMode.UpdateScript;

			// First remove lines from the text system (if they don't have recorded dialog)
			m_component.EditorGetTextDataOrdered().RemoveAll(
				item=>
				{
					return m_component.EditorHasAudio(item.m_id,item.m_character) == false;
				});
			// Then process text in update mode to update game text
			ProcessAllText(m_component);		
			EditorUtility.DisplayProgressBar("Removing ids", "Refreshing asset database",0);	
			EditorUtility.SetDirty(target);	
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			EditorUtility.ClearProgressBar();
		}

	}



	static void GetFilePaths(string startingDirectory, string extention, ref List<string> paths)
  	{
		try
		{
			string[] files = Directory.GetFiles(startingDirectory);
			for ( int j = 0; j < files.Length; ++j )
			{
				string file = files[j];
				if ( file.EndsWith(extention) )
				{
					paths.Add(file);
				}
			}

			string[] directories = Directory.GetDirectories(startingDirectory);
			for ( int i = 0; i < directories.Length; ++i )
			{
				string dir = directories[i];
				GetFilePaths(dir, extention, ref paths);
			}
		}
		catch (System.Exception excpt)
		{
			Debug.LogError(excpt.Message);
		}
	}

	//
	// 
	//
	void ProcessFile( string filePath )
	{
		filePath = Path.GetFullPath(filePath);
		if ( m_processedFiles.Exists(item=>item == filePath) )
			return;
		try 
		{
			string content = File.ReadAllText(filePath);
			content = REGEX_DIALOG_METHOD.Replace( content, EvaluateDialogMatch );
			File.WriteAllText( filePath,  content );
			m_processedFiles.Add(filePath);
		} 
		catch (System.Exception ex) 
		{
			if ( ex is System.IO.FileNotFoundException )
			{
				//Debug.LogFormat("Skipping non-existant script: {0}", filePath);
			}
			else 
			{
				Debug.LogWarningFormat("Failed to process text in {0}.\n\nError: {1}",filePath, ex.ToString());
			}
		}
	}
	

	// Trawls through the game and adds text to the manager, inserting IDs into script files where it feels like it
	public void ProcessAllText( SystemText systemText )
	{
		if ( PowerQuestEditor.IsReady() == false )
			return;
			
		if ( PowerQuestEditor.GetPowerQuestEditor().GetSmartCompileRequired() )
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			EditorUtility.DisplayDialog("Asset database requires import", "The asset database requires updating first, wait a few seconds then try again. If that doesn't work, try playing and stopping your game","Ok");
			return;
		}

		// Clear list of processed files before starting processing again
		m_processedFiles.Clear();

		m_component = systemText;

		PowerQuest powerQuest = PowerQuestEditor.GetPowerQuest();
		if ( powerQuest == null )
			return;

		// Loop through all script files and run teh regex over them
		if ( m_processTextMode != eProcessTextMode.UpdateScript )
			systemText.EditorOnBeginAddText();

		// Process Room scripts
		foreach ( RoomComponent component in powerQuest.GetRoomPrefabs() )
		{
			m_currSourceFile = STR_ROOM+component.GetData().ScriptName;
			ProcessFile( QuestEditorUtils.GetFullPath(component.gameObject, "Room" + component.GetData().GetScriptName() +".cs") ); 

			if ( m_processDialogOnly == false )
			{
				m_lastFunction = "Prop Description";
				component.GetPropComponents().ForEach( item=> 
					{
						if ( item != null )
						{
							bool hasCollider = item.GetComponent<Collider2D>() != null;
							if ( hasCollider )
								item.GetData().Description = AddStringWithEmbeddedId( item.GetData().Description);
						}
					} );
				m_lastFunction = "Hotspot Description";
				component.GetHotspotComponents().ForEach( item=> 
					{ 
						if ( item != null )
							item.GetData().Description = AddStringWithEmbeddedId( item.GetData().Description); 
					} );
				EditorUtility.SetDirty(component);
			}
		}

		// Process Character scripts
		foreach ( CharacterComponent component in powerQuest.GetCharacterPrefabs() )
		{
			m_currSourceFile = "Character- "+component.GetData().ScriptName;
			ProcessFile( QuestEditorUtils.GetFullPath(component.gameObject, PowerQuest.STR_CHARACTER + component.GetData().GetScriptName() +".cs") ); 

			if ( m_processDialogOnly == false )
			{
				m_lastFunction = "Character Description";
				component.GetData().Description = AddStringWithEmbeddedId( component.GetData().Description);
				EditorUtility.SetDirty(component);
			}
		}

		// Process Inventory scripts
		foreach ( InventoryComponent component in powerQuest.GetInventoryPrefabs() )
		{
			m_currSourceFile = "Item- "+component.GetData().ScriptName;
			ProcessFile( QuestEditorUtils.GetFullPath(component.gameObject, PowerQuest.STR_INVENTORY + component.GetData().GetScriptName() +".cs") ); 

			if ( m_processDialogOnly == false )
			{
				m_lastFunction = "Item Description";
				component.GetData().Description = AddStringWithEmbeddedId(component.GetData().Description);
				EditorUtility.SetDirty(component);
			}
		}

		// Process Dialog scripts
		foreach ( DialogTreeComponent component in powerQuest.GetDialogTreePrefabs() )
		{
			m_currSourceFile = "Dialog- "+component.GetData().ScriptName;
			ProcessFile( QuestEditorUtils.GetFullPath(component.gameObject, "Dialog" + component.GetData().GetScriptName() +".cs") ); 

			if ( m_processDialogOnly == false )
			{
				m_lastFunction = "Dialog Option";
				component.GetData().Options.ForEach( item=> 
					{ 
						item.Text = AddStringWithEmbeddedId(item.Text); 
					} );	
				EditorUtility.SetDirty(component);
			}
		}		

		// Process Game script
		m_currSourceFile = "Global Script";
		ProcessFile( PowerQuestEditor.PATH_GLOBAL_SCRIPT );

		// Process Guis
		foreach ( GuiComponent component in powerQuest.GetGuiPrefabs() )
		{					
			m_currSourceFile = "Gui- "+component.GetData().ScriptName;
			ProcessFile( QuestEditorUtils.GetFullPath(component.gameObject, "Gui" + component.GetData().GetScriptName() +".cs") ); 

			if ( m_processDialogOnly == false )
			{
				m_lastFunction = "Gui Description";
				GuiComponent[] childComponents = component.GetComponentsInChildren<GuiComponent>(true);
				foreach ( GuiComponent item  in childComponents )
				{
					item.GetData().Description = AddStringWithEmbeddedId(item.GetData().Description);
				}
				
				m_lastFunction = "Gui Control Description";
				GuiControl[] childControlComponents = component.GetComponentsInChildren<GuiControl>(true);
				foreach ( GuiControl item  in childControlComponents )
				{
					item.Description = AddStringWithEmbeddedId(item.Description);
				}

				// Look for [QuestLocalizable] attribute in guis. This attribute/function/system could be extended to more random components I guess.
				//ProcessLocalizableFieldAttributes(component.transform);

				EditorUtility.SetDirty(component);
			}
		}

		if ( m_processDialogOnly == false )
		{
			// Process QuestText components that may need localising
			m_currSourceFile = "General Text";
			ProcessQuestText();

			// Process all "SystemText.Localise" calls in game directory (that haven't been processed yet)
			List<string> paths = new List<string>();
			GetFilePaths(@Path.Combine("Assets", "Game"), ".cs", ref paths );
			foreach( string path in paths)
			{
				m_currSourceFile = Path.GetFileNameWithoutExtension(path);
				m_lastFunction = "";
				ProcessFile(path);
			}

			// Look for [QuestLocalizable] attribute. I assume this will be suuuuuuper slow lol.
			ProcessLocalizableFieldAttributes();
		}
	}

	string AddStringWithEmbeddedId(string line)
	{		
		if ( string.IsNullOrEmpty(line) )
			return line;
		int existingId = m_component.ParseIdFromText(ref line);		
		TextData data = ProcessTextLine(line, m_currSourceFile,m_lastFunction,null,existingId,m_preserveIds);
		if ( data == null )
			return line;
		return string.Format("&{0} {1}", data.m_id.ToString(), data.m_string);
	}

	//
	//
	//
	string m_lastFunction = null;
	public string EvaluateDialogMatch( Match match )
	{	
		if ( m_component == null || match.Groups == null)
			return match.Value;

		// If it matches the function part of the expression, we just record that group and continue
		string function = match.Groups["function"].Value;
		if ( string.IsNullOrEmpty(function) == false  )
		{
			m_lastFunction = function;
			return match.Value;
		}

		string result = match.Value;
			
		string start = match.Groups["start"].Value;
		string character = match.Groups["character"].Value;
		string text = match.Groups["text"].Value;
		string existingId = match.Groups["id"].Value;
		string assignStart = match.Groups["assignStart"].Value;
		bool isAssignment = string.IsNullOrEmpty(assignStart) == false;

		if ( string.IsNullOrEmpty(text) )
			return match.Value;
		
		if ( isAssignment == false && string.IsNullOrEmpty(start) )
			return match.Value;


		int id = -1;
		if ( int.TryParse(existingId, out id) == false )
		{
			id = -1;
		}

		// if no character name, then it's the narrator
		if ( isAssignment )
		{
			character = null;
		}
		else if ( string.IsNullOrEmpty(character) )
		{
			character = PowerQuest.STR_NARR; // Hack to set chracter to "Narr" when it's a display function
		}
		else if ( character == "Section" )
		{
			m_lastFunction = Regex.Unescape(text); // Hack for "Section"
			return match.Value;
		}
		else if ( character == "Localize" ) 
		{
			character = null;
		}

		if ( character == null && m_processDialogOnly )
			return match.Value; // Ignore non-dialog
			
		text = Regex.Unescape(text);

		TextData textData = ProcessTextLine(text, m_currSourceFile, m_lastFunction, character, id, m_preserveIds);
		if ( textData != null )
			text = textData.m_string;

		// replace carriage returns in text, before saving it back into script
		text = REGEX_NEWLINE.Replace(text,"\\n");
				
		if ( textData != null )
		{
			id = textData.m_id;

			if ( isAssignment ) // Final string is .Description = "&123 Blah"
				result = string.Format(REPLACE_ASSIGN, assignStart, id.ToString(), text); 
			else // Final string is  C.Jon.Say("blah", 123);
				result = string.Format(REPLACE_DIALOG, start, text, id.ToString()); 
		}
		else 
		{
			// If not found, remove the id from the in-game text
			if ( isAssignment ) // Final string is .Description = "&123 Blah"
			{
				m_component.ParseIdFromText(ref text); // strip id from text
				result = string.Format(REPLACE_ASSIGN_NOID, assignStart, text); 
			}
			else // Final string is  C.Jon.Say("blah", 123);			
				result = string.Format(REPLACE_DIALOG_NOID, start, text); 
		}

		return result;
	}


	// If processting text, adds the line to text data. If merging processed text, reads it instead.
	TextData ProcessTextLine( string line, string sourceFile = null, string sourceFunction = null, string characterName = null, int existingId = -1, bool preserveExistingIds = false)
	{
		if ( m_processTextMode == eProcessTextMode.UpdateScript )
			return m_component.EditorFindText(line, existingId, characterName);
		
		return m_component.EditorAddText(line, sourceFile, sourceFunction, characterName, existingId, preserveExistingIds);
	}

	void ProcessQuestText()
	{
		List<string> paths = new List<string>();
		GetFilePaths("Assets", ".prefab", ref paths );

		for ( int i = 0; i < paths.Count; ++i )
		{
			GameObject obj = AssetDatabase.LoadMainAssetAtPath(paths[i]) as GameObject;
			if ( obj != null )
			{						
				bool dirty = false;
				m_lastFunction = "Text in "+ obj.name;
				QuestText[] textObjects = obj.GetComponentsInChildren<QuestText>(true);
				foreach( QuestText textObj in textObjects )
				{
					if ( textObj.GetShouldLocalize() )
					{
						textObj.SetText( AddStringWithEmbeddedId(textObj.GetUnlocalizedText()) );
						dirty = true;
					}
				}
				if ( dirty )
					EditorUtility.SetDirty(obj);	
			}
		}
	}

	void ProcessLocalizableFieldAttributes()
	{
		List<string> paths = new List<string>();
		GetFilePaths("Assets", ".prefab", ref paths );

		for ( int i = 0; i < paths.Count; ++i )
		{
			GameObject obj = AssetDatabase.LoadMainAssetAtPath(paths[i]) as GameObject;
			if ( obj != null )
			{
				m_lastFunction = "Text in "+ obj.name;				
				if ( ProcessLocalizableFieldAttributes(obj.transform) )
					EditorUtility.SetDirty(obj);	
					
			}
		}
	}

	// Finds [QuestLocalizable] attribute on string fields, and adds that text to system. returns true if found
	public bool ProcessLocalizableFieldAttributes(Transform transform, bool root = true)
	{
		bool result = false;
		for ( int i = 0; i < transform.childCount; ++i )
			result |= ProcessLocalizableFieldAttributes(transform.GetChild(i), false );

		Component[] components = transform.GetComponents<Component>();
		
		BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		foreach( Component component in components) 
		{
			foreach(FieldInfo fieldInfo in component.GetType().GetFields(flags))
			{
				if ( fieldInfo.GetCustomAttributes(typeof(QuestLocalizeAttribute),true).Count() > 0 )
				{					
					if ( fieldInfo.FieldType == typeof(string))
					{
						string val = fieldInfo.GetValue(component) as string;
						if ( string.IsNullOrEmpty(val) == false )
						{
							val = AddStringWithEmbeddedId(val);
							fieldInfo.SetValue(component, val); 
							// Debug.Log("Found: "+ val);
							result = true;
						}
					}
				}
			}
		}
		return result;
	}

	static readonly string STR_ROOM = "Room- ";


	private string[] GetLanguageCodes(SystemText systemText) { return systemText.GetLanguages().Select(language => language.m_code).ToArray<string>(); }

	private int GetLanguageIndexByCode(SystemText systemText, string languageCode = null) {
		// find the language index by code
		if (languageCode == null)
			return -1;
		return Array.IndexOf(GetLanguageCodes(systemText), languageCode);
	}

	private bool HasRecording(TextData data)
	{
        string fullFileName = data.m_character + data.m_id.ToString();			
        AudioClip clip = Resources.Load("Voice/" + fullFileName) as AudioClip;

        if (clip == null)
        {
	        return false;
        }

        return true;
	}

	// Generates a screenplay style script for recording dialog
	public void GenerateScript( SystemText systemText )
	{
		if ( m_exportLanguages != null && m_exportLanguages.Length > 0)	{
			foreach(string languageCode in m_exportLanguages) {
				// if the lang code is not a valid one, do nothing
				if (!GetLanguageCodes(systemText).Contains(languageCode)) {
					Debug.LogWarning($"Language index not found for code {languageCode}. Mind the code is case sensitive.");
					continue;
				}
				// else generate a script with the selected language
				GenerateScriptForLanguage(systemText, languageCode);
			}
		} 
		else 
		{
			// if no language specified, use the predefined one (generate only one file)
			GenerateScriptForLanguage(systemText, systemText.GetLanguages()[0].m_code);
		}
	}

	void GenerateScriptForLanguage(SystemText systemText, string languageCode = null)
	{
		// A lang code MUST be always specified or no script will be generated.
		if (languageCode == null)
		{
			Debug.LogWarning("Script generation invoked without language code. Exiting.");
			return;
		}

		int languageIndex = GetLanguageIndexByCode(systemText, languageCode);
		if (languageIndex < 0)
		{
			Debug.LogWarning($"Language index not found for code {languageCode}. Exiting.");
			return;
		}

		m_component = systemText;

		Dictionary<string, Color> colourCache = new Dictionary<string, Color>();

		System.Text.StringBuilder builder = new System.Text.StringBuilder();		
		builder.Append(SCRIPT_DIALOG_FILE_START);
		// builder.AppendFormat(SCRIPT_DIALOG_LINE, "fred", "23", "blah di blah di blah blah blah");

		List<TextData> list = systemText.EditorGetTextDataOrdered();
		DialogScript dialogScript = GatherDialog(list);

		foreach (DialogSection section in dialogScript.Sections)
		{
			builder.AppendFormat(SCRIPT_FILE_LINE, section.Name);
			
			foreach (DialogSubsection subsection in section.Subsections)
			{
				builder.AppendFormat(SCRIPT_FUNCTION_LINE, subsection.Name);

				foreach (CharacterBlock block in subsection.CharacterBlocks)
				{
					if (m_highlightCharacter && block.SatisifiesFilter(m_exportCharacters) && (!m_skipFullyRecordedSections || block.HasUnrecordedLines))
					{
						// For the character name to be highlighted,
						// the Highlight Character checkbox must be set,
						// But if "skip fully recorded sections" has been selected,
						// then we only highlight the character name if the actor still
						// has to record the line. They wouldn't be interested in
						// it otherwise except for the purposes of getting context.

						string lowerCaseChar = block.CharacterName.ToLower();

						if (!colourCache.TryGetValue(lowerCaseChar, out Color highlightColour))
						{
							for (int i = 0; i < m_exportCharacters.Length; ++i)
							{
								if (m_exportCharacters[i] == lowerCaseChar)
								{
									int colourIndex = i % HIGHLIGHT_COLOURS.Length;

									if (!HIGHLIGHT_COLOURS.IsIndexValid(colourIndex))
									{
										Debug.LogError($"Computed invalid highlight colour index: {colourIndex}, array length: {HIGHLIGHT_COLOURS.Length}");

										colourIndex = 0;
									}

									highlightColour = HIGHLIGHT_COLOURS[colourIndex];
									colourCache.Add(lowerCaseChar, highlightColour);
									break;
								}
							}
						}

						builder.AppendFormat(SCRIPT_DIALOG_CHARACTER_HIGHLIGHTED, ColorUtility.ToHtmlStringRGBA(highlightColour), block.CharacterName);
					}
					else
					{
						builder.AppendFormat(SCRIPT_DIALOG_CHARACTER, block.CharacterName);
					}

					bool blockIsPartiallyRecorded = block.HasBothRecordedAndUnrecordedLines;

					foreach (CharacterDialogLine line in block.Lines)
					{

						bool recorded = line.IsRecorded;

						// here we decide whether to grey out the dialog line,
						string dialogFmt = blockIsPartiallyRecorded && recorded && block.SatisifiesFilter(m_exportCharacters) && m_skipFullyRecordedSections
							? SCRIPT_DIALOG_RECORDED_LINE
							: SCRIPT_DIALOG_LINE;

						if (languageIndex > 0)
						{
							// index is greater than zero so we want one of the translations
							// TODO: check if the index is never out of bound
							builder.AppendFormat(dialogFmt, block.CharacterName, line.Id.ToString(), line.Translations[languageIndex - 1]); // can be an empty string
						}
						else
						{
							// index is 0 so we refer to the primary language
							builder.AppendFormat(dialogFmt, block.CharacterName, line.Id.ToString(), line.DialogLine);
						}
					}
				}
			}
		}

		builder.Append(SCRIPT_DIALOG_FILE_END);


		string scriptPath = EditorUtility.SaveFilePanel("Save Script File", "", $"Script-{languageCode}.html", "html");
		if (string.IsNullOrEmpty(scriptPath) == false)
		{
			File.WriteAllText(scriptPath, builder.ToString());
			Application.OpenURL(scriptPath);
		}

	}

	/// <summary>
	/// Do a first pass and collate the dialog lines and group them together,
	/// noting down the important information along the way...
	/// </summary>
	/// <param name="list">List of Text Data to parse</param>
	/// <returns>The collated dialog data</returns>
	private DialogScript GatherDialog(List<TextData> list)
	{
		DialogScript dialog = new DialogScript();

		List<TextData> unprocessedTextData = new List<TextData>();
		unprocessedTextData.AddRange(list);

		while (TryGetDialogSection(unprocessedTextData, out DialogSection section))
		{
			dialog.Sections.Add(section);
		}

		// Remove sections we don't want to include
		{
			List<DialogSection> toRemove = new List<DialogSection>();

			foreach (DialogSection section in dialog.Sections)
			{
				// If scripts are specified, only export specified scripts
				if (m_exportScripts != null && m_exportScripts.Length > 0)
				{
					bool includeGlobals = System.Array.Exists(m_exportScripts, item => item.EqualsIgnoreCase("globalscript"));
					bool includeItems = System.Array.Exists(m_exportScripts, item => item.EqualsIgnoreCase("items"));
					bool includeGuis = System.Array.Exists(m_exportScripts, item => item.EqualsIgnoreCase("guis"));
					bool includeCharacters = System.Array.Exists(m_exportScripts, item => item.EqualsIgnoreCase("characters"));

					string scriptName = section.Name;
					bool found = false;
					found |= includeGlobals && scriptName.StartsWithIgnoreCase("Global");
					found |= includeItems && scriptName.StartsWithIgnoreCase("Item");
					found |= includeGuis && scriptName.StartsWithIgnoreCase("Gui");
					found |= includeCharacters && scriptName.StartsWithIgnoreCase("Character");

					if (found == false)
					{
						// Ignore everything before the space- (eg: "Room- Forest" becomes "Forest");
						int spaceIndex = scriptName.LastIndexOf(' ');
						if (spaceIndex >= 0)
							scriptName = scriptName.Substring(spaceIndex + 1);
						if (System.Array.Exists(m_exportScripts, item => item.EqualsIgnoreCase(scriptName)) == false)
						{ 
							toRemove.Add(section);
							continue;
						}
					}
				}

				if (section.IsEmpty(m_exportCharacters, m_skipFullyRecordedSections))
				{
					toRemove.Add(section);
					continue;
				}
				
				for ( int i = section.Subsections.Count-1; i >= 0; --i )
				{
					DialogSubsection subsection = section.Subsections[i];
					if (subsection.IsEmpty(m_exportCharacters, m_skipFullyRecordedSections))
						section.Subsections.RemoveAt(i);
				}
				if ( section.Subsections.Count <= 0 )
				{ 
					toRemove.Add(section);
					continue;
				}
			}

			foreach (DialogSection section in toRemove)
			{ 
				dialog.Sections.Remove(section);
			}
		}

		// Order sections based on names in m_sectionsOrder		
		string[] sectionNamesOrdered = {string.Empty};
		if ( m_sectionsOrder != null )
			sectionNamesOrdered = m_sectionsOrder.Split(new char[]{'\n','\r'});
		
		DialogSection currSection = null;

		List<DialogSection> orderedSections = new List<DialogSection>();

		foreach ( string orderName in sectionNamesOrdered )
		{	
			int splitId = orderName.IndexOf(':');
			if ( splitId <= 0)
				continue;
			string sectionName = orderName.Substring(0,splitId);
			string subsectionName = orderName.Substring(splitId+2); // skip colon and space ": "

			if ( currSection == null || sectionName.EqualsIgnoreCase(currSection.Name) == false )
			{ 
				// new Section
				currSection = new DialogSection(sectionName);
				orderedSections.Add(currSection);
			}

			// find and add subsection- removing it from old section. If old
			DialogSection oldSection = dialog.Sections.Find(item=>item.Name.EqualsIgnoreCase(sectionName));
			if (oldSection != null )
			{
				DialogSubsection subsection = oldSection.Subsections.Find(item=>item.Name.EqualsIgnoreCase(subsectionName));
				if ( subsection != null )
				{ 
					oldSection.Subsections.Remove(subsection);
					currSection.Subsections.Add(subsection);
					if (oldSection.Subsections.Count <= 0)
						dialog.Sections.Remove(oldSection);
				}
			}
		}
		// Add ordered sections at start of dialog sections list
		dialog.Sections.InsertRange(0,orderedSections);
		/* // sections only way
		foreach ( string orderName in sectionNamesOrdered )
		{	

			DialogSection section = dialog.Sections.Find(item=>item.Name.EqualsIgnoreCase(orderName));
			if (section != null )
			{ 
				dialog.Sections.Remove(section);
				dialog.Sections.Insert(orderId,section);
				orderId++;
			}
		}
		*/
		
		return dialog;
	}

	/// <summary>
	/// Try and get a section (A section might correspond to a specific script file or something)
	/// from <paramref name="unprocessedTextData"/> and return it in <paramref name="dialogSection"/> if one is found.
	/// </summary>
	/// <param name="unprocessedTextData">The list of <see cref="TextData"/> that has not yet been processed, elements will be removed from this if a Section has been successfully formed from them</param>
	/// <param name="dialogSection">The Section that has been found, if one has been found, will be set to null otherwise.</param>
	/// <returns>true if a section has been found, false otherwise (if for example <paramref name="unprocessedTextData"/> is empty).</returns>
	private bool TryGetDialogSection(List<TextData> unprocessedTextData, out DialogSection dialogSection)
	{
		if (unprocessedTextData.Count == 0)
		{
			dialogSection = null;
			return false;
		}

		dialogSection = new DialogSection(unprocessedTextData[0].m_sourceFile);

		while (unprocessedTextData.Count > 0 && unprocessedTextData[0].m_sourceFile == dialogSection.Name)
		{
			if (TryGetDialogSubsection(unprocessedTextData, dialogSection.Name, out DialogSubsection subsection))
			{
				dialogSection.Subsections.Add(subsection);
			}
			else
			{
				if (unprocessedTextData == null || unprocessedTextData.Count == 0 || unprocessedTextData[0] == null)
				{
					Debug.LogError("Failed to extract a DialogSection.");
				}
				else
				{
					TextData textData = unprocessedTextData[0];
					
					Debug.LogError($"Failure occurred around {textData.m_sourceFile}:{textData.m_sourceFunction}");
				}

				dialogSection = null;

				return false;
			}
		}
		
		return true;
	}

	/// <summary>
	/// This will try and gather together a block of consecutive dialog lines from the same function within a file...
	/// </summary>
	/// <param name="unprocessedTextData">The TextData that has not been processed yet.</param>
	/// <param name="sectionName">The name of the current section</param>
	/// <param name="dialogSubsection">The sub section retrieved, set to null if nothing is found</param>
	/// <returns>true if a subsection is found and false otherwise.</returns>
	private bool TryGetDialogSubsection(List<TextData> unprocessedTextData, string sectionName, out DialogSubsection dialogSubsection)
	{
		if (unprocessedTextData == null || unprocessedTextData.Count == 0)
		{
			dialogSubsection = null;
			return false;
		}

		dialogSubsection = new DialogSubsection(unprocessedTextData[0].m_sourceFunction);

		while (unprocessedTextData.Count > 0 && unprocessedTextData[0].m_sourceFile == sectionName && unprocessedTextData[0].m_sourceFunction == dialogSubsection.Name)
		{
			if (TryGetCharacterBlock(unprocessedTextData, sectionName, dialogSubsection.Name, out CharacterBlock characterDialogBlock))
			{
				dialogSubsection.CharacterBlocks.Add(characterDialogBlock);
			}
			else
			{
				if (unprocessedTextData.Count == 0)
				{
					// Finished processing all text data and ran out of character dialog
					// (the rest of the input was skipped because it doesnt belong in the
					// script..).
					return true;
				}
				
				if (unprocessedTextData[0] == null)
                {
                	Debug.LogError("Failed to extract a DialogSubsection.");
                }
                else
                {
	                TextData textData = unprocessedTextData[0];

	                if (textData.m_sourceFile != sectionName || textData.m_sourceFunction != dialogSubsection.Name)
	                {
		                // we just traversed into a new (sub)section, not a problem...
		                return true;
	                }
                	Debug.LogError($"Failure occurred around {textData.m_sourceFile}:{textData.m_sourceFunction}");
                }

				dialogSubsection = null;
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// This will pick off the running sequence of dialog from the same character from the beginning of <paramref name="unprocessedTextData"/>
	/// </summary>
	/// <param name="unprocessedTextData">List of <see cref="TextData"/> that hasn't been processed yet, elements will be popped off from this list as they get processed.</param>
	/// <param name="sectionName">The name of the current section</param>
	/// <param name="subsectionName">The name of the current subsection</param>
	/// <param name="characterBlock">If a <see cref="CharacterBlock"/> is found, this is where it will be returned</param>
	/// <returns>true if a <see cref="CharacterBlock"/> was found, false otherwise.</returns>
	private bool TryGetCharacterBlock(List<TextData> unprocessedTextData, string sectionName, string subsectionName, out CharacterBlock characterBlock)
	{
		// skip non-character lines
		while (unprocessedTextData.Count > 0 && string.IsNullOrEmpty(unprocessedTextData[0].m_character))
		{
			unprocessedTextData.RemoveAt(0);
		}
		
		if (unprocessedTextData.Count == 0)
		{
			characterBlock = null;
			return false;
		}

		characterBlock = new CharacterBlock(unprocessedTextData[0].m_character);

		while (unprocessedTextData.Count > 0 && unprocessedTextData[0] != null && !string.IsNullOrEmpty(unprocessedTextData[0].m_character) && unprocessedTextData[0].m_sourceFile == sectionName && unprocessedTextData[0].m_sourceFunction == subsectionName && unprocessedTextData[0].m_character == characterBlock.CharacterName)
		{
			if (TryGetCharacterLine(unprocessedTextData, out CharacterDialogLine line))
			{
				characterBlock.Lines.Add(line);

				continue;
			}

			if (unprocessedTextData.Count == 0 || unprocessedTextData[0] == null)
			{
				Debug.LogError("Failed to extract a CharacterBlock.");
			}
			else
			{
				TextData textData = unprocessedTextData[0];

				Debug.LogError($"Failure occurred around {textData.m_sourceFile}:{textData.m_sourceFunction}");
			}

			characterBlock = null;
			return false;
		}

		if (characterBlock.Lines.Count == 0)
		{
			characterBlock = null;
			return false;
		}

		return true;
	}

	/// <summary>
	/// Try and get a single line of dialog from <paramref name="unprocessedTextData"/>, removing it from the list if so.
	/// </summary>
	/// <param name="unprocessedTextData">The list of <see cref="TextData"/>, if a line of dialog can be found here, it will be removed from the list</param>
	/// <param name="line">If a line of dialog is found, this is where it will go...</param>
	/// <returns>true if dialog was found, false otherwise...</returns>
	private bool TryGetCharacterLine(List<TextData> unprocessedTextData, out CharacterDialogLine line)
	{
		if (unprocessedTextData.Count == 0)
		{
			Debug.LogError("Expected to find dialog line but ran out!");
			line = null;
			return false;
		}

		TextData data = unprocessedTextData[0];

		line = new CharacterDialogLine(
			data.m_id,
			data.m_string,
			data.m_translations
		);

		if (HasRecording(data))
		{
			line.MarkAsRecorded();
		}
		
		unprocessedTextData.RemoveAt(0);

		return true;
	}

	static readonly string CSV_HEADERS = "Character,ID,File,Context";
	static readonly int CSV_NUM_HEADERS = 4;	
	static readonly int CSV_INDEX_LANGUAGES = CSV_NUM_HEADERS;

	public void ExportToCSV( SystemText systemText  )
	{

		m_component = systemText;

		//string lastFile = null;
		//string lastFunction = null;

		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		List<TextData> list = systemText.EditorGetTextDataOrdered();
		builder.Append(CSV_HEADERS);

		// Add languages
		foreach (LanguageData language in systemText.GetLanguages())
		{
			builder.Append(",");
			builder.Append(language.m_description);
		}
		// builder.AppendFormat(SCRIPT_DIALOG_LINE, "fred", "23", "blah di blah di blah blah blah");
		foreach( TextData data in list )
		{

			builder.Append("\n\"");
			builder.Append(data.m_character);
			builder.Append("\",\"");
			builder.Append(data.m_id);
			builder.Append("\",\"");
			builder.Append(data.m_sourceFile);
			builder.Append("\",\"");
			builder.Append(data.m_sourceFunction);
			builder.Append("\",\"");
			// this is first language
			builder.Append(data.m_string.Replace("\"","\"\"")); // in CSV, double quotes are escaped by having two of them, so modify that here""
			builder.Append('"');
			int languagesRemaining = systemText.GetNumLanguages()-1; // don't include first
			if ( data.m_translations != null )
			{
				foreach( string translation in data.m_translations )
				{
					builder.Append(",\"");
					builder.Append(translation.Replace("\"","\"\"")); // in CSV, double quotes are escaped by having two of them, so modify that here""
					builder.Append('"');
					--languagesRemaining;
				}
			}

			for ( int i = 0; i < languagesRemaining; ++i )
			{
				builder.Append(",");
			}
		}

		string scriptPath = EditorUtility.SaveFilePanel("Export to CSV", "", "Export.csv","csv");
		if ( string.IsNullOrEmpty(scriptPath) == false )
		{
			try 
			{
				File.WriteAllText(scriptPath, builder.ToString(), GetCsvEncoding());
			}
			catch (System.Exception e)
			{
				EditorUtility.DisplayDialog("CSV Export Failed","Failed to export to CSV file.\nCheck it's not open elsewhere.\n\nError: "+e.Message,"Ok");
			}


			Application.OpenURL(Path.GetDirectoryName(scriptPath));
		}

	}

	System.Text.Encoding GetCsvEncoding()
	{
		if ( m_csvEncoding == eCsvEncoding.ASCII )
			return System.Text.Encoding.ASCII;
		else if ( m_csvEncoding == eCsvEncoding.UTF8 )
			return System.Text.Encoding.UTF8;
		else if ( m_csvEncoding == eCsvEncoding.Unicode || m_csvEncoding == eCsvEncoding.GoogleSheets )
			return System.Text.Encoding.Unicode;
		else if ( m_csvEncoding == eCsvEncoding.Windows1252 || m_csvEncoding == eCsvEncoding.Excel )
			return System.Text.Encoding.GetEncoding(1252);
	
		return System.Text.Encoding.Default;
	}

	// Returns true on success
	public bool ImportFromCSV( SystemText systemText )
	{
		bool result = false;
		string scriptPath = EditorUtility.OpenFilePanel("Import Text from CSV", "", "csv");
		if ( string.IsNullOrEmpty(scriptPath) )
			return result;

		int lineId = -1;
		int numLanguages = systemText.GetNumLanguages();

		// Using CSV-Reader https://github.com/tspence/csharp-csv-reader
		// Using patched version found at https://github.com/domdere/csharp-csv-reader

		FileStream stream = null;
		StreamReader streamReader = null;

		try
		{
			stream = File.OpenRead(scriptPath);
			streamReader = new StreamReader(stream, GetCsvEncoding(), true);

			using ( CSVFile.CSVReader reader = new CSVFile.CSVReader(streamReader, new CSVFile.CSVSettings() { HeaderRowIncluded = false }) )
			{
				foreach ( string[] line in reader )
				{
					++lineId;
					if ( lineId == 0 )
					{
						// Check for expected languages
						if ( line.Length < CSV_NUM_HEADERS + numLanguages )
						{
							string error = "Import canceled, unexpected columns:\nFound: ";
							for ( int i = 0; i < line.Length; ++i )
								error += line[i] + ", ";
							error += "\nExpected: "
								  + "Character,ID,File,Context";
							foreach (LanguageData language in systemText.GetLanguages())
								error+=","+language.m_description;
							Debug.LogError(error);
							break;
						}
						continue;
					}
					if ( line.Length < CSV_NUM_HEADERS+1 )
					{
						continue; // skipping line, since it doesn't have the right amount of stuff
					}
					string character = line[0];
					int id = -1;
					if ( int.TryParse(line[1], out id) == false )
						id = -1;
					string defaultText = line[CSV_INDEX_LANGUAGES];

					// Find the line
					TextData textData = systemText.EditorFindText(defaultText, id, character );
					if ( textData == null )
					{
						Debug.Log("Failed to import line (not found in text system): "+character+id+": "+defaultText);
					}
					else
					{
						//Debug.Log($"Imported {textData.m_id}:{textData.m_string}");
						if ( systemText.EditorGetShouldImportDefaultStringFromCSV() ) // NB: Now always importing text
							textData.m_string = line[CSV_INDEX_LANGUAGES];
						if ( numLanguages > 1 )
						{
							// Import other languages
							textData.m_translations = new string[numLanguages-1];
							for ( int i = 1; i < numLanguages && CSV_INDEX_LANGUAGES+i < line.Length; ++i)
							{
								textData.m_translations[i-1] = line[CSV_INDEX_LANGUAGES+i];
							}
						}
						textData.m_changedSinceImport = false;
					}					
				}
			}

			EditorUtility.SetDirty(systemText);	
			return true;

		}
		catch (System.IO.IOException e )
		{
			result=false;
			EditorUtility.DisplayDialog("CSV Import Failed","Failed to open CSV file. \nCheck it's not already open elsewhere.\n\nError: "+e.Message ,"Ok");
		}
		catch (System.Exception e)
		{
			result=false;	
			EditorUtility.DisplayDialog("CSV Import Failed","Failed to import CSV file.\n\nError: "+e.Message ,"Ok");
		}
		finally
		{
			if ( streamReader != null )
				streamReader.Close();
			if ( stream != null )
				stream.Close();
		}

		return result;
	}

	/// Reads in from rhubarb's output.txt into the TextData
	public static void ReadRhubarbData(SystemText systemText, int id)
	{
		if ( systemText == null )
	    {
	        return;
	    }

		if( id < 0)
		    return;

	    // Get data from processed line
		TextData data = systemText.EditorGetTextDataOrdered()[id];
		string[] phones = File.ReadAllLines("RhubarbOutput.txt");
		data.m_phonesTime = new float[phones.Length];
		data.m_phonesCharacter = new char[phones.Length];
		for (int i = 0; i < phones.Length; ++i )
		{
		    // read in phones (tab separated)
		    string[] strings = phones[i].Split('\t');
			data.m_phonesTime[i] = float.Parse(strings[0]);
			data.m_phonesCharacter[i] = strings[1][0];
		}
	}

	/// Runs the rhubarb tool to generate lip-sync data for the specific data id
	public static System.Diagnostics.Process StartRhubarb(SystemText systemText, int id )
	{
		TextData data = systemText.EditorGetTextDataOrdered()[id];
				
		// Skip lines that have lip sync data already
		if( m_skipExistingLipsyncData && data.m_phonesCharacter.Length > 0 )
			return null;

	    // Skip narrated lines
		if ( string.IsNullOrEmpty(data.m_character) || string.Equals( data.m_character, PowerQuest.STR_NARR, StringComparison.OrdinalIgnoreCase ) )
			return null;

		// Skip lines that don't have dialog wav
		string fullFileName =  data.m_character	+ data.m_id.ToString();			
		AudioClip clip = Resources.Load("Voice/"+fullFileName) as AudioClip;		
		if ( clip == null )
			return null;	

		File.WriteAllText("RhubarbInput.txt", data.m_string);

		System.Diagnostics.Process rhubarbProcess = new System.Diagnostics.Process();
		rhubarbProcess.StartInfo.FileName = @Path.Combine("Assets", "PowerQuest", "Scripts", "PowerQuest", "Editor", "RunRhubarb.bat");

		fullFileName = @Path.Combine("Assets", "Audio", "Resources", "Voice", fullFileName);
		
		// Find extention by checking which file exists
		string extention = null;
		string[] extentions = {".wav", ".ogg", ".mp3"};
		for ( int i = 0; i < extentions.Length; ++i ) 
		{			
			if ( File.Exists(fullFileName+extentions[i]) )
			{
				extention = extentions[i];
				break;
			}
		}
		if ( extention == null )
		{
			Debug.LogWarning("Rhubarb: File not found or unsupported format: "+fullFileName);
			return null;
		}

		rhubarbProcess.StartInfo.Arguments = $"{fullFileName}{extention} {systemText.GetLipsyncExtendedMouthShapes()}";
		rhubarbProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
		rhubarbProcess.Start();
		return rhubarbProcess;

	}

}
}
