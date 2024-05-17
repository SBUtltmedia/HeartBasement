using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

// This can be used for more complex inventory items, where you want to choose which sprite is used for the actual Item image
public class InventoryPanelItem : MonoBehaviour // Note for future: This could be made to inherit from GuiControl so each item is a gui control itself. That might make it easier to have the active control be the specific item in the gui...
{
	[SerializeField] SpriteRenderer m_itemSpriteComponent = null;

	SpriteAnim m_animComponent = null;
	
	// Caching image so we only have to change it if it changes
	string m_cachedImage = null;

	public Sprite GetSpriteName() { return m_itemSpriteComponent != null ? m_itemSpriteComponent.sprite : null; }
	public AnimationClip GetAnimName() { return m_animComponent != null ? m_animComponent.Clip : null; }

	public string GetCachedAnimSpriteName() { return m_cachedImage; }

	public void SetInventorySprite(Sprite sprite)
	{
		if ( sprite == null )
			return;

		if ( m_itemSpriteComponent == null )
			m_itemSpriteComponent = GetComponentInChildren<SpriteRenderer>();

		if ( m_animComponent != null )
			m_animComponent.Stop();

		if ( m_itemSpriteComponent != null )
		{
			m_itemSpriteComponent.sprite = sprite;
			m_cachedImage = sprite.name;
		}
	}	
	public void SetInventoryAnim(AnimationClip anim)
	{
		if ( anim == null )
			return;
		if ( m_animComponent == null )
		{
			m_itemSpriteComponent = GetComponentInChildren<SpriteRenderer>();
			m_animComponent = m_itemSpriteComponent.GetComponent<SpriteAnim>();
		}

		if ( m_animComponent != null )
		{
			m_animComponent.Play(anim);
			m_cachedImage = anim.name;
		}
	}


}
