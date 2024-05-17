using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogUsePhone : DialogTreeScript<DialogUsePhone>
{
	public IEnumerator OnStart()
	{
		D.UsePhone.OptionOff(1);
		D.UsePhone.OptionOff(2);
		D.UsePhone.OptionOff(5);
		D.UsePhone.OptionOff(6);
		
		if (Globals.gameStage >= gameProgress.TriedPump){
			D.UsePhone.OptionOn(1);
		
		}
		if (Globals.gameStage == gameProgress.TonyPumped) {
			D.UsePhone.OptionOn(2);
			}
		
		if (Globals.gameStage >= gameProgress.BoughtHouse) {
			D.UsePhone.OptionOn(6);
			}
		
		
		yield return E.ConsumeEvent;
	}

	public IEnumerator OnStop()
	{
		yield return E.Break;
	}

	IEnumerator Option1( IDialogOption option )
	{
		yield return C.Dave.Say("Hey Tony. My basement is flooded, I've got this pump but I need more muscle!", 27);
		yield return E.WaitSkip();
		yield return C.Display("Again?! Alright, I'll be right over", 17);
		yield return E.WaitSkip();
		
		yield return E.FadeOut(1);
		
		C.Tony.Enable();
		C.Tony.ChangeRoom(R.Home);
		C.Tony.SetPosition(Point("HomeDoorPosition"));
		
		yield return C.Dave.Face(eFace.Right);
		if (C.Dave.Position == Point("HomeDoorPosition"))
			C.Dave.SetPosition(new Vector2(Point("HomeDoorPosition")[0] - 250, Point("HomeDoorPosition")[1]));
		
		yield return E.FadeIn(1);
		
		Camera.SetCharacterToFollow(C.Tony, 1);
		
		while(Camera.GetHasPositionOverrideOrTransition())
		{
		yield return E.WaitSkip();
		}
		
		yield return C.Tony.Say("Ay Dave, I'm here to he-", 1);
		yield return E.WaitSkip();
		yield return E.WaitSkip();
		yield return E.WaitSkip();
		yield return C.Tony.Say("Oh boy.", 3);
		yield return E.WaitSkip();
		Stop();
		Camera.SetCharacterToFollow(C.Dave, 1);
		D.UsePhone.OptionOffForever(1);
		
		yield return E.Break;
	}

	IEnumerator Option3( IDialogOption option )
	{
		
		
		switch (Globals.gameStage) {
			case gameProgress.None:
				switch (Globals.tutorialStage) {
					case tutorialProgress.start:
						yield return C.Display("The bucket is located on the shelf above the laundry machine. Click on it to add it to your toolbox.", 29);
						break;
					case tutorialProgress.clickedBucket:
						yield return C.Display("Your toolbox is located at the bottom left. Move the mouse down to the lower left, and the toolbox will slide up. Then, click on the bucket icon to select it.", 31);
						break;
					case tutorialProgress.selectedBucket:
						yield return C.Display("With the bucket selected, use it on the water by clicking anywhere on the water.", 35);
						break;
					case tutorialProgress.usedBucket:
						yield return C.Display("To move around the room, click the spot you would like to walk to.", 38);
						break;
					case tutorialProgress.complete:
						yield return C.Display("Now the fun begins!", 39);
						break;
					default:
						yield return C.Display("Try scooping up some of the water with a bucket.", 40);
						break;
					}
				break;
			case gameProgress.UsedBucket:
				// player has acquired the pump, but not yet placed it in the basement
				if (I.BilgePump.Owned) {
					yield return C.Display(" Now that you have a pump, you can select it from your toolbox and use it on the water in your basement.", 41);
					if (C.Dave.Room == R.Hardware) {
						yield return C.Display(" You should go back home. Walk all the way to the left and click on the door.", 42);
					}
				}
				// player has placed the pump in the basement, but not yet used it.
				else if (Prop("Pump").Visible) {
					yield return C.Display("Now that you've placed the pump, time to try it out! Click on the pump to give it a go.", 43);
				}
				// player has not yet acquired the pump
				else {
					yield return C.Display("I hear Doc over at the hardware store is running a big sale on pumps. I bet one of those would help you.", 44);
					if (C.Dave.Room == R.Home) {
						yield return C.Display(" You can leave your basement by clicking on the door, located all the way to the right. If you cannot see it, keep walking to the right.", 45);
						}
				}
				break;
			// #TODO flesh out remaining hint scenarios
			case gameProgress.TriedPump:
				// player has acquired replacement parts
				// player has replaced parts, but they are not the correct parts
				// player has not yet acquired replacement parts
				yield return C.Display("You should go back to Doc at his hardware store. Maybe he has something else that could help.", 46);
				break;
			case gameProgress.RightParts:
				// player has called Tony and he has arrived, but not yet pumped.
				// player has not called Tony
				yield return C.Display("You could use some extra muscle. Try calling a friend, maybe they could help out.", 47);
				break;
			case gameProgress.TonyPumped:
				// player has ordered pizza, but not yet given it to tony
				// player has not yet ordered pizza
				yield return C.Display("Tony is tired, he needs something to boost his energy. That pizza place delivers, try giving them a call.", 48);
				break;
			default: 
				yield return C.Display("Thank for for calling the Hint Hotline. Our lines our busy at the moment, please call back later.", 49);
				break;
		}
		
		//C.Display(hintString[(int)Globals.gameStage]);
		Stop();
		yield return E.Break;
	}

	IEnumerator Option2( IDialogOption option )
	{
		yield return C.Dave.Say("Hi, I'd like to order a large cheese and pepperoni pizza for delivery", 28);
		yield return C.Display(" Okay, anything else?", 18);
		yield return C.Dave.Say("Yeah, a 6 pack of beer.", 105);
		yield return C.Display("Got it, what is the address?", 50);
		yield return C.Dave.Say(" 22 Hart Street", 29);
		yield return C.Display(" Your order will be there in 30 minutes.", 19);
		D.UsePhone.OptionOffForever(2);
		yield return E.FadeOut(1);
		yield return C.Display("30 minutes later...", 51);
		yield return E.FadeIn(1);
		yield return C.Display("Pizza & Beer added to your toolbox", 52);
		C.Dave.AddInventory(I.Beer);
		Stop();
		yield return E.Break;
	}

	IEnumerator Option4( IDialogOption option )
	{
		Stop();
		
		yield return E.Break;
	}

	IEnumerator Option5( IDialogOption option )
	{
		yield return C.Display("You've reached PumpCo, how may I help you?", 55);
		yield return C.Dave.Say(" I need the biggest, baddest pump you've got.", 108);
		yield return C.Display(" That would be the Pump-o-matic 5000.", 56);
		yield return C.Display(" It's our top of the line model, all electric.", 57);
		yield return C.Dave.Say(" I'll take it.", 109);
		yield return C.Dave.Say(" And make that express delivery.", 110);
		yield return C.Display("Ok, it will be there pronto.", 58);
		
		
		yield return E.FadeOut(1);
		if (R.Current != R.Home){
			C.Dave.ChangeRoom(R.Home);
		}
		yield return C.Display("3 minutes later", 59);
		Prop("Box").Enable();
		Prop("Pump").Disable();
		Prop("Handle").Disable();
		Prop("Hose").Disable();
		Stop();
		C.Dave.SetPosition(Point("HomeDoorPosition"));
		yield return E.FadeIn(1);
		yield return C.Dave.Say("Wow.", 111);
		yield return E.Wait();
		yield return C.Dave.Say("That was fast.", 112);
	}

	IEnumerator Option6( IDialogOption option )
	{
		yield return C.Dave.Say(" Hello, I'm Dave, I'm interested in the property on 11 Hill Lane.", 140);
		yield return C.Dave.Say(" Is it still available?", 141);
		yield return C.Display(" Yes it is!", 67);
		yield return C.Display(" It's a beautiful hilltop property with a stunning view of the entire island, and-", 68);
		yield return C.Dave.Say(" Yeah yeah, got it, does the basement ever get flooded?", 142);
		yield return C.Display(" Never has, no.", 69);
		yield return C.Dave.Say(" I'll take it!", 143);
		yield return C.Display("Really? Don't you want to see it first? I can give a tour today.", 70);
		yield return C.Dave.Say(" Nope that's quite alright, I'm in, where do I sign?", 144);
		yield return C.Display(" Great! This is the easiest sale I've ever had!", 71);
		
		yield return E.WaitForGui(G.Explanation);

		yield return C.Display(" The house will be ready for you in three months.", 72);
		yield return C.Dave.Say(" THREE MONTHS?!", 145);
		yield return C.Dave.Say(" I'm standing here in a water up to my neck, and no matter what I do, it keeps coming back!", 146);
		yield return C.Dave.Say(" What am I supposed to do for three months?!", 147);
		yield return C.Display(" Well Dave, since you have been such a wonderful client...", 73);
		yield return C.Display("How about I set you up with an electric pump rental?", 74);
		yield return C.Display(" Free of charge!", 75);
		yield return C.Dave.Say(" Well alrighty then, but it better be quick!", 148);
		
		D.UsePhone.OptionOff(6);
		
		yield return E.FadeOut(1);
		if (R.Current != R.Home){
			C.Dave.ChangeRoom(R.Home);
		}
		yield return C.Display("3 minutes later", 59);
		Prop("Box").Enable();
		Prop("Pump").Disable();
		Prop("Handle").Disable();
		Prop("Hose").Disable();
		Stop();
		C.Dave.SetPosition(Point("HomeDoorPosition"));
		yield return E.FadeIn(1);
		yield return C.Dave.Say("Wow.", 111);
		yield return E.Wait();
		yield return C.Dave.Say("That was fast.", 112);
		
		yield return E.Break;
	}
}