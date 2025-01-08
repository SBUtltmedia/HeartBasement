using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;


namespace PowerTools.Quest
{ 

[ExecuteAlways/*, RequireComponent(typeof(PowerSprite), typeof(Sortable))*/]
public class CharacterPreview : MonoBehaviour
{
	[SerializeField] CharacterComponent m_character = null;

	[SerializeField, HideInInspector] PowerSprite m_characterSprite = null;

	RoomComponent m_room = null;
	PowerSprite m_spriteComponent = null;
	List<RegionComponent> m_regionComponents = null;
	
	public CharacterComponent Character 
	{ 
		get => m_character;
		set 
		{
			m_characterSprite = null;
			m_character = value; 
			Update(); 
		}
	}
	
	public List<RegionComponent> Regions => m_regionComponents;

    // Start is called before the first frame update
    void Awake()
    {
		if ( Application.isPlaying )
			Destroy(gameObject);
    }

	
	void Update()
	{
		if (m_spriteComponent == null)
			m_spriteComponent = GetComponentInChildren<PowerSprite>(true);

		if ( m_room == null )
			m_room = GameObject.FindFirstObjectByType<RoomComponent>();
			
		if ( m_room != null )		
			m_regionComponents = m_room.GetRegionComponents();

		if ( m_character != null && (m_characterSprite == null || m_characterSprite.transform != m_character.transform) )
		{ 
			// Character changed
			m_characterSprite = m_character.GetComponent<PowerSprite>();
				
			// Set sprite offset
			if ( m_spriteComponent != null && m_characterSprite != null )
				m_spriteComponent.Offset = m_characterSprite.Offset;

			m_spriteComponent.SetShaderOverride(m_characterSprite.GetShaderOverride());

			// Set default sprite			
			GetComponent<SpriteRenderer>().sprite = m_characterSprite.GetComponent<SpriteRenderer>().sprite;
			
		}

		if ( m_regionComponents != null )		
			UpdateRegions();
	}
	
	void UpdateRegions()
	{
			
		Vector2 characterPos = transform.position;
		Color tint = new Color(1,1,1,0);
		float scale = 1;

		foreach( RegionComponent regionComponent in m_regionComponents)
		{
			if ( regionComponent == null )
				continue; // probably been deleted

			regionComponent.InitializeCollider();
			Region region = regionComponent.GetData();
			if ( region.Enabled && regionComponent.GetPolygonCollider().OverlapPoint(characterPos) )
			{ 
				
				//if ( character.UseRegionScaling )
				{
					float tmpScale = regionComponent.GetScaleAt(characterPos);
					if ( tmpScale != 1 )
						scale = tmpScale;
				}						
				//if ( character.UseRegionTinting )
				{
					if ( region.Tint.a > 0 )
					{
						float ratio = regionComponent.GetFadeRatio(characterPos);
						if ( tint.a <= 0 )
						{
							tint = region.Tint;
							tint.a *= ratio;
						}
						else 
						{
							Color newCol = region.Tint;
							tint = Color.Lerp(tint, newCol, ratio);
						}
					}
				}
			}
		}

		// Apply scale to character
		transform.localScale = new Vector3(scale*Mathf.Sign(transform.localScale.x), scale, scale);
						
		// Apply tint to character
		if ( m_spriteComponent != null )
			m_spriteComponent.Tint = tint;
		
			
	}
}
}