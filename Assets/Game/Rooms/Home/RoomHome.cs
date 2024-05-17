using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomHome : RoomScript<RoomHome>
{
	// This area is where you can put variables you want to use for game logic in your room
	
	// Pump Part variables
	public enum handleType
	{
		small,
		medium,
		large
	};
	
	public enum hoseType
	{
		small,
		medium,
		large
	};
	
	private string[] sizeString = { "Small", "Medium", "Large" };

	private GameObject fountain;
	
	handleType currentHandle = handleType.small;
	hoseType currentHose = hoseType.small;
	
	public int pumpRepairs = 0;
	
	// Water level variables
	// public int waterLevelInt = (int)Globals.gameStage * 40;
	public int waterLevelInt = 0;
	/*
    public IEnumerator lowerWater()
    {
				waterLevelInt++;
				Prop("Back").Animation = "WaterLower" + waterLevelInt;
		        yield return E.Wait((float)1.0);
		        Prop("Back").Animation = "WaterLevel" + waterLevelInt;       
                yield return E.Break; 
    }
    */
    // enums like this are a nice way of keeping track of what's happened in a room
    enum eThingsYouveDone
    {
        Start,
        InsultedChimp,
        EatenSandwich,
        LoadedCrossbow,
        AttackedFlyingNun,
        PhonedAlbatross
    }

    eThingsYouveDone m_thingsDone = eThingsYouveDone.Start;

    public void OnEnterRoom()
    {
		// Put things here that you need to set up BEFORE the room fades in (but nothing "blocking")
		// Note, you can also just do this at the top of OnEnterRoomAfterFade
		
		// Set
		//C.Dave.MoveTo(Point("PumpPosition"));
		//Temp
		//LowerWaterShader(40, "CharacterDave");
		//Globals.gameStage = gameProgress.UsedBucket;
		//E.WaitForGui(G.ExplanationOld);
		//E.WaitForGui(G.Explanation);
		
		// End Temp
		fountain = GameObject.Find("Fountain");
		
		fountain.SetActive(false);
		
		
		Audio.PlayMusic("Basement1", 2);
		
		if (Globals.LoadingChapter) {
			Globals.LoadingChapter = false;
			Prop("Box").Disable();
			// Disable bucket if past tutorial
			if (Globals.gameStage > gameProgress.None){
				Prop("Bucket").Disable();
			} else {
				Prop("Bucket").Enable();
			}
			// Set proper hoses and pump if past replacing parts chapter
			if (Globals.gameStage >= gameProgress.RightParts) {
				currentHandle = handleType.large;
				currentHose = hoseType.large;
				Prop("Handle").SetPosition(190, -81 + (int)currentHandle*10);
				Prop("Hose").Animation ="HoseL";
			}
			// set electric pump
			if (Globals.gameStage <= gameProgress.SecondFlood){
			 Prop("ElectricPump").Disable();
			} else {
			Prop("ElectricPump").Enable();
			Prop("Pump").Disable();
			Prop("Hose").Disable();
			Prop("Handle").Disable();
			}
		
			// set pump
			if (Globals.gameStage > gameProgress.UsedBucket && Globals.gameStage < gameProgress.SecondFlood){
				Prop("Pump").Enable();
				Prop("Hose").Enable();
				Prop("Handle").Enable();
			} else {
				Prop("Pump").Disable();
				Prop("Hose").Disable();
				Prop("Handle").Disable();
			}
		
			// set tony to pump position
			if(Globals.gameStage == gameProgress.TonyPumped || Globals.gameStage== gameProgress.TonyAte){
				C.Tony.Enable();
				C.Tony.SetPosition(Point("PumpPosition"));
		
			} else{
				C.Tony.Disable();
			}
		
		
		}
		
		
		
		if (C.Tony.Room == R.Home)
		{
			C.Tony.SetPosition(
				new Vector2(Point("HomeDoorPosition")[0] - 100, Point("HomeDoorPosition")[1])
			);
		}
		
		
		
		
		
 }

    public IEnumerator OnEnterRoomAfterFade()
    {
		if (Globals.gameStage >= gameProgress.SecondFlood && Globals.rained){
			C.Tony.Disable();
		}
		
		
		//TEMP
		
		//Globals.gameStage = gameProgress.UsedBucket;
		
		//yield return E.WaitForGui(G.Explanation);
		
		
		// Put things here that happen when you enter a room
		if ((Globals.gameStage <= gameProgress.UsedBucket))
		{
		
			Prop("Pump").Disable();
			Prop("Handle").Disable();
			Prop("Hose").Disable();
		
			  Prop("ElectricPump").Disable();
			Prop("Box").Disable();
		
		
		}
		
		
		if ((Globals.gameStage < gameProgress.UsedBucket)) // Only run this part the first time you visit
		{
		
			C.Dave.SetPosition(Point("StartPosition"));
			C.Dave.Moveable = false;
			yield return E.WaitSkip();
			yield return C.Dave.Say("Gosh Durnit! My basement flooded again!", 0);
			yield return E.WaitSkip();
			yield return C.Dave.Say("I think I have a bucket 'round here somewhere...", 41);
			yield return E.WaitSkip();
			yield return C.Display(
				"Your bucket is over on the shelf. Click on it to add it to your toolbox.", 30);
			yield return E.WaitSkip();
		
		}
		else
		{
			C.Dave.Position = Point("HomeDoorPosition");
			yield return C.Dave.Face(eFace.Left, true);
		}
		
		
		if ((Globals.gameStage <= gameProgress.TonyAte)){
			// sets water level according to the stage of the game
				Prop("ElectricPump").Disable();
			Prop("Box").Disable();
			yield return ChangeWaterStage((int) Globals.gameStage, false);
		
		} else if (!Globals.rained && Globals.gameStage == gameProgress.SecondFlood){
			C.Tony.Disable();
			//C.Dave.Visible = false;
			yield return FloodBasement();
			Globals.rained = true;
			C.Dave.Position = Point("HomeDoorPosition");
			C.Dave.Visible = true;
			yield return E.WaitSkip();
			yield return E.Wait(1f);
			yield return C.Dave.Say(" Not again!", 3);
			yield return E.Wait();
			yield return E.WaitSkip();
		  yield return E.WaitForGui(G.Explanation);
		  //Globals.gameStage++;
		  //I.BilgePump.Add();
		
			  yield return E.WaitSkip();
			yield return E.Break;
		
		
		} else if (Globals.gameStage == gameProgress.FixedPump){
			yield return ChangeWaterStage(2, false);
		}
		
		
		
		
		yield return E.Break;
		
 }

    IEnumerator OnLookAtHotspotDoor(IHotspot hotspot)
    {
        yield return C.Dave.Say(" It's a door to the outside.", 2);
        yield return E.Break;
    }

    IEnumerator OnInteractHotspotDoor(IHotspot hotspot)
    {
		yield return C.Dave.WalkTo(Point("HomeDoorPosition"));
		C.Dave.ChangeRoomBG(R.Map);
		yield return E.Break;
		
 }

    IEnumerator OnUseInvHotspotDoor(IHotspot hotspot, IInventory item)
    {
        yield return E.Break;
    }

    IEnumerator OnUseInvPropWater(IProp prop, IInventory item)
    {
		// NB: You need to check they used the correct item!
		if (item == I.Bucket)
		{
			 //I.Active = null;
			 Debug.Log(I.Active);
			Prop("Water").Clickable = false;
			if (Globals.tutorialStage== tutorialProgress.selectedBucket)
			{
				I.Bucket.AnimCursor = "bucketFull";
				I.Bucket.AnimCursorInactive = "bucketFull";
				I.Bucket.AnimGui = "bucketFull";
				// Display: You scoop some water up.
				Globals.tutorialStage= tutorialProgress.complete;
				Globals.gameStage = gameProgress.UsedBucket;
				yield return StageComplete();
		
		
		
				yield return E.WaitSkip();
				yield return E.WaitForGui(G.Explanation);
				yield return E.WaitSkip();
		
				yield return C.Dave.Say("Oh man... this is going to take forever.", 44);
		
		
		
				yield return C.Dave.Say(
					"Maybe there's something at Doc's hardware store that can help.", 45);
				yield return E.WaitSkip();
				yield return C.Display("Click on the door to the right to leave your basement.", 28);
				C.Dave.Moveable = true;
		
				// I.Bucket.SetActive();
			}
			else
			{
				yield return C.Dave.Say(" This bucket ain't gonna cut it...", 6);
			}
		}
		if (item == I.BilgePump)
		{
			Prop("Water").Clickable = false;
			yield return C.Dave.WalkTo(
				new Vector2(Point("PumpPosition")[0], Point("PumpPosition")[1] + 50)
			);
			I.BilgePump.Remove();
			yield return ChangeWaterStage((int) Globals.gameStage, false);
			Prop("Pump").Enable();
			Prop("Handle").Enable();
			Prop("Hose").Enable();
			Prop("Hose").Clickable = false;
		
		
		}
		yield return E.Break;
		
 }

    IEnumerator OnLookAtPropWater(IProp prop)
    {
        yield return C.Dave.Say("That's a lot of water!", 5);
        yield return E.Break;
    }

    IEnumerator OnInteractPropWater(IProp prop)
    {
        // Dave(6):  I can't clean out all of this using only my hands.
        yield return E.Break;
    }

    IEnumerator OnExitRoom(IRoom oldRoom, IRoom newRoom)
    {
		
		yield return E.Break;
		
 }

    IEnumerator UpdateBlocking()
    {
		
		if (
			(Globals.tutorialStage== tutorialProgress.usedBucket)
			&& (C.Player.Position != Point("StartPosition") && !C.Player.Walking)
		)
		{
			Globals.tutorialStage= tutorialProgress.complete;
			Globals.gameStage = gameProgress.UsedBucket;
		
			yield return C.Display(
				"Click the door to leave your basement.", 36);
		}
		
		if ((int)Globals.gameStage == 5){
			Globals.gameStage = gameProgress.SecondFlood;
			yield return C.Dave.Say(" Finally! We got that dang water outta my dang basement!", 46);
			yield return C.Dave.Say(" I sure hope that never happens again!", 47);
			yield return E.Wait(1);
			yield return E.FadeOut(1);
			//Prop("Pump").Disable();
			//Prop("Hose").Disable();
			//Prop("Handle").Disable();
			yield return C.Display(" Two weeks later...", 53);
			//Globals.gameStage = 0;
			C.Dave.ChangeRoom(R.Map);
			E.FadeInBG(1);
		}
		
		yield return E.Break;
		
 }

    void Update() 
	{
		//yield return E.Break;
	}


    IEnumerator OnAnyClick()
    {
        yield return E.Break;
    }

    IEnumerator OnWalkTo()
    {
        yield return E.Break;
    }

    IEnumerator OnInteractPropPump(IProp prop)
    {
		string[] pumpAnims = { "PumpingS", "PumpingM", "PumpingL" };
		
		float pumpAnimTime = (float) (1.75 +  ((float)currentHandle * 0.5) );
		
		if (C.Tony.VisibleInRoom) {
			yield return OnInteractCharacterTony( C.Tony);
			yield break;
		}
		
		bool beenSprayed = false;
		
		yield return C.Dave.WalkTo(Point("PumpPosition"));
		Prop("Pump").Visible = false;
		Prop("Handle").Visible = false;
		
		//Debug.Log((int)currentHandle);
		//Debug.Log(pumpAnims[(int)currentHandle]);
		C.Dave.PlayAnimationBG(pumpAnims[(int)currentHandle]);
		//C.Dave.PlayAnimation(pumpAnims[(int)currentHandle]);
		//C.Dave.PlayAnimation(pumpAnims[(int)currentHandle]);
		
		
		if (Globals.gameStage == gameProgress.UsedBucket)
		{
			Globals.gameStage = gameProgress.TriedPump;
			yield return StageComplete();
			C.Dave.StopAnimation();
			Prop("Pump").Visible = true;
			Prop("Handle").Visible = true;
			yield return C.Display(
				"Congratulations! The water level has decreased. However, it is not enough...", 2);
		   yield return E.WaitForGui(G.Explanation);
		   yield return E.WaitSkip();
			yield return C.Dave.Say(" This is too hard!", 4);
			yield return C.Dave.Say("I'd better head back to the hardware store.", 15);
			yield return C.Dave.FaceDown();
		}
		else if (Globals.gameStage >= gameProgress.SecondFlood){
		
			if (Globals.gameStage == gameProgress.FixedPump){
		
				bool firstTime = true;
				if (pumpRepairs == 3){
					//Audio.Play("Motor");
					yield return UnfloodBasement();
						C.Dave.StopAnimation();
					Prop("Pump").Visible = true;
					Prop("Handle").Visible = true;
					//Audio.Stop("Motor");
					yield return E.WaitForGui(G.Explanation);
					Globals.gameStage = gameProgress.BoughtHouse;
						yield return E.WaitSkip();
					yield return C.Dave.Say(" Phew!", 115);
					yield return E.WaitSkip();
		
					Audio.Play("Rain");
					yield return E.WaitSkip();
		
					yield return FloodBasement();
		
					yield return C.Dave.Say(" Are you kidding me?!", 31);
		
					yield return C.Dave.Say("That's it.", 33);
		
					yield return C.Dave.Say(" I'm done.", 60);
		
					yield return C.Dave.Say(" I ain't puttin up with this dang basement any longer!", 92);
		
					yield return C.Dave.Say("I bet that house up there on the hill has a basement dryer than a desert!", 100);
		
					yield return C.Dave.Say(" I oughtta give that real estate agent a call.", 103);

					

		
				} else {
						yield return E.WaitSkip(1.5f);
						C.Dave.StopAnimation();
						Prop("Pump").Visible = true;
						Prop("Handle").Visible = true;
					//Audio.Play("MotorFailure");
					if (pumpRepairs == 0){
		
		
		
						yield return C.Dave.Say("It's broken!", 118);
							yield return E.WaitSkip(1.0f);
		
						yield return C.Dave.Say(" It's that rusty washer.", 119);
		
					}  else if (pumpRepairs == 1){
						yield return C.Dave.Say(" I'd better put the new washer on.", 120);
					} else if (pumpRepairs == 2){
						yield return C.Dave.Say(" Whoops, forgot to tighten the washer!", 121);
					}
				}
			}
		
		 else if (!beenSprayed){
				yield return E.Wait(1f);
				 Audio.Play("WaterHose");
				fountain.SetActive(true);
				yield return E.Wait(1.5f);
				C.Dave.StopAnimation();
				Prop("Pump").Visible = true;
				Prop("Handle").Visible = true;
				yield return E.Wait(0.5f);
				 Audio.Stop("WaterHose");
				yield return E.Wait(1f);
				//fountain.SetActive(false);
				yield return C.Dave.Say(" This pump is busted!", 50);
				beenSprayed = true;
				yield return C.Dave.Say(" The valve is leaking, it looks like there's a rusty washer.", 56);
				yield return C.Dave.Say(" I don't have the tools to replace it, I need to go to Doc's.", 57);
				Globals.gameStage = gameProgress.FixedPump;
			} else {
				yield return C.Dave.Say(" The valve is leaking, it looks like there's a rusty washer.", 58);
				yield return C.Dave.Say(" I don't have the tools to replace it, I need to go to Doc's.", 59);
			}
			// Dave(31): This water ain't goin nowhere!
			//Dave(33): I'm gonna need a better pump.
		}
		else if (Globals.gameStage >= gameProgress.RightParts) {
			C.Dave.StopAnimation();
			Prop("Pump").Visible = true;
			Prop("Handle").Visible = true;
			yield return C.Dave.Say("I'm pooped! I need some extra muscle.", 25);
			yield return C.Dave.Say("I bet my friend Tony could help out, I should give him a call.", 26);
		}
		else if (currentHandle == handleType.large && currentHose == hoseType.large)
		{
			Globals.gameStage = gameProgress.RightParts;
			yield return StageComplete();
			C.Dave.StopAnimation();
			Prop("Pump").Visible = true;
			Prop("Handle").Visible = true;
			yield return C.Display(
				"You've chosen the correct parts for the pump and the water level has decreased.", 3);
		
		
			yield return E.WaitForGui(G.Explanation);
			yield return E.WaitSkip();
			yield return C.Dave.Say("Still not enough... I could use some extra hands.", 7);
			// C.Dave.ChangeRoom(R.Cutscene);
		}
		else
		{
		
			yield return E.Wait(pumpAnimTime);
			C.Dave.StopAnimation();
			Prop("Pump").Visible = true;
			Prop("Handle").Visible = true;
			yield return C.Dave.Say(" This isn't any better. I should try different parts.", 8);
		}
		
		
		
		yield return E.Break;
		
 }

    IEnumerator OnLookAtPropPump(IProp prop)
    {
        yield return C.Display(
            sizeString[(int)currentHandle] + " Handle\n" + sizeString[(int)currentHose] + " Hose"
        );

        yield return E.Break;
    }

		// Helper to replace handle 
		IEnumerator replaceHandle(int handleInt)
		{
			string prevHandle = sizeString[(int)currentHandle];
			returnHandleToInv();
			currentHandle = (handleType) handleInt;
			yield return C.Display(
				prevHandle + " Handle replaced with " + sizeString[(int)currentHandle] + " Handle"
			);
			Prop("Handle").SetPosition(190, -81 + (int)currentHandle*10);
		}

		// Helper to replace hose 
		IEnumerator replaceHose(int hoseInt)
	  {
			string prevHose = sizeString[(int)currentHose];
			returnHoseToInv();
			currentHose = (hoseType) hoseInt;
			
			yield return C.Display(
				prevHose + " Hose replaced with " + sizeString[(int)currentHose] + " Hose"
			);
			string[] hoseAnim = {"HoseS", "HoseM", "HoseL"};
			Prop("Hose").Animation = hoseAnim[hoseInt];
		}

    IEnumerator OnUseInvPropPump(IProp prop, IInventory item) {
		if(Globals.gameStage == gameProgress.FixedPump ) {
			if (item == I.Wrench) {
				if (pumpRepairs == 0) {
					yield return C.Display(" Rusty washer removed.", 62);
					 pumpRepairs++;
				} else if (pumpRepairs == 1) {
					yield return C.Dave.Say(" Where's that new washer?", 122);
				}  else if (pumpRepairs == 2) {
					yield return C.Dave.Say(" That should do it!", 123);
					pumpRepairs++;
				}
			} if (item == I.Washer){
		
				if (pumpRepairs == 0) {
					yield return C.Dave.Say(" Whoop, gotta take that rusty old washer off before I can put this shiny new one on.", 124);
				} else if (pumpRepairs == 1) {
					pumpRepairs++;
					item.Remove();
					yield return C.Display(" New washer placed on pump.", 63);
				}
			}
			yield return E.Break;
		} else {
		
			int handleInt = 0;
			int hoseInt = 0;

			switch(item.ScriptName){	

				case (nameof(I.LargeHandle)):
					handleInt++;
					goto case (nameof(I.MediumHandle));
				case (nameof(I.MediumHandle)):
					handleInt++;
					goto case (nameof(I.SmallHandle));
				case (nameof(I.SmallHandle)):
					yield return replaceHandle(handleInt);
					item.Remove();
					break;
					
				case (nameof(I.LargeHose)):	
					hoseInt++;
				 	goto case (nameof(I.MediumHose));
				case (nameof(I.MediumHose)):	
					hoseInt++;
					goto case (nameof(I.SmallHose));
				case (nameof(I.SmallHose)):
					yield return replaceHose(hoseInt);
					item.Remove();
					break;

				default:
					break;
			}

			yield return E.Break;
		
 		}
	}

		IEnumerator OnUseInvPropHandle(IProp prop, IInventory item)
		{
			yield return OnUseInvPropPump(prop, item);
			yield return E.Break;
		}
	
		IEnumerator OnUseInvPropHose(IProp prop, IInventory item)
		{
			yield return OnUseInvPropPump(prop, item);
			prop.Clickable = false;
			yield return E.Break;
		}
	

    private void returnHandleToInv()
    {
        switch (currentHandle)
        {
            case handleType.small:
                I.SmallHandle.Add();
                break;
            case handleType.medium:
                I.MediumHandle.Add();
                break;
            case handleType.large:
                I.LargeHandle.Add();
                break;
            default:
                break;
        }
    }

    private void returnHoseToInv()
    {
        switch (currentHose)
        {
            case hoseType.small:
                I.SmallHose.Add();
                break;
            case hoseType.medium:
                I.MediumHose.Add();
                break;
            case hoseType.large:
                I.LargeHose.Add();
                break;
            default:
                break;
        }
    }

    IEnumerator OnInteractCharacterTony(ICharacter character)
    {
		if(Globals.gameStage != gameProgress.TonyPumped)
		{
		
		C.Tony.WalkToBG(Point("PumpPosition"));
		
		yield return C.Dave.WalkTo(
			new Vector2(Point("PumpPosition")[0] - 250, Point("PumpPosition")[1])
		);
		yield return C.Dave.Face(eFace.Right);
		
		yield return E.Wait(2);
		
		yield return C.Tony.Say("Here goes nothing!", 0);
		Prop("Pump").Visible = false;
		Prop("Handle").Visible = false;
		
		C.Tony.PlayAnimationBG("Pumping");
		Globals.gameStage = gameProgress.TonyPumped;
		yield return StageComplete();
		C.Tony.StopAnimation();
		
		Prop("Pump").Visible = true;
		Prop("Handle").Visible = true;
		
		yield return C.Display(" The recruited muscle has helped bring the water level down.", 4);
		yield return E.WaitForGui(G.Explanation);
		yield return E.WaitSkip();
		yield return C.Tony.Say(" Phew... I'm wiped out. Got any grub?", 4);
		}
		else {
			  yield return C.Dave.Say(" Gee Tony, I haven't got any food", 34);
		
			  yield return C.Tony.Say(" Why don't you give that pizza place a call?", 2);
		
			  yield return C.Tony.Say(" I could eat an entire pie", 7);
		
		  }
		yield return E.Break;
		
 }


    // void OnPostRestore(int version) { }

    IEnumerator OnInteractPropBucket(IProp prop)
    {
		I.Bucket.Add();
		Prop("Bucket").Disable();
		yield return C.Display("Bucket added to your toolbox.", 34);
		
		if (Globals.tutorialStage== tutorialProgress.start)
		{
			Globals.tutorialStage= tutorialProgress.clickedBucket;
			yield return E.WaitSkip();
			yield return C.Dave.Say(" There it is! Now I can scoop up some of this water.", 42);
			yield return E.WaitSkip();
			yield return C.Display(" Click on the bucket icon in your toolbox at the bottom left to select it.", 32);
			yield return C.Display("You also have a cell phone, which you can use to call the Hint Hotline.", 60);
		}
		
		yield return E.Break;
		
 }

    IEnumerator OnLookAtPropBucket(IProp prop)
    {
        yield return C.Dave.Say(" There's my bucket! I can use that to scoop out the water.", 43);
        yield return E.Break;
    }

    IEnumerator OnUseInvHotspotWindow(IHotspot hotspot, IInventory item)
    {
        if (item == I.Bucket)
        {
            I.Active = null;
            if (Globals.tutorialStage== tutorialProgress.usedBucket)
            {
                I.Bucket.AnimCursor = "bucket";
                I.Bucket.AnimCursorInactive = "bucket";
                I.Bucket.AnimGui = "bucket";

                Globals.tutorialStage= tutorialProgress.complete;

                // ...
                // Display(35):  You use the bucket to scoop some water out of the window.
                yield return E.WaitSkip();
                yield return C.Dave.Say("Oh man... this is going to take forever.", 44);
                yield return C.Dave.Say(
                    "Maybe there's something at Doc's hardware store that can help.", 45);
                yield return E.WaitSkip();
                yield return C.Display("Click on a space in the room to walk to it.", 37);
                C.Dave.Moveable = true;
            }
            else
            {
                yield return C.Dave.Say(" This bucket aint going to cut it...", 30);
            }
        }
        yield return E.Break;
    }

    IEnumerator OnUseInvPropBucket(IProp prop, IInventory item)
    {
        yield return E.Break;
    }

    IEnumerator OnInteractHotspotWindow(IHotspot hotspot)
    {
		yield return E.Break;
 }

    IEnumerator OnUseInvPropWaterFront(IProp prop, IInventory item)
    {
        yield return E.Break;
    }

	IEnumerator OnLookAtHotspotWindow( IHotspot hotspot )
	{
		yield return C.Dave.Say(" It's a window. Yup.", 48);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBleach( IHotspot hotspot )
	{
		yield return C.Dave.Say("It's bleach, fer cleanin' yer clothes.", 49);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotSprayPaint( IHotspot hotspot )
	{
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotSprayPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("It's a can of Mach brand orange spray paint.", 51);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotWashingMachine( IHotspot hotspot )
	{
		yield return C.Dave.Say(" It's my trusty old washing machine.", 52);
		
		yield return C.Dave.Say(" Although at this point I probably could just throw some detergent in the water and make my whole basement the washing machine.", 35);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBoiler( IHotspot hotspot )
	{
		yield return C.Dave.Say("This clunker of a boiler is from the 1940's.", 53);
		
		 yield return C.Dave.Say("  It works, but it makes the worst darn noises you've ever dun heard.", 36);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotTV( IHotspot hotspot )
	{
		yield return C.Dave.Say("Haven't used this in a while.", 54);
		
		yield return C.Dave.Say("For all you kids out there, this is what TV's looked like in the stone age.", 37);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotCouch( IHotspot hotspot )
	{
		yield return C.Dave.Say(" I can't believe I thought this looked good.", 55);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBleach( IHotspot hotspot )
	{
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotWashingMachine( IHotspot hotspot )
	{
		
		
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBoiler( IHotspot hotspot )
	{
		
		
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotTV( IHotspot hotspot )
	{
		
		
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotCouch( IHotspot hotspot )
	{
		
		
		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterTony( ICharacter character, IInventory item )
	{
		if (item == I.Beer) {
		item.Remove();
		yield return C.Tony.Say("Pizza AND beer?!", 5);
		yield return C.Tony.Say("Just what I needed!", 6);
		Prop("Pump").Visible = false;
		Prop("Handle").Visible = false;
		C.Tony.PlayAnimationBG("Pumping");
		
		
		Globals.gameStage = gameProgress.TonyAte;
		yield return StageComplete();
		C.Tony.StopAnimation();
		Prop("Pump").Visible = true;
		Prop("Handle").Visible = true;
		
		 yield return E.WaitForGui(G.Explanation);
		 yield return E.WaitSkip();
		
		}
		yield return E.Break;
	}

	IEnumerator OnLookAtPropWaterFront( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropWaterFront( IProp prop )
	{

		yield return E.Break;
	}

	public void LowerWaterShader(int spriteIndex, string CharacterName)
	{
		GameObject character = GameObject.Find(CharacterName);
		Renderer renderer = character.GetComponent<Renderer>();
		Material uniqueMaterial = renderer.material;
		double baseLevel = 0.5;
		int totalFrames = 20;
		double increment = baseLevel / totalFrames;
		int numIncrements = totalFrames - spriteIndex;
		uniqueMaterial.SetFloat("_Level", (float)(baseLevel + numIncrements*increment) );

	}

	public void LowerWater(int spriteIndex)
	{
		GameObject water = GameObject.Find("Water");
		SpriteRenderer spriteRenderer = water.GetComponent<SpriteRenderer>();
		Sprite waterSprite = Resources.Load<Sprite>("Water"+spriteIndex);
		spriteRenderer.sprite = waterSprite;
		Debug.Log("howdy");	
	
	}

	private IEnumerator AnimateUnflooding(int startingIndex, int endingIndex, float interval = 0.5f){
		for(int i = startingIndex; i >= endingIndex; i--){
			LowerWater(i);
			LowerWaterShader(i, "CharacterDave");
			LowerWaterShader(i, "CharacterTony");
			LowerWaterShader(i, "PropPumpProp");
			LowerWaterShader(i, "PropHose");
		
			yield return new WaitForSeconds(interval);
		}
	}

	public  IEnumerator ChangeWaterStage(int stageNum, bool animate = true, bool full = false)
	{
		//Debug.Log("changewaterstage start");
		
		int totalFrames = 20;
		int framesPerStage = 5;
		int startingIndex, endingIndex;
		
		if (stageNum == 0){
			startingIndex = 20;
			endingIndex = 20;
		} else if (stageNum == 1){
			startingIndex = 20;
			endingIndex = 19;
		} else if (stageNum == 2){
		   startingIndex = 19;
			endingIndex = 16;
		} else {
			 startingIndex = totalFrames - framesPerStage*(stageNum-2) + 1;
			endingIndex = startingIndex - framesPerStage;
		 }
		if (!animate)
			startingIndex = endingIndex;

		yield return AnimateUnflooding(startingIndex, endingIndex);
		
		yield return E.Break;
	}

	public IEnumerator FloodBasement() {
		for(int i = 1; i <= 20; i++){
			//Debug.Log("begin loop iteration "+ i);
			LowerWater(i);
			LowerWaterShader(i, "CharacterDave");
			LowerWaterShader(i, "CharacterTony");
			LowerWaterShader(i, "PropPumpProp");
			LowerWaterShader(i, "PropElectricPump");
			LowerWaterShader(i, "PropHose");
		
			yield return new WaitForSeconds((float)0.1);
			//Debug.Log(" end loop iteration "+ i);
		}
	}

	public IEnumerator UnfloodBasement() {
		yield return AnimateUnflooding(20, 1, 0.2f);
	}

	private void LoadChapter(int chapter){
		Globals.LoadingChapter = true;
		Globals.gameStage = (gameProgress) chapter;
		if(chapter <= 5){
			Globals.rained = false;
		}
		Globals.SetInventory();
		Debug.Log(Globals.gameStage);
		//E.Restart(R.Home);
		//C.Dave.ChangeRoom(R.Home);
	}



	public IEnumerator StageComplete()
	{
		yield return ChangeWaterStage((int)Globals.gameStage);
		yield return E.Break;
	}

	public void SwapShader()
	{
		//E.GetScript<CharacterDave>();
		//Assets/Shaders/water.shadergraph;
	}

	IEnumerator OnLookAtCharacterTony( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtPropHandle( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropHandle( IProp prop )
	{
		yield return OnInteractPropPump(prop);
		yield return E.Break;
	}

	IEnumerator OnInteractPropBox( IProp prop )
	{
		Prop("Box").Disable();
		Prop("ElectricPump").Enable();
		Prop("Hose").Enable();
		yield return C.Dave.Say(" It's beautiful!", 38);
		yield return E.WaitSkip();
		/*
		//Dave(39): What's this?
		//C.Dave.AddInventory(I.RepairKit);
		//Display(54): Repair Kit added to your toolbox.
		*/
		yield return E.Break;
	}

	IEnumerator OnUseInvHotspotWashingMachine( IHotspot hotspot, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropElectricPump( IProp prop )
	{
		if (Globals.gameStage == gameProgress.BoughtHouse){
			Audio.Play("Motor");
			yield return UnfloodBasement();
			Audio.Stop("Motor");
			 yield return C.Dave.Say("Like a charm!", 40);
			Globals.gameStage = gameProgress.UsedElectricPump;
			  yield return E.WaitForGui(G.Explanation);
		
		  // E.Restart();
			yield return E.WaitSkip();
			yield return E.FadeOut(1, false);
			Audio.Stop("Rain", 1f);
			yield return C.Display(" 3 months later", 65);
			yield return E.FadeIn(1, false);
			yield return E.Wait(1f);
			Audio.Play("ring");
			yield return C.Dave.Say("Hello?", 104);
			yield return C.Display(" Hi Dave, your new home is ready!", 66);
			yield return C.Dave.Say("Finally!", 131);
		
		}
		
		
		
		
		if (Globals.gameStage == gameProgress.SecondFlood){
		
		Audio.Play("Motor");
		yield return UnfloodBasement();
		Audio.Stop("Motor");
		Globals.gameStage = gameProgress.FixedPump;
		  yield return E.WaitForGui(G.Explanation);
			yield return E.WaitSkip();
		  yield return C.Dave.Say("Like a charm!", 40);
		yield return E.FadeOut();
		yield return C.Display("6 months later...", 61);
		yield return FloodBasement();
		yield return E.FadeIn();
		yield return C.Dave.Say(" Another flood?", 113);
		yield return E.WaitSkip();
		yield return C.Dave.Say("Nothing my Pump-o-matic 5000 can't handle!", 114);
		//LowerWater(4);
		} else if (Globals.gameStage == gameProgress.FixedPump){
		
		  bool firstTime = true;
		  if (pumpRepairs == 3){
			  Audio.Play("Motor");
			  yield return UnfloodBasement();
			  Audio.Stop("Motor");
			  yield return E.WaitForGui(G.Explanation);
			  Globals.gameStage = gameProgress.BoughtHouse;
				yield return E.WaitSkip();
			  yield return C.Dave.Say(" Phew!", 115);
			  yield return E.WaitSkip();
			  yield return C.Dave.Say(" The pump works again!", 116);
		
		  } else {
			  Audio.Play("MotorFailure");
			  if (pumpRepairs == 0){
		
		
				  yield return C.Dave.Say("Seriously?!", 117);
				  yield return E.WaitSkip();
				  yield return C.Dave.Say("It's broken!", 118);
					yield return E.WaitSkip(1.0f);
				  yield return C.Dave.Say(" It looks like there's a rusty washer.", 119);
		
			  }  else if (pumpRepairs == 1){
				  yield return C.Dave.Say(" I'd better put the new washer on.", 120);
			  } else if (pumpRepairs == 2){
				  yield return C.Dave.Say(" Whoops, forgot to tighten the washer!", 121);
			  }
		  }
		}
		yield return E.Break;
	}

	IEnumerator OnUseInvPropElectricPump( IProp prop, IInventory item )
	{
		if(Globals.gameStage == gameProgress.FixedPump ) {
			if (item == I.Wrench) {
				if (pumpRepairs == 0) {
					yield return C.Display(" Rusty washer removed.", 62);
					 pumpRepairs++;
				} else if (pumpRepairs == 1) {
					yield return C.Dave.Say(" Where's that new washer?", 122);
				}  else if (pumpRepairs == 2) {
					yield return C.Dave.Say(" That should do it!", 123);
					pumpRepairs++;
				}
			} if (item == I.Washer){
		
				if (pumpRepairs == 0) {
					yield return C.Dave.Say(" Whoop, gotta take that rusty old washer off before I can put this shiny new one on.", 124);
				} else if (pumpRepairs == 1) {
					pumpRepairs++;
					item.Remove();
					yield return C.Display(" New washer placed on pump.", 63);
				}
			}
		}
		yield return E.Break;
	}

	IEnumerator OnLookAtPropElectricPump( IProp prop )
	{

		yield return C.Dave.Say(" Ain't she a beauty?", 125);
		yield return E.Break;
	}

	IEnumerator OnUseInvHotspotBoiler( IHotspot hotspot, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropHose( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvPropBox( IProp prop, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtPropBox( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtPropHose( IProp prop )
	{

		yield return E.Break;
	}
}
