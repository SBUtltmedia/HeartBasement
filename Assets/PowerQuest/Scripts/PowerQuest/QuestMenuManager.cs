//#define DEBUG_FADE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;


namespace PowerTools.Quest
{


/** Quest Menu Manager- This is where menu stuff was originally going to be managed. 
	At the moment it only does a couple of things:
	- Fading in and out (and not particularly well)
	- Using GUI baselines to control their sort order
	- Controlling GUI Keyboard input for navigation
	
	I imagine this will be refactored when I get to doing better gui/menu management
*/
[System.Serializable]
public class QuestMenuManager
{
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Definitions

	static readonly float KB_REPEAT_TIME_FIRST = 0.4f; // how long before holding kb navigation before it'll repeat
	static readonly float KB_REPEAT_TIME_REPEAT = 0.12f; // how fast held kb navigation repeats

	public static readonly string DEFAULT_FADE_SOURCE = "";

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Editor variables

	[SerializeField] SpriteRenderer m_prefabEffectMenuFadeOut = null;
	[SerializeField] Color m_fadeColour = Color.black;
	[Tooltip("If using custom fade, poll GetFadeRatio() and do your own fading")]
	[SerializeField] bool m_customFade = false;
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Private vars

	//
	// Fade variables 
	//


	// list of things causing a fade in or out. Call fadeIn/FadeOut with a different source if you don't want code somewhere else to conflict with your fading
	SourceList m_fadeSources = new SourceList();

	float m_fadeInTime = 0;
	float m_fadeOutTime = 0;
	float m_fadeAlpha = 0;
	SpriteRenderer m_sprite = null;
	Color m_fadeColourDefault = Color.black;		
	
	//
	// Gui keyboard variables
	//	
	bool m_kbActive = false; // True when plr is using keyboard rather than mouse
	bool m_kbFocus = false; // True when keyboard has taken focus on a gui
	BitMask m_kbPrevState = new BitMask(); // Mask of current gui keyboard inputs
	BitMask m_kbState = new BitMask(); // Mask of current gui keyboard inputs
	BitMask m_kbStateDown = new BitMask(); // Mask of current gui keyboard inputs
	float m_kbRepeatTimer = KB_REPEAT_TIME_FIRST; // Timer used for repeating gui keyboard L/R/U/D events
	bool m_kbWaitForRelease = false; // Whether to wait for next input before processing next input (used when )
	Vector2 m_cachedMousePos = Vector2.zero;

	private List<Gui> m_sortedGuis = new List<Gui>();

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Public functions

	// Static getter
	public static QuestMenuManager Get { get => PowerQuest.Get.GetMenuManager(); }

	// Callback for implementing custom fades
	public System.Action CallbackOnUpdateFade = null;

	public void Awake()
	{
		// init deafult fadecolor. This is a bit round-about to preserve back-compatibility
		m_fadeColourDefault = m_fadeColour;
	}
	public bool KeyboardActive => m_kbActive;

	public Color FadeColor { get { return m_fadeColour;} set{ m_fadeColour = value; UpdateFadeSprite(); } }
	public Color FadeColorDefault { get { return m_fadeColourDefault;} set{ m_fadeColourDefault = value; UpdateFadeSprite(); } }
	public void FadeColorRestore() { m_fadeColour = m_fadeColourDefault; /* UpdateFadeSprite();*/ } 

	public bool GetFading() 
	{
		return (m_fadeSources.Empty() == false && m_fadeAlpha < 1) || (m_fadeSources.Empty()  && m_fadeAlpha > 0);
	}
	public float GetFadeRatio()	{ return m_fadeAlpha; }
	public Color GetFadeColor()	{ return m_fadeColour; }
	public bool GetCustomFade() { return m_customFade; }

	// Resets all fade data (game will be faded in). Used when restoring a game, etc.
	public void ResetFade()
	{
		m_fadeInTime = 0;
		m_fadeOutTime = 0;
		m_fadeAlpha = 0;
		m_fadeSources.Clear();
		// Coroutines should be stopped along with this function being called.
	}
	
	//
	// Fade out (for main menu)
	//
	public void FadeOut(float time) { FadeOut(time,DEFAULT_FADE_SOURCE); }
	public void FadeOut(float time, string source)
	{
		#if DEBUG_FADE
		Debug.Log("FadeOut: " + source+": "+time);
		#endif
		m_fadeOutTime = time;
		if (m_fadeSources.Contains(source) == false )		
			m_fadeSources.Add(source);
		if ( m_fadeOutTime == 0 )
		{
			m_fadeAlpha = 1;
			UpdateFadeSprite();
		}
	}
	public void FadeSkip()
	{		
		#if DEBUG_FADE
		Debug.Log("FadeSkip: "+ (m_fadeSources.Empty() ? "In" : "Out"));
		#endif
		m_fadeAlpha = ( m_fadeSources.Empty() ) ? 0 : 1;
		UpdateFadeSprite();
	}
	public void FadeIn(float time) { FadeIn(time,DEFAULT_FADE_SOURCE); }
	public void FadeIn(float time, string source)
	{
		
		#if DEBUG_FADE
		Debug.Log("FadeIn: " + source+": "+time);
		#endif
		m_fadeInTime = time;
		m_fadeSources.Remove(source);
		if ( m_fadeInTime == 0 && m_fadeSources.Empty() )
		{
			m_fadeAlpha = 0;
			FadeColorRestore(); // After fading back in fully, restore the fade color. Otherwise it leads to lots of bugs where you forget to change it back.
			UpdateFadeSprite();
		}
	}
		
	// Update is called once per frame
	public void Update() 
	{
		// Fade Out
		if ( m_fadeSources.Empty() == false && m_fadeAlpha < 1.0f )
		{
			m_fadeAlpha += Time.deltaTime / m_fadeOutTime;
			if ( m_fadeAlpha > 1.0f )
				m_fadeAlpha = 1.0f;
			UpdateFadeSprite();
		}

		// fade in
		if( m_fadeSources.Empty() && m_fadeAlpha > 0.0f )
		{
			m_fadeAlpha -= Time.deltaTime / m_fadeInTime;
			if ( m_fadeAlpha <= 0.0f )
			{
				m_fadeAlpha = 0.0f;
				FadeColorRestore(); // After fading back in fully, restore the fade color. Leads to lots of bugs where you forget to change it back otherwise.
			}			
			UpdateFadeSprite();
		}


		// Update Gui sort order based on "baseline"
		m_sortedGuis.Clear();
		m_sortedGuis.AddRange(PowerQuest.Get.GetGuis());
		// Sort guis by baseline
		m_sortedGuis.Sort( (a,b)=>
			{ 
				int result = b.Baseline.CompareTo(a.Baseline);
				if ( result == 0 && a.Instance != null && b.Instance != null )
					result = a.Instance.GetInstanceID().CompareTo(b.Instance.GetInstanceID());
				return result;
			});
		int guiIndex = 0;
		foreach ( Gui gui in m_sortedGuis )
		{
			if ( gui.Instance != null )
				gui.Instance.transform.SetSiblingIndex(guiIndex++);			
		}

		UpdateKb();
	}

	//
	// Menu keyboard handline
	//
	
	// Call to ignore the same keypress being used
	public void IgnoreNextKeypress() { m_kbWaitForRelease = true; }

	public bool KeyboardInputValid => m_kbWaitForRelease = false;
	
	// Called when gui input is pressed- pass it through to gui system and record presses for queries.
	public bool NavigateGui(eGuiNav input = eGuiNav.Ok)
	{
		if ( PowerQuest.Get.GetBlocked())
			return false;
		
		// update keyboard input
		if ( OnKeyboardInput(input) )
		{ 
			bool handled = false;
			if ( PowerQuest.Get.GetFocusedGui() != null )
				handled = PowerQuest.Get.GetFocusedGui().Navigate(input);
			
			// if gui didn't consume, cache input for others
			if ( handled == false )
				m_kbStateDown.SetAt(input);

			return true;
		}
		
		return false;
	}

	// Handles keyboard input and returns true if input was "pressed" (or repeated with timer)
	bool OnKeyboardInput( eGuiNav key )
	{ 
		m_kbActive = true;
		m_kbState.SetAt(key);
		if ( m_kbWaitForRelease )
			return false;

		/*
		// Return true if pressed this frame		
		if ( key <= eGuiNav.Down )
		{ 
			// handle up/down/left/right together. Stops "rolling" analog stick counting as lots of presses which feels bad when pushing diagonally- you get it jumping over 2 at once...
			// But if one direction isn't "handled" and other is, this feels bad not too :/
			int num = 0;
			for ( eGuiNav butt = eGuiNav.Left; butt <= eGuiNav.Down; butt++ )
				if ( m_kbPrevState.IsSet(butt) )
					num++;
			if ( num > 0 && m_kbPrevState.IsSet(key) == false ) // lower repeat time when rolling
				m_kbRepeatTimer = KB_REPEAT_TIME_REPEAT;

			if ( num == 0 )
				return true;
		}
		else */
		if ( m_kbPrevState.IsSet(key) == false )  // Return true if pressed this frame	
			return true;


		// Return true if held until repeat timer is zero
		if ( m_kbRepeatTimer <= 0 )
		{
			m_kbRepeatTimer = KB_REPEAT_TIME_REPEAT;
			return true;
		}

		return false;
	}

	// gui keyboard input stuff	

	// Return whether key was pressed this frame (or repeated) and marks the input as having been "used" so other thigns won't use it
	public bool ConsumeGuiKey( eGuiNav key )
	{ 		
		if ( GetGuiKeyPress(key) )
		{ 
			m_kbStateDown.UnsetAt(key);
			return true;
		}
		return false;
	}
	// Returns wheter the key is currently held down. Only valid between this component update and When gui.HandleKeyboardInput functions are called. Not a real input system.
	public bool GetGuiKey(eGuiNav button) { return m_kbWaitForRelease ? false : m_kbState.IsSet(button); }
	// Return whether key was pressed this frame
	public bool GetGuiKeyPress(eGuiNav button) { return m_kbWaitForRelease ? false : m_kbStateDown.IsSet(button); } // (m_kbState.IsSet(button) && m_kbPrevState.IsSet(button) == false); }
	// Returns whether key was released this frame
	public bool GetGuiKeyRelease(eGuiNav button) { return m_kbWaitForRelease ? false : (m_kbState.IsSet(button) == false && m_kbPrevState.IsSet(button)); }
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Private functions

	void UpdateFadeSprite()
	{
		if ( m_prefabEffectMenuFadeOut != null && m_sprite == null && m_customFade == false )
		{
			GameObject obj = GameObject.Instantiate(m_prefabEffectMenuFadeOut.gameObject);
			if ( obj != null )
			{
				m_sprite = obj.GetComponent<SpriteRenderer>();
				// If pixel cam's enabled, set to the sub-pixel layer so it renders on top of everything
				if ( PowerQuest.Get.GetPixelCamEnabled() )
					obj.layer = LayerMask.NameToLayer("HighRes");
			}
		}

		if ( m_sprite != null && m_customFade == false )
		{
			m_sprite.color = m_fadeColour.WithAlpha(m_fadeAlpha);
			m_sprite.gameObject.SetActive(m_fadeAlpha > 0.0f);
		}

		if ( m_customFade && CallbackOnUpdateFade != null )
			CallbackOnUpdateFade.Invoke();

			
	}
	

	void UpdateKb()
	{

		// Update repeat timer
		if ( m_kbPrevState == 0 && m_kbState != 0 )
		{
			// Keys was first pressed			
		}

		m_kbRepeatTimer -= Time.deltaTime;		

		// All keys released, reset timer
		if ( m_kbState.Value == 0 )
		{
			m_kbRepeatTimer = KB_REPEAT_TIME_FIRST;
		}

		// Update last state
		m_kbPrevState.Value = m_kbState.Value;
		m_kbState.Value = 0;
		m_kbStateDown.Value = 0;

		// Reset 'wait for release' flag
		if ( m_kbWaitForRelease && m_kbPrevState.Value == 0 )
			m_kbWaitForRelease = false;
		
		// Relinquish control if mouse moved
		if (m_kbActive )
		{
			if ( ((Vector2)Input.mousePosition-m_cachedMousePos).magnitude > 5 )
				m_kbActive = false;
		}
		else 
		{
			m_cachedMousePos = Input.mousePosition; 
		}

		// relinquish keyboard control if gui no longer active, or keyboard no longer active
		if ( m_kbFocus )
		{
			// Relinquish keyboard control if no gui active, or mouse moved			
			bool relinquish = m_kbActive == false;
			relinquish |= PowerQuest.Get.GetFocusedGui() == null || PowerQuest.Get.GetFocusedGui().Visible == false;

			if ( relinquish )
				RelinquishKbControl();
		}
	}
	
	public void SetKeyboardFocus(IGuiControl control)
	{
		if ( control == null )
		{
			RelinquishKbControl();
			return;
		}
		m_kbActive = true;		
		m_kbFocus = true;
		PowerQuest.Get.SetMouseOverClickableOverride( control as IQuestClickable );
		
	}

	// Gives focus control back to mouse (if it was focused)
	public void RelinquishKbControl()
	{
		if ( m_kbFocus )
			PowerQuest.Get.ResetMouseOverClickableOverride();
		m_kbFocus = false;
	}


}

}
