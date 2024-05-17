using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryMediumHose : InventoryScript<InventoryMediumHose>
{
    public bool isHose = true;

	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		Prop("Hose").Clickable = true;
		yield return E.Break;
	}
}