using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Reflection;
using PowerTools.Quest;

[CustomEditor(typeof(Transform)), CanEditMultipleObjects]
public class TransformEditor : Editor 
{
	static bool m_advanced = false;

	Editor m_defaultEditor = null;

    void OnEnable()
    {		
		m_defaultEditor = Editor.CreateEditor(targets, System.Type.GetType("UnityEditor.TransformInspector, UnityEditor"));		
    }

	void OnDisable()
	{
		//When OnDisable is called, the default editor we created should be destroyed to avoid memory leakage.
		//Also, make sure to call any required methods like OnDisable
		MethodInfo disableMethod = m_defaultEditor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		if (disableMethod != null)
			disableMethod.Invoke(m_defaultEditor,null);
		DestroyImmediate(m_defaultEditor);

	}

	
	public override void OnInspectorGUI() 
	{	
		Transform t = (Transform)target;
		EditorGUIUtility.labelWidth = 35;		

		//EditorGUI.indentLevel = 1;
			
		m_advanced = EditorGUILayout.Foldout(m_advanced,"Advanced",true);
		
		if ( m_advanced )
		{
			m_defaultEditor.OnInspectorGUI();
			//DrawDefaultInspector();
			return;
		}

		EditorGUILayout.BeginHorizontal();	
		Vector2 position  = EditorGUILayout.Vector2Field("",t.localPosition);		
		
		GUI.enabled = ( position != Utils.SnapRound(position,1) );
		{	
			if ( GUILayout.Button("Snap",EditorStyles.miniButtonLeft, GUILayout.Width(37)) )
				position = Utils.SnapRound(position,1);		
		}
		GUI.enabled = ( position != Vector2.zero );
		{	
			if ( GUILayout.Button("Zero", EditorStyles.miniButtonMid, GUILayout.Width(34)) )
				position = Vector2.zero;
		}
		GUI.enabled=true;
		bool flip = t.localScale.x < 0;			
		flip = GUILayout.Toggle(flip,"Flip",EditorStyles.miniButtonRight, GUILayout.Width(28));	

		Vector3 eulerAngles = t.localEulerAngles;	
		eulerAngles.z = EditorGUILayout.FloatField("Angle",t.localEulerAngles.z, GUILayout.MinWidth(55),GUILayout.MaxWidth(90));	

		EditorGUILayout.EndHorizontal();

		//Vector2 scale = EditorGUILayout.Vector2Field("Scale", t.localScale );			
		
		//EditorGUIUtility.LookLikeInspector();
		if (GUI.changed)
		{			
			Undo.RecordObject(t,"Transform Change");
			//Undo.RegisterUndo(t, "Transform Change");
			EditorUtility.SetDirty(target);		
						
			t.localPosition = FixIfNaN( new Vector3( position.x, position.y, t.localPosition.z ));
			t.localEulerAngles = FixIfNaN( new Vector3( t.localEulerAngles.x, t.localEulerAngles.y, eulerAngles.z ));
			t.localScale = FixIfNaN( new Vector3( Mathf.Abs(t.localScale.x)*(flip?-1.0f:1.0f), t.localScale.y, t.localScale.z ));
		}
		
	}
	
	private Vector3 FixIfNaN(Vector3 v)
	{
		if (float.IsNaN(v.x))
		{
			v.x = 0;
		}
		if (float.IsNaN(v.y))
		{
			v.y = 0;
		}
		if (float.IsNaN(v.z))
		{
			v.z = 0;
		}
		return v;
	}
}
