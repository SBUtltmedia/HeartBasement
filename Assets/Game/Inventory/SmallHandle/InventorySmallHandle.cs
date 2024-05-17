using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventorySmallHandle : InventoryScript<InventorySmallHandle>
{
    public bool isHandle = true;

	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		Prop("Handle").Clickable = true;
		yield return E.Break;
	}
}