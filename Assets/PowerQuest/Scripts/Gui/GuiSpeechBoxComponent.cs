using UnityEngine;
using System.Collections;
using PowerTools;

namespace PowerTools.Quest
{


public class GuiSpeechBoxComponent : GuiComponent, ISpeechGui
{
	[SerializeField] bool m_usePlayerTextColour = true;

	QuestText m_text = null;
	SpriteRenderer m_sprite = null;
	SpriteAnim m_spriteAnimator = null;

	Character m_character = null;
	int m_currLineId = -1;

	public void StartSay( Character character, string text, int currLineId, bool background )
	{		
		GetData().Show();

		m_character = character;
		m_currLineId = currLineId;

		m_text.text = text;
		if ( m_usePlayerTextColour )
			m_text.color = m_character.TextColour;

		CharacterComponent prefabComponent = character.GetPrefab().GetComponent<CharacterComponent>();

		// Make object active so can start anim before "visible", this is set back afterwards
		bool wasActive = gameObject.activeSelf;
		gameObject.SetActive(true);

		// Find and play character's talk anim (non facing)
		AnimationClip clip = prefabComponent.GetAnimations().Find(item=>string.Equals(m_character.AnimTalk, item.name, System.StringComparison.OrdinalIgnoreCase));
		if ( clip != null )
		{
			m_sprite.enabled = true;
			gameObject.SetActive(true);
			m_spriteAnimator.Play(clip);

		}
		else
		{
			m_sprite.enabled = false;
		}			

		// update immediately
		Update();
		
		gameObject.SetActive(wasActive);
	}

	public void EndSay( Character character )
	{
		GetData().Hide();
	}

	// Use this for initialization
	void Awake() 
	{
		m_text = GetComponentInChildren<QuestText>(true);		
		m_spriteAnimator = GetComponentInChildren<SpriteAnim>(true);
		m_sprite = m_spriteAnimator.GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update() 
	{
		if ( m_character != null && m_character.LipSyncEnabled )
	    {
	        // Update frames for lip sync
			TextData data = SystemText.FindTextData( m_currLineId, m_character.ScriptName );
	        // get time from audio source
	        float time = 0; 
			if ( m_character.GetDialogAudioSource() != null )
				time = m_character.GetDialogAudioSource().time;

			// Get character from time			
			int index = -1;
			if ( data != null )
				index = System.Array.FindIndex( data.m_phonesTime, item => item > time );
	        index--;
	        char character = 'X'; // default is mouth closed
	        if ( index >= 0 && index < data.m_phonesCharacter.Length )
	            character = data.m_phonesCharacter[index];

	        // map character to frame
			int characterId = character-'A';

			//Debug.Log(character+": "+characterId+", "+((float)characterId+0.5f)/7.0f);
	        m_spriteAnimator.SetNormalizedTime( ((float)characterId+0.5f)/7.0f);
			m_spriteAnimator.Pause();

	    }
	}
}
}
