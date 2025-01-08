using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

namespace PowerTools.QuestGui
{

//
[System.Serializable] 
[AddComponentMenu("Quest Gui/Image")]
public partial class Image : GuiControl, IImage
{
	#region Vars: Editor
		
	[SerializeField] string m_anim = null;
	[SerializeField, HideInInspector] RectCentered m_customSize = RectCentered.zero;

	#endregion
	#region Vars: Private
	
	SpriteRenderer m_sprite = null; 
	SpriteAnim m_spriteAnimator = null;
	bool m_overrideAnimPlaying = false;
	int m_stopOverrideAnimDelay = -1;
	
	#endregion
	#region Funcs: Unity
	
	protected override void Start()
	{
		base.Start();
		OnAnimationChanged();	
	}

	#endregion
	#region Funcs: IImage interface
		
	public override string Anim 
	{ 
		get { return m_anim;} 
		set 
		{ 
			if ( m_anim != value )
			{
				m_anim = value;
				OnAnimationChanged(); 
			}
		} 
	}
	
	public bool Animating
	{
		get { return GetAnimating(); }
	}

	public void PauseAnimation()
	{
		if ( m_spriteAnimator != null && m_overrideAnimPlaying )
		{
			m_spriteAnimator.Pause();
		}
	}

	public void ResumeAnimation()
	{
		if ( m_spriteAnimator != null && m_overrideAnimPlaying )
		{
			m_spriteAnimator.Resume();
		}
	}

	// Return to current animation set in data
	public void StopAnimation()
	{
		if ( m_spriteAnimator != null && m_overrideAnimPlaying )
			PlayAnimInternal(m_anim);		
		m_overrideAnimPlaying = false;
	}
	
	// NB: Animation play/pause/resume/stop stuff doesn't get saved. If you want to permanently change anim, set the Animation property
	public Coroutine PlayAnimation(string animName)
	{
		return PowerQuest.Get.StartCoroutine(CoroutinePlayAnimation(animName)); 
	}
	public void PlayAnimationBG(string animName) 
	{ 
		if ( PowerQuest.Get.GetSkippingCutscene() == false ) PlayOverrideAnim(animName); 
	}

	public void AddAnimationTrigger(string triggerName, bool removeAfterTriggering, System.Action action)
	{		
		QuestAnimationTriggers triggerComponent = GetComponent<QuestAnimationTriggers>();
		if ( triggerComponent == null )
			triggerComponent = gameObject.AddComponent<QuestAnimationTriggers>();
		if ( triggerComponent != null )
			triggerComponent.AddTrigger(triggerName, action, removeAfterTriggering);		
	}
	public void RemoveAnimationTrigger(string triggerName)
	{
		QuestAnimationTriggers triggerComponent = GetComponent<QuestAnimationTriggers>();
		if ( triggerComponent != null )
			triggerComponent.RemoveTrigger(triggerName);		
	}

	public Coroutine WaitForAnimTrigger(string triggerName) { return PowerQuest.Get.StartCoroutine(CoroutineWaitForAnimTrigger(triggerName)); }	
		
	public IQuestClickable IClickable { get{ return this; } }

	#endregion
	#region Funcs: Public (Non interface)
		
	public SpriteRenderer GetSprite() { return m_sprite; }
	public SpriteAnim GetSpriteAnimator() { return m_spriteAnimator; }	
	
	public override RectCentered CustomSize {get {return m_customSize;} set{m_customSize=value;}}
	
	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public override RectCentered GetRect(Transform excludeChild = null)
	{
		RectCentered result = RectCentered.zero;
		if ( m_sprite == null ) // this can be called from editor, so needs extra check
			m_sprite = GetComponentInChildren<SpriteRenderer>();
		if ( m_sprite != null )
		{
			result = GuiUtils.CalculateGuiRectFromSprite(transform, false, m_sprite, excludeChild);
			result.Transform(transform);
		}
		return result;
	}

	#endregion
	#region Funcs: Unity

	// Use this for initialization
	void Awake() 
	{	
		if ( m_sprite == null )
			m_sprite = GetComponentInChildren<SpriteRenderer>(true);
		if ( m_sprite != null )
			m_spriteAnimator = m_sprite.GetComponent<SpriteAnim>();
		
		ExAwake();
	}

	
	#endregion
	#region Partial Functions for extentions
	
	partial void ExAwake();
	//partial void ExOnDestroy();
	//partial void ExUpdate();


	#endregion
	#region Funcs: Private Internal
	/* NB: This was never used
	void OnSetVisible()
	{
		if ( gameObject.activeSelf == false && Visible)
			gameObject.SetActive(true);
		
		if ( GetSprite() )
			GetSprite().GetComponent<Renderer>().enabled = Visible;

		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		foreach( Renderer renderer in renderers )
		{   
			renderer.GetComponent<Renderer>().enabled = Visible;
		}
	}*/

	void OnAnimationChanged()
	{
		PlayAnimInternal(m_anim, true);
	}

	// Plays anim. Returns false if clip not found	
	bool PlayAnimInternal(string animName, bool fromStart = true)
	{
		m_stopOverrideAnimDelay = 0;
		
		if ( string.IsNullOrEmpty( animName ) || GuiComponent == null )
			return false;

		// Find anim in gui's list of anims
		AnimationClip clip = GetAnimation(animName);		
		if ( clip != null && FindSpriteAnimator() != null )
		{
			if ( fromStart || m_spriteAnimator.Clip == null  )
			{
				m_spriteAnimator.Play(clip);
			}
			else
			{
				float animTime = m_spriteAnimator.Time;
				m_spriteAnimator.Play(clip);
				m_spriteAnimator.Time = animTime;
			}
			return true;
		}
		else
		{
		
			Sprite sprite = GetSprite(animName);
			if ( sprite != null )
			{
				if ( m_spriteAnimator != null )
					m_spriteAnimator.Stop();
				m_sprite.sprite=sprite;
				return true;
			}
		}
		
		return false;
	}
	
	void UpdateOverrideAnim()
	{
		if ( m_overrideAnimPlaying  && PowerQuest.Get.GetSkippingCutscene() && m_spriteAnimator.GetCurrentAnimation().isLooping == false ) 
			StopAnimation();
	}

	void LateUpdateOverrideAnim()
	{
		// There's a delay before going back to original animation after an override, incase on the next line of script the animation is changed
		if ( m_stopOverrideAnimDelay > 0 )
		{
			m_stopOverrideAnimDelay--;
			if ( m_stopOverrideAnimDelay == 0 )
				PlayAnimInternal(m_anim);
		}

		// If animation has finished, return to default anim
		if ( m_overrideAnimPlaying && m_spriteAnimator.Playing == false )
		{
			if ( m_overrideAnimPlaying ) 
				m_stopOverrideAnimDelay = 1; // Set the delay
			m_overrideAnimPlaying = false;
		}
	}

	void PlayOverrideAnim(string animName)
	{
		if ( PlayAnimInternal(animName,true) == false && PowerQuest.Get.IsDebugBuild )
			Debug.LogWarning("Failed to find Gui Image animation: "+animName); // warn when trying to play anim
		m_overrideAnimPlaying = true;
	}

	bool GetAnimating()
	{
		return m_overrideAnimPlaying && m_spriteAnimator.Playing;
	}

	SpriteAnim FindSpriteAnimator()
	{ 
		if ( m_spriteAnimator != null )
			return m_spriteAnimator;
		if ( m_sprite != null )
			m_spriteAnimator = m_sprite.gameObject.AddComponent<SpriteAnim>();
		return m_spriteAnimator;		
	}
	

	// Handles setting up defaults incase items have been added or removed since last loading a save file
	/*[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}*/
	
	#endregion
	#region Funcs: Coroutines


	IEnumerator CoroutinePlayAnimation(string animName) 
	{
		PlayOverrideAnim(animName);
		while ( GetAnimating() && PowerQuest.Get.GetSkippingCutscene() == false )
		{				
			yield return new WaitForEndOfFrame();
		}
		if ( PowerQuest.Get.GetSkippingCutscene() )
		{
			SpriteAnim animComponent = FindSpriteAnimator();
			if ( animComponent != null )
			{
				// Skip to "end" of animation, and force update so that any animation changes are applied
				animComponent.NormalizedTime = 1;
				GetComponent<Animator>().Update(0);
			}

			StopAnimation();
		}
		yield break;
	}
	

	IEnumerator CoroutineWaitForAnimTrigger(string triggerName)
	{
		if ( PowerQuest.Get.GetSkippingCutscene() == false )
		{
			bool hit = false;
			AddAnimationTrigger(triggerName,true,()=>hit=true);
			yield return PowerQuest.Get.WaitUntil(()=> hit || GetSpriteAnimator().Playing == false );
		}
		yield break;
	}	
	
	#endregion	
	#region Funcs: IQuestClickable
	
	
	#endregion
	#region Funcs: Anim Events

	void AnimSound(Object obj)
	{
		if ( obj != null && (obj as GameObject) != null )
		{
			SystemAudio.Play((obj as GameObject).GetComponent<AudioCue>());
		}
	}

	void AnimSound(string sound)
	{
		SystemAudio.Play(sound);	    
	}
	
	void AnimSoundStop(Object obj)
	{
		if ( obj == null || (obj as GameObject) == null )
			return;
		SystemAudio.Stop((obj as GameObject).name,0.1f);
	}

	void AnimSoundStop(string sound)
	{
		SystemAudio.Stop(sound,0.1f);	
	}

	// Listen for QuestAnimTrigger tags
	QuestAnimationTriggers m_animTriggerComponent = null;
	void _Anim(string function)
	{
		if ( m_animTriggerComponent == null )
		{
			m_animTriggerComponent = transform.GetComponent<QuestAnimationTriggers>();
			if ( m_animTriggerComponent == null )
				m_animTriggerComponent = transform.gameObject.AddComponent<QuestAnimationTriggers>();
		}
	}

	#endregion
	
	}



}
