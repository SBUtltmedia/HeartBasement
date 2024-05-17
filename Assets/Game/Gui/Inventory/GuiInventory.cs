using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiInventory : GuiScript<GuiInventory>
{


	IEnumerator OnClickNewButtonText( IGuiControl control )
	{

		yield return E.Break;
	}
}