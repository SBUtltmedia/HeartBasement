using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiToolbar : GuiScript<GuiToolbar>
{



	IEnumerator OnClickNewButtonText( IGuiControl control )
	{
		 E.Save(1, "test");
		C.Dave.ChangeRoom(R.Menu);
		yield return E.Break;
		
	}
}