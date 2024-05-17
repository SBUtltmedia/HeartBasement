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
			C.Dave.ChangeRoomBG(R.Home);
		}
		IButton startButton = (IButton) G.TitleMenu.GetControl("StartGame");
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