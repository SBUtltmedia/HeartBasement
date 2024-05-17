using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{

[ExecuteInEditMode]
[AddComponentMenu("Quest Gui Layout/Align To Object")]
public class AlignToObject : MonoBehaviour 
{
	public enum eAlignHorizontal { None,Left,Center,Right,Position }
	public enum eAlignVertical { None,Top,Middle,Bottom,Position }
		
	[Header("Align to the...")]
	public eAlignVertical m_vertical = eAlignVertical.None;	
	public eAlignHorizontal m_horizontal = eAlignHorizontal.None;
	[Header("...side of ...")]
	public Transform m_object = null;
	[Header("...with offset...")]
	public Vector2 m_offset = Vector2.zero;
	public Vector2 m_offsetRatio = Vector2.zero;	
	
	Transform m_transform = null;
	Renderer m_objectRenderer = null;
	GuiControl m_objectControl = null;

	int m_callsThisFrame = 0;
	
	// Use this for initialization
	void Awake () 
	{
		m_transform = transform;
	}
	
	// Update is called once per frame
	void LateUpdate() 
	{
		UpdatePos();
		// After we've 'definitely called it this frame, reset the 'calls this frame'
		m_callsThisFrame = 0;
	}
	
	static int s_debugCalls = 0;

	public void UpdatePos()
	{		
		if (m_object == null )
			return;
						
		// Don't update if already updated this frame?
		if ( gameObject.activeInHierarchy )
		{
			++m_callsThisFrame;
			if ( m_callsThisFrame > s_debugCalls )
			{
				s_debugCalls = m_callsThisFrame;
				if ( s_debugCalls > 2 )
					Debug.Log($"Detected {m_callsThisFrame} Recursive AlignToObject calls in: {gameObject.name}");
			}		
		}

		// Cache control and renderer objects
		if ( m_objectControl == null || m_objectControl.transform != m_object )
			m_objectControl = m_object.GetComponent<GuiControl>();
		
		if ( m_objectControl == null && (m_objectRenderer == null || m_objectRenderer.transform != m_object) )
			m_objectRenderer = m_object.GetComponent<Renderer>();
			
		bool foundObjectBounds = false;

		// Update non-control object
		if ( m_objectControl == null && m_object.gameObject.activeInHierarchy && m_objectRenderer != null)		
		{			
			// If what we're aligning to is a container, make sure that one's updated first
			FitToObject otherContain = m_object.GetComponentInChildren<FitToObject>(false);
			if ( otherContain && otherContain.isActiveAndEnabled )
				otherContain.UpdateSize();
		}	
		if ( m_objectControl == null )		
		{			
			// If what we're aligning to is an AlignToObject, make sure that one's updated first
			AlignToObject otherAlign = m_object.GetComponent<AlignToObject>();
			if ( otherAlign && otherAlign.enabled )
				otherAlign.UpdatePos();
		}

		RectCentered objectBounds = new RectCentered(m_object.position,Vector2.zero);		
		if ( m_objectControl != null && m_objectControl.enabled )
		{
			m_objectControl.UpdateFitAndAlign();
			if ( m_objectControl.isActiveAndEnabled )
			{
				objectBounds = m_objectControl.GetRect(transform);
				foundObjectBounds = true;
			}
		}
		else if ( m_objectRenderer != null && m_objectRenderer.enabled && m_object != null )
		{
			objectBounds = new RectCentered(m_object.GetComponent<Renderer>().bounds);
			foundObjectBounds = true;
		}

		Vector3 position = Vector3.zero;
		switch ( m_horizontal )
		{
			default: break;
			case eAlignHorizontal.Left:		position.x = objectBounds.MinX;		break;
			case eAlignHorizontal.Center:	position.x = objectBounds.CenterX;	break;
			case eAlignHorizontal.Right:	position.x = objectBounds.MaxX;		break;	
			case eAlignHorizontal.Position:	position.x = m_object.position.x;	break;	
		}
		switch ( m_vertical )
		{
			default: break;
			case eAlignVertical.Top:		position.y = objectBounds.MaxY;		break;
			case eAlignVertical.Middle:		position.y = objectBounds.CenterY;	break;
			case eAlignVertical.Bottom:		position.y = objectBounds.MinY;		break;	
			case eAlignVertical.Position:	position.y = m_object.position.y;	break;		
		}
		
		// Add the offset
		if ( foundObjectBounds )
		{
			// Note that if the object wasn't found (or was disabled) the offset isn't applied. (So in a string of aligned objects, some can be removed and others will still line up correctly)
			//		But also note that it won't be updating it's own alignment, so maybe aligning to it will not really work... 
			position += (Vector3)m_offset;
			Vector2 offsetRatioFinal = m_offsetRatio;
			offsetRatioFinal.Scale( new Vector2(objectBounds.Width, objectBounds.Height) );
			position += (Vector3)offsetRatioFinal;
		}
		
		m_transform.position = new Vector3( 
			(m_horizontal == eAlignHorizontal.None) ? m_transform.position.x :  position.x,
			(m_vertical == eAlignVertical.None) ? m_transform.position.y :  position.y,
			m_transform.position.z);
	}
}
}
