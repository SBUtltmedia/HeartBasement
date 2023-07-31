using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterHardwareClerk : CharacterScript<CharacterHardwareClerk>
{
	 
	
	
	IEnumerator OnLookAt()
	{
		yield return C.Dave.Say("There's Doc, the titular owner of Doc's Hardware", 14);
		yield return E.Break;
	}

	IEnumerator OnInteract()
	{
		yield return C.Dave.WalkTo(Point("HWCounterPosition"));
		yield return C.Dave.Face(eFace.Right);
		
		if ( Globals.gameStage >= gameProgress.SecondFlood ) {
			yield return E.HandleOption( D.DialogWithClerk, "3");
		
		} else if (D.DialogWithClerk.GetOption(2).Used) {
		
			 D.BuyOptions.Start();
		 } else if (D.DialogWithClerk.GetOption(1).Used && Globals.gameStage <= gameProgress.UsedBucket) {
			 yield return C.HardwareClerk.Say("Good luck.", 0);
		 } else if (D.DialogWithClerk.GetOption(1).Used){
			 yield return E.HandleOption( D.DialogWithClerk, "2");
		 } else {
			 yield return E.HandleOption( D.DialogWithClerk, "1");
		
		 }
		
		
		//D.DialogWithClerk.Start();
		yield return E.Break;
	}

	IEnumerator OnUseInv( IInventory item )
	{

		yield return E.Break;
	}
}