using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace PowerTools.Quest
{


public class GuiToolbarComponent : MonoBehaviour 
{
	[System.Serializable]
	class AudioSetting
	{
		public float m_volume = 0;
		public Sprite m_buttonsprite = null;
	}

	int m_dialogSetting = 0;
	int m_audioSetting = 0;

	[SerializeField] AudioSetting[] m_audioSettings = null;

	[SerializeField] Sprite[] m_spriteDialog = null;

	//[SerializeField] Image m_btnPower = null;
	[SerializeField] Image m_btnDialog = null;
	[SerializeField] Image m_btnAudio = null;
	[SerializeField] GuiComponent m_btnQuit = null;

	public void SaveAndQuit()
	{
		#if EXHIBITION 
		PowerQuest.Get.Restart();
		#else

		if ( PowerQuest.Get.GetCurrentRoom().ScriptName != "Title" )
			PowerQuest.Get.Save(1,"SaveOnQuit");
		Application.Quit();

		#endif
	}

	static readonly string[] TEXT_DISPLAY_STRINGS = {"Subtitles: On", "Subtitles: Off", "Subtitles: Text Only"};

	public void ToggleDialogSetting()
	{
		//#if !DEMO 

		m_dialogSetting = (int)Mathf.Repeat((m_dialogSetting+1), m_spriteDialog.Length);
		PowerQuest.Get.Settings.DialogDisplay = (QuestSettings.eDialogDisplay)m_dialogSetting;
		m_btnDialog.sprite = m_spriteDialog[m_dialogSetting];
		m_btnDialog.GetComponent<GuiComponent>().GetData().Description = TEXT_DISPLAY_STRINGS[m_dialogSetting];

		//#endif
	}

	public void ToggleAudioSetting()
	{
		//#if !DEMO 

		m_audioSetting =(int) Mathf.Repeat(m_audioSetting+1, m_audioSettings.Length);
		m_btnAudio.sprite = m_audioSettings[m_audioSetting].m_buttonsprite;
		PowerQuest.Get.Settings.Volume = m_audioSettings[m_audioSetting].m_volume;

		//#endif
	}

	public void SetQuitText(string text)
	{
		m_btnQuit.GetData().Description = text;
	}

	void Start()
	{
		// Load the settings
		float volume = PowerQuest.Get.Settings.Volume;
		for (int i = 0; i < m_audioSettings.Length; i++)
		{
			if ( m_audioSettings[i].m_volume <= volume )
			{
				m_audioSetting = i;
			}
		}
		m_btnAudio.sprite = m_audioSettings[m_audioSetting].m_buttonsprite;
		m_dialogSetting = (int)PowerQuest.Get.Settings.DialogDisplay;
		m_btnDialog.sprite = m_spriteDialog[m_dialogSetting];
		m_btnDialog.GetComponent<GuiComponent>().GetData().Description = TEXT_DISPLAY_STRINGS[m_dialogSetting];
	}


}
}