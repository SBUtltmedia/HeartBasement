using UnityEngine;
using System.Collections;
using PowerScript;

namespace PowerTools.Quest
{

[System.Serializable]
public class CharacterScript<T> : QuestScript where T : QuestScript
{
	/// Used internally to access one script from another
	public static T Script { get { return E.GetScript<T>(); } }
}

}
