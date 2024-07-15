using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PowerTools.Quest
{

/*
public interface IAudioCue
{
	
	int Type {get;}
	bool Loop {get;}
	MinMaxRange Volume {get;}
	MinMaxRange Pitch {get;}
	MinMaxRange Pan {get;}
	AudioCue AlsoPlay {get;}
	MinMaxRange StartDelay {get;}
	float NoDuplicateTime {get;}
	UnityEngine.Audio.AudioMixerGroup MixerGroup {get;}
	AudioReverbPreset ReverbPreset {get;}
	float DistortionLevel {get;}
	int LowPass {get;}
	float LowPassQ {get;}
	float HighPass {get;}
	float HighPassQ {get;}
	AudioEchoFilter echoFilter {get;}
	AudioLowPassFilter lowPassFilter {get;}
	AudioHighPassFilter highPassFilter {get;}
	AudioChorusFilter chorusFilter {get;}
	List<AudioCue.Clip> sounds {get;}
	
	void SetTypeToMusic();
	void SetTypeToSFX();
	void SetTypeToDialog();

	void Play();
	void Play( Transform emmitter );
	AudioCue.Clip GetClip();
	int GetClipIndex(AudioClip clip);
	AudioClip GetClip(int index);
	AudioCue.Clip GetClipData(int index);
	AudioCue.Clip GetClipData(AudioClip clip);
	int GetClipCount();
	WeightedShuffledIndex GetShuffledIndex() ;
}
*/

public class AudioCue : MonoBehaviour 
{
	/*public enum eShake 
	{
		None,
		Small,
		Medium,
		Large
	}*/

	public enum eAudioType
	{
		Sound =  1<<0,
		Music =  1<<1,
		Dialog = 1<<2,
		User1 =  1<<3,
		User2 =  1<<4,
		User3 =  1<<5,
		User4 =  1<<6,
		User5 =  1<<7,
	};
	
	[System.Serializable]
	public class Clip
	{
		[HideInInspector]public AudioClip m_sound = null;
		[HideInInspector]public float m_weight = 100;
		[HideInInspector]public bool m_loop = false; // Legacy: No longer used
		[Tooltip("Volume. Multiplier on the base cue volume")]//, MinMaxRange(0,1)]
		public MinMaxRange m_volume = new MinMaxRange(1.0f);
		[Tooltip("Volume. Multiplier on the base cue pitch")]//,MinMaxRange(0.01f,4)]
		public MinMaxRange m_pitch = new MinMaxRange(1.0f);		
		[Tooltip("At what point in the clip should play end (crops the sound)")]
		public float m_startTime = 0.0f;
		[Tooltip("At what point in the clip should play end (crops the sound)")]
		public float m_endTime = 0.0f;
	}

	[System.Serializable]
	public struct LoopSection { public float m_startTime; public float m_endTime; public float m_fadeIn; public float m_fadeOut; }

	[Header("Type of Sound")]
	[Tooltip("Type of sound. Used to allow adjusting volume of different types of sounds")]
	[BitMask(typeof(eAudioType))]
	public int m_type = (int)eAudioType.Sound;
	[Tooltip("Whether it's a looping sound")]
	public bool m_loop = false;
	[Tooltip("If set, the sound will loop back to the start time when it reaches the end time. Useful for music that has an intro/outro section")]
	public LoopSection m_loopSection = new LoopSection();

	[Header("Basic settings")]
	[Tooltip("Volume. Usually Randomly chosen within the specified range. But if Camera Size Range is set then it's the min/max for volume as it goes out of camera range")]
	[MinMaxRange(0,1/*,2*/)]
	public MinMaxRange m_volume =new MinMaxRange(1.0f);
	[Tooltip("Pitch. Randomly chosen within the specified range"),MinMaxRange(0.01f,4)]
	public MinMaxRange m_pitch =new MinMaxRange(1.0f);
	[Tooltip("Stereo Pan. 0 is center, -1  is left, 1 is right. Randomly chosen within the specified range"),MinMaxRange(-1,1)]
	public MinMaxRange m_pan =new MinMaxRange(0.0f);
	//public int m_priority = 128;

	[Header("Playback Settings")]
	[Tooltip("Trigger another cue when this is played")]
	public AudioCue m_alsoPlay = null;	
	[Tooltip("The random chance that this sound will play at all")]
	[Range(0,1)]
	public float m_chance = 1;	
	[Tooltip("Delay before playing sound, after cue is played")]
	public MinMaxRange m_startDelay = new MinMaxRange(0.0f);
	[Tooltip("Time ove which the sound fades in")]
	public float m_fadeInTime = 0;
	//[Tooltip("Time before the end of the sound before it fades out")]
	//public bool m_fadeOutTime = 0;
	
	[Tooltip("If >= 0 this overrides the default delay before the same sound can be repeated")] 
	public float m_noDuplicateTime = -1;

	[Header("Mixer Group")]
	[Tooltip("For more advanced enviroment effects, play using a mixer group")]
	public UnityEngine.Audio.AudioMixerGroup m_mixerGroup = null;
	[Header("Effects")]
	[Tooltip("Reverb setting")]
	public AudioReverbPreset m_reverbPreset = AudioReverbPreset.Off;	
	[Range(0,1)]
	public float m_reverbLevel = 0.0f;
	[Range(0,0.9f)]
	[Tooltip("Distortion amount")]
	public float m_distortionLevel = 0;
	[Tooltip("Low pass filter (remove treble) cutoff frequency in Hz (eg: 5000. Or Zero to disable)")]
	[Range(0,22000)]
	public int m_lowPass = 0;
	[Tooltip("Determines how much of the filter's self resonance is dampened (1 by default)")]
	public float m_lowPassQ = 1;
	[Tooltip("High pass filter (remove bass) cutoff frequency in Hz (eg: 5000. Or Zero to disable)")]
	[Range(0,22000)]
	public float m_highPass = 0;
	[Tooltip("Determines how much of the filter's self resonance is dampened (1 by default)")]
	public float m_highPassQ = 1;

	[Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioEchoFilter m_echoFilter = null;
	[HideInInspector, Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioLowPassFilter m_lowPassFilter = null;
	[HideInInspector,Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioHighPassFilter m_highPassFilter = null;
	[Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioChorusFilter m_chorusFilter = null;

	//public float m_shakeTime = 0.0f;
	//public eShake m_shakeType = eShake.None;

	public List<Clip> m_sounds = new List<Clip>(1);

	public void SetTypeToMusic() { m_type = (int)eAudioType.Music; }
	public void SetTypeToSFX() { m_type = (int)eAudioType.Sound; }
	public void SetTypeToDialog() { m_type = (int)eAudioType.Dialog; }



	WeightedShuffledIndex m_shuffledIndex = null;

	public void Play()
	{
		SystemAudio.Play(this);
	}
	public void Play( Transform emmitter )
	{
		SystemAudio.Play(this, emmitter);
	}
	
	public Clip GetClip()
	{
		if ( m_sounds.Count == 0 )
			return null;
		ValidateShuffledList();
		return m_sounds[m_shuffledIndex];
	}

	public int GetClipIndex(AudioClip clip)
	{
		return m_sounds.FindIndex(item=>item.m_sound == clip);
	}

	public AudioClip GetClip(int index)
	{
		return m_sounds.IsIndexValid(index) ? m_sounds[index].m_sound : null;
	}

	public Clip GetClipData(int index)
	{
		return m_sounds.IsIndexValid(index) ? m_sounds[index] : null;
	}

	public Clip GetClipData(AudioClip clip)
	{
		return m_sounds.Find(item=>item.m_sound == clip);
	}

	public int GetClipCount() { return m_sounds.Count; }

	public WeightedShuffledIndex GetShuffledIndex() 
	{ 
		ValidateShuffledList();
		return m_shuffledIndex; 
	}

	void ValidateShuffledList()
	{
		if ( m_shuffledIndex == null || m_shuffledIndex.Count != m_sounds.Count )
		{
			m_shuffledIndex = new WeightedShuffledIndex();
			m_shuffledIndex.SetWeights( m_sounds, (item)=>item.m_weight );		
		}
	}


}

}
