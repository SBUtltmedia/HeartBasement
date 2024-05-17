using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiExplanation : GuiScript<GuiExplanation>
{


	void OnShow()
	{
		G.Inventory.Hide();
		
		string image = "Slide" + ((int) Globals.gameStage);
		Image("SlideImage").Anim = image;
		Debug.Log(image);
		
		
		
	}

	IEnumerator OnClickContinueButton( IGuiControl control )
	{
		G.Explanation.Hide();
		G.Inventory.Show();
		yield return E.Break;
		
	}
}