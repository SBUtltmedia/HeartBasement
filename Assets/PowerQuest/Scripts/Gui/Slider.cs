using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

namespace PowerTools.QuestGui
{

//
[System.Serializable] 
[AddComponentMenu("Quest Gui/Slider")]
public partial class Slider : GuiControl, ISlider
{

	enum eElement { Bar,Handle }
	enum eState	{ Default, Hover, Click, Off }	
	enum eDirection { Horizontal, Vertical }
	
	/*public enum eSizeSetting
	{
		Custom,           // Set Size-> it gets set in hotspot. No change to image.  No stretch component
		ResizableImage, // Set Size-> it gets set in hotspot, and image is scaled to match (or w/h set).  No stretch component
		Image,	      // Hotspot size taken from image. No change to image. No stretch component
	}*/
	public enum eColorUse { None, Text, Image }

	#region Vars: Editor
	
	[Tooltip("Whether button can be clicked. When false, the button's anim/colour is set to the 'Inactive' one")]
	[SerializeField] bool m_clickable = true;
	[SerializeField] eDirection m_direction = eDirection.Horizontal;
	[SerializeField, HideInInspector] RectCentered m_customSize = RectCentered.zero;

	
	[Header("Mouse-over Defaults")]
	[TextArea(1,10)]
	[SerializeField] string m_description = "New Button";
	[Tooltip("If set, changes the name of the cursor when moused over")]
	[SerializeField] string m_cursor = null;
	
	[Header("Visuals")]
	[SerializeField] string m_barAnim = null;
	[SerializeField] string m_barAnimHover = null;
	[SerializeField] string m_barAnimClick = null;
	[SerializeField] string m_barAnimOff = null;
	
	[SerializeField] string m_handleAnim = null;
	[SerializeField] string m_handleAnimHover = null;
	[SerializeField] string m_handleAnimClick = null;
	[SerializeField] string m_handleAnimOff = null;
	
	[SerializeField] eColorUse m_colorWhat = eColorUse.Text;
	[SerializeField] Color m_color = new Color(0,0,0,1);	
	[SerializeField] Color m_colorHover = new Color(0,0,0,0);
	[SerializeField] Color m_colorClick = new Color(0,0,0,0);
	[SerializeField] Color m_colorOff = new Color(0,0,0,0);
	
	[Header("Audio")]
	[SerializeField] string m_soundHover = string.Empty;
	[SerializeField] string m_soundClick = string.Empty;
	[SerializeField] string m_soundSlide = string.Empty;

	[Header("Hotspot size")]
	[SerializeField] Padding m_hotspotPadding = Padding.zero;
	[SerializeField, Tooltip("Padding on the handle to stop it going too far of edges of hotspot")] Padding m_handlePadding = Padding.zero;
	//[SerializeField, HideInInspector] eSizeSetting m_sizeSetting = eSizeSetting.Image; 

	// Callback on mouse released: void OnClick(GuiControl slider)
	public System.Action<GuiControl> OnClick = null;
	// Callback on slider changed/dragged: void OnDrag(GuiControl slider)
	public System.Action<GuiControl> OnDrag = null;	

	[Header("Children")]
	[SerializeField]SpriteRenderer m_barSprite = null; 
	[SerializeField]SpriteRenderer m_handleSprite = null; 
	[SerializeField]QuestText m_questText = null;

	#endregion
	#region Vars: Private
	
	SpriteAnim m_barSpriteAnimator = null;
	SpriteAnim m_handleSpriteAnimator = null;
	BoxCollider2D m_bgBoxCollider2D = null;
	eState m_state = eState.Default;	
		
	float m_ratio = -1.0f;

	float m_keyboardIncrement = 0.1f;

	#endregion
	#region Funcs: IButton interface
	
	public IQuestClickable IClickable { get{ return this; } }
	
	public float Ratio
	{
		get
		{
			return m_ratio;
		}
		set
		{
			value = Mathf.Clamp01(value);
			if ( m_ratio != value )
			{
				m_ratio = value;
				
				RectCentered rect = m_customSize;
				rect.AddPadding(m_hotspotPadding);						
				rect.RemovePadding(m_handlePadding);
				if ( m_direction == eDirection.Horizontal )
				{				
					m_handleSprite.transform.localPosition = m_handleSprite.transform.localPosition.WithX(Utils.SnapRound(Mathf.Lerp(rect.MinX,rect.MaxX,m_ratio)));
				}
				else 
				{
					m_handleSprite.transform.localPosition = m_handleSprite.transform.localPosition.WithY(Utils.SnapRound(Mathf.Lerp(rect.MinY,rect.MaxY,m_ratio)));
				}

			}
		}
	}

	/// How much to move the slider when arrow keys are pressed (ratio from 0 to 1. Default is 0.1f)
	public float KeyboardIncrement { get { return m_keyboardIncrement; } set { m_keyboardIncrement = value; } }

	public override string Text 
	{
		get
		{
			if ( m_questText == null )
				m_questText = GetComponentInChildren<QuestText>();
			if ( m_questText != null )
				return m_questText.text;
			return string.Empty;
		}
		set
		{
			if ( m_questText == null )
				m_questText = GetComponentInChildren<QuestText>();
			if ( m_questText != null )
				m_questText.text = value;
		}
	}

	public override string Anim    { get { return m_barAnim;} set { m_barAnim = value; OnAnimationChanged(); } }
	public string AnimBar          { get { return m_barAnim;} set { m_barAnim = value; OnAnimationChanged(); } }
	public string AnimBarHover     { get { return m_barAnimHover;} set { m_barAnimHover = value; OnAnimationChanged(); } }
	public string AnimBarClick	   { get { return m_barAnimClick;} set { m_barAnimClick = value; OnAnimationChanged(); } }
	public string AnimBarOff	   { get { return m_barAnimOff;} set { m_barAnimOff = value; OnAnimationChanged(); } }
	
	public string AnimHandle       { get { return m_handleAnim;} set { m_handleAnim = value; OnAnimationChanged(); } }
	public string AnimHandleHover  { get { return m_handleAnimHover;} set { m_handleAnimHover = value; OnAnimationChanged(); } }
	public string AnimHandleClick  { get { return m_handleAnimClick;} set { m_handleAnimClick = value; OnAnimationChanged(); } }
	public string AnimHandleOff    { get { return m_handleAnimOff;} set { m_handleAnimOff = value; OnAnimationChanged(); } }
	
	public override Color Color { get{return m_color;}      set { m_color = value; OnColorChanged(); } }
	public Color ColorHover    { get{return m_colorHover;} set { m_colorHover = value; OnColorChanged(); } }
	public Color ColorClick    { get{return m_colorClick;} set { m_colorClick = value; OnColorChanged(); } }
	public Color ColorOff      { get{return m_colorOff;}   set { m_colorOff = value; OnColorChanged(); } }

	public eColorUse ColorWhat => m_colorWhat;

	#endregion
	#region Functions: Public (Non interface)
		
	public Padding HotspotPadding { get {return m_hotspotPadding; } set{m_hotspotPadding=value;} }
	public override RectCentered CustomSize {get {return m_customSize;} set{m_customSize=value;}}

	public void UpdateHotspot()
	{
		if ( m_bgBoxCollider2D == null )
		{
			m_bgBoxCollider2D = GetComponent<BoxCollider2D>();
			if ( m_bgBoxCollider2D == null )
			{
				Debug.LogWarning("Buttons need a BoxCollider2D to Auto-Scale their Hotspot");				
			}
		}
		if ( m_bgBoxCollider2D != null )
		{			
			InitComponentReferences();

			RectCentered bounds = GuiUtils.CalculateGuiRectInternal( transform, false, m_barSprite );
			bounds.AddPadding(m_hotspotPadding);
			if ( bounds.Center != m_bgBoxCollider2D.offset || m_bgBoxCollider2D.size != bounds.Size )
			{
				m_bgBoxCollider2D.offset=bounds.Center;
				m_bgBoxCollider2D.size=bounds.Size;
			}
		}
	}	
			
	public SpriteRenderer GetSprite() { return m_barSprite; }
	public SpriteAnim GetSpriteAnimator() { return m_barSpriteAnimator; }
	public SpriteRenderer GetHandleSprite() { return m_handleSprite; }
	public SpriteAnim GetHandleSpriteAnimator() { return m_handleSpriteAnimator; }
	public QuestText GetQuestText() { return m_questText; }
	
	public void EditorUpdateSprite()
	{
		if ( m_barSprite == null )
			m_barSprite = GetComponentInChildren<SpriteRenderer>();
		if ( m_barSprite != null )
		{			
			GuiComponent guiComponent = GetComponentInParent<GuiComponent>();				
			guiComponent.GetAnimation(m_barAnim);
		}
		
		if ( m_handleSprite == null )
			m_handleSprite = GetComponentInChildren<SpriteRenderer>();
		if ( m_handleSprite != null )
		{			
			GuiComponent guiComponent = GetComponentInParent<GuiComponent>();				
			guiComponent.GetAnimation(m_handleAnim);
		}
	}
		
	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public override RectCentered GetRect(Transform excludeChild = null)
	{		
		InitComponentReferences();
					
		if( m_barSprite != null )
		{
			RectCentered result = GuiUtils.CalculateGuiRectInternal(transform, false, m_barSprite, null, excludeChild);
			result.Transform(transform);
			return result;
		}
		return RectCentered.zero;
	}

	// Call to have a control handle a keyboard input. Return true if the button was 'used'.
	public override bool HandleKeyboardInput(eGuiNav input)
	{
		
		if ( m_direction == eDirection.Horizontal && input != eGuiNav.Left && input != eGuiNav.Right )
			return false;
		if ( m_direction == eDirection.Vertical && input != eGuiNav.Up && input != eGuiNav.Down )
			return false;

		float newRatio = m_ratio;
		if ( input == eGuiNav.Left || input == eGuiNav.Down )			
			newRatio -= m_keyboardIncrement;
		else 
			newRatio += m_keyboardIncrement;
			
		// Set the ratio- this updates the visuals and calls through to script functions
		if ( m_ratio != newRatio )
		{
			
			if ( IsString.Valid(m_soundSlide) )
				SystemAudio.Play(m_soundSlide);	

			// update the ratio, also sets the visuals
			Ratio = newRatio;

			// Call script 'onDrag' functions
			PowerQuest.Get.ProcessGuiEvent(PowerQuest.SCRIPT_FUNCTION_DRAGGUI, GuiData, this);

			// Also send a message upwards for gui components to use
			SendMessageUpwards(PowerQuest.SCRIPT_FUNCTION_DRAGGUI+ScriptName, this, SendMessageOptions.DontRequireReceiver );
			OnDrag?.Invoke(this);
			
			PowerQuest.Get.ProcessGuiClick(GuiData, this);
			// Also send a message upwards for gui components to use
			SendMessageUpwards(PowerQuest.SCRIPT_FUNCTION_CLICKGUI+ScriptName, this, SendMessageOptions.DontRequireReceiver );
			OnClick?.Invoke(this);
		}
		return true;
	}
	
	#endregion
	#region Component: Functions: Unity
	
	// Use this for initialization
	void Awake() 
	{	
		InitComponentReferences();
		ExAwake();
	}

	void InitComponentReferences()
	{
		//if ( m_barSprite == null )
		//	m_barSprite = GetComponentInChildren<SpriteRenderer>(true);
		if ( m_barSprite != null && m_barSpriteAnimator == null )
			m_barSpriteAnimator = m_barSprite.GetComponent<SpriteAnim>();
		//if ( m_handleSprite == null )
		//	m_handleSprite = GetComponentInChildren<SpriteRenderer>(true);
		
		if ( m_handleSprite != null && m_handleSpriteAnimator == null )
			m_handleSpriteAnimator = m_handleSprite.GetComponent<SpriteAnim>();
		if ( m_questText == null )
			m_questText = GetComponentInChildren<QuestText>();
	}

	
	protected override void Start()
	{
		base.Start();
		InitComponentReferences();
			
		// Set state if clickable/not clickable			
		SetState(Clickable ? eState.Default : eState.Off);

		StartStateAnimation();

		if ( Ratio < 0 )
			Ratio = 0;

		UpdateHotspot();
		
	}	

	void Update()
	{

		// Set state if clickable/not clickable
		if ( (m_state != eState.Off) != Clickable )			
			SetState(Clickable ? eState.Default : eState.Off);

		switch ( m_state )
		{
			case eState.Default:
			{
				if ( Focused )
					SetState(eState.Hover);
			} break;
			case eState.Hover:
			{
				// check for click
				if ( Focused == false )
					SetState(eState.Default);				
				else if ( Input.GetMouseButtonDown(0) )
					SetState(eState.Click);				

			} break;
			case eState.Click:
			{
				if ( Input.GetMouseButton(0) )
				{
					float newRatio = 0;
					RectCentered rect = m_customSize;
					rect.AddPadding(m_hotspotPadding);	
					if ( m_direction == eDirection.Horizontal )					
						newRatio = Mathf.InverseLerp( rect.MinX, rect.MaxX, PowerQuest.Get.GetMousePositionGui().x-transform.position.x );
					else
						newRatio = Mathf.InverseLerp( rect.MinY, rect.MaxY, PowerQuest.Get.GetMousePositionGui().y-transform.position.y );

					// Set the ratio- this updates the visuals and calls through to script functions
					if ( m_ratio != newRatio )
					{
						// update the ratio, also sets the visuals
						Ratio = newRatio;

						// Call script 'onDrag' functions
						PowerQuest.Get.ProcessGuiEvent(PowerQuest.SCRIPT_FUNCTION_DRAGGUI, GuiData, this);

						// Also send a message upwards for gui components to use
						SendMessageUpwards(PowerQuest.SCRIPT_FUNCTION_DRAGGUI+ScriptName, this, SendMessageOptions.DontRequireReceiver );
						OnDrag?.Invoke(this);
					}
				}
				else
				{
					// Mouse button lifted. call "click" function- Note, this isn't using the "focused" thing... since mouse may have moved off the slider while sliding it...
					//if ( Focused )
					{
						PowerQuest.Get.ProcessGuiClick(GuiData, this);
						// Also send a message upwards for gui components to use
						SendMessageUpwards(PowerQuest.SCRIPT_FUNCTION_CLICKGUI+ScriptName, this, SendMessageOptions.DontRequireReceiver );
						OnClick?.Invoke(this);
					}
					if ( Focused )
						SetState(eState.Hover);
					else
						SetState(eState.Default);					
				}

			} break;
		}

		if ( Input.GetMouseButtonDown(1) && Focused )
		{
			// Handle right clicking when focused
			PowerQuest.Get.ProcessGuiClick(GuiData, this);
		}

		ExUpdate();
	}

	void SetState(eState newState)
	{
		if ( m_state != newState )
		{
			if ( newState == eState.Hover && IsString.Valid(m_soundHover) )
				SystemAudio.Play(m_soundHover);
			if ( newState == eState.Click && IsString.Valid(m_soundClick) )
				SystemAudio.Play(m_soundClick);	
		}

		ExOnSetState(newState);

		m_state = newState;
		UpdateColor();
		StartStateAnimation();		
	}
	
	#endregion
	#region Funcs: Private Internal
		

	void OnColorChanged()
	{
		UpdateColor();
	}
	void OnAnimationChanged()
	{
		StartStateAnimation();
	}	

	void UpdateColor()
	{	
		Color color = m_color;		
		switch ( m_state )
		{
			case eState.Hover: color = m_colorHover; break;
			case eState.Click: color = m_colorClick; break;
			case eState.Off: color = m_colorOff; break;
		}			
		if ( m_questText != null && m_colorWhat == eColorUse.Text )
			m_questText.color = color;
		else if ( m_barSprite != null && m_colorWhat == eColorUse.Image )
			m_barSprite.color = color;
	}


	// 
	void StartStateAnimation()
	{
		// Called when default animation changes

		// Try finding animation based on state- 		
		{
			eElement element = eElement.Bar;
			bool foundAnim = false;
			switch( m_state )
			{
				case eState.Hover: foundAnim = PlayAnimInternal(element, m_barAnimHover); break;
				case eState.Click:
				{
					foundAnim = PlayAnimInternal(element, m_barAnimClick);
					if ( foundAnim == false )
						foundAnim = PlayAnimInternal(element, m_barAnimHover);
				} break;	
				case eState.Off: foundAnim = PlayAnimInternal(element, m_barAnimOff); break;
			}
			if ( foundAnim == false )
				PlayAnimInternal(element, m_barAnim);		
		}
		{
			eElement element = eElement.Handle;
			bool foundAnim = false;
			switch( m_state )
			{
				case eState.Hover: foundAnim = PlayAnimInternal(element, m_handleAnimHover); break;
				case eState.Click:
				{
					foundAnim = PlayAnimInternal(element, m_handleAnimClick);
					if ( foundAnim == false )
						foundAnim = PlayAnimInternal(element, m_handleAnimHover);
				} break;	
				case eState.Off: foundAnim = PlayAnimInternal(element, m_handleAnimOff); break;
			}
			if ( foundAnim == false )
				PlayAnimInternal(element, m_handleAnim);		
		}
	}
	
	// Plays anim. Returns false if clip not found	
	bool PlayAnimInternal(eElement element, string animName, bool fromStart = true)
	{		
		SpriteAnim animator = element == eElement.Bar ? m_barSpriteAnimator : m_handleSpriteAnimator;
		SpriteRenderer spriteRenderer = element == eElement.Bar ? m_barSprite : m_handleSprite;
				
		if ( string.IsNullOrEmpty( animName ) )
			return false;

		// Find anim in gui's list of anims
		AnimationClip clip = GetAnimation(animName);
		if ( clip != null && animator != null )
		{
			if ( fromStart || animator.Clip == null  )
			{
				animator.Play(clip);
			}
			else
			{
				float animTime = animator.Time;
				animator.Play(clip);
				animator.Time = animTime;
			}
			return true;
		}
		else
		{
		
			Sprite sprite = GetSprite(animName);
			if ( sprite != null )
			{
				animator.Stop();
				spriteRenderer.sprite=sprite;
				return true;
			}
		}
		
		return false;
	}
	
	
	#endregion
	#region Partial Functions for extentions
	
	partial void ExAwake();
	//partial void ExOnDestroy();
	partial void ExUpdate();
	partial void ExOnSetState(eState newState);

	#endregion	
	#region Implementing IQuestClickable
	
	public override string Description { get{ return m_description;} set{m_description = value;} }
	public override bool Clickable { get{ return m_clickable;} set{ m_clickable = value; } }
	public override string Cursor { get { return m_cursor; } set { m_cursor = value; }  }
	
	#endregion
	#region Funcs: Coroutines
	
	
	#endregion
	#region Funcs: Private Internal

	#endregion
}



}
