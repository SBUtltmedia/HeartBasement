using UnityEngine;
using System.Collections;

namespace PowerTools.Quest
{


[ExecuteInEditMode]
[AddComponentMenu("Quest Gui Layout/Align To Screen")]
public class AlignToScreen : MonoBehaviour 
{
	public enum eAlignHorizontal { None,Left,Center,Right }	
	public enum eAlignVertical { None,Top,Middle,Bottom	}
	[Header("Align to the Screen's...")]
	[SerializeField] eAlignVertical m_vertical = eAlignVertical.None;	
	[SerializeField] eAlignHorizontal m_horizontal = eAlignHorizontal.None;
	[Header("With offset...")]
	[SerializeField] Vector2 m_offset = Vector2.zero;
	[SerializeField] Vector2 m_offsetRatio = Vector2.zero;
	[Header("Optional camera override")]
	[SerializeField] Camera m_camera = null;
		
	public Vector2 Offset { get { return m_offset; } set { m_offset = value; ForceUpdate(); } }
	public Vector2 OffsetRatio { get { return m_offsetRatio; } set { m_offsetRatio = value; ForceUpdate(); } }
		
	float m_timeToUpdate = 0;
	
	public void ForceUpdate() // Stuff isn't aligned eveyr frame, so if you need to do it immediate, call this.
	{
		m_timeToUpdate = 0;
		if ( isActiveAndEnabled )
			Update();		
	}
	
	// Use this for initialization
	void Start() 
	{
		ForceUpdate();
	}

	void OnEnable()
	{
		ForceUpdate();
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
			m_camera = PowerTools.Quest.GuiUtils.FindGuiCamera();

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
		if ( PowerQuest.GetValid() && PowerQuest.Get.GetSnapToPixel() )
			position = position.Snap(PowerQuest.Get.SnapAmount);
		position.z = transform.position.z;
		transform.position = new Vector3( 
			(m_horizontal == eAlignHorizontal.None) ? transform.position.x :  position.x,
			(m_vertical == eAlignVertical.None) ? transform.position.y :  position.y,
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
