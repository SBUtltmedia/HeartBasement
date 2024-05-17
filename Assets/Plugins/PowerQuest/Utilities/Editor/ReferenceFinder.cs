using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace PowerTools
{

public class ReferenceFinder : EditorWindow
{
    public static GUIContent ICON_SEARCH = null;// EditorGUIUtility.IconContent("d_ViewToolZoom");

	Vector2 m_scrollPosition = Vector2.zero;
	List<GameObject> m_matches = new List<GameObject>();
	List<string> m_paths = null;

	// Used to queue a call to FindObjectReferences() to avoid doing it mid-layout
	Object m_findReferencesAfterLayout = null;

	void OnEnable()
	{
		ICON_SEARCH = EditorGUIUtility.IconContent("d_ViewToolZoom"); 
	}

	void OnGUI () 		
	{	
		GUILayout.Space(5);	
		m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Found: "+m_matches.Count);
		if ( GUILayout.Button("Clear", EditorStyles.miniButton) )
		{
			m_matches.Clear();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		for ( int i = m_matches.Count-1; i >= 0; --i)
		{					
			LayoutItem(i, m_matches[i] );
		}
		
		EditorGUILayout.EndScrollView();	

		if ( m_findReferencesAfterLayout != null )
		{
			FindObjectReferences(m_findReferencesAfterLayout);
			m_findReferencesAfterLayout = null;
		}
		
	}


	void LayoutItem(int i, UnityEngine.Object obj)
	{				
		
		GUIStyle style = new GUIStyle(EditorStyles.miniButtonLeft){alignment = TextAnchor.MiddleLeft};
		
		if (  obj != null)
		{ 
			GUILayout.BeginHorizontal();

			if ( GUILayout.Button( obj.name, style ) )
			{							
				Selection.activeObject = obj;
				EditorGUIUtility.PingObject(obj);
			}			

			// Use "right arrow" unicode character 
			if ( GUILayout.Button( ICON_SEARCH/*"\u25B6"*/, EditorStyles.miniButtonRight, GUILayout.MaxWidth(25), GUILayout.MaxHeight(15) ) )
			{		
				m_findReferencesAfterLayout = obj;
			}
			
			GUILayout.EndHorizontal();
			
		}
	}

	[MenuItem ("CONTEXT/Component/Find References")]
	static void FindComponentReferences (MenuCommand command)
	{
		//Show existing window instance. If one doesn't exist, make one.
		ReferenceFinder window = EditorWindow.GetWindow<ReferenceFinder>(false,"References");
		window.FindComponentReferences(command.context.GetType());
	}

	void FindComponentReferences( System.Type searchType )
	{
		EditorUtility.DisplayProgressBar("Searching","Generating file paths",0.0f);

		if ( m_paths == null )
		{
			m_paths = new List<string>();
			GetFilePaths("Assets", ".prefab", ref m_paths );
		}

		float progressBarPos = 0;
		int numPaths = m_paths.Count;
		int hundredthIteration = Mathf.Max(1,numPaths/100);

		List<Component> matches = new List<Component>();
		for ( int i = 0; i < numPaths; ++i )
		{
			GameObject obj = AssetDatabase.LoadMainAssetAtPath(m_paths[i]) as GameObject;
			if ( obj != null )
			{						
				matches.AddRange( obj.GetComponentsInChildren(searchType, true) );
			}
			if ( i % hundredthIteration == 0 )
			{
				progressBarPos += 0.01f;
				EditorUtility.DisplayProgressBar("Searching","Searching for components",progressBarPos);
			}
		}

		//  Convert from components to game gameobjects
		m_matches = matches.ConvertAll( item => item.gameObject );
		matches.Clear();
		EditorUtility.ClearProgressBar();
	}

	// Find object usage
	[MenuItem ("Assets/Find References",false,39)]
	static void FindObjectReferences()
	{
		//Show existing window instance. If one doesn't exist, make one.
		ReferenceFinder window = EditorWindow.GetWindow<ReferenceFinder>(true, "Find References", true);
		window.FindObjectReferences(Selection.activeObject);
	}

	void FindObjectReferences( Object toFind )
	{			
		EditorUtility.DisplayProgressBar("Searching","Generating file paths",0.0f);

		if ( m_paths == null )
		{
			m_paths = new List<string>();
			GetFilePaths("Assets", ".prefab", ref m_paths );
		}

		float progressBarPos = 0;
		int numPaths = m_paths.Count;
		int hundredthIteration = Mathf.Max(1,numPaths/100); // So we only update progress bar 100 times, not for every item

		// Get object path
		string toFindName = AssetDatabase.GetAssetPath(toFind);
		toFindName = System.IO.Path.GetFileNameWithoutExtension (toFindName);
		Object[] tmpArray = new Object[1];
		m_matches.Clear();
		for ( int i = 0; i < numPaths; ++i )
		{
			tmpArray[0] = AssetDatabase.LoadMainAssetAtPath(m_paths[i]);
			if ( tmpArray != null && tmpArray.Length > 0 && tmpArray[0] != toFind) // Don't add self
			{
				Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);
				if ( System.Array.Exists(dependencies, item=>item == toFind ) ) 
				{						
					// Don't add if another of the dependencies is already in there
					m_matches.Add(tmpArray[0] as GameObject);
				}
				
			}
			if ( i % hundredthIteration == 0 )
			{
				progressBarPos += 0.01f;
				EditorUtility.DisplayProgressBar("Searching","Searching dependencies",progressBarPos);
			}
		}

		EditorUtility.DisplayProgressBar("Searching","Removing redundant references",1);

		//
		// Go through matches, get dependencies and remove any that have another dependency on the match list. We only want direct dependencies
		//
		for ( int i = m_matches.Count - 1; i >= 0; i-- ) 
		{
			tmpArray[0] = m_matches[i];
			Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);

			bool shouldRemove = false;

			for ( int j = 0; j < dependencies.Length && shouldRemove == false; ++j )
			{
				Object dependency = dependencies[j];
				shouldRemove =  ( m_matches.Find( item => item == dependency && item != tmpArray[0] ) != null );
			}

			if ( shouldRemove )
				m_matches.RemoveAt(i);
		}


		EditorUtility.ClearProgressBar();
	}

	static void GetFilePaths(string startingDirectory, string extention, ref List<string> paths)
  	{
		try
		{
			string[] directories = Directory.GetDirectories(startingDirectory);
			for ( int i = 0; i < directories.Length; ++i )
		   	{
		   		string dir = directories[i];
				string[] files = Directory.GetFiles(dir);
				for ( int j = 0; j < files.Length; ++j )
	       		{
	       			string file = files[j];
					if ( file.EndsWith(extention) )
					{
						paths.Add(file);
					}
		       	}
				GetFilePaths(dir, extention, ref paths);
		   	}
		}
		catch (System.Exception excpt)
		{
			Debug.LogError(excpt.Message);
		}
	}

}

}