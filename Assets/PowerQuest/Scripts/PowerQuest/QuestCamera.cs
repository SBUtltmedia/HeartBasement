using UnityEngine;
using System.Collections;
using PowerTools;

namespace PowerTools.Quest
{

//
// Camera Data and functions. Persistant between scenes, as opposed to CameraComponent which lives on a GameObject in a scene.
//
[System.Serializable] 
public partial class QuestCamera : ICamera
{
	[Tooltip("Offset from the target character that the camera tries to center on")]
	[SerializeField] Vector2 m_offsetFromCharacter = new Vector2(0,30);
	//
	// Private variables
	//
	QuestCameraComponent m_instance = null;
	string m_characterToFollow = null; // String so it will be saved
	bool m_hasPositionOverride = false; // Flag is necessary to be reversable mid-transition
	Vector2 m_positionOverride = new Vector2(float.MaxValue,float.MaxValue);	
	float m_zoom = 1.0f;	
	bool m_hasZoom = false; // Flag is necessary to be reversable mid-transition
	Vector2 m_position = Vector2.zero;
	Vector2 m_targetPosition = Vector2.zero;
	bool m_enabled = true;
	bool m_ignoreBounds = false;

	//
	//  Properties
	//

	/// Sets whether PowerQuest controls the camera, set to false if you want to control the camera yourself (eg. animate it)
	public bool Enabled 
	{ 
		get { return m_enabled; }
		set 
		{ 
			bool didEnable = m_enabled == false && value == true;
			m_enabled = value;
			if ( m_instance != null )
			{
				m_instance.enabled = value;
				if ( didEnable)
					m_instance.Snap();
			}
		}
	}


	/// Sets whether overrides to camera position ignore room bounds. Useful for snapping camera to stuff off to the side of the room in cutscenes
	public bool IgnoreBounds 
	{ 
		get { return m_ignoreBounds; }
		set 
		{ 
			m_ignoreBounds = value; 
			if ( m_instance != null )
				m_instance.Snap();
		}
	}

	public Vector2 OffsetFromCharacter { 
		get { return m_offsetFromCharacter; } 
		set { m_offsetFromCharacter = value; } }
		
	/// Returns the current shake intensity. \sa Shake
	public float ShakeIntensity => (m_instance?.ShakeIntensity ?? 0);

	//
	// Getters/Setters - These are used by the engine. Scripts mainly use the properties
	//

	public QuestCameraComponent GetInstance() { return m_instance; }
	public void SetInstance(QuestCameraComponent instance) 
	{ 
		m_instance = instance; 
		m_instance.SetData(this);
	}
	public ICharacter GetCharacterToFollow() { return PowerQuest.Get.GetCharacter(m_characterToFollow); }
	public bool GetHasPositionOverride() { return m_hasPositionOverride; }

	public Vector2 GetPositionOverride() { return m_positionOverride; }	

	public bool GetHasPositionOverrideOrTransition() 
	{ 
		if ( m_instance != null )
			return m_instance.GetHasPositionOverrideOrTransitioning(); 
		return m_hasPositionOverride; 
	}
	
	// Returns true if transitioning to/from position override or zoom
	public bool GetTransitioning() => m_instance == null ? false : m_instance.GetTransitioning();

	public void SetCharacterToFollow(ICharacter character, float overTime = 0) 
	{ 
		m_characterToFollow = character.ScriptName;
		if ( overTime > 0 ) 
		{
			SetPositionOverride(m_position,0); 
			ResetPositionOverride(overTime); 
		}
		else if ( m_instance != null ) 
		{
			m_instance.Snap(); 
		}
	}
	public void SetPositionOverride	(float x, float y = 0, float transitionTime = 0 ) 
	{ 
		m_hasPositionOverride = true; 
		m_positionOverride = new Vector2(x,y);  
		if ( m_instance != null )
			m_instance.OnOverridePosition(transitionTime);
	}
	public void SetPositionOverride	(Vector2 positionOverride, float transitionTime = 0 ) { SetPositionOverride(positionOverride.x, positionOverride.y, transitionTime); }
	public void ResetPositionOverride(float transitionTime = 0)
	{ 		
		m_hasPositionOverride = false;  
		if ( m_instance != null) 
			m_instance.OnOverridePosition(transitionTime); 
	}	

	/// Gets the current camera zoom (mulitplier on default/room vertical height)
	public float GetZoom() { return m_zoom > 0 ? m_zoom : 1; }
	/// Returns true if the camera has a zoom override
	public bool GetHasZoom() { return m_hasZoom; }	
	/// Returns true if the camera's zoom is overriden, or if it's still transitioning back to default
	public bool GetHasZoomOrTransition() { return ( m_instance != null ) ? m_instance.GetHasZoomOrTransitioning() : m_hasZoom; }

	/// Sets a camera zoom (mulitplier on default/room vertical height)
	public void SetZoom(float zoom, float transitionTime = 0)
	{
		m_hasZoom = true;
		m_zoom = zoom;
		if ( m_instance != null ) 
			m_instance.OnZoom(transitionTime); 
	}

	/// Removes any zoom override, returning to the default/room vertical height
	public void ResetZoom(float transitionTime = 0)
	{ 
		m_hasZoom = false;
		m_zoom = 1;
		if ( m_instance != null ) 
			m_instance.OnZoom(transitionTime);
	}
	
	/// Gets or sets the camera zoom  (mulitplier on default/room vertical height). Use SetZoom() to set transition time.
	public float Zoom { get{return m_zoom;} set { SetZoom(value); } }

	public void Snap() { m_instance.Snap(); }

	// Returns the actual position of the camera
	public Vector2 GetPosition() { return m_position; }
	
	/// Returns the actual position of the camera. Use SetPositionOverride to set a transition time \sa ResetPositionOverride()
	public Vector2 Position { get{return m_position;} set { SetPositionOverride(value); } }

	// Set position of camera object, usually you'd use SetPositionOverride to stop tracking a player.
	public void SetPosition(Vector2 position) { m_position = position; }

	// Returns the target position of the camera
	public Vector2 GetTargetPosition() { return m_targetPosition; }

	// Set position of camera object
	public void SetTargetPosition(Vector2 position) { m_targetPosition = position; }

	public bool GetSnappedLastUpdate() { return m_instance == null ? true : m_instance.GetSnappedLastUpdate(); }
	public bool GetTargetPosChangedLastUpdate() { return m_instance == null ? true : m_instance.GetTargetChangedLastUpdate(); }	
	public float GetTransitionTime() { return m_instance == null ? 0 : m_instance.GetTransitionTime(); }	
	
	public Vector2 Velocity => m_instance == null ? Vector2.zero : m_instance.Velocity;

	public void Shake(float intensity, float duration = 0.1f, float falloff = 0.15f)
	{
		m_instance.Shake(intensity, duration, falloff);
	}
	public void Shake(CameraShakeData data)
	{
		m_instance.Shake(data.m_intensity, data.m_duration, data.m_falloff);
	}

	public Camera Camera => (m_instance == null ? null : m_instance.Camera);

	//
	// Internal Functions
	//
}

}
