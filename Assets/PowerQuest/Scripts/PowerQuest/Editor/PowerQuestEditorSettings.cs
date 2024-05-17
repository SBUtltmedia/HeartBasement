using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Testing adding proper unity project settings for PowerQuest
namespace PowerTools.Quest
{
/*
// Create a new type of Settings Asset.
class PowerQuestEditorSettings : ScriptableObject
{
    static readonly string PATH_GAME = "Assets/Game";
    static readonly string SETTINGS_FOLDER = "Editor";
    public static readonly string PATH_SETTINGS = "Assets/Game/Editor/PowerQuestEditorSettings.asset";

    [SerializeField] bool m_spellCheckEnabled = false;
    [SerializeField] List<string> m_spellCheckIgnoredWords = new List<string>();

    internal static PowerQuestEditorSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PowerQuestEditorSettings>(PATH_SETTINGS);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PowerQuestEditorSettings>();
            AssetDatabase.CreateFolder( PATH_GAME, SETTINGS_FOLDER );
            AssetDatabase.CreateAsset(settings, PATH_SETTINGS);
            AssetDatabase.SaveAssets();
        }
        return settings;
    }

    internal static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
}


[CustomEditor(typeof(PowerQuestEditorSettings))]
public class PowerQuestEditorSettingsEditor : Editor 
{	
	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Lets roawk", EditorStyles.boldLabel);
		DrawDefaultInspector();
		PowerQuestEditorSettings component = (PowerQuestEditorSettings)target;
    }
}


// Register a SettingsProvider using IMGUI for the drawing framework:
static class PowerQuestSettingsGui
{
    [SettingsProvider]
    public static SettingsProvider CreatePowerQuestSettingsProvider()
    {
        // First parameter is the path in the Settings window.
        // Second parameter is the scope of this setting: it only appears in the Project Settings window.
        SettingsProvider provider = new SettingsProvider("Project/PowerQuestEditorSettings", SettingsScope.Project);

        // By default the last token of the path is used as display name if no label is provided.        
        provider.label = "PowerQuest";
        // Populate the search keywords to enable smart search filtering and label highlighting:
        provider.keywords = new HashSet<string>(new[] { "spell check", "compile", "powerquest" });        

        // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
        provider.guiHandler = (searchContext) =>
        {
            
            Editor componentEditor = Editor.CreateEditor(PowerQuestEditorSettings.GetOrCreateSettings());
            componentEditor.DrawDefaultInspector();
            
            // SerializedObject settings = PowerQuestEditorSettings.GetSerializedSettings();
            // EditorGUILayout.PropertyField(settings.FindProperty("m_Number"), new GUIContent("My Number"));
            // EditorGUILayout.PropertyField(settings.FindProperty("m_SomeString"), new GUIContent("My String"));
            
        };

        return provider;
    }

}
*/
/*
// Create PowerQuestSettingsProvider by deriving from SettingsProvider:
class PowerQuestSettingsProvider : SettingsProvider
{
    private SerializedObject m_CustomSettings;

    class Styles
    {
        public static GUIContent number = new GUIContent("My Number");
        public static GUIContent someString = new GUIContent("Some string");
    }

    public PowerQuestSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
        : base(path, scope) {}

    public static bool IsSettingsAvailable()
    {
        return File.Exists(PowerQuestEditorSettings.PATH_SETTINGS);
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        // This function is called when the user clicks on the MyCustom element in the Settings window.
        m_CustomSettings = PowerQuestEditorSettings.GetSerializedSettings();
    }

    public override void OnGUI(string searchContext)
    {
        // Use IMGUI to display UI:
        EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_Number"), Styles.number);
        EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_SomeString"), Styles.someString);
    }

    // Register the SettingsProvider
    [SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider()
    {
        if (IsSettingsAvailable())
        {
            var provider = new PowerQuestSettingsProvider("Project/PowerQuestSettingsProvider", SettingsScope.Project);

            // Automatically extract all keywords from the Styles.
            provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }

        // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
        return null;
    }
}    
*/
}