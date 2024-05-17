using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;

namespace PowerScript
{	
	// Shortcut access to SystemAudio.Get
	public class Audio : SystemAudio
	{
	}

	public static partial class C
	{
		// Access to specific characters (Auto-generated)
		public static ICharacter Dave		{ get{return PowerQuest.Get.GetCharacter("Dave"); } }
		public static ICharacter HardwareClerk		{ get{return PowerQuest.Get.GetCharacter("HardwareClerk"); } }
		public static ICharacter Tony		{ get{return PowerQuest.Get.GetCharacter("Tony"); } }
		public static ICharacter r		{ get{return PowerQuest.Get.GetCharacter("r"); } }
		// #CHARS# - Do not edit this line, it's used by the system to insert characters
	}

	public static partial class I
	{		
		// Access to specific Inventory (Auto-generated)
		public static IInventory BilgePump		{ get{return PowerQuest.Get.GetInventory("BilgePump"); } }
		public static IInventory MediumHandle		{ get{return PowerQuest.Get.GetInventory("MediumHandle"); } }
		public static IInventory LargeHandle		{ get{return PowerQuest.Get.GetInventory("LargeHandle"); } }
		public static IInventory MediumHose		{ get{return PowerQuest.Get.GetInventory("MediumHose"); } }
		public static IInventory LargeHose		{ get{return PowerQuest.Get.GetInventory("LargeHose"); } }
		public static IInventory CellPhone		{ get{return PowerQuest.Get.GetInventory("CellPhone"); } }
		public static IInventory SmallHandle		{ get{return PowerQuest.Get.GetInventory("SmallHandle"); } }
		public static IInventory SmallHose		{ get{return PowerQuest.Get.GetInventory("SmallHose"); } }
		public static IInventory Beer		{ get{return PowerQuest.Get.GetInventory("Beer"); } }
		public static IInventory Bucket         { get { return PowerQuest.Get.GetInventory("Bucket"); } }
		public static IInventory ElectricPump   { get { return PowerQuest.Get.GetInventory("ElectricPump"); } }
		public static IInventory RepairKit      { get { return PowerQuest.Get.GetInventory("RepairKit"); } }
		public static IInventory Washer         { get { return PowerQuest.Get.GetInventory("Washer"); } }
		public static IInventory Wrench         { get { return PowerQuest.Get.GetInventory("Wrench"); } }
		// #INVENTORY# - Do not edit this line, it's used by the system to insert rooms for easy access
	}

	public static partial class G
	{
		// Access to specific gui (Auto-generated)
		public static IGui DisplayBox		{ get{return PowerQuest.Get.GetGui("DisplayBox"); } }
		public static IGui InfoBar		{ get{return PowerQuest.Get.GetGui("InfoBar"); } }
		public static IGui Toolbar		{ get{return PowerQuest.Get.GetGui("Toolbar"); } }
		public static IGui Inventory		{ get{return PowerQuest.Get.GetGui("Inventory"); } }
		public static IGui DialogTree		{ get{return PowerQuest.Get.GetGui("DialogTree"); } }
		public static IGui SpeechBox		{ get{return PowerQuest.Get.GetGui("SpeechBox"); } }
		public static IGui Options        { get { return PowerQuest.Get.GetGui("Options"); } }
		public static IGui Save           { get { return PowerQuest.Get.GetGui("Save"); } }
		public static IGui ChapterSelect           { get { return PowerQuest.Get.GetGui("ChapterSelect"); } }
		public static IGui TitleMenu      { get { return PowerQuest.Get.GetGui("TitleMenu"); } }
		public static IGui MenuButton     { get { return PowerQuest.Get.GetGui("MenuButton"); } }
		public static IGui Explanation    { get { return PowerQuest.Get.GetGui("Explanation"); } }
		public static IGui ExplanationOld         { get { return PowerQuest.Get.GetGui("ExplanationOld"); } }
		// #GUI# - Do not edit this line, it's used by the system to insert rooms for easy access
	}

	public static partial class R
	{
		// Access to specific room (Auto-generated)
		public static IRoom Home		{ get{return PowerQuest.Get.GetRoom("Home"); } }
		public static IRoom Map		{ get{return PowerQuest.Get.GetRoom("Map"); } }
		public static IRoom Hardware		{ get{return PowerQuest.Get.GetRoom("Hardware"); } }
		public static IRoom Cutscene		{ get{return PowerQuest.Get.GetRoom("Cutscene"); } }
		public static IRoom Menu           { get { return PowerQuest.Get.GetRoom("Menu"); } }
		public static IRoom NewHouse       { get { return PowerQuest.Get.GetRoom("NewHouse"); } }
		// #ROOM# - Do not edit this line, it's used by the system to insert rooms for easy access
	}

	// Dialog
	public static partial class D
	{
		// Access to specific dialog trees (Auto-generated)

		public static IDialogTree DialogWithClerk		{ get{return PowerQuest.Get.GetDialogTree("DialogWithClerk"); } }
		public static IDialogTree BuyOptions		{ get{return PowerQuest.Get.GetDialogTree("BuyOptions"); } }
		public static IDialogTree UsePhone		{ get{return PowerQuest.Get.GetDialogTree("UsePhone"); } }
		public static IDialogTree Tutorial             { get { return PowerQuest.Get.GetDialogTree("Tutorial"); } }
		// #DIALOG# - Do not edit this line, it's used by the system to insert rooms for easy access	    	    
	}


}
