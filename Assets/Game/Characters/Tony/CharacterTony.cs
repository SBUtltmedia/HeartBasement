using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterTony : CharacterScript<CharacterTony>
{


	IEnumerator OnUseInv( IInventory item )
	{

		if (item == I.Beer)
		yield return E.Break;
	}

	IEnumerator OnLookAt()
	{
		yield return C.Dave.Say(" It's my buddy Tony. Maybe he can help.", 13);
		
		yield return E.Break;
	}

	IEnumerator OnInteract()
	{

		yield return E.Break;
	}
}