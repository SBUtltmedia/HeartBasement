using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryLargeHandle : InventoryScript<InventoryLargeHandle>
{

	public bool isHandle = true;

	IEnumerator OnLookAtInventory( IInventory thisItem )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		Prop("Handle").Clickable = true;
		yield return E.Break;
	}

	IEnumerator OnUseInvInventory( IInventory thisItem, IInventory item )
	{

		yield return E.Break;
	}
}