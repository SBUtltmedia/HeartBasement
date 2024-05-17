using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;
using eDialogDisplay = PowerTools.Quest.QuestSettings.eDialogDisplay;

public class GuiOptions : GuiScript<GuiOptions>
{
	
	int m_resolution = 0;
	FullScreenMode m_fullScreenMode = FullScreenMode.Windowed;
	bool m_resDirty = false;
	
	List<int> m_uniqueResolutions = new List<int>();
	
	
	
	
	IEnumerator OnClickBack( IGuiControl control )
	{
		G.Options.Hide();
		E.SaveSettings();
		yield return E.Break;
	}

	void OnShow()
	{
		// Read data from settings
		
		Slider("Volume").Ratio = Settings.Volume;
		
		// Build list of unique resolutions
		UpdateResolutionList();
		
		// Hide apply button
		Control("Apply").Hide();
		
		if ( Settings.GetLanguages().Length < 2 )
		{
			Control("Language").Hide();
			Container("GridContainer").Grid.RemoveItem( Control("Language").Instance.transform );
		}
		
		UpdateText();
	}

	public void UpdateResolutionList()
	{
		float screenWidth = Screen.width;
		float screenHeight = Screen.height;
		m_resDirty = false;
		
		m_uniqueResolutions.Clear();
		Resolution[] resolutions = Screen.resolutions;
		
		m_resolution = 0;
		Resolution last = new Resolution();
		for ( int i = 0; i < resolutions.Length; ++i )
		{
			Resolution res = resolutions[i];
			if ( last.width != res.width || last.height != res.height )
			{
				last = res;
				m_uniqueResolutions.Add( i );
		
				// Also check for the current resolution
				if ( res.width == screenWidth && res.height == screenHeight )
					m_resolution = m_uniqueResolutions.Count-1;
			}
		}
		
		//Debug.Log($"Res Id: {m_resolution}, total: {m_uniqueResolutions.Count}, ratio: {(float)m_resolution/(float)(m_uniqueResolutions.Count-1)}");
		//Label("Debug").Text = $"Res Id: {m_resolution}, total: {m_uniqueResolutions.Count}, ratio: {(float)m_resolution/(float)(m_uniqueResolutions.Count-1)}";

		Slider("Resolution").Ratio = (float)m_resolution/(float)(m_uniqueResolutions.Count-1);
		
		m_fullScreenMode = Screen.fullScreenMode;
	}
	
	public void UpdateText()
	{
		// Volume slider text
		Slider("Volume").Text = string.Format( SystemText.Localize("Volume: {0}"), Mathf.RoundToInt(Settings.Volume * 100.0f));
		
		// Some games might want sliders for types of sound...
		/*
		Slider("VolumeSound").Text = string.Format( SystemText.Localize("Sound: {0}"), Mathf.RoundToInt(Settings.VolumeSFX * 100.0f));
		Slider("VolumeMusic").Text = string.Format( SystemText.Localize("Music: {0}"), Mathf.RoundToInt(Settings.VolumeMusic * 100.0f));
		Slider("VolumeSpeech").Text = string.Format( SystemText.Localize("Speech: {0}"), Mathf.RoundToInt(Settings.VolumeDialog * 100.0f));
		*/
		
		// Language text
		Button("Language").Text = string.Format( SystemText.Localize("Language: {0}"), Settings.LanguageData.m_description );
		
		// Cursor lock mode
		if ( Settings.LockCursor == CursorLockMode.Confined )
			Button("LockCursor").Text = SystemText.Localize("Lock Cursor: On");
		else
			Button("LockCursor").Text = SystemText.Localize("Lock Cursor: Off");
		
		// Dialog display toggle button
		switch( Settings.DialogDisplay )
		{
			case eDialogDisplay.TextAndSpeech: Button("Subtitles").Text = SystemText.Localize("Speech + Subtitles"); break;
			case eDialogDisplay.SpeechOnly:	Button("Subtitles").Text = SystemText.Localize("Speech Only"); break;
			case eDialogDisplay.TextOnly:	  Button("Subtitles").Text = SystemText.Localize("Subtitles Only"); break;
		}
		
		// Screen res slider
		if ( m_uniqueResolutions.IsIndexValid(m_resolution) && Screen.resolutions.IsIndexValid(m_uniqueResolutions[m_resolution]) )
		{
			Resolution res = Screen.resolutions[m_uniqueResolutions[m_resolution]];
			Slider("Resolution").Text = $"{res.width}x{res.height}";
		}
		
		// Fullscreen toggle
		switch( m_fullScreenMode )
		{
			case FullScreenMode.FullScreenWindow:	Button("Fullscreen").Text = "Fullscreen"; break;
			case FullScreenMode.ExclusiveFullScreen: Button("Fullscreen").Text = "Exclusive Fullscreen"; break;
			case FullScreenMode.Windowed:			Button("Fullscreen").Text = "Windowed"; break;
		}
		
		// Show "apply" button if resolution slider has been moved
		if ( m_resDirty )
			Button("Apply").Show();
		Button("Apply").Clickable = m_resDirty;
	}

	IEnumerator OnDragVolume( IGuiControl control )
	{
		Settings.Volume = Slider("Volume").Ratio;
		UpdateText();
		yield return E.Break;
	}

	IEnumerator OnClickVolume( IGuiControl control )
	{
		// Play a sound when button's released
		Audio.Play("Bucket");
		yield return E.Break;
	}
	
	IEnumerator OnDragResolution( IGuiControl control )
	{	
		m_resDirty = true;
		m_resolution = Mathf.RoundToInt( Slider("Resolution").Ratio * (float)(m_uniqueResolutions.Count-1) );
				
		UpdateText();
		
		yield return E.Break;
	}

	IEnumerator OnClickFullscreen( IGuiControl control )
	{
		switch (m_fullScreenMode)
		{
			case FullScreenMode.Windowed:            m_fullScreenMode = FullScreenMode.FullScreenWindow; break;
			case FullScreenMode.FullScreenWindow:    m_fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
			case FullScreenMode.ExclusiveFullScreen: m_fullScreenMode = FullScreenMode.Windowed; break;
			//case FullScreenMode.MaximizedWindow:     m_fullScreenMode = FullScreenMode.Windowed; break;
		}			
		m_resDirty = true;
		UpdateText();
		yield return E.Break;
	}

	
	IEnumerator OnClickApply( IGuiControl control )
	{
		// Set resolution				
		if ( m_uniqueResolutions.IsIndexValid(m_resolution) && Screen.resolutions.IsIndexValid(m_uniqueResolutions[m_resolution]) )
		{
			Resolution res = Screen.resolutions[m_uniqueResolutions[m_resolution]];
			Screen.SetResolution( res.width, res.height, m_fullScreenMode, 0 );
		}
		
		m_resDirty = false;
		UpdateText();

		// Update resolution list in case it changed (Have to wait a bit first though)
		E.DelayedInvoke(0.1f,UpdateResolutionList);					
		yield return E.Break;
	}

	void Update()
	{
		if ( G.Options.HasFocus && Input.GetKeyUp(KeyCode.Escape) )
			G.Options.Hide();
	}

	IEnumerator OnClickSubtitles( IGuiControl control )
	{		

		switch (Settings.DialogDisplay)
		{
			case eDialogDisplay.TextAndSpeech: Settings.DialogDisplay = eDialogDisplay.TextOnly; break;
			case eDialogDisplay.TextOnly:      Settings.DialogDisplay = eDialogDisplay.SpeechOnly; break;
			case eDialogDisplay.SpeechOnly:    Settings.DialogDisplay = eDialogDisplay.TextAndSpeech; break;
		}
		
		UpdateText();
		yield return E.Break;
	}


	IEnumerator OnClickLanguage( IGuiControl control )
	{
		Settings.LanguageId = (int)Mathf.Repeat(Settings.LanguageId + 1,Settings.GetLanguages().Length);
		UpdateText();		
		yield return E.Break;
	}

	IEnumerator OnClickLockCursor( IGuiControl control )
	{
		Settings.LockCursor = (Settings.LockCursor == CursorLockMode.Confined) ? CursorLockMode.None : CursorLockMode.Confined;
		UpdateText();
		yield return E.Break;
	}
}
