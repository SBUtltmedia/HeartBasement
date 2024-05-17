using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;

namespace PowerTools.Quest
{

[CustomEditor(typeof(Sortable)), CanEditMultipleObjects]
public class SortableEditor : Editor 
{
	float m_oldYPos = float.MaxValue;
	public void OnSceneGUI()
	{		
		Sortable component = (Sortable)target;
		
		GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel);

		Transform transform = component.transform;
		{
			Vector3 transformPosition = component.Fixed ? Vector3.zero : transform.position;
			float transformPosY = transformPosition.y;

			float oldY = transformPosition.y + component.Baseline;
			Vector3 position = transformPosition + new Vector3( -15, component.Baseline, 0);			

			Handles.color = Color.cyan;
			GUI.color = Color.cyan;
			textStyle.normal.textColor = GUI.color;

			EditorGUI.BeginChangeCheck();
			position = Handles.FreeMoveHandle( position, Quaternion.identity,4.0f,new Vector3(0,1,0),Handles.DotHandleCap);

			Handles.Label(position + new Vector3(5,0,0), "Baseline", textStyle);
			Handles.color = Color.cyan.WithAlpha(0.5f);
			Handles.DrawLine( position + (Vector3.left * 500), position + (Vector3.right * 500) );

			if ( EditorGUI.EndChangeCheck() ) 
			{
				Undo.RecordObject(component,"Changed Baseline");
				component.Baseline = Utils.Snap(position.y - transformPosition.y,PowerQuestEditor.SnapAmount);				
			}			

			if ( m_oldYPos + component.Baseline != oldY && Application.isPlaying == false)
				component.EditorRefresh();
			
			m_oldYPos = transformPosition.y;
		}
	}
}
}
