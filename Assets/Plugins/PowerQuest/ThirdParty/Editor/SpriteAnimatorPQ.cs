//-----------------------------------------
// Sprite Animator extentions for PowerQuest
//----------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Anim;

namespace PowerTools
{


public partial class SpriteAnimator
{
	static readonly string AUDIO_CUE_STR = "AudioCue";
	bool m_dragDropHovering2 = false;

	partial void exOnUpdate()
	{
		if ( m_dragDropHovering2 )
		{
			Repaint();
		}
	}

	partial void exOnDragDropEventsBar( Event e, Rect rect )
	{		
		m_dragDropHovering=true;
			//Rect selectionRect = new Rect(rect){ xMin = e.mousePosition.x, xMax = e.mousePosition.x+1 };
			//DrawRect(selectionRect,COLOR_UNITY_BLUE.WithAlpha(0.1f),COLOR_UNITY_BLUE.WithAlpha(0.6f));	
		if ( (e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && rect.Contains( e.mousePosition ) )
		{
			bool isCue = System.Array.Exists( DragAndDrop.objectReferences, item => item is GameObject && (item as GameObject).GetComponent(AUDIO_CUE_STR) != null );
			if ( isCue )
			{
				float time = SnapTimeToFrameRate(GuiPosToAnimTime(rect,e.mousePosition.x));
				DrawRect( new Rect( AnimTimeToGuiPos(rect, time), rect.y, 1, rect.height), COLOR_INSERT_FRAMES_LINE );
				DrawLine(new Vector2(AnimTimeToGuiPos(rect, time), rect.yMin),new Vector2(AnimTimeToGuiPos(rect, time), rect.yMax), COLOR_INSERT_FRAMES_LINE);
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				m_dragDropHovering2 = true;
			
				if ( e.type == EventType.DragPerform )
				{
					DragAndDrop.AcceptDrag();				
					foreach (Object obj in DragAndDrop.objectReferences)
					{
						if ( obj is GameObject && (obj as GameObject).GetComponent(AUDIO_CUE_STR) != null )
						{	
							AddSoundEvent(time,obj);
						}
					}
				
					ApplyChanges();
					Repaint();
				
				}
			}
		
		}
		
		if ( e.type == EventType.DragExited || rect.Contains( e.mousePosition ) == false )
		{
			m_dragDropHovering2 = false;
		}
		else 
		if ( m_dragDropHovering2 )
		{
			float time = SnapTimeToFrameRate(GuiPosToAnimTime(rect,e.mousePosition.x));
			DrawRect( new Rect( AnimTimeToGuiPos(rect, time), rect.y, 1, rect.height), COLOR_INSERT_FRAMES_LINE );			
		}
	}

	partial void exOnDragDropTimeline( Event e, Rect rect )
	{	
		bool isCue = System.Array.Exists( DragAndDrop.objectReferences, item => item is GameObject && (item as GameObject).GetComponent(AUDIO_CUE_STR) != null );
		if ( isCue )
		{

			int closestFrame = 0;
			
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			closestFrame = MousePosToInsertFrameIndex(rect);
			LayoutInsertFramesLine(rect, closestFrame);			
			
			m_dragDropHovering = true;

			if ( e.type == EventType.DragPerform )
			{
				DragAndDrop.AcceptDrag();

				foreach (Object obj in DragAndDrop.objectReferences)
				{
					if ( obj is GameObject && (obj as GameObject).GetComponent(AUDIO_CUE_STR) != null )
					{	
						AddSoundEvent(m_frames[closestFrame].m_time,obj);
					}
				}
				ApplyChanges();
				Repaint();
			}
			
		}
	}

	void AddSoundEvent(float time, Object obj)
	{
		InsertEvent( time, true );
		Debug.Assert(m_selectedEvents.Count == 1);
		//if ( m_selectedEvents.Count == 1 )
									
		m_selectedEvents[0].m_functionName = "Sound";
		m_selectedEvents[0].m_sendUpwards = true;
		m_selectedEvents[0].m_usePrefix = true;
		m_selectedEvents[0].m_paramType = eAnimEventParameter.Object;
		m_selectedEvents[0].m_paramObjectReference = obj;
		
	}

	

}
}
