using UnityEngine;
using System.Collections;
using PowerTools;

namespace PowerTools.Quest
{

[AddComponentMenu("Quest Gui Layout/Dropdown Bar")]
public class GuiDropDownBar : MonoBehaviour 
{
	static readonly float BLEND_TIME = 0.1f;

	enum eScreenEdgeX { Left, Right, Center };
	enum eScreenEdgeY { Bottom, Top, Middle  };


	[Tooltip("Gui is shown whem mouse is this dist from edge of screen. \n Can be ratio (0.0-1.0), or pixel offset (>1)")]
	[SerializeField] Vector2 m_mouseEdgeDistanceShow = Vector2.one;
	[Tooltip("Gui is hidden when mouse is this dist from edge of screen. \n Can be ratio (0.0-1.0), or pixel offset (>1)")]
	[SerializeField] Vector2 m_mouseEdgeDistanceHide = Vector2.one;
	[Tooltip("Edge of screen that mouse has to be near to Show gui")]
	[SerializeField] eScreenEdgeX m_edgeX = eScreenEdgeX.Left;
	[Tooltip("Edge of screen that mouse has to be near to Show gui")]
	[SerializeField] eScreenEdgeY m_edgeY = eScreenEdgeY.Top;
	[Tooltip("Should pause game while mouse is over the gui?")]
	[SerializeField] bool m_pauseWhenVisible = true;	
	[Tooltip("Whether to have the gui hide when a blocking script is running")]
	[SerializeField] bool m_hideDuringCutscenes = false; // obsolete with new gui, since this can just be set in the actual gui component
	[Tooltip("How far to move (multiplied by the result of the animation curves")]
	[SerializeField] float m_dropDownDistance = 1;
	[Tooltip("Anim curves that play when show/hiding the gui")]
	[SerializeField] AnimationCurve m_curveIn = new AnimationCurve();
	[SerializeField] AnimationCurve m_curveOut = new AnimationCurve();


	[Header("Sounds")]
	[SerializeField] AudioCue m_soundShow = null;
	[SerializeField] AudioCue m_soundHide = null;

	GuiComponent m_guiComponent = null;

	bool m_shown = false;
	float m_ratio = 0.0f;
	Vector2 m_offset = Vector2.zero;
	float m_blendTimer = 0;
	Vector2 m_blendOffset = Vector2.zero;
	bool m_delayShow = false;

	float m_highlightPopupTimer = 0.0f;
	bool m_triggeredPause = false;

	bool m_forceOff = false;


	public Vector2 MouseEdgeDistanceShow { get{return m_mouseEdgeDistanceShow; } set{ m_mouseEdgeDistanceShow = value;} }

	public void SetForceOff(bool forceOff) {  m_forceOff = forceOff; }
	public bool GetDown() { return m_shown || m_ratio > 0.5f; } // down if shown or at least 50% down

	public Vector2 GetOffset() { return m_offset; }

	// Shows for time to draw attention to the UI. Doesn't pause the game while this is happening even for m_pauseWhenVisible popups
	public void HighlightForTime(float time)
	{
		m_highlightPopupTimer = time;
	}

	public void Show()
	{
		if ( m_forceOff )
			return;
		
		if (  m_shown == false )
			SystemAudio.Play(m_soundShow);

		m_shown = true;
		//Debug.Log("Show");
		if ( m_guiComponent != null )
		{
			m_guiComponent.GetData().Clickable = true;
			if ( m_pauseWhenVisible && m_highlightPopupTimer <= 0 )
			{
				// Pause game if m_pauseWhenVisible (unless force show is used)
				PowerQuest.Get.Pause(gameObject.name);
				m_triggeredPause = true;
			}
		}

		if ( m_ratio > 0 )
		{
			m_blendTimer = BLEND_TIME;
			m_blendOffset = m_offset;
		}
	}


	public void Hide()
	{
		if (  m_shown == true )
			SystemAudio.Play(m_soundHide);

		m_shown = false;
		//Debug.Log("Hide");

		if ( m_guiComponent != null )
		{
			m_guiComponent.GetData().Clickable = true;
			if ( m_pauseWhenVisible && m_triggeredPause )
			{
				// Unpause game
				PowerQuest.Get.UnPause(gameObject.name);
				m_triggeredPause = false;
			}
		}

		if ( m_ratio < 1 )
		{
			m_blendTimer = BLEND_TIME;
			m_blendOffset = m_offset;
		}

		// Dont' show again the next update- Gives time for "block" to be enabled
		m_delayShow = true;
	}

	void OnEnable()
	{
		// Always start 'hidden'
		m_guiComponent = GetComponentInParent<GuiComponent>();
		m_ratio = 0;
		m_shown = false;
		m_blendTimer = 0;
		Update();
	}

	// Use this for initialization
	//void Start () 
	//{
	//	OnEnable();
	//}

	void OnDisable()
	{
		if ( m_triggeredPause && PowerQuest.Exists )
		{
			PowerQuest.Get.UnPause(gameObject.name);
			m_triggeredPause = false;
		}
	}

	void OnDestroy()
	{
		OnDisable();
	}
	
	// Update is called once per frame
	void Update () 
	{	
		if ( m_highlightPopupTimer > 0 )
			m_highlightPopupTimer -= Time.deltaTime;
		
		RectTransform rectTransform = GetComponent<RectTransform>();
		AlignToScreen guiAlign = GetComponent<AlignToScreen>();

		if ( rectTransform != null )
			rectTransform.localPosition -= m_offset.WithZ(0);
		else if ( guiAlign != null )
			guiAlign.Offset -= m_offset;
		else
			transform.Translate(-m_offset);		

		bool underModal = PowerQuest.Get.GetIsGuiObscuredByModal(m_guiComponent.GetData());

		if ( m_shown )
		{
			if ( m_ratio < 1.0f && m_curveIn.keys.Length > 0 )
			{
				// animate in
				float time = m_curveIn.keys[m_curveIn.keys.Length-1].time;
				m_ratio += (1.0f/time) * Time.deltaTime;
				m_ratio = Mathf.Clamp01(m_ratio);
				m_offset.y = m_curveIn.Evaluate(m_ratio * time) * m_dropDownDistance;
			}
			else 
			{
				m_offset.y = 0;
			}			

			if ( m_forceOff 
				|| ( PowerQuest.Get.GetBlocked() == false && (CalcMouseInBounds(m_mouseEdgeDistanceHide) == false || underModal) && m_highlightPopupTimer <= 0 ) )
			{
				Hide();
			}
		}
		else 
		{	

			if ( m_ratio > 0.0f  && m_curveOut.keys.Length > 0 )
			{
				// animate out
				float time = m_curveOut.keys[m_curveOut.keys.Length-1].time;
				m_ratio -= (1.0f/time) * Time.deltaTime;
				m_ratio = Mathf.Clamp01(m_ratio);
				m_offset.y = m_curveOut.Evaluate((1.0f-m_ratio) * time) * m_dropDownDistance;
			}
			else 
			{
				m_offset.y = m_dropDownDistance;
			}

			if ( m_delayShow == false && PowerQuest.Get.GetBlocked() == false && ((CalcMouseInBounds(m_mouseEdgeDistanceShow) && underModal == false) || m_highlightPopupTimer > 0))
			{
				Show();
			}
		}

		// Blend between in and out trasitions
		if ( m_blendTimer > 0 )
		{
			m_blendTimer -= Time.deltaTime;
			m_offset = Vector2.Lerp(m_offset, m_blendOffset, m_blendTimer/BLEND_TIME);
		}

		if ( m_highlightPopupTimer <= 0 && m_hideDuringCutscenes && PowerQuest.Get.GetBlocked() ) 
		{
			m_offset.y += m_dropDownDistance*10.0f;
		}
		
		if ( rectTransform != null )
			rectTransform.localPosition += m_offset.WithZ(0);
		else if ( guiAlign != null )
		{
			guiAlign.Offset += m_offset;
			guiAlign.ForceUpdate();
		}
		else
			transform.Translate(m_offset);		
		
		m_delayShow = false; // reset single frame flag
	}


	bool CalcMouseInBounds(Vector2 size)
	{
		Camera cam = PowerQuest.Get.GetCameraGui();
		if ( cam == null )
			return false; // Only null when testing with gui placed in scene before runningI

		Vector2 mouseScreenPos = ((Vector2)cam.ScreenToViewportPoint(Input.mousePosition)).Clamp(new Vector2(0.01f,0.01f), new Vector2(0.99f,0.99f));

		if ( size.x > 1.0f )
		{
			// Treat as pixel size rather than ratio 
			size.x = cam.WorldToViewportPoint(cam.transform.position + Vector3.zero.WithX(size.x)).x-0.5f;
		}
		if ( size.y > 1.0f )
		{
			// Treat as pixel size rather than ratio 
			size.y = cam.WorldToViewportPoint(cam.transform.position + Vector3.zero.WithY(size.y)).y-0.5f;
		}

		Vector2 startPos = Vector2.zero;
		switch ( m_edgeX )
		{
			case eScreenEdgeX.Center: 	startPos.x = size.x; break;
			case eScreenEdgeX.Left: 	startPos.x = 0; break;
			case eScreenEdgeX.Right: 	startPos.x = 1.0f-size.x; break;
		}
		switch ( m_edgeY )
		{
			case eScreenEdgeY.Middle: 	startPos.y = size.y; break;
			case eScreenEdgeY.Bottom: 	startPos.y = 0; break;
			case eScreenEdgeY.Top: 		startPos.y = 1.0f-size.y; break;
		}

		if ( m_edgeX == eScreenEdgeX.Center )
			size.x = 1.0f-(size.x*2.0f);
		if ( m_edgeY == eScreenEdgeY.Middle)
			size.y = 1.0f-(size.y*2.0f);

		Rect rect = new Rect( startPos.x, startPos.y, 
			size.x, 
			size.y);

		return rect.Contains(mouseScreenPos);
	}
}

}
