using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

namespace PowerTools.QuestGui
{

[System.Serializable] 
[AddComponentMenu("Quest Gui/InventoryPanel")]
public partial class InventoryPanel : GuiControl, IInventoryPanel
{
	//////////////////////////////////////////////////////////////////////////////////////////
	// Editor vars
	
	[Tooltip("Name of character to show inventory of. If empty will use the current player")]
	[SerializeField] string m_targetCharacter = null;
	[SerializeField] bool m_reverseOrder = false;
	[Tooltip("Sets the cursor to show if hovering over the item")]
	[SerializeField] string m_itemCursor = null;
	[SerializeField] InventoryPanelItem m_itemPrefab = null;
	[SerializeField] Button m_buttonScrollBack = null;
	[SerializeField] Button m_buttonScrollForward = null;
	[SerializeField] SpriteMask m_mask = null;
	
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Private vars
	
	GridContainer m_grid = null;
	
	Character.CollectedItem m_lastCollectedItem = null;	
	Vector2 m_itemOffset = Vector2.zero;	
	
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Functions: IInventoryPanel interface	
	
	public ICharacter TargetCharacter 
	{
		get { return string.IsNullOrEmpty(m_targetCharacter)  ? PowerQuest.Get.GetPlayer() : PowerQuest.Get.GetCharacter(m_targetCharacter); }
		set 
		{ 
			m_targetCharacter = value.ScriptName; 
			// Update layout?
		}
	}
	
	public IQuestClickable IClickable { get{ return this; } }

	public Vector2 ScrollOffset {get=> m_grid.ScrollOffset; set=>m_grid.ScrollOffset=value;}
	
	public void NextRow()    { m_grid.NextRow();    }
	public void NextColumn() { m_grid.NextColumn(); }
	public void PrevRow()    { m_grid.PrevRow();    }
	public void PrevColumn() { m_grid.PrevColumn(); }
		
	public bool HasNextColumn() { return m_grid.HasNextColumn(); }
	public bool HasPrevColumn() { return m_grid.HasPrevColumn(); }
	public bool HasNextRow()    { return m_grid.HasNextRow();    }
	public bool HasPrevRow()    { return m_grid.HasPrevRow();    }
	
	public bool ScrollForward() 
	{
		if ( HasNextColumn() )
		{
			NextColumn();
			return true;
		}
		else if ( HasNextRow() )
		{
			NextRow();
			return true;
		}		
		return false;
	}
	public bool ScrollBack() 
	{
		if ( HasPrevColumn() )
		{
			PrevColumn();
			return true;
		}
		else if ( HasPrevRow() )
		{
			PrevRow();
			return true;
		}		
		return false;
	}

	// Inventory Panel override's custom size using values from grid
	public override RectCentered CustomSize 
	{ 		
		get
		{
			GridContainer grid = GetComponent<GridContainer>();
			if ( grid == null )
				return RectCentered.zero;
			return grid.Rect;
			
		} 
		set
		{
			GridContainer grid = GetComponent<GridContainer>();
			if ( grid == null )
				return;
			grid.Rect = value;
		} 
	}
	
	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public override RectCentered GetRect(Transform excludeChild = null)
	{	
		return CustomSize;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Functions: Public (Non interface)
	
	
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Component: Functions: Unity
	
	// Use this for initialization
	void Awake() 
	{	
		m_grid = GetComponentInChildren<GridContainer>();
		if ( m_buttonScrollBack != null )
			m_buttonScrollBack.OnClick += OnBackButton;
		if ( m_buttonScrollForward != null )
			m_buttonScrollForward.OnClick += OnForwardButton;
	}

	// Start is called before the first frame update
	protected override void Start()
	{
		base.Start();
	}

	void OnDestroy()
	{
		if ( m_buttonScrollBack != null )	
			m_buttonScrollBack.OnClick -= OnBackButton;			
		if ( m_buttonScrollForward != null )	
			m_buttonScrollForward.OnClick -= OnForwardButton;
	}

	bool IsMouseOverItem()
	{
		if ( PowerQuest.Get.GetMouseOverType() == eQuestClickableType.Inventory && PowerQuest.Get.GetFocusedGui() == (IGui)m_gui )
		{			
			for (int i = 0; i < m_grid.Items.Count; ++i )
			{
				if ( m_grid.GetItemVisible(i) )
				{
					Transform trans = m_grid.Items[i];
					if ( trans == PowerQuest.Get.GetFocusedGuiControl().Instance.transform )
					{
						return true;
					}
				}
			}
		}
		return false;

	}

	Transform GetMouseOverItem()
	{
		
		if ( PowerQuest.Get.GetMouseOverType() == eQuestClickableType.Inventory && PowerQuest.Get.GetFocusedGuiControl() != null )
		{
			for (int i = 0; i < m_grid.Items.Count; ++i )
			{
				if ( m_grid.GetItemVisible(i) )
				{
					Transform trans = m_grid.Items[i];
					if ( trans == PowerQuest.Get.GetFocusedGuiControl().Instance.transform )
					{
						return trans;
					}
				}
			}
		}
		return null;
	}

	void Update()
	{
		UpdateItems();		
		
		Character character = TargetCharacter as Character;
		List<Character.CollectedItem> inventory = character.GetInventory();

		// If last collected item changed, scroll to it automatically		
		if (inventory.Count > 0 && inventory[inventory.Count-1] != m_lastCollectedItem)
		{			
			if ( m_reverseOrder )
			{
				// Scroll to start
				m_grid.ScrollOffset = Vector2.zero;
			}
			else 
			{
				// scroll to end
				while ( m_grid.HasNextColumn() )
					m_grid.NextColumn();
				while ( m_grid.HasNextRow() )
					m_grid.NextRow();
			}

			// take note of the last collected item
			m_lastCollectedItem = inventory[inventory.Count-1];
		}
		
		if ( (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ) && GetMouseOverItem() )
		{			
			if ( PowerQuest.Get.OnInventoryClick() )
			{
				// Unpause gui if it's paused
				GetComponentInParent<GuiDropDownBar>()?.Hide();
			}
		}


		if ( m_mask )
		{
			int sortOrder = -Mathf.RoundToInt((GuiData.Baseline * 100.0f) + Baseline);
			int layer = SortingLayer.NameToID("Gui");
			//m_mask.sortingLayerName = "Gui";
			m_mask.backSortingLayerID = layer;
			m_mask.frontSortingLayerID = layer;
			m_mask.frontSortingOrder = sortOrder;
			m_mask.backSortingOrder = sortOrder-1;

			// Size and position mask to match rect
			m_mask.transform.position = m_grid.Rect.Center.WithZ(0);
			m_mask.transform.localScale = m_grid.Rect.Size.WithZ(1);
		}


	}


	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Funcs: Private Internal
	
	GameObject CreateButton()
	{
		GameObject obj = null;
		if( m_itemPrefab != null )
		{
			obj = GameObject.Instantiate(m_itemPrefab.gameObject) as GameObject;
		}
		else
		{
			// Try just creating the button without a specific prefab... Dunno if this will work
			obj = new GameObject("InvItem", typeof(InventoryPanelItem),typeof(GuiControl),typeof(SpriteAnim),typeof(PowerSprite));
			obj.layer = LayerMask.NameToLayer("UI");
			BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
			box.size = m_grid.ItemSpacing.WithOffset(-2,-2);
			box.isTrigger = true;
			obj.GetComponent<SpriteRenderer>().sortingLayerName="Gui";
		}

		obj.AddComponent<InventoryComponent>();
		obj.transform.SetParent(m_grid.transform,false);

		// Once its set up, set the baseline
		GuiControl control = obj.GetComponent<GuiControl>();
		if ( control == null )
			obj.AddComponent<GuiControl>();
		control.Baseline = Baseline;
		control.SetGui(GuiData);

		if ( m_mask != null )
		{
			SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
			foreach( SpriteRenderer renderer in renderers)
			{
				renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
			}
		}

		return obj;
	}

	void UpdateItems()
	{
		Character character = TargetCharacter as Character;
		List<Character.CollectedItem> inventory = character.GetInventory();
		
		// add to end if not enough
		while ( m_grid.Items.Count < inventory.Count )
		{
			GameObject obj = CreateButton();
			m_grid.AddItem(obj.transform);			
		}
				
		// remove from end if too many
		while ( m_grid.Items.Count > inventory.Count )
		{
			Transform toRemove = m_grid.Items[m_grid.Items.Count-1];
			m_grid.RemoveItem(toRemove);
			if ( toRemove != null )
				GameObject.Destroy(toRemove.gameObject);
		}
				
		// Update data on all visible items
		for ( int i = 0; i < m_grid.Items.Count; ++i )
		{
			// check it's visible, don't bother otherwise
			if ( m_grid.GetItemVisible(i) == false )
				continue; 
			
			Inventory inventoryItem = PowerQuest.Get.GetInventory( m_reverseOrder ? inventory[inventory.Count-1-i].m_name : inventory[i].m_name );
			// Set data of the InventoryComponent in the control. So it can be retrieved when you click
			m_grid.Items[i].GetComponent<InventoryComponent>().SetData(inventoryItem);

			// Override cursor
			if ( IsString.Set(m_itemCursor) )
				inventoryItem.Cursor=m_itemCursor;
			
			// Set up visuals of the InventoryPanelItem
			InventoryPanelItem panelItem = m_grid.Items[i].GetComponent<InventoryPanelItem>();
			if ( panelItem.GetCachedAnimSpriteName() != inventoryItem.AnimGui) 
			{
				AnimationClip clip = GuiComponent.GetAnimation(inventoryItem.AnimGui);
				if ( clip != null )
					panelItem.SetInventoryAnim( clip );
				else if ( clip == null )
					panelItem.SetInventorySprite( GuiComponent.GetSprite(inventoryItem.AnimGui) );	
			}
		}

		// Update forward/back arrows on grid.. This could really be in teh grid itself instead of inventory panel?
		if ( m_buttonScrollForward != null )
		{
			if ( m_grid.HasNextColumn() || m_grid.HasNextRow() )
				m_buttonScrollForward.Show();
			else 
				m_buttonScrollForward.Hide();
		}

		if ( m_buttonScrollBack != null )
		{
			if ( m_grid.HasPrevColumn() || m_grid.HasPrevRow() )
				m_buttonScrollBack.Show();
			else 
				m_buttonScrollBack.Hide();
		}
	}
		/* NB: This was never used
	void OnSetVisible()
	{
		if ( gameObject.activeSelf == false && Visible)
			gameObject.SetActive(true);
		
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		foreach( Renderer renderer in renderers )
		{   
			renderer.GetComponent<Renderer>().enabled = Visible;
		}
	}*/
	
	void OnForwardButton(GuiControl control) { ScrollForward(); }
	void OnBackButton(GuiControl control) { ScrollBack(); }

	// Handles setting up defaults incase items have been added or removed since last loading a save file
	/*[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}*/

}


}
