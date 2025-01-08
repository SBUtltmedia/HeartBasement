using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using PowerTools;

namespace PowerTools.Quest
{


public partial class PowerQuest
{

	/* Some fun random code for making enum state easier to set/query. Don't have to worry about instance name of state, just the enum
		- It's not a whole lot quicker to type SetEnum(eStateBlah.Whatever); than m_stateBlah = eStateBlah.Whatever. 
		- Also, when exclusively using these enums, you get a warning on the base.
		- in short, it's not really much easier. Better autocomplete would solve it. (eg: if "m_state = " autocompleted the 'eState' bit for you via reflection )
			- Also, making m_ selected when double clicking in QuestScript editor
	*/

	///  Returns true if this script has an enum with the specified enum value. Usage: `if ( At(eDoor.Unlocked) ) {...}`. This is experimental shorthand for: `if ( m_door == eDoor.Unlocked ) {...}`
	//public bool At<tEnum>(tEnum enumState) where tEnum : struct, System.IConvertible, System.IComparable
	//{	
	//	// Use reflection to find enum type instance and check it matches		
	//	return GetEnum<tEnum>().Equals(enumState);
	//}	
	public bool At<tEnum>(params tEnum[] enumStates) where tEnum : struct, System.IConvertible, System.IComparable
	{	
		tEnum state = GetEnum<tEnum>();
		return System.Array.Exists(enumStates, item=> state.Equals(item));		
	}	

	///  Returns true if this script has an enum with the specified enum value. Usage: `if ( In(eDoor.Unlocked) ) {...}`. This is experimental shorthand for: `if ( m_door == eDoor.Unlocked ) {...}`
	//public bool Is<tEnum>(tEnum enumState) where tEnum : struct, System.IConvertible, System.IComparable
	//{	
	//	// Use reflection to find enum type instance and check it matches		
	//	return GetEnum<tEnum>().Equals(enumState);
	//}	
	public bool Is<tEnum>(params tEnum[] enumStates) where tEnum : struct, System.IConvertible, System.IComparable
	{	
		tEnum state = GetEnum<tEnum>();
		return System.Array.Exists(enumStates, item=> state.Equals(item));		
	}

	/// Returns true if reached state (same as m_state >= eState.myState )
	public bool Reached<tEnum>(tEnum enumState) where tEnum : struct, System.IConvertible, System.IComparable
	{	
		// Use reflection to find enum type instance and check it matches		
		return GetEnum<tEnum>().CompareTo(enumState) >= 0;
	}	
	/// Returns true if passed state (same as m_state > eState.myState )
	public bool After<tEnum>(tEnum enumState) where tEnum : struct, System.IConvertible, System.IComparable
	{	
		// Use reflection to find enum type instance and check it matches		
		return GetEnum<tEnum>().CompareTo(enumState) > 0;
	}		
	/// Returns true if haven't reached state (same as m_state < eState.myState )
	public bool Before<tEnum>(tEnum enumState) where tEnum : struct, System.IConvertible, System.IComparable
	{	
		// Use reflection to find enum type instance and check it matches		
		return GetEnum<tEnum>().CompareTo(enumState) < 0;
	}

	/// Returns true if reached first state, but before second state (same as m_state >= eState.first && m_state < eState.second )
	//public bool Between<tEnum>(tEnum firstInclusive, tEnum secondExclusive) where tEnum : struct, System.IConvertible, System.IComparable
	//{	
	//	// Use reflection to find enum type instance and check it matches
	//	return Reached(firstInclusive) && Before(secondExclusive);
	//}

	/// Sets an enum in this script with the specified value. Usage: `SetEnum(eDoor.Unlocked);`. This is experimental shorthand for: `m_door = eDoor.Unlocked;`
	public void Set<tEnum>(tEnum enumState) where tEnum : struct, System.IConvertible
	{
		// Use reflection to find enum type instance and set it
		FieldInfo[] fields = m_globalScript.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		FieldInfo info = System.Array.Find(fields, item=>item.FieldType == typeof(tEnum));
		if ( info != null )
		{
			info.SetValue(m_globalScript,enumState);		
		}
		else
		{
			// Try room
			//Debug.Log("set enum room: "+enumState.ToString());
			fields = m_currentRoom.GetScript().GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			info = System.Array.Find(fields, item=>item.FieldType == typeof(tEnum));
			
			if ( info != null )
				info.SetValue(m_currentRoom.GetScript(),enumState);	
			else
				Debug.Log("Failed to set enum: "+enumState.ToString());
		}	
	}

	tEnum GetEnum<tEnum>() where tEnum : struct, System.IConvertible
	{		
		FieldInfo[] fields = m_globalScript.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		FieldInfo info = System.Array.Find(fields, item=>item.FieldType == typeof(tEnum));
		if ( info != null )
			return (tEnum)info.GetValue(m_globalScript);
		
		// Try room
		fields = m_currentRoom.GetScript().GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		info = System.Array.Find(fields, item=>item.FieldType == typeof(tEnum));
		if ( info != null )
			return (tEnum)info.GetValue(m_currentRoom.GetScript());
		else 
			Debug.Log("Failed to find enum: "+typeof(tEnum).ToString());
		return default(tEnum);
	}
}


}
