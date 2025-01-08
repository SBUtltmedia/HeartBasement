using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PowerTools;

namespace PowerTools.Quest
{

//
// Character Data and functions. Persistant between scenes, as opposed to CharacterComponent which lives on a GameObject in a scene.
//
[System.Serializable]
public partial class Character : IQuestClickable, ICharacter, IQuestScriptable, IQuestSaveCachable
{	
	#region Constants/Definitions

	// Corresponds to eFace enum: Left, Right, Down, Up, DownLeft, DownRight, UpLeft, UpRight
	public static readonly Vector2[] FACE_DIRECTIONS = { Vector2.left, Vector2.right, Vector2.down, Vector2.up, new Vector2(-1,-1).normalized, new Vector2(1,-1).normalized, new Vector2(-1,1).normalized, Vector2.one.normalized }; 

	[System.Serializable]
	public partial class CollectedItem
	{
		public string m_name = string.Empty; // String rather than reference to make saving/loading easy
		public float m_quantity = 1; // Why a float? Well, maybe you have half a cup of water, I don't know!
	}
	[System.Serializable]
	public class FaceCharacterData
	{		
		public string m_character = string.Empty;
		public float m_minTime = 0;
		public float m_maxTime = 0;
		public float m_timer = 0;
	}

	// The animation state of the character. Note they could be talking, walking and animating at the same time. But only playing 1 animation, indicated by this state
	public enum eState
	{
		Idle,
		Walk,
		Talk,
		Animate,
		None		
	}

	/*/ TODO: change facing angles to be customisable, like this:
	float m_maxAngleUR = 60.0f; // Higher than this is "Up"
	float m_maxAngleR = 30.0f; // Higher than this is "UpRight"
	float m_maxAngleDR = -30.0f; // Hihger than this is "DownRight", Lower than this is "Down"
	float m_maxAngleD = -60.0f; // Hihger than this is "DownRight", Lower than this is "Down"
	/*/// TODO: also give option for "AGS" style facing. eg. if no diagonal, use closest 45 degree.

	#endregion
	#region Vars: Inspector
		
	//
	// Default values set in inspector
	//
	[Header("Mouse-over Defaults")]
	[TextArea(1,10)]
	[SerializeField] string m_description = "New Character";
	[Tooltip("If set, changes the name of the cursor when moused over")]
	[SerializeField] string m_cursor = null;

	[Header("Starting Room, Position, etc")]
	[SerializeField] string m_room = null;
	[SerializeField] Vector2 m_position = Vector2.zero;
	[SerializeField] eFace m_faceDirection = eFace.Down;
	[Tooltip("Whether character is clickable/can be interacted with")]
	[SerializeField] bool m_clickable = true; 
	[Tooltip("Whether character sprites are visible")]
	[SerializeField] bool m_visible = true;
	[SerializeField] List<CollectedItem> m_inventory = new List<CollectedItem>();

	[Header("Movement Defaults")]
	[SerializeField] Vector2 m_walkSpeed = new Vector2(50,50);
	Vector2 m_defaultWalkSpeed = -Vector2.one; // Default walkspeed starts at zero, 
	[SerializeField] bool m_moveable = true;    // Whether character can walk
	[Tooltip("If true, this character will walk around other characters marked as solid (Using their Solid Size)")]
	[SerializeField] bool m_solid = false;
	[Tooltip("Width & height of rectangle for other characters to pathfind around, centered on character pivot")]
	[SerializeField] Vector2 m_solidSize = new Vector2(20,4);
	[SerializeField] bool m_turnBeforeWalking = true;
	[SerializeField] bool m_turnBeforeFacing = true;
	[Tooltip("How fast character turns (Frames per second)")]
	[SerializeField] float m_turnSpeedFPS = 12;
	[SerializeField] bool m_adjustSpeedWithScaling = true;

	[Header("Visuals Setup")]
	[SerializeField] Color m_textColour = Color.white;
	[SerializeField] string m_animIdle = "Idle";
	[SerializeField] string m_animWalk = "Walk";
	[SerializeField] string m_animTalk = "Talk";
	[SerializeField] string m_animMouth = string.Empty;
	[SerializeField] string m_animShadow = "";
	[SerializeField] bool m_useRegionTinting = true;
	[SerializeField] bool m_useRegionScaling = true;	
	[SerializeField, Tooltip("Dialog text offset from the top of the sprite, added to the global one set in PowerQuest settings")] Vector2 m_textOffset = Vector2.zero;
	[SerializeField, Tooltip("To use, talk anims should be frames ABCDEFX in that order from https://github.com/DanielSWolf/rhubarb-lip-sync. Rhubarb must be downloaded to Project/Rhubarb/Rhubarb.exe")] 
	bool m_LipSyncEnabled = false;
	[SerializeField] bool m_antiGlide = false;

	[Header("Audio")]
	[Tooltip("Add Footstep event to animation to trigger the footstep sound")]
	[SerializeField,QuestAudioCueName] string m_footstepSound = string.Empty;

	[Header("Other Settings")]
	[Tooltip("Whether clickable collider shape is taken from the sprite")]
	[SerializeField] bool m_useSpriteAsHotspot = false;
	[SerializeField] float m_baseline = 0;
	[SerializeField] Vector2 m_walkToPoint = Vector2.zero;
	[SerializeField] Vector2 m_lookAtPoint = Vector2.zero;

	// Todo: Maybe... allow easy set up to walk to "either side" of things. Alternates include list of "walkto"s, and/or option to "WalkToEvenIfCloser"
	//[SerializeField] bool m_walkToSymmetrical = true;

	[ReadOnly][SerializeField] string m_scriptName = "New";
	[ReadOnly][SerializeField] string m_scriptClass = "CharacterNew";

	#endregion
	#region Vars: Private

	//
	// private variables
	//

	// Script and instance
	QuestScript m_script = null;
	GameObject m_prefab = null;
	CharacterComponent m_instance = null;

	// Inventory- currently selected item
	string m_activeInventory = null;
	// Inventory- Every item ever owned (for GetEverHadInventory()). Was using hashSet but that can't be serialised unfortunately
	List<string> m_inventoryAllTime = new List<string>();

	eFace m_targetFaceDirection = eFace.Right; // Used for turn-to-face
	eFace m_facingVerticalFallback = eFace.Right; // Should only be Left or Right- Used if there's no up/down frames to choose whether R or L anim is used
	
	eFace m_faceAfterWalk = eFace.None; // used for face after walking in background
	
	QuestText m_dialogText = null;

	AudioHandle m_dialogAudioSource = null;

	string m_lastRoom = null;	
	
	Vector2 m_textPositionOverride = Vector2.zero;
	int m_clickableColliderId = 0;

	IEnumerator m_coroutineSay = null;

	int m_useCount = 0;
	int m_lookCount = 0;

	// Used when character is enabled/disabled.
	bool m_enabled = true;

	// When character is enabled/disabled, these are used to remember what old visible/clickable/etc were.
	//bool m_visibleWhenEnabled = true; 
	//bool m_clickableWhenEnabled = true;
	//bool m_solidWhenEnabled = false;

	string m_animPrefix = null;    // When playing an animation, try to find one with this prefix first
	string m_animOverride = null;  // Used when Animation property is set	
	bool m_pauseAnimAtEnd = false; // If true, animation (from PlayAnimation()) will pause on last frame and not return to Idle (Until StopAnimation is called).
	float m_animationTime = -1;    // Normalised time of current animation, cached here for save/loading from/to animations
	float m_loopStartTime = -1;    // whether a loop tag was hit, and at what time
	float m_loopEndTime = -1;      // whether a loop end tag was hit, and at what time
	
	List<Vector2> m_waypoints = new List<Vector2>();

	// Variables for facing characters
	FaceCharacterData m_faceChar = null;
	
	bool m_shadowOn = true;
	
	// Hack for data inside the character that we explicitly don't want to save
	[QuestSave] // this actually means the class is saved but, any fields inside without [QuestSave] will be ommitted
	class NonSavedData
	{
		public Room m_roomCached = null;
	}
	NonSavedData m_nonSavedData = new NonSavedData();
	
	#endregion 
	#region Partial functions for extending class

	partial void ExOnSpawnInstance();
	partial void ExOnActiveInventorySet(IInventory newInventory);

	#endregion 
	#region Properties

	//
	//  Properties
	//
	public eQuestClickableType ClickableType { get {return eQuestClickableType.Character; } }
	public string Description { get{ return m_description;} set{ m_description=value;} }
	public string ScriptName { get{ return m_scriptName;} }
	public MonoBehaviour Instance { get{ return m_instance; } }
	public Character Data { get{ return this; } }
	public IQuestClickable IClickable { get{ return this; } }


	public IRoom Room 
	{ 		
		get 
		{
			// Ugly caching for efficiency
			if ( string.IsNullOrEmpty(m_room) )
				m_nonSavedData.m_roomCached = null;				
			else if ( m_nonSavedData.m_roomCached == null || m_room != m_nonSavedData.m_roomCached.ScriptName )
				m_nonSavedData.m_roomCached = PowerQuest.Get.GetRoom(m_room);

			return m_nonSavedData.m_roomCached; 
		} 
		set
		{
			string oldRoom = m_room;
			m_room = value?.ScriptName ?? null; 
			
			string currRoom = PowerQuest.Get.GetCurrentRoom().ScriptName;

			if ( oldRoom == m_room && (IsPlayer== false || m_room == currRoom))
				return;

			if ( PowerQuest.Get.GetRestoringGame() == false ) // Don't set the m_lastRoom when restoring, it'll already have been restored from the save file
				m_lastRoom = oldRoom;

			// Add/remove instance of character if it's the current room that effected. 
			if ( currRoom == oldRoom )
			{
				// Handle the player changing rooms (should trigger scene change)
				if ( IsPlayer )
				{		
					StopFacingCharacter(); // Stop facing character (it's easy to forget to reset this, causes wierd bugs)
					PowerQuest.Get.StartRoomTransition(value.Data);
				}
				// Character has left the current room, remove them from the scene
				else if ( Instance != null )
				{
					Instance.gameObject.name = "deleted"; // so it doesn't turn up in searches in same frame
					GameObject.Destroy(Instance.gameObject);
				}
			}	
			else if ( currRoom == m_room )
			{
				// Character has entered the current room, add them to the scene
				SpawnInstance();
			}	
			else if ( currRoom != m_room && IsPlayer && PowerQuest.Get.GetRestoringGame() == false )
			{
				// Player must have changed, so move to their room.
				PowerQuest.Get.StartRoomTransition(value.Data);
			}
		}	
	}

	public void ChangeRoomBG(IRoom room) { Room = room; }
	public Coroutine ChangeRoom(IRoom room) { return PowerQuest.Get.StartCoroutine(CoroutineChangeRoom(room)); }
	IEnumerator CoroutineChangeRoom(IRoom room)
	{
		if ( PowerQuest.Get.GetPlayer() == this )
		{
			yield return PowerQuest.Get.ChangeRoom(room);
		}
		else 
		{
			Room = room; 
		}		
	}

	// Returns the last room visited before the current one.
	public IRoom LastRoom => PowerQuest.Get.GetRoom(m_lastRoom);

	// Debugging function to set the last room, useful in PlayFrom functions in particular
	public void DebugSetLastRoom(IRoom room)
	{
		(room as Room).DebugSetVisited(1);
		if ( room == null ) 
			m_lastRoom = null; 
		else 
			m_lastRoom = room.ScriptName; 
	}

	public Vector2 Position{ get{return m_position;} set {SetPosition(value);} }
	public Vector2 TargetPosition { get {
		if ( m_instance != null )
			return m_instance.GetTargetPosition();
		return m_position;
	} }
	public List<Vector2> Waypoints { get { return m_waypoints; } }

	public float Baseline { get{return m_baseline;} set{m_baseline = value;} }	
	public void SetBaselineInFrontOf(IQuestClickableInterface clickable) 
	{ 		
		Baseline = (clickable.IClickable.Baseline+clickable.IClickable.Position.y-1) - Position.y;
	}

	public Vector2 WalkSpeed 
	{
		get{ return m_walkSpeed; } 
		set 
		{  
			if ( m_defaultWalkSpeed.x < 0 )
				m_defaultWalkSpeed = m_walkSpeed;
			m_walkSpeed = value; 
		} 
	}
	public void ResetWalkSpeed() { if ( m_defaultWalkSpeed.x > 0 )  WalkSpeed = m_defaultWalkSpeed; }
	public bool TurnBeforeWalking { get{ return m_turnBeforeWalking; } set { m_turnBeforeWalking = value; } }
	public bool TurnBeforeFacing { get{ return m_turnBeforeFacing; } set { m_turnBeforeFacing = value; } }
	public float TurnSpeedFPS { get{ return m_turnSpeedFPS; } set { m_turnSpeedFPS = value; } }

	public bool AdjustSpeedWithScaling { get { return m_adjustSpeedWithScaling; } set { m_adjustSpeedWithScaling = value; } }

	public eFace GetFaceAfterWalk() { return m_faceAfterWalk; }

	public eFace Facing 
	{ 
		get { return m_faceDirection;}
		set 
		{ 			
			m_faceAfterWalk = eFace.None;
			m_faceDirection = value; 
			m_targetFaceDirection = m_faceDirection; // Set target too to snap to target
			if (m_faceDirection != eFace.Up && m_faceDirection != eFace.Down && m_faceDirection != eFace.None )
				m_facingVerticalFallback = CharacterComponent.ToCardinal(m_faceDirection);
			if ( m_instance != null ) 
				m_instance.UpdateFacingVisuals(value); 
		} 
	}
	

	public bool Enabled 
	{
		get 
		{ 
			return m_enabled 
				&& (IsPlayer == false || PowerQuest.Get.GetCurrentRoom().PlayerVisible); // Also check if player is off in this room.
		}
		set
		{
			if ( m_enabled == value )
				return;
			m_enabled = value;
			if ( m_instance != null ) 
				m_instance.UpdateEnabled();
		}
	}

	public bool Clickable 
	{ 
		get	{ return m_clickable && Enabled; } 
		set
		{		
			// Disabled this warning, which was really only for when upgrading old PQ from ages ago
			//if ( value && m_enabled == false )
			//	Debug.LogWarning("Character Clickable set when Character is not Enabled. Did you mean to call Show() or Enable() first?");
			//m_clickableWhenEnabled = value; 
			m_clickable = value;
		} 
	}

	public bool Visible 
	{ 
		get	{ return m_visible && Enabled; } 
		set
		{
			//if ( value && m_enabled == false )
			//	Debug.LogWarning("Character Visible set when Character is not Enabled. Did you mean to call Show() or Enable() first?");
			bool changed = value != m_visible;
			//m_visibleWhenEnabled = value;
			m_visible = value;
			if ( m_instance != null && changed )
				m_instance.UpdateVisibility();
		} 
	}
	
	// Whether the player is *Actually* visible in the room. (ie: their visible flag is true, and they're also in the room, and enabled)
	public bool VisibleInRoom {  get { return Visible && Room != null && Room.Current; } }

	public bool Solid
	{
		get { return m_solid && Enabled; }
		set 
		{
			if ( m_solid != value )
			{
				m_solid = value;
				//m_solidWhenEnabled = value;
				if ( m_instance != null )
					m_instance.UpdateSolid();
			}
		}
	}
	public Vector2 SolidSize
	{
		get { return m_solidSize; }
		set 
		{
			if ( (m_solidSize-value).sqrMagnitude > float.Epsilon )
			{
				m_solidSize = value;
				if ( m_instance != null )
					m_instance.UpdateSolidSize();
			}
		}
	}

	public bool UseSpriteAsHotspot
	{
		get { return m_useSpriteAsHotspot; }
		set
		{
			if ( value == m_useSpriteAsHotspot )
				return;
			m_useSpriteAsHotspot = value;
			if ( m_instance != null )
				m_instance.UpdateUseSpriteAsHotspot();
		}
	}
		
	/// Shows the character again after a call to Hide(), moving them to current room, and forcing Visible to true.
	/// This maintains its old Clickable and Solid properties (Unlike Show/Hide functions). 
	/// You can optionally pass in a position or face direction. If not passed no change will be made.
	/// The Enable() function is similar, but doesn't set Visible to true, or move them to the current room.
	/// \sa Hide() \sa Disable() \sa Enable()
	public void Show( float posX, float posy, eFace facing = eFace.None ) { Show(new Vector2(posX,posy),facing); }
	public void Show( eFace facing ) { Show(Vector2.zero,facing); }	
	public void Show(IQuestClickableInterface atClickableWalkToPos, eFace face = eFace.None) { Show(atClickableWalkToPos.IClickable.Position + atClickableWalkToPos.IClickable.WalkToPoint, face); }
	public void Show( Vector2 pos = new Vector2(), eFace facing = eFace.None )
	{
		/*
		bool wasVisible = m_visible;
		bool wasClickable = m_clickable;
		
		if ( m_solidWhenEnabled )
			Solid = true;
		if (m_visibleWhenEnabled)
			Visible = true;
		if ( m_clickableWhenEnabled)
			Clickable = true;
		*/
		Enabled = true;
		Visible = true;

		if ( pos != Vector2.zero )
			Position = pos;
		if ( facing != eFace.None )
			Facing = facing;

		// Change character to be in current room
		Room = PowerQuest.Get.GetCurrentRoom();
	}

	/// Obsolete: Set's visible & clickable, and changes them to the current room (if they weren't there already)
	public void Show( bool clickable ) { Enable( clickable ); }
	/// Obsolete: Set's visible & clickable, and changes them to the current room (if they weren't there already)
	public void Enable( bool clickable )
	{
		Enabled = true;
		Visible = true;
		Clickable = clickable;
		/*
		Visible = true;
		Clickable = clickable;
		if ( m_solidWhenEnabled )
			Solid = true;
		*/
		// Change character to be in current room
		Room = PowerQuest.Get.GetCurrentRoom();
	}

	/// Disables the character (sets them non-visible, clickable, solid, movable).  (same as `Disable()`)
	public void Hide() { Disable(); }

	public void Enable()
	{
		Enabled = true;
	}

	/// Set's invisible, non-clickable, and non-solid
	public void Disable() 
	{
		Enabled = false;		

		/* Testing other bits
		if ( m_visible )
		{
		Visible = false; 
			m_visibleWhenEnabled = true;
		}
		if ( m_clickable )
		{
		Clickable = false; 
			m_clickableWhenEnabled = true;
		}
		if ( m_solid )
		{
			Solid = false; 
			m_solidWhenEnabled = true;
		}
		*/
	}

	
	public bool Moveable { get{return m_moveable && Enabled;} set{m_moveable = value;} }
	public bool Walking { get{ return m_instance == null ? false : m_instance.Walking; } }
	public bool Talking { get{ return (m_dialogText != null && m_dialogText.gameObject.activeSelf) || (m_dialogAudioSource != null && m_dialogAudioSource.isPlaying); /*//Can't just check if component talking, since can be off-screen and still talking- return m_instance == null ? false : m_instance.GetIsTalking();*/ } }
	// Returns true if currently playing an anim (using PlayAnimation, or Animation = xxx)
	public bool Animating { get{ return m_instance == null ? false : m_instance.Animating; } }
	public bool IsPlayer { get{ return PowerQuest.Get.GetPlayer() == this; } }
	
	public Color TextColour 
	{ 
		get { return m_textColour;} 
		set 
		{
			m_textColour = value; 
			if (m_dialogText != null )
			{
				m_dialogText.GetComponent<TextMesh>().color = m_textColour;
			}
		}
	}

	public string AnimIdle
	{ 
		get{return m_animIdle;}
		set
		{
			bool changed = m_animIdle != value;
			m_animIdle = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.Idle );
		}
	}
	public string AnimWalk
	{ 
		get{return m_animWalk;}
		set
		{
			bool changed = m_animWalk != value;
			m_animWalk = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.Walk );
		}
	}
	public string AnimTalk
	{ 
		get{return m_animTalk;}
		set
		{
			bool changed = m_animTalk != value;
			m_animTalk = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.Talk );
		}
	}
	public string AnimMouth
	{ 
		get{return m_animMouth;}
		set
		{
			bool changed = m_animMouth != value;
			m_animMouth = value;
			if ( m_instance != null && changed ) m_instance.UpdateMouthAnim();
		}
	}
	public string AnimPrefix
	{
		get { return m_animPrefix; }
		set
		{
			bool changed = m_animPrefix != value;
			m_animPrefix = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.None );

		}
	}

	public bool LipSyncEnabled
	{
		get{ return m_LipSyncEnabled; }
		set { m_LipSyncEnabled = value; }
	}

	public bool AntiGlide
	{
		get{ return m_antiGlide; }
		set { m_antiGlide = value; }
	}

	public string FootstepSound
	{
		get{ return m_footstepSound; }
		set { m_footstepSound = value; }
	}

	
	// The animation state of the character. Note they could be talking, walking and animating at the same time. But only playing 1 animation, indicated by this state
	public eState State { get { return ( m_instance != null ) ? (m_instance.GetState()) : eState.None; } }

	// The currently selected inventory item
	public IInventory ActiveInventory 
	{ 
		get { return PowerQuest.Get.GetInventory(m_activeInventory); } 
		set	
		{
			m_activeInventory = value == null ? null : value.ScriptName; 
			ExOnActiveInventorySet(value);
		} 
	}
	public bool HasActiveInventory { get { return string.IsNullOrEmpty(m_activeInventory) == false; } }

	public string ActiveInventoryName { get { return m_activeInventory; } set { m_activeInventory = value; } }
	public bool HasActiveInventoryName { get { return string.IsNullOrEmpty(m_activeInventory); } }

	public string Cursor { get { return m_cursor; } set { m_cursor = value; } }

	public bool UseRegionTinting { get { return m_useRegionTinting; } set { m_useRegionTinting = value; } }
	public bool UseRegionScaling { get { return m_useRegionScaling; } set { m_useRegionScaling = value; } }

	public bool FirstUse { get { return UseCount == 0; } } 
	public bool FirstLook { get { return LookCount == 0; } }
	public int UseCount { get { return m_useCount - (PowerQuest.Get.GetInteractionInProgress(this,eQuestVerb.Use) ? 1 : 0); } }
	public int LookCount { get { return m_lookCount - (PowerQuest.Get.GetInteractionInProgress(this,eQuestVerb.Look) ? 1 : 0); } }

	public Vector2 WalkToPoint { get{ return m_walkToPoint;} set{m_walkToPoint = value;} }
	public Vector2 LookAtPoint { get{ return m_lookAtPoint;} set{m_lookAtPoint = value;} }
	//public bool WalkToSymmetrical { get{ return m_walkToSymmetrical ;} set{m_walkToSymmetrical = value;} }
		
	public Vector2 TextPositionOffset { get { return m_textOffset; } set { m_textOffset = value; } }
	public Vector2 TextPositionOverride { get{ return m_textPositionOverride; } set { m_textPositionOverride = value; } }
	
	public void SetTextPosition(Vector2 worldPosition) { TextPositionOverride = worldPosition; }
	public void SetTextPosition(float worldPosX, float worldPosY)  { TextPositionOverride = new Vector2(worldPosX,worldPosY); }
	public void LockTextPosition()
	{
		if ( m_instance != null )
		{
			ResetTextPosition();
			TextPositionOverride = m_instance.GetTextPosition();
		}
		else
		{
			TextPositionOverride = Position;
		}
	}
	public void ResetTextPosition() { TextPositionOverride = Vector2.zero; }
	
	public void StartFacingCharacter(ICharacter character, float minWaitTime = 0.2f, float maxWaitTime = 0.4f) 
	{ 
		m_faceChar = new FaceCharacterData() { m_character = character.ScriptName, m_minTime = minWaitTime, m_maxTime = maxWaitTime, m_timer = Random.Range(minWaitTime,maxWaitTime) };
	}
	public void StopFacingCharacter() { m_faceChar = null; }


	// Callback when starting to say something. CallbackSay(string dialog, int id)
	public System.Action<string,int> CallbackOnSay = null;
	// Callback when finished saying something
	public System.Action CallbackOnEndSay = null;

	#endregion
	#region Funcs: Getter/Setter

	//
	// Getters/Setters - These are used by the engine. Scripts mainly use the properties
	//
	public QuestScript GetScript() { return m_script; }
	public IQuestScriptable GetScriptable() { return this; }
	public T GetScript<T>() where T : CharacterScript<T> {  return ( m_script != null ) ? m_script as T : null; }
	public string GetScriptName(){ return m_scriptName; }
	public string GetScriptClassName() { return m_scriptClass; }
	public void HotLoadScript(Assembly assembly) { QuestUtils.HotSwapScript( ref m_script, m_scriptClass, assembly ); }

	public GameObject GetPrefab() { return m_prefab; }

	// Spawns instance of ths character in the current room. Should only be used by PowerQuest or internally
	public GameObject SpawnInstance()	
	{
		GameObject characterInstance = GameObject.Find(GetPrefab().name);
		if ( characterInstance == null )
		{
			characterInstance = GameObject.Instantiate( GetPrefab() ) as GameObject;
		}
		SetInstance(characterInstance.GetComponent<CharacterComponent>());
		SetPosition(GetPosition());
		Facing = GetFaceDirection();
		ExOnSpawnInstance();
		return characterInstance;
	}
	
	public GameObject GetInstance() { return m_instance != null ? m_instance.gameObject : null; }
	public void SetInstance(CharacterComponent instance)
	{ 
		m_instance = instance; 
		m_instance.SetData(this);
		m_instance.name = m_prefab.name;
	}	
	public void SetPosition(float x, float y, eFace face = eFace.None) { SetPosition(new Vector2(x,y),face); }
	public void SetPosition(IQuestClickableInterface clickable, eFace face = eFace.None) { SetPosition(clickable.IClickable.Position + clickable.IClickable.WalkToPoint, face); }
	public void SetPosition(Vector2 position, eFace face = eFace.None) 
	{ 
		m_position = position; 
		if ( m_instance != null )
		{
			m_instance.transform.position = Utils.SnapRound(m_position,PowerQuest.Get.SnapAmount);
		}
		if ( face != eFace.None )
			Facing = face;
	}
	public Vector2 GetPosition() { return m_position; }

	// Facing get/setters for component
	public eFace GetFacingVerticalFallback() { return m_facingVerticalFallback; }
	public void SetFacingVerticalFallback(eFace value) { m_facingVerticalFallback = value; }
	public eFace GetTargetFaceDirection() { return m_targetFaceDirection; }
	public eFace GetFaceDirection() { return m_faceDirection; }
	public void SetFaceDirection(eFace direction) { m_faceDirection = direction; } // used by CharacterComponent, doesnt' set target, just actual facing

	public List<CollectedItem> GetInventory() {return m_inventory;}

	public float GetInventoryItemCount()
	{
		return m_inventory.Count;
	}

	// REturns quantity of specific item
	public float GetInventoryQuantity(string itemName)
	{ 
		float result = 0;
		foreach ( CollectedItem inv in m_inventory )
		{
			if ( inv.m_name == itemName )
			{
				result += inv.m_quantity;
			}
		}
		return result;
	}

	public bool HasInventory(string itemName)
	{
		return m_inventory.Exists( inv=>inv.m_name == itemName );
	}
	public bool GetEverHadInventory(string itemName)
	{
		return m_inventoryAllTime.Contains(itemName);
	}

	public void AddInventory(string itemName, float quantity = 1)
	{
		Inventory invItem = PowerQuest.Get.GetInventory(itemName);
		if ( invItem == null ) 
			return;

		if ( invItem.Stack )
		{
			// Find existing and increment quantity
			CollectedItem collectedItem =  m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			if ( collectedItem == null )
			{
				m_inventory.Add(new CollectedItem() {m_name = itemName, m_quantity = quantity});
				if ( m_inventoryAllTime.Contains(itemName) == false )
					m_inventoryAllTime.Add(itemName);
			}
			else 
			{
				collectedItem.m_quantity += quantity;
			}
		}
		else 
		{
			// Add 'quantity' number of the item
			for ( int i = 0; i < quantity; ++i )
			{
				m_inventory.Add(new CollectedItem() {m_name = itemName, m_quantity = quantity});
			}
			if ( m_inventoryAllTime.Contains(itemName) == false )
				m_inventoryAllTime.Add(itemName);
		}

		invItem.OnCollected();
		if ( PowerQuest.Get.CallbackOnInventoryCollected != null )
			PowerQuest.Get.CallbackOnInventoryCollected.Invoke(Data,invItem);

	}
	
	public void RemoveInventory( string itemName, float quantity = 1 )
	{
		Inventory invItem = PowerQuest.Get.GetInventory(itemName);
		if ( invItem == null ) 
			return;
		if ( invItem.Stack )
		{
			// Find existing and decrement quantity, and remove if none left
			CollectedItem collectedItem =  m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			if ( collectedItem != null )
			{
				collectedItem.m_quantity -= quantity;
				if ( collectedItem.m_quantity <= 0 )
				{
					m_inventory.Remove(collectedItem);
				}
			}
		}
		else 
		{
			// remove up to "quantity" number of the item
			CollectedItem collectedItem =  m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			for (int i = 0; i < quantity && collectedItem != null; ++i)
			{
				m_inventory.Remove(collectedItem);
				collectedItem = m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			}
		}

		// if the currently selected item was Removed, remove it
		if ( itemName == m_activeInventory && HasInventory(itemName) == false )
		{
			ActiveInventory = null;
		}

		
	}
	public void ClearInventory()
	{
		m_inventory.Clear();
		ActiveInventory = null;
	}

	public float GetInventoryQuantity(IInventory item) { return GetInventoryQuantity(item?.ScriptName); }
	public bool HasInventory(IInventory item) { return HasInventory(item?.ScriptName); }
	public bool GetEverHadInventory(IInventory item) { return GetEverHadInventory(item?.ScriptName); }
	public void AddInventory(IInventory item, float quantity = 1) { AddInventory( item?.ScriptName,quantity ); }
	public void RemoveInventory( IInventory item, float quantity = 1 ) { RemoveInventory( item?.ScriptName,quantity ); }
	
	/// Replaces an inventory item with another (keeping the same slot position)
	public void ReplaceInventory(IInventory oldItem, IInventory newItem)
	{	
		// Add new item to the end
		AddInventory(newItem);
		
		// Find the index of the one we're replacing
		int oldIndex = GetInventory().FindIndex(item=>item.m_name==oldItem.ScriptName);
		if ( oldIndex >= 0 )
		{
			// If found, swap positions, and remove the old inventory item
			GetInventory().Swap(oldIndex,GetInventory().Count-1);
			RemoveInventory(oldItem);
		}
	}

	public AudioSource GetDialogAudioSource() { return m_dialogAudioSource; }
		
	// Properties for CharacterComponent
	public bool PauseAnimAtEnd { get => m_pauseAnimAtEnd; set => m_pauseAnimAtEnd=value; }	
	public float AnimationTime { get => m_animationTime; set => m_animationTime=value; }	
	public float LoopStartTime { get=>m_loopStartTime; set=>m_loopStartTime=value; }
	public float LoopEndTime { get=>m_loopEndTime; set=>m_loopEndTime=value; }

	#endregion
	#region Funcs: Init

	//
	// Initialisation
	//

	public void EditorInitialise( string name )
	{
		// creates defaults
		m_description = name;
		m_scriptName = name;
		m_scriptClass = PowerQuest.STR_CHARACTER+name;	
	}
	public void EditorRename(string name)
	{
		m_scriptName = name;
		m_scriptClass = PowerQuest.STR_CHARACTER+name;	
	}
	public string EditorGetRoom() { return m_room; }
	public void EditorSetRoom(string roomName) { m_room = roomName; }
	public bool EditorGetSolid() { return m_solid; } // Simple flag accessor

	public void OnPostRestore( int version, GameObject prefab )
	{
		m_prefab = prefab;
		if ( m_script == null ) // script could be null if it didn't exist in old save game, but does now.
			m_script = QuestUtils.ConstructByName<QuestScript>(m_scriptClass);

		m_nonSavedData.m_roomCached = null;	// Cached room will poitn to wrong place after restoring, so null it
		SaveDirty=false;


		/*	NB: The below doesn't work because instance won't have loaded yet.
		
		// Set the clickable collider to enable the correct collision after restoring
		if ( m_instance != null ) m_instance.OnClickableColliderIdChanged();
		
		// Start any background animation back up.
		Animation = m_animOverride;
		if ( m_instance && m_animationTime > 0 )
			m_instance.GetSpriteAnimator().NormalizedTime = m_animationTime;		
		*/
	}

	public void Initialise( GameObject prefab )
	{
		m_prefab = prefab;
		// Construct the script
		m_script = QuestUtils.ConstructByName<QuestScript>(m_scriptClass);

		// Deep copy inventory	
		m_inventory = QuestUtils.CopyListFields(m_inventory);  // The points will have been shallow copied already, but we want a deep copy.

		// Add starting inventory to m_inventoryAllTime
		m_inventoryAllTime.Clear();
		foreach ( CollectedItem item in m_inventory )
		{
			if ( m_inventoryAllTime.Contains(item.m_name) == false )
				m_inventoryAllTime.Add(item.m_name);
		}

		// Hack to reset non-saved data stuff. We don't want to use the cached one from the prefab.
		m_nonSavedData = new NonSavedData();
	}

	//
	// Implementing IQuestSaveCachable
	//	
	bool m_saveDirty = true;
	bool m_saveDirtyEver = false;
	public bool SaveDirty { get=>m_saveDirty; set { m_saveDirty=value; 
		if (value) 
		{
			//if ( m_saveDirtyEver == false ) Debug.Log($"Char {ScriptName} set dirty");
			m_saveDirtyEver=true; 
		} } }
	public bool SaveDirtyEver => m_saveDirtyEver;

	#endregion
	#region Funcs: Public

	//
	// Public Functions
	//

	public void OnInteraction( eQuestVerb verb )
	{		
		if ( verb == eQuestVerb.Look ) ++m_lookCount;
		else if ( verb == eQuestVerb.Use) ++m_useCount;
	}
	public void OnCancelInteraction( eQuestVerb verb )
	{		
		if ( verb == eQuestVerb.Look ) --m_lookCount;
		else if ( verb == eQuestVerb.Use) --m_useCount;
	}
	public void WalkToBG( float x, float y, bool anywhere = false, eFace thenFace = eFace.None )
	{
		WalkToBG(new Vector2(x,y), anywhere,thenFace);
	}

	public void WalkToBG( Vector2 pos, bool anywhere = false, eFace thenFace = eFace.None )
	{
		m_faceAfterWalk = thenFace;
		m_waypoints.Clear();
		//Debug.Log(GetDescription() + " Walking to "+pos.ToString());	
			
		// This stops the sequence being cancellable,  since 'Walking' property is used to check if it's possible to cancel, and BG walking shouldn't be cancelable
		if ( IsPlayer )
			PowerQuest.Get.DisableCancel();

		if ( Moveable == false )
			return;
		if ( m_instance != null && PowerQuest.Get.GetSkippingCutscene() == false )
		{
			m_instance.WalkTo(pos, anywhere,true);
			if ( Walking == false && m_faceAfterWalk != eFace.None ) // Face after walk manually (if 'walkTo' didn't start walking, maybe already reached destination)
				FaceBG(thenFace);
		}
		else 
		{			
			SetPosition(pos);
			if ( m_faceAfterWalk != eFace.None ) // Face after walk manually
				Facing = m_faceAfterWalk;
			StopWalking();
		}	

	}
	public void WalkToBG(IQuestClickableInterface clickable, bool anywhere = false, eFace thenFace = eFace.None)
	{
		m_faceAfterWalk = thenFace;
		m_waypoints.Clear();
		if ( clickable != null )
		{			
			if ( clickable.IClickable.Instance != null )
				WalkToBG((Vector2)clickable.IClickable.Instance.transform.position + clickable.IClickable.WalkToPoint, anywhere, thenFace);
			else
				WalkToBG(clickable.IClickable.WalkToPoint, anywhere, thenFace);
		}
	}
	
	public void WalkToBG( Vector2 pos, eFace thenFace ) { WalkToBG(pos,false,thenFace); }
	public void WalkToBG( float x, float y, eFace thenFace ) { WalkToBG(x,y,false,thenFace); }
	public void WalkToBG(IQuestClickableInterface clickable,eFace thenFace) { WalkToBG(clickable,false,thenFace); }
	
	public Coroutine WalkTo(float x, float y, bool anywhere = false) {	return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(new Vector2(x,y), anywhere)); }
	public Coroutine WalkTo(Vector2 pos, bool anywhere = false) {	return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(pos, anywhere)); }
	public Coroutine WalkTo(IQuestClickableInterface clickable, bool anywhere = false) 
	{
		m_faceAfterWalk = eFace.None;
		m_waypoints.Clear();
		if ( clickable != null )
		{
			if ( clickable.IClickable.Instance !=  null )
				return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(((Vector2)clickable.IClickable.Instance.transform.position + clickable.IClickable.WalkToPoint), anywhere));			
			else
				return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(clickable.IClickable.WalkToPoint, anywhere));
		}
		return null;
	}
	public Coroutine WalkToClicked(bool anywhere = false) {	return PowerQuest.Get.StartCoroutine(CoroutineWalkTo(PowerQuest.Get.GetLastWalkTo(), anywhere)); }

	public Coroutine MoveTo(float x, float y, bool anywhere = false )  { return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(new Vector2(x,y), anywhere,false)); }
	public Coroutine MoveTo(Vector2 pos, bool anywhere = false )  { return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(pos, anywhere,false)); }
	public Coroutine MoveTo(IQuestClickableInterface clickable, bool anywhere = false) 
	{
		m_faceAfterWalk = eFace.None;
		m_waypoints.Clear();
		if ( clickable != null )
		{
			if ( clickable.IClickable.Instance !=  null )
				return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(((Vector2)clickable.IClickable.Instance.transform.position + clickable.IClickable.WalkToPoint), anywhere,false));			
			else
				return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(clickable.IClickable.WalkToPoint, anywhere,false));
		}
		return null;
	}
	public void MoveToBG(float x, float y, bool anywhere = false ) { MoveToBG(new Vector2(x,y), anywhere); }
	public void MoveToBG(Vector2 pos, bool anywhere = false )
	{		
		m_faceAfterWalk = eFace.None;			
		m_waypoints.Clear();
		// This stops the sequence being cancellable,  since 'Walking' property is used to check if it's possible to cancel, and BG walking shouldn't be cancelable
		if ( IsPlayer )
			PowerQuest.Get.DisableCancel();

		if ( Moveable == false )
			return;
		if ( m_instance != null && PowerQuest.Get.GetSkippingCutscene() == false )
		{
			m_instance.WalkTo(pos, anywhere,false);
		}
		else 
		{
			SetPosition(pos);
		}	
	}	
	public void MoveToBG(IQuestClickableInterface clickable, bool anywhere = false)
	{
		m_faceAfterWalk = eFace.None;
		m_waypoints.Clear();
		if ( clickable != null )
		{			
			if ( clickable.IClickable.Instance != null )
				MoveToBG((Vector2)clickable.IClickable.Instance.transform.position + clickable.IClickable.WalkToPoint, anywhere);
			else
				MoveToBG(clickable.IClickable.WalkToPoint, anywhere);
		}
	}

	// Stops walking on the spot
	public void StopWalking() 
	{ 
		m_faceAfterWalk = eFace.None; 
		m_waypoints.Clear();
		if ( m_instance!= null ) 
			m_instance.StopWalk();
	}	
	
	public void AddWaypoint(float x, float y, eFace thenFace = eFace.None ) { AddWaypoint(new Vector2(x,y),thenFace); }
	public void AddWaypoint(Vector2 pos, eFace thenFace = eFace.None)
	{	
		m_faceAfterWalk = thenFace;
		//Debug.Log(GetDescription() + " Walking to "+pos.ToString());	
			
		// This stops the sequence being cancellable,  since 'Walking' property is used to check if it's possible to cancel, and BG walking shouldn't be cancelable
		if ( IsPlayer )
			PowerQuest.Get.DisableCancel();
		
		if ( Moveable == false )
			return;

		if ( m_instance != null && PowerQuest.Get.GetSkippingCutscene() == false )
		{
			m_waypoints.Add(pos);
			if ( m_instance.Walking == false )
				m_instance.WalkTo(pos, true, true);
		}
		else 
		{
			SetPosition(pos);
		}

	}
	
	// Non-blocking versions of facing functions
	public void FaceDownBG(bool instant = false) { Face(eFace.Down, instant); }
	public void FaceUpBG(bool instant = false) { Face(eFace.Up, instant); }
	public void FaceLeftBG(bool instant = false) { Face(eFace.Left, instant); }
	public void FaceRightBG(bool instant = false) { Face(eFace.Right, instant); }
	
	public void FaceUpRightBG(bool instant = false) { Face(eFace.UpRight, instant); }
	public void FaceUpLeftBG(bool instant = false) { Face(eFace.UpLeft, instant); }
	public void FaceDownRightBG(bool instant = false) { Face(eFace.DownRight, instant); }
	public void FaceDownLeftBG(bool instant = false) { Face(eFace.DownLeft, instant); }

	public void FaceBG( eFace direction, bool instant = false ) { Face(direction,instant); }
	public void FaceBG( IQuestClickableInterface clickable, bool instant = false ) { FaceBG(clickable.IClickable, instant); }
	public void FaceBG( IQuestClickable clickable, bool instant = false ) { Face(clickable,instant);}
	public void FaceBG(float x, float y, bool instant = false) { Face(x,y, instant); }
	public void FaceBG(Vector2 location, bool instant = false) { Face(location,instant); }
	public void FaceClickedBG(bool instant = false) { FaceClicked(instant); }
	public void FaceAwayBG(bool instant = false) { FaceAway(instant); }
	public void FaceDirectionBG(Vector2 directionV2, bool instant = false) { FaceDirection(directionV2,instant); }

	// Face enum direction
	public Coroutine Face( eFace direction, bool instant = false)
	{	
		eFace verticalFallback = eFace.None;
		if (direction != eFace.Up && direction != eFace.Down)
			verticalFallback = CharacterComponent.ToCardinal(direction);
		return FaceInternal(direction, instant, verticalFallback );
	}

	Coroutine FaceInternal( eFace direction, bool instant, eFace fallback )
	{		
		if ( direction == eFace.None )
			return null;
		if ( PowerQuest.Get.GetRoomLoading() || PowerQuest.Get.GetSkippingCutscene() || m_instance == null || Visible == false )
			instant = true;		
		if ( Walking && m_turnBeforeWalking == false )
			instant = true;
		if ( Walking == false && m_turnBeforeFacing == false )
			instant = true;

		if ( instant )
		{
			eFace oldFacingVerticalFallback = m_facingVerticalFallback;
			m_targetFaceDirection = direction;		

			if ( fallback != eFace.None )
				m_facingVerticalFallback = fallback;
				
			m_faceDirection = direction; // set this to snap to target
			if ( m_instance != null ) 
				m_instance.UpdateFacingVisuals(direction); 
			return null;
		}

		return PowerQuest.Get.StartQuestCoroutine(CoroutineFace(direction,fallback));//, oldFacingVerticalFallback)); 
	}

	public Coroutine FaceDown(bool instant = false) { return Face(eFace.Down, instant); }
	public Coroutine FaceUp(bool instant = false) { return Face(eFace.Up, instant); }
	public Coroutine FaceLeft(bool instant = false) { return Face(eFace.Left, instant); }
	public Coroutine FaceRight(bool instant = false) { return Face(eFace.Right, instant); }

	public Coroutine FaceUpRight(bool instant = false) { return Face(eFace.UpRight, instant); }
	public Coroutine FaceUpLeft(bool instant = false) { return Face(eFace.UpLeft, instant); }
	public Coroutine FaceDownRight(bool instant = false) { return Face(eFace.DownRight, instant); }
	public Coroutine FaceDownLeft(bool instant = false) { return Face(eFace.DownLeft, instant); }
	
	public Coroutine Face( IQuestClickableInterface clickable, bool instant = false ) { return Face(clickable.IClickable, instant); }
	public Coroutine Face( IQuestClickable clickable, bool instant = false )
	{
		if ( clickable == this.IClickable )
		{
			Debug.LogWarning($"Character {clickable.ScriptName} tried to Face() themselves");
			return null;
		}
		if ( clickable != null )
		{
			return Face(clickable.Position + clickable.LookAtPoint, instant);
		}
		return null;
	}

	// Face Location, from character's feet to coordinate. (NB: not actually a coroutine, just so yield returning this doesn't error)
	public Coroutine Face(float x, float y, bool instant = false) { return Face(new Vector2(x,y), instant);}
	public Coroutine Face(Vector2 location, bool instant = false)
	{
		return FaceDirection((location - m_position).normalized, instant);
	}
	public Coroutine FaceClicked(bool instant = false)
	{
		return Face(PowerQuest.Get.GetLastLookAt(), instant);
	}
	public Coroutine FaceAway(bool instant = false)
	{
		return FaceDirection( -FACE_DIRECTIONS[(int)GetTargetFaceDirection()], instant );		
	}

	// Face a vector direction. (NB: not actually a coroutine, just so yield returning this doesn't error)
	public Coroutine FaceDirection(Vector2 directionV2, bool instant = false)
	{	
		// Calculates the closest direction- Each direction (including diagonals) is a 30 degree segment

		if ( directionV2.sqrMagnitude <= 0 )
		{
			Debug.LogWarning("FaceDirection called with zero direction passed. Ignoring");
			return null;
		}
		int count = (int)eFace.UpRight+1;
		float cosAngle = Mathf.Cos(Mathf.Deg2Rad * PowerQuest.Get.FacingSegmentAngle*0.5f);
		directionV2.Normalize();
		//Debug.Log($"Angle: {directionV2.GetDirectionAngle()}");
		for ( int i = 0; i < count; ++i )
		{
			// Find match - within 30 degree tolerance
			if ( Vector2.Dot(FACE_DIRECTIONS[i],directionV2) >= cosAngle )
			{
				// If up or down- save a hint of left/right in case there's no up/down frames
				eFace face = (eFace)i;

				eFace verticalFallback = eFace.None;
				if ( directionV2.x > 0 ) 
					verticalFallback = eFace.Right;
				else if ( directionV2.x < 0 ) 
					verticalFallback = eFace.Left;
				
				return FaceInternal(face, instant, verticalFallback);
			}
		}
		return null;
	}


	/// Start charcter saying something
	public Coroutine Say(string dialog, int id = -1)
	{
		PowerQuest pq = PowerQuest.Get;
		if ( m_coroutineSay != null )
		{			
			pq.StopCoroutine(m_coroutineSay);
			EndSay();
			pq.OnSay(); // not sure what this is for... lol. is it meant to be outside this if?
		}
		
		if ( CallbackOnSay != null )
			CallbackOnSay.Invoke(dialog,id);
			
		if (pq.DialogInterruptRequested)
		{
			pq.ResetInterruptNextLine();
			return pq.StartCoroutine(CoroutineSayEndEarly(dialog, pq.DialogInterruptDuration, id));
		}

		m_coroutineSay = CoroutineSay(dialog, id);		
		return pq.StartCoroutine(m_coroutineSay); 
	}

	// Start character saying something in the background.
	public Coroutine SayBG(string dialog, int id = -1) 
	{
		if ( m_coroutineSay != null )
		{
			PowerQuest.Get.StopCoroutine(m_coroutineSay);
			EndSay();
		}
		m_coroutineSay = CoroutineSayBG(dialog, id);
		return PowerQuest.Get.StartCoroutine(m_coroutineSay); 
	}

	public void CancelSay()
	{
		if ( m_coroutineSay != null )
		{			
			PowerQuest.Get.StopCoroutine(m_coroutineSay);
			EndSay();
		}
	}

	public Coroutine PlayAnimation(string animName)
	{	
		ResetAnimationData();
		if ( m_instance != null ) return PowerQuest.Get.StartCoroutine(CoroutinePlayAnimation(animName)); 
		return null;
	}

	public Coroutine WaitForAnimation()
	{
		if ( m_instance != null ) return PowerQuest.Get.StartCoroutine(CoroutineWaitForAnimation()); 
		return null;
	}

	public string Animation 
	{
		get
		{
			return m_animOverride;
		}
		set
		{
			if ( value == null )
				StopAnimation();
			else
				PlayAnimationBG(value,true);
		}
	}

	public void PlayAnimationBG(string animName, bool pauseAtEnd = false )
	{
		ResetAnimationData();
		m_animOverride = animName;
		m_pauseAnimAtEnd = pauseAtEnd;
		if ( PowerQuest.Get.GetSkippingCutscene() && pauseAtEnd == false )
			return;
		if ( m_instance != null ) m_instance.PlayAnimation(animName); 
	}

	public void PauseAnimation()
	{
		if ( m_instance != null ) m_instance.PauseAnimation();
	}

	public void ResumeAnimation()
	{
		if ( m_instance != null ) m_instance.ResumeAnimation();
	}

	public void StopAnimation()
	{
		ResetAnimationData();
		if ( m_instance != null ) m_instance.StopAnimation();
	}

	public void SkipTransition()
	{
		if ( m_instance != null ) m_instance.SkipTransition();
	}

	

	public Coroutine WaitForTransition(bool skippable = false) { return PowerQuest.Get.StartCoroutine(CoroutineWaitForTransition(skippable)); }
	public IEnumerator CoroutineWaitForTransition(bool skippable)
	{
		if ( m_instance != null )
		{
			yield return PowerQuest.Get.WaitWhile(
				()=> m_instance != null && m_instance.GetPlayingTransition(), skippable ); // have to check for null incase instance is removed while waiting for idle
			if ( skippable )
			{
				SkipTransition();
				yield return null; // Wait a frame so we don't also skip any transition on the next frame
			}
		}
		yield break;
	}

	public bool Idle { get
	{
		return Animating == false
			&& Walking == false
			&& Talking == false
			&& (m_instance == null || m_instance.GetPlayingTransition() == false)
			&& m_targetFaceDirection == m_faceDirection;
	} }
	
	/// Waits until a character is idle. ie: Not Walking,Talking,Animating,Turning, or Transitioning
	public Coroutine WaitForIdle(bool skippable = false) { return PowerQuest.Get.StartCoroutine(CoroutineWaitForIdle(skippable)); }
	public IEnumerator CoroutineWaitForIdle(bool skippable)
	{		
		yield return null; // Wait an extra frame (doesn't seem to work otherwise)

		bool skipped = false;
		bool first = true; // first frame the mouse will always be down, so don't skip until 2nd
		while ( Idle == false && PowerQuest.Get.GetSkippingCutscene() == false )
		{
			if ( skippable )
				skipped = PowerQuest.Get.HandleSkipDialogKeyPressed();
			if ( skipped && first == false )
				break;

			first = false;
			yield return null;
		}	
		if ( skipped || PowerQuest.Get.GetSkippingCutscene() )
		{
			// Incase skipped, clear
			if (m_instance != null )
				m_instance.SkipWalk();
			CancelSay();
			StopAnimation();			
			Facing = m_targetFaceDirection;
			SkipTransition();
			yield return null; // Wait a frame so we don't also skip any transition on the next frame
		}

		yield break;	
	}

	public void AddAnimationTrigger(string triggerName, bool removeAfterTriggering, System.Action action )
	{
		if ( m_instance != null )
		{
			QuestAnimationTriggers triggerComponent = m_instance.GetComponent<QuestAnimationTriggers>();
			if ( triggerComponent == null )
				triggerComponent = m_instance.gameObject.AddComponent<QuestAnimationTriggers>();
			if ( triggerComponent != null )
				triggerComponent.AddTrigger(triggerName, action, removeAfterTriggering);
		}
	}

	public void RemoveAnimationTrigger(string triggerName)
	{
		if ( m_instance != null )
		{
			QuestAnimationTriggers triggerComponent = m_instance.GetComponent<QuestAnimationTriggers>();
			if ( triggerComponent != null )
				triggerComponent.RemoveTrigger(triggerName);
		}
	}

	public Coroutine WaitForAnimTrigger(string triggerName) { return PowerQuest.Get.StartCoroutine(CoroutineWaitForAnimTrigger(triggerName)); }

	public int ClickableColliderId 
	{ 
		get { return m_clickableColliderId; }
		set
		{
			m_clickableColliderId = value;
			if ( m_instance != null )
				m_instance.OnClickableColliderIdChanged();
		} 
	}
	
	public void UpdateFacingCharacter()
	{
		if ( m_faceChar == null || PowerQuest.Get.GetCharacter(m_faceChar.m_character) == null )
			return;
		if ( Walking == false && m_targetFaceDirection == m_faceDirection
			&& Utils.GetTimeIncrementPassed(m_faceChar.m_minTime, m_faceChar.m_maxTime, ref m_faceChar.m_timer)
			)
		{
			Character charctr = PowerQuest.Get.GetCharacter(m_faceChar.m_character);
			if ( charctr != null && charctr.VisibleInRoom )
				FaceBG(charctr as IQuestClickable);
		}
	}

	public string AnimShadow 
	{
		get { return m_animShadow; }
		set 
		{
			if ( m_animShadow == value)
				return;
			m_animShadow = value;
			if ( m_instance )
				m_instance.UpdateShadow();
		}
	}
	public bool ShadowEnabled 
	{
		get { return m_shadowOn; } 
		set
		{
			if ( m_shadowOn == value )
				return;
			m_shadowOn = value;
			if ( m_instance != null )
				m_instance.UpdateShadow();
		}
	}
	public void ShadowOn() { ShadowEnabled = true; }
	public void ShadowOff(){ ShadowEnabled = false; }

	#endregion
	#region Funcs: Private
	//
	// Internal Functions
	//


	IEnumerator CoroutineWalkTo( Vector2 position, bool anywhere, bool playWalkAnim = true )
	{		
		m_faceAfterWalk = eFace.None;			
		m_waypoints.Clear();

		// EndSay(); // 2021 06 11 - Don't stop talking when start walking anymore.

		if ( Moveable == false )
			yield break;

		// WalkToBG( position, anywhere ); // can't use walktoBG since that stops the sequence being cancelable
		{
			if ( m_instance != null && PowerQuest.Get.GetSkippingCutscene() == false )
				m_instance.WalkTo(position, anywhere, playWalkAnim );
			else 
				SetPosition(position);
		}		

		if ( PowerQuest.Get.GetSkippingCutscene() )
		{
			m_instance.SkipWalk();
			yield break;
		}
			
		bool skip = false;
		//bool cancel = false;
		while ( m_instance != null && skip == false && Moveable && m_instance.Walking )
		{
			if ( PowerQuest.Get.GetSkippingCutscene() )
			{
				// when walk is skipped, character teleports to location and face direction
				skip = true;
				m_instance.SkipWalk();
			}
			else 
			{
				yield return new WaitForEndOfFrame();
			}
		}

		// After first walk, sequences are no longer cancelable. 
		// Note, Previously ANY characters walk wouldstop the sequence being cancelable (It was buggy back on Coatrack when it was just the player... )
		// Trying it with player only again now, after having issues with following players breaking cancellable stuff.
		if ( IsPlayer )
			PowerQuest.Get.OnPlayerWalkComplete();
		
		yield break;
	}


	IEnumerator CoroutineSay(string text, int id = -1)
	{
		if ( PowerQuest.Get.GetStopWalkingToTalk() )
			StopWalking(); 
		if ( PowerQuest.Get.GetSkippingCutscene() )
			yield break;
		
		QuestText sayText = StartSay( text, id );
		yield return PowerQuest.Get.WaitForDialog(PowerQuest.Get.GetTextDisplayTime(text), m_dialogAudioSource, PowerQuest.Get.GetShouldSayTextAutoAdvance(), true, sayText);		
		EndSay();
	}

	IEnumerator CoroutineSayBG(string text, int id = -1)
	{			
		QuestText sayText = StartSay( text, id,true );
		yield return PowerQuest.Get.WaitForDialog(PowerQuest.Get.GetTextDisplayTime(text), m_dialogAudioSource, true, false, sayText);
		EndSay();
	}	
	
	IEnumerator CoroutineSayEndEarly(string text, float endTime, int id = -1)
	{
		if ( PowerQuest.Get.GetStopWalkingToTalk() )
			StopWalking(); 
		if ( PowerQuest.Get.GetSkippingCutscene() )
			yield break;

		// Start background dialog. This is the actual dialog that plays right to the end.
		SayBG(text,id);
		IEnumerator sayBGCoroutine = m_coroutineSay; // just incase things get messy with these multiple dialog coroutines, cache the saybg's one.

		// Next we wait until the dialog is skipped, or the full dialog is played

		float time = PowerQuest.Get.GetTextDisplayTime(text);
		//m_timeLastTextShown = Time.timeSinceLevelLoad;
		bool first = true; // first frame the mouse will always be down, so don't skip until 2nd
		
		bool skipped = false;
		bool stillPlaying = true;
		while (stillPlaying)
		{
			// Past end			
			stillPlaying = PowerQuest.Get.ShouldContinueDialog(first, ref time, true, PowerQuest.Get.GetShouldSayTextAutoAdvance(), m_dialogAudioSource, m_dialogText, endTime);						
			if ( stillPlaying )
			{
				first = false;
				yield return new WaitForEndOfFrame();
				if ( SystemTime.Paused == false )
				{
					time -= Time.deltaTime;
				}
			}
			else 
			{
				// check the "shouldcontinue" again- but this time with skippable off so we can tell if thats why its ended
				skipped = PowerQuest.Get.ShouldContinueDialog(first, ref time, false, PowerQuest.Get.GetShouldSayTextAutoAdvance(), m_dialogAudioSource, m_dialogText, endTime);
			}
		}
		if ( skipped && m_coroutineSay != null && m_coroutineSay == sayBGCoroutine )
		{
			// End the SayBG early because it was skipped
			PowerQuest.Get.StopCoroutine(sayBGCoroutine);
			EndSay();
		}
	}

	IEnumerator CoroutinePlayAnimation(string animName)
	{
		StopWalking();

		if ( PowerQuest.Get.GetSkippingCutscene() )
			yield break;
		
		if ( m_instance == null ) yield break;
		m_instance.PlayAnimation(animName);
		while ( Animating && PowerQuest.Get.GetSkippingCutscene() == false )
		{				
			yield return new WaitForEndOfFrame();
		}
		if ( m_instance.Animating )
			m_instance.StopAnimation();
	}

	IEnumerator CoroutineWaitForAnimation()
	{
		if ( PowerQuest.Get.GetSkippingCutscene() )
			yield break;

		if ( m_instance == null ) yield break;
		while ( m_instance.Animating && PowerQuest.Get.GetSkippingCutscene() == false )
		{				
			yield return new WaitForEndOfFrame();
		}
		if ( m_instance.Animating )
			m_instance.StopAnimation();		
	}

	
	IEnumerator CoroutineFace(eFace direction, eFace verticalFallback )
	{
		// Finish any transition animation or turn
		if ( m_instance.GetPlayingTransition() )
			yield return WaitForTransition(false);

		// Set turny stuff- after finishing transition animation
		eFace oldFacingVerticalFallback = m_facingVerticalFallback;
		m_targetFaceDirection = direction;
		if ( verticalFallback != eFace.None )
			m_facingVerticalFallback = verticalFallback;

		// Check for animation- if so, snap direction immediately
		if ( m_instance.StartTurnAnimation(oldFacingVerticalFallback))
		{
			m_faceDirection = direction; // set this to snap to target
			if ( m_instance != null ) 
				m_instance.UpdateFacingVisuals(direction); 			
		}

		if ( PowerQuest.Get.GetSkippingCutscene() )
		{			
			m_targetFaceDirection = direction;
			m_instance.UpdateFacingVisuals(direction);
			yield break;
		}
		bool skip = false;

		if ( m_instance != null && skip == false && Visible && PowerQuest.Get.GetSkippingCutscene() == false && PowerQuest.Get.GetRoomLoading() == false )
		{			
			while ( m_instance != null && skip == false && Visible && PowerQuest.Get.GetSkippingCutscene() == false && PowerQuest.Get.GetRoomLoading() == false
				&& (m_targetFaceDirection != m_faceDirection || m_instance.GetPlayingTurnAnimation()))
			{
				yield return new WaitForEndOfFrame();		
			}			
		}
		m_targetFaceDirection = m_faceDirection;
		m_instance.UpdateFacingVisuals(m_faceDirection);

		yield break;
	}

	bool m_startSayCalled = false;

	QuestText StartSay(string line, int id = -1, bool background = false )
	{		
		//Debug.Log($"StartSay- {ScriptName}: {line}");	
		m_startSayCalled=true;
		
		PowerQuest powerQuest = PowerQuest.Get;

		// Get translated string
		line = SystemText.GetDisplayText(line, id, m_scriptName, IsPlayer);

		// Start audio (if enabled)
		SystemAudio.Stop(m_dialogAudioSource);
		m_dialogAudioSource = null;
		m_dialogAudioSource = SystemText.PlayAudio(id, m_scriptName, (m_instance!= null?m_instance.transform:null));

		// if set to text only, set the volume to zero, still play it though for timing and lipsync
		if ( powerQuest.Settings.DialogDisplay == QuestSettings.eDialogDisplay.TextOnly && m_dialogAudioSource != null )
			m_dialogAudioSource.volume = 0;		

		// Create or set dialog text active (if enabled)
		GameObject speechObj = null;
		eSpeechStyle speechStyle = powerQuest.SpeechStyle;	
		if ( speechStyle == eSpeechStyle.AboveCharacter || speechStyle == eSpeechStyle.Caption || background )
		{
			// Above character speech (Lucasarts style)
			bool showText = ( m_dialogAudioSource == null || powerQuest.Settings.DialogDisplay != QuestSettings.eDialogDisplay.SpeechOnly );
			if ( showText )
			{
				if (m_dialogText == null )
				{
					GameObject go = GameObject.Instantiate(powerQuest.GetDialogTextPrefab().gameObject) as GameObject;
					m_dialogText = go.GetComponent<QuestText>();
					m_dialogText.color = m_textColour;
				}
				else
				{
					m_dialogText.gameObject.SetActive(true);
				}
				speechObj = m_dialogText.gameObject;

				// Convert position to gui camera space
				if( speechStyle != eSpeechStyle.Caption )
				{
					m_dialogText.OrderInLayer = background ? -15 : -10;
					if ( m_instance == null )
					{
						//Vector2 dialogWorldPos = m_position.WithZ(m_dialogText.transform.position.z);
						
						Vector3 dialogWorldPos = m_position;
						if ( TextPositionOverride != Vector2.zero )
							dialogWorldPos = TextPositionOverride;
						m_dialogText.AttachTo(dialogWorldPos);					
					}
					else 
					{
						// Attach with instance transform so it'll move with it.
						Vector3 dialogWorldPos = m_instance.GetTextPosition();
						m_dialogText.AttachTo(m_instance.transform, dialogWorldPos);
					}
				}
			}

			if ( m_instance != null )
			{
				m_instance.StartSay(line, id);
			}
			
			if ( showText )
			{			
				m_dialogText.SetText(line);
			}
		}
		
		else 
		{
			string guiName = (speechStyle == eSpeechStyle.Portrait ) ? "SpeechBox" : powerQuest.CustomSpeechGui;
			Gui dialogGui =  powerQuest.GetGui(guiName);
			if ( dialogGui != null && dialogGui.Instance != null )
				speechObj = dialogGui.Instance.gameObject;
		}

		QuestText resultText = m_dialogText;
		if ( speechObj != null )
		{				
			System.Array.ForEach(speechObj.GetComponents<ISpeechGui>(), iSpeechGui=>iSpeechGui.StartSay(this, line, id, background));
			// Find the quest text that is used for the text. (This is currently only used for skipping "typing" text. Would be neater as ISpeechGui function, but that'd break for people updating.
			System.Array.ForEach(speechObj.GetComponentsInChildren<QuestText>(), item=>
			{
				if ( resultText == null || (item != null && item.text == line) )
					resultText = item;
			});
		}

		// Return a text component- this is so speech guis can have their text skipped. NB: would be neater for ISpeechGui.StartSay to return this, but it would break for people updating :(
		return resultText;
	}

	void EndSay()
	{	
		//Debug.Log($"EndSay- {ScriptName}");
		if ( m_startSayCalled && CallbackOnEndSay != null )
		{
			//Debug.Log("CallbackOnEndSay");
			CallbackOnEndSay.Invoke();
		}

		SystemAudio.Stop(m_dialogAudioSource);
		if ( m_dialogText != null )
		{
			m_dialogText.gameObject.SetActive(false);
		}
		if ( m_instance != null )
		{
			m_instance.EndSay();
		}
		
		// Get the speech gameobject
		GameObject speechObj = null;
		PowerQuest powerQuest = PowerQuest.Get;
		eSpeechStyle speechStyle = powerQuest.SpeechStyle;
		if ( speechStyle == eSpeechStyle.AboveCharacter || speechStyle == eSpeechStyle.Caption )
		{
			if ( m_dialogText != null )
				speechObj = m_dialogText.gameObject;
		}
		else 
		{
			string guiName = (speechStyle == eSpeechStyle.Portrait ) ? "SpeechBox" : powerQuest.CustomSpeechGui;
			Gui dialogGui =  powerQuest.GetGui(guiName);
			if ( dialogGui != null && dialogGui.Instance != null )
				speechObj = dialogGui.Instance.gameObject;
		}
		
		// Call end say on ISpeechGui component, if found
		if ( speechObj != null )
		{
			System.Array.ForEach(speechObj.GetComponents<ISpeechGui>(),iSpeechGui=>iSpeechGui.EndSay(this));
		}
		
		m_startSayCalled = false;
	}

	IEnumerator CoroutineWaitForAnimTrigger(string triggerName)
	{
		if ( PowerQuest.Get.GetSkippingCutscene() == false )
		{
			bool hit = false;
			AddAnimationTrigger(triggerName,true,()=>hit=true);
			yield return PowerQuest.Get.WaitUntil(()=> hit || m_instance == null || m_instance.GetSpriteAnimator().Playing == false );
		}
		yield break;
	}
	
	// Animation state that's saved. Needs to be reset when playing/stopping animations
	void ResetAnimationData()
	{
		m_animOverride = null;
		m_animationTime = -1;
		m_pauseAnimAtEnd = false;

		/* // NB: don't want to reset loop stuff, since that would stop transitioning out when "Stopanimtion" is called.
		m_loopEndTime = -1;
		m_loopStartTime = -1;
		*/
	}

	#endregion

	// Handles setting up defaults incase items have been added or removed since last loading a save file
	[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}
	
}

}
