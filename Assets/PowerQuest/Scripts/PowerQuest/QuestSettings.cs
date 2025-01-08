using UnityEngine;
using System.Collections;

namespace PowerTools.Quest
{


/// Settings that can be set by players in game.
/*
	These are saved seperately to the individual game saves, so a player's preferences are maintained throughout the game
	You can add data to this partial class, and implement the "ExtentionOnInitialise()" function which is called on launch and after loading the game.
*/
[System.Serializable]
public partial class QuestSettings
{	
	/// Whether dialog's displayed, audio only, or both
	public enum eDialogDisplay
	{
		TextAndSpeech,
		SpeechOnly,
		TextOnly
	};

	/// Gets/sets the master volume (0 to 1)
	public float Volume 
	{ 
		get { return m_masterVolume; } 
		set
		{ 
			m_masterVolume = value; 
			AudioListener.volume = value;
		} 
	}

	/// Gets/sets the music volume (0 to 1)
	public float VolumeMusic
	{ 
		get { return m_musicVolume; } 
		set
		{ 
			m_musicVolume = value; 
			SystemAudio.Get.SetVolume(AudioCue.eAudioType.Music, value);
		} 
	}

	/// Gets/sets the sound effect volume (0 to 1)
	public float VolumeSFX
	{ 
		get { return m_sfxVolume; } 
		set
		{ 
			m_sfxVolume = value; 
			SystemAudio.Get.SetVolume(AudioCue.eAudioType.Sound, value);
		} 
	}

	/// Gets/sets the dialog volume (0 to 1)
	public float VolumeDialog
	{ 
		get { return m_dialogVolume; } 
		set
		{ 			
			m_dialogVolume = value;
			SystemAudio.Get.SetVolume(AudioCue.eAudioType.Dialog, value);
		} 
	}

	/// Gets/Sets the speed of text in the game
	public float TextSpeedMultiplier
	{
		get => m_textSpeedMultiplier;
		set => m_textSpeedMultiplier=value;
	}

	/// Gets/sets the dialog display style (0 to 1)
	public eDialogDisplay DialogDisplay
	{ 
		get { return m_dialogDisplay; }
		set { m_dialogDisplay = value; }
	}
	
	public CursorLockMode m_lockCursor = CursorLockMode.Confined;
	/// Gets or sets whether the cursor is locked to the screen.  Note, in editor it's always unlocked.
	public CursorLockMode LockCursor 
	{ 
		get => m_lockCursor; 
		set 
		{
			m_lockCursor = value;			
			if ( Application.isEditor == false )
				UnityEngine.Cursor.lockState = m_lockCursor;
		} 
	}
	
	/// Initialises the system (Called from inside PowerQuest)
	public void OnInitialise()
	{
		AudioListener.volume = m_masterVolume;
		SystemAudio.Get.SetVolume(AudioCue.eAudioType.Music, m_musicVolume);
		SystemAudio.Get.SetVolume(AudioCue.eAudioType.Sound, m_sfxVolume);
		SystemAudio.Get.SetVolume(AudioCue.eAudioType.Dialog, m_dialogVolume);
		LockCursor = m_lockCursor;
		Language = m_languageCode;
		ExOnInitialise();
	}

	partial void ExOnInitialise();

	/// Initialises the system (Called from inside PowerQuest)
	public void OnPostRestore( int version )
	{
		OnInitialise();
	}

	/// Gets/Sets the current language by code
	public string Language
	{
		get { return m_languageCode; }
		set 
		{ 
			if ( SystemText.Get.SetLanguage(value) )
				m_languageCode = value; 
		}
	}

	/// Gets data for the currently selected language
	public LanguageData LanguageData => Systems.Text.GetLanguageData();
	
	/// Gets/Sets the current language by id
	public int LanguageId
	{
		get { return SystemText.Get.GetLanguage(); }
		set 
		{ 
			// Find the language code
			if ( Systems.Text.GetLanguages().IsIndexValid(value) == false )			
			{
				Debug.LogWarning("Couldn't find language id: "+value);
				return;
			}
			m_languageCode = Systems.Text.GetLanguages()[value].m_code; 
			SystemText.Get.SetLanguage(value);
		}
	}

	/// Get's the current language
	public LanguageData[] GetLanguages() { return SystemText.Get.GetLanguages(); }

	//
	// Private vars, these are what are saved
	//
	[SerializeField] float m_masterVolume = 1.0f;
	[SerializeField] float m_musicVolume = 1.0f;
	[SerializeField] float m_sfxVolume = 1.0f;
	[SerializeField] float m_dialogVolume = 1.0f;
	[SerializeField] eDialogDisplay m_dialogDisplay = eDialogDisplay.TextAndSpeech;
	[Tooltip("The Default Language. Should match codes set in SystemText")]
	[SerializeField] string m_languageCode = "EN";
	[SerializeField] float m_textSpeedMultiplier = 1;


}
}
