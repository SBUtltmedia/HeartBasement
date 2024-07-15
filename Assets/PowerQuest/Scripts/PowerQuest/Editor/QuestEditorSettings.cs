using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

//namespace PowerTools.Quest { public static partial class Systems {	public static QuestEditorSettings EditorSettings { get{return QuestEditorSettings.Get; } } } }

// Create a new type of Settings Asset.
public class QuestEditorSettings : ScriptableObject
{
	public const string SETTINGS_PATH = "Assets/Game/PowerQuestEditorSettings.asset";

	[SerializeField] internal bool m_smartCompile = true;	
	
	#if UNITY_EDITOR_WIN
	[SerializeField] internal string m_scriptEditorFont = "Consolas";
	#else
	[SerializeField] internal string m_scriptEditorFont = "Lucida Grande";		
	#endif

	[SerializeField] internal QuestScriptEditor.Colors m_scriptEditorColors =	new QuestScriptEditor.Colors();
	[SerializeField] internal QuestScriptEditor.Colors.eTheme m_scriptEditorTheme = QuestScriptEditor.Colors.eTheme.LightMono;

	[SerializeField] internal bool m_spellCheckEnabled = false;
	[SerializeField] internal List<string> m_spellCheckIgnoredWords = new List<string>();
	[SerializeField] internal string m_spellCheckDictionaryPath = "Assets/Plugins/PowerQuest/ThirdParty/Editor/SpellCheck/en_US.dic";

	[Tooltip(@"Regex for functions that should have 'yield return' hidden in script editor. Eg: 'E\.MyBlockingFunc\(', or 'C\.\w+\.MyBlockingFunc\('")]
	public string[] m_yieldRegexes = new string[]{};
	[NonReorderable] // if reorderable can't fold  out internals :/
	public QuestScriptEditor.FindReplaceRegexData[] m_scriptReplaceRegexes = new QuestScriptEditor.FindReplaceRegexData[]{};
	[Tooltip(@"Regex for words that should auto-complete, and be colored in editor. Eg: `MyCoolFunction`")]
	public string[] m_autoCompleteRegexes = new string[]{};
	[Tooltip(@"Regex for colored keywords Eg: `MyCoolFunction`")]
	public string[] m_colorRegexes = new string[]{};
	
	// Singleton instance
	static QuestEditorSettings m_instance = null;

	public bool m_regexFoldOut = false;
	public string m_regexError = null;
		
	public Color m_scriptBtnUsedCol = Color.white;
	public Color m_scriptBtnUnusedCol = new Color(0.8f, 0.8f, 0.8f);
	
	///////////////////////////////////////////////////////////////
	// Setup and access functions

	// Singleton accessor
	public static QuestEditorSettings Get { get
	{
		if  ( m_instance == null )
			m_instance = LoadSettings();
		return m_instance;
	}}
	
	public static QuestEditorSettings LoadSettings()
	{    
		QuestEditorSettings settings = AssetDatabase.LoadAssetAtPath<QuestEditorSettings>(SETTINGS_PATH);
		if (settings == null)
		{
			settings = ScriptableObject.CreateInstance<QuestEditorSettings>();
			AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
			AssetDatabase.SaveAssets();
		}

		return settings;
	}
}

namespace PowerTools.Quest
{

// Use this for initializatio
[CustomEditor(typeof(QuestEditorSettings))]
public class QuestEditorSettingsEditor : Editor 
{
	
	override public void OnInspectorGUI() 
	{
		//
		// Script colours
		//

		QuestEditorSettings editorSettings = target as QuestEditorSettings;
		SerializedObject serializedObj = new SerializedObject(editorSettings);

		GUILayout.Space(5);
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Script Button Color Used/Unused");
		EditorGUILayout.PropertyField(serializedObj.FindProperty("m_scriptBtnUsedCol"),new GUIContent(""));
		EditorGUILayout.PropertyField(serializedObj.FindProperty("m_scriptBtnUnusedCol"),new GUIContent(""));
		EditorGUILayout.EndHorizontal();
		if ( serializedObj.ApplyModifiedProperties() )
				PowerQuestEditor.OnUpdateScriptColors?.Invoke();
		
		GUILayout.Space(5);
		GUILayout.Label("Script Editor Style", EditorStyles.boldLabel);

		QuestScriptEditor.Colors.eTheme oldTheme = editorSettings.m_scriptEditorTheme;			
		editorSettings.m_scriptEditorTheme = (QuestScriptEditor.Colors.eTheme)EditorGUILayout.EnumPopup("Color Theme", editorSettings.m_scriptEditorTheme);
		if ( oldTheme != editorSettings.m_scriptEditorTheme )
		{
			editorSettings.m_scriptEditorColors.SetTheme(editorSettings.m_scriptEditorTheme);
			PowerQuestEditor.OnUpdateScriptColors?.Invoke();	
		}
		if ( editorSettings.m_scriptEditorTheme == QuestScriptEditor.Colors.eTheme.Custom )
		{
			EditorGUILayout.PropertyField(serializedObj.FindProperty("m_scriptEditorColors"),new GUIContent("Custom Colors"),true);
			if ( serializedObj.ApplyModifiedProperties() )
				PowerQuestEditor.OnUpdateScriptColors?.Invoke();
		}
		{
			EditorGUILayout.PropertyField(serializedObj.FindProperty("m_scriptEditorFont"),new GUIContent("Font"),true);
			if ( serializedObj.ApplyModifiedProperties() )
				PowerQuestEditor.OnUpdateScriptColors?.Invoke();
		}
		EditorGUILayout.HelpBox( "'Ctrl + Scroll Wheel' changes font size in the script editor.", 
				MessageType.None);

		GUILayout.Space(5);

		GUILayout.Label("Spell Check Settings", EditorStyles.boldLabel);
		PowerQuestEditor.Get.SpellCheckEnabled = GUILayout.Toggle(PowerQuestEditor.Get.SpellCheckEnabled, "Enable Spell Check");
		PowerQuestEditor.Get.SpellCheckDictionaryPath =  EditorGUILayout.DelayedTextField("Dictionary Path",PowerQuestEditor.Get.SpellCheckDictionaryPath);
		if ( GUILayout.Button("Clear ignored word list" ) )
		{
			PowerQuestEditor.Get.SpellCheckIgnoredWords.Clear();
			QuestScriptEditor.InitSpellCheck(true);
		}
		GUILayout.Space(5);

		//editorSettings.m_regexFoldOut = GUILayout.Toggle(editorSettings.m_regexFoldOut, "Custom script expression regular expressions");
		editorSettings.m_regexFoldOut = EditorGUILayout.Foldout(editorSettings.m_regexFoldOut,"Custom script expression regular expressions",true);
		if ( editorSettings.m_regexFoldOut )
		{
			
			EditorGUILayout.HelpBox( "Advanced: These allow you to extend the functionality of the script editor. Most of these fields are regular expressions, you have to know what you're doing. Pretty impossible to do without an example so, ask on the discord if you think you need this ;)",
				MessageType.Info);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(serializedObj.FindProperty("m_yieldRegexes"),new GUIContent("Yield functions"),true);
			EditorGUILayout.PropertyField(serializedObj.FindProperty("m_scriptReplaceRegexes"),new GUIContent("Replace on Script Save/Load"),true);		
			EditorGUILayout.PropertyField(serializedObj.FindProperty("m_autoCompleteRegexes"),new GUIContent("Auto-Complete Keywords"),true);		
			EditorGUILayout.PropertyField(serializedObj.FindProperty("m_colorRegexes"),new GUIContent("Syntax highlight expressions"),true);

			if ( EditorGUI.EndChangeCheck() )
			{ 
				try 
				{
					foreach(QuestScriptEditor.FindReplaceRegexData item in editorSettings.m_scriptReplaceRegexes)
					item.Recompile();			
					QuestScriptEditor.OnRegExChanged();
					editorSettings.m_regexError = null;
				}
				catch (System.Exception e)
				{
					editorSettings.m_regexError = e.Message;
				}
			}
			if ( string.IsNullOrEmpty(editorSettings.m_regexError) == false )
			{
				EditorGUILayout.HelpBox( "Regex error:\n"+editorSettings.m_regexError, 
					MessageType.Error);
			}
			GUILayout.BeginHorizontal();
			if (  GUILayout.Button("Regex reference") )
				Application.OpenURL("https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference");
			if (  GUILayout.Button("Regex tester") )
				Application.OpenURL("http://regexstorm.net/tester");
			GUILayout.EndHorizontal();

			GUILayout.Space(10);	
		}

		

		GUILayout.Space(5);	
		
		//
		// Auto Compile
		//

		GUILayout.Label("Smart Compile Settings", EditorStyles.boldLabel);
		bool toggle = GUILayout.Toggle(editorSettings.m_smartCompile, "Enable Smart Compile");
		if ( toggle != editorSettings.m_smartCompile )
			PowerQuestEditor.Get.SetSmartCompileEnabled(toggle);
		if ( editorSettings.m_smartCompile )
		{
			EditorGUILayout.HelpBox( "When ticked, unity won't compile whenever you Save a QuestScript or add a Quest Object.\n Instead it'll wait until you hit play, or save the project.\n\nThe drawback is that unity won't import other files that you may have placed in your project directory.\n\nTo manually refresh, press Ctrl+R, or hit the 'Compile'(F7) button in the script editor. If it's grey, then there's nothing to refresh.", 
				MessageType.Info);
		}
		GUILayout.Space(5);	

		if ( GUI.changed )
		{
			serializedObj.ApplyModifiedProperties();
			EditorUtility.SetDirty(target);		
		}
	}
}

}
