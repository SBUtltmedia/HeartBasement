using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

namespace PowerTools.QuestGui
{

//
[System.Serializable] 
[AddComponentMenu("Quest Gui/Text Field")]
public partial class TextField : GuiControl, ITextField
{

	enum eState	{ Default, Hover, Click, Edit, Off }
	
	#region Vars: Editor
	
	[Tooltip("Whether button can be clicked. When false, the button's anim/colour is set to the 'Inactive' one")]
	[SerializeField] bool m_clickable = true;
	
	[Header("Mouse-over Defaults")]
	[TextArea(1,10)]
	[SerializeField] string m_description = "New Button";
	[Tooltip("If set, changes the name of the cursor when moused over")]
	[SerializeField] string m_cursor = null;

	[Header("Text Field Options")]
	[SerializeField] bool m_requireKeyboardFocus = true;
	[SerializeField] int m_maxCharacters = -1;
	[SerializeField] string m_caretCharacter = "_";
	[SerializeField] float m_caretBlinkRate = 0.4f; 

	[Header("Visuals")]
	[SerializeField] string m_anim = null;
	[SerializeField] string m_animHover = null;
	[SerializeField] string m_animClick = null;
	[SerializeField] string m_animEdit = null;
	[SerializeField] string m_animOff = null;
	
	[SerializeField] Color m_color = new Color(0,0,0,1);	
	[SerializeField] Color m_colorHover = new Color(0,0,0,0);
	[SerializeField] Color m_colorClick = new Color(0,0,0,0);	
	[SerializeField] Color m_colorEdit = new Color(0,0,0,0);
	[SerializeField] Color m_colorOff = new Color(0,0,0,0);


	[Header("Hotspot size")]
	[SerializeField] Padding m_hotspotPadding = Padding.zero;	
		
	[SerializeField, HideInInspector] RectCentered m_customSize = RectCentered.zero;
	[SerializeField, HideInInspector] Vector2 m_textPadding = Vector2.zero;
	
	// Callback on click: void OnClick(GuiControl button)
	public System.Action<GuiControl> OnClick = null;
	public System.Action<GuiControl> OnTextChange = null;
	public System.Action<GuiControl> OnTextConfirm = null;


	#endregion
	#region Vars: Private
	
	SpriteRenderer m_sprite = null; 
	SpriteAnim m_spriteAnimator = null;
	QuestText m_questText = null;
	BoxCollider2D m_boxCollider2D = null;
	eState m_state = eState.Default;	
	
	TextEditor m_textEditor = new TextEditor();
	
	#endregion
	#region Funcs: IButton interface
	
	public IQuestClickable IClickable { get{ return this; } }
	
	public override string Text 
	{
		get
		{
			// Should by default, return the text editor text, not what's displayed (which includes caret character)
			if ( m_textEditor != null )
				return m_textEditor.text;

			if ( m_questText == null )
				m_questText = GetComponentInChildren<QuestText>();
			if ( m_questText != null )
				return m_questText.text;
			return string.Empty;
		}
		set
		{
			if ( m_textEditor != null )
			{
				m_textEditor.text = value;				
				m_textEditor.cursorIndex = m_textEditor.text.Length;
				m_textEditor.selectIndex = m_textEditor.cursorIndex;
			}
			if ( m_questText == null )
				m_questText = GetComponentInChildren<QuestText>();

			if ( m_questText != null )
			{
				if ( HasKeyboardFocus )				
					UpdateVisualText(m_textEditor.text,true);
				else 				
					UpdateVisualText(m_textEditor.text,false);
			}
		}
	}

	public override string Anim{ get { return m_anim;} set { m_anim = value; OnAnimationChanged(); } }
	public string AnimHover	   { get { return m_animHover;} set { m_animHover = value; OnAnimationChanged(); } }
	public string AnimClick	   { get { return m_animClick;} set { m_animClick = value; OnAnimationChanged(); } }
	public string AnimEdit	   { get { return m_animEdit;} set { m_animEdit = value; OnAnimationChanged(); } }
	public string AnimOff      { get { return m_animOff;} set { m_animOff = value; OnAnimationChanged(); } }
	
	public override Color Color{ get{return m_color;} set { m_color = value; OnColorChanged(); } }
	public Color ColorHover    { get{return m_colorHover;} set { m_colorHover = value; OnColorChanged(); } }
	public Color ColorClick    { get{return m_colorClick;} set { m_colorClick = value; OnColorChanged(); } }
	public Color ColorEdit     { get{return m_colorEdit;} set { m_colorEdit = value; OnColorChanged(); } }
	public Color ColorOff      { get{return m_colorOff;} set { m_colorOff = value; OnColorChanged(); } }
	 
	
	public void FocusKeyboard()
	{
		HasKeyboardFocus = true;
	}
		
	#endregion
	#region Functions: Public (Non interface)
	
	// Called from PowerQuest when keyboard focus changes
	override public void OnKeyboardFocus()
	{
		base.OnKeyboardFocus();
		StartEditingText();

	}
	
	// Called from PowerQuest when keyboard focus changes
	override public void OnKeyboardDefocus()
	{
		base.OnKeyboardDefocus();		
		StopEditingText();
	}


	void StartEditingText()
	{
		m_textEditor.OnFocus();
		m_textEditor.text = m_questText.text;		
		UpdateVisualText(m_textEditor.text,true);
		m_textEditor.cursorIndex = m_textEditor.text.Length;
		m_textEditor.selectIndex = m_textEditor.cursorIndex;
		SetState(eState.Edit);
	}
	void StopEditingText()
	{
		UpdateVisualText(m_textEditor.text,false);
		m_textEditor.OnLostFocus();
		SetState(eState.Default);
	}


	
	public Padding HotspotPadding { get {return m_hotspotPadding; } set{m_hotspotPadding=value;} }
	public override RectCentered CustomSize {get {return m_customSize;} set{m_customSize=value;}}
	public Vector2 TextPadding {get {return m_textPadding;} set{m_textPadding=value;}}

	public void UpdateHotspot()
	{
		if ( m_boxCollider2D == null )
		{
			m_boxCollider2D = GetComponent<BoxCollider2D>();
			if ( m_boxCollider2D == null )
				Debug.LogWarning("Text fields need a BoxCollider2D to Auto-Scale their Hotspot");
		}
		if ( m_boxCollider2D != null )
		{			
			InitComponentReferences();

			RectCentered bounds = GuiUtils.CalculateGuiRectInternal( transform, false, m_sprite, m_questText == null ? null : m_questText.GetComponent<MeshRenderer>() );
			bounds.AddPadding(m_hotspotPadding);
			if ( bounds.Center != m_boxCollider2D.offset || m_boxCollider2D.size != bounds.Size )
			{
				m_boxCollider2D.offset=bounds.Center;
				m_boxCollider2D.size=bounds.Size;
			}
		}
	}	
			
	public SpriteRenderer GetSprite() { return m_sprite; }
	public SpriteAnim GetSpriteAnimator() { return m_spriteAnimator; }
	public QuestText GetQuestText() { return m_questText; }
	
	public void EditorUpdateSprite()
	{
		if ( m_sprite == null )
			m_sprite = GetComponentInChildren<SpriteRenderer>(true);
		if ( m_sprite != null )
		{			
			GuiComponent guiComponent = GetComponentInParent<GuiComponent>();				
			guiComponent.GetAnimation(m_anim);
		}
		// TODO: this doesn't do anything yet!
	}
		
	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public override RectCentered GetRect(Transform excludeChild = null)
	{		
		InitComponentReferences();
		MeshRenderer textRenderer = null;
		if ( m_questText != null )
			textRenderer = m_questText.GetComponent<MeshRenderer>();
					
		if( m_sprite != null || textRenderer != null )
		{
			RectCentered result = GuiUtils.CalculateGuiRectInternal(transform, false, m_sprite, textRenderer, excludeChild);
			result.Transform(transform);
			return result;
		}
		return RectCentered.zero;
	}
	
	#endregion
	#region Component: Functions: Unity
	

	void InitComponentReferences()
	{
		if ( m_sprite == null )
			m_sprite = GetComponentInChildren<SpriteRenderer>(true);
		if ( m_sprite != null && m_spriteAnimator == null )
			m_spriteAnimator = m_sprite.GetComponent<SpriteAnim>();
		if ( m_questText == null )
			m_questText = GetComponentInChildren<QuestText>();
	}

	// Use this for initialization
	void Awake() 
	{
		InitComponentReferences();
		ExAwake();

		// Set state if clickable/not clickable.
		// NB: set this in Awake, since if color is changed by fading on show, it'll be overriden by this state change
		SetState(Clickable ? eState.Default : eState.Off);

		m_textEditor.text = m_questText.text;
	}
	
	protected override void Start()
	{
		base.Start();   
		InitComponentReferences();

		StartStateAnimation();

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
				if ( m_requireKeyboardFocus == false )
				{
					if ( GuiData.ObscuredByModal == false )					
						StartEditingText();
				}
				else if ( Focused )
				{
					SetState(eState.Hover);
				}
			} break;
			case eState.Hover:
			{
				// check for click
				if ( Focused == false )
				{
					SetState(eState.Default);				
				}
				else if ( Input.GetMouseButtonDown(0) )
				{
					SetState(eState.Click);
				}
			} break;
			case eState.Click:
			{
				if ( Input.GetMouseButton(0) == false )
				{
					if ( Focused )
					{
						// Set keyboard state- this sets state
						FocusKeyboard();

						PowerQuest.Get.ProcessGuiClick(GuiData, this);
						// Also send a message upwards for gui components to use
						SendMessageUpwards(PowerQuest.SCRIPT_FUNCTION_CLICKGUI+ScriptName, this, SendMessageOptions.DontRequireReceiver );
						if ( OnClick != null )
							OnClick.Invoke(this);
							
					}
					//SetState(eState.Default);
				}

			} break;

			case eState.Edit:
			{
				bool textChanged = false;
				if ( m_questText != null && m_textEditor != null )
				{
					foreach ( char c in Input.inputString )
					{
						if ( c == '\b' ) // Backspace
						{
							m_textEditor.Backspace();
							textChanged = true;
						}
						else if ( c == '\n' || c == '\r' ) // return
						{
							// Trigger event on enter														
							if ( m_requireKeyboardFocus )
							{
								PowerQuest.Get.ProcessGuiEvent(PowerQuest.SCRIPT_FUNCTION_ONTEXTCONFIRM+ScriptName, GuiData, this);
								SendMessageUpwards(PowerQuest.SCRIPT_FUNCTION_ONTEXTCONFIRM+ScriptName, this, SendMessageOptions.DontRequireReceiver );
								// Defocus
								HasKeyboardFocus = false;
							}
						}
						else 
						{
							if ( m_maxCharacters <= 0 || m_textEditor.text.Length < m_maxCharacters )
							{
								m_textEditor.Insert(c);
								textChanged = true;
							}
						}
					}
					

					if ( textChanged || Utils.GetTimeIncrementPassed(m_caretBlinkRate) )
					{
						UpdateVisualText(m_textEditor.text,true);

						if ( textChanged )
							PowerQuest.Get.ProcessGuiEvent(PowerQuest.SCRIPT_FUNCTION_ONTEXTEDIT+ScriptName, GuiData, this);					
					}

					if ( m_requireKeyboardFocus )
					{				
						if ( Input.GetKeyDown(KeyCode.Escape) || (Input.GetMouseButtonDown(0) && Focused == false) )
						{
							// Defocus on escape or click outside area. This might want to be an option
							HasKeyboardFocus = false;
						}
					}
					else 
					{
						// Don't require keybaord focus, just that gui is not obscured
						if ( GuiData.ObscuredByModal )
							StopEditingText();
					}

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

	// Set the displayed text in the QuestText component, with width limited by field size
	void UpdateVisualText(string newText, bool showCaret)
	{	
		// Ensure text fits in box
		if ( CustomSize.Width > 0 )
		{						
			float width = CustomSize.Width - (TextPadding.x*2.0f);
			// width doesn't include padding

			TextWrapper wrapper = new TextWrapper(m_questText.GetComponent<TextMesh>());
			while ( wrapper.GetTextWidth(newText+(showCaret?m_caretCharacter:"")) > width && newText.Length > 0)
					newText = newText.Substring(1);
		}

		if (showCaret && (m_caretBlinkRate <= 0 || (Time.timeSinceLevelLoad % (m_caretBlinkRate*2))>m_caretBlinkRate) )
			newText += m_caretCharacter;

		m_questText.text = newText;
	}
	
	void SetState(eState newState)
	{
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
			case eState.Edit: color = m_colorEdit; break;
			case eState.Off: color = m_colorOff; break;
		}			
		if ( m_questText != null )//&& (m_colorWhat == eColorUse.Text || m_colorWhat == eColorUse.Both) )
			m_questText.color = color;
		//if ( m_sprite != null && (m_colorWhat == eColorUse.Image || m_colorWhat == eColorUse.Both) )
		//	m_sprite.color = color;
	}

	// 
	void StartStateAnimation()
	{
		// Try finding animation based on state- 
		 
		bool foundAnim = false;
		switch( m_state )
		{
			case eState.Hover: foundAnim = PlayAnimInternal(m_animHover); break;
			case eState.Click:
			{
				foundAnim = PlayAnimInternal(m_animClick);
				if ( foundAnim == false )
					foundAnim = PlayAnimInternal(m_animHover);
			} break;	
			case eState.Edit:
			{
				foundAnim = PlayAnimInternal(m_animEdit);
				if ( foundAnim == false )
					foundAnim = PlayAnimInternal(m_animHover);
			} break;	
			case eState.Off: foundAnim = PlayAnimInternal(m_animOff); break;
		}
		if ( foundAnim == false )
			PlayAnimInternal(m_anim);		
	}
	
	// Plays anim. Returns false if clip not found	
	bool PlayAnimInternal(string animName, bool fromStart = true)
	{
		if ( m_spriteAnimator == null )
			return true;

		
		if ( string.IsNullOrEmpty( animName ) )
			return false;

		// Find anim in gui's list of anims
		AnimationClip clip = GetAnimation(animName);
		if ( clip != null && m_spriteAnimator != null )
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
				m_spriteAnimator.Stop();
				m_sprite.sprite=sprite;
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
	
	#endregion	
	#region Implementing IQuestClickable
	
	public override string Description { get{ return m_description;} set{m_description = value;} }
	public override bool Clickable { get{ return m_clickable;} set{ m_clickable = value; } }
	public override string Cursor { get { return m_cursor; } set { m_cursor = value; }  }
	
	#endregion
	#region Funcs: Coroutines
		
	
	#endregion
	#region Funcs: Private Internal
	

	// Handles setting up defaults incase items have been added or removed since last loading a save file
	/*[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}*/
	
	#endregion
	#region Functions: Anim Events

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
		
	// Listen for QuestAnimTrigger tags so can pass them up
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
