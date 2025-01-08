using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace PowerTools.Quest
{


[ExecuteInEditMode]
/// The PowerQuest Audio System. Provides convenient, 2d specific audio functionality, using audio cues.
/** 
 * Audio Functions are accessable from QuestScripts using `Audio.`, or from other scripts from using the static class `SystemAudio.`
 * 
 * __Examples:__
 *	~~~
 *		Audio.Play("Explosion");
 *		Audio.PlayMusic("WikkidBeats");
 *		Audio.Play("BeachAmbience").FadeIn(3.0f); // start a looping sound and fade it in
 *		....
 *		Audio.PlayMusic("ChillBeats", 2.0f); // crossfade to new track
 *		Audio.Stop("BeachAmbience",0.5f); // stop the loopsing sound, fading over 0.5 sec.
 *	~~~
 * 
 * __Features:__
 * - This is an audio cue based system. You create an AudioCue in your project, set up which clips it plays and data like volume, pitch, etc. Then you play that "Cue".  
 * - See the [Audio manual](@ref Audio) for info on Sound Cues
 * - Cues can be added to SystemAudio so that you can play them by name. Then it's as simple as calling `Audio.Play("gunshot");`
 * - Cues have a lot of controls for randomisation so common sounds don't get repetive: set min/max pitch, a list of clips to randomly choose from, and even string multiple cues together with random delays
 * - Type based volume control: Flag cues with a type (eg SoundEffect, Music, Dialog), and then set those volumes seperately
 * - Simple functions for playing music and an ambient loop and crossfading between them
 * - Stop/Pause/Modify volume/pitch/pan, etc of currently playing sounds by their cue name, or the AudioHandle returned from the Play Function. eg: `Audio.GetHandle("gunshot").pitch = 0.5f`, `Audio.Stop("gunshot");`
 * - Ducking of music when playing dialog
 * - Simple 2d falloff and stereo control: Some basic controls set how sounds get quieter, and move left/right based on their position relative to the camera
 * - Automatic pooling system for efficiency
 * - Save/Restore audio state
 */
public partial class SystemAudio : SingletonAuto<SystemAudio>
{	
	#region Definitions

	[System.Serializable]
	class AudioTypeVolume
	{
		public AudioCue.eAudioType m_type = AudioCue.eAudioType.Sound;
		public float m_volume = 1.0f;
		// Default mixer group for the type of sound. Can be overridden per-cue.
		public AudioMixerGroup m_mixerGroup = null;
	};

	class ClipInfo
	{
		//ClipInfo used to maintain default audio source info
		public AudioCue cue { get; set; }
		public AudioHandle handle { get; set; }
		public int type { get; set; }
		public float defaultVolume { get; set; }
		public float defaultPitch { get; set; }
		public float defaultPan { get; set; }
		public float targetVolume { get; set; } // targetVolume used for fading in/out (especially when handling save/load)		
		public float fadeDelta {get;set;} // volume change per second
		public float startFromTime {get;set;}
		public float stopAfterTime {get;set;}
		public bool stopAfterFade { get; set; } // Whether to stop after fadeout
		public Transform emmitter { get; set; }
		public bool paused { get; set; }
	}

	static List<AudioHandle> s_defaultAudioHandleList = new List<AudioHandle>();

	static readonly string STRING_NAME_PREFIX = "Audio: ";

	static int LAYER_NOPAUSE = -1;

	// The initial size of the audio pool, if this limit is reached, more will be added at runtime, no big deal. Just stops potential framerate drop first time a bunch of sounds are played
	static readonly int AUDIO_SOURCE_POOL_SIZE = 16;

	#endregion 
	#region Editable vars

	[Header("Default volume levels(and/or mixer groups), by type (Music, SFX, Dialog)")]
	[SerializeField, ReorderableArray, NonReorderable] List<AudioTypeVolume> m_volumeByType = new List<AudioTypeVolume>();
	[SerializeField] float m_musicDuckingMaxVolume = 0.5f;

	[Header("Default falloff values (radios of screen width)")]
	[SerializeField] float m_falloffMinVol = 0.2f;
	[SerializeField] float m_falloffPanMax= 0.8f;

	[SerializeField] float m_falloffStart = 1.0f;
	[SerializeField] float m_falloffEnd = 2.0f;

	[SerializeField] float m_falloffPanStart = 0.5f;
	[SerializeField] float m_falloffPanEnd = 2.0f;

	[Header("Misc settings")]
	[Tooltip("Default minimum time between the same sound playing again")]
	[SerializeField] float m_noDuplicateTime = 0.05f;

	[Tooltip("If the narrator has its own mixer group, set it here. Otherwise it will use the default one for Dialog")]
	[SerializeField] AudioMixerGroup m_narratorMixerGroup = null;

	[Header("List of audio cues that can be played by name")]
	[Tooltip("If set, any cues in the Audio folder will be automatically added (you don't have to click the button in the cue)")]
	[SerializeField] bool m_autoAddCues = true;
	[Tooltip("Audio cues that can be played by name")]
	[SerializeField] List<AudioCue> m_audioCues = new List<AudioCue>();

	#endregion
	#region private vars

	List<ClipInfo> m_activeAudio = new List<ClipInfo>();
	AudioHandle m_activeMusic;
	AudioHandle m_activeAmbientLoop = null;

	Transform m_audioListener = null;

	bool m_hasFocus = true;

	List<AudioSource> m_audioSources = new List<AudioSource>();

	Camera m_cameraGame = null;

	// For save data
	string m_musicCueName = string.Empty;
	float m_musicVolOverride = 1;
	string m_ambientCueName = null;	
	bool m_restartMusicIfAlreadyPlaying = false;
	
	float m_falloffDistanceMultiplier = 1;

	//float m_musicLoopStart = 0;
	//float m_musicLoopEnd = 0;

	#endregion
	#region Public Static Functions
	
	/// The AudioHandle for the currently playing Music track \sa PlayMusic \sa StopMusic
	public static AudioHandle MusicHandle => SystemAudio.Get.GetActiveMusicHandle();

	/// A multiplier on the falloff distance for volume/panning. Useful when you want to bring it in close, or expand it outwards temporarily
	public static float FalloffDistanceMultiplier { get{ return m_instance.m_falloffDistanceMultiplier; } set{ m_instance.m_falloffDistanceMultiplier = value; } }


	/// Retrieves the volume for audio of a particular type (eg: Sound effects, Music, Dialog) 
	public static float GetVolume( AudioCue.eAudioType type )
	{
		SystemAudio self = SystemAudio.Get;
		for ( int i = 0; i < self.m_volumeByType.Count; ++i )
		{
			if ( self.m_volumeByType[i].m_type == type )
			{
				return Mathf.Clamp01(self.m_volumeByType[i].m_volume);
			}
		}
		return 1.0f;
	}

	/// Retrieves a sound cue by name
	public static AudioCue GetCue(string cueName)
	{
		return m_instance.m_audioCues.Find(item=>string.Equals(cueName, item.name, System.StringComparison.OrdinalIgnoreCase) );
	}

	/// Returns true if the cue is currently playing, otherwise false
	public static bool IsPlaying(string cueName) { return AudioHandle.IsPlaying(GetHandle(cueName)); }

	/// Play a cue by name. This is the main way to play sounds. If emmitter is set, the sound will falloff/pan as it goes off camera. 
	/**
	 Eg:
			Audio.Play("DoorKnock");
			Audio.Play("Gunshot", C.Gunman.Instance);
			Audio.Play("SeasideAmbience").FadeIn(2.0f);
	 */
	public static AudioHandle Play( string cueName, Transform emmitter = null )
	{
		SystemAudio self = SystemAudio.Get;
		AudioCue cue = self.m_audioCues.Find(item=>item != null && string.Equals(cueName, item.name, System.StringComparison.OrdinalIgnoreCase));
		if ( cue == null && string.IsNullOrEmpty(cueName) == false && (Application.isEditor || PowerQuest.Get.IsDebugBuild))
		{
			Debug.LogWarning("Sound cue not found: "+cueName);
		}
		return Play( cue , emmitter );
	}

	/// Play the specified cue with extended options. If emmitter is set, the sound will falloff/pan as it goes off camera. Can also set volume and pitch overrides, and override the start time of the cue
	static public AudioHandle Play(AudioCue cue, Transform emitter = null, float volumeMult = 1, float pitchMult = 1, float fromTime = 0, float withStartDelay = 0 )
	{
		return Play( cue, ref s_defaultAudioHandleList, emitter, volumeMult, pitchMult, fromTime, withStartDelay);
	}

	/// Advanced Play function with extended options, and returning a list of all handles started (for when multiple are started). If emmitter is set, the sound will falloff/pan as it goes off camera. Can also set volume and pitch overrides, and override the start time of the cue
	static public AudioHandle Play(AudioCue cue, ref List<AudioHandle> handles, Transform emmitter = null, float volumeMult = 1, float pitchMult = 1, float fromTime = 0, float withStartDelay = 0 )
	{
		if ( cue == null )
			return new AudioHandle(null);

		SystemAudio self = SystemAudio.Get;

		AudioCue.Clip cueClip = cue.GetClip();	

		if ( cueClip == null )
			return new AudioHandle(null);

		if ( Application.isPlaying && PowerQuest.Get.GetSkippingCutscene() && cue.m_loop == false && ((int)AudioCue.eAudioType.Music & cue.m_type) == 0 ) // Quest hack- don't play sounds while skipping cutscenes
			return new AudioHandle(null);

		if ( Random.value > cue.m_chance )
			return new AudioHandle(null);

		AudioClip clip = cueClip.m_sound;

		if ( clip == null )
			return new AudioHandle(null);

		// Check if sound is already being played within X time. The if the cue can override this time, or default (-1) to the parent
		for ( int i = 0; i < self.m_activeAudio.Count; ++i )
		{
			ClipInfo info = self.m_activeAudio[i];
			if ( info.handle != null && info.handle.clip == clip && (info.cue == null || info.cue == cue) //&& info.handle.isPlaying
				&& Application.isPlaying
				&& (withStartDelay <= 0 && fromTime <= 0 && cue.m_loopSection.m_endTime <= 0 && cue.m_startDelay <= 0 )
				&& ( info.handle.time < ( cue.m_noDuplicateTime >= 0 ? cue.m_noDuplicateTime : self.m_noDuplicateTime) ) )
			{
				//Debug.Log("AUD: duplicate"+info.handle.cueName);
				return new AudioHandle(null);
			}
		}

		float volume = cueClip.m_volume.GetRandom() * volumeMult;

		volume *= cue.m_volume.GetRandom();

		// Apply type volumes & default mixer groups
		AudioMixerGroup defaultMixerGroup = null;
		for ( int i = 0; i < self.m_volumeByType.Count; ++i )
		{
			if ( ((int)self.m_volumeByType[i].m_type & cue.m_type) != 0 )
			{
				volume *= self.m_volumeByType[i].m_volume;
				defaultMixerGroup = self.m_volumeByType[i].m_mixerGroup;
			}			
		}

		float pitch =  cue.m_pitch.GetRandom() *  cueClip.m_pitch.GetRandom() * pitchMult;
		float pan = cue.m_pan.GetRandom();

		// Create the source
		AudioSource source = self.SpawnAudioSource( 
			( Debug.isDebugBuild ) ? STRING_NAME_PREFIX + cue.name : STRING_NAME_PREFIX, // don't bother with strings if not debugging
			self.transform.position);

		{
			AudioReverbFilter sourceFilter = source.gameObject.GetComponent<AudioReverbFilter>();
			if ( sourceFilter == null ) 
			{
				self.AddSourceFilters(source.gameObject); 
				sourceFilter = source.gameObject.GetComponent<AudioReverbFilter>();
			}
			sourceFilter.enabled = cue.m_reverbPreset != AudioReverbPreset.Off;
			if ( sourceFilter.enabled )
			{
				sourceFilter.reverbPreset = cue.m_reverbPreset;
				if ( cue.m_reverbLevel > 0 )
				{
					//sourceFilter.reverbLevel = Mathf.Lerp(-10000,2000,cue.m_reverbLevel);
					//sourceFilter.reflectionsLevel = Mathf.Lerp(-10000,1000,cue.m_reverbLevel);
					sourceFilter.reverbPreset = AudioReverbPreset.User; // have to set to user to be able to tweak settings
					sourceFilter.room = Mathf.Lerp(-10000,0,Mathf.InverseLerp(-144,0,ToDecibel(cue.m_reverbLevel)));
					sourceFilter.roomLF = Mathf.Lerp(-10000,0,Mathf.InverseLerp(-144,0,ToDecibel(cue.m_reverbLevel)));
					sourceFilter.roomHF = Mathf.Lerp(-10000,0,Mathf.InverseLerp(-144,0,ToDecibel(cue.m_reverbLevel)));
					//sourceFilter.dryLevel= Mathf.Lerp(-10000,0,1.0f-cue.m_reverbLevel);
				}
				//if ( cue.m_reverbLevel >= 0 )
					//sourceFilter.dryLevel = Mathf.Lerp(-10000,2000,cue.m_reverbLevel) 1.0f-cue.m_reverbLevel;
			}

		}
		{
			AudioEchoFilter sourceFilter = source.gameObject.GetComponent<AudioEchoFilter>();
			sourceFilter.enabled = cue.m_echoFilter != null;
			if ( sourceFilter.enabled )
			{
				sourceFilter.delay = cue.m_echoFilter.delay;
				sourceFilter.decayRatio = cue.m_echoFilter.decayRatio;
				sourceFilter.dryMix = cue.m_echoFilter.dryMix;
			}
		}
		{
			AudioDistortionFilter sourceFilter = source.gameObject.GetComponent<AudioDistortionFilter>();
			sourceFilter.enabled = cue.m_distortionLevel > 0;
			if ( sourceFilter.enabled )
			{
				sourceFilter.distortionLevel = cue.m_distortionLevel;
			}
		}
		{
			AudioHighPassFilter sourceFilter = source.gameObject.GetComponent<AudioHighPassFilter>();
			sourceFilter.enabled = cue.m_highPassFilter != null || cue.m_highPass > 10;
			if ( sourceFilter.enabled )
			{
				if ( cue.m_highPass > 10 )
				{
					sourceFilter.cutoffFrequency = cue.m_highPass;
					sourceFilter.highpassResonanceQ = cue.m_highPassQ;
				}
				else 
				{
					sourceFilter.cutoffFrequency = cue.m_highPassFilter.cutoffFrequency;
					sourceFilter.highpassResonanceQ = cue.m_highPassFilter.highpassResonanceQ;
				}
			}			
		}
		{
			AudioLowPassFilter sourceFilter = source.gameObject.GetComponent<AudioLowPassFilter>();
			sourceFilter.enabled = cue.m_lowPassFilter != null || cue.m_lowPass > 10;
			if ( sourceFilter.enabled )
			{
				if ( cue.m_lowPass > 10 )
				{
					sourceFilter.cutoffFrequency = cue.m_lowPass;
					sourceFilter.lowpassResonanceQ = cue.m_lowPassQ;
				}
				else 
				{
					sourceFilter.cutoffFrequency = cue.m_lowPassFilter.cutoffFrequency;
					sourceFilter.lowpassResonanceQ = cue.m_lowPassFilter.lowpassResonanceQ;
				}
			}
		}

		{
			AudioChorusFilter sourceFilter = source.gameObject.GetComponent<AudioChorusFilter>();
			sourceFilter.enabled = cue.m_chorusFilter != null;
			if ( sourceFilter.enabled )
			{
				sourceFilter.delay = cue.m_chorusFilter.delay;
				sourceFilter.depth = cue.m_chorusFilter.depth;
				sourceFilter.dryMix = cue.m_chorusFilter.dryMix;
				sourceFilter.wetMix1 = cue.m_chorusFilter.wetMix1;
				sourceFilter.wetMix2 = cue.m_chorusFilter.wetMix2;
				sourceFilter.wetMix3 = cue.m_chorusFilter.wetMix3;
				sourceFilter.rate = cue.m_chorusFilter.rate;				
			}
		}
		
		if ( cue.m_mixerGroup != null )
			source.outputAudioMixerGroup = cue.m_mixerGroup;
		else 
			source.outputAudioMixerGroup = defaultMixerGroup;
		

		/* Use audiosystem as source of all sounds */
		source.transform.parent = self.transform;

		source.transform.localPosition = Vector3.zero;

		self.SetSource(ref source, clip, volume, pitch, pan, 128, emmitter );		
		source.loop = cue.m_loop;
		
		if ( fromTime <= 0.0f && cueClip.m_startTime > 0 )
		{
			fromTime = cueClip.m_startTime;
		}		
		if ( fromTime > 0.0f ) // There's a known unity error where starting from time errors. So don't set source time if it's close to the end (NB: This still may happen, need to watch for it)
			source.time = Mathf.Clamp(fromTime, 0, Mathf.Max(source.clip.length-1.0f,source.clip.length*0.9f)); // There's a known unity error where starting from time errors. So don't set source time if it's close to the end.  (NB: This still may happen, need to watch for it)
		else
			source.time = 0.0f;

		// Play (with start delay if one exists)
		if ( withStartDelay <= 0 )
			withStartDelay = cue.m_startDelay.GetRandom();

		if ( withStartDelay > 0.0f )
		{
			source.PlayDelayed(withStartDelay);
		}
		else 
		{			
			source.Play();	
			if ( source.isPlaying == false && source.time > 0 )
			{
				// Must have errored when trying to play from time (ther'es a known unity bug) so try playing again without start offset
				if ( Debug.isDebugBuild ) Debug.LogWarning("Failed to play sound from specific time. Retrying from beginning");
				source.time = 0;
				source.Play();
			}
		}

		AudioHandle handle = new AudioHandle(source,cue.name);

		if ( handles != s_defaultAudioHandleList )
		{
			if ( handles == null )
				handles = new List<AudioHandle>();
			handles.Add(handle);
		}

		// Stop clip before it's end
		if ( cueClip.m_endTime > 0 )
		{			
			float stopTime = cueClip.m_endTime;
			if ( cueClip.m_startTime > 0 )
				stopTime -= cueClip.m_startTime;
			stopTime /= pitch;
			if ( withStartDelay > 0 )
				stopTime += withStartDelay;			
			handle.source.SetScheduledEndTime(AudioSettings.dspTime + (double)stopTime);
		}

		// Shake camera
		/*if ( self.m_camera != null && cueClip.m_shakeTime > 0 )
		{
			self.m_camera.SendMessage("Shake", cueClip.m_shakeTime, cueClip.m_shakeType );
		}*/

		//Set the source as active
		self.AddActiveAudio(new ClipInfo{
			handle = handle, 
			cue = cue,
			type = cue.m_type,
			defaultVolume = volume, 
			targetVolume = volume,
			defaultPitch = pitch,
			defaultPan = pan,
			startFromTime=fromTime,
			emmitter = (emmitter == null ? self.transform : emmitter) }
			
			);

		// Fade in the sound if fadeInTime set
		if ( cue.m_fadeInTime > 0 )
			handle.FadeIn(cue.m_fadeInTime);

		// NB: Probably should apply "active audio" updates to the clip immediately

		if ( cue.m_alsoPlay != null )
			Play( cue.m_alsoPlay, ref handles, emmitter );

		
		return handle;
	}

	/// Plays a specific audio clip rather than an audio cue
	static public AudioHandle Play( AudioClip clip, int type = (int)AudioCue.eAudioType.Sound, Transform emmitter = null, float volume = 1, float pitch = 1, bool loop = false, AudioMixerGroup mixerGroup = null )
	{
		if ( clip == null)
			return null;

		SystemAudio self = SystemAudio.Get;

		// Check if sound is already being played within X time
		for ( int i = 0; i < self.m_activeAudio.Count; ++i )
		{
			ClipInfo info = self.m_activeAudio[i];
			if ( info.handle != null && info.handle.clip == clip && info.handle.time < self.m_noDuplicateTime )
			{
				//Debug.Log("AUD: duplicate"+info.source.name);
				return null;
			}
		}

		// Apply type volumes
		AudioMixerGroup defaultMixerGroup = null;
		for ( int i = 0; i < self.m_volumeByType.Count; ++i )
		{
			if ( ((int)self.m_volumeByType[i].m_type & type) != 0 )
			{
				volume *= self.m_volumeByType[i].m_volume;
				defaultMixerGroup = self.m_volumeByType[i].m_mixerGroup;
			}
		}

		// Create the source
		AudioSource source = self.SpawnAudioSource( 
			( Debug.isDebugBuild ) ? STRING_NAME_PREFIX + clip.name : STRING_NAME_PREFIX, // don't bother with strings if not debugging
			self.transform.position);
		// Disable filters
		{
			AudioReverbFilter sourceFilter = source.gameObject.GetComponent<AudioReverbFilter>();
			if ( sourceFilter != null )
				sourceFilter.enabled = false;

		}
		{
			AudioEchoFilter sourceFilter = source.gameObject.GetComponent<AudioEchoFilter>();
			if ( sourceFilter != null )
				sourceFilter.enabled = false;
		}
		{
			AudioDistortionFilter sourceFilter = source.gameObject.GetComponent<AudioDistortionFilter>();
			if ( sourceFilter != null )
				sourceFilter.enabled = false;
		}
		{
			AudioHighPassFilter sourceFilter = source.gameObject.GetComponent<AudioHighPassFilter>();
			if ( sourceFilter != null )
				sourceFilter.enabled = false;			
		}
		{
			AudioLowPassFilter sourceFilter = source.gameObject.GetComponent<AudioLowPassFilter>();
			if ( sourceFilter != null )
				sourceFilter.enabled = false;
		}
		{
			AudioChorusFilter sourceFilter = source.gameObject.GetComponent<AudioChorusFilter>();
			if ( sourceFilter != null )
				sourceFilter.enabled = false;
		}
		
		if ( mixerGroup != null )
			source.outputAudioMixerGroup = mixerGroup;
		else 		
			source.outputAudioMixerGroup = defaultMixerGroup;

		/* Use audiosystem as source of all sounds */
		source.transform.parent = self.transform;

		source.transform.localPosition = Vector3.zero;

		self.SetSource(ref source, clip, volume, pitch, 0, 0, emmitter ); // NB: Priority is 0 (highest) for clips, since they're usually dialog	
		source.loop = loop;
		source.time = 0;
		source.Play();

		// Shake camera
		/*if ( self.m_camera != null && cueClip.m_shakeTime > 0 )
		{
			self.m_camera.SendMessage("Shake", cueClip.m_shakeTime, cueClip.m_shakeType );
		}*/

		// if no emmitter specified, emit from audio system
		if ( emmitter == null )
		{
			emmitter = self.transform;
		}

		AudioHandle handle = new AudioHandle(source);

		//Set the source as active
		self.AddActiveAudio(new ClipInfo{
			handle = handle, 
			cue = null,
			type = type,
			defaultVolume = volume, 
			targetVolume = volume,
			defaultPitch = pitch,
			defaultPan = 0,
			emmitter = emmitter == null ? self.transform : emmitter } );

		return handle;
	}	

	/// Pauses the specified sound by it's handle
	public static void Pause(AudioHandle handle)
	{
		if ( handle == null )
			return;
		ClipInfo clipInfo = m_instance.m_activeAudio.Find( clip=>clip.handle == handle );
		if ( clipInfo != null && clipInfo.paused == false ) 
		{
			clipInfo.paused = true;
			handle.source.Pause();
		}
	}

	/// Resumes the specified sound by it's handle
	public static void UnPause(AudioHandle handle)
	{
		if ( handle == null )
			return;
		ClipInfo clipInfo = m_instance.m_activeAudio.Find( clip=>clip.handle == handle );
		if ( clipInfo != null && clipInfo.paused == true ) 
		{
			clipInfo.paused = false;
			//source.UnPause();
			handle.source.Play();	
		}
	}

	/// Pauses the specified sound by it's cue name
	public static void Pause(string cueName)
	{
		foreach ( ClipInfo clip in m_instance.m_activeAudio )
		{
			if ( clip.cue != null && string.Equals( clip.cue.name, cueName, System.StringComparison.OrdinalIgnoreCase ) )
			{
				Pause(clip.handle);
			}
		}
	}

	/// Resumes the specified sound by it's cue  name
	public static void UnPause(string cueName)
	{
		foreach ( ClipInfo clip in m_instance.m_activeAudio )
		{
			if ( clip.cue != null && string.Equals( clip.cue.name, cueName, System.StringComparison.OrdinalIgnoreCase ) )
			{
				UnPause(clip.handle);
			}
		}
	}

	/// Gets a playing AudioHandle by its cue name
	public static AudioHandle GetHandle(string cueName)
	{	
		ClipInfo info = SystemAudio.Get.m_activeAudio.Find(item=>item.cue != null && string.Equals( item.cue.name, cueName, System.StringComparison.OrdinalIgnoreCase ) );
		return  info == null ? null :  info.handle;
	}

	/// Gets any currently playing AudioHandles by their cue name
	public static AudioHandle[] GetHandles(string cueName)
	{
		List<ClipInfo> info = m_instance.m_activeAudio.FindAll(item=>item.cue != null && string.Equals( item.cue.name, cueName, System.StringComparison.OrdinalIgnoreCase ) );
		if ( info != null && info.Count > 0 )
		{
			AudioHandle[] handles = new AudioHandle[info.Count];
			for ( int i = 0; i < info.Count; ++i )
				handles[i] = info[i].handle;
			return handles;
		}
		return null;
	}

	/// Stops the specified sound by it's cue name
	public static void Stop(string cueName, float overTime = 0)
	{
		// Copy list because stop may remove some
		//ClipInfo[] allAudio = new ClipInfo[m_instance.m_activeAudio.Count];
		//m_instance.m_activeAudio.CopyTo(allAudio);
		foreach ( ClipInfo clip in m_instance.m_activeAudio )
		{
			if ( clip != null && clip.cue != null && string.Equals( clip.cue.name, cueName, System.StringComparison.OrdinalIgnoreCase ) )
			{
				Stop(clip.handle,overTime);
			}
		}
	}

	/// Stops the specified sound by it's handle
	public static void Stop(AudioHandle handle, float overTime = 0, float afterDelay = 0)
	{		
		if ( handle != null )
			handle.Stop(overTime, afterDelay);			
	}

	// Called from handle to cleanup sounds when they're stopped. Returns true if stopped immediately so handle can remove its source (since they're pooled) @private
	public bool StopHandleInternal( AudioHandle handle, float overTime = 0, float afterDelay = 0)
	{				
		if ( handle == null || handle.source == null )
			return false;

		ClipInfo clipInfo = m_activeAudio.Find( clip=>clip.handle == handle );

		// This calls back to SystemAudio once "isPlaying" is set false, so the active audio can be removed. Hacky, but means Stop can be called either from here or the handle.			
		if ( overTime > 0 || afterDelay > 0 )
		{ 
			if ( clipInfo != null )
				clipInfo.stopAfterTime = afterDelay;
			//Debug.Log($"Aftertime {afterTime}, Overtime {overTime}");
			if ( afterDelay > 0 && overTime <= 0 ) // Hack: if stopping after time, need to set *some* fade time so update logic knows to stop
				overTime = 0.0001f;
			
			StartFade(handle,0,overTime,true);
			return false;
		}
		
		// Stop immediately
		if ( clipInfo != null )
			clipInfo.stopAfterFade=true; // this ensures it'll be removed from active audio				
		handle.source.Stop();
		handle.source.gameObject.SetActive(false);
		return true;
	}

	/// Play a music track using the cue name, with optional crossfade time
	public static AudioHandle PlayMusic( string cueName, float fadeTime = 0 ) { return PlayMusic(cueName, fadeTime, fadeTime); }
	/// Play a music track using the cue name, with seperate fade out and fade in times
	public static AudioHandle PlayMusic( string cueName, float fadeOutTime, float fadeInTime ) 
	{
		AudioCue cue = m_instance.m_audioCues.Find(item=>string.Equals(cueName, item.name, System.StringComparison.OrdinalIgnoreCase));
		if ( cue == null && Debug.isDebugBuild && string.IsNullOrEmpty(cueName) == false)
		{			
			Debug.LogWarning("Music sound cue not found: "+cueName);
		}
		return PlayMusic( cue, fadeOutTime, fadeInTime );
	}

	/// Play a music cue, with optional crossfade time
	public static AudioHandle PlayMusic( AudioCue cue, float fadeTime = 0 ) { return PlayMusic(cue, fadeTime,fadeTime); }
	/// Play a music cue, with seperate fade out and fade in times
	public static AudioHandle PlayMusic( AudioCue cue, float fadeOutTime, float fadeInTime ) 
	{
		if ( m_instance.m_restartMusicIfAlreadyPlaying == false && m_instance.GetIsActiveMusic(cue) )
		{		
			// If flag set, we don't want to restart music that's already playing, just update it's volume
			m_instance.UpdateCurrentMusicVolumeFromCue(cue, fadeInTime);
			return m_instance.m_activeMusic;
		}
		StopMusic(fadeOutTime);		
		m_instance.m_musicCueName = cue == null ? null : cue.name;
		m_instance.m_musicVolOverride = 0;
		m_instance.m_activeMusic = Play(cue);

		//SetMusicLoopPoints( cue.m_loopStart, cue.m_loopEnd );

		if ( fadeInTime > 0 && cue != null )
		{
			//m_instance.StartCoroutine( m_instance.CoroutineFadeIn(m_instance.m_activeMusic, fadeInTime) );
			m_instance.StartFadeIn( m_instance.m_activeMusic, fadeInTime );
		}
		return m_instance.m_activeMusic;
	}

	/// Play a music track using the cue name, Crossfades beteween two music tracks, attempting to keep them synced (assumes identical length/tempo/etc). Optional volume multiplier
	public static AudioSource PlayMusicSynced( string name, float fadeTime, float volumeOverride = 0 ) 
	{
		return PlayMusicSynced( m_instance.m_audioCues.Find(item=>string.Equals(name, item.name, System.StringComparison.OrdinalIgnoreCase)), fadeTime, volumeOverride );
	}

	/// Crossfades beteween two music tracks, attempting to keep them synced (assumes identical length/tempo/etc). Optional volume multiplier
	public static AudioSource PlayMusicSynced( AudioCue cue, float fadeTime, float volumeOverride = 0)
	{
		if ( m_instance.m_activeMusic == null )
			return PlayMusic(cue);
		/* Doesn't make sense to play music synced if its th same track	*/
		if ( m_instance.GetIsActiveMusic(cue) )
		{
			// If flag set, we don't want to restart music that's already playing, just update it's volume
			m_instance.UpdateCurrentMusicVolumeFromCue(cue, fadeTime, volumeOverride);
			return m_instance.m_activeMusic;
		}
		
		const float spinUpTime = 0.25f; // Time buffer to give time to load the new track so they sync up correctly
		float syncTime = m_instance.m_activeMusic.time+spinUpTime;

		StopMusic(fadeTime*1.5f,spinUpTime);
		m_instance.m_musicCueName = cue == null ? null : cue.name;
		m_instance.m_musicVolOverride = 0;
		m_instance.m_activeMusic = Play(cue,null,1,1,syncTime,spinUpTime);

		// set the volume of the clip
		if ( volumeOverride > 0.0f )
		{
			m_instance.SetVolume(m_instance.m_activeMusic, volumeOverride);
			m_instance.m_musicVolOverride = volumeOverride;
		}
		//m_instance.StartCoroutine( m_instance.CoroutineFadeIn(m_instance.m_activeMusic, fadeTime) );
		m_instance.StartFadeIn( m_instance.m_activeMusic, fadeTime );
		return m_instance.m_activeMusic;
	}

	/// Stops the currently playing music, with optional fade out time
	public static void StopMusic(float fadeTime = 0, float afterDelay = 0)
	{
		Stop(SystemAudio.Get.m_activeMusic, fadeTime, afterDelay);
		m_instance.m_activeMusic = null;		
		m_instance.m_musicCueName = null;
		//m_instance.m_musicLoopStart = 0;
		//m_instance.m_musicLoopEnd = 0;
	}

	
	/// Flag to set whether playing the same music cue again will restart it, or leave the old one playing
	public static bool ShouldRestartMusicIfAlreadyPlaying => m_instance.m_restartMusicIfAlreadyPlaying;

	/// Plays an ambient sound by it's cue name, with optional fade time. Sound cue is assumed to be looping.
	public static void PlayAmbientSound( string name, float fadeTime = 0.4f )
	{
		PlayAmbientSound(name,fadeTime,fadeTime);
	}
	/// Plays an ambient sound by it's cue name, with seperate fade out and fade in times. Sound cue is assumed to be looping.
	public static void PlayAmbientSound( string name, float fadeoutTime, float fadeInTime )
	{
		StopAmbientSound(fadeoutTime);
		m_instance.m_activeAmbientLoop = SystemAudio.Play(name);
		if ( fadeInTime > 0 )
			m_instance.m_activeAmbientLoop.FadeIn(fadeInTime);
		m_instance.m_ambientCueName = name;
	}	

	/// Stops the current ambient sound cue, with optional fade time
	public static void StopAmbientSound(float overTime = 0.4f)
	{
		Stop(m_instance.m_activeAmbientLoop, overTime);
		m_instance.m_ambientCueName = string.Empty;
	}

	/// <summary>
	/// Sets a sound's volume and stereo pan based on distance to it.
	///  Use this function when you want sounds to be louder when standing near them, and quieter as you walk away.
	///  The function should be placed in your room's Update() function.
	///  eg. 
	///  ~~~
	///	void Update()
	//  {
	//		// Plays fire dist
	//  	AudioCue.UpdateCustomFalloff( "FireCrackle", P.Fireplace.LookAtPoint, C.Plr.Position, 10, 200, 0.2f, 1.0f );
	//  }
	///  ~~~
	/// </summary>
	/// <param name="cueName">Cue name to fade.</param>
	/// <param name="soundPos">The location of the sound. Eg: `P.Fireplace.LookAtPoint`.</param>
	/// <param name="listenerPos">The location of the listener (usually the player). Eg: `C.Player.Position`.</param>
	/// <param name="closeDist">How close should the player be to the sound for it to be at maxVol. In game units.</param>
	/// <param name="farDist">How far should the player be from the sound for it to be at minVol. In game units.</param>
	/// <param name="farVol">The volume when the player is standing FAR from the sound (eg. quiet). From 0 to 1.</param>
	/// <param name="closeVol">The volume when the player is standing CLOSE to the sound (eg. loud). From 0 to 1.</param>
	/// <param name="farPan">The maximum amount to pan in stereo, when the player is FAR from the sound. From 0 to 1.</param>
	public static void UpdateCustomFalloff(string cueName, Vector2 soundPos, Vector2 listenerPos, float closeDist, float farDist, float farVol = 0, float closeVol = 1, float farPan = 0.7f )
	{	
		AudioHandle[] handles = SystemAudio.GetHandles(cueName);
		if ( handles == null )
			return;
		foreach( AudioHandle handle in handles )
		{
			// Query the clip info to see if sound is "fading" if it's fading in or out we want to preserve that
			ClipInfo clipInfo = m_instance.m_activeAudio.Find(item=>item.handle == handle);
			if ( clipInfo == null || clipInfo.stopAfterFade ) // NB: once stop after fade is called, don't allow volume changes through this function				
				continue;

			float diff = soundPos.x - listenerPos.x;
			float vol = Mathf.Lerp(closeVol,farVol, Utils.EaseCubic(
				Mathf.InverseLerp(closeDist,farDist,Vector2.Distance(soundPos,listenerPos))));
			float pan = Mathf.Lerp(0,farPan, Utils.EaseCubic(
				Mathf.InverseLerp(closeDist,farDist,Mathf.Abs(diff))));
			pan = pan*Mathf.Sign(diff);

			if ( clipInfo.targetVolume != clipInfo.defaultVolume )
				clipInfo.targetVolume = vol;
			else 
				handle.volume = vol;
			handle.panStereo = pan;
		}
	}

	#endregion
	#region Public, advanced Functions
	
	

	/// @cond IGNORE

	/// Gets the base volume of a particular sound effect (usually use the AudioHandle for this) \noop
	public float GetVolume( AudioHandle source )
	{
		ClipInfo clipInfo = m_instance.m_activeAudio.Find(item=>item.handle == source);
		if ( clipInfo != null )
			return clipInfo.defaultVolume;
		return 0;
	}

	/// Sets the base volume of a particular sound effect (usually use the AudioHandle for this)
	public void SetVolume( AudioHandle source, float volume )
	{
		ClipInfo clipInfo = m_instance.m_activeAudio.Find(item=>item.handle == source);
		if ( clipInfo != null && clipInfo.stopAfterFade == false ) // NB: once stop after fade is called, don't allow volume changes through this function			
		{
			clipInfo.defaultVolume = volume;
			clipInfo.targetVolume = volume;
			// Set the volume immediately (Added 04/03/2024)- otherwise waits for next game update, which could be too late if changing rooms
			UpdateActiveAudioClip(clipInfo);
		}
	}

	
	/// Gets the base pan of a particular sound effect (usually use the AudioHandle for this) \private
	public float GetPan( AudioHandle source )
	{
		ClipInfo clipInfo = m_instance.m_activeAudio.Find(item=>item.handle == source);
		if ( clipInfo != null )
			return clipInfo.defaultPan;
		return 0;
	}

	/// Sets the base pan of a particular sound effect (usually use the AudioHandle for this) \private
	public void SetPan( AudioHandle source, float pan )
	{
		ClipInfo clipInfo = m_instance.m_activeAudio.Find(item=>item.handle == source);
		if ( clipInfo != null )
		{
			clipInfo.defaultPan = pan;
			source.source.panStereo = pan;
		}
	}
	
	/// Sets the volume (from 0.0 to 1.0) for audio of a particular type (eg: Sound effects, Music, Dialog). 
	public void SetVolume( AudioCue.eAudioType type, float volume )
	{		
		SystemAudio self = SystemAudio.Get;
		if ( self == null )
		{
			Debug.LogWarning("Failed to set AudioSystem volume. It hasn't been initialised");
			return;
		}

		float oldVolume = 1.0f;
		bool exists = false;

		if ( volume <= 0 ) // If volume goes to zero we lose the scale information. So make it -1 instead... mathyhack!
			volume = -1;

		for ( int i = 0; i < self.m_volumeByType.Count; ++i )
		{
			if ( self.m_volumeByType[i].m_type == type )
			{
				oldVolume = self.m_volumeByType[i].m_volume;
				self.m_volumeByType[i].m_volume = volume;
				exists = true;
				break;
			}
		}

		if ( exists == false )
		{
			// Doesn't exist yet, so add
			self.m_volumeByType.Add( new AudioTypeVolume() { m_type = type, m_volume = volume } );
		}

		float volChange = volume / oldVolume;
		// Update active audio volumes
		for ( int i = 0; i < self.m_activeAudio.Count; ++i )
		{
			ClipInfo info = self.m_activeAudio[i];
			if ( info != null && (info.type & (int)type) != 0)
			{					
				if ( info.handle.source != null )
					info.handle.source.volume *= volChange;	
				info.defaultVolume *= volChange;
				info.targetVolume *= volChange; 
			}
		}

	}

	public AudioMixerGroup NarratorMixerGroup => m_narratorMixerGroup;

	public bool GetAnyMusicPlaying() 
	{		
		return m_activeAudio.Exists( item=> (item.type&(int)AudioCue.eAudioType.Music)>0  );
	}

	/// Advanced function to retrieve a cue's default volume
	public float GetCueVolume(AudioCue cue, AudioClip specificClip = null)
	{
		if ( cue == null )
			return 0;
		SystemAudio self = SystemAudio.Get;

		AudioCue.Clip cueClip = cue.GetClipData(specificClip);				
		if ( cueClip == null )
			cueClip = cue.GetClip();

		float volume = cue.m_volume.GetRandom() * cueClip.m_volume.GetRandom() * 1;

		// Apply type volumes
		for ( int i = 0; i < self.m_volumeByType.Count; ++i )
		{
			if ( ((int)self.m_volumeByType[i].m_type & cue.m_type) != 0 )
				volume *= self.m_volumeByType[i].m_volume;
		}

		return volume;

	}

	/// Advanced function that pauses all sounds (used when pausing game)
	public void PauseAllSounds(bool alsoPauseMusic = false) 
	{ 
		foreach (ClipInfo audioClip in m_activeAudio) 
		{
			try 
			{
				if (alsoPauseMusic || audioClip.handle != m_activeMusic) 
				{
					Pause (audioClip.handle);
				}
			} 
			catch 
			{
				continue;
			}
		}
	}

	/// Advanced function that resumes all paused sounds (used when pausing game)
	public void ResumeAllSounds() 
	{
		foreach (ClipInfo audioClip in m_activeAudio) 
		{
			try 
			{
				if (!audioClip.handle.isPlaying) 
				{					
					UnPause(audioClip.handle);
				}
			} 
			catch 
			{
				continue;
			}
		}
	}

	/// Advanced function for retrieving the active music handle
	public AudioHandle GetActiveMusicHandle() { return  m_activeMusic; }

	/// Advanced function returns true if the passed cue is the active music
	public bool GetIsActiveMusic(AudioCue cue)
	{
		if ( m_activeMusic == null )
			return cue == null;
		if ( cue == null )
			return false;
		return cue.GetClipData(m_activeMusic.clip) != null;
	}

	// Used when playing the same music again, so it'll set the volume without restarting the music.
	void UpdateCurrentMusicVolumeFromCue(AudioCue cue, float fadeTime, float volumeOverride = 0) 
	{
		if ( m_activeMusic == null || cue == null )
			return;

		float vol = cue.GetClipData(m_activeMusic.clip).m_volume.GetRandom();
		vol *= cue.m_volume.GetRandom();

		// Don't play the same music again, but do update volume
		if ( volumeOverride > 0.0f )
		{
			vol = volumeOverride;
			m_musicVolOverride = volumeOverride;
		}
		m_activeMusic.Fade(vol,fadeTime);
	}


	/// Editor function for adding an audio cue to the list of cues playable by name. Primarily used by editor. Returns true if added, false if it already existed
	public bool EditorAddCue(AudioCue cue)
	{
		if ( m_audioCues.Contains(cue) == false )
		{
			m_audioCues.Add(cue);
			return true;
		}
		return false; 
	}

	/// Editor function for retrieving all audio cues
	public List<AudioCue> EditorGetAudioCues() { return m_audioCues; }
	/// If set, any cues in the Audio folder will be automatically added (you don't have to click the button in the cue)
	public bool EditorGetAutoAddCues() { return m_autoAddCues; }

	/// Starts fading in a handle. In powerquest it's usually more convenient to use `Audio.Play("MySound").FadeIn(overTime);`
	public void StartFadeIn(AudioHandle handle, float time)
	{
		float targetVolume = GetVolume(handle);
		SetVolume(handle,0);
		StartFade(handle, targetVolume, time);
	}

	/// Starts fading out a handle, optionally stopping it once faded out. In powerquest it's usually more convenient to use `Audio.Stop("MySound", overTime);
	public void StartFadeOut(AudioHandle handle, float time, bool stopOnFinish = false )
	{
		StartFade(handle,0,time,true);
	}

	/// Starts fading a handle to the target volume, optionally stopping it once finished. In powerquest it's usually more convenient to use `Audio.GetCue("MySound").Fade(targetVolume, overTime);`
	public void StartFade( AudioHandle handle, float targetVolume, float time, bool stopOnFinish = false )
	{
		ClipInfo clipInfo = m_activeAudio.Find( clip=>clip.handle == handle );
		if ( clipInfo == null )
			return;

		float oldTarget = clipInfo.targetVolume;

		clipInfo.targetVolume = targetVolume;
		clipInfo.stopAfterFade = stopOnFinish;

		if ( time <= 0 )
		{
			clipInfo.defaultVolume = targetVolume;
			if ( stopOnFinish )
				handle.Stop();
			return;
		}

		// Calc fade over time delta
		float dist = Mathf.Abs( targetVolume - oldTarget );
		if ( dist <= 0 )
		{
			if ( stopOnFinish )
				handle.Stop();
			return;
		}

		clipInfo.fadeDelta = dist/time;
	}

	// Save system callbacks
	//////////////////////////////////////////////////////////////////////

	/// @cond InternalSystem

	class SaveData
	{
		public string m_musicCueName = null;
		public float m_musicVolOverride = -1;
		public float m_musicTime = 0;
		public string m_ambientCueName = null;
		public bool m_restartMusicIfAlreadyPlaying = false;
		public float m_falloffDistanceMultiplier = 1;
		
		//public float m_musicLoopStart = 0;
		//public float m_musicLoopEnd = 0;

		public class ActiveAudioSaveData
		{
			public string cueName = null;
			public bool paused = false;
			public float time = 0;
			public float pan = 0;
			public float pitch = 1;
			public float volume = 1;
			public float volumeTarget = 1;
			public float fadeDelta = 0;
			public bool stopAfterFade = false;

			public ActiveAudioSaveData() {}
			public ActiveAudioSaveData(ClipInfo clipInfo)
			{
				cueName = clipInfo.cue.name;
				paused = clipInfo.paused;
				time =  clipInfo.handle.time;
				pitch = clipInfo.defaultPitch;
				pan = clipInfo.defaultPan;
				volume = clipInfo.defaultVolume;
				volumeTarget = clipInfo.targetVolume;
				fadeDelta = clipInfo.fadeDelta;
				stopAfterFade = clipInfo.stopAfterFade;
			}

			public void InitClipInfo(ClipInfo clipInfo)
			{
				if ( clipInfo == null )
					return;
				clipInfo.defaultVolume = volume;
				clipInfo.targetVolume = volumeTarget;
				clipInfo.defaultPitch = pitch;
				clipInfo.defaultPan = pan;
				clipInfo.fadeDelta = fadeDelta;
				clipInfo.stopAfterFade = stopAfterFade;
			}
		}
		public ActiveAudioSaveData[] activeAudio = null;
	}

	/// Internal system function
	public object GetSaveData()
	{		
		SaveData result = new SaveData();
		result.m_musicCueName = m_musicCueName;
		result.m_musicVolOverride = m_musicVolOverride;
		result.m_ambientCueName = m_ambientCueName;
		result.m_restartMusicIfAlreadyPlaying = m_restartMusicIfAlreadyPlaying;
		result.m_falloffDistanceMultiplier = m_falloffDistanceMultiplier;
		result.m_musicTime = m_activeMusic == null ? 0 : m_activeMusic.time;
		//result.m_musicLoopStart=m_musicLoopStart;
		//result.m_musicLoopEnd=m_musicLoopEnd;

		result.activeAudio = new SaveData.ActiveAudioSaveData[m_activeAudio.Count];
		for ( int i = 0; i < m_activeAudio.Count; ++i )
		{
			ClipInfo clipInfo = m_activeAudio[i];				
			if ( clipInfo != null && clipInfo.cue != null
				&& clipInfo.handle.isPlaying
				&& clipInfo.handle != m_activeMusic && clipInfo.handle != m_activeAmbientLoop )
			{				
				result.activeAudio[i] = new SaveData.ActiveAudioSaveData(clipInfo);
			}
		}

		return result as object;
	}

	/// Internal system function
	public void RestoreSaveData(object obj)
	{
		if ( obj == null || obj as SaveData == null )
			return;
		SaveData data = obj as SaveData;

		m_restartMusicIfAlreadyPlaying = data.m_restartMusicIfAlreadyPlaying;
		m_falloffDistanceMultiplier = data.m_falloffDistanceMultiplier;

		// Stop/fade current sounds and start them from the ones in data.m_activeAudio. 
		foreach ( ClipInfo clip in m_activeAudio )
		{
			if ( clip != null && clip.handle != null )
				Stop(clip.handle,0.1f);
		}

		if ( data.activeAudio != null )
		{
			// Start cues from save data
			foreach ( SaveData.ActiveAudioSaveData activeAudio in data.activeAudio )
			{
				if ( activeAudio == null )
					continue;
				AudioHandle handle = Play( activeAudio.cueName );				
				ClipInfo clipInfo = m_instance.m_activeAudio.Find( item=>item.handle == handle );
				activeAudio.InitClipInfo(clipInfo);
				handle.time = activeAudio.time;
				handle.panStereo = activeAudio.pan;
				if ( activeAudio.paused )
					Pause(handle);
				//else if ( clipInfo.targetVolume != clipInfo.defaultVolume )
				//	StartFadeIn(handle,0.1f); // if not already fading, fade the sound in
			}
		}

		if ( string.IsNullOrEmpty( data.m_musicCueName ) )
		{
			StopMusic(0.1f);
			m_musicVolOverride = -1;
		}
		else
		{
			AudioHandle handle = PlayMusic(data.m_musicCueName);
			if ( handle != null )
			{
				if ( data.m_musicVolOverride > 0 )
				{
					m_musicVolOverride = data.m_musicVolOverride;
					SetVolume(m_instance.m_activeMusic, m_musicVolOverride);	
				}
				if ( m_activeMusic != null && m_activeMusic.clip != null )					
					m_activeMusic.time = data.m_musicTime % Mathf.Max(m_activeMusic.clip.length-1.0f,m_activeMusic.clip.length*0.9f); // There's a known unity error where starting from time errors. So don't set source time if it's close to the end (NB: This still may happen, need to watch for it)
				
				m_instance.StartFadeIn( m_instance.m_activeMusic, 0.1f );
			}
		}

		// Restart ambient sound
		if ( string.IsNullOrEmpty( data.m_ambientCueName ) )
		{
			StopAmbientSound(0.1f);
		}
		else 
		{
			PlayAmbientSound(data.m_ambientCueName);
		}

	}

	/// @endcond

	#endregion
	#region Private Functions

	void OnSceneLoaded( Scene scene, LoadSceneMode loadSceneMode )
	{
		AudioListener audioListener = System.Array.Find<AudioListener>( GameObject.FindObjectsOfType<AudioListener>(), item => item.enabled );
		if ( audioListener != null )
			m_audioListener = audioListener.transform;
		if ( m_audioListener == null )
		{
			if ( Debug.isDebugBuild ) Debug.Log("Unable to find audio listener in scene");
			return;
		}

		/* PowerQuest: Don't stop sound effects when loading scene
		// Stop all sound effects - Loop through in reverse since clips may be stopped, removing them from this list
		for (int i = m_activeAudio.Count - 1; i >= 0; i--)
		{
			ClipInfo info = m_activeAudio[i];
			if ( (info.type & (int)AudioCue.eAudioType.Music) == 0 )
			{
				// Not music, so end it. Give it a nice fade...
				Stop( info.handle, 0.1f);
			}
		}
		*/

	}


	void Awake() 
	{

		SceneManager.sceneLoaded += OnSceneLoaded;

		SetSingleton();
		if ( Application.isPlaying )
			Object.DontDestroyOnLoad(this);	

		if ( LAYER_NOPAUSE == -1 )
			LAYER_NOPAUSE = LayerMask.NameToLayer("NoPause");

		if ( Application.isPlaying )
		{
			for ( int i = 0; i < AUDIO_SOURCE_POOL_SIZE; ++i )
			{
				GameObject soundLoc = new GameObject(STRING_NAME_PREFIX);
				m_audioSources.Add( soundLoc.AddComponent<AudioSource>() );
				soundLoc.layer = LAYER_NOPAUSE;
				soundLoc.SetActive(false);
				soundLoc.transform.parent = transform;
			}
		}
		m_audioCues.RemoveAll(item=>item == null);


		if ( m_audioListener == null )
		{
			AudioListener listener = ((AudioListener)GameObject.FindObjectOfType(typeof(AudioListener)) );
			if ( listener != null ) 
				m_audioListener = listener.transform;
		}
		if ( m_audioListener == null )
			if ( Debug.isDebugBuild ) Debug.LogWarning("Unable to find audio listener in scene");


		m_activeMusic = null;

	}

	void OnApplicationFocus(bool hasFocus )
	{	    
		m_hasFocus = hasFocus;
	}

	// Update is called once per frame
	void Update() 
	{
		if ( m_audioListener == null )
		{
			AudioListener listener = ((AudioListener)GameObject.FindObjectOfType(typeof(AudioListener)) );
			if ( listener != null ) 
				m_audioListener = listener.transform;
		}
		if ( m_audioListener == null )
			return; // if it's still null, bail. maybe not ready yet

		transform.position = m_audioListener.position;
		UpdateActiveAudio();
	}

	float GetFalloff( Vector2 soundPos )
	{
		if (m_cameraGame == null )			
			return 1.0f;
		float xStart = m_cameraGame.orthographicSize * m_cameraGame.aspect * m_falloffStart * m_falloffDistanceMultiplier; // Falloff starts half a screen past the edge
		float xFalloff = (xStart * m_falloffEnd * m_falloffDistanceMultiplier);
		return Mathf.Lerp( 1, m_falloffMinVol, Utils.EaseCubic((Mathf.Abs(soundPos.x - m_cameraGame.transform.position.x) - xStart) / xFalloff ) );

	}

	float GetPanPos( Vector2 soundPos )
	{
		if (m_cameraGame == null )	
			return 0.0f;
		float dist = soundPos.x - m_cameraGame.transform.position.x;
		float distMult = m_cameraGame.orthographicSize * m_cameraGame.aspect * m_falloffDistanceMultiplier;	
		float xFalloff = Mathf.InverseLerp(m_falloffPanStart * distMult, m_falloffPanEnd * distMult, Mathf.Abs(dist) );
		return Mathf.Lerp( 0, m_falloffPanMax, Utils.EaseCubic(xFalloff) ) * Mathf.Sign(dist);
	}

	void SetSource(ref AudioSource source, AudioClip clip, float volume, float pitch, float panStereo, int priority, Transform emmitter) 
	{
		source.spatialize = false;
		source.priority = priority;
		source.pitch = pitch;				
		source.clip = clip;
		source.playOnAwake = false;

		if ( emmitter )
		{
			source.volume = volume * GetFalloff(emmitter.position);
			source.panStereo = Mathf.Clamp(panStereo + GetPanPos(emmitter.position),-1,1);
		}
		else 
		{
			source.volume = volume;
			source.panStereo = panStereo;	
		}
	}	

	// Adds clip to active audio for updating
	void AddActiveAudio( ClipInfo info )
	{
		m_activeAudio.Add(info);

		// Update activeaudio for this clip immediately incase it starts playing before update loop
		UpdateActiveAudioClip(info);
	}


	void UpdateActiveAudioClipLoopPoints(ClipInfo audioClip)
	{
		if ( audioClip.paused )
			return;

		// Update 'stop after time'		
		if ( audioClip.stopAfterTime > 0 )
			audioClip.stopAfterTime -= Time.deltaTime;
		
		if ( audioClip.handle == null || audioClip.cue == null )
			return;

		// Handle looping cue at loop points
		AudioCue.LoopSection loop = audioClip.cue.m_loopSection;
		float time = audioClip.handle.time;

		float endTime = loop.m_endTime;
		if ( endTime > 0 )
		{
			if ( loop.m_startTime >= endTime )
			{
				Debug.LogError("Loop point start time after end time");
				return;
			}

			endTime = Mathf.Max(loop.m_startTime+0.5f, loop.m_endTime); // make sure end is after start
			if ( audioClip.handle.source != null && audioClip.handle.source.clip != null  )
				endTime = Mathf.Min(endTime, audioClip.handle.source.clip.length);

			if ( endTime > 0 && (audioClip.handle.isPlaying == false || ((float)endTime-time) < 1.0f) && audioClip.stopAfterFade == false ) // Queue next track a second early so it has time to stream in
			{		
				float timeBeforeLoop = Mathf.Max(0,endTime-time);//-(Time.deltaTime*0.5f));
				if ( audioClip.handle.isPlaying == false )
					timeBeforeLoop = 0.0f; // loop immediately if was already stopped
				Stop(audioClip.handle, Mathf.Max(0.067f,loop.m_fadeOut), timeBeforeLoop);
				AudioHandle handle = Play(audioClip.cue, null,1,1,loop.m_startTime, timeBeforeLoop ).FadeIn(Mathf.Max(0.033f,loop.m_fadeIn));
				// If we're looping the active music track, swap the handles
				if ( m_activeMusic == audioClip.handle )
					m_activeMusic = handle;
				// same for ambient
				if ( m_activeAmbientLoop == audioClip.handle )
					m_activeAmbientLoop = handle;
			}
		}
	}

	// Updates all active audio
	void UpdateActiveAudio() 
	{
		// First check if the game has focus, since if not, the audio is potentially still active
		if ( m_hasFocus == false )
			return;

		if ( m_cameraGame == null )
			m_cameraGame = Camera.main;
		/*
		// Handle looping music at loop points from SetMusicLoopPoints()
		if ( m_activeMusic != null && m_musicLoopEnd > 0 && (m_activeMusic.isPlaying == false || ((float)m_musicLoopEnd-m_activeMusic.time) < 1.0f) ) // Queue next music a second early so it has time to stream in
		{			
			float timeBeforeLoop = Mathf.Max(0,m_musicLoopEnd-m_activeMusic.time-(Time.deltaTime*0.5f));
			if ( m_activeMusic.isPlaying == false )
				timeBeforeLoop = 0;
			m_activeMusic.Stop(0.067f, timeBeforeLoop);
			List<AudioHandle> handles=new List<AudioHandle>();
			AudioHandle handle = Play(GetCue(m_activeMusic.cueName), null,1,1,m_musicLoopStart, timeBeforeLoop ).FadeIn(0.033f);
			m_instance.m_activeMusic = handle;
		}*/

		bool playingDialog = GetVolume(AudioCue.eAudioType.Dialog) > 0.5f // Only duck if dialog volume is turned up more than 50%
			&& m_activeAudio.Exists(item=> item != null && (item.type & (int)AudioCue.eAudioType.Dialog) != 0 );

		float finalDuckVol = 1;
		if ( playingDialog )
		{
			finalDuckVol = m_musicDuckingMaxVolume;
		}
		

		List<ClipInfo> toRemove = new List<ClipInfo>();	    
		for ( int i = 0; i < m_activeAudio.Count; ++i )
		{
			ClipInfo audioClip = m_activeAudio[i];
			
			UpdateActiveAudioClipLoopPoints(audioClip);
			if (audioClip.handle == null || (audioClip.handle.isPlaying == false && audioClip.paused == false ) ) 
			{
				toRemove.Add(audioClip);
			} 
			else 
			{
				UpdateActiveAudioClip(audioClip, finalDuckVol);
			}
		}

		// Cleanup
		foreach (ClipInfo audioClip in toRemove) 
		{
			m_activeAudio.Remove(audioClip);
			if ( audioClip.handle != null )
				audioClip.handle.Stop();
		}

		#if !UNITY_2018_3_OR_NEWER
		// this dunnae really work, because the clip isn't destroyed ever because you can't use destroy with a timer in edit mode		
		if ( Application.isEditor && Application.isPlaying == false && m_activeAudio.Count <= 0  )
		{
			DestroyImmediate(gameObject);
		}
		#endif

	}


	// updates single active audio clip
	void UpdateActiveAudioClip( ClipInfo audioClip, float duckingVolume = 1.0f )
	{
		float volumeMod = 1.0f;		
		float panMod = 0;

		if ( audioClip.emmitter != null && audioClip.emmitter != transform )
		{
			//audioClip.position = audioClip.emmitter.position;
			volumeMod *= GetFalloff(audioClip.emmitter.position);	
			panMod = GetPanPos(audioClip.emmitter.position);
		}	

		// Update Fading
		if ( Utils.ApproximatelyZero(audioClip.fadeDelta) == false )// Removed 18/4/23, cues were getting stuck at target value without ever stopping// && (Mathf.Approximately(audioClip.targetVolume, audioClip.defaultVolume) == false) )
		{
			// if cue has start delay, dont' start fading in yet			
			bool waitingToStart = ( audioClip.defaultVolume <= 0 && audioClip.targetVolume > 0 && audioClip.handle.source.time <= audioClip.startFromTime);
			//if ( audioClip.targetVolume > 0 )
			//	Debug.Log($"Time: {audioClip.handle.source.time}");
			// If stopAfterTime is set, don't start fading yet
			bool waitingToStop = audioClip.stopAfterTime > 0 && audioClip.stopAfterFade;
			if ( waitingToStart == false && waitingToStop == false)
			{
				audioClip.defaultVolume = Mathf.MoveTowards(audioClip.defaultVolume,audioClip.targetVolume, audioClip.fadeDelta*Time.deltaTime);

				// If flagged to stop on fadeout, stop the source
				if ( audioClip.stopAfterFade && Mathf.Approximately(audioClip.targetVolume, audioClip.defaultVolume) && audioClip.handle.isPlaying  )				
					audioClip.handle.source.Stop();
			}
		}

		// Duck music
		if ( duckingVolume < 1.0f && (audioClip.type & (int)AudioCue.eAudioType.Music) != 0 && duckingVolume < audioClip.defaultVolume )
		{
			volumeMod = duckingVolume / audioClip.defaultVolume;
		}

		audioClip.handle.source.volume = audioClip.defaultVolume * volumeMod;
		audioClip.handle.source.panStereo = Mathf.Clamp(audioClip.defaultPan + panMod,-1,1);
	}

	AudioSource SpawnAudioSource( string name, Vector2 position )
	{
		AudioSource source = null;
		for ( int i = 0; i < m_audioSources.Count; ++i )
		{
			source = m_audioSources[i];
			if ( source == null )
			{
				// source has been deleted, add new one in place
				GameObject soundLoc = new GameObject(STRING_NAME_PREFIX);			
				//Create the source
				source = soundLoc.AddComponent<AudioSource>();
				soundLoc.layer = LAYER_NOPAUSE;
				m_audioSources[i] = source;
				soundLoc.SetActive(false);
				soundLoc.transform.parent = transform;
				break;
			}
			else if ( source.gameObject.activeSelf == false )
			{
				break;
			}
			source = null;
		}

		if ( source == null )
		{
			// no pooled source found, add new source
			GameObject soundLoc = new GameObject(STRING_NAME_PREFIX);			
			//Create the source
			source = soundLoc.AddComponent<AudioSource>();
			soundLoc.layer = LAYER_NOPAUSE;
			m_audioSources.Add(source);
			soundLoc.SetActive(false);
			soundLoc.transform.parent = transform;
		}
		if ( source != null )
		{			
			if ( source.isPlaying )
				Debug.Log("Reusing audio Source that's playing: "+source.clip.name);
			source.gameObject.SetActive(true);
			if ( Debug.isDebugBuild )
				source.gameObject.name = name;
			source.gameObject.transform.position = position;
		}
		else 
		{
			if ( Debug.isDebugBuild ) Debug.Log("Failed to spawn audio source");
		}
		return source;

	}

	void AddSourceFilters(GameObject source)
	{

		{
			AudioLowPassFilter filter = source.AddComponent<AudioLowPassFilter>();
			filter.enabled = false;
		}
		{
			AudioHighPassFilter filter = source.AddComponent<AudioHighPassFilter>();
			filter.enabled = false;
		}
		{
			AudioEchoFilter filter = source.AddComponent<AudioEchoFilter>();
			filter.enabled = false;
		}
		{
			AudioChorusFilter filter = source.AddComponent<AudioChorusFilter>();
			filter.enabled = false;
		}
		{
			AudioDistortionFilter filter = source.AddComponent<AudioDistortionFilter>();
			filter.enabled = false;
		}
		{
			AudioReverbFilter filter = source.AddComponent<AudioReverbFilter>();
			filter.enabled = false;
		}
	}
	
	
	// @endcond

	// From linear volume (0 to 1) to DB (-144 to 0);
	public static float ToDecibel(float linear)
	{
		linear = Mathf.Clamp01(linear);
		return (linear != 0) ? 20.0f * Mathf.Log10(linear) : -144;
	}
	// From decibel volume (-144 to 0) to linear volume (0 to 1)
	public static float FromDecibel(float dB)
	{
		return Mathf.Pow(10.0f, dB * 0.05f);
	}

	#endregion

}

#region Class: AudioHandle
/// Audio handle- Use for changing a sound that's currently playing
/**
 * - All the Play(...) functions in the audio system return an AudioHandle. You can store and use this to manipluate the clip that's playing. Especially useful for looping sounds (at least, if you ever want to stop them)
 * - It wraps around unity's "AudioSource", but gives access to it so you can still use whatever functionality unity provides with it's AudioSources
 * - See Unity's docs on AudioSource for more specific info
 */
public class AudioHandle
{
	public AudioHandle(AudioSource source, string fromCue = null ) { m_source = source; m_cueName = fromCue; }

	public static bool IsPlaying( AudioHandle handle ) { return handle != null && handle.isPlaying; }
	public static bool IsNullOrStopped( AudioHandle handle ) { return handle == null || handle.isPlaying == false; }

	public static implicit operator AudioSource( AudioHandle handle ) { return handle == null ? null : handle.m_source; }

	/// Retrieves the unity AudioSource this handle is using
	public AudioSource source { get { return m_source; } }	
	public string cueName { get { return m_cueName; } }	
	/// Is the clip playing right now? (Read Only). Note: will return false when AudioSource.Pause is called.
	public bool isPlaying { get { return m_source != null && m_source.isPlaying; } }
	/// The base volume of clip (0.0 to 1.0) Before falloff, ducking, etc is applied. To get the final volume, use source.volume. 
	public float volume { get { return m_source == null ? 0 : SystemAudio.Get.GetVolume(this); } set { if ( m_source != null ) SystemAudio.Get.SetVolume(this,value); } }
	/// The pitch of the audio source. See Unity docs for more info.
	public float pitch { get { return m_source == null ? 0 : m_source.pitch; } set { if ( m_source != null ) m_source.pitch = value; } }
	/// Pans a playing sound in a stereo way (left or right). See Unity docs for more info.
	public float panStereo { get { return m_source == null ? 0 : SystemAudio.Get.GetPan(this); } set { if ( m_source != null ) SystemAudio.Get.SetPan(this, value); } }
	/// Playback position in seconds. See Unity docs for more info
	public float time { get { return m_source == null ? 0 : m_source.time; } set { if ( m_source != null ) m_source.time = Mathf.Min(value, (m_source.clip.length-0.01f)*0.9f); } } // Hack, cause sounds won't play if time gets set too close to end
	/// Is the audio clip looping 
	public bool loop { get { return m_source == null ? false : m_source.loop; } }
	/// Delay before cue is started- must be set immediately after playing (same as `AfterDelay()`)
	public float startDelay { set { AfterDelay(value); } }
	/// The audio clip that is playing
	public AudioClip clip { get { return m_source == null ? null : m_source.clip; } }
	/// Pauses playing the clip
	public void Pause() { SystemAudio.Pause(this); }
	/// Resumes playing the clip after pausing
	public void UnPause() { SystemAudio.UnPause(this); }
	/// Stops the clip, optionally fading out over time, and/or after a delay
	public void Stop(float overTime = 0.0f, float afterDelay = 0.0f) 
	{ 			
		if ( SystemAudio.GetValid() == false )
			return;		
		if ( SystemAudio.Get.StopHandleInternal(this, overTime,afterDelay) )
			m_source = null; // need to clear the source, and don't want to expose it, so doing it here. SystemAudio.Stop calls through to this to make sure its cleared.
	}


	/// Makes the clip start after the passed in time (if called immediately after play) eg. `Audio.Play("GunshotEcho").AfterDelay(1.0f);
	public void AfterDelay(float time)
	{
		if ( m_source == null )
		{
			Debug.LogWarning("Attempted to set start delay on sound handle that is not playing");
			return;
		}

		m_source.Stop();
		m_source.PlayDelayed(time);
	}

	/// Fades in the sound from zero, call directly after playing the sound to fade it in. eg `Audio.Play("MoodyAmbience").FadeIn(1);`
	public AudioHandle FadeIn(float overTime)
	{
		SystemAudio.Get.StartFadeIn(this,overTime);
		return this;
	}

	/// Fades the volume from it's current volume to another over the specified length of time
	public AudioHandle Fade(float targetVolume, float overTime)
	{
		SystemAudio.Get.StartFade(this,targetVolume,overTime);
		return this;
	}

	// The audio source
	AudioSource m_source = null;
	string m_cueName = null;
}
#endregion

/// @cond IGNORE
// Attribute used to mark string fields that should be added to the text system to support localization. Eg: [QuestLocalize, SerializeField] string m_description;
public class QuestAudioCueName : PropertyAttribute
{
	public QuestAudioCueName(){}
}
/// @endcond
}
