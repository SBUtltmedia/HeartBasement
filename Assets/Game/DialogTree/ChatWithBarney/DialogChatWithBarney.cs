using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogChatWithBarney : DialogTreeScript<DialogChatWithBarney>
{
	public IEnumerator Start()
	{
		yield return C.WalkToClicked();
		C.FaceClicked();
		C.HardwareClerk.Face(C.Dave);
		yield return C.HardwareClerk.Say("Nope.", 9);
		yield return E.Break;
		
	}

	public IEnumerator Option1( IDialogOption option )
	{
		yield return C.Dave.Say(" This bucket aint going to cut it...", 30);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Face(C.Dave);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Say("Alright, alright, you got me. These are the biggest handles and hoses we have in stock.", 10);
		yield return E.WaitSkip();
		yield return C.Dave.Say("This water ain't goin nowhere!", 31);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Say("Yes");
		yield return E.WaitSkip();
		yield return C.Dave.Say(" What a trip!", 32);
		
		Audio.Play("Bucket");
		//I.BilgePump.Disable();
		I.BilgePump.AddAsActive();
		yield return E.WaitSkip();
	}


	public IEnumerator Option2( IDialogOption option )
	{
		yield return C.Dave.Say("I'm gonna need a better pump.", 33);
		yield return C.HardwareClerk.Say("Whaddaya mean?");
		
		// Here we start a seperate branch of dialog (about forests):
		// Turn off the main options
		OptionOff(1,2,3);
		OptionOff("bye");
		// Turn on the 'forest' branch options
		OptionOn("tree","leaf","forestdone");
		
		yield return E.Break;
		
	}


	public IEnumerator Option3( IDialogOption option )
	{
		yield return C.Dave.Say(" Gee Tony, I haven't got any food", 34);
		yield return C.HardwareClerk.Say("No. I hate it. Lets never speak of it again");
		yield return C.Dave.Say(" Although at this point I probably could just throw some detergent in the water and make my whole basement the washing machine.", 35);
		option.OffForever();
		yield return E.Break;
		
	}



	public IEnumerator OptionForestDone( IDialogOption option )
	{
		// Here we're returning from the 'forest' dialog branch to the main dialog
		
		// Turn off the 'forest' options
		OptionOff("tree","leaf","forestdone");
		
		// Turn on the main options. If they've had 'OptionOffForever' they wont turn on again.
		OptionOn(1,2,3);
		OptionOn("bye");
		
		// Only set the 'Forest' option as used (which changes the color) if all its child options have been used.
		Option(2).Used = Option("tree").Used && Option("leaf").Used;
		yield return E.Break;
		
	}


	public IEnumerator OptionTree( IDialogOption option )
	{
		yield return C.Dave.Say("  It works, but it makes the worst darn noises you've ever dun heard.", 36);
		yield return C.HardwareClerk.Say(" I guess");
		yield return E.Break;
		
	}


	public IEnumerator OptionLeaf( IDialogOption option )
	{
		yield return C.Dave.Say("For all you kids out there, this is what TV's looked like in the stone age.", 37);
		yield return C.HardwareClerk.Say("Yes. It is pleasant foliage");
		yield return E.Break;
		
	}

	public IEnumerator OptionBye( IDialogOption option )
	{
		yield return C.Dave.Say(" It's beautiful!", 38);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.FaceRight();
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Say("Whatever");
		
		// Don't mark the 'end' option as used
		option.Used = false;
		
		// stop the dialog
		Stop();
	}
}