using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterDave : CharacterScript<CharacterDave>
{


	IEnumerator OnLookAt()
	{
		yield return C.Dave.Say(" My lucky day...", 12);
		string blah = SystemText.Localize("Save Game");
		Debug.Log(blah);
		yield return E.Break;
	}

	IEnumerator OnUseInv( IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnInteract()
	{

		yield return E.Break;
	}
}