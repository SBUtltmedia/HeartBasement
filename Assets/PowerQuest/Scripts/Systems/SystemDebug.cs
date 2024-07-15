using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;

namespace PowerTools.Quest
{

public class SystemDebug : SingletonAuto<SystemDebug>
{
	abstract class DebugElement
	{
		public Color m_color = Color.yellow;
		public float m_time = 0;
		public abstract void Draw();		
	}

	class DebugLine : DebugElement
	{		
		public Vector2 m_start = Vector2.zero;
		public Vector2 m_end = Vector2.zero;
		
		public override void Draw()
		{
			Debug.DrawLine(m_start, m_end, m_color,0,false);
		}
	}

	class DebugPoint: DebugElement
	{
		public Vector2 m_point = Vector2.zero;		

		public override void Draw()
		{
			Debug.DrawLine(m_point.WithOffset(-1,-1), m_point.WithOffset(1,1), m_color,0,false);
			Debug.DrawLine(m_point.WithOffset(-1,1), m_point.WithOffset(1,-1), m_color,0,false);
		}		
	}
	class DebugText: DebugElement
	{
		public Vector2 m_pos = Vector2.zero;	
		public string text = string.Empty;

		public override void Draw()
		{	
			/*
			#if UNITY_EDITOR
			UnityEditor.Handles.BeginGUI();
			Color oldCol = GUI.color;
			GUI.color = m_color;
			var view = UnityEditor.SceneView.currentDrawingSceneView;
			Vector3 screenPos = view.camera.WorldToScreenPoint(m_pos);
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.fontStyle = FontStyle.Bold;
			Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
			GUI.Label(new Rect(screenPos.x, view.position.height-screenPos.y - size.y-8, size.x*2, size.y), text,style);
			UnityEditor.Handles.EndGUI();
			GUI.color = oldCol;
			#endif
			*/
			//Gizmos.color=m_color;
			//GizmosEx.DrawText(text, m_pos);

		}
	}

	class DebugCircle: DebugElement
	{
		public Vector2 m_pos = Vector2.zero;		
		public float m_radius = 1;		

		public override void Draw()
		{
			int segmentSize = 36;
			for ( int i = 0; i < 360; i+=segmentSize)
			{
				float ang = Mathf.Deg2Rad*(float)(i-segmentSize);
				Vector2 from = m_pos + new Vector2( m_radius*Mathf.Sin(ang), m_radius*Mathf.Cos(ang) );
				ang = Mathf.Deg2Rad*(float)(i);
				Vector2 to = m_pos + new Vector2( m_radius*Mathf.Sin(ang), m_radius*Mathf.Cos(ang) );
				Debug.DrawLine(from,to,m_color,0,false);
			}
		}		
	}

	List<DebugElement> m_elements = new List<DebugElement>();

	void LateUpdate()
	{
		for ( int i = m_elements.Count-1; i >= 0; --i )
		{
			DebugElement element = m_elements[i];
			element.Draw();

			element.m_time -= Time.deltaTime;
			if ( m_elements[i].m_time <= 0 )
			{
				m_elements.RemoveAt(i);
			}
		}		
	}
	
	public void DrawText(Vector2 pos, string text, Color color, float time = 0 ) 
	{
		m_elements.Add(new DebugText(){m_color=color, m_time=time, m_pos=pos} );	
	}
	public void DrawLine(Vector2 start, Vector2 end, Color color, float time = 0 ) 
	{
		m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=start,m_end=end} );	
	}
	public void DrawPoint(Vector2 point, Color color, float time = 0 ) 
	{
		m_elements.Add(new DebugPoint(){m_color=color, m_time=time, m_point=point} );	
	}
	public void DrawCircle(Vector2 pos, float radius, Color color, float time = 0 ) 
	{
		m_elements.Add(new DebugCircle(){m_color=color, m_time=time, m_pos=pos,m_radius=radius} );	
	}
	public void DrawRect(RectCentered rect, Color color, float time = 0 ) 
	{
		m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=new Vector2(rect.MinX, rect.MinY),m_end=new Vector2(rect.MinX, rect.MaxY)} );
		m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=new Vector2(rect.MinX, rect.MaxY),m_end=new Vector2(rect.MaxX, rect.MaxY)} );
		m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=new Vector2(rect.MaxX, rect.MaxY),m_end=new Vector2(rect.MaxX, rect.MinY)} );
		m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=new Vector2(rect.MaxX, rect.MinY),m_end=new Vector2(rect.MinX, rect.MinY)} );
	}
	public void DrawPoly(Vector2[] poly, Color color, float time = 0 ) 
	{		
		for ( int j = 0; j < poly.Length; ++j )
		{
			int i = j == 0 ? poly.Length-1 : j-1;
			m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=poly[i],m_end=poly[j]} );				
		}
	}
}

}
