using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomNewHouse : RoomScript<RoomNewHouse>
{


	void OnEnterRoom()
	{
		C.Dave.SetPosition(Point("spawnpoint"));
	}

	IEnumerator OnEnterRegionScale( IRegion region, ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropHouse( IProp prop )
	{
		yield return C.Dave.WalkTo(Point("doorway"));
		Globals.gameStage = gameProgress.WonGame;
		yield return E.Wait();
		yield return E.WaitForGui(G.Explanation);
		E.Restart();
		yield return E.Break;
	}
}