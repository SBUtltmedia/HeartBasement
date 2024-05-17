using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomHardware : RoomScript<RoomHardware>
{


	IEnumerator OnInteractHotspotDoor( IHotspot hotspot )
	{
		if (Globals.gameStage == gameProgress.None) {
		
			if (I.BilgePump.Owned){
		
				yield return C.Dave.Say("Let's take this back home and see how it works!", 10);
				yield return C.Dave.WalkTo(Point("HWDoorPosition"));
				C.Dave.ChangeRoomBG(R.Map);
			} else {
				yield return C.Dave.Say("I ain't leavin' til I've got somethin' to get that water outta my basement!", 11);
			}
		} else {
			yield return C.Dave.WalkTo(Point("HWDoorPosition"));
			C.Dave.ChangeRoomBG(R.Map);
		}
		
		yield return E.Break;
	}

	void OnEnterRoom()
	{
		C.Dave.Position = Point("HWDoorPosition");
		C.Dave.Face(eFace.Right, true);
		C.HardwareClerk.ChangeRoomBG(R.Hardware);
		C.HardwareClerk.Position = Point("HWClerkPosition");
		
		// Globals.gameStage = gameProgress.TriedPump1;
		
		Audio.PlayMusic("Hardware1", 2);
		
		if( Globals.gameStage >= gameProgress.RightParts)
		{
			C.Tony.Enable();
		}
		else {
			C.Tony.Disable();
		}
		
		
		
		//E.SetPlayer(C.Tony);
	}

	IEnumerator OnEnterRoomAfterFade()
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotDoor( IHotspot hotspot )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterHardwareClerk( ICharacter character )
	{
		
		yield return E.Break;
	}

	IEnumerator OnInteractPropBeerPack( IProp prop )
	{
		I.Beer.Add();
		Prop("BeerPack").Disable();
		
		yield return C.Display(" Beer has been added to your toolbolx.", 11);
		
		yield return E.Break;
	}

	IEnumerator OnInteractCharacterTony( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}



	IEnumerator OnEnterRegionScale( IRegion region, ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnExitRegionScale( IRegion region, ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterHardwareClerk( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("Used paint?! For only a dollar?! What a steal!", 61);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotQuestionBox( IHotspot hotspot )
	{
		yield return C.Dave.Say("I wonder what's in there.", 62);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotPokeball( IHotspot hotspot )
	{
		yield return C.Dave.Say("Poke a man? Now why would I do that?", 63);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotPokeball( IHotspot hotspot )
	{
		yield return C.Dave.Say("Poke a man? Now why would I do that?", 64);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotQuestionBox( IHotspot hotspot )
	{
		yield return C.Dave.Say("I wonder what's in there.", 65);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("Used paint?! For only a dollar?! What a steal!", 66);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotPOWBlock( IHotspot hotspot )
	{
		yield return C.Dave.Say(" Looks POWerful.", 67);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotPOWBlock( IHotspot hotspot )
	{
		yield return C.Dave.Say(" Looks POWerful.", 68);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotGoldRing( IHotspot hotspot )
	{
		yield return C.Dave.Say("Blue rodent not included.", 69);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotGoldRing( IHotspot hotspot )
	{
		yield return C.Dave.Say("Blue rodent not included.", 70);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotPipeBomb( IHotspot hotspot )
	{
		yield return C.Dave.Say("Oh. That's a pipe bomb.", 71);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotPipeBomb( IHotspot hotspot )
	{
		yield return C.Dave.Say("Oh. That's a pipe bomb.", 72);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBusterSword( IHotspot hotspot )
	{
		yield return C.Dave.Say("Lugging that hunk of iron around would be a work out in itself.", 73);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBusterSword( IHotspot hotspot )
	{
		yield return C.Dave.Say("Lugging that hunk of iron around would be a work out in itself.", 74);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotGhost( IHotspot hotspot )
	{
		yield return C.Dave.Say("Why so glum, chum?", 75);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotGhost( IHotspot hotspot )
	{
		yield return C.Dave.Say("Why so glum, chum?", 76);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotNuts( IHotspot hotspot )
	{
		yield return C.Dave.Say("A box of peanuts, walnuts, and pistachios.", 77);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotNuts( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBolts( IHotspot hotspot )
	{
		yield return C.Dave.Say("It's filled to the brim with lightning bolts.", 79);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBolts( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBanjosAndKazoos( IHotspot hotspot )
	{
		yield return C.Dave.Say("It's filled with banjos and kazoos. My favorite instruments!", 81);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBanjosAndKazoos( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotRedPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("A can of red paint. Definitely my favorite color.", 83);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotRedPaint( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotOrangePaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("A can of orange paint. Definitely my favorite color.", 85);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotOrangePaint( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotYellowPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("A can of yellow paint. Definitely my favorite color.", 87);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotYellowPaint( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotGreenPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("A can of green paint. Definitely my favorite color.", 89);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotGreenPaint( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBluePaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("A can of blue paint. Definitely my favorite color.", 91);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBluePaint( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotPurplePaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("A can of purple paint. Definitely my favorite color.", 93);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotPurplePaint( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotLampOil( IHotspot hotspot )
	{
		yield return C.Dave.Say("Lamp Oil.", 95);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotLampOil( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotRope( IHotspot hotspot )
	{
		yield return C.Dave.Say("It's rope. What did you expect?", 97);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotRope( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBombs( IHotspot hotspot )
	{
		yield return C.Dave.Say("I hope he has the correct permits to sell these.", 99);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBombs( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotSign( IHotspot hotspot )
	{
		yield return C.Dave.Say("Hey! I'm no chump...", 101);
		yield return E.WaitSkip();
		yield return C.Dave.Say("Am I?", 102);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotSign( IHotspot hotspot )
	{
		
		yield return E.Break;
	}
}