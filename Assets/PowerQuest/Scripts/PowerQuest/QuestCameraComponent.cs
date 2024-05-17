using UnityEngine;
using System.Collections;
using PowerTools;

namespace PowerTools.Quest
{


//
// Actual Camera component in the scene
//
public partial class QuestCameraComponent : MonoBehaviour 
{
	// Seperated state data so we can calc zoomed and unzoomed positions independently and lerp between them
	public class StateData
	{
		public Vector2 position = Vector2.zero;		
		public Vector2 targetPosition = Vector2.zero; // Position without any smoothing

		// The current amount we're zoomed from the default set in PowerQuest
		public float zoom = 1;
		
		public bool followPlayer = true;
		public Vector2 playerDragged = Vector2.zero;
		public Vector2 playerPosCached = Vector2.zero;
	}

	[SerializeField] QuestCamera m_data = null;
	[SerializeField] float m_smoothingFactor = 10.0f;
	[SerializeField] float m_smoothingMinSpeed = 10.0f;
	[Tooltip("How far player has to move before scrolling starts")]
	[SerializeField] Vector2 m_distFromPlayerBeforeScroll = new Vector2(25,10);
	[SerializeField] float m_characterFacingOffset = 0.0f;

	[Header("Screenshake global multipliers")]
	[SerializeField] float m_shakeIntensityMult = 1.0f;
	[SerializeField] float m_shakeFalloffMult = 1.0f;	
	[SerializeField] float m_shakeSpeed = 40.0f;

	[Header("Prefab References")]
	[SerializeField] GameObject m_prefabPixelCam = null;
	
	float m_shakeIntensity = 0;	
	float m_shakeFalloff = 1;
	float m_shakeDurationTimer = 0;
	Vector2 m_screenShakeOffset = Vector2.zero;
	
	bool m_onLerpChange = false;
	float m_lerpTime = 0;
	float m_lerpTimer = 0;	
	bool m_posLerpActive = false;
	bool m_zoomLerpActive = false;

	bool m_targetPositionChanged = false;
	Vector2 m_cachedTargetNoPixelSnap = Vector2.zero;

	Camera m_camera = null;
	GameObject m_pixelCam = null;
	
	// Working state data for the camera, and the camera it was transitioning from
	StateData m_state = new StateData();
	StateData m_statePrev = new StateData();
	
	// Working state data, without the zoom. This is done because changing zoom also changes position, since you can get closer to screen edge
	StateData m_stateParallax = new StateData();
	StateData m_stateParallaxPrev = new StateData();	
			
	eFace m_playerFaceLast = eFace.Down;

	// Hack to ensure can check if snapped last update
	bool m_snappedSinceUpdate = true;
	bool m_snappedLastUpdate = true;

	// Cached positions for parallax
	Vector2 m_parallaxPos = Vector2.zero;
	Vector2 m_parallaxTargetPos = Vector2.zero;
	RectCentered m_parallaxOffsetLimits = new RectCentered();

	// Camera's current velocity (or its velocity last frame at least...)
	Vector2 m_velocity = Vector2.zero;
	
	public QuestCamera GetData() { return m_data; }
	public void SetData(QuestCamera data) { m_data = data; }

	public Camera Camera => m_camera;

	public void OnEnterRoom()
	{
		Snap();
	}

	public void Snap()
	{		
		ResetPlayerDragPos(m_stateParallax);
		ResetPlayerDragPos(m_state);
		UpdatePos(true);
	}

	public bool GetSnappedLastUpdate() { return m_snappedLastUpdate; }
	
	public void OnOverridePosition(float transitionTime)
	{	
		m_lerpTime = transitionTime;
		m_lerpTimer = transitionTime;
		m_onLerpChange = true;
		
		// Snap if there's no transition
		if ( transitionTime <= 0 )
			Snap();
	}

	public bool GetTransitioning() { return m_posLerpActive || m_zoomLerpActive; } 

	public bool GetHasPositionOverrideOrTransitioning()
	{
		return m_data.GetHasPositionOverride() || m_posLerpActive;
	}
		
	public void OnZoom(float transitionTime) { OnOverridePosition(transitionTime); } // Zooms are combined now

	public bool GetHasZoomOrTransitioning()
	{
		return m_data.GetHasZoom() || m_zoomLerpActive;
	}
		
	Vector2 GetHalfCamSize(float zoomMult)
	{
		float orthoSize = PowerQuest.Get.VerticalResolution*0.5f/zoomMult;
		return new Vector2(orthoSize * m_camera.aspect,orthoSize);
	}

	public Vector2 GetPositionForParallax() { return m_parallaxPos; }
	public Vector2 GetParallaxTargetPosition() { return m_parallaxTargetPos; }
	public RectCentered GetParallaxOffsetLimits() { return m_parallaxOffsetLimits; }	

	// Returns the distance the camera can move before showing screen edges
	RectCentered CalcOffsetLimits(float zoomMult)
	{	
		Vector2 halfCamSize = GetHalfCamSize(zoomMult);
		RectCentered result = PowerQuest.Get.GetCurrentRoom().Bounds;
		result.Min = result.Min + halfCamSize;
		result.Max = result.Max - halfCamSize;
		if ( result.Width < 0 )
			result.Width = 0;
		if ( result.Height < 0 )
			result.Height = 0;
		return result;
	}	

	// Returns a position where camera won't be outside room bounds
	public Vector2 ClampPositionToRoomBounds(float zoomMult, Vector2 position )
	{
		if ( m_data.IgnoreBounds )
			return position;
		RectCentered maxOffset = CalcOffsetLimits(zoomMult);
		position.x = Mathf.Clamp( position.x, maxOffset.Min.x, maxOffset.Max.x );
		position.y = Mathf.Clamp( position.y, maxOffset.Min.y, maxOffset.Max.y );
		return position;		
	}

	void ResetPlayerDragPos(StateData s)
	{
		Vector2 plrTargetPos = GetCharacterTargetPos(s);
		s.playerPosCached = plrTargetPos;
		s.playerDragged = plrTargetPos;		
	}

	public Vector2 GetCharacterTargetPos(StateData s)
	{
		Vector2 characterPos = Vector2.zero;
		ICharacter character = m_data.GetCharacterToFollow();
		if ( character != null || PowerQuest.Get.GetCurrentRoom() != character.Room )
		{
			// When character is facing left/right, add/change offset from the character so the camera leads in the direction they're facing
			if ( m_playerFaceLast != character.Facing )
			{					
				if ( character.Facing == eFace.Left || character.Facing == eFace.Right )
				{
					m_playerFaceLast = character.Facing;
				}
			}

			characterPos = character.Position + m_data.OffsetFromCharacter;

			// When facing left/right, offset so that camera leads infront of player
			if ( character.Walking )
			{
				if ( m_playerFaceLast == eFace.Left )
				{
					characterPos.x = characterPos.x - (m_characterFacingOffset / s.zoom);
				}
				else if ( m_playerFaceLast == eFace.Right )
				{
					characterPos.x = characterPos.x + (m_characterFacingOffset / s.zoom);
				}
			}
		}
		return characterPos;
	}

	// Calcuates the target camera position it's following the player (even if it's not currently). 
	// Quest Scripts can use this to find where the camera would be when overrideing the position.
	public Vector2 GetCameraFollowTargetPosition( StateData s, bool disablePixelSnap = false )
	{
		if ( PowerQuest.Get == null && m_data == null )
			return Vector2.zero;
		
		Vector2 position = s.position;
		
		ICharacter character = m_data.GetCharacterToFollow();
		if ( character != null && PowerQuest.Get.GetCurrentRoom() == character.Room )
		{
			Vector2 characterPos = GetCharacterTargetPos(s);

			// When player moves back/forth quickly, don't scroll the room
			Vector2 distFromPlayerBeforeScroll = m_distFromPlayerBeforeScroll / s.zoom;

			if ( characterPos.x > s.playerPosCached.x )
			{
				if ( characterPos.x > s.playerDragged.x + distFromPlayerBeforeScroll.x )
					s.playerDragged.x = characterPos.x - distFromPlayerBeforeScroll.x;
			}
			else 
			{
				if ( characterPos.x < s.playerDragged.x - distFromPlayerBeforeScroll.x )
					s.playerDragged.x = characterPos.x + distFromPlayerBeforeScroll.x;			
			}

			if ( characterPos.y > s.playerPosCached.y )
			{
				if ( characterPos.y > s.playerDragged.y + distFromPlayerBeforeScroll.y )
					s.playerDragged.y = characterPos.y - distFromPlayerBeforeScroll.y;
			}
			else 
			{
				if ( characterPos.y < s.playerDragged.y - distFromPlayerBeforeScroll.y )
					s.playerDragged.y = characterPos.y + distFromPlayerBeforeScroll.y;			
			}
			s.playerPosCached = characterPos;	
			

			if ( m_camera != null )
			{									
				RectCentered scrollSize = PowerQuest.Get.GetCurrentRoom().ScrollBounds;
				RectCentered offsetLimits = CalcOffsetLimits(s.zoom);

				if ( scrollSize.Width <= 0.0f )
				{
					position.x = s.playerDragged.x;
				}
				else 
				{
					if ( s.zoom != 1.0f ) // When Zoom is applied, scale the scroll limits, otherwise character will probably be unable to walk around
					{
						scrollSize.MinX = offsetLimits.MinX + ((scrollSize.MinX - offsetLimits.MinX) / s.zoom);
						scrollSize.MaxX = offsetLimits.MaxX + ((scrollSize.MaxX - offsetLimits.MaxX) / s.zoom);
					}
					position.x = Mathf.Lerp( offsetLimits.Min.x, offsetLimits.Max.x, Mathf.InverseLerp(scrollSize.Min.x, scrollSize.Max.x, s.playerDragged.x) );
				}

				if ( scrollSize.Height <= 0.0f )
				{
					position.y = s.playerDragged.y;
				}
				else 
				{
					if ( s.zoom != 1.0f ) // When Zoom is applied, scale the scroll limits, otherwise character will probably be unable to walk around
					{
						scrollSize.MinY = offsetLimits.MinY + ((scrollSize.MinY - offsetLimits.MinY) / s.zoom);
						scrollSize.MaxY = offsetLimits.MaxY + ((scrollSize.MaxY - offsetLimits.MaxY) /s.zoom);
					}
					position.y = Mathf.Lerp( offsetLimits.Min.y, offsetLimits.Max.y, Mathf.InverseLerp(scrollSize.Min.y, scrollSize.Max.y, s.playerDragged.y) );
				}
				
			}				
		}		

		// Snap target position
		if ( disablePixelSnap == false )
			position = Utils.Snap(position, PowerQuest.Get.SnapAmount);

		//
		// Clamp to room bounds
		//
		position = ClampPositionToRoomBounds(s.zoom,position);

		return position;
	}

	public bool GetTargetChangedLastUpdate()  { return m_targetPositionChanged; } 
	public float GetTransitionTime()  { return m_lerpTime; } 

	public Vector2 Velocity => m_velocity;
	
	// Use this for initialization
	void Awake() 
	{
		m_camera = GetComponent<Camera>();
	}

	void Start() 
	{
		if ( PowerQuest.Get.GetPixelCamEnabled() && m_prefabPixelCam != null )
		{
			// Set up pixel camera
			m_pixelCam = GameObject.Instantiate(m_prefabPixelCam) as GameObject;
			// Set pixel cam render layer (camera layers without the "HighRes" one)
			int layerHighRes = LayerMask.NameToLayer("HighRes");			
			m_pixelCam.GetComponent<Camera>().cullingMask = Utils.MaskUnsetAt(m_camera.cullingMask,layerHighRes);
			m_pixelCam.transform.GetChild(0).gameObject.layer = layerHighRes;
			// Set this camera to only render HighRes stuff
			m_camera.cullingMask = 1<<layerHighRes;
		}
		// Removed this again- since it's probably trying to render over the top of hi-res background
		//m_pixelCam.GetComponent<Camera>().backgroundColor = GetComponent<Camera>().backgroundColor;

		// Start with lerp change true so camera state data gets set up first time
		m_onLerpChange = true;
	}

	// Update is called once per frame
	void Update() 
	{	
		// Hack to ensure can check if snapped last update
		if ( m_snappedSinceUpdate == false )
			m_snappedLastUpdate = false;
		m_snappedSinceUpdate = false;

		UpdatePos(PowerQuest.Get.GetSkippingCutscene());
	}

	void LateUpdate()
	{
		// Update pixel cam position if it exists
		if ( m_pixelCam != null )			
			m_pixelCam.transform.position = Utils.Snap(transform.position).WithZ(m_pixelCam.transform.position.z);
	}

	// Used when changing bounds to keep parallax locked in same position... maybe unnecessary...
	bool m_lockParallaxAlignment = false;
	public bool LockParallaxAlignment 
	{ 
		get { return m_lockParallaxAlignment;} 
		set 
		{
			m_lockParallaxAlignment=value;	
			m_parallaxOffsetLimits = CalcOffsetLimits(1);
		} 
	}

	
	void UpdatePos(bool snap)
	{
		if ( snap )
		{
			m_snappedSinceUpdate = true;
			m_snappedLastUpdate = true;
		}

		if ( m_data.Enabled == false || Time.deltaTime == 0 )
			return;

		Vector2 position = m_data.GetPosition();
		Vector2 oldPosition = position;
		Vector2 targetPosition = position;				
		Vector2 parallaxTargetPos = position;
		float zoomMultiplier = 1;
		float orthoSize = PowerQuest.Get.VerticalResolution*0.5f;
		if ( snap )
			m_velocity = Vector2.zero;
		else 
			m_velocity = (position-oldPosition) / Time.deltaTime;
		
		m_targetPositionChanged = false; // this gets set true again 

		if ( m_onLerpChange )
		{
			m_onLerpChange = false;

			// Copy camera current state to 'prev' states
			QuestUtils.CopyFields(m_stateParallaxPrev,m_stateParallax);
			QuestUtils.CopyFields(m_statePrev,m_state);
			
			m_state.zoom = m_data.GetHasZoom() ? m_data.GetZoom() : 1;

			// Set up new state data			
			if ( m_data.GetHasPositionOverride() )
			{
				if ( m_statePrev.followPlayer == false )
				{
					// Chaining position overrides, so use the current camera position as the previous target pos
					m_statePrev.position = position;
					m_statePrev.targetPosition = position;
					m_stateParallaxPrev.position = position;
					m_stateParallaxPrev.targetPosition = position;
				}

				m_stateParallax.followPlayer = false;
				m_stateParallax.position = ClampPositionToRoomBounds(1, m_data.GetPositionOverride());
				m_stateParallax.targetPosition = m_stateParallax.position;
				m_state.followPlayer = false;
				m_state.position = ClampPositionToRoomBounds(m_state.zoom, m_data.GetPositionOverride());;
				m_state.targetPosition = m_state.position;
				
				m_targetPositionChanged = true;
			}
			else 
			{
				m_stateParallax.followPlayer = true;
				m_state.followPlayer = true;

				// Reset dragpos when tranitioning back
				ResetPlayerDragPos(m_stateParallax);
				ResetPlayerDragPos(m_state);
			}
			
			// Update
			m_posLerpActive = m_stateParallax.followPlayer != m_stateParallaxPrev.followPlayer || (m_stateParallax.followPlayer == false && m_stateParallax.position != m_stateParallaxPrev.position);
			m_zoomLerpActive = m_state.zoom != m_statePrev.zoom;
		}
		
		// Update lerp ratio
		float ratio = 1;
		if ( snap )
			m_lerpTimer = 0;
		if ( m_lerpTimer > 0 )
		{
			m_lerpTimer -= Time.deltaTime;

			if ( m_lerpTimer <= 0 )
			{
				// Finished transition
				m_posLerpActive = false;
				m_zoomLerpActive = false;
			}
			else if ( m_lerpTime > 0 )
			{
				ratio = Mathf.Clamp01( 1.0f-(m_lerpTimer/m_lerpTime) );
				ratio = QuestUtils.Ease(ratio);
			}
		}		
		
		// Update non-zoomed state
		UpdateCameraState(m_stateParallax,snap,false);
		if ( m_posLerpActive )			
			UpdateCameraState(m_stateParallaxPrev,snap,false);

		// Update zoomed state
		UpdateCameraState(m_state,snap,true);
		if ( m_posLerpActive || m_zoomLerpActive )
			UpdateCameraState(m_statePrev,snap,true);

		// Lerp for transitions
		targetPosition = Vector2.Lerp( m_statePrev.targetPosition, m_state.targetPosition, ratio);

		// Change to bounds, so can lerp linearly with zoom		
		RectCentered boundsFrom =  new RectCentered(m_statePrev.position.x,m_statePrev.position.y,GetHalfCamSize(m_statePrev.zoom).x,GetHalfCamSize(m_statePrev.zoom).y);		
		RectCentered boundsTo =    new RectCentered(m_state.position.x,m_state.position.y,GetHalfCamSize(m_state.zoom).x,GetHalfCamSize(m_state.zoom).y);		
		RectCentered boundsFinal = new RectCentered( Vector2.Lerp(boundsFrom.Min,boundsTo.Min,ratio), Vector2.Lerp(boundsFrom.Max,boundsTo.Max,ratio) );		
		position = boundsFinal.Center;
		orthoSize = boundsFinal.Height;
		zoomMultiplier = PowerQuest.Get.VerticalResolution * 0.5f/orthoSize;
		position = ClampPositionToRoomBounds(zoomMultiplier,position);

		m_parallaxPos = Vector2.Lerp( m_stateParallaxPrev.position, m_stateParallax.position, ratio);
		m_parallaxTargetPos = m_stateParallax.targetPosition;//Vector2.Lerp( m_stateParallaxPrev.targetPosition, m_stateParallax.targetPosition, ratio);
		if ( LockParallaxAlignment == false )
			m_parallaxOffsetLimits = CalcOffsetLimits(1);
		

		//
		// Screenshake
		//
		m_screenShakeOffset = Vector2.zero;				
		{	
			if ( m_shakeIntensity > 0 )
			{
				m_screenShakeOffset = (((new Vector2( Mathf.PerlinNoise(m_shakeSpeed * Time.time, 0), Mathf.PerlinNoise(1, m_shakeSpeed * Time.time) )) * 2) - Vector2.one) * m_shakeIntensity * m_shakeIntensityMult / zoomMultiplier;			
				
				if ( m_shakeDurationTimer > 0 )
				{
					m_shakeDurationTimer -= Time.deltaTime;
					if ( m_shakeDurationTimer <= 0 )
					if ( m_shakeFalloff > 0 )
					{
						m_shakeIntensity -= (-m_shakeDurationTimer) / m_shakeFalloff; // If we overshot the end time, apply the amount we overshot to the falloff
					}
					else 
					{
						m_shakeIntensity = 0;
					}
				}
				else if ( m_shakeFalloff > 0 )
				{			
					m_shakeIntensity -= Time.deltaTime / m_shakeFalloff;
				}
				else 
				{
					m_shakeIntensity = 0;
				}
			}
		}

		//
		// Apply position
		//	
		m_data.SetPosition(position); // Store in data
		m_data.SetTargetPosition(targetPosition);
		transform.position = (m_screenShakeOffset + position).WithZ(transform.position.z); 		
		m_camera.orthographicSize = orthoSize;
	}
	
	
	void UpdateCameraState(StateData s, bool snap, bool allowZoom)
	{
		Vector2 oldPosition = s.position;
		s.targetPosition = oldPosition;
		Vector2 targetPositionNoPixelSnap = oldPosition;
		
		if ( s.followPlayer )
		{
			// Following player
			s.position = GetCameraFollowTargetPosition(s);
			s.targetPosition = s.position;
			targetPositionNoPixelSnap = GetCameraFollowTargetPosition(s,true);
			
			if ( s == m_stateParallax ) // only do this for the parallax camera
			{
				// Update whether targetposition changed. this ignores snapping, otherwise gives wrong results when checking if it changed last update.
				m_targetPositionChanged = ( targetPositionNoPixelSnap != m_cachedTargetNoPixelSnap );
				m_cachedTargetNoPixelSnap = targetPositionNoPixelSnap;
			}		
		}

		//
		// Smooth camera movement
		//
		if ( snap == false && s.followPlayer ) // Only smoothing when following the player
		{
			Vector2 diff = s.position - oldPosition;
			float dist = diff.magnitude;
			float smoothDist = Mathf.Max(m_smoothingMinSpeed, m_smoothingFactor * dist) * Time.deltaTime * s.zoom;
			if ( dist > smoothDist ) // don't overshoot
				s.position = oldPosition + (smoothDist*diff.normalized);
		}
		
	}

	// The current shake intensity. Can be used to add your own
	public float ShakeIntensity => m_shakeIntensity;

	public void Shake( CameraShakeData shakeData )  { Shake(shakeData.m_intensity, shakeData.m_duration, shakeData.m_falloff); }

	public void Shake( float intensity = 1.0f, float duration = 0.1f, float falloff = 0.15f ) 
	{
		if ( intensity > m_shakeIntensity )
		{
			m_shakeDurationTimer = duration;
			m_shakeIntensity = intensity;
			m_shakeFalloff = m_shakeIntensity <= 0 ? 0 : ( falloff * m_shakeFalloffMult / m_shakeIntensity);		
		}
		else if ( intensity == 0 )
		{
			m_shakeFalloff = 0;
		}
	}

	void MsgShake( float intensity, float duration, float falloff ) { Shake(intensity, duration,  falloff); }
	void MsgShake( float intensity, float duration ) { Shake(intensity, duration); }
	void MsgShake( float intensity ) { Shake(intensity); }

}

// CameraShakeData lets you have a camera shake set up in the inspector as a single variable rather than passing individual vars to the screenshake
[System.Serializable]
public class CameraShakeData
{
	public float m_intensity = 1;
	public float m_duration = 0.1f;
	public float m_falloff = 0.15f;
}

}
