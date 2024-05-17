using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryBilgePump : InventoryScript<InventoryBilgePump>
{
	public enum handleType {small, medium, large};
	
	public enum hoseType {small, medium, large};

//	public IInventory hose = I.SmallHose;
//	public IInventory handle = I.SmallHandle;
	
	private string[] sizeString  = {"Small", "Medium", "Large"};

	handleType currentHandle = handleType.small;
	hoseType currentHose = hoseType.small;
	
	
	IEnumerator OnLookAtInventory( IInventory thisItem )
	{
		yield return C.Display(sizeString[(int)currentHandle] + " Handle\n" + sizeString[(int)currentHose] + " Hose" );

		yield return E.Break;
	}

	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		E.GetRoom("Home").GetProp("Water").Clickable = true;
		
		yield return E.Break;
	}

	IEnumerator OnUseInvInventory( IInventory thisItem, IInventory item )
	{ 
		
		
		
		if (item == I.SmallHandle) {
			string prevHandle = sizeString[(int)currentHandle];
			returnHandleToInv();
			currentHandle = handleType.small;
		
			I.SmallHandle.Remove();
			yield return C.Display(prevHandle + " Handle replaced with " + sizeString[(int)currentHandle] + " Handle");
		} else if (item == I.MediumHandle) {
			string prevHandle = sizeString[(int)currentHandle];
			returnHandleToInv();
			currentHandle = handleType.medium;
		
			I.MediumHandle.Remove();
			yield return C.Display(prevHandle + " Handle replaced with " + sizeString[(int)currentHandle] + " Handle");
		} else if (item == I.LargeHandle) {
			string prevHandle = sizeString[(int)currentHandle];
			returnHandleToInv();
			currentHandle = handleType.large;
		
			I.LargeHandle.Remove();
			yield return C.Display(prevHandle + " Handle replaced with " + sizeString[(int)currentHandle] + " Handle");
		} else if (item == I.SmallHose) {
			string prevHose = sizeString[(int)currentHose];
			returnHoseToInv();
			currentHose = hoseType.small;
			I.SmallHose.Remove();
			yield return C.Display(prevHose + " Hose replaced with " + sizeString[(int)currentHose] + " Hose");
		} else if (item == I.MediumHose) {
			string prevHose = sizeString[(int)currentHose];
			returnHoseToInv();
			currentHose = hoseType.medium;
			I.MediumHose.Remove();
			yield return C.Display(prevHose + " Hose replaced with " + sizeString[(int)currentHose] + " Hose");
		} else if (item == I.LargeHose) {
			string prevHose = sizeString[(int)currentHose];
			returnHoseToInv();
			currentHose = hoseType.large;
			I.LargeHose.Remove();
			yield return C.Display(prevHose + " Hose replaced with " + sizeString[(int)currentHose] + " Hose");
		} else {}
		
		yield return E.Break;
		/*  alternative way to simplify - doesnt work atm
		if (item == I.SmallHandle) {
			handle.Add();
			handle = item;
			item.Remove();
		} else if (item == I.MediumHandle) {
			handle.Add();
			handle = item;
			item.Remove();
		} else if (item == I.LargeHandle) {
			handle.Add();
			handle = item;
			item.Remove();
		} else if (item == I.SmallHose) {
			hose.Add();
			hose = item;
			item.Remove();
		} else if (item == I.MediumHose) {
			hose.Add();
			hose = item;
			item.Remove();
		} else if (item == I.LargeHose) {
			hose.Add();
			hose = item;
			item.Remove();
		} else {}
		*/
		
	}


// following functions add the handle or hose part currently on the pump to the inventory
	private void returnHandleToInv() {
		switch(currentHandle) {
			case handleType.small:
				I.SmallHandle.Add();
				break;
			case handleType.medium:
				I.MediumHandle.Add();
				break;
			case handleType.large:
				I.LargeHandle.Add();
				break;
			default:
				break;
		}
	}

	private void returnHoseToInv() {
		switch(currentHose) {
			case hoseType.small:
				I.SmallHose.Add();
				break;
			case hoseType.medium:
				I.MediumHose.Add();
				break;
			case hoseType.large:
				I.LargeHose.Add();
				break;
			default:
				break;
		}		
	}
}