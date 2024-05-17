using UnityEngine;
using System.Collections;
using PowerScript;

namespace PowerTools.Quest
{

[System.Serializable]
public class RoomScript<T> : QuestScript where T : QuestScript
{
	// Allows access to specific room's script by calling eg. RoomKitchen.Script instead of E.GetScript<RoomKitchen<()
	public static T Script { get {return E.GetScript<T>(); } }

	/*! Debug function, true when the player has hit play and started in this room. Useful in OnEnter functions when you want to set up some debugging code.
	 * Eg. 
	 *		if ( EnteredFromEditor )
	 *		{
	 *			// When debugging from this room, set some stuff up
	 *			I.ItemPlayerShouldHaveByNow.Owned = true;
	 *			Plr.AnimIdle = "WearingHatIdle";
	 *			m_someVariableThatWouldHaveBeenSet = true;
	 *		}
	 */ 
	protected bool EnteredFromEditor { get => R.EnteredFromEditor; }

	/// True the first time the player has visited the room. Shortcut to R.Current.FirstTimeVisited
	protected bool FirstTimeVisited { get => R.Current.FirstTimeVisited; }
}
}