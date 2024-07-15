using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Reflection;
using PowerTools;
using PowerTools.QuestGui;

//
// PowerQuest Gui stuff: Toss stuff in here while working on it lol
//

namespace PowerTools.Quest
{

public partial class PowerQuest
{

	// Gui managment stuff
	Gui m_focusedGui = null;
	GuiControl m_focusedControl = null;
	GuiControl m_keyboardFocusedControl = null;
	IGui m_blockingGui = null;
	GuiControl m_focusedControlLock = null;
		
	// Used when focus should not change, even though mouse is still active (eg: when in middle of pressing button/transitioning menu out, etc)
	public void LockFocusedControl() { m_focusedControlLock = m_focusedControl; }
	public void UnlockFocusedControl() {m_focusedControlLock=null;}
	
	// Called when gui input is pressed- pass it through to gui system and record presses for queries.
	public bool NavigateGui(eGuiNav input = eGuiNav.Ok) { return QuestMenuManager.Get.NavigateGui(input); }
	// Return whether key was pressed this frame (or repeated) and marks the input as having been "used" so other thigns won't use it
	public bool ConsumeGuiKey(eGuiNav input = eGuiNav.Ok) { return QuestMenuManager.Get.ConsumeGuiKey(input); }

	// Called from PowerQuest update
	void UpdateGuiFocus()
	{
		if ( m_focusedControlLock != null )
		{
			// Check that control lock is still enabled
			if ( m_focusedControlLock.gameObject.activeInHierarchy == false )
				UnlockFocusedControl();			
		}
		if ( m_focusedControlLock != null )
		{
			m_mouseOverClickable = m_focusedControl;
			return;
		}

		Gui prevFocusedGui = m_focusedGui;
		GuiControl prevFocusedControl = m_focusedControl;
		
		GameObject pickedGameObject = null;
		
		m_focusedControl = null;				
		m_focusedGui = null;

		if ( m_overrideMouseOverClickable == false )
		{
			m_mouseOverClickable = null;			

			// Check UI elements getting the hit/hover

			// First check if mouse is over Unity Gui (checking "event" system)
			if ( EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) 
			{			
				List<RaycastResult> raycastResults = new List<RaycastResult>();
				EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Input.mousePosition }, raycastResults);
				foreach (RaycastResult raycastResult in raycastResults)
				{
					if ( m_inventoryClickStyle == eInventoryClickStyle.OnMouseClick ) // Old games don't handle inventory clicks in OnMouseClick
					{
						InventoryComponent invComponent = raycastResult.gameObject.GetComponentInParent<InventoryComponent>();
						if ( invComponent != null && invComponent.GetData().Clickable )
						{
							m_mouseOverClickable = invComponent.GetData();
							break;
						}
					}

					GuiComponent guiComponent = raycastResult.gameObject.GetComponentInParent<GuiComponent>();
					if ( guiComponent?.GetData()?.Clickable ?? false )
					{
						m_mouseOverClickable = guiComponent.GetData();
						break;
					}
				}
	 		}
			

			// If mouse isn't over Unity Gui then check other clickables
			if ( m_mouseOverClickable == null && SV.m_captureInputSources.Count <= 0 )
	 		{
				// Check if mouse is over gui/inv first (in UI layer)
				if ( m_cameraGui != null )
				{
					m_mouseOverClickable = GetObjectAt(m_mousePosGui, (1<<LAYER_UI), out pickedGameObject ); // Find object under mouse in gui layer
				}

				if ( m_mouseOverClickable == null && GetModalGuiActive() == false ) // if not null, the mouse is over a clickable gui
				{
					m_mouseOverClickable = GetObjectAt(m_mousePos, ~(1<<LAYER_UI), out pickedGameObject ); // Find object under mouse (excluding gui)
				}
			}			

		}

		// Update gui focus
		if ( m_mouseOverClickable != null )
		{
			if ( m_mouseOverClickable.ClickableType == eQuestClickableType.Gui || m_mouseOverClickable.ClickableType == eQuestClickableType.Inventory )
			{
				if ( m_mouseOverClickable.ClickableType == eQuestClickableType.Inventory )
				{				
					if ( pickedGameObject != null )
					{
						m_focusedControl = pickedGameObject.GetComponent<GuiControl>();
						// Find control's gui
						if ( m_focusedControl != null )
							m_focusedGui = m_focusedControl.GuiData;
						else 
							m_focusedGui = null;		
					}
				}
				else if ( m_mouseOverClickable is GuiControl )
				{				
					m_focusedControl = m_mouseOverClickable as GuiControl;
					// Find control's gui
					if ( m_focusedControl != null )
						m_focusedGui = m_focusedControl.GuiData;
					else 
						m_focusedGui = null;				
				}
				else 
				{
					m_focusedGui = m_mouseOverClickable as Gui;
				}	

			}
		}			

		// Check if there's a modal gui above what the mouse is over, and if so, that overrides the focus, even if it's not "pickable"
		Gui modalGui = GetTopModalGui();
		if ( modalGui != null && modalGui != m_focusedGui && (m_focusedGui == null || modalGui.Baseline < m_focusedGui.Baseline) )
		{
			if ( modalGui.Clickable )
			{
				m_mouseOverClickable = modalGui;
				m_focusedGui = modalGui;
			}
			else 
			{
				m_mouseOverClickable = null;
				m_focusedGui = null;
			}
			m_focusedControl = null;
		}

		if ( prevFocusedGui != m_focusedGui )
		{
			if ( prevFocusedGui != null  )
				prevFocusedGui.OnDefocus();
			if ( m_focusedGui != null && m_focusedGui.Instance != null )
				m_focusedGui.OnFocus();			
		}

		if ( prevFocusedControl != m_focusedControl )
		{
			if ( prevFocusedControl != null )
				prevFocusedControl.OnDefocus();
			if ( m_focusedControl != null )
				m_focusedControl.OnFocus();
		}
	}

	private List<Gui> m_sortedGuis = new List<Gui>();

	// Called from PQ update
	void UpdateGuiVisibility()
	{
		// Sort guis by baseline
		m_sortedGuis.Clear();
		m_sortedGuis.AddRange(m_guis);
		m_sortedGuis.Sort((Gui a, Gui b)=> a.Baseline.CompareTo(b.Baseline));

		bool hideRemaining = false;

		// Loop through from front to back, and hide any that are obscured, or if cutscene is active
		foreach ( Gui gui in m_sortedGuis )
		{			
			if ( hideRemaining )
				gui.HiddenBySystem = true;		
			else if ( gui.VisibleInCutscenes == false && (GetBlocked() || m_queuedScriptInteractions.Count > 0 ))
				gui.HiddenBySystem = true;
			else
				gui.HiddenBySystem = false;

			if ( gui.Visible && gui.HideObscuredGuis )
					hideRemaining = true;
		}
		// Hide guis that specifically want others hidden
		foreach ( Gui gui in m_sortedGuis )
		{
			// Hide any specfiic guis on sorted guis
			if ( gui.Visible )
			{
				foreach( string toHide in gui.HideSpecificGuis )
				{
					Gui toHideG = GetGui(toHide);
					if ( toHideG != null )
						toHideG.HiddenBySystem = true;
				}
			}
		}
	}
	
	public IGui GetBlockingGui() {return m_blockingGui;}
	public Coroutine WaitForGui(IGui gui) {	return StartQuestCoroutine(CoroutineWaitForGui(gui)); }
	
	public void OnGuiShown(Gui gui)
	{
		// Call OnShow on gui script
		if ( gui == null )
			throw new System.Exception("gui is null");
		if ( gui.GetScript() != null )
		{
			System.Reflection.MethodInfo method = gui.GetScript().GetType().GetMethod( "OnShow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			if ( method != null ) method.Invoke(gui.GetScript(),null);
		}		
	}

	IEnumerator CoroutineWaitForGui(IGui gui)
	{
		bool wasCutscene = m_cutscene;
		if ( wasCutscene )
			EndCutscene();
					
		// Set the blocking gui so it can be updated while blocked
		m_blockingGui = gui;
		gui.Show();

		bool hideCursor = GetCursor().HideWhenBlocking;
		GetCursor().HideWhenBlocking = false;
		GetCursor().Visible = true;
		yield return WaitWhile(()=> m_blockingGui.Visible ); // NB: Real risk of soft-locking if something else is causing gui to be hidden or un-clickable
		GetCursor().HideWhenBlocking = hideCursor;
		m_blockingGui=null;

		if (wasCutscene)
			StartCutscene();

		yield return Break;
	}

	// Checks if a particular gui can be focused or clicked on. (ie: there's no modal guis on top of it)
	public bool GetIsGuiObscuredByModal(Gui gui)
	{
		for ( int i = 0; i < m_guis.Count; ++i )
		{
			Gui other = m_guis[i];
			if ( other.Visible && other.HiddenBySystem == false && other.Baseline <= gui.Baseline && other.Modal )
				return true;
		}
		return false;
	}
	


}

}
