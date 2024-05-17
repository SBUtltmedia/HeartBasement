using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryMediumHandle : InventoryScript<InventoryMediumHandle>
{

	public bool isHandle = true;
	IEnumerator OnUseInvInventory( IInventory thisItem, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		Prop("Handle").Clickable = true;
		yield return E.Break;
	}
}