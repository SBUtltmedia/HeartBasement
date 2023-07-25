using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryRepairKit : InventoryScript<InventoryRepairKit>
{


	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		C.Dave.AddInventory(I.Wrench);
		C.Dave.AddInventory(I.Washer);
		C.Dave.RemoveInventory(I.RepairKit);
		yield return C.Dave.Say(" Oh boy!", 126);
		yield return C.Dave.Say(" A washer and a wrench!", 127);
		yield return E.Break;
	}
}