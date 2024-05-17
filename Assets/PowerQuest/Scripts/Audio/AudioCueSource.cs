using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PowerTools.Quest
{

public class AudioCueSource : MonoBehaviour 
{
	static readonly int LAYER_UI = LayerMask.NameToLayer("UI");

	public AudioCue m_playOnSpawn = null;	
	public bool m_stopOnDestroy = false;
	public List<AudioCue> m_cues = new List<AudioCue>();	


	List<AudioHandle> m_currSounds = new List<AudioHandle>();
	
	
	public void AnimSound( Object cueName )
	{
		GameObject obj = cueName as GameObject;
		if ( obj == null )
			return;
		
		AudioCue cue = 	obj.GetComponent<AudioCue>();
		if ( gameObject.layer == LAYER_UI )
			SystemAudio.Play(cue, ref m_currSounds);				
		else 
			SystemAudio.Play(cue, ref m_currSounds, transform);
	}	
	
	public void AnimSound( string cueName )
	{
		if ( gameObject.layer == LAYER_UI )			
			SystemAudio.Play(SystemAudio.GetCue(cueName), ref m_currSounds);				
		else 
			SystemAudio.Play(SystemAudio.GetCue(cueName), ref m_currSounds, transform);		
	}

	public void AnimSoundStop()
	{
		StopSounds();
	}

	void Start() { OnSpawn(); }

	// Use this for initialization
	void OnSpawn () 
	{
		if ( m_playOnSpawn )
		{			
			if ( gameObject.layer == LAYER_UI )
				SystemAudio.Play(m_playOnSpawn, ref m_currSounds);				
			else 
				SystemAudio.Play(m_playOnSpawn, ref m_currSounds, transform);		
		}
		StartCoroutine(CoroutineClearFinishedSounds());
	}	

	void OnDestroy()
	{
		if ( m_stopOnDestroy )
			StopSounds();
	}
	void OnDisable()
	{	
		if ( m_stopOnDestroy )
			StopSounds();
	}

	void StopSounds()
	{

		for ( int i = 0; i < m_currSounds.Count; ++i )
		{
			m_currSounds[i].Stop(0.2f);
		}
		m_currSounds.Clear();
	}

	IEnumerator CoroutineClearFinishedSounds()
	{
		while( true )
		{
			yield return new WaitForSeconds(0.2f);
			for ( int i = m_currSounds.Count-1; i >= 0; --i )
			{
				AudioSource source = m_currSounds[i];
				if ( source == null || source.isPlaying == false )
					m_currSounds.RemoveAt(i);
			}
		}
	}

	/*
	// Update is called once per frame
	void Update () 
	{
	
	}
	*/
}

}