using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiTitleMenu : GuiScript<GuiTitleMenu>
{


	IEnumerator OnClickChapterSelect( IGuiControl control )
	{
		G.TitleMenu.Hide();
		G.ChapterSelect.Show();
		yield return E.Break;
	}

	IEnumerator OnClickStartGame( IGuiControl control )
	{
		G.TitleMenu.Hide();
		
		if (!E.RestoreSave(1)){
			yield return C.Dave.ChangeRoom(R.Home);
		} else {
			Debug.Log("save restored");
		}
		
		IButton startButton = (IButton) G.TitleMenu.GetControl("StartGame");
		
		/*
		if (startButton.Text == "Start Game"){
			 yield return C.Dave.ChangeRoom(R.Home);
		} else {
			yield return C.Dave.ChangeRoom(R.Previous);
			C.Dave.Position = Globals.continue_pos;
		}
		*/
		startButton.Text = "Continue";
		
		
		yield return E.Break;
	}

	IEnumerator OnClickRestartGame( IGuiControl control )
	{
		
		E.Restart();
		yield return E.Break;
	}

	void OnShow()
	{
		if (!Globals.testflag) {
			G.TitleMenu.GetControl("ChapterSelect").Hide();
		}
		
		if (E.GetSaveSlotData().Count ==0 ){
			G.TitleMenu.GetControl("RestartGame").Hide();
		} else {
			G.TitleMenu.GetControl("RestartGame").Show();
		}
	}
}