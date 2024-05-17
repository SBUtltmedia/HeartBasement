using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools.Quest;


public partial class GlobalScript : GlobalScriptBase<GlobalScript>
{

}

public class GlobalScriptBase<T> : QuestScript where T : QuestScript
{
	// Allows access to specific room's script by calling eg. GlobalScript.Script instead of E.GetScript<GlobalScript>()
	// This is needed when hotloading to reference the correct assembly
	public static T Script { get {return E.GetScript<T>(); } }


}
