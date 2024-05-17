using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PowerTools.Quest
{

#region Class: Wait

// Util class with coroutines that can be paused.
public static class Wait
{

	public static IEnumerator CoroutineWaitForTime( float time )
	{		
		while ( time > 0.0f )
		{
			yield return new WaitForEndOfFrame();
			if ( SystemTime.Paused == false )
			{
				time -= Time.deltaTime;
			}
		}
		yield break;
	}
	
	public static IEnumerator CoroutineWaitForFixedUpdate( )
	{
		yield return new WaitForFixedUpdate();
		while ( SystemTime.Paused )
		{
			yield return new WaitForFixedUpdate();
		}
		yield break;
	}
	
	public static IEnumerator CoroutineWaitForEndOfFrame( )
	{
		yield return new WaitForEndOfFrame();
		while ( SystemTime.Paused )
		{
			yield return new WaitForEndOfFrame();
		}
		yield break;
	}
	
	public static IEnumerator CoroutineWaitForTimeNoPause( float time ) { yield return new WaitForSeconds(time); }	
	public static IEnumerator CoroutineWaitForFixedUpdateNoPause( ) { yield return new WaitForFixedUpdate(); }	
	public static IEnumerator CoroutineWaitForEndOfFrameNoPause( ) { yield return new WaitForEndOfFrame(); }

	public static Coroutine ForTime( float time )
	{
		return SystemTime.Get.StartCoroutine(CoroutineWaitForTime(time));
	}	
	public static Coroutine ForTimeUnscaled( float time ) // Waits for time, ignoring any slow mo stuff
	{
		return SystemTime.Get.StartCoroutine(CoroutineWaitForTime(time*SystemTime.Get.GetTimeScale()));
	}	
	
	public static Coroutine ForFixedUpdate()
	{
		return SystemTime.Get.StartCoroutine(CoroutineWaitForFixedUpdate());
	}	
	
	public static Coroutine ForEndOfFrame()
	{
		return SystemTime.Get.StartCoroutine(CoroutineWaitForEndOfFrame());
	}
	/*
	public static Coroutine WaitForTime( this MonoBehaviour self, float time )
	{
		if ( SystemTime.Get.IsLayerPauseable(self.gameObject.layer) == false )
		    return self.StartCoroutine(CoroutineWaitForTimeNoPause(time));
		return self.StartCoroutine(CoroutineWaitForTime(time));
	}
	public static Coroutine WaitForTimeUnscaled( this MonoBehaviour self, float time ) // Waits for time, ignoreing slow mo stuff
	{
		if ( SystemTime.Get.IsLayerPauseable(self.gameObject.layer) == false )
			return self.StartCoroutine(CoroutineWaitForTimeNoPause(time*SystemTime.Get.GetTimeScale()));
		return self.StartCoroutine(CoroutineWaitForTime(time*SystemTime.Get.GetTimeScale()));
	}		
	
	public static Coroutine WaitForFixedUpdate(this MonoBehaviour self)
	{
		if ( SystemTime.Get.IsLayerPauseable(self.gameObject.layer) == false )
			return self.StartCoroutine(CoroutineWaitForFixedUpdateNoPause());
		return self.StartCoroutine(CoroutineWaitForFixedUpdate());
	}	
	
	public static Coroutine WaitForEndOfFrame(this MonoBehaviour self)
	{
		if ( SystemTime.Get.IsLayerPauseable(self.gameObject.layer) == false )
			return self.StartCoroutine(CoroutineWaitForEndOfFrameNoPause());
		return self.StartCoroutine(CoroutineWaitForEndOfFrame());
	}*/


}


#endregion

public class SystemTime : Singleton<SystemTime> 
{	

	#region Definitions

	class tTimeScaleMultiplier
	{
		public float m_mult = 1;
		public float m_time = 0;
	}

	[Header("Layers and components that shouldn't pause")]
	[SerializeField] string[] m_ignoredLayers = {"UI","NoPause","PostProcessing"};
	[SerializeField] string[] m_ignoredComponents = {"PostProcessLayer"};
	//System.Type[] m_ignoredTypes = null;

	static int m_layersNoPause = 0;

	#endregion
	#region Vars: Private
	
	Dictionary< string, tTimeScaleMultiplier > m_timeScaleMultipliers = new Dictionary< string, tTimeScaleMultiplier >();			
	float m_timeSinceLastFrame = 0.0f;
	float m_timeFixedOriginal = 0.1f;
	
	float m_debugTimeMultiplier = 1;
	
	bool m_gamePaused = false;	
	List<string> m_pauseSources = new List<string>();
	
	List<Behaviour> m_pausedComponents = new List<Behaviour>();
	List<Rigidbody2D> m_pausedBodies = new List<Rigidbody2D>();
	List<Vector2> m_pausedVelocities = new List<Vector2>();
	List<float> m_pausedAngularVelocities = new List<float>();
	List<ParticleSystem> m_particleSystems = new List<ParticleSystem>();

	#endregion
	#region Funcs: Callbacks

	public System.Action CallbackOnPause = null;
	public System.Action CallbackOnUnpause = null;

	#endregion
	#region Funcs: Public

	public static bool Paused { get { return m_instance.m_gamePaused; } }
	public static bool GetPausedBy(string source) { return m_instance.m_pauseSources.Contains(source); }

	public bool IsBehaviourPausable(Behaviour behaviour)
	{
		/* // This is more efficient but needs more testing if it's necessary to use it for speed
		System.Type behaviourType = behaviour.GetType();
		if ( System.Array.Exists(m_ignoredTypes, item=> item==behaviourType) )
			return false;
		*/
		// Check if behaviour name is ignored
		string behaviourName = behaviour.GetType().Name;
		if ( System.Array.Exists(m_ignoredComponents, item=> item==behaviourName) )			
			return false;
		
		return true;
	}

	public void PauseGame(string source = null)
	{			
		if ( source == null ) source = string.Empty;
		m_gamePaused = true;
		m_pauseSources.Add(source);
		//if ( Debug.isDebugBuild) Debug.Log("Paused: "+source+", new count: "+m_instance.m_pauseSources.Count);
		
		GameObject[] allObjects = FindObjectsOfType(typeof(GameObject))  as GameObject[];
		GameObject obj = null;
		//bool tmpWasRendererEnabled = false;
		Behaviour[] tmpComponents = null;
		int tmpComponentCount = 0;
		Behaviour tmpComponent = null;

		ParticleSystem[] tmpParticles = null; // Why tf  can't particles be a behaviour... can't be consistant unity jeez
		ParticleSystem tmpParticle = null;
			
		int count = allObjects.Length;
		for ( int i = 0; i < count; ++i )
		{
			obj = allObjects[i];
		   	if (obj.activeInHierarchy && IsLayerPauseable(obj.layer))
			{
				Rigidbody2D body = obj.GetComponent<Rigidbody2D>();
				if (body != null && body.isKinematic == false )
				{
					m_pausedVelocities.Add( body.velocity );
					m_pausedAngularVelocities.Add( body.angularVelocity );
					m_pausedBodies.Add( body);
					body.isKinematic = true;
					body.velocity = Vector2.zero; 
					body.angularVelocity = 0;
				}				
								
				tmpComponents = obj.GetComponents<Behaviour>();				
				tmpComponentCount = tmpComponents.Length;
				for ( int j = 0; j < tmpComponentCount; ++j )
				{ 
					tmpComponent = tmpComponents[j];					
					if ( tmpComponent.enabled && (tmpComponent is MonoBehaviour || tmpComponent is Animator ) && IsBehaviourPausable(tmpComponent) )
					{
						m_pausedComponents.Add(tmpComponent);
						tmpComponent.enabled = false;
					}
				}

				// Particles aren't a behaviour, they're a component, have to be *special*
				tmpParticles = obj.GetComponents<ParticleSystem>();				
				tmpComponentCount = tmpParticles.Length;
				for ( int j = 0; j < tmpComponentCount; ++j )
				{
					tmpParticle = tmpParticles[j];
					if ( tmpParticle.isPlaying && tmpParticle.isPaused == false )
					{
						tmpParticle.Pause();
						m_particleSystems.Add(tmpParticle);
					}
				}
			}			

		}

		if ( CallbackOnPause != null )
			CallbackOnPause.Invoke();
	}
	
	public void UnPauseGame(string source = null)
	{
		if ( source == null ) source = string.Empty;

		m_pauseSources.RemoveAll( item => item ==source );
		//if ( Debug.isDebugBuild) Debug.Log("UnPaused: "+source+", new count: "+m_instance.m_pauseSources.Count);
		
		if ( m_pauseSources.Count == 0 )
		{
			
			Behaviour behaviour = null;
			int count = m_pausedComponents.Count;
			for ( int i = 0; i < count; ++i )
			{
				behaviour = m_pausedComponents[i];
				if ( behaviour != null )
					behaviour.enabled = true;
			}
			m_pausedComponents.Clear();
			
			Rigidbody2D body = null;
			count = m_pausedBodies.Count;
			for ( int i = 0; i < count; ++i )
			{
				body = m_pausedBodies[i];
				if ( body != null )
				{
					body.isKinematic = false;
					body.velocity = m_pausedVelocities[i];					
					body.angularVelocity = m_pausedAngularVelocities[i];	
					body.WakeUp();
				}
			}
			m_pausedBodies.Clear();
			m_pausedVelocities.Clear();
			m_pausedAngularVelocities.Clear();

			count = m_particleSystems.Count;
			for ( int i = 0; i < count; ++i )
			{
				if ( m_particleSystems[i] != null )
					m_particleSystems[i].Play();
			}
			m_particleSystems.Clear();
			
			m_gamePaused = false;	
		}


		if ( CallbackOnUnpause != null )
			CallbackOnUnpause.Invoke();
		
	}
	
	// REturns the time scale but not including debug time multiplier
	public float GetTimeScale() { return (Time.timeScale / m_debugTimeMultiplier); }
	
	// Returns the delta with only debug time scaling (no game-time scaling)
	public float GetUnscaledDeltaTime() { return Time.unscaledDeltaTime * m_debugTimeMultiplier; }
	
	
	// Time meddling functions
	public void SlowMoPause( string source, float time )
	{
		tTimeScaleMultiplier multiplier = new tTimeScaleMultiplier();
		multiplier.m_time = time;
		multiplier.m_mult = 0.001f;
		m_timeScaleMultipliers[source] = multiplier;
	}

	public void SlowMoBegin( string source, float scale, float time )
	{		
		//if ( m_timeScaleMultipliers.ContainsKey(source)
		m_timeScaleMultipliers[source] = new tTimeScaleMultiplier{ m_mult = scale, m_time = time };
	}
	
	public void SlowMoBegin( string source, float scale )
	{		
		m_timeScaleMultipliers[source] = new tTimeScaleMultiplier{ m_mult = scale, m_time = -1.0f };
	}	
	
	public void SlowMoEnd( string source )
	{
		m_timeScaleMultipliers.Remove(source);
	}
	
	
	public void SlowMoClear()
	{
		ClearTimeMultipliers();
	}
	
	public void SetDebugTimeMultiplier( float multiplier )
	{
		m_debugTimeMultiplier = multiplier;
	}
	
	public float GetDebugTimeMultiplier(  )
	{
		return m_debugTimeMultiplier;
	}

	#endregion
	#region Funcs: Utils

	/// Can call every update and returns true on updates where the time has elapsed
	public static bool TimePassed( float period )
	{
		return  ( Time.timeSinceLevelLoad % period) < ((Time.timeSinceLevelLoad - Time.deltaTime) % period);
	}	
	/// Can call every update and returns true on fixed updates where the time has elapsed
	public static bool TimePassedFixed( float period )
	{
		return  ( Time.timeSinceLevelLoad % period) < ((Time.timeSinceLevelLoad - Time.fixedDeltaTime) % period);
	}
	/// Can call every update and returns true on fixed updates where the time has elapsed. Can also offset into the period
	public static bool TimePassedFixed( float period, float offsetRatio )
	{
		float timeSinceLoad = period*offsetRatio+Time.timeSinceLevelLoad;
		return  ( timeSinceLoad % period) < ((timeSinceLoad - Time.fixedDeltaTime) % period);
	}

	#endregion
	#region Funcs: Private
	
	// Use this for initialization
	void Awake () 
	{
		SetSingleton();
		Object.DontDestroyOnLoad(this);	
		m_timeFixedOriginal = Time.fixedDeltaTime;
	
	}
	
	// Update is called once per frame
	void Update() 
	{
		UpdateTimeMultipliers();	

		if ( PowerQuest.Get.UseCustomKBShortcuts == false )
		{
			if ( PowerQuest.GetDebugKeyHeld() && Input.GetKeyDown(KeyCode.PageDown) )
			{
				SetDebugTimeMultiplier( GetDebugTimeMultiplier()*0.8f );
			}
			if ( PowerQuest.GetDebugKeyHeld() && Input.GetKeyDown(KeyCode.PageUp) )
			{
				SetDebugTimeMultiplier( GetDebugTimeMultiplier() + 0.2f );
			}
			if ( PowerQuest.GetDebugKeyHeld() && Input.GetKeyDown(KeyCode.End) )
			{
				SetDebugTimeMultiplier( 1.0f );
			}
		}
	
	}
	
	
	void ClearTimeMultipliers()
	{
		m_timeScaleMultipliers.Clear();		
		Time.timeScale = 1.0f;
		Time.fixedDeltaTime = m_timeFixedOriginal * 1.0f;
	}
	
	void UpdateTimeMultipliers()
	{
		float timeMultiplier = 1.0f;		
		
		if ( m_gamePaused == false )
		{
			
			float realTimeDelta = Time.realtimeSinceStartup - m_timeSinceLastFrame;
			m_timeSinceLastFrame = Time.realtimeSinceStartup;

			if (m_timeScaleMultipliers.Count > 0) 
			{
				var keys = new List<string>(m_timeScaleMultipliers.Keys);

				foreach (string key in keys) 
				{
					float timeRemaining = m_timeScaleMultipliers[key].m_time;

					if (timeRemaining > 0) 
					{
						timeRemaining -= realTimeDelta;

						if (timeRemaining <= 0) 
						{
							m_timeScaleMultipliers.Remove(key);
						}
						else 
						{
							m_timeScaleMultipliers[key].m_time = timeRemaining;
							timeMultiplier = Mathf.Min(timeMultiplier, m_timeScaleMultipliers[key].m_mult);
						}
					} 
					else 
					{
						timeMultiplier = Mathf.Min(timeMultiplier, m_timeScaleMultipliers[key].m_mult);
					}
				}
			}
			/*  Time mult smoothing (disabled for now) /
			if ( timeMultiplier > 0.0f )
			{
				timeMultiplier = Time.timeScale + ( (timeMultiplier - Time.timeScale) * realTimeDelta * 5.0f );
			}
			/**/
		}
				
		timeMultiplier *= m_debugTimeMultiplier;
		Time.timeScale = timeMultiplier;
		Time.fixedDeltaTime = m_timeFixedOriginal * timeMultiplier;
	}

	// Returns true if the 
	public bool IsLayerPauseable(int layer)
	{
		if ( m_layersNoPause == 0 )
		{
			m_layersNoPause = LayerMask.GetMask(m_ignoredLayers);
			/* // This is more efficient but needs more testing if it's necessary to use it for speed
			List<System.Type> typeList = new List<System.Type>();
			List<string> typeNameList = new List<string>();

			foreach( string typeName in m_ignoredComponents )
			{
				System.Type ignoredType = System.Type.GetType(typeName);
				if ( ignoredType != null )
					typeList.Add(ignoredType);
				else 
					typeNameList.Add(typeName);
			}
			m_ignoredTypes = typeList.ToArray();

			// Copy over the ignored components list with the new one that excludes any we cached the types for.
			// This allows you to have some type names with namespaces for efficiency, and others without (for robustness)
			m_ignoredComponents = typeNameList.ToArray();
			*/
		}
		return  (m_layersNoPause & 1 << layer ) == 0;
	}

	#endregion
	
}
}

