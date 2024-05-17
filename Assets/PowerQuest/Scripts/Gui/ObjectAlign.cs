using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PowerTools.Quest
{


[ExecuteInEditMode]
public class ObjectAlign : MonoBehaviour 
{
	public enum eAlignHorizontal
	{
		None,Left,Center,Right,Position
		
	}
	
	public enum eAlignVertical
	{
		None,Top,Middle,Bottom,Position
	}
		
	public Transform m_object;
	public eAlignHorizontal m_horizontal = eAlignHorizontal.None;
	public eAlignVertical m_vertical = eAlignVertical.None;	
	public Vector2 m_offset;
	public Vector2 m_offsetRatio;	
	
	Transform m_transform = null;
	Renderer m_objectRenderer = null;
	
	// Use this for initialization
	void Awake () 
	{
		m_transform = transform;
	}
	
	// Update is called once per frame
	void LateUpdate() 
	{
		UpdatePos();
	}

	public void UpdatePos()
	{		
		if (m_object == null )
			return;
		if ( m_objectRenderer == null )
			m_objectRenderer = m_object.GetComponent<Renderer>();


		if ( m_object.gameObject.activeInHierarchy && m_object.GetComponent<Renderer>() != null)		
		{
			// If what we're aligning to is a container, make sure that one's updated first
			GUIContain otherContain = m_object.GetComponent<GUIContain>();
			if ( otherContain )
				otherContain.UpdateSize();

			// If what we're aligning to is an ObjectAlign, make sure that one's updated first
			ObjectAlign otherAlign = m_object.GetComponent<ObjectAlign>();
			if ( otherAlign )
				otherAlign.UpdatePos();
		}

		
		Bounds objectBounds = new Bounds(transform.position,Vector2.zero);
		if ( m_objectRenderer != null && m_objectRenderer.enabled  )
			objectBounds = m_object.GetComponent<Renderer>().bounds;

		Vector3 position = Vector3.zero;
		switch ( m_horizontal )
		{
			default: break;
			case eAlignHorizontal.Left:		position.x = objectBounds.min.x;		break;
			case eAlignHorizontal.Center:	position.x = objectBounds.center.x;		break;
			case eAlignHorizontal.Right:	position.x = objectBounds.max.x;		break;	
			case eAlignHorizontal.Position:	position.x = m_object.position.x;		break;	
		}
		switch ( m_vertical )
		{
			default: break;
			case eAlignVertical.Top:		position.y = objectBounds.max.y;		break;
			case eAlignVertical.Middle:		position.y = objectBounds.center.y;		break;
			case eAlignVertical.Bottom:		position.y = objectBounds.min.y;		break;	
			case eAlignVertical.Position:	position.y = m_object.position.y;		break;		
		}
		
		position += (Vector3)m_offset;
		Vector2 offsetRatioFinal = m_offsetRatio;
		offsetRatioFinal.Scale( new Vector2(objectBounds.size.x, objectBounds.size.y) );
		position += (Vector3)offsetRatioFinal;
		
		m_transform.position = new Vector3( 
			(m_horizontal == eAlignHorizontal.None) ? m_transform.position.x :  position.x,
			(m_vertical == eAlignVertical.None) ? m_transform.position.y :  position.y,
			m_transform.position.z);
	}
}
}