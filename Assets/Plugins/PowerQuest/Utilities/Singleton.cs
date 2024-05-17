using UnityEngine;

namespace PowerTools
{


// Singleton that automatically creates an instance if one didn't already exist
public class SingletonAuto<T> : MonoBehaviour where T : MonoBehaviour
{
	protected static T m_instance;

	// Call from the interhitor to set the singleton instance
	protected void SetSingleton()
	{
		if ( m_instance == null )
			m_instance = gameObject.GetComponent<T>();
	}
	
    // Returns the instance of this singleton.    
	public static T Get 
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = (T)FindObjectOfType(typeof(T));
				if (m_instance == null) 
				{
					GameObject container = new GameObject();
					container.name = typeof(T).ToString();
					m_instance = (T)container.AddComponent(typeof(T));
					if ( Application.isPlaying == false )
						container.hideFlags = HideFlags.HideAndDontSave;
				}
			}
			return m_instance;
		}		
	}
	public static T Instance {get{return Get; } }
	
	public static bool GetValid() { return m_instance != null; }
	public static bool HasInstance() { return m_instance != null; }
}

// Simple singleton system
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	protected static T m_instance; 

	// Call from the interhitor to set the singleton instance
	protected void SetSingleton()
	{
		if ( m_instance == null )
			m_instance = gameObject.GetComponent<T>();
	}
	protected void SetSingleton(T instance)
	{
		m_instance = instance;
	}
	
    // Returns the instance of this singleton.    
	public static T Get { get{ return m_instance; }	}	

	public static bool GetValid() { return m_instance != null; }
	public static bool HasInstance() { return m_instance != null; }
	public static bool Exists { get { return m_instance != null; } }
}

}