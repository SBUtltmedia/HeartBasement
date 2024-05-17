using UnityEngine;
using System.Collections;

namespace PowerTools
{


[ExecuteInEditMode]
public class ScreenAlign : MonoBehaviour 
{
	public enum eAlignHorizontal
	{
		None,Left,Center,Right
	}
	
	public enum eAlignVertical
	{
		None,Top,Middle,Bottom
	}
	
	[SerializeField] Camera m_camera;
	[SerializeField] eAlignHorizontal m_horizontal = eAlignHorizontal.None;
	[SerializeField] eAlignVertical m_vertical = eAlignVertical.None;	
	[SerializeField] Vector2 m_offset;
	[SerializeField] Vector2 m_offsetRatio;

	public Vector2 Offset { get { return m_offset; } set { m_offset = value; ForceUpdate(); } }
	public Vector2 OffsetRatio { get { return m_offsetRatio; } set { m_offsetRatio = value; ForceUpdate(); } }

	
	Transform m_transform = null;
	//SpriteRenderer m_sprite = null;	
	float m_timeToUpdate = 0;
	
	public void ForceUpdate() // Stuff isn't aligned eveyr frame, so if you need to do it immediate, call this.
	{
		m_timeToUpdate = 0;
	}
	
	// Use this for initialization
	void Start () 
	{
		m_transform = transform;
		/*if ( m_scaleHorizontal > 0 || m_scaleVertical > 0 )
		{
			m_sprite = GetComponent<SpriteRenderer>();
		}*/
		Update();
	}	
	
		
	// Update is called once per frame
	void Update() 
	{	
		if ( Application.isPlaying )
		{
			// This is a little expensive, and doesn't really need to be done continuously	
			m_timeToUpdate -= Time.deltaTime;
			if ( m_timeToUpdate >= 0 )
				return;
			m_timeToUpdate = 0.25f;
		}
		
	
		if (m_camera == null)
		{
			Camera[] cameras = new Camera[10];
			int count = Camera.GetAllCameras(cameras);

			// Take a guess at which is a gui camera
			for ( int i = 0; i < count && i < cameras.Length; ++i )
			{
				Camera cam = cameras[i];
				if ( cam.gameObject.layer == 5 || cam.gameObject.name.Contains("GUI") || cam.gameObject.name.Contains("Menu") )
				{
					m_camera = cam;
					break;
				}
			}
			if ( m_camera == null && cameras.Length > 0 )
				m_camera = cameras[0];
		}
		if (m_camera == null )
			return;

		Rect cameraRect = m_camera.pixelRect;

		Vector3 position = Vector3.zero;
		switch ( m_horizontal )
		{
			default: break;
			case eAlignHorizontal.Left:		position.x = cameraRect.xMin;		break;
			case eAlignHorizontal.Center:	position.x = cameraRect.center.x;	break;
			case eAlignHorizontal.Right:	position.x = cameraRect.xMax;		break;		
		}
		switch ( m_vertical )
		{
			default: break;
			case eAlignVertical.Top:	position.y = cameraRect.yMax;		break;
			case eAlignVertical.Middle:	position.y = cameraRect.center.y;	break;
			case eAlignVertical.Bottom:	position.y = cameraRect.yMin;		break;		
		}

		Vector2 offsetRatioFinal = m_offsetRatio;
		offsetRatioFinal.Scale( new Vector2(cameraRect.width, cameraRect.height) );
		position += (Vector3)offsetRatioFinal;
		
		position = m_camera.ScreenToWorldPoint(position);
		position += (Vector3)m_offset;
		position.z = m_transform.position.z;
		m_transform.position = new Vector3( 
			(m_horizontal == eAlignHorizontal.None) ? m_transform.position.x :  position.x,
			(m_vertical == eAlignVertical.None) ? m_transform.position.y :  position.y,
			position.z);

		/*
		if ( m_sprite != null )
		{
			if ( m_scaleVertical > 0 )
			{
				m_sprite.height = m_camera.orthographicSize * m_scaleVertical * 2;
			}
			if ( m_scaleHorizontal > 0 )
			{
				m_sprite.width = m_camera.orthographicSize * m_scaleHorizontal * m_camera.aspect * 2;
			}
		}*/
	}
}

}