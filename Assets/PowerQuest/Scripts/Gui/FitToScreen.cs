using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;

namespace PowerTools.Quest
{

[ExecuteInEditMode]
[AddComponentMenu("Quest Gui Layout/Fit To Screen")]
public class FitToScreen : MonoBehaviour 
{	
	[SerializeField] bool m_fitWidth = false;
	[SerializeField] bool m_fitHeight = false;

	[SerializeField] Padding m_padding = Padding.zero;
	[SerializeField] bool m_snapToPixel = false;
	
	// TODO: Allow you to fit to % of screen size. (eg: 0.5 of screen)
	// [SerializeField] Padding m_paddingRatio = Padding.zero;

	[Header("Camera override (optional")]
	[SerializeField] Camera m_camera=null;

	Vector2 m_spriteSizeInverted = Vector2.one;
	SpriteRenderer m_sprite = null;


	public void UpdateSize()
	{
		if ( m_sprite == null && Application.isPlaying == false )
			SetupSprite();

		bool sliced = ( m_sprite != null &&  m_sprite.drawMode != SpriteDrawMode.Simple );
		Vector2 newPos = transform.position;
		Vector2 newScale = transform.localScale;
		if ( sliced )	
		    newScale = m_sprite.size;

		if (m_camera == null)
			m_camera = GuiUtils.FindGuiCamera();
		if (m_camera == null )
			return;

		RectCentered bounds = new RectCentered( m_camera.pixelRect );
		bounds.Min = m_camera.ScreenToWorldPoint(bounds.Min);
		bounds.Max = m_camera.ScreenToWorldPoint(bounds.Max);

		if ( m_snapToPixel )
		{
			bounds.MinX = Utils.Snap(bounds.MinX);
			bounds.MaxX = Utils.Snap(bounds.MaxX);
		}

		if ( m_fitWidth )
		{
			if ( sliced )
				newScale.x = bounds.Width + m_padding.width;
			else 
				newScale.x = bounds.Width * m_spriteSizeInverted.x + m_padding.width;	
			newPos.x = bounds.Center.x + (-m_padding.left + m_padding.right)*0.5f;	
		}
		if ( m_fitHeight )
		{
			if ( sliced )
				newScale.y = bounds.Height + m_padding.height;
			else 
				newScale.y = bounds.Height * m_spriteSizeInverted.y + m_padding.height;		
			newPos.y = bounds.Center.y + (-m_padding.bottom + m_padding.top)*0.5f;
		}

		if ( sliced )	
		    m_sprite.size = newScale;
		else 
			transform.localScale = newScale.WithZ(1);
		transform.position = newPos.WithZ(transform.position.z);
	}


	public Padding Padding {get{return m_padding;} set {m_padding = value; UpdateSize(); }}

	
	// Use this for initialization
	void Awake() 
	{
		SetupSprite();
	}
	void SetupSprite()
	{
		m_sprite = GetComponentInChildren<SpriteRenderer>();
		Vector2 spriteSize = Vector2.one;
		if ( m_sprite != null && m_sprite.sharedMaterial != null && m_sprite.sharedMaterial.mainTexture != null )
		{
			spriteSize = new Vector2(m_sprite.sharedMaterial.mainTexture.width, m_sprite.sharedMaterial.mainTexture.height);
			//spriteSize.Scale(sprite.scale);
		}
		m_spriteSizeInverted.x = 1.0f/spriteSize.x;
		m_spriteSizeInverted.y = 1.0f/spriteSize.y;	
	}
	
	void OnEnable()
	{
		UpdateSize();
	}

	float m_timeToUpdate = 0;
	
	public void ForceUpdate() // Stuff isn't aligned eveyr frame, so if you need to do it immediate, call this.
	{
		m_timeToUpdate = 0;
		
	}
	

	// Update is called once per frame
	void LateUpdate() 
	{
		if ( Application.isPlaying )
		{
			// This is a little expensive, and doesn't really need to be done continuously	
			m_timeToUpdate -= Time.deltaTime;
			if ( m_timeToUpdate >= 0 )
				return;
			m_timeToUpdate = 0.25f;
		}
		
		UpdateSize();

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
