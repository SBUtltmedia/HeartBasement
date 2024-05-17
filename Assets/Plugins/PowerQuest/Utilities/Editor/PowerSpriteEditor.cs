using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PowerTools
{


[CustomEditor(typeof(PowerSprite))]
[CanEditMultipleObjects]
public class PowerSpriteEditor : Editor 
{
	bool m_snap = true;
	
	override public void OnInspectorGUI() 
	{		
		base.OnInspectorGUI();
		m_snap = GUILayout.Toggle(m_snap, "Snap Offset To Pixel");

		/*
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Top-Left", EditorStyles.miniButtonLeft) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignTop() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignLeft() );
		}
		if ( GUILayout.Button("Top", EditorStyles.miniButtonMid) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignTop() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignCenter() );
		}
		if ( GUILayout.Button("Top-Right", EditorStyles.miniButtonRight) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignTop() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignRight() );
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Center-Left", EditorStyles.miniButtonLeft) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignMiddle() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignLeft() );
		}
		if ( GUILayout.Button("Center", EditorStyles.miniButtonMid) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignMiddle() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignCenter() );
		}
		if ( GUILayout.Button("Center-Right", EditorStyles.miniButtonRight) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignMiddle() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignRight() );
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Bottom-Left", EditorStyles.miniButtonLeft) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignBottom() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignLeft() );
		}
		if ( GUILayout.Button("Bottom", EditorStyles.miniButtonMid) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignBottom() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignCenter() );
		}
		if ( GUILayout.Button("Bottom-Right", EditorStyles.miniButtonRight) )
		{
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignBottom() );
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).AlignRight() );
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		*/
		if ( GUI.changed && m_snap )
			System.Array.ForEach( targets, obj=> (obj as PowerSprite).Snap() );
		
		if (GUI.changed)
		{
		    //serializedObject.ApplyModifiedProperties();
		    EditorUtility.SetDirty(target);
		}

	}

	public void OnSceneGUI()
	{
		PowerSprite component = target as PowerSprite;
		if ( component == null )
			return;
		
		EditorGUI.BeginChangeCheck();

		Vector2 pos = Vector2.zero;
		Color oldCol = Handles.color;
		Handles.color = Color.green;
		Handles.DrawLine((Vector2)component.transform.position + component.Offset, (Vector2)component.transform.position + component.Offset + Vector2.right);
		pos.x = Handles.FreeMoveHandle( (Vector2)component.transform.position + component.Offset + Vector2.right, Quaternion.identity,0.4f,Vector3.zero, Handles.SphereHandleCap).x;
		Handles.color = Color.red;
		Handles.DrawLine((Vector2)component.transform.position + component.Offset, (Vector2)component.transform.position + component.Offset + Vector2.up);
		pos.y = Handles.FreeMoveHandle( (Vector2)component.transform.position + component.Offset + Vector2.up, Quaternion.identity,0.4f,Vector3.zero, Handles.SphereHandleCap).y;

		Handles.color = oldCol;

		if ( EditorGUI.EndChangeCheck() )
		{
			Undo.RecordObject(target,"offset");

			pos -= (Vector2)component.transform.position + Vector2.one;
			
			component.Offset = pos;
			if ( m_snap )
				component.Snap();
		}
	}
}



} 