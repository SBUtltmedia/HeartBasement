using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

namespace PowerTools.Quest
{


public static class QuestUtils
{


	public static float Ease( float ratio, eEaseCurve curve = eEaseCurve.InOutSmooth )
	{		
		if ( ratio <= 0 )
			return 0;
		if ( ratio >= 1)
			return 1;
			
		float x = ratio;
			
		// I think this is basically a cheap version of the sin wave one. It's what's used for perlin noise
		switch (curve)
		{
			case eEaseCurve.InSmooth:
			{
				ratio *= 0.5f;
				return (-2.0f*ratio*ratio*ratio + 3.0f*ratio*ratio) * 2.0f;
			} 

			case eEaseCurve.OutSmooth:
			{
				ratio = ratio * 0.5f + 0.5f;
				return (-2.0f*ratio*ratio*ratio + 3.0f*ratio*ratio) * 2.0f - 1.0f;
			} 

			case eEaseCurve.InOutSmooth:
			{
				return (-2.0f*ratio*ratio*ratio + 3.0f*ratio*ratio);
			} 

			case eEaseCurve.InSine:
			{
				return 1.0f - Mathf.Cos(ratio*Mathf.PI*0.5f);
			} 
			case eEaseCurve.OutSine:
			{
				return Mathf.Sin((x*Mathf.PI)*0.5f);
			} 
			case eEaseCurve.InOutSine:
			{
				return -(Mathf.Cos(x*Mathf.PI)-1)*0.5f;
			} 
			case eEaseCurve.InQuad:
			case eEaseCurve.OutQuad:
			case eEaseCurve.InOutQuad:
			case eEaseCurve.InCubic:
			case eEaseCurve.OutCubic:
			case eEaseCurve.InOutCubic:
			case eEaseCurve.InQuart:
			case eEaseCurve.OutQuart:
			case eEaseCurve.InOutQuart:
			case eEaseCurve.InQuint:
			case eEaseCurve.OutQuint:
			case eEaseCurve.InOutQuint:
			{				
				float pow = (((int)curve-(int)eEaseCurve.InQuad)/3)+2;
				int dir = ((int)curve-(int)eEaseCurve.InQuad)%3;
				//Debug.Log($"Enum: {curve}, pow: {pow}, dir: {dir}");
				if ( dir == 0)
				{
					return Mathf.Pow(ratio,pow);
				}
				else if ( dir == 1 )
				{
					return 1.0f - Mathf.Pow(1.0f-ratio, pow);
				}
				else 
				{
					return x < 0.5f ? Mathf.Pow(2,pow-1f)*Mathf.Pow(x,pow) :  1.0f - Mathf.Pow(-2f*x + 2f, pow) * 0.5f;
				}
			
			} 
			case eEaseCurve.InExp:
			{
				return Mathf.Pow(2f,(10*x) - 10f);
			} 
			case eEaseCurve.OutExp:
			{
				return 1.0f - Mathf.Pow(2f,-10f*x);
			} 
			case eEaseCurve.InOutExp:
			{
				return x < 0.5f ? Mathf.Pow(2f,(20f*x)-10f) * 0.5f : (2.0f - Mathf.Pow(2f,(-20f*x)+10f))*0.5f;
			} 
			case eEaseCurve.InElastic:
			{
				const float c4 = (2f * Mathf.PI) / 3f - 0.62734f;
				return -Mathf.Pow(2,10*x-10) * Mathf.Sin((x*10-10.75f)*c4);
			} 
			case eEaseCurve.OutElastic:
			{
				const float c4 = (2f * Mathf.PI) / 3f - 0.62734f;
				return Mathf.Pow(2,-10*x) * Mathf.Sin((x*10-0.75f)*c4)+1;
			} 
			case eEaseCurve.InOutElastic:
			{
				throw new System.NotImplementedException();
			} 
		}		

		// Linear, etc
		return ratio;
	}

	//
	// Reflection doodads
	//

	static readonly BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;	// NB: I added 'declared only' not sure if it'll break anything yet!

	/// Copies properties and variables from one class to another. NB: This is a shallow copy! Any lists or class references of the object will still point to the original! Copy them manually using CopyListFields(...)!
	public static void CopyFields<T>(T to, T from)
	{
		System.Type type = to.GetType();
		if (type != from.GetType()) return; // type mis-match

		FieldInfo[] finfos = type.GetFields(BINDING_FLAGS);
		
		foreach (var finfo in finfos) 
		{
			finfo.SetValue(to, finfo.GetValue(from));
		}
	}

	// Deep copies a list of class objects. Used for copying default inventory item data and points data when a character/room is initialised from its prefab
	public static List<T> CopyListFields<T>(List<T> from) where T : new()
	{
		// Deep copy inventory	
		List<T> result = new List<T>(from.Count);
		foreach ( T original in from )
		{
			T newItem = new T();
			QuestUtils.CopyFields(newItem, original);
			result.Add(newItem);	
		}
		return result;
	}

	// Deep copies a list of class objects. Used for copying default inventory item data and points data when a character/room is initialised from its prefab
	public static T[] CopyListFields<T>(T[] from) where T : new()
	{
		// Deep copy inventory	
		T[] result = new T[from.Length];
		int i=0;
		foreach ( T original in from )
		{
			T newItem = new T();
			QuestUtils.CopyFields(newItem, original);
			result[i]=newItem;	
			++i;
		}
		return result;
	}

	// Used to copy data when assembly has changed due to hotloading a script
	public static void CopyHotLoadFields<T>(T to, T from)
	{
		System.Type toType = to.GetType();

		// Match fields by name, since types are potentially different due to assembly change. this means that if type changes we'll assert
		FieldInfo[] finfos = toType.GetFields(BINDING_FLAGS);
		FieldInfo[] finfosFrom = from.GetType().GetFields(BINDING_FLAGS);
		foreach (FieldInfo finfo in finfos) 
		{
			var finfoFrom = System.Array.Find(finfosFrom, item=>item.Name == finfo.Name);
			if ( finfoFrom != null )
			{	
				System.Type fieldType = finfo.ReflectedType;
				try
				{	
					object fromValue = finfoFrom.GetValue(from);
					if ( fromValue is System.Enum ) // Enums could be in a different assembly, so the type won't match, but can just cast from int to handle most cases.
						finfo.SetValue( to, (int)fromValue );
					else
						finfo.SetValue( to, fromValue );

				}
				catch (System.Exception e )
				{
					// Doesn't matter so m uch if we can't copy some data for hotswap
					Debug.LogWarning($"Hotloading script warning: {e.ToString()}"); 
				}
			}
		}
	}

	// Copies variables from a newly instantiated version to the passed in class
	public static void InitWithDefaults<T>( T toInit ) where T : class
	{

		/*
			Ok, doing some crazy stuff here, so bear with me-
				If a variable was added since the game was saved and we want to load it back, the varibale won't be set (it's not in the save file). But it won't even have it's default variable either.
				This function gets called before the class is deserialised, so I'm constructing a fresh instance of this class, and copying the defaults from that.
				Then when it's deserialised, the missing/ignored values will still have been set up.

			Note that it doesn't work with monobehaviours since they need to be created by unity, (not new'd)
		*/

		T newInstance = System.Activator.CreateInstance(toInit.GetType()) as T;
		if ( newInstance != null )
		{
			// Now shallow copy everything from the newInstance, to toInit
			CopyFields(toInit,newInstance);
		}

	}

	public static void HotSwapScript<T>( ref T toSwap, string name, Assembly assembly ) where T : class
	{
		if (toSwap == null)
			return;			
		T old = toSwap;
		toSwap = QuestUtils.ConstructByName<T>(name, assembly);
		CopyHotLoadFields(toSwap, old);
	}

	// Instantiates and returns a class by it's name, returning null if it wasn't found. Returns as the templated type (eg. base type of class you're instantiating)
	public static T ConstructByName<T>(string name) where T : class
	{		
		T result = null;
		try 
		{	
			#if UNITY_2018_1_OR_NEWER // Added for .NET 2.0 core support
				System.Type type = System.Type.GetType( string.Format("{0}, {1}", name,  typeof(PowerQuest).Assembly.FullName ));
				result = type.GetConstructor(new System.Type[]{}).Invoke(new object[]{}) as T;
			#else 
				System.Runtime.Remoting.ObjectHandle handle = System.Activator.CreateInstance("Assembly-CSharp", name, new object[0]);
				if ( handle != null )
				{
					result = handle.Unwrap() as T;
				}
			#endif
		} 
		catch
		{
			// Assume that this just means the class doesn't exist, which is fine, we'll just return null.
		}
		return result;
	}
	// Instantiates and returns a class by it's name, returning null if it wasn't found. Returns as the templated type (eg. base type of class you're instantiating)
	public static T ConstructByName<T>(string name, Assembly assembly) where T : class
	{
		T result = null;
		try 
		{	
			#if UNITY_2018_1_OR_NEWER // Added for .NET 2.0 core support
				System.Type type = System.Type.GetType( string.Format("{0}, {1}", name, assembly.FullName ));
				result = type.GetConstructor(new System.Type[]{}).Invoke(new object[]{}) as T;
			#else 
				System.Runtime.Remoting.ObjectHandle handle = System.Activator.CreateInstance(assembly.GetName().ToString(), name);//,name, new object[0])
				if ( handle != null ) 
				{
					result = handle.Unwrap() as T;
				}
			#endif
		} 
		catch
		{
			// Assume that this just means the class doesn't exist, which is fine, we'll just return null.
		}
		return result;
	}

	// Diagnostic code
	
	#if UNITY_EDITOR || DEVELOPMENT_BUILD
	static System.Diagnostics.Stopwatch s_stopwatch = new System.Diagnostics.Stopwatch();
	#endif
	public static void StopwatchStart() 
	{ 
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		s_stopwatch.Start(); 
		#endif
	}
	public static void StopwatchStop(string logTxt) 
	{ 
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		// Get the elapsed time as a TimeSpan value.
		TimeSpan ts = s_stopwatch.Elapsed;

		// Format and display the TimeSpan value.
		string elapsedTime = String.Format("{0:00}:{1:000}",
			ts.Seconds,
			ts.Milliseconds);
		Debug.Log(logTxt + elapsedTime);		
		s_stopwatch.Reset();
		#endif
	}

	/**
	 * Find a scriptable by ScriptName, null if not found
	 */
	public static T FindScriptable<T>(List<T> scriptables, string scriptName) where T : class, IQuestScriptable 
	{
		foreach (var scriptable in scriptables) 
		{
			if (scriptable != null && string.Equals(scriptable.GetScriptName(), scriptName, StringComparison.OrdinalIgnoreCase)) 
				return scriptable;
		}
		return null;
	}
	
	/**
	 * Find a scriptable monobehaviour by ScriptName, null if not found. (NB: When a scriptable is also a component this function must be used to avoid edge case null errors)
	 */
	public static T FindScriptableMono<T>(List<T> scriptables, string scriptName) where T : MonoBehaviour, IQuestScriptable 
	{
		foreach (var scriptable in scriptables) 
		{
			if (scriptable != null && string.Equals(scriptable.GetScriptName(), scriptName, StringComparison.OrdinalIgnoreCase)) 
				return scriptable;
		}
		return null;
	}

	public static T FindByName<T>(List<T> objects, string name) where T : UnityEngine.Object 
	{
		foreach (var obj in objects) 
		{
			if (obj != null && string.Equals(obj.name, name, StringComparison.OrdinalIgnoreCase)) 
				return obj;
		}

		return null;
	}

}

// Attribute used for including global enums in autocomplete
[AttributeUsage(AttributeTargets.All)]
public class QuestAutoCompletableAttribute : System.Attribute
{
	public QuestAutoCompletableAttribute(){}
}

// Attribute used for adding functions to debug startup fucntions
[System.AttributeUsage( System.AttributeTargets.Method )]
public class QuestPlayFromFunctionAttribute : System.Attribute
{
	public QuestPlayFromFunctionAttribute(){}
}

// Attribute used for adding functions to debug startup fucntions
[System.AttributeUsage( System.AttributeTargets.Method )]
public class QuestScriptEditorIgnoreAttribute : System.Attribute
{
	public QuestScriptEditorIgnoreAttribute(){}
}

// Attribute used to mark string fields that should be added to the text system to support localization. Eg: [QuestLocalize, SerializeField] string m_description;
[AttributeUsage(AttributeTargets.Field)]
public class QuestLocalizeAttribute : System.Attribute
{
	public QuestLocalizeAttribute(){}
}

}
