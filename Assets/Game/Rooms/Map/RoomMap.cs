using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomMap : RoomScript<RoomMap>
{
	
	GameObject rain;
	
	bool firstExit = true;
	void OnEnterRoom()
	{
		rain = GameObject.Find("Rain");
		rain.SetActive(false);
		
		Audio.PlayMusic("Map1", 2);
		
		C.Dave.SetPosition(Point(string.Format("{0}Point",C.Dave.LastRoom.ScriptName)));
		
		
		if (Globals.gameStage >= gameProgress.BoughtHouse){
			R.Map.ActiveWalkableArea = 2;
			Prop("Back").Animation = "HeartBasementMapEnd";
			Prop("NewHouse").Clickable = true;
		}else if (Globals.gameStage > gameProgress.SecondFlood) {
			R.Map.ActiveWalkableArea = 1;
			Prop("Back").Animation = "HeartBasementMapFlooded";
		} else {
			R.Map.ActiveWalkableArea = 0;
			Prop("Back").Animation = "HeartBasementMapNew";
		}
		
		
		// if (C.Dave.LastRoom == R.Hardware) {
		//	 C.Dave.SetPosition(Point("HardwarePoint"));
		// } else if (C.Dave.LastRoom == R.Home){
		//	 C.Dave.SetPosition(Point("HomePoint"));
		// } else {
		//	 C.Dave.SetPosition(Point("StartingPoint"));
		// }
		
		// // Display:  The hardware store is to the north
		// if ( R.Current.FirstTimeVisited ) {
		//  C.Dave.Say(" What a trip!", 32);
		// }
		
		
		
		
		//GUI
		G.Inventory.Hide();
	}

	IEnumerator OnInteractPropHome( IProp prop )
	{
		yield return C.Dave.WalkTo(Point("HomePoint"));
		C.Dave.ChangeRoomBG(R.Home);
		G.Inventory.Show();
		yield return E.Break;
	}

	IEnumerator OnInteractPropHardware( IProp prop )
	{
		yield return C.Dave.WalkTo(Point("HardwarePoint"));
		C.Dave.ChangeRoomBG(R.Hardware);
		G.Inventory.Show();
		yield return E.Break;
	}



	IEnumerator OnInteractPropNeighbor1( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtPropHardware( IProp prop )
	{

		yield return C.Display(" The Hardware Store", 7);
		yield return E.Break;
	}

	IEnumerator OnLookAtPropHome( IProp prop )
	{
		yield return C.Display(" Home", 8);
		yield return E.Break;
	}

	IEnumerator OnLookAtPropNeighbor1( IProp prop )
	{
		yield return C.Display(" Jim's House", 9);
		yield return E.Break;
	}


	IEnumerator OnLookAtCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterDave( ICharacter character, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterTony( ICharacter character, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterTony( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterHardwareClerk( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterHardwareClerk( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterHardwareClerk( ICharacter character, IInventory item )
	{

		yield return E.Break;
	}


	public IEnumerator Thunderstorm()
	{
		rain.SetActive(true);
		C.Dave.Visible = false;
		//yield return E.FadeIn(1);
		
		yield return Prop("Back").Fade(1,(float) 0.75, 1.5f,eEaseCurve.Smooth);
		AudioHandle RainAudio = Audio.Play("Rain");
		yield return Prop("Back").Fade(0.75f,(float) 0.50, 1.5f,eEaseCurve.Smooth);
		
		yield return E.Wait(3);
		yield return E.FadeOut(1);
		rain.SetActive(false);
		
		//C.Dave.Visible = true;
		RainAudio.volume = 0.1f;
		C.Dave.ChangeRoom(R.Home);
		
		E.FadeInBG(1);
		Prop("Back").FadeBG(1,1, 0,eEaseCurve.Smooth);
		
		//Debug.Log("HEY");
		yield return E.Break;
	}

	IEnumerator OnEnterRoomAfterFade()
	{
		if(!Globals.rained && (int)Globals.gameStage == 6){
			yield return Thunderstorm();
		}
		if (firstExit && C.Dave.LastRoom == R.Home && Globals.gameStage == gameProgress.FixedPump){
			yield return C.Dave.Say("The road is flooded!", 78);
			yield return C.Dave.Say(" The only other way to Doc's is over that huge hill...", 80);
			yield return C.Dave.Say("Better get moving.", 82);
			firstExit = false;
		 }
		yield return E.Break;
	}

	IEnumerator OnExitRoom( IRoom oldRoom, IRoom newRoom )
	{
		G.Inventory.Show();
		yield return E.Break;
	}

	IEnumerator OnEnterRegionByhouse( IRegion region, ICharacter character )
	{
		yield return C.Dave.Say(" What's this?", 84);
		yield return C.Dave.Say(" The house on the hill is for sale!", 86);
		yield return C.Dave.Say(" I'm sure whoever lives there doesn't have to deal with any flooding.", 88);
		yield return C.Dave.Say(" Better save the real estate agent's number.", 90);
		yield return C.Dave.WalkTo(Point("HardwarePoint"));
		C.Dave.ChangeRoom(R.Hardware);
		Region("Byhouse").Enabled = false;
		yield return E.Break;
	}

	IEnumerator OnInteractPropNewHouse( IProp prop )
	{
		if (Globals.gameStage > gameProgress.BoughtHouse){
			yield return C.Dave.WalkTo(Point("NewHousePoint"));
			C.Dave.ChangeRoom(R.NewHouse);
		} else {
			yield return C.Dave.Say(" There's my new house", 132);
			yield return C.Dave.Say(" I can't wait to move in!", 133);
		}
		yield return E.Break;
	}
}