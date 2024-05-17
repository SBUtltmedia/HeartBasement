using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using ePlayerName = PowerTools.Quest.SystemText.ePlayerName;

namespace PowerScript
{	

	public static partial class C
	{
		/// The current player
		public static ICharacter Player		{ get{ Systems.Text.LastPlayerName=ePlayerName.Player; return PowerQuest.Get.GetPlayer(); } }
		/// The current player (same as C.Player)
		public static ICharacter Plr		{ get{ Systems.Text.LastPlayerName=ePlayerName.Plr; return PowerQuest.Get.GetPlayer(); } }
		/// The current player (same as C.Player)
		public static ICharacter Ego		{ get{ Systems.Text.LastPlayerName=ePlayerName.Ego; return PowerQuest.Get.GetPlayer(); } }

		/// Display narrator dialog
		public static Coroutine Display(string dialog) { return PowerQuest.Get.Display(dialog); }
		/// Display narrator dialog
		public static Coroutine Display(string dialog, int id) { return PowerQuest.Get.Display(dialog, id); }
		/// Display narrator dialog
		public static Coroutine DisplayBG(string dialog) { return PowerQuest.Get.Display(dialog); }
		/// Display narrator dialog
		public static Coroutine DisplayBG(string dialog, int id) { return PowerQuest.Get.Display(dialog, id); }

		/// Add section to dialog script- only appears in voice script. Useful to separate parts of a function to give context for voice actors/translators. eg. `Section: Dave uses the bucket on the well`
		public static void Section( string dialog ) {/*No op- just for editor*/}

		/// Makes current player walk to what was clicked (Shortcut to C.Player.WalkToClicked())
		public static Coroutine WalkToClicked() { return C.Player.WalkToClicked(); }
		/// Makes current player face what was clicked (Shortcut to C.Player.FaceClicked())
		public static Coroutine FaceClicked() { return C.Player.FaceClicked(); }

	}

	public static partial class I
	{		
		/// Shortcut to active inventory of current player (same as PowerQuest.Get.GetPlayer().ActiveInventory)
		public static IInventory Active { get { return PowerQuest.Get.GetPlayer().ActiveInventory; } set { PowerQuest.Get.GetPlayer().ActiveInventory = value; } }
		/// Shortcut to active inventory of current player (same as PowerQuest.Get.GetPlayer().ActiveInventory)
		public static IInventory Current { get { return PowerQuest.Get.GetPlayer().ActiveInventory; } set { PowerQuest.Get.GetPlayer().ActiveInventory = value; } }

	}

	public static partial class G
	{
	}

	public static partial class R
	{
		/// The room the player is in
		public static IRoom Current { get { return PowerQuest.Get.GetCurrentRoom(); } }
		/// The last room the player was in
		public static IRoom Previous { get { return PowerQuest.Get.GetPlayer().LastRoom; } }

		/// True when loading into a room while testing in the editor
		public static bool EnteredFromEditor { get { return PowerQuest.Get.IsDebugBuild && PowerQuest.Get.GetPlayer().LastRoom == null; } }
		public static bool FirstTimeVisited { get { return Current.FirstTimeVisited; } }

	}

	// Dialog
	public static partial class D
	{
		/// Returns the specified dialog. Eg `D.Get("ChatWithBarney")` 
		public static IDialogTree Get(string name) { return PowerQuest.Get.GetDialogTree(name); }
		/// The currently active dialog. Eg. `if ( D.Current == D.ChatWitBarney ) {...}`, or `D.Current.OptionOff(1);`
		public static IDialogTree Current { get { return PowerQuest.Get.GetCurrentDialog(); } }
		/// The last dialog entered. This can be used to return to a previous dialog. Eg. `D.Previous.Start();`. Within a dialog you can use `GotoPrevious();`
		public static IDialogTree Previous { get { return PowerQuest.Get.GetPreviousDialog(); } }


	}


}
