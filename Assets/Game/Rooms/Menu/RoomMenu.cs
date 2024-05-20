using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;
using System;

using System.Runtime.InteropServices;


public class RoomMenu : RoomScript<RoomMenu>
{

	// QueryString Code
	[DllImport("__Internal")]
	private static extern string getQueryString();



	void OnEnterRoom()
	{
		
		// QueryString Code
		try {
		Globals.urlQuery = getQueryString();
		
		Debug.Log(Globals.urlQuery);
		if (Globals.urlQuery.Contains("test"))
		{
			Globals.testflag = true;
		}
		} catch (Exception e) {}


		Audio.PlayMusic("Menu2", 1);
		//G.Inventory.Hide();
		G.MenuButton.Hide();
		G.TitleMenu.Show();




	}

	IEnumerator OnExitRoom(IRoom oldRoom, IRoom newRoom)
	{
		G.TitleMenu.Hide();
		G.ChapterSelect.Hide();
		G.MenuButton.Show();
		//G.Inventory.Show();

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterTony(ICharacter character)
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterTony(ICharacter character, IInventory item)
	{

		yield return E.Break;
	}
}