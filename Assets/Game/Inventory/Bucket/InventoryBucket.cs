using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryBucket : InventoryScript<InventoryBucket>
{


	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		if (Globals.tutorialStage== tutorialProgress.clickedBucket)
		{
			Globals.tutorialStage= tutorialProgress.selectedBucket;
		
			yield return C.Display(" Click on the water with the bucket to scoop it up.", 33);
		
			Prop("Water").Clickable = true;
			I.Bucket.SetActive();
		
		
		}
		yield return E.Break;
	}

	IEnumerator OnUseInvInventory( IInventory thisItem, IInventory item )
	{

		yield return E.Break;
	}
}