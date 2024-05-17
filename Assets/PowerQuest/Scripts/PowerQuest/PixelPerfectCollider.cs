using UnityEngine;
using System.Collections;

namespace PowerTools.Quest
{

public class PixelPerfectCollider : MonoBehaviour 
{

	// NB: THIS DOESN'T WORK AT ALL, WAS JUST AN EXPERIMENT IN PROGRESSS LOL!

	SpriteRenderer m_sprite = null;
	
	// Use this for initialization
	void Awake() 
	{
		m_sprite = GetComponent<SpriteRenderer>();	
	}
	
	
	// Update is called once per frame
	void Update() 
	{
		if ( Input.GetMouseButtonDown(0) )
		{
			if ( PointOverlapping( PowerQuest.Get.GetMousePosition() ) )
			{
				Debug.Log("Clicked "+gameObject.name);
			}
			else 
			{
				Debug.Log("Missed "+gameObject.name);
			}			
		}	
	}
	
	bool PointOverlapping(Vector2 point)
	{
		
		Sprite sprite = m_sprite.sprite;


		Rect spritebounds = sprite.rect;		
		
		if ( spritebounds.Contains(point) )
		{			
		
			// Calc point offset
			
			float ratioX = Mathf.InverseLerp(spritebounds.xMin, spritebounds.xMax, point.x);
			float ratioY = Mathf.InverseLerp(spritebounds.yMin, spritebounds.yMax, point.y);
			
			float u = Mathf.Lerp (	m_sprite.sprite.uv[0].x, m_sprite.sprite.uv[2].x, ratioX);
			float v = Mathf.Lerp (	m_sprite.sprite.uv[0].y, m_sprite.sprite.uv[1].y, ratioY);			
			
			Texture2D texture = (Texture2D)sprite.texture;
			
			Debug.Log(string.Format("Ratio: ( {0}, {1} ), UV: ( {2}, {3} ), Pixel: ( {4}, {5} )", ratioX,ratioY,u,v,(int)(u*texture.width), (int)(v*texture.height) ) );			
			if ( texture.GetPixelBilinear(u,v).a > 0.1f )//(int)(u*texture.width),(int)(v*texture.height)).a > 0.5f ) 
				return true;
		}
		return false;
	}
}

}