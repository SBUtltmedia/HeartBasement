using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using System.Reflection;

namespace PowerTools.Quest
{

/// This is experimental, will probably be made into something simpler to use
public class QuestAnimationTriggers : MonoBehaviour
{

	[Tooltip("AnimShake(int shakeDataIndex)")]
	[SerializeField] CameraShakeData[] m_shakeData = null;

	 Dictionary<string, System.Action> m_animCallbacks = new Dictionary<string, System.Action>(System.StringComparer.OrdinalIgnoreCase);
	 Dictionary<string, System.Action> m_animCallbacksTemp = new Dictionary<string, System.Action>(System.StringComparer.OrdinalIgnoreCase);

	bool m_character = false;

	void Awake()
	{
		m_character = GetComponentInParent<CharacterComponent>() != null;		
	}

	// Use this to set a callback when something's hit in an animation. In the anim add a "Trigger" tag with a "triggerName" parameter. Once the tag is called, it will be removed.
	public void AddTrigger(string triggerName, System.Action action, bool removeAfterTrigger)
	{
		if ( removeAfterTrigger )
			m_animCallbacksTemp[triggerName] = action;
		else 
			m_animCallbacks[triggerName] = action;
	}

	public void RemoveTrigger(string triggerName)
	{
		m_animCallbacks.Remove(triggerName);
		m_animCallbacksTemp.Remove(triggerName);
	}


	void AnimShake(int index)
	{
		if ( m_shakeData.IsIndexValid(index) )
		{
			PowerQuest.Get.GetCamera().Shake(m_shakeData[index]);
		}
	}

	bool AnimTrigger(string name)
	{
		System.Action callback = null;
		if ( m_animCallbacksTemp.TryGetValue(name, out callback) && callback != null )
		{
			callback.Invoke();
			m_animCallbacksTemp.Remove(name);
			return true;
		}
		if ( m_animCallbacks.TryGetValue(name, out callback) && callback != null )
		{
			callback.Invoke();
			return true;
		}
		return false;
	}

	#region Funcs: Anim Events

	// Hijack the _Anim event that PowerSprite uses internally to send custom messages, so can send them to quest scripts
	void _Anim(string function)
	{		
		/*
		Note: This will:
			First, check if there'a  trigger from "WaitForAnimTrigger" calls. The trigger has the "Anim" bit stripped.
			Then, try to call first the function name, then the function name without the Anim prefix, for:
				Character Script (if it's a character)
				Current Room Script
				Global script
		*/

		bool sent = false;

		string triggerName = function;
		bool hadPrefix = triggerName.StartsWith("Anim", System.StringComparison.OrdinalIgnoreCase);
		if ( hadPrefix )
			triggerName = triggerName.Substring(4);

		// Check trigger system for regisered events
		sent = AnimTrigger(triggerName);

		// Try calling function in the current room's script
		if ( sent == false )
		{				
			QuestScript roomScript = PowerQuest.Get.GetCurrentRoom().GetScript();
			if ( roomScript != null )
			{			
				System.Reflection.MethodInfo method = roomScript.GetType().GetMethod( function, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				if ( method != null ) 
				{
					method.Invoke( roomScript, null );					
					sent = true;
				}
				else if ( hadPrefix )
				{
					method = roomScript.GetType().GetMethod( triggerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					if ( method != null ) 
					{
						method.Invoke( roomScript, null );					
						sent = true;
					}
				}
			}
		}

		// If not found, try in character script
		if ( sent == false && m_character )
		{		
			CharacterComponent comp = GetComponentInParent<CharacterComponent>();	
			if ( comp != null && comp.GetData().GetScript() != null )
			{			
				System.Reflection.MethodInfo method = comp.GetData().GetScript().GetType().GetMethod( function, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

				if ( method != null ) 
				{
					method.Invoke( comp.GetData().GetScript(), null );
					sent = true;
				}
				else if ( hadPrefix )
				{
					method = comp.GetData().GetScript().GetType().GetMethod( triggerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					if ( method != null ) 
					{
						method.Invoke( comp.GetData().GetScript(), null );
						sent = true;
					}
				}
			}
		}

		// Finally, try calling function in global script
		if ( sent == false )
		{		
			QuestScript globalScript = PowerQuest.Get.GetGlobalScript();						
			System.Reflection.MethodInfo method = globalScript.GetType().GetMethod( function, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

			if ( method != null ) 
			{
				method.Invoke( globalScript, null );
				sent = true;
			}
			else if ( hadPrefix )
			{
				method = globalScript.GetType().GetMethod( triggerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

				if ( method != null ) 
				{
					method.Invoke( globalScript, null );
					sent = true;
				}
			}
		}

		/* NB: Removed 6/1/2021: it was causing tags to be called twice, shouldn't be necessary, since it'll be sent upwards by "SpriteAnim" component
		// Try sending message upwards 
		if ( sent == false )
			SendMessageUpwards(function, SendMessageOptions.DontRequireReceiver);
		*/
	}

	#endregion
}

}
