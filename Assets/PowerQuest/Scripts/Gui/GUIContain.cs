using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PowerTools.Quest
{


// Better version of GUIContain. but didn't want to break existing prefabs, so didn't just update the old one :/
[ExecuteInEditMode]
public class GUIContain : MonoBehaviour 
{
	[SerializeField] List<GameObject> m_containX = null;
	[SerializeField] List<GameObject> m_containY = null;
	[SerializeField] Vector2 m_padding = Vector2.zero;

	Vector2 m_spriteSizeInverted = Vector2.one;
	Transform m_transform = null;
	SpriteRenderer m_sprite = null;

	public void UpdateSize()
	{

		bool sliced = ( m_sprite != null &&  m_sprite.drawMode == SpriteDrawMode.Sliced );
		Vector2 newScale = m_transform.localScale;
		if ( sliced )	
		    newScale = m_sprite.size;
		
		Bounds bounds = new Bounds( m_transform.position, Vector3.zero);
		if ( m_containX != null &&  m_containX.Count > 0 )
		{
			for ( int i = 0; i < m_containX.Count; ++i )
			{
				GameObject contain = m_containX[i];
				if ( contain && contain.activeInHierarchy && contain.GetComponent<Renderer>() != null)		
				{
					// If what we're containing is also a container, make sure that one's updated first
					GUIContain otherContain = contain.GetComponent<GUIContain>();
					if ( otherContain )
						otherContain.UpdateSize();

					Bounds otherBounds = contain.GetComponent<Renderer>().bounds;
					bounds.Encapsulate( otherBounds );

				}
			}
			if ( sliced )
				newScale.x = (bounds.size.x + (m_padding.x*2) + Mathf.Abs(bounds.center.x-transform.position.x)*2.0f );
			else 
				newScale.x = (bounds.size.x + (m_padding.x*2) + Mathf.Abs(bounds.center.x-transform.position.x)*2.0f ) * m_spriteSizeInverted.x;
		}
		
		
		if ( m_containY != null && m_containY.Count > 0 )
		{
			for ( int i = 0; i < m_containY.Count; ++i )
			{
				GameObject contain = m_containY[i];
				if ( contain && contain.activeInHierarchy && contain.GetComponent<Renderer>() != null )
				{		
					// If what we're containing is also a container, make sure that one's updated first
					GUIContain otherContain = contain.GetComponent<GUIContain>();
					if ( otherContain )
						otherContain.UpdateSize();

					Bounds otherBounds = contain.GetComponent<Renderer>().bounds;
					bounds.Encapsulate( otherBounds );
				}
			}	
			if ( sliced )	
				newScale.y = (bounds.size.y + (m_padding.y*2) + Mathf.Abs(bounds.center.y-transform.position.y)*2.0f);
			else
				newScale.y = (bounds.size.y + (m_padding.y*2) + Mathf.Abs(bounds.center.y-transform.position.y)*2.0f) * m_spriteSizeInverted.y;
		}

		//Debug.DrawLine(new Vector3(bounds.min.x,bounds.min.y,0),new Vector3(bounds.max.x,bounds.max.y,-10),Color.cyan);

		if ( sliced )	
		    m_sprite.size = newScale;
		else 
			m_transform.localScale = newScale.WithZ(1);
	}

	public void ContainX(GameObject obj)
	{
		m_containX.Add(obj);
	}

	public void ContainY(GameObject obj)
	{
		m_containY.Add(obj);
	}
	
	// Use this for initialization
	void Awake() 
	{
		m_transform = transform;
		m_sprite = GetComponent<SpriteRenderer>();
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
	
	// Update is called once per frame
	void LateUpdate() 
	{
		UpdateSize();
	}
}
}