using UnityEngine;
using System.Collections;
using PowerTools;

public class Effect : MonoBehaviour 
{
	[SerializeField] int m_baseline = 0;
	[SerializeField] bool m_destroyOnAnimEnd = true;

	SpriteAnim m_spriteAnimator = null;
	SpriteRenderer m_sprite = null;


	// Use this for initialization
	void Start () 
	{		
		m_sprite = GetComponent<SpriteRenderer>();
		m_spriteAnimator = GetComponent<SpriteAnim>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		if ( m_destroyOnAnimEnd && m_spriteAnimator != null && m_spriteAnimator.IsPlaying() == false )
		{
			Destroy(gameObject);
		}

		// Update sorting order
		if ( m_sprite != null )
		{
			m_sprite.sortingOrder = -Mathf.RoundToInt((transform.position.y + m_baseline)*10.0f);
		}
	}
}
