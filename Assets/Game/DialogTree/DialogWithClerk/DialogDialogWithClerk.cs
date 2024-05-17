using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogDialogWithClerk : DialogTreeScript<DialogDialogWithClerk>
{
	public IEnumerator OnStart()
	{
		//Globals.gameStage = gameProgress.TriedPump1;
		
		
		
		if (Globals.gameStage <= gameProgress.UsedBucket) {
			D.DialogWithClerk.OptionOff(2);
			D.DialogWithClerk.OptionOn(1);
		 //   D.DialogWithClerk.GetOption(1).Start();
		} else {
			D.DialogWithClerk.OptionOff(1);
			D.DialogWithClerk.OptionOn(2);
		}
		
		
		
		yield return E.ConsumeEvent;
	}

	public IEnumerator OnStop()
	{
		yield return E.Break;
	}

	IEnumerator Option1( IDialogOption option )
	{
				yield return C.HardwareClerk.Face(C.Dave);
		
				yield return E.WaitSkip();
		
		
				yield return C.HardwareClerk.Say("Hello valued customer, what can I do for you?", 1);
		
				yield return E.WaitSkip();
		
				yield return C.Dave.Say("My basement's flooded. Got anything to help with that?", 18);
		
				yield return E.WaitSkip();
		
				yield return C.HardwareClerk.Say("We've got a special Pumps 4 Chumps promotion going on!", 2);
		
				yield return C.HardwareClerk.Say(" It'll be sure to unflood your flood, no money back guarantee!", 11);
		
				yield return E.WaitSkip();
		
				yield return C.Dave.Say("I hate everything you just said, but I guess I'll take it.", 19);
		
				I.BilgePump.Add();
		
				yield return C.Display("Pump added to your toolbox.", 12);
		
				Stop();
		
		yield return E.Break;
	}

	IEnumerator Option2( IDialogOption option )
	{
				yield return C.HardwareClerk.Face(C.Dave);
				yield return E.WaitSkip();
				yield return C.Dave.Say("Cut the crap, carpetbagger!", 20);
				yield return C.Dave.Say(" That pump didn't do squat!", 94);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("I'm so sorry to hear that!", 3);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("I recommend upgrading to a higher end model handle and hose.", 4);
		
				yield return C.HardwareClerk.Say("We're running a 25% off special on all pump accessories!", 12);
				Stop();
				D.BuyOptions.Start();
		
		
		
		
		yield return E.Break;
	}

	IEnumerator Option3( IDialogOption option )
	{
		yield return C.Dave.Say(" Hey Doc, that bilge pump ain't cuttin it no more.", 96);
		yield return C.Dave.Say(" Got anything better?", 98);
		yield return C.HardwareClerk.Say("I'm afraid not.", 13);
		yield return C.HardwareClerk.Say("You'll have to call PumpCo to get anything better than that bilge pump.", 14);
		yield return E.Break;
	}

	IEnumerator Option4( IDialogOption option )
	{
		yield return C.Dave.Say(" Hey Doc, that bilge pump's busted.", 128);
		yield return C.Dave.Say(" The valve is leaking because of a rusted washer.", 129);
		yield return C.HardwareClerk.Say("I've got just what you need: Doc's bilge pump repair kit!", 15);
		yield return C.HardwareClerk.Say("This repair kit has everything you need to replace that rusted washer.", 16);
		yield return C.HardwareClerk.Say("And you're in luck, I happen to have one left in stock!", 17);
		yield return C.HardwareClerk.Say("They're very popular, you know, this won't last long.", 18);
		yield return C.Dave.Say(" Alright alright Doc, I'll take it", 130);
		
		C.Dave.AddInventory(I.RepairKit);
		yield return C.Display(" Repair Kit added to your toolbox.", 64);
		yield return E.Break;
	}
}