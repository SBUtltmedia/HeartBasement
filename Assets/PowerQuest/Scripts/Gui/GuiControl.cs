using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using PowerTools;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{


public partial class GuiControl : MonoBehaviour, IQuestClickable, IQuestScriptable, IGuiControl
{

	public enum eAlignHorizontal { None, Left, Center, Right }
	public enum eAlignVertical { None, Top, Middle, Bottom }

	#region Vars: Editor

	//	[Header("Size")]
	//	[SerializeField] Vector2 m_size = new Vector2(50,20);

	//	[Header("Mouse-over Defaults")]
	//	[TextArea]
	//	[SerializeField] string m_description = "New Button";
	//	[Tooltip("If set, changes the name of the cursor when moused over")]
	//	[SerializeField] string m_cursor = null;
	//[Header("Data")]
	[Range(-98,98)]
	[SerializeField] protected float m_baseline = 0;
	[SerializeField] protected bool m_visible = true;
	
	// NB: just going to use gameobject name for now
	//[HideInInspector, SerializeField] protected string m_scriptName = "";
	
	#endregion
	#region Vars: Private
	
	protected Gui m_gui = null;
	protected GuiComponent m_guiComponent = null;
	protected GuiNavigation m_navComponent = null;

	#endregion
	#region Funcs: For inheritors to implement
	
	// Things with images can overide this to allow image size controls
	public virtual RectCentered CustomSize {get {return RectCentered.zero;} set{} }
			
	// Called when the control should be forced to update it's alignment and fit
	public virtual void UpdateFitAndAlign()
	{
		// TODO: might want/need to check children objects too?

		// TODO: cache?

		// If what we're aligning to is a container, make sure that one's updated first
		if ( gameObject.activeInHierarchy ) //  Assume renderers aren't working when not active. But can still align.
		{
			FitToObject otherContain = GetComponentInChildren<FitToObject>(false); // Get this one in children, since images are often nested inside controls.
			if ( otherContain && otherContain.isActiveAndEnabled)
				otherContain.UpdateSize();
		}
			
		// If what we're aligning to is an AlignToObject, make sure that one's updated first. NB: this may be called on inactive object. (nice to be able to align to inactive objects
		AlignToObject otherAlign = GetComponent<AlignToObject>();
		if ( otherAlign && otherAlign.enabled )
			otherAlign.UpdatePos();

	}

	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public virtual RectCentered GetRect(Transform exclude = null)
	{
		// Brute force expensive! Should be cached... Tricky bit is working out when! Don't want to have complicated error prone "setDirty" flag
		RectCentered result = GuiUtils.CalculateGuiRectInternal(transform, true,null,null,exclude); // using internal version so we don't cause infinite loop
		result.Transform(transform);
		return result;
		//return new RectCentered(transform.position, transform.position);
	}

	// Called when mouse moved over active control (called from PowerQuest)
	public virtual void OnFocus() 
	{
		CallbackOnFocus?.Invoke();
	}

	// Called when focused control stops being focus (called from PowerQuest)
	public virtual void OnDefocus()
	{
		CallbackOnDefocus?.Invoke();
	}
	
	// Called when mouse moved over active control (called from PowerQuest)
	public virtual void OnKeyboardFocus() 
	{	
		CallbackOnKeyboardFocus?.Invoke();		
		PowerQuest.Get.ProcessGuiEvent(PowerQuest.SCRIPT_FUNCTION_ONKBFOCUS, GuiData, this);
	}

	// Called when focused control stops being focus (called from PowerQuest)
	public virtual void OnKeyboardDefocus()
	{
		CallbackOnKeyboardDefocus?.Invoke();
		PowerQuest.Get.ProcessGuiEvent(PowerQuest.SCRIPT_FUNCTION_ONKBDEFOCUS, GuiData, this);
	}

	// Call to have a control handle a keyboard input. Return true if the button was 'used'.
	public virtual bool HandleKeyboardInput(eGuiNav input)
	{
		return false;
	}
		
	/// Duplicates the control. Useful for adding elements dynamically to a gui. You can optionally specify a new name for the control, and a parent object (such as a grid container)
	public virtual GuiControl DuplicateControl(string name = null, IGuiControl parent = null)
	{
		GuiControl control = null;
		if ( parent != null )
			control = GameObject.Instantiate( gameObject, parent.Instance.transform ).GetComponent<GuiControl>();		
		else 
			control = GameObject.Instantiate( gameObject ).GetComponent<GuiControl>();		
		control.name = name == null ? ScriptName : name;
		control.SetGui(m_gui);		
		return control;
	}

	#endregion
	#region Funcs: Public


	protected virtual void Start()
	{	
		GuiComponent.EditorUpdateChildComponents(); // TEMP HACK- Should just check its in there itself
		GuiComponent.RegisterControl(this);
		Visible = m_visible;
		// update sort order
		UpdateBaseline();
		m_navComponent = GetComponent<PowerTools.Quest.GuiNavigation>();
		ExStart();
	}

	public virtual bool Visible 
	{ 
		get { return gameObject.activeSelf;} 
		set	
		{ 
			bool old = m_visible; 
			m_visible = value; 
			gameObject.SetActive(value); 

			if ( old != m_visible )
			{
				ExOnVisibilityChange(m_visible);
				if ( m_visible )
					ExOnShow();
				else
					ExOnHide();
			}
		}
	}
	public void Show() { Visible = true; }
	public void Hide() { Visible = false; }

	


	public void SetGui(Gui gui) { m_gui=gui;}
	public Gui GuiData => m_gui;
	public GuiComponent GuiComponent 
	{
		get 
		{  
			if (m_guiComponent == null)
				m_guiComponent = GetComponentInParent<GuiComponent>();	
			return m_guiComponent;
		}
	}	
	public GuiNavigation NavigationComponent => m_navComponent;	
	public AnimationClip GetAnimation(string animName) { return GuiComponent.GetAnimation(animName); }
	public List<AnimationClip> GetAnimations() { return GuiComponent.GetAnimations(); }
	public Sprite GetSprite(string name) { return GuiComponent.GetSprite(name); }
	public List<Sprite> GetSprites() { return GuiComponent.GetSprites(); }

	public bool Focused 
	{ 
		get 
		{
			return PowerQuest.Get.GetFocusedGuiControl() == (IGuiControl)this;
		}	
	}

	// Check for keyboard focus. Eg: Text Input controls that  capture kb. (different to navigation focus)
	public bool HasKeyboardFocus
	{
		// Simply sets state in PowerQuest, which will call back to the control with OnKbFocus or OnKbDefocus
		get
		{
			return PowerQuest.Get.GetKeyboardFocus() == this;
		}
		set
		{
			if ( value )
				PowerQuest.Get.SetKeyboardFocus(this);
			else if ( HasKeyboardFocus )
				PowerQuest.Get.SetKeyboardFocus(null);
		}
		
	}

	// Callback when control is focused (ie: mouse overs over it)
	public System.Action CallbackOnFocus = null;

	// Callback when control is un-focused (ie: mouse stops hovering over it)
	public System.Action CallbackOnDefocus = null;

	// Callback when control gains keyboard focus
	public System.Action CallbackOnKeyboardFocus = null;

	// Callback when control loses keyboard focus
	public System.Action CallbackOnKeyboardDefocus = null;

	// Gets all components under this Control, EXCLUDING any nested under other controls. This is for setting sort order of any sprites directly under a Control.
	List<T> GetThisControlsComponents<T>() where T : Component
	{
		List<T> list = new List<T>();
		GetControlsComponents(transform, list);
		return list;
	}
	
	static void GetControlsComponents<T>(Transform from, List<T> list) where T : Component
	{
		// Add own components
		list.AddRange(from.GetComponents<T>());
		
		// Add child components, unless you hit a GuiControl
		for (int i = 0; i < from.childCount; ++i )
		{
			Transform child = from.transform.GetChild(i);
			if ( child.GetComponent<GuiControl>() )
				continue;
			// Recurse
			GetControlsComponents(child, list);				
		}
	}

	// Updates baseline of self based on gui's baseline, and sets sprite render order
	public virtual void UpdateBaseline()
	{
		GuiComponent guiComponent = GuiComponent;
		if ( guiComponent == null )
			return;	
			
		// Gui baselines are multiplied by 100, and added to control baselines which are multiplied by 1. So each control should sort inside the gui. 		
		// Sort orders are clamped between +/-32000. So gui values of +/-320 are valid
		m_baseline = Mathf.Clamp(Baseline,-99,99);
		int sortOrder = -Mathf.RoundToInt((guiComponent.GetData().Baseline * 100.0f) + (m_baseline*1));
		
		// Just doing 1 sprite and text for now. if some end up being made of multiple, could add that later, and use z depth as another 
		{
			List<SpriteRenderer> renderers = GetThisControlsComponents<SpriteRenderer>();
			foreach( SpriteRenderer renderer in renderers )
				renderer.sortingOrder = sortOrder;
		}
		// Just doing 1 text for now. if some end up being made of multiple, could add that later, and use z depth as another 
		{
			QuestText renderer = GetComponentInChildren<QuestText>();
			if ( renderer != null )
				renderer.OrderInLayer = sortOrder;
		}
	}

	float m_alpha = 1;

	/// Note: setting alpha is unoptimised, since it finds all sprites/texts and applies individually
	public float Alpha
	{
		get { return m_alpha; }
		set 
		{
			m_alpha = value;
			
			SpriteRenderer[] sprites = Instance.GetComponentsInChildren<SpriteRenderer>(true);
			QuestText[] texts = Instance.GetComponentsInChildren<QuestText>(true);
	
			System.Array.ForEach( sprites, sprite => { if ( sprite != null ) sprite.color = sprite.color.WithAlpha( m_alpha ); });
			System.Array.ForEach( texts, text => { if ( text != null ) text.color = text.color.WithAlpha( m_alpha ); });
			
		}
	}
	
	/// Fade the sprite's alpha
	public Coroutine Fade(float start, float end, float duration, eEaseCurve curve = eEaseCurve.Smooth ) { return PowerQuest.Get.StartCoroutine(CoroutineFade(start, end, duration,curve)); }
	/// Fade the sprite's alpha (non-blocking)
	public void FadeBG(float start, float end, float duration, eEaseCurve curve = eEaseCurve.Smooth ) { PowerQuest.Get.StartCoroutine(CoroutineFade(start, end, duration,curve)); }
		
	protected IEnumerator CoroutineFade(float start, float end, float duration, eEaseCurve curve = eEaseCurve.Smooth  )
	{
		if ( Instance == null )
			yield break;

		SpriteRenderer[] sprites = Instance.GetComponentsInChildren<SpriteRenderer>(true);
		QuestText[] texts = Instance.GetComponentsInChildren<QuestText>(true);
		
		float time = 0;
		m_alpha = start;
		
		System.Action FadeSetAlpha = ()=>
		{		
			System.Array.ForEach( sprites, sprite => { if ( sprite != null ) sprite.color = sprite.color.WithAlpha( m_alpha ); });
			System.Array.ForEach( texts, text => { if ( text != null ) text.color = text.color.WithAlpha( m_alpha ); });
		};

		FadeSetAlpha();
		while ( time < duration ) //&& PowerQuest.Get.GetSkippingCutscene() == false )
		{
			yield return new WaitForEndOfFrame();
					
			//if ( SystemTime.Paused == false )
			time += Time.deltaTime;
			float ratio = time/duration;
			ratio = QuestUtils.Ease(ratio, curve);
			m_alpha = Mathf.Lerp(start,end, ratio);
			FadeSetAlpha();
		}

		m_alpha = end;
		FadeSetAlpha();

	}

	#endregion
	#region Unity Functions
	
	
	#endregion
	#region Partial Functions for extentions
	
	partial void ExStart();
	//partial void ExAwake();
	//partial void ExOnDestroy();
	//partial void ExUpdate();
	partial void ExOnShow();
	partial void ExOnHide();
	partial void ExOnVisibilityChange(bool visible);

	#endregion	
	#region Implementing IQuestClickable
	
	// TODO: check if there's alignto/fitto components, and handle appropriately
	public virtual void SetPosition(float x, float y) { Position = new Vector2(x,y); }
	public virtual Vector2 Position { get{ return transform.position;} set{ transform.position = value.WithZ(transform.position.z); } }
	public virtual float Baseline 
	{ 
		get{ return m_baseline;} 
		set
		{
			m_baseline = value;
			if ( Application.isPlaying )
				UpdateBaseline();
		} 
	}	
	public virtual bool Clickable { get { return false; }  set{} }
	public virtual string Description { get { return null; } set{} }
	public virtual string Cursor { get { return null; } set{} }
			
	public virtual string Text { get { return null; } set{} }
	public virtual string Anim { get { return null; } set{} }
	public virtual Color Color { get { return Color.white; } set{} }


	public eQuestClickableType ClickableType { get {return eQuestClickableType.Gui; } }
	public MonoBehaviour Instance { get{ return this; } }

	static readonly Regex REGEX_SANITIZE = new Regex(@"(\W|_)+", RegexOptions.Compiled);
	static readonly string REGEX_REPLACE = "";
	static readonly string STR_UNINSTNATIATED = "UninstantiatedGui";
	
	// Use a cleaned up version of the game objects's name as the script name. NB this is probably rather expensive, probably need to change it later
	public string ScriptName { get 
	{
		if ( gameObject == null )
			return STR_UNINSTNATIATED;
		return REGEX_SANITIZE.Replace(gameObject.name,REGEX_REPLACE);
	} }
	public Vector2 WalkToPoint { get { return Vector2.zero;} set{} }
	public Vector2 LookAtPoint { get { return Vector2.zero;} set{} }
	public virtual void OnInteraction( eQuestVerb verb ) {}
	public virtual void OnCancelInteraction( eQuestVerb verb ) {}

	// Return gui's script
	public QuestScript GetScript() { return GuiData?.GetScript(); } 
	public IQuestScriptable GetScriptable() { return this; }
	
	#endregion
	#region Implementing IQuestScriptable

	// Doesn't use all functions
	public string GetScriptName() { return ScriptName; }
	public string GetScriptClassName() { return ScriptName; }
	public void HotLoadScript(System.Reflection.Assembly assembly) { /*No-op*/ }
	
	public void EditorRename(string name)
	{
		//m_scriptName = name;
		gameObject.name = name;
	}

	void OnDrawGizmosSelected()
	{
		GuiComponent gui = GetComponentInParent<GuiComponent>();
		if ( gui == null )
			return;

		
	}

	#endregion
}


}
