using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogChatWithClerk2 : DialogTreeScript<DialogChatWithClerk2>
{
public IEnumerator OnStart()
	{
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
				yield return C.HardwareClerk.Say("HSO: Sure, I have this bilge pump (a hand operated pump).");
				 yield return E.WaitSkip();
				 yield return C.Dave.Say("What's this?", 39);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("Yes");
				yield return E.WaitSkip();
				yield return C.Dave.Say("Like a charm!", 40);
				I.BilgePump.Add();
				yield return C.Display("Click on the door to the right to leave your basement.", 28);
				Stop();
		yield return E.Break;
	}
}