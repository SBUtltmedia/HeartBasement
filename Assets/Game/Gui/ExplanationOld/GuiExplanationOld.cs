using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiExplanationOld : GuiScript<GuiExplanationOld>
{


	IEnumerator OnAnyClick( IGuiControl control )
	{
		G.ExplanationOld.Hide();
		G.Inventory.Show();
		yield return E.Break;
	}

	IEnumerator OnClickNewButtonText( IGuiControl control )
	{
		G.ExplanationOld.Hide();
		
		yield return E.Break;
	}

	void OnShow()
	{
		G.Inventory.Hide();
		
		string image = "Slide" + (int) Globals.gameStage;
		Image("EndChapter2").Anim = image;
		Debug.Log(image);
	}
}