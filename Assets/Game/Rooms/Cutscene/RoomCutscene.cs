using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomCutscene : RoomScript<RoomCutscene>
{


	void Update()
	{
	}

	void OnEnterRoom()
	{
		
		G.Inventory.Hide();
	}

	IEnumerator OnInteractPropBack( IProp prop )
	{
		G.Inventory.Show();
		E.Restart();
		//C.Dave.SetPosition(returnPosition);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtPropBack( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropTextBack( IProp prop )
	{
		G.Inventory.Show();
		E.Restart();
		yield return E.Break;
	}
}