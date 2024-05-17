using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;

namespace PowerTools.Quest
{

#region Class: Quest Cursor 
[System.Serializable] 
public partial class QuestCursor : ICursor
{
	
	public enum eInventoryOutlineOnGui { Never, OtherItemsOnly, Always }

	//
	// Default values set in inspector
	//

	#endregion
	#region QuestCursor Editor variables
	[SerializeField] bool m_visible = true;
	[SerializeField] string m_animationClickable = "Active";
	[SerializeField] string m_animationNonClickable = "Idle";
	[SerializeField] string m_animationUseInv = "UseInv";
	[SerializeField] string m_animationOverGui = "Idle";
	[SerializeField] string m_animationWait = "Wait";
	[SerializeField] Color m_inventoryOutlineColor = new Color(1,1,1,0);
	[Tooltip("Controls when the inventory outline shows when hovered over other inventory items")]
	[SerializeField] eInventoryOutlineOnGui m_inventoryOutlineOnGui = eInventoryOutlineOnGui.OtherItemsOnly;
	[Tooltip("If true, the cursor will be hidden when the game isn't interactive")]
	[SerializeField] bool m_hideWhenBlocking = true;


	#endregion
	#region QuestCursor private variables

	GameObject m_prefab = null;
	QuestCursorComponent m_instance = null;
	
	string m_animationOverride = null;

	#endregion
	#region QuestCursor Public functions/properties

	public MonoBehaviour Instance { get{ return m_instance; } }
	public bool Visible 
	{ 
		get{ return m_visible;} 
		set
		{ 
			m_visible = value;
			if ( m_instance ) m_instance.SetVisible(m_visible && (m_hideWhenBlocking == false || PowerQuest.Get.GetBlocked() == false ));
		}
	}

	public void PlayAnimation(string animation) { if ( m_instance != null ) m_instance.PlayAnimation(animation); }
	public void StopAnimation() { if ( m_instance != null ) m_instance.StopAnimation(); }

	public string AnimationOverride
	{		
		get{ return m_animationOverride; } 
		set{ m_animationOverride = value; OnChangeAnimation(); }
	}
	
	/// Disables any AnimationOverride, returning to default behaviour
	public void ResetAnimationOverride() { m_animationOverride = null; OnChangeAnimation(); }

	public string AnimationClickable 
	{ 
		get{ return m_animationClickable; } 
		set{ m_animationClickable = value; OnChangeAnimation(); }
	}

	public string AnimationNonClickable 
	{ 
		get{ return m_animationNonClickable; } 
		set{ m_animationNonClickable = value; OnChangeAnimation(); }
	}

	public string AnimationUseInv
	{ 
		get{ return m_animationUseInv; } 
		set{ m_animationUseInv = value; OnChangeAnimation(); }
	}

	public string AnimationOverGui 
	{ 
		get{ return m_animationOverGui; } 
		set{ m_animationOverGui = value; OnChangeAnimation(); }
	}
	public string AnimationWait
	{ 
		get{ return m_animationWait; } 
		set{ m_animationWait = value; OnChangeAnimation(); }
	}

	public bool HideWhenBlocking { get {return m_hideWhenBlocking; } set{m_hideWhenBlocking = value; OnChangeAnimation(); } }

	public Color InventoryOutlineColor
	{
		get{ return m_inventoryOutlineColor; }
		set{ m_inventoryOutlineColor = value; OnChangeAnimation(); }
	}
	public eInventoryOutlineOnGui InventoryOutlineOnGui
	{
		get{ return m_inventoryOutlineOnGui; }
		set{ m_inventoryOutlineOnGui = value; OnChangeAnimation(); }
	}

	public GameObject GetPrefab() { return m_prefab; }
	public QuestCursorComponent GetInstance() { return m_instance; }
	public void SetInstance(QuestCursorComponent instance) 
	{ 
		m_instance = instance; 
		m_instance.SetData(this);
	}

	public bool NoneCursorActive { get { return m_instance.GetNoneCursorActive(); } }
	public bool InventoryCursorOverridden { get { return m_instance.GetInventoryCursorOverridden(); } }
	
	// Accessors to mouse override in PowerQuest
	public Vector2 PositionOverride { get { return PowerQuest.Get.GetMousePosition(); } set { PowerQuest.Get.SetMousePositionOverride(value); } }
	public bool HasPositionOverride { get { return PowerQuest.Get.GetHasMousePositionOverride(); } }
	public void SetPositionOverride(Vector2 position) { PowerQuest.Get.SetMousePositionOverride(position); }
	public void ClearPositionOverride() {  PowerQuest.Get.ResetMousePositionOverride(); }
	
	#endregion
	#region QuestCursor Initialisation functions

	public void EditorInitialise( string name ) {}
	public void EditorRename( string name ) {}

	public void OnPostRestore( int version, GameObject prefab )
	{
		m_prefab = prefab;
	}
	public void Initialise( GameObject prefab )
	{
		m_prefab = prefab;
	}

	void OnChangeAnimation() { if ( m_instance) m_instance.OnChangeAnimation(); }
}

#endregion
#region Cursor Monobehaviour

public partial class QuestCursorComponent : MonoBehaviour 
{
	static readonly string STR_NONE = "None";

	#endregion
	#region Component Editor variables

	[SerializeField] QuestCursor m_data = new QuestCursor();
	[Tooltip("List of animations that cursor will use even if an inv item is selected")]
	[SerializeField] List<string> m_inventoryOverrideAnims = new List<string>();
	[SerializeField, ReadOnly] List<AnimationClip> m_animations =  new List<AnimationClip>();
	[SerializeField, ReadOnly] List<Sprite> m_sprites =  new List<Sprite>();

	#endregion
	#region Component Private Variables

	SpriteRenderer m_sprite = null;
	SpriteAnim m_spriteAnimator = null;
	bool m_noneCursor = false; // Flag that's set when "none" cursor is used
	bool m_inventoryOverride = false; // Flag that's set when inventoryOverride anim active
	PowerSprite m_powerSprite = null;

	string m_playingAnim = null;

	#endregion
	#region Component Public functions

	public bool GetNoneCursorActive() { return m_noneCursor; }
	public bool GetInventoryCursorOverridden() { return m_inventoryOverride; }

	//bool m_overGUI = false;
	public QuestCursor GetData() { return m_data; }
	public void SetData(QuestCursor data) { m_data = data; }

	public void SetVisible(bool visible )
	{
		GetComponent<Renderer>().enabled = visible;
	}

	public List<AnimationClip> GetAnimations() { return m_animations; }
	public List<Sprite> GetSprites() { return m_sprites; }
	
	public string CurrentAnim { get => m_currAnim; }
	public SpriteRenderer SpriteRenderer { get => m_sprite; }

	public AnimationClip GetAnimation(string animName) 
	{	
		AnimationClip clip = QuestUtils.FindByName(GetAnimations(), animName);
		// If not found in own list of anims, try in the inventory
		if ( clip == null )
			clip = PowerQuest.Get.GetInventoryAnimation(animName);			
		return clip;
	}
	public Sprite GetSprite(string animName) 
	{ 
		Sprite sprite = PowerQuest.FindSpriteInList(m_sprites, animName);
		// If not found in own list of anims, try in the inventory
		if ( sprite == null )
			sprite = PowerQuest.Get.GetInventorySprite(animName);
		return sprite;
	}

	// Finds the clip and outline colour the cursor should have for the specified clickable
	public void CalcCursorVisuals(IQuestClickable clickable, out string newAnim, out Color outlineColor)
	{
		newAnim = null;
		outlineColor = new Color(1,1,1,0);

		// Handle PlayAnimation()
		if ( Utils.HasText(m_playingAnim) )
		{
			newAnim = m_playingAnim;
			return;
		}

		// Handle AnimationOverride
		if ( Utils.HasText(m_data.AnimationOverride) )
		{
			newAnim = m_data.AnimationOverride;
			return;
		}

		if ( PowerQuest.Get.GetBlocked() && PowerQuest.Get.GetCurrentDialog() == null && PowerQuest.Get.GetBlockingGui() == null )
		{
			newAnim = m_data.AnimationWait;
			return;
		}

		// Back-compatibility with dialog trees
		if ( PowerQuest.Get.GetCurrentDialog() != null && PowerQuest.Get.DialogTreeGui == "DialogTree")
			newAnim = m_data.AnimationOverGui;

		// Work out which cursor to be showing

		Character player = PowerQuest.Get.GetPlayer();

		string clickableCursor = (clickable == null || clickable.Cursor == null) ? string.Empty : clickable.Cursor;
		// Clickable
		if ( Utils.IsEmpty(newAnim) && clickable != null )
		{
			bool overInventoryOverrideCursor = m_inventoryOverrideAnims.Contains(clickableCursor); 
				

			// If over gui
			if ( clickable.ClickableType == eQuestClickableType.Gui || clickable.ClickableType == eQuestClickableType.Inventory )
			{
				Gui guiData = clickable as Gui;	
				GuiControl guiControl = clickable as GuiControl;						
				if ( clickable.ClickableType == eQuestClickableType.Inventory  )
					guiControl = PowerQuest.Get.GetFocusedGuiControl() as GuiControl;
				
				if ( guiControl != null )
					guiData = guiControl.GuiData;				

				// Show inv item cursor, if that's allowed in the gui, (or if the cursor is set to the string "Inventory", or some legacy support)
				if ( player.HasActiveInventory && ( // If inventory active, and 
					    clickableCursor.EqualsIgnoreCase(PowerQuest.STR_INVENTORY) // gui's cursor set to 'Inventory'
					|| (guiData != null && guiData.AllowInventoryCursor) // or guis cursor 'allowInventoryCursor' set to true
					|| (guiData==null && clickable.ClickableType == eQuestClickableType.Inventory) // or its a legacy inventory gui (guiData is null), and clickable is an inventory item
				))
				{	
					// If gui allows invetnory cursor, use that			
					if ( Utils.IsEmpty(clickable.Description) && Utils.IsEmpty(player.ActiveInventory.AnimCursorInactive) == false )
					{
						newAnim = player.ActiveInventory.AnimCursorInactive;					
					}
					else 
					{
						newAnim = player.ActiveInventory.AnimCursor;					

						// Set whether inventory outline shows when hovering over other items in gui
						if ( (m_data.InventoryOutlineOnGui == QuestCursor.eInventoryOutlineOnGui.Always
								|| (m_data.InventoryOutlineOnGui == QuestCursor.eInventoryOutlineOnGui.OtherItemsOnly && clickable != player.ActiveInventory ) )
							&& clickableCursor.EqualsIgnoreCase(STR_NONE) == false )
						{
							outlineColor = m_data.InventoryOutlineColor;
						}
					}
				}

				// if there's a cursor set on the gui, use that
				if ( Utils.IsEmpty(newAnim) )
					newAnim = clickableCursor;

				if ( Utils.IsEmpty(newAnim) )
					newAnim = m_data.AnimationOverGui;
			}

			// If there's an inventory item selected use that cursor, otherwise the pointer
			if ( Utils.IsEmpty(newAnim) && player.HasActiveInventory && overInventoryOverrideCursor == false )
			{		
				if ( clickableCursor.EqualsIgnoreCase(STR_NONE) == false )
					outlineColor = m_data.InventoryOutlineColor;
				if ( Utils.IsEmpty(player.ActiveInventory.AnimCursor) == false )
					newAnim = player.ActiveInventory.AnimCursor;
				else  
					newAnim = m_data.AnimationUseInv;
			}

			// Find function override on clickable
			if ( Utils.IsEmpty(newAnim) )
			{
				string cursorFunc = PowerQuest.SCRIPT_FUNCTION_GETCURSOR;
				if ( clickable.ClickableType != eQuestClickableType.Character )
					cursorFunc += clickable.ClickableType.ToString() + clickable.ScriptName;
				string overrideFromFunction = GetCursorScriptOverride(clickable.GetScript(),  cursorFunc );
				if ( Utils.IsEmpty(overrideFromFunction) == false )
					newAnim = overrideFromFunction;
			}

			// If cursor anim is overriden
			if ( IsString.Empty(newAnim) && IsString.Empty(clickableCursor) == false )	
			{				
				if ( player.HasActiveInventory == false || overInventoryOverrideCursor )
					newAnim = clickableCursor;
			}

			// Clickable
			if ( Utils.IsEmpty(newAnim) )
			{
				newAnim = m_data.AnimationClickable;
			}


		}

		// Not clickable -  If ther'es an inventory item selected show that
		if ( Utils.IsEmpty(newAnim) && player.HasActiveInventory 
			&& PowerQuest.Get.Paused == false  ) // Temporary Hack for dialog system) 
		{
			// Fall back to pointer anim if there's no inventory one, as a default
			if ( Utils.IsEmpty(player.ActiveInventory.AnimCursorInactive) == false )
				newAnim = player.ActiveInventory.AnimCursorInactive;
			else if ( Utils.IsEmpty(player.ActiveInventory.AnimCursor) == false )
				newAnim = player.ActiveInventory.AnimCursor;
			else
				newAnim = m_data.AnimationUseInv;				
		}

		// not clickable, and no inventory
		if ( Utils.IsEmpty(newAnim) ) 
		{
			newAnim = m_data.AnimationNonClickable;
		}

		bool noneCursor = newAnim.EqualsIgnoreCase(STR_NONE) || (clickableCursor.EqualsIgnoreCase(STR_NONE) );
		if ( noneCursor )
			outlineColor = new Color(1,1,1,0);

	}

	#endregion
	#region Component Private funcs

	void Awake () 
	{
		m_sprite = GetComponent<SpriteRenderer>();
		m_spriteAnimator = m_sprite.GetComponent<SpriteAnim>();	 

		m_powerSprite = m_sprite.GetComponent<PowerSprite>();
		//m_renderer = GetComponent<Renderer>();
	}

	// Use this for initialization
	void Start () 
	{		 
		m_spriteAnimator.Play( GetAnimations().Find( item=>string.Equals(m_data.AnimationClickable, item.name, System.StringComparison.OrdinalIgnoreCase) ) );
	}

	
	public void PlayAnimation(string animation) 
	{
		m_playingAnim=animation;
		m_spriteAnimator.Play( GetAnimation(animation) );
	}
	public void StopAnimation() 
	{ 		
		if ( Utils.IsEmpty(m_playingAnim) )
			return;
		
		m_playingAnim = null;
		if ( m_spriteAnimator.IsPlaying(m_playingAnim) )
			m_spriteAnimator.Stop();		
		OnChangeAnimation(); 
	}

	// Called from data when some anim changed
	public void OnChangeAnimation() { Update(); }

	string m_currAnim = null;

	void Update()
	{
		if ( m_data.HideWhenBlocking )
		{
			SetVisible((!PowerQuest.Get.GetBlocked()) && m_data.Visible);
		}

		Camera camMain = Camera.main;
		Camera cam = PowerQuest.Get.GetCameraGui();
		
		// translate world pos of mouse pos into gui camera pos
		if ( camMain != null && cam != null )
		{
			transform.position = cam.ScreenToWorldPoint(camMain.WorldToScreenPoint(PowerQuest.Get.GetMousePosition()) ).WithZ(0);
		}

		IQuestClickable clickable = PowerQuest.Get.GetMouseOverClickable();

		// Get the visuals
		string newAnim = null;
		Color outlineColor = new Color(1,1,1,0);
		CalcCursorVisuals(clickable, out newAnim, out outlineColor);

		// Play the anim if it's changed
		if ( m_currAnim != newAnim )
		{
			m_currAnim = newAnim;

			AnimationClip clip = GetAnimation(newAnim);
			if ( clip != null )
			{
				m_spriteAnimator.Play(clip);
			}
			else 
			{
				Sprite sprite = GetSprite(newAnim);
				if ( sprite != null )
				{
					m_spriteAnimator.Stop();
					m_sprite.GetComponent<SpriteRenderer>().sprite=sprite;
				}
			}
		}		
				
		m_noneCursor = m_currAnim.EqualsIgnoreCase(STR_NONE) || (clickable != null && clickable.Cursor != null && clickable.Cursor.EqualsIgnoreCase(STR_NONE));

		if ( m_noneCursor )
			outlineColor = new Color(1,1,1,0);
		m_inventoryOverride = m_inventoryOverrideAnims.Contains(m_currAnim);

		if ( m_powerSprite != null )
			m_powerSprite.Outline = outlineColor;
	}

	string GetCursorScriptOverride( QuestScript scriptClass, string methodName )
	{
		string result = null;
		if ( scriptClass != null )
		{
			System.Reflection.MethodInfo method = scriptClass.GetType().GetMethod( methodName );
			if ( method != null )
			{
				// Start sequence
				result = method.Invoke(scriptClass,null) as string;
			}
		}
		return result;
	}
	#endregion
}

}
