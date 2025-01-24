using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiMenuButton : GuiScript<GuiMenuButton>
{


	IEnumerator OnClickNewButtonText( IGuiControl control )
	{
		 E.Save(1, "test");
		 Debug.Log("saved game");
		 Debug.Log(E.GetSaveSlotData());
		
		Globals.continue_pos = C.Dave.Position;
		
		yield return C.Dave.ChangeRoom(R.Menu);
		
		
		yield return E.Break;
	}
}