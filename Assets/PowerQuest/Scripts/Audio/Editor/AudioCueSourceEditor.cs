using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;


namespace PowerTools.Quest
{


	// Use this for initializatio
[CustomEditor(typeof(AudioCueSource))]
public class AudioCueSourceEditor : Editor {
		
	AudioCueSource m_object;
	List<AudioCue> m_items;
	SerializedObject m_targetObject = null;
	//SerializedProperty m_listProperty = null;
	
	void OnEnable()
	{
		
		m_object = (AudioCueSource)target;
		m_items = m_object.m_cues;
		m_targetObject = new SerializedObject( target );			
		
	}
	
	override public void OnInspectorGUI() 
	{
		//EditorGUILayout.LabelField("Hi!");
		
		//EditorGUILayout.Space();
				
		SerializedProperty prop = m_targetObject.GetIterator();
		if ( prop.Next(true) )
		{
			// Skip built in properites
			for ( int i = 0; i < 8 && prop.Next (false); ++i )	{}
			
			while ( prop.Next (false) )
			{				
				if ( prop.editable && prop.name != "m_cues"  )
				{
					EditorGUILayout.PropertyField( prop );
				}
			}
		}
		
		EditorGUILayout.Space();
			
		EditorUtils.UpdateListInspector<AudioCue>( ref m_items, null, new EditorUtils.CreateListItemGUIDelegate(BuildSpawnItemInspector),  null );		
		
		EditorGUI.indentLevel = 0;
				
		if (GUI.changed)
		{			
        	m_targetObject.ApplyModifiedProperties();	
			EditorUtility.SetDirty(target);	
		}
	}
	
	// Delegate
	void BuildSpawnItemInspector( int i )
	{		
		//EditorGUILayout.BeginVertical();					
		m_items[i] = EditorGUILayout.ObjectField("", (Object)m_items[i], typeof(AudioCue), false) as AudioCue; // false is "allowSceneObjects"
			/*
			if ( i >= m_listProperty.arraySize )
			{
				m_targetObject = new SerializedObject( target );	
				m_listProperty = m_targetObject.FindProperty("m_cues");
			}*/		
		//EditorGUILayout.EndVertical();
		
	}
	
	
}
 
 
}