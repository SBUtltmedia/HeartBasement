using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PowerScript;
using PowerTools.Quest;
using static RoomHome;

///	Global Script: The home for your game specific logic
/**		
 * - The functions in this script are used in every room in your game.
 * - Add your own variables and functions in here and you can access them with `Globals.` (eg: `Globals.m_myCoolInteger`)
 * - If you've used Adventure Game Studio, this is equivalent to the Global Script in that



*/
public partial class GlobalScript : GlobalScriptBase<GlobalScript>
{	
	////////////////////////////////////////////////////////////////////////////////////
	// Global Game Variables
	/// Just an example of using an enum for game state.
	/// This can be accessed from other scripts, eg: if ( Globals.gameStage == gameProgress.DrankWater )...
	public enum gameProgress
	{
		None,
		UsedBucket,
		TriedPump,
		RightParts,
		TonyPumped,
		TonyAte,
		SecondFlood,
		FixedPump, 
		BoughtHouse, 
		UsedElectricPump,
		WonGame
	};
	
	
	public gameProgress gameStage = gameProgress.None;
	
	// tutorial sequence variables
	public enum tutorialProgress {start, clickedBucket, selectedBucket, usedBucket, complete};
	public tutorialProgress tutorialStage= tutorialProgress.start;
	
	public bool LoadingChapter = false;
	public int ChapterToLoad = -1;
	
	public bool rained = false;
	
	/// Just an example of using a global variable that can be accessed in any room with `Globals.m_spokeToBarney`.
	/// All variables like this in Quest Scripts are automatically saved
	// public bool
	
	////////////////////////////////////////////////////////////////////////////////////
	// Global Game Functions
	
	/// Called when game first starts
	
	
	
	public void OnGameStart()
	{     
		// GAME STAGE
		Globals.gameStage = (gameProgress)0;
		//G.Explanation.Show();
		E.DeleteSave(1);
		
		
		//Debug.Log(stage);
		
		
		I.CellPhone.Add();
		SetInventory();
		C.Tony.Disable();
		
		// GUI
		//G.Toolbar.Hide();
		//E.WaitForGui(G.ExplanationOld);
		//E.WaitForGui(G.Explanation);
		
		
		// temporary
		I.RepairKit.Add();
		
		// I.BilgePump.Add();
		 // C.Tony.Enable();
		 // C.Tony.ChangeRoom(R.Home);
		
		//I.Beer.Add();
		/*
		
		 I.BilgePump.Add();
		I.MediumHandle.Add();
		 I.LargeHandle.Add();
		I.MediumHose.Add();
		 I.LargeHose.Add();
		
		
		*/
		
		
		
		
		
		
		
	} 

	/// Called after restoring a game. Use this if you need to update any references based on saved data.
	public void OnPostRestore(int version)
	{
	}

	/// Blocking script called whenever you enter a room, before fading in. Non-blocking functions only
	public void OnEnterRoom()
	{
		
		
	}

	/// Blocking script called whenever you enter a room, after fade in is complete
	public IEnumerator OnEnterRoomAfterFade()
	{
		if (R.Current != R.Home){
		   LowerWaterShader(0,"CharacterDave");
		   
		}
		yield return E.Break;
	}

	/// Blocking script called whenever you exit a room, as it fades out
	public IEnumerator OnExitRoom( IRoom oldRoom, IRoom newRoom )
	{
		Audio.StopMusic(0.25f);
		yield return E.Break;
	} 

	/// Blocking script called every frame when nothing's blocking, you can call blocking functions in here that you'd like to occur anywhere in the game
	public IEnumerator UpdateBlocking()
	{
		
		
		yield return E.Break;
	}

	/// Called every frame. Non-blocking functions only
	public void Update()
	{
		// PowerQuest.Get.ResetWalkClickDown();
		
		// yield return E.WaitUntil( ()=> Input.GetMouseButtonUp(0));
		
		Cursor.ResetAnimationOverride();
		
		if (Input.GetKeyDown("s"))
				{
				   E.Save(1, "test");
				}
		
		 if (Input.GetKeyDown("l"))
				{
				   E.RestoreSave(1);
				}
		
		if (Input.GetKeyDown(KeyCode.Escape))
				{
				   E.Save(1, "test");
				   C.Dave.ChangeRoom(R.Menu);
				}
		
		
		
		/*
		// set game stage
		
		// set to beginning of game
		if (Input.GetKeyDown(KeyCode.Alpha1)){
			E.Restart();
		}
		// set to end of tutorial
		if (Input.GetKeyDown(KeyCode.Alpha2)){
			E.FadeOut();
			Globals.gameStage = gameProgress.UsedBucket;
			Globals.tutorialStage = tutorialProgress.complete;
			//StageComplete();
			C.Dave.ClearInventory();
			I.CellPhone.Add();
			I.Bucket.Add();
			C.Dave.ChangeRoomBG(R.Home);
			E.FadeIn();
		}
		*/
		
	}

	/// Blocking script called whenever the player clicks anywwere. This function is called before any other click interaction. If this function blocks, it will stop any other interaction from happening.
	public IEnumerator OnAnyClick()
	{
		// yield return E.WaitUntil( ()=> Input.GetMouseButtonUp(0));
		yield return E.Break;
	}

	/// Blocking script called whenever the player tries to walk somewhere. Even if `C.Player.Moveable` is set to false.
	public IEnumerator OnWalkTo()
	{
		//yield return E.WaitUntil( ()=> Input.GetMouseButtonUp(0));
		//E.ProcessClick( eQuestVerb.Walk );
		//E.ProcessClick( E.GetMouseOverType() );
		// Vector2 coords = Cursor.PositionOverride;
		
		// E.WaitUntil( ()=> Input.GetMouseButtonUp(0));
		// C.Player.WalkToBG(E.GetMousePosition());
		//Debug.Log(Input.GetMouseButtonUp(0));
		
		//C.Player.WalkTo(coords);
		
		//while (!Input.GetMouseButtonUp(0)) yield return E.Wait(0);
		
		yield return E.Break;
	}



	/// Called when the mouse is clicked in the game screen. Use this to customise your game interface by calling E.ProcessClick() with the verb that should be used. By default this is set up for a 2 click interface
    public void OnMouseClick( bool leftClick, bool rightClick )
    {
		bool mouseOverSomething = E.GetMouseOverClickable() != null;
		// Debug.Log(isDragging);
		
		// if player is not walking, clicks are enabled, if they are walking, they cannot click until they are done
		if (!C.Player.Walking) {
			//E.WaitUntilRelease();
			// Check if should clear inventory
			if ( C.Player.HasActiveInventory && ( rightClick || (mouseOverSomething == false && leftClick ) || Cursor.NoneCursorActive ) )
			{
				// Clear inventory on Right click, or left click on empty space, or on hotspot with cursor set to "None"
				I.Active = null;
			}
			else if ( Cursor.NoneCursorActive ) // Checks if cursor is set to "None"
			{
				// Special case for clickables with cursor set to "None"- Don't do anything
			}
			else if ( E.GetMouseOverType() == eQuestClickableType.Gui )  // Checks if clicked on a gui
			{
				// Clicked on a gui - Don't do anything
			}
			else if ( leftClick ) // Checks if player left clicked
			{
				if ( mouseOverSomething ) // Check if they clicked on anything
				{
					// Check if they clicked inventory, or something else
					if ( C.Player.HasActiveInventory && Cursor.InventoryCursorOverridden == false )
					{
						// Left click with active inventory, use the inventory item
						
						Cursor.AnimationOverride = "Active";
						E.ProcessClick( eQuestVerb.Inventory );
						I.Active = null;
						// This is to have the ordinary cursor show during blocking scenes
						
						/*
						// Save active inventory item
						eQuestVerb activeInventoryItem = eQuestVerb.Inventory;
						Debug.Log(activeInventoryItem);
						Debug.Log(eQuestVerb.Inventory);
						// Set inventory item to null
						
						E.ProcessClick(activeInventoryItem);
						*/
					}
					else
					{
						// Left click on item, so use it
						E.ProcessClick(eQuestVerb.Use);
					}
				}
				else  // They've clicked empty space
				{
		
		
					E.ProcessClick( eQuestVerb.Walk );
		
				}
			}
			else if ( rightClick )
			{
				// If right clicked something, look at it (if 'look' enabled in PowerQuest Settings)
				if ( mouseOverSomething )
					E.ProcessClick( eQuestVerb.Look );
			}
		}
	}

	////////////////////////////////////////////////////////////////////////////////////
	// Unhandled interactions

	/// Called when player interacted with something that had not specific "interact" script
	public IEnumerator UnhandledInteract(IQuestClickable mouseOver)
	{		
		// This function is called when the player interacts with something that doesn't have a response
		/*
		if ( R.Current.ScriptName == "Title")
			yield break;
		
		// This bit of logic cycles between three options. The '% 3' makes it cycle between 3 options.
		int option = E.Occurrence("unhandledInteract") % 3;
		if ( option == 0 )	
			yield return C.Display("You can't use that", 20);
		else if ( option == 1 )
			yield return C.Display("That doesn't work", 21);
		else if ( option == 2 )
			yield return E.Display("Nothing happened", 22);
			*/
			
			// We just want nothing to happen
			
			yield break; 
		
	}

	/// Called when player looked at something that had not specific "Look at" script
	public IEnumerator UnhandledLookAt(IQuestClickable mouseOver)
	{
		// This function is called when the player looks at something that doesn't have a response
		
		// In the title screen we don't want any response when looking at things, so we 'return' to stop the script
		if ( R.Current.ScriptName == "Title")
			yield break;
		
		// This bit of logic randomly chooses between three options
		int option = UnityEngine.Random.Range(0,3);

	
		if ( option == 0 )	
			yield return C.Display("It's nothing interesting ", 23);
		else if ( option == 1 )
			yield return C.Display("You don't see anything", 24);
		else if ( option == 2 ) // in this one we do some fancy manipulation to include the name of what was clicked
			yield return C.Display("The "+ mouseOver.Description.ToLower() + " isn't very interesting", 25);
	}

	/// Called when player used one inventory item on another that doesn't have a response
	public IEnumerator UnhandledUseInvInv(Inventory invA, Inventory invB)
	{
		// Called when player used one inventory item on another that doesn't have a response
		yield return C.Display( "You can't use those together", 26); 

	}

	/// Called when player used inventory on something that didn't have a response
	public IEnumerator UnhandledUseInv(IQuestClickable mouseOver, Inventory item)
	{		
		// This function is called when the uses an item on things that don't have a response
		yield return C.Display( "You can't use that", 27); 
	}

	    /// Called when a player clicked an inventory item that didn't have an interaction
    public IEnumerator UnhandledInteractInventory(IInventory item)
    {        
        // This function is called when an item in the inventory is clicked that doesn't have a "use" interaction
        E.ActiveInventory = item;
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

	public void SetInventory()
	{
		C.Dave.ClearInventory();
		I.CellPhone.Add();
		switch((int)Globals.gameStage)
		{
			case(0):
				break;
			case(1):
				I.Bucket.Add();
				break;
			case(2):
				break;
			case(4):
				C.Tony.Enable();
				break;
		
			case(7):
				I.RepairKit.Add();
				break;
			default:
				break;
		
		}
	}
}