using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiChapterSelect : GuiScript<GuiChapterSelect>
{

	void LoadChapter(int chapter){
		Globals.LoadingChapter = true;
		Globals.gameStage = (gameProgress) chapter;
		if(chapter <= 5){
			Globals.rained = false;
		}
		Globals.SetInventory();
		//E.Restart(R.Home);
		C.Dave.ChangeRoom(R.Home);
	}

	void OnShow()
	{
		
	}

	IEnumerator OnClickBack( IGuiControl control )
	{
		G.ChapterSelect.Hide();
		G.TitleMenu.Show();
		yield return E.Break;
	}

	IEnumerator OnClick0( IGuiControl control )
	{
		//Globals.ChapterToLoad = 0;
		//E.Restart(R.Home,"LoadChapter(0)");
		
		LoadChapter(0);
		yield return E.Break;
	}
	IEnumerator OnClick1( IGuiControl control )
	{
		//Globals.ChapterToLoad = 1;
		//E.Restart(R.Home,"LoadChapter(1)");		
		LoadChapter(1);
		yield return E.Break;
	}
	IEnumerator OnClick2( IGuiControl control )
	{
		//E.Restart(R.Home,"GuiChapterSelect.LoadChapter(2)");
		LoadChapter(2);
		yield return E.Break;
	}
	IEnumerator OnClick3( IGuiControl control )
	{
		LoadChapter(3);
		yield return E.Break;
	}
	IEnumerator OnClick4( IGuiControl control )
	{
		LoadChapter(4);
		yield return E.Break;
	}
	IEnumerator OnClick5( IGuiControl control )
	{
		LoadChapter(5);
		yield return E.Break;
	}
	IEnumerator OnClick6( IGuiControl control )
	{
		LoadChapter(6);
		yield return E.Break;
	}
	IEnumerator OnClick7( IGuiControl control )
	{
		LoadChapter(7);
		yield return E.Break;
	}
	IEnumerator OnClick8( IGuiControl control )
	{
		LoadChapter(8);
		yield return E.Break;
	}
	IEnumerator OnClick9( IGuiControl control )
	{
		LoadChapter(9);
		yield return E.Break;
	}
}