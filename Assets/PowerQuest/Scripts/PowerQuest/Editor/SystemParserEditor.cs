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

[CustomPropertyDrawer(typeof(SystemParser.WordGroup))]
public class WordGroupPropertyDrawer : PropertyDrawer
{
	public static readonly char[] LABEL_DELIMITER_WRITE=new char[]{',',' ','\n'};
	public static readonly string WHITESPACE = " ";
	public static readonly string LABEL_DELIMITER_READ_1LINE = ", ";
	public static readonly string LABEL_DELIMITER_READ = "\n";

	//bool m_expanded = false;

	public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) 
	{
		return base.GetPropertyHeight (prop, label) ;//* (m_expanded ? 1.0f : 4.0f);
	}

	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		float lineHeight = base.GetPropertyHeight(prop, label);
		Rect guiRect = position;
		guiRect.height = lineHeight;
		EditorGUI.BeginProperty(position, label, prop);

		SerializedProperty valuesProp = prop.FindPropertyRelative("m_words");
		
		EditorGUI.BeginChangeCheck();
		string[] values = new string[valuesProp.arraySize];
		
		for ( int i = 0; i < values.Length; ++i)
			values[i] = valuesProp.GetArrayElementAtIndex(i).stringValue;			
		
		// Display		
		EditorGUILayout.BeginHorizontal();
		//m_foldoutIgnored = EditorGUILayout.Foldout(m_foldoutIgnored, "Ignore:",true) ;
						
		string labelText = values.Length > 0 ? values[0] : string.Empty;
		if ( labelText.Length > 0 ) 
			labelText = char.ToUpper(labelText[0]) + labelText.Substring(1);

		EditorLayouter layoutRect = new EditorLayouter(guiRect).Label(labelText, 40).Space.Variable();

		EditorGUI.LabelField(layoutRect,labelText);		
		string inLabels = (values.Length > 0) ?  string.Join(LABEL_DELIMITER_READ_1LINE, values) : string.Empty;
		//EditorGUI.LabelField(guiRect, label);
		string outLabels = EditorGUI.TextField(layoutRect, inLabels);	
		EditorGUILayout.EndHorizontal();
	
		if ( EditorGUI.EndChangeCheck() )
		{					
			//outLabels = outLabels.Replace(WHITESPACE,string.Empty);
			outLabels = outLabels.ToLower();
			string[] result = outLabels.Split(LABEL_DELIMITER_WRITE, System.StringSplitOptions.RemoveEmptyEntries);
			valuesProp.ClearArray();
			valuesProp.arraySize = result.Length;			
			for ( int i = 0; i < result.Length; ++i)
				valuesProp.GetArrayElementAtIndex(i).stringValue = result[i];
		}
		EditorGUI.EndProperty();
	}
}

[CustomEditor(typeof(SystemParser))]
public class SystemParserEditor : Editor 
{
	public static readonly char[] LABEL_DELIMITER_WRITE=new char[]{',',' ','\n'};
	public static readonly string WHITESPACE = " ";
	public static readonly string LABEL_DELIMITER_READ_1LINE = ", ";
	public static readonly string LABEL_DELIMITER_READ = "\n";
	
	[SerializeField] bool m_foldoutIgnored =false;

	SystemParser m_component = null;
	
	string m_testSaid=string.Empty;
	string m_testParseText=string.Empty;
	enum eTestResult { None, Fail, Success}
	eTestResult  m_testResult = eTestResult.None;


	public override void OnInspectorGUI()
	{
		m_component = (SystemParser)target;
		
		// Script buttons

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Global Parser Script") )
			QuestScriptEditor.Open(PowerQuestEditor.PATH_GLOBAL_SCRIPT, PowerQuest.GLOBAL_SCRIPT_NAME, QuestScriptEditor.eType.Global, "OnParser" );
		else if ( PowerQuestEditor.Get.GetSelectedRoom() != null && GUILayout.Button("Room Parser Script") )
			QuestScriptEditor.Open(PowerQuestEditor.Get.GetSelectedRoom(), "OnParser" );
		GUILayout.EndHorizontal();
		GUILayout.Space(10);
		// Ignore words
		{ 
			string inLabels = null;
			string outLabels = null;

			EditorGUI.BeginChangeCheck();

			if ( m_foldoutIgnored )
			{
				m_foldoutIgnored = EditorGUILayout.Foldout(m_foldoutIgnored, "Ignored words",true) ;
				string[] wordgroup = m_component.EditorIgnoredWords;
				inLabels = (wordgroup != null && wordgroup.Length > 0) ?  string.Join(LABEL_DELIMITER_READ, m_component.EditorIgnoredWords) : "";
				//string outLabels = EditorGUILayout.TextField(new GUIContent("Ignored words","Words ignored by the parser"), inLabels );		
				//EditorGUILayout.LabelField("Ignored words");
				outLabels = EditorGUILayout.TextArea(inLabels);//, new GUIContent("Ignored words","Words ignored by the parser") );
			}
			else 
			{
				EditorGUILayout.BeginHorizontal();
				m_foldoutIgnored = EditorGUILayout.Foldout(m_foldoutIgnored, "Ignore:",true) ;
				inLabels = (m_component.EditorIgnoredWords.Length > 0) ?  string.Join(LABEL_DELIMITER_READ_1LINE, m_component.EditorIgnoredWords) : "";
				outLabels = EditorGUILayout.TextField(inLabels );		
				EditorGUILayout.EndHorizontal();
			}

			//
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_words"), new GUIContent("Synonyms"));
			if ( serializedObject.ApplyModifiedProperties() )
				EditorUtility.SetDirty(target);
						
			if ( EditorGUI.EndChangeCheck() )
			{					
				//outLabels = outLabels.Replace(WHITESPACE,string.Empty);
				outLabels = outLabels.ToLower();
				m_component.EditorIgnoredWords = outLabels.Split(LABEL_DELIMITER_WRITE, System.StringSplitOptions.RemoveEmptyEntries);
				EditorUtility.SetDirty(target);
			}



			EditorGUI.BeginChangeCheck();
			m_testSaid = EditorGUILayout.TextField("Test Condition:",m_testSaid);
			m_testParseText = EditorGUILayout.TextField("Test Parser:",m_testParseText);
			//GUILayout.Button("Test");
			if ( EditorGUI.EndChangeCheck() && m_testSaid.Length>0 && m_testParseText.Length>0 )
			{ 
				m_component.InitDictionaries();
				m_component.ParseText(m_testParseText);
				bool result = m_component.Said(m_testSaid);
				m_testResult = result? eTestResult.Success : eTestResult.Fail;
			}
			if ( m_testResult == eTestResult.Success )
				EditorGUILayout.HelpBox("Test passed", MessageType.Info );
			else if ( m_testResult == eTestResult.Fail )
				EditorGUILayout.HelpBox("Test failed", MessageType.Error );

		}
	}


}

}
