using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace PowerTools.Quest
{

/// The main system where all the juicy goodness happens
public partial class PowerQuest : Singleton<PowerQuest>, ISerializationCallbackReceiver, IQuestScriptable, IPowerQuest
{	
	#region internal classes/definitions
	
	static readonly System.Type TYPE_COMPILERGENERATED = typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute);
	static readonly string FUNC_UPDATE = "Update";
	static readonly string FUNC_UPDATE_NOPAUSE = "UpdateNoPause";
	public static readonly string STR_NARR = "Narr";
	public static readonly string SPRITE_NUM_POSTFIX_0 = "_0";

	private static readonly int MaxColliderInteractions = 256;
	private Collider2D[] m_tempPicked = new Collider2D[MaxColliderInteractions];

	// Class for timers
	[System.Serializable] class Timer { public string n; public float t; }	

	// Variables in this class are all saved/restored automatically
	partial class SavedVarCollection
	{
		public SourceList m_occurrences = new SourceList();
		public List<string> m_tempDisabledProps = new List<string>();
		public List<string> m_tempDisabledHotspots = new List<string>();
		public List<string> m_tempCursorNoneProps = new List<string>();
		public List<string> m_tempCursorNoneHotspots = new List<string>();
		public List<string> m_tempCursorNoneCursor = new List<string>();

		public List<string> m_currentInteractionOccurrences = new List<string>();
		public List<string> m_captureInputSources = new List<string>();
		public List<Timer> m_timers = new List<Timer>();
		public Dictionary<string,ShuffledIndex> m_shuffledItems = new Dictionary<string,ShuffledIndex>();
		public bool m_callEnterOnRestore = false; // when true, restoring a game will call "OnEnterAfterFade, etc again"		
		public bool m_useFancyParallaxSnapping = true;
	}

	enum eInventoryClickStyle { SelectInventory, UseInventory, OnMouseClick }

	#endregion
	#region Variables: Inspector

	[Header("Default In-Game Settings")]

	[Tooltip("Config Settings (like volume) that are game wide.")]
	[SerializeField] QuestSettings m_settings = new QuestSettings();

	
	[Header("Screen Setup")]
	[Tooltip("The default vertical resolution of the game. How many pixels high the camera view will be.")]
	[SerializeField] float m_verticalResolution = 180;	
	[Tooltip("The range of horizontal resolution your game supports. How many pixels wide the camera view will be. If the screen aspect ratio goes narrower or wider than this the game will be letterboxed. (Use to set what aspect ratios you support)")]
	[UnityEngine.Serialization.FormerlySerializedAs("m_horizontalResolution")]
	[SerializeField] MinMaxRange m_letterboxWidth = new MinMaxRange(320);
	[Tooltip("Whether camera and other things snap to pixel. For pixel art games")]
	[SerializeField] bool m_snapToPixel = true;
	[Tooltip("Whether to set up a pixel camera that renderes sprites at pixel resolution. For pixel art games")]
	[SerializeField] bool m_pixelCamEnabled = false;
	[Tooltip("Default pixels per unit that sprites are imported at")]
	[SerializeField] float m_defaultPixelsPerUnit = 1;
		
	[Header("Dialog Speech Display Setup")]
	[Tooltip("How is dialog displayed. Above head (lucasarts style), next to a portrait (sierra style), or as a caption not attached to character position")]
	[SerializeField] eSpeechStyle m_speechStyle = eSpeechStyle.AboveCharacter;
	[Tooltip("Which side is portrait located (currently only LEFT is implemented)")]
	[SerializeField] eSpeechPortraitLocation m_speechPortraitLocation = eSpeechPortraitLocation.Left;
	
	[Tooltip("Prefab for displayed above character")]
	[SerializeField] QuestText m_dialogTextPrefab = null;
	[Tooltip("Global offset of dialog text (above character sprite)")]
	[SerializeField] Vector2 m_dialogTextOffset = Vector2.zero;
	[Tooltip("Set speech style to AboveCharacter to use, and implement ISpeechGui")]
	[SerializeField] string m_customSpeechGui = "";
	
	[SerializeField] string m_displayBoxGui = "DisplayBox";
	[SerializeField] string m_dialogTreeGui = "DialogTree";

	[Header("Other Dialog Speech Setup")]
	[Tooltip("When clicking to skip text, ignore clicks until text has been shown for this time")]
	[SerializeField] float m_textNoSkipTime = 0.25f;	
	[Tooltip("Whether charaters stop walking automatically when they start talking")]
	[SerializeField] bool m_stopWalkingToTalk = true;
	[Tooltip("Whether character dialog text requires a click to advance, or dismisses after dialog's spoken/after time")]
	[SerializeField] bool m_sayTextAutoAdvance = true;
	[Tooltip("Whether display requires a click to advance, or dismisses after dialog's spoken/after time")]
	[SerializeField] bool m_displayTextAutoAdvance = false;
	[Tooltip("After dialog's audio finishes, how long before going to next line (sec)")]
	[SerializeField] float m_textAutoAdvanceDelay = 0.0f;	
	[Tooltip("If true, display is shown even when subtitles off")]
	[SerializeField] bool m_alwaysShowDisplayText = true;

	[Header("Project Verb Setup")]

	[SerializeField] bool m_enableUse = true;
	[SerializeField] bool m_enableLook = true;
	[SerializeField] bool m_enableInventory = true;
	[Tooltip("Whether clicking inventory results in 'Selecting' it Broken Sword style, or 'Using' it Lucasarts style, or specified in GlobalScript's OnMouseClick")]
	[SerializeField] eInventoryClickStyle m_inventoryClickStyle = eInventoryClickStyle.OnMouseClick;		
	[Tooltip("When true, no editor keyboard shortcuts will be used")]
	[SerializeField] bool m_customKbShortcuts = false;
	[Tooltip("Enable when using the parser. When enabled, also add SystemParser to the list of Systems")]
	[SerializeField] bool m_enableParser = false;

	[Header("Screen-Fade Setup")]
	[SerializeField] float m_transitionFadeTime = 0.3f;
	[SerializeField] QuestMenuManager m_menuManager = null;
		
	[Header("Spawnables")]
	[Tooltip("Add objects here so you can spawn them by name in QuestScripts")]
	[SerializeField] List<GameObject> m_spawnablePrefabs = new List<GameObject>();
		
	[Header("Project Character settings")]
	[Tooltip("Controls what angles the player is considered 'facing' a direction (right, up, down-left,etc). Increase to favour cardinal directions more than diagonals")]
	[Range(45,90)]
	[SerializeField] float m_facingSegmentAngle = 45.0f;
	
	[Header("Save Game Settings")]
	[Tooltip("Height in pixels of screenshot recorded in save-game slot data. Set to 0 to disable saving screenshot with save games")]
	[SerializeField] int m_saveScreenshotHeight = 180;
	[Tooltip("Increase when the data you're saving changes, and you need to know if you're loading an old save game")]
	[SerializeField] int m_saveVersion = 0;
	[Tooltip("Increase when you can no longer save games of a specific version. After launch you should avoid increasing this if possible or player's save files get invalidated!")]
	[SerializeField] int m_saveVersionRequired = 0;

	[Header("Text Sprite Setup")]

	[SerializeField]  Material m_textSpriteMaterial = null;
	[ReorderableArray,NonReorderable]
	[SerializeField] QuestText.TextSpriteData[] m_textSprites = null;

	[Header("Other Systems To Create")]
	[SerializeField] List<Component> m_systems = null;
		
	[Header("Prefab Lists (Read only, enable debug inspector to edit)")]	
	[SerializeField, ReadOnly] QuestCursorComponent m_cursorPrefab = null;
	[SerializeField, ReadOnly] QuestCameraComponent m_cameraPrefab = null;
	[SerializeField, ReadOnly, NonReorderable] List<RoomComponent> m_roomPrefabs = new List<RoomComponent>();
	[SerializeField, ReadOnly, NonReorderable] List<CharacterComponent> m_characterPrefabs = new List<CharacterComponent>();
	[SerializeField, ReadOnly, NonReorderable] List<GuiComponent> m_guiPrefabs = new List<GuiComponent>();
	[SerializeField, ReadOnly, NonReorderable] List<DialogTreeComponent> m_dialogTreePrefabs = new List<DialogTreeComponent>();
	[SerializeField, ReadOnly, NonReorderable] List<InventoryComponent> m_inventoryPrefabs = new List<InventoryComponent>();	
	[SerializeField, ReadOnly, NonReorderable] List<AnimationClip> m_inventoryAnimations = new List<AnimationClip>();
	[SerializeField, ReadOnly, NonReorderable] List<Sprite> m_inventorySprites = new List<Sprite>();
	[SerializeField, ReadOnly, NonReorderable] List<AnimationClip> m_guiAnimations = new List<AnimationClip>();
	[SerializeField, ReadOnly, NonReorderable] List<Sprite> m_guiSprites = new List<Sprite>();

	[Header("Pre-loaded Shaders")]

	#pragma warning disable 414
	[SerializeField] Shader[] m_preloadedShaders = null;
	#pragma warning restore 414

	// Used when upgrading powerquest
	[SerializeField, HideInInspector] int m_version = -1;

	// Version checking vars
	[SerializeField, HideInInspector] int m_newVersion = -1;

	//
	// Private variables
	//
	#endregion
	#region Variables: Private

	QuestScript m_globalScript = null;
	QuestCamera m_cameraData = new QuestCamera();
	UnityEngine.Camera m_cameraGui = null;
	Canvas m_canvas = null;
	QuestCursor m_cursor = null;
	Character m_player = null;
	List<Character> m_characters = new List<Character>();
	List<Inventory> m_inventoryItems = new List<Inventory>();
	List<DialogTree> m_dialogTrees = new List<DialogTree>();
	List<Gui> m_guis = new List<Gui>();
	List<Room> m_rooms = new List<Room>();

	Room m_currentRoom = null;
	#pragma warning disable 414
	DialogOption m_dialogOptionSelected = null; // Will probably use this later
	#pragma warning restore 414
	DialogTree m_currentDialog = null;
	DialogTree m_previousDialog = null;
		
	bool m_skipCutscene = false;		// If true, coroutines will skip any waiting and get to the end
	bool m_interruptNextLine = false;
	float m_interruptNextLineTime = 0.0f;
	bool m_skipDialog = false;			// If true talk/wait coroutines will skip
	bool m_blocking = false;			// True when other interactions cannot be performed
	bool m_transitioning = false;		// True when transitioning between rooms
	bool m_roomLoopStarted = false;		// True while mainloop is running (after transition and OnRoomEnterAfterFadein)
	bool m_cutscene = false;			// True when in a cutscene
	bool m_initialised = false; 	    // Flag set true after first ever awake has finished and all questobjects have been created 
	bool m_walkClickDown = false;       // Flag set when clicked to walk and reset when mouse button released
	bool m_guiConsumedClick = false;	 // Flags "game" mouse clicks to be ignored, resets after every update. NB: No longer necessary (AFAIK)
	bool m_sequenceIsCancelable = false; // If true, the current sequence will be stopped on click
	bool m_allowEnableCancel = false; 	// Used to determine if rest of sequence can be safely skipped
	bool m_serializeComplete = false;
	bool m_restoring = false;
	bool m_displayActive = false;		// True when Display dialog is visible or audio is playing (triggered by Display or DisplayBG)
	bool m_skipCutsceneButtonConsumed = false; // true when skip cutscene button is pressed, and waiting for it to be released (so don't process "held down esc" events)
	bool m_waitingForBGDialogSkip = false; // Used for "WaitForDialogSkip", which turns any background dialog into foreground dialog. Experimental feature.
	bool m_restartOnUpdate = false;	// Flag used to restart at start of next update
	int m_nextOption = -1;			// Used for FirstOption, NextOption, NextOccurrence, etc
	
	static bool s_hasRestarted = false;
	static string s_restartScene = null;	
	static string s_restartPlayFromFunction = null;
	static Assembly s_restartAssembly = null;

	// Extra snapping tweaks
	public bool UseFancyParalaxSnapping { get { return SV.m_useFancyParallaxSnapping; } set { SV.m_useFancyParallaxSnapping = value; } }

	// These mouse clicks are for the blocking update, which 
	//bool m_leftClick = false;		
	//bool m_rightClick = false;
	bool m_leftClickPrev = false;
	bool m_rightClickPrev = false;
	bool m_overrideMousePos = false;
	Vector2 m_mousePos = Vector2.zero;
	Vector2 m_mousePosGui = Vector2.zero;
	float m_timeLastTextShown = 0;

	Coroutine m_currentSequence = null;	// Main active sequence
	List<Coroutine> m_queuedScriptInteractions = new List<Coroutine>();
	Coroutine m_coroutineMainLoop = null;	// Stored so that when changing scenes, the main loop coroutine can be stopped, and then restarted in next room.
	IEnumerator m_coroutineSay = null;
	bool m_levelLoadedCalled = false; 	// So that OnLevelLoad code can be only handled once
	bool m_overrideMouseOverClickable = false;
	IQuestClickable m_mouseOverClickable = null; // What the mouse is hovering over
	IQuestClickable m_lastClickable = null; // Used for accessing "WalkTo" and "LookAt" of current interaction
	string m_mouseOverDescriptionOverride = null;

	AudioHandle m_dialogAudioSource = null;
	SavedVarCollection m_savedVars = new SavedVarCollection();
	SavedVarCollection SV {get {return m_savedVars;}} // Shortcut to saved variables collection

	//
	// Variables for canceling current sequences
	//
	Coroutine m_backgroundSequence = null;	// Cancelable sequence
	List<Coroutine> m_currentSequences = new List<Coroutine>(); // List of current sequence coroutines. Used for canceling sequences
	List<Coroutine> m_backgroundSequences = new List<Coroutine>(); // ListOf current cancelable coroutines
	// Cached current interaction/verb for rolling back, incase edited before first (cancelable) WalkTo
	List<IQuestClickable>m_currentInteractionClickables = new List<IQuestClickable>();
	List<eQuestVerb> m_currentInteractionVerbs = new List<eQuestVerb>();


	// Hot-loading stuff
	static Assembly m_hotLoadAssembly = null; // static because I don't want it to reset when resetting game for debugging
	// List<IQuestScriptable> m_hotLoadedScriptables = null; // for now just recompiling all scriptables

	// Stuff for auto-loading scripts based on current interaction running
	static readonly string STR_UNHANDLED = "Unhandled";
	IQuestScriptable m_autoLoadScriptable = null;
	string m_autoLoadFunction = string.Empty;
	IQuestScriptable m_autoLoadUnhandledScriptable = null;
	string m_autoLoadUnhandledFunction = string.Empty;
	
	bool m_ignoreAutoLoadFunc = false;
	string m_ignoreAutoLoadFuncName = string.Empty;

	Coroutine m_consumedInteraction = null;


	#endregion
	
	#region Properties: Public

	public bool DialogInterruptRequested => m_interruptNextLine;
	public float DialogInterruptDuration => m_interruptNextLineTime;
	public float FacingSegmentAngle => m_facingSegmentAngle;

	#endregion
	
	#region Functions: Implementing IPowerQuest

	/// Breaks instantly, rather than yielding for a frame
	public YieldInstruction Break { get { return EMPTY_YIELD_INSTRUCTION; } } 
	/// Breaks instantly, and but registers as having "done something", so player won't say "I can't do that"
	public YieldInstruction ConsumeEvent { get { return CONSUME_YIELD_INSTRUCTION; } } 
	
	/// Returns true for developer builds, or if QUESTDEBUG is defined. So you can have debug features enabled in non-debug builds
	public bool IsDebugBuild { get 
	{
		// Later, could also add option for a bool that can be enabled by cheat command
		#if QUESTDEBUG
			return true;
		#else
			return Debug.isDebugBuild;
		#endif	
	} }

	/// Convenient shortcut to the game camera
	public ICamera Camera	{ get{ return GetCamera(); } }
	/// Convenient shortcut to the Cursor
	public ICursor Cursor { get{ return GetCursor(); } }


	/// Restart the game from the first scene
	public void Restart()
	{		
		s_hasRestarted = true;
		s_restartScene = null;
		s_restartPlayFromFunction = null;
		s_restartAssembly = m_hotLoadAssembly;
		m_restartOnUpdate = true;
		// Destroy everything, including powerquest, and load the game scene again
		StopAllCoroutines();
	}

	/// Restart the game on a specific scene, optionally with a specific 'playFromFunction'.
	public void Restart( IRoom room, string playFromFunction= null )
	{		
		s_hasRestarted = true;
		s_restartScene = (room as Room).GetSceneName();		
		LoadAtlas(room.ScriptName);
		s_restartPlayFromFunction = playFromFunction;
		s_restartAssembly = m_hotLoadAssembly;
		m_restartOnUpdate = true;
		// Destroy everything, including powerquest, and load the game scene again
		StopAllCoroutines();
	}

	// Wait for time (or default 0.5 sec)
	public Coroutine Wait(float time = 0.5f)	{	return StartQuestCoroutine(CoroutineWaitForTime(time, false)); }
	// Wait for time (or default 0.5 sec). Pressing button will skip the waiting
	public Coroutine WaitSkip(float time = 0.5f)	{	return StartQuestCoroutine(CoroutineWaitForTime(time, true)); }
	// Wait for a timer to expire (use the name used with E.SetTimer()). Will remove the timer on complete
	public Coroutine WaitForTimer(string timerName, bool skippable = false)	{ return StartQuestCoroutine(CoroutineWaitForTimer(timerName, skippable)); }

	/// Invokes the specified function after the specified time has elapsed (non-blocking). EG: `E.DelayedInvoke(1, ()=/>{ C.Plr.FaceLeft(); } );`
	public void DelayedInvoke( float time, System.Action functionToInvoke ) { StartQuestCoroutine(CoroutineDelayedInvoke(time, functionToInvoke)); }

	/// <summary>
	/// Use this when you want to yield to another function that returns an IEnumerator
	/// Usage: yield return E.WaitFor( SimpleExampleFunction ); or yield return E.WaitFor( ()=>ExampleFunctionWithParams(C.Dave, "lol") );
	/// 
	/// NB: Currently this cannot be used from a sequence started by E.StartBackgroundSequence(). It'll cause problems. If needed, tell Dave!
	/// </summary>
	/// 
	/// <param name="functionToWaitFor">A function that returns IEnumerator. Eg: "SimpleExampleFunction" or, "()=/>ExampleFunctionWithParams(C.Dave, 69)" if it has params</param>
	public Coroutine WaitFor( DelegateWaitForFunction functionToWaitFor, bool autoLoadQuestScript = true ) 
	{ 
		// update the script to auto-load (ignored when not in editor)
		if ( Application.isEditor && functionToWaitFor != null && functionToWaitFor.Target != null && autoLoadQuestScript )
		{			
			// Search through all scriptables and find one with matching classname
			List<IQuestScriptable> scriptables = PowerQuest.Get.GetAllScriptables();
			string classname = functionToWaitFor.Target.GetType().Name;
			IQuestScriptable scriptable = scriptables.Find(item=>item.GetScriptClassName() == classname);
			// Set the auto-laod script. TODO: Once the coroutine has finished, return to the previous script.
			if ( scriptable != null )
			{
				// If function is called like this: WaitFor(()=>MyFunction(34); The function name will be dynamic, eg. "<OnInteractDoor>b__20_0" don't think we can auto-load it.				
				//if ( functionToWaitFor.Method.IsSpecialName == false && functionToWaitFor.Method.Name[0] != '<') // check it's not dynamic lambda expression method. Can't auto-load these.
				if ( System.Attribute.IsDefined(functionToWaitFor.Method, TYPE_COMPILERGENERATED) == false )
					SetAutoLoadScript( scriptable, functionToWaitFor.Method.Name, true, true ); // Note that if the calling function hasn't yielded yet, the auto-load script will be set by the calling function AFTER this call :(
			}	
		}		

		// Note: If started from a background sequence, we don't want to start a "skippable" coroutine... otherwise we do... But no way of knowing afaik..
		return StartQuestCoroutine(functionToWaitFor(),true); 
	}
	public Coroutine WaitWhile( System.Func<bool> condition, bool skippable = false ) { return StartQuestCoroutine(CoroutineWaitWhile(condition, skippable)); }
	public Coroutine WaitUntil( System.Func<bool> condition, bool skippable = false ) { return StartQuestCoroutine(CoroutineWaitUntil(condition, skippable)); }
	public Coroutine WaitForDialog() {	return StartQuestCoroutine(CoroutineWaitForDialog()); }	
	public Coroutine WaitForDialogSkip() { return StartQuestCoroutine(CoroutineWaitForDialogSkip()); }
	
	
	//
	// Narrator 
	//

	public Coroutine Display( string dialog, int id = -1 ) 
	{ 
		if ( m_coroutineSay != null )
		{
			StopCoroutine(m_coroutineSay);
			EndDisplay();
		}
		m_coroutineSay = CoroutineDisplay(dialog,id);
		return StartCoroutine(m_coroutineSay); 
	}
	public Coroutine DisplayBG( string dialog, int id = -1 ) 
	{ 
		if ( m_coroutineSay != null )
		{
			StopCoroutine(m_coroutineSay);
			EndDisplay();
		}
		m_coroutineSay = CoroutineDisplayBG(dialog,id);
		StartCoroutine(m_coroutineSay); 
		return StartCoroutine(CoroutineEmpty()); // Start a coroutine so can wait a frame
	}

	//
	// Cutscenes
	//

	public void StartCutscene() { m_cutscene = true; }
	public void EndCutscene() { OnEndCutscene(); }

	//
	// Screen transitions (fading to/from a color)
	//

	public Coroutine FadeIn( float time = 0.2f, bool skippable = true )	{	return StartCoroutine(CoroutineFadeIn(DEFAULT_FADE_SOURCE, time, skippable)); }
	public Coroutine FadeOut( float time = 0.2f, bool skippable = true )	{	return StartCoroutine(CoroutineFadeOut(DEFAULT_FADE_SOURCE, time, skippable)); }
	public Coroutine FadeIn( float time, string source, bool skippable = true )	{	return StartCoroutine(CoroutineFadeIn(source, time, skippable)); }
	public Coroutine FadeOut( float time, string source, bool skippable = true )	{	return StartCoroutine(CoroutineFadeOut(source, time, skippable)); }

	public void FadeInBG( float time = 0.2f ) {	m_menuManager.FadeIn(time, DEFAULT_FADE_SOURCE);}
	public void FadeOutBG( float time = 0.2f ){	m_menuManager.FadeOut(time, DEFAULT_FADE_SOURCE);}
	public void FadeInBG( float time, string source ) {	m_menuManager.FadeIn(time, source);}
	public void FadeOutBG( float time, string source ){	m_menuManager.FadeOut(time, source);}

	public bool GetFading() { return m_menuManager.GetFading(); }
	public Color FadeColor { get{return m_menuManager.FadeColor;} set{ m_menuManager.FadeColor = value;} }
	public Color FadeColorDefault { get{return m_menuManager.FadeColorDefault;} set{ m_menuManager.FadeColorDefault = value;} }
	public void FadeColorRestore() { m_menuManager.FadeColorRestore(); }

	public float GetFadeRatio() {return m_menuManager.GetFadeRatio();}
	public Color GetFadeColor() {return m_menuManager.GetFadeColor();}

	public QuestMenuManager GetMenuManager() { return m_menuManager; }
	

	//
	// Pause/Unpause the game
	//

	public bool Paused 
	{ 
		get { return SystemTime.Paused; } 

		set 
		{ 
			if ( value ) 
				Pause(); 
			else 
				UnPause(); 
		} 
	}

	public void Pause(string source = null)
	{
		if ( SystemTime.HasInstance() )
			SystemTime.Get.PauseGame(source);
	}

	public void UnPause(string source = null)
	{
		if ( SystemTime.HasInstance() )
			SystemTime.Get.UnPauseGame(source);
	}

	//
	// Start/Stop timers
	//

	private Timer FindTimer(string name) 
	{
		foreach (var timer in SV.m_timers) 
		{
			if (timer.n.Equals(name, System.StringComparison.OrdinalIgnoreCase)) 
				return timer;
		}

		return null;
	}

	/// Starts timer with a *name*, counting down from `time` in seconds. Set to zero to remove timer
	public void SetTimer(string name, float time) 
	{
		Timer timer = FindTimer(name);

		// Setting to zero removes timer
		if ( time <= 0 )
		{
			if ( timer != null )
				SV.m_timers.Remove(timer);
			return;
		}

		if ( timer == null)
		{
			timer = new Timer(){n = name, t = time};
			SV.m_timers.Add(timer);
		}		
		
		timer.t = time;		
	}
	/// Checks whether the timer with specified `name` has expired. If the timeout set with SetTimer has elapsed, returns *true*. Otherwise, returns *false*.
	public bool GetTimerExpired(string name)
	{
		Timer timer = FindTimer(name);
		if ( timer != null && timer.t <= 0 )
		{
			SV.m_timers.Remove(timer);
			return true;
		}
		return false;
	}
	
	///	Returns timer value 	
	public float GetTimer(string name)
	{
		Timer timer = FindTimer(name);
		return (timer != null && timer.t > 0) ? timer.t : 0;
	}

	//
	// Change room
	//


	/// Change the current room. Alternative to C.Plr.Room = room;
	public void ChangeRoomBG( IRoom room ) { GetPlayer().Room = room; }

	/// Change the current room. Can be yielded too, and blocks until after OnEnterAfterFade of the new room finishes.
	public Coroutine ChangeRoom( IRoom room ) { return StartCoroutine( CoroutineChangeRoom(room) ); }

	//
	// Access to Quest Objects (rooms, characters, inventory, dialog, guis)
	//

	/// Returns the quest script matching the object type passed in. Eg: E.GetScript<RoomKitchen>().m_drawerOpen = true;
	public T GetScript<T>() where T : QuestScript
	{ 
		string scriptName = typeof(T).ToString();
		IQuestScriptable scriptable = null;
		System.Type type = typeof(T);
		if ( type.IsSubclassOf(typeof(RoomScript<T>)) )
		{
			scriptName = scriptName.Substring(4);
			scriptable = GetRoom(scriptName);
		}
		else if ( type.IsSubclassOf(typeof(CharacterScript<T>)) )
		{
			scriptName = scriptName.Substring(9);
			scriptable = GetCharacter(scriptName);
		}
		else if ( type.IsSubclassOf(typeof(DialogTreeScript<T>)) )
		{
			scriptName = scriptName.Substring(6);
			scriptable = GetDialogTree(scriptName);
		}
		else if ( type.IsSubclassOf(typeof(InventoryScript<T>)) )
		{
			scriptName = scriptName.Substring(9);
			scriptable = GetInventory(scriptName);
		}
		else if ( type.IsSubclassOf(typeof(GuiScript<T>)) )
		{
			scriptName = scriptName.Substring(3);
			scriptable = GetGui(scriptName);
		}
		else if ( type.ToString() == GLOBAL_SCRIPT_NAME ) // type.IsSubclassOf(typeof(GlobalScriptBase<T>)) )
		{
			scriptName = GLOBAL_SCRIPT_NAME;
			scriptable = this;
		}
		if ( scriptable != null && scriptable.GetScript() != null )
			return scriptable.GetScript() as T;

		return null;
	}

	public IRoom GetRestoringRoom() { return GetSavable( m_player.Room as Room ); }
	public Room GetCurrentRoom()
	{
		//if ( GetRestoringGame() )
		//	return GetSavable( m_player.Room as Room ); // While restoring a game, the m_currentRoom hasn't been set yet, so set the room the players actually in.  But actually no. doesn't work.
		 return GetSavable(m_currentRoom); 
	}
	
	/// Debugging function that overrides the value of `R.Previous`. Useful for testing, paricularly in 'Play from` functions- (when using the [QuestPlayFromFunction] attribute)
	public void DebugSetPreviousRoom(IRoom room)
	{
		GetPlayer().DebugSetLastRoom(room);
	}

	public Room GetRoom(string scriptName) 
	{ 		
		Room result = QuestUtils.FindScriptable(m_rooms, scriptName);
		if ( result == null && string.IsNullOrEmpty(scriptName) == false )
			Debug.LogError("Room doesn't exist: "+scriptName+". Check for typos and that it's added to PowerQuest");				
		return GetSavable(result);
	}

	public ICharacter Player { get { return GetSavable(m_player); } set { SetPlayer(value, 0.6f); } }
	public Character GetPlayer() { return GetSavable(m_player); }
	public void SetPlayer(ICharacter character, float cameraTransitionTime = 0) 
	{ 
		bool sameRoom = character != null && m_player != null && character.Room == m_player.Room;
		Character newPlayer = GetCharacter(character.ScriptName);
		m_player = newPlayer;
		GetCamera().SetCharacterToFollow(newPlayer,sameRoom ? cameraTransitionTime : 0 );
		ChangeRoomBG(m_player.Room);
	}
	public Character GetCharacter(string scriptName) { Systems.Text.LastPlayerName=SystemText.ePlayerName.Character; return GetSavable(QuestUtils.FindScriptable(m_characters, scriptName)); }
	/// Shortcut to the current player's active inventory  
	public IInventory ActiveInventory { get { return GetSavable(m_player.ActiveInventory as Inventory); } set { m_player.ActiveInventory = value; } }
	
	// NB: Potential pitfall, if modify items from this list, they aren't marked as dirty for save system. To flag dirty, use PowerQuest.GetSavable(item);
	public List<Inventory> GetInventoryItems_SaveFlagNotDirtied() { return m_inventoryItems; }
	public Inventory GetInventory(string scriptName) { return GetSavable(QuestUtils.FindScriptable(m_inventoryItems, scriptName));	}
		
	public static T GetSavable<T>(T savable) where T: IQuestSaveCachable 
	{ 
		if ( savable != null )
		{ 
			/* // Debugging /
			if ( savable.SaveDirtyEver == false )
			{ 
				if ( savable is Prop )
					Debug.Log( $"Marked {(savable as Prop).ScriptName} dirty" );
			}
			/**/
			savable.SaveDirty = true; 
		}
		return savable; 
	}
	public DialogTree GetCurrentDialog() { return GetSavable(m_currentDialog); }	
	public DialogTree GetPreviousDialog() { return GetSavable(m_previousDialog); }
	public DialogTree GetDialogTree(string scriptName) { return GetSavable(QuestUtils.FindScriptable(m_dialogTrees, scriptName)); }
	public Gui GetGui(string scriptName) { return QuestUtils.FindScriptable(m_guis, scriptName); }
	public GameObject GetSpawnablePrefab(string name) { return QuestUtils.FindByName(m_spawnablePrefabs, name); }

	//
	// Access to useful system data
	//

	public UnityEngine.Camera GetCameraGui() { return m_cameraGui; }
	public Canvas GetCanvas() { return m_canvas; }
	public Vector2 GetMousePosition() { return m_mousePos; }
	public Vector2 GetMousePositionGui() { return m_mousePosGui; }
	public bool GetHasMousePositionOverride() { return m_overrideMousePos; }
	public void SetMousePositionOverride(Vector2 mousePos) { m_overrideMousePos = true; m_mousePos = mousePos; }
	public void ResetMousePositionOverride() { m_overrideMousePos = false; } 
	public IQuestClickable GetMouseOverClickable() { return m_mouseOverClickable; }
	public eQuestClickableType GetMouseOverType() { return m_mouseOverClickable == null ? eQuestClickableType.None : m_mouseOverClickable.ClickableType; }
	public void SetMouseOverClickableOverride( IQuestClickable clickable ) { if ( m_focusedControlLock ) return;  m_overrideMouseOverClickable = true; m_mouseOverClickable = clickable; }
	public void ResetMouseOverClickableOverride() { m_overrideMouseOverClickable = false; if ( m_focusedControlLock ) return; m_mouseOverClickable = null; } 
	public string GetMouseOverDescription() 
	{ 
		if ( m_mouseOverDescriptionOverride != null )
			return m_mouseOverDescriptionOverride;		
		return (m_mouseOverClickable != null) ? m_mouseOverClickable.Description : string.Empty; 
	}

	// Exposed to allow overriding for WalkToClicked/FaceClicked for the parser
	public void SetLastClickable(IQuestClickable clickable) { m_lastClickable = clickable; }
	public Vector2 GetLastLookAt() { return (m_lastClickable == null || m_lastClickable.Instance == null) ? Vector2.zero : (m_lastClickable.LookAtPoint + (Vector2)m_lastClickable.Instance.transform.position); }
	public Vector2 GetLastWalkTo() { return (m_lastClickable == null || m_lastClickable.Instance == null) ? Vector2.zero : (m_lastClickable.WalkToPoint + (Vector2)m_lastClickable.Instance.transform.position); }

	
	public IGui GetFocusedGui() { return m_focusedGui; }
	public IGuiControl GetFocusedGuiControl() 
	{ 
		// When script blocked, contols aren't focused UNLESS it's part of a "blocking gui" (gui shown during blocked scripts, like prompts)...
		// Maybe guis themselves should be able to set whether they're controllable while blocking...?
		if ( PowerQuest.Get.GetBlocked() == false || (m_focusedGui != null && m_focusedGui == m_blockingGui) )
			return m_focusedControl; 
		return null;
	}

	public bool GameHasKeyboardFocus => m_keyboardFocusedControl == null;
	public GuiControl GetKeyboardFocus() { return m_keyboardFocusedControl; }
	public void SetKeyboardFocus(GuiControl control) 
	{ 
		if ( control == m_keyboardFocusedControl )
			return;

		// Defocus old focused control
		if ( m_keyboardFocusedControl != null )
			m_keyboardFocusedControl.OnKeyboardDefocus();
			
		// change current focus
		m_keyboardFocusedControl = control; 

		// Focus new focused control
		if ( m_keyboardFocusedControl != null )
			m_keyboardFocusedControl.OnKeyboardFocus();
	}


	/// Returns the current vertical resolution (using room override if one exists)
	public float VerticalResolution { get 
		{
			if ( m_currentRoom != null && m_currentRoom.VerticalResolution > 0 )
				return m_currentRoom.VerticalResolution;				
			return m_verticalResolution; 
		} 
	}
	public float DefaultVerticalResolution { get { return m_verticalResolution; } }

	public MinMaxRange HorizontalResolution => m_letterboxWidth;
	public void EditorSetHorizontalResolution(MinMaxRange range) { m_letterboxWidth = range; }

	//
	// Settings
	//

	// Shortcut to access settings
	public QuestSettings Settings { get { return m_settings; } }
		
	public string DisplayBoxGui
	{ 
		get { return m_displayBoxGui; }
		set { m_displayBoxGui = value; }
	}
	public string DialogTreeGui
	{ 
		get { return m_dialogTreeGui; }
		set { m_dialogTreeGui = value; }
	}

	public string CustomSpeechGui
	{
		get { return m_customSpeechGui; }
		set { m_customSpeechGui = value; }
	}

	/// Whether "Display" text is shown regardless of the DialogDisplay setting
	public bool AlwaysShowDisplayText 
	{ 
		get { return m_alwaysShowDisplayText; }
		set { m_alwaysShowDisplayText = value; }
	}

	public eSpeechStyle SpeechStyle 
	{ 
		get { return m_speechStyle; }
		set { m_speechStyle = value; }
	}

	public eSpeechPortraitLocation SpeechPortraitLocation
	{ 
		get { return m_speechPortraitLocation; }
		set { m_speechPortraitLocation = value; }
	}

	/// Length of time transition between rooms takes
	public float TransitionFadeTime { get { return m_transitionFadeTime; } set {m_transitionFadeTime = value; } }

	//
	// Functions for handling mouse clicks on things
	//
		
	/// ProcessGuiClick is a bit different to ProcessClick. 
	/// It doesn't call OnAnyClick, or care verb is selected, there's no unhandled event for it eithers
	public bool ProcessGuiClick(Gui gui, GuiControl control = null )
	{
		// If control is null it means the gui itself was clicked
		IQuestClickable clickable = (control == null) ? (gui as IQuestClickable) : (control as IQuestClickable);
		
		if ( control != null )
			gui = control.GuiData;

		bool interactionFound = false;

		if ( gui == null ) // can be null with legacy guis
			return interactionFound;
			
		if ( ( Paused || gui.Modal ) && m_inventoryClickStyle == eInventoryClickStyle.OnMouseClick )
		{
			// Call globalscript onmouse click... Mainly because we want to be able to right click when inventory's up and have it clear the active inventory item.				
			System.Reflection.MethodInfo method = null;
			if ( m_globalScript != null )
			{
				method = m_globalScript.GetType().GetMethod( SCRIPT_FUNCTION_ONMOUSECLICK, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				if ( method != null ) 
					method.Invoke(m_globalScript, new object[]{Input.GetMouseButton(0), Input.GetMouseButton(1)});

				// If any script interactions were queued, return 'true' to unpause the inventory
				if ( m_queuedScriptInteractions.Count > 0 )
					interactionFound = true;
			}		
		}

		// Call gui's OnAnyClick function, passing in the control. If this is blocking, the main button interaction will still be called
		if ( StartScriptInteraction(gui.GetScriptable(), SCRIPT_FUNCTION_ONANYCLICK, new object[] {control}, false, true ) )
		{		
			m_queuedScriptInteractions.Add(m_currentSequence);
			interactionFound = true;
		}

		if (  Input.GetMouseButton(1) == false && control != null ) // Note: we're checking that it's NOT the right mousebutton... dumb. But easier than passing down whether the LeftMouseButton was *lifted* from the button... ugh.
		{
			PowerQuest.Get.UnlockFocusedControl(); // do this here so can use Gui.Navigate functions in the OnClick
			if ( StartScriptInteraction( gui.GetScriptable(), SCRIPT_FUNCTION_CLICKGUI+control.ScriptName, new object[] {control}, false, true ) )
			{		
				m_queuedScriptInteractions.Add(m_currentSequence);

				interactionFound = true;
			}
		}

		return interactionFound;
	}

	// Similar to ProcessGuiClick, but any event name can be passed in, and it'll call it on the script, with the control name appended. Event name maybe something like "onDrag" or "onHover" I guess...
	public bool ProcessGuiEvent(string eventName, Gui gui, GuiControl control = null )
	{
		// If control is null it means the gui itself was clicked
		IQuestClickable clickable = (control == null) ? (gui as IQuestClickable) : (control as IQuestClickable);
		
		if ( control != null )
			gui = control.GuiData;

		bool interactionFound = false;

		/*
		if ( ( Paused || gui.Modal ) && m_inventoryClickStyle == eInventoryClickStyle.OnMouseClick )
		{
			// Call globalscript onmouse click... Mainly because we want to be able to right click when inventory's up and have it clear the active inventory item.				
			System.Reflection.MethodInfo method = null;
			if ( m_globalScript != null )
			{
				method = m_globalScript.GetType().GetMethod( SCRIPT_FUNCTION_ONMOUSECLICK, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				if ( method != null ) 
					method.Invoke(m_globalScript, new object[]{Input.GetMouseButton(0), Input.GetMouseButton(1)});

				// If any script interactions were queued, return 'true' to unpause the inventory
				if ( m_queuedScriptInteractions.Count > 0 )
					interactionFound = true;
			}		
		}
		*/

		// Call gui's function, passing in the control. If this is blocking, the main button interaction will still be called
		if ( StartScriptInteraction(gui.GetScriptable(), eventName, new object[] {control}, false, true ) )
		{		
			m_queuedScriptInteractions.Add(m_currentSequence);
			interactionFound = true;
		}

		if ( control != null )
		{
			if ( StartScriptInteraction( gui.GetScriptable(), eventName+control.ScriptName, new object[] {control}, false, true ) )
			{		
				m_queuedScriptInteractions.Add(m_currentSequence);
				interactionFound = true;
			}
		}

		return interactionFound;
	}

	/// Starts the specified action for the verb on whatever the mouse is over (whatever the current GetMouseOverClickable() happens to be ). 
	/**
	 * This would usually be called from the OnMouseClick function in your global script
	 */
	// Processes click on whatever the current mouse is over
	public bool ProcessClick( eQuestVerb verb ) { return ProcessClick(verb, m_mouseOverClickable, m_mousePos); }
	// Processes click on a specified clickable and/or mouse position
	public bool ProcessClick( eQuestVerb verb, IQuestClickable clickable, Vector2 mousePosition )
	{
		bool interactionFound = false;
		bool clickHandled = false;
		GameObject clickedObj = (clickable == null || clickable.Instance == null) ? null : (clickable.Instance.gameObject);

		// 
		// Call "OnAnyClick" first
		//
		if ( clickHandled == false )
		{
			// OnAnyClick is first checked in global script and in the current room. So can be used to interrupt what would normally cause an action
			if ( clickHandled == false )
				clickHandled = StartScriptInteraction(m_currentRoom, SCRIPT_FUNCTION_ONANYCLICK );
			if ( clickHandled == false )
				clickHandled = StartScriptInteraction( this, SCRIPT_FUNCTION_ONANYCLICK );

			if ( clickHandled )
			{
				m_queuedScriptInteractions.Add(m_currentSequence);
				interactionFound = true;
			}
		}

		if ( clickHandled == false && verb == eQuestVerb.None )
		{
			clickHandled = true;
		}
		
		if ( clickHandled == false && verb == eQuestVerb.Parser )
		{
			// Cancel any current interaction.
			OnInteraction(null, eQuestVerb.Parser);
			
			// Handle parser
			{
				// Check for "OnWalkTo" interrupts
				if ( clickHandled == false )
					clickHandled = StartScriptInteraction(m_currentRoom, SCRIPT_FUNCTION_ONPARSER );
				if ( clickHandled == false )
					clickHandled = StartScriptInteraction( this, SCRIPT_FUNCTION_ONPARSER );

				if ( clickHandled )
					m_queuedScriptInteractions.Add(m_currentSequence); 
				
				//CancelCurrentInteraction();
				clickHandled = true;
				interactionFound = true;
			}
		}

		if ( clickHandled == false && verb == eQuestVerb.Walk )
		{
			// Cancel any current interaction.
			OnInteraction(null,eQuestVerb.Walk);

			// Handle walk to clicked
			{
				// Check for "OnWalkTo" interrupts
				if ( clickHandled == false )
					clickHandled = StartScriptInteraction(m_currentRoom, SCRIPT_FUNCTION_ONWALKTO );
				if ( clickHandled == false )
					clickHandled = StartScriptInteraction( this, SCRIPT_FUNCTION_ONWALKTO );
				if ( clickHandled )
				{
					m_queuedScriptInteractions.Add(m_currentSequence); 
					interactionFound = true;
				}
			}

			if ( clickHandled == false )
			{
				// Walk to the current mouse position	
				m_player.WalkToBG(mousePosition);
				if ( mousePosition == m_mousePos ) // don't "hold down walk" if passed in position isn't just the mouse pos (otherwise it'll be overridden next update).
					m_walkClickDown = true; 
				clickHandled = true;
				interactionFound = true;
			}
		}

		//
		// Action the verb
		//
		

		// Click character
		if ( clickedObj != null && clickHandled == false )
		{							
			CharacterComponent component = clickedObj.GetComponent<CharacterComponent>();
			if ( component != null ) 
			{
				m_lastClickable = clickable;
				OnInteraction(m_lastClickable,verb);
				// First try in room
				{
					switch ( verb )
					{
						case eQuestVerb.Inventory: { clickHandled = StartScriptInteraction( m_currentRoom.GetScriptable(), SCRIPT_FUNCTION_USEINV_CHARACTER+m_lastClickable.ScriptName, new object[] {component.GetData(), m_player.ActiveInventory}, true ); } break;
						case eQuestVerb.Look: { clickHandled = StartScriptInteraction( m_currentRoom.GetScriptable(), SCRIPT_FUNCTION_LOOKAT_CHARACTER+m_lastClickable.ScriptName, new object[] {component.GetData()}, true ); } break;
						case eQuestVerb.Use: { clickHandled = StartScriptInteraction( m_currentRoom.GetScriptable(), SCRIPT_FUNCTION_INTERACT_CHARACTER+m_lastClickable.ScriptName, new object[] {component.GetData()}, true ); } break;
					}
				}
				if ( clickHandled == false )
				{
					// If didn't find override in room script, try in character script
					switch ( verb )
					{
					case eQuestVerb.Inventory: { clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), SCRIPT_FUNCTION_USEINV, new object[] {m_player.ActiveInventory}, true ); } break;
					case eQuestVerb.Look: { clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), SCRIPT_FUNCTION_LOOKAT, null, true ); } break;
					case eQuestVerb.Use: { clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), SCRIPT_FUNCTION_INTERACT, null, true ); } break;
					}
				}
				if ( clickHandled )
				{
					m_queuedScriptInteractions.Add(m_currentSequence);
					interactionFound = true;
				}
			}
		}

		// Click hotspot
		if ( clickedObj != null && clickHandled == false )
		{
			// try hotspot
			HotspotComponent component = clickedObj.GetComponent<HotspotComponent>();
			if ( component != null ) 
			{
				m_lastClickable = clickable;
				OnInteraction(m_lastClickable,verb);
				if ( verb == eQuestVerb.Inventory && m_player.HasActiveInventory )
					clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), PowerQuest.SCRIPT_FUNCTION_USEINV_HOTSPOT + m_lastClickable.ScriptName, new object[] {component.GetData(), m_player.ActiveInventory}, true );
				else 
					clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), (verb != eQuestVerb.Look ? SCRIPT_FUNCTION_INTERACT_HOTSPOT : SCRIPT_FUNCTION_LOOKAT_HOTSPOT) + m_lastClickable.ScriptName, new object[] {component.GetData()}, true );
				if ( clickHandled )
				{
					m_queuedScriptInteractions.Add(m_currentSequence);
					interactionFound = true;
				}
			}
		}

		// Click prop
		if ( clickedObj != null && clickHandled == false )
		{
			// try prop
			PropComponent component = clickedObj.GetComponent<PropComponent>();
			if ( component != null )
			{
				m_lastClickable = clickable;
				OnInteraction(m_lastClickable,verb);
				if ( verb == eQuestVerb.Inventory && m_player.HasActiveInventory )
					clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), PowerQuest.SCRIPT_FUNCTION_USEINV_PROP + m_lastClickable.ScriptName, new object[] {component.GetData(), m_player.ActiveInventory}, true );
				else 
					clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), (verb != eQuestVerb.Look ? SCRIPT_FUNCTION_INTERACT_PROP : SCRIPT_FUNCTION_LOOKAT_PROP) + m_lastClickable.ScriptName, new object[] {component.GetData()}, true );
				if ( clickHandled )
				{
					m_queuedScriptInteractions.Add(m_currentSequence);
					interactionFound = true;
				}
			}
		}
		
		// Click inventory item - Only if inventory click style is "OnMouseClick"
		if ( clickHandled == false && m_inventoryClickStyle == eInventoryClickStyle.OnMouseClick)
		{				
			if ( clickable.ClickableType == eQuestClickableType.Inventory )
			{
				m_lastClickable = clickable;
				OnInteraction(m_lastClickable,verb);
				if ( verb == eQuestVerb.Inventory && m_player.HasActiveInventory )
				{
					// clicked one inventory item on another
					clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, new object[] {clickable as IInventory, m_player.ActiveInventory}, true );
					// If click wasn't handled, try swapping the items
					if ( clickHandled == false && m_player.HasActiveInventory )
						clickHandled = StartScriptInteraction( m_player.ActiveInventory as IQuestScriptable, PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, new object[] {m_player.ActiveInventory, clickable as IInventory}, true );					
				}
				else 
				{
					clickHandled = StartScriptInteraction( m_lastClickable.GetScriptable(), (verb != eQuestVerb.Look ? SCRIPT_FUNCTION_INTERACT_INVENTORY : SCRIPT_FUNCTION_LOOKAT_INVENTORY), new object[] {clickable as IInventory}, true );
				}
				if ( clickHandled )
				{
					m_queuedScriptInteractions.Add(m_currentSequence);
					interactionFound = true;
				}
			}
		}
		
		
		//
		// Clicked something, but no sequence was started, so fallback to UnhandledEvent
		//
		if ( clickable != null && clickHandled == false )
		{
			// Unhandled event
			//StartScriptInteraction(m_gameScript, verb == eQuestVerb.Use ? "UnhandledInteract" : "UnhandledLookAt" );

			if ( m_globalScript != null )
			{								
				string methodName = "";
				object[] parameters = null;
				bool stopWalk = true;
				if ( verb == eQuestVerb.Inventory && m_player.HasActiveInventory )
				{
					// Add if ( item == I.Blah ) code to clipboard so can paste it in easily
					#if UNITY_EDITOR
						if ( ActiveInventory != null )
							UnityEditor.EditorGUIUtility.systemCopyBuffer = $"\nif ( I.{ActiveInventory.ScriptName} == item )\n{{\n    \n}}\n";
					#endif
					if ( clickable.ClickableType == eQuestClickableType.Inventory ) // use items on eachother
					{
						// Try unhandledUseInvInv, but fallback to UnhandledUseInv
						
						methodName = "UnhandledUseInvInv";
						IInventory inv = clickable as IInventory;
						IInventory activeInv = m_player.ActiveInventory;

						if ( clickHandled == false )
							clickHandled = StartScriptInteraction( this, methodName, new object[] {inv, activeInv}, stopWalk );
						
						// If click wasn't handled, try using inventory item in opposite order, if it works one way it should work the other
						if ( clickHandled == false )							
							clickHandled = StartScriptInteraction( this, methodName, new object[] {activeInv, inv}, stopWalk );
						
						// If still not handled, fall back to UnhandledUseInv
						if ( clickHandled == false )
							methodName = "UnhandledUseInv";
					}
					else  // use items on other things
					{
						methodName = "UnhandledUseInv";
					}
					parameters = new object[]{ clickable, m_player.ActiveInventory };
				}
				else // other clicks on things
				{
					methodName = verb != eQuestVerb.Look ? "UnhandledInteract" : "UnhandledLookAt";
					parameters = new object[]{ clickable };
					if ( clickable.ClickableType == eQuestClickableType.Inventory )
						stopWalk = false; // Don't stop walking for unhandled inventory event. Since we want to let you change inventory without stopping
				}
				
				// Try Unhandled in room first
				if ( clickHandled == false )				
					clickHandled = StartScriptInteraction( m_currentRoom.GetScriptable(), methodName, parameters, stopWalk );

				if ( clickHandled == false )				
					clickHandled = StartScriptInteraction( this, methodName, parameters, stopWalk );

				if ( clickHandled )
				{
					m_queuedScriptInteractions.Add(m_currentSequence);
					interactionFound = true;
				}
			}
		}
		if ( CallbackOnProcessClick != null )
			CallbackOnProcessClick.Invoke(interactionFound);

		ExProcessClick(verb,clickable,mousePosition,interactionFound);

		return interactionFound;
	}

	//
	// Functions that let scripts call other scripts interaction functions
	//
	public Coroutine HandleInteract( IHotspot target )
	{		
		return Handle(target.IClickable, eQuestVerb.Use, SCRIPT_FUNCTION_INTERACT_HOTSPOT, new object[] {target}, "UnhandledInteract",new object[]{ target.IClickable });
	}
	public Coroutine HandleLookAt( IHotspot target )
	{
		return Handle(target.IClickable, eQuestVerb.Look, SCRIPT_FUNCTION_LOOKAT_HOTSPOT, new object[] {target}, "UnhandledLookAt",new object[]{ target.IClickable });
	}
	public Coroutine HandleInventory( IHotspot target, IInventory item )
	{
		return Handle(target.IClickable, eQuestVerb.Inventory, SCRIPT_FUNCTION_USEINV_HOTSPOT, new object[] {target, item},"UnhandledUseInv",new object[]{ target.IClickable, item });
	}
	public Coroutine HandleInteract( IProp target )
	{
		return Handle(target.IClickable, eQuestVerb.Use, SCRIPT_FUNCTION_INTERACT_PROP, new object[] {target},"UnhandledUse",new object[]{ target.IClickable });
	}
	public Coroutine HandleLookAt( IProp target )
	{
		return Handle(target.IClickable, eQuestVerb.Look, SCRIPT_FUNCTION_LOOKAT_PROP, new object[] {target},"UnhandledLookAt",new object[]{ target.IClickable });
	}
	public Coroutine HandleInventory( IProp target, IInventory item )	
	{
		return Handle(target.IClickable, eQuestVerb.Inventory, SCRIPT_FUNCTION_USEINV_PROP, new object[] {target, item},"UnhandledUseInv",new object[]{ target.IClickable, item as Inventory });
	}
	public Coroutine HandleInteract( ICharacter target )	
	{ 		
		OnHandleInteraction(target.IClickable, eQuestVerb.Use);
		// first try in room script, then fall back to character script
		Coroutine result = StartScriptInteractionCoroutine( m_currentRoom.GetScriptable().GetScript(), SCRIPT_FUNCTION_INTERACT_CHARACTER+target.ScriptName, new object[] {target}, true ); 
		if ( result != null )
		{
			SetAutoLoadScript( m_currentRoom.GetScriptable(), SCRIPT_FUNCTION_INTERACT_CHARACTER+target.ScriptName, true, true ); 
			return result;
		}
		result = StartScriptInteractionCoroutine( target.IClickable.GetScript(), SCRIPT_FUNCTION_INTERACT, null, true ); 
		SetAutoLoadScript( target.Data.GetScriptable(), SCRIPT_FUNCTION_INTERACT, result != null, true ); 
		return result;
	}
	public Coroutine HandleLookAt( ICharacter target )	
	{ 
		OnHandleInteraction(target.IClickable, eQuestVerb.Look);
		// first try in room script, then fall back to character script
		Coroutine result = StartScriptInteractionCoroutine( m_currentRoom.GetScriptable().GetScript(), SCRIPT_FUNCTION_LOOKAT_CHARACTER+target.ScriptName, new object[] {target}, true ); 
		if ( result != null )
		{
			SetAutoLoadScript( m_currentRoom.GetScriptable(), SCRIPT_FUNCTION_LOOKAT_CHARACTER+target.ScriptName, true, true ); 
			return result;
		}
		result = StartScriptInteractionCoroutine( target.IClickable.GetScript(), SCRIPT_FUNCTION_LOOKAT, null, true ); 
		SetAutoLoadScript( target.Data.GetScriptable(), SCRIPT_FUNCTION_LOOKAT, result != null, true );
		return result;
	}
	public Coroutine HandleInventory( ICharacter target, IInventory item ) 
	{ 
		OnHandleInteraction(target.IClickable, eQuestVerb.Inventory);
		// first try in room script, then fall back to character script
		Coroutine result = StartScriptInteractionCoroutine( m_currentRoom.GetScriptable().GetScript(), SCRIPT_FUNCTION_USEINV_CHARACTER+target.ScriptName, new object[] {target,item as Inventory}, true ); 
		if ( result != null )
		{
			SetAutoLoadScript( m_currentRoom.GetScriptable(), SCRIPT_FUNCTION_USEINV_CHARACTER+target.ScriptName, true, true ); 
			return result;
		}
		result = StartScriptInteractionCoroutine( target.IClickable.GetScript(), SCRIPT_FUNCTION_USEINV, new object[] {item}, true ); 
		SetAutoLoadScript( target.Data.GetScriptable(), SCRIPT_FUNCTION_USEINV, result != null, true );
		return result;
	}
	public Coroutine HandleInteract( IInventory target )	
	{ 
		return Handle(target.IClickable, eQuestVerb.Use, SCRIPT_FUNCTION_INTERACT_INVENTORY, new object[] {target},"UnhandledUse",new object[]{ target.IClickable });
		//Coroutine result = StartScriptInteractionCoroutine( target.Data.GetScript(), SCRIPT_FUNCTION_INTERACT_INVENTORY, new object[] {target}, true ); 
		//SetAutoLoadScript( target.Data.GetScriptable(), SCRIPT_FUNCTION_INTERACT_INVENTORY, result != null, true ); // set skipCallingFunction so it loads after its called function.
		//return result;
	}
	public Coroutine HandleLookAt( IInventory target ) 
	{ 
		return Handle(target.IClickable, eQuestVerb.Look, SCRIPT_FUNCTION_LOOKAT_INVENTORY, new object[] {target},"UnhandledLookAt",new object[]{ target.IClickable });
		//Coroutine result = StartScriptInteractionCoroutine( target.Data.GetScript(), PowerQuest.SCRIPT_FUNCTION_LOOKAT_INVENTORY, new object[] {target}, true ); 
		//SetAutoLoadScript( target.Data.GetScriptable(), SCRIPT_FUNCTION_INTERACT_INVENTORY, result != null, true ); // set skipCallingFunction so it loads after its called function.
		//return result;
	}
	public Coroutine HandleInventory( IInventory target, IInventory item ) 
	{ 		
		return Handle(target.IClickable, eQuestVerb.Inventory, SCRIPT_FUNCTION_USEINV_INVENTORY, new object[] {target, item},"UnhandledUseInvInv",new object[]{ target as Inventory, item as Inventory });
		//return StartScriptInteractionCoroutine( target.Data.GetScript(), PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, new object[] {target, item}, true ); 
	}
	

	/// Runs a specific dialog option. NB: Does NOT start the dialog tree first
	public Coroutine HandleOption( IDialogTree dialog, string optionName )
	{ 
		DialogOption option = (DialogOption)dialog.GetOption(optionName);
		//return StartScriptInteractionCoroutine( dialog.Data.GetScript(), PowerQuest.SCRIPT_FUNCTION_DIALOG_OPTION+option.ScriptName, new object[] {option} ); 

		// Increment 'used' status- will be undone if option isn't handled.
		bool wasUsed = option.Used;
		option.Used=true;
		option.TimesUsed++;

		Coroutine coroutine = StartScriptInteractionCoroutine(dialog.Data.GetScript(), SCRIPT_FUNCTION_DIALOG_OPTION + option.Name, new object[]{option}, false );
		if ( coroutine == null )
			coroutine = StartScriptInteractionCoroutine(dialog.Data.GetScript(), SCRIPT_FUNCTION_DIALOG_OPTION + option.Name, null, false );			
		
		SetAutoLoadScript( dialog.Data.GetScriptable(), SCRIPT_FUNCTION_DIALOG_OPTION + option.Name, coroutine != null, true ); // set skipCallingFunction so it loads after its called function.

		if ( coroutine != null )
		{
			GetGui(DialogTreeGui).Visible = false;
		}
		else
		{
			// Undo Used and TimesUsed- This ensures these properties are consistant no matter where in the script coroutine they're checked.
			option.Used = wasUsed;
			option.TimesUsed--;
		}
		return coroutine;
	}
	
	// Generic "Handle" function for passing events through
	Coroutine Handle<T>(T clickable, eQuestVerb verb, string function, object[] parameters, string unhandledFunc, object[] unhandledParams) where T: IQuestClickable
	{ 
		// when parser's enabled, handle stuff like you're clicking on it
		if ( m_enableParser )
			m_lastClickable = clickable;

		OnHandleInteraction(clickable, verb);		
		if ( clickable.ClickableType != eQuestClickableType.Inventory )
			function = function + clickable.ScriptName;
		Coroutine result =  StartScriptInteractionCoroutine( clickable.GetScript(), function, parameters, true );
		if ( result != null )		
			SetAutoLoadScript( clickable.GetScriptable(), function, result != null, true ); // set skipCallingFunction so it loads after its called function.
		if ( m_enableParser )
		{	// when parser's enabled- 'Handle' functions call through to unhandled events so "look at bklajsdf" will still call UnhandledLook
			if ( result == null ) //|| result != m_consumedInteraction ) // removed this check during PowerJam3 - it wsa stopping Consume working in LookAt
				result = StartScriptInteractionCoroutine( m_currentRoom.GetScript(), unhandledFunc, unhandledParams, true );
			if ( result == null ) //|| result != m_consumedInteraction )
				result = StartScriptInteractionCoroutine( GetScript(), unhandledFunc, unhandledParams, true );
		}
		return result;
	}

	//
	// Misc utilities
	//

	public Vector2 WorldPositionToGui(Vector2 position)
	{
		return (Vector2)m_cameraGui.ViewportToWorldPoint( m_cameraData.Camera.WorldToViewportPoint(position) );
	}
	public Vector2 GuiPositionToWorld(Vector2 position)
	{
		return (Vector2)m_cameraData.Camera.ViewportToWorldPoint( m_cameraGui.WorldToViewportPoint(position) );
	}
	
	/// Get whether canceling the current sequence is enabled
	public bool GetCanCancel()
	{
		return m_sequenceIsCancelable;
	}

	/// Enables canceling in the current sequence. This would potentially cause lots of issues with half-run functions, so not recommended.
	public void EnableCancel() 
	{
		if ( m_sequenceIsCancelable == false && m_allowEnableCancel )
		{
			EnableCancelInternal();

			// Restart the main loop
			StopCoroutine(m_coroutineMainLoop);
			m_coroutineMainLoop = StartCoroutine( MainLoop() );
		}
	}

	/// Disables canceling during the current sequence
	public void DisableCancel() 
	{ 
		if ( m_sequenceIsCancelable && m_backgroundSequence != null )
		{
			// swap background sequenct to be the current sequence
			m_currentSequence = m_backgroundSequence;
			m_backgroundSequence = null;

			// Swap coroutines lists back
			m_currentSequences = m_backgroundSequences;
			m_backgroundSequences.Clear();

		}
		m_sequenceIsCancelable = false; 
		m_allowEnableCancel = false;
	}

	// Advanced function- allows you to cancel current sequence in progress. Use to interupt player interactions when something else happens (eg: on trigger or something in UpdateBlocking)
	public void CancelCurrentInteraction()
	{
		if ( m_sequenceIsCancelable && m_backgroundSequence != null )
		{
			
			// Rollback sequence "used" and "Clicked" and "Occurrences"
			for ( int i = 0; i < m_currentInteractionClickables.Count; ++i)
				m_currentInteractionClickables[i]?.OnCancelInteraction(m_currentInteractionVerbs[i]);
				
			m_currentInteractionClickables.Clear();
			m_currentInteractionVerbs.Clear();
			
			SV.m_currentInteractionOccurrences.ForEach(occurrence=>SV.m_occurrences.Remove(occurrence));
			SV.m_currentInteractionOccurrences.Clear();	

			// Stop the background sequence as well as all other sequences started since "cancelable"
			StopCoroutine(m_backgroundSequence);
			m_backgroundSequence = null;
			foreach( Coroutine routine in m_backgroundSequences )
			{
				if ( routine != null )
					StopCoroutine(routine);			
			}
			m_backgroundSequences.Clear();

			// Clear the current sequence and sequences list
			m_currentSequence = null;
			m_currentSequences.Clear();
			m_sequenceIsCancelable = false;

			//Debug.Log($"Cancelled Interaction. Queued sequences: {m_queuedScriptInteractions.Count}");
		}	

	}


	// Returns true the first time something occurrs, increments each time
	public bool FirstOccurrence(string uniqueString)
	{
		if ( m_allowEnableCancel )
			SV.m_currentInteractionOccurrences.Add(uniqueString);
		int result = SV.m_occurrences.Add(uniqueString)-1;
		m_nextOption = result-1;
		return result <= 0;
	}
	// Checks how many times something has occurred without incrementing the occurrence
	public int GetOccurrenceCount(string thing)
	{
		return SV.m_occurrences.Count(thing);
	}

	// Returns number of times something has occurred, and incrementing the number
	public int Occurrence(string thing)
	{
		if ( m_allowEnableCancel )
			SV.m_currentInteractionOccurrences.Add(thing);
		return SV.m_occurrences.Add(thing) - 1;
	}

	public bool NextOccurrence { get 
	{
		// Return whether we've reached the correct occurrence, and decrement for the next Option
		return m_nextOption-- == 0;
	} }	
	
	////////////////
	// Shuffle/NextOption stuff

	//! Returns a random int from 0 to max inclusive. items will not repeat until all items have been used, and will not be repeated twice in a row.
	public int Shuffle(int max, string source = null)
	{ 
		if ( source != null )
		{
			ShuffledIndex index = null;
			if ( SV.m_shuffledItems.TryGetValue(source, out index) )
				return index.Next();
			else 
			{ 
				 index = new ShuffledIndex(max+1);
				 SV.m_shuffledItems.Add(source,index);
				 return index;
			}
		}
		return ShuffledIndex.Random(max);
	}
	
	//! Returns a random int from min up to and including the "max". The id will not repeat until all ids have been used, and will not be repeated twice in a row.
	public int Shuffle(int min, int max, string source = null) { return Shuffle(max-min,source)+min; }
	
	public bool FirstOption(int count, string source = null)
	{
		m_nextOption = Shuffle(count-1, source);
		return NextOption;
	}

	public bool NextOption {
	get {
		// Return whether we've reached the correct option, and decrement for the next Option
		return m_nextOption-- == 0;
	} }

	
	/// Helper function that temporarily disables all clickables, except those specified (will probably move to PowerQuest system)
	public void DisableAllClickablesExcept()
	{
		RestoreAllClickables();
		foreach( Prop prop in PowerQuest.Get.GetCurrentRoom().GetProps_SaveFlagNotDirtied() )		
		{
			if ( prop.Clickable )
			{
				SV.m_tempDisabledProps.Add( prop.ScriptName);
				prop.Clickable = false;
				prop.SaveDirty=true;
			}
		}

		foreach( Hotspot hotspot in PowerQuest.Get.GetCurrentRoom().GetHotspots_SaveFlagNotDirtied() )
		{
			if ( hotspot.Clickable )
			{
				SV.m_tempDisabledHotspots.Add(hotspot.ScriptName);
				hotspot.Clickable = false;
				hotspot.SaveDirty = true;
			}
		}
		
	}
	
	/// Helper function that temporarily disables all clickables, except those specified (will probably move to PowerQuest system)
	public void DisableAllClickablesExcept(params string[] exceptions)
	{
		RestoreAllClickables();
		foreach( Prop prop in PowerQuest.Get.GetCurrentRoom().GetProps_SaveFlagNotDirtied() )		
		{
			if ( prop.Clickable && System.Array.Exists(exceptions, item=> string.Equals(prop.ScriptName, item, System.StringComparison.OrdinalIgnoreCase)) == false )
			{
				SV.m_tempDisabledProps.Add( prop.ScriptName);
				prop.Clickable = false;
				prop.SaveDirty=true;

			}
		}

		foreach( Hotspot hotspot in PowerQuest.Get.GetCurrentRoom().GetHotspots_SaveFlagNotDirtied() )
		{
			if ( hotspot.Clickable && System.Array.Exists(exceptions, item => string.Equals(hotspot.ScriptName, item, System.StringComparison.OrdinalIgnoreCase)) == false )
			{
				SV.m_tempDisabledHotspots.Add(hotspot.ScriptName);
				hotspot.Clickable = false;
				hotspot.SaveDirty = true;
			}
		}
	}

	public void DisableAllClickablesExcept(params IQuestClickableInterface[] exceptions)
	{
		RestoreAllClickables();
		foreach( Prop prop in PowerQuest.Get.GetCurrentRoom().GetProps_SaveFlagNotDirtied() )		
		{
			if ( prop.Clickable && System.Array.Exists(exceptions, item=> item==prop) == false )
			{
				SV.m_tempDisabledProps.Add( prop.ScriptName);
				prop.Clickable = false;
				prop.SaveDirty=true;
			}
		}

		foreach( Hotspot hotspot in PowerQuest.Get.GetCurrentRoom().GetHotspots_SaveFlagNotDirtied() )
		{
			if ( hotspot.Clickable && System.Array.Exists(exceptions, item => item==hotspot) == false )
			{
				SV.m_tempDisabledHotspots.Add(hotspot.ScriptName);
				hotspot.Clickable = false;
				hotspot.SaveDirty = true;
			}
		}
	}

	/// Helper function that restores clickables disabled with the DisableAllClickablesExcept function (will probably move to PowerQuest system)
	public void RestoreAllClickables()
	{
		foreach( string name in SV.m_tempDisabledProps )
		{
			Prop prop = PowerQuest.Get.GetCurrentRoom().GetProp(name);
			if ( prop != null )
				prop.Clickable = true;
		}
		SV.m_tempDisabledProps.Clear();

		foreach( string name in SV.m_tempDisabledHotspots )
		{
			Hotspot hotspot = PowerQuest.Get.GetCurrentRoom().GetHotspot(name);
			if ( hotspot != null )
				hotspot.Clickable = true;
		}
		SV.m_tempDisabledHotspots.Clear();
	}


	/// Set all clickables to have a specific cursor temporarily, restore using RestoreAllClickableCursors() (will probably move to PowerQuest system)
	public void SetAllClickableCursors( string cursor, params string[] exceptions)
	{
		foreach( Prop prop in PowerQuest.Get.GetCurrentRoom().GetProps_SaveFlagNotDirtied() )
		{	        
			if ( prop.Clickable && prop.Cursor != cursor && System.Array.Exists(exceptions, item=> item == prop.ScriptName) == false )
			{
				SV.m_tempCursorNoneCursor.Add(prop.Cursor);
				SV.m_tempCursorNoneProps.Add( prop.ScriptName);
				prop.Cursor = cursor;
				prop.SaveDirty=true;
			}
		}

		foreach( Hotspot hotspot in PowerQuest.Get.GetCurrentRoom().GetHotspots_SaveFlagNotDirtied() )
		{
			if ( hotspot.Clickable && hotspot.Cursor != cursor && System.Array.Exists(exceptions, item=> item == hotspot.ScriptName) == false )
			{
				SV.m_tempCursorNoneCursor.Add(hotspot.Cursor);
				SV.m_tempCursorNoneHotspots.Add(hotspot.ScriptName);
				hotspot.Cursor = cursor;
				hotspot.SaveDirty = true;
			}
		}
	}

	/// Resets all clickable cursors (will probably move to PowerQuest system)
	public void RestoreAllClickableCursors()
	{
		int i = 0; 
		foreach( string name in SV.m_tempCursorNoneProps )
		{
			Prop prop = PowerQuest.Get.GetCurrentRoom().GetProp(name);
			if ( prop != null )
				prop.Cursor = SV.m_tempCursorNoneCursor[i];
			++i;
		}
		SV.m_tempCursorNoneProps.Clear();

		foreach( string name in SV.m_tempCursorNoneHotspots )
		{
			Hotspot hotspot = PowerQuest.Get.GetCurrentRoom().GetHotspot(name);
			if ( hotspot != null )
				hotspot.Cursor = SV.m_tempCursorNoneCursor[i];
			++i;
		}
		SV.m_tempCursorNoneHotspots.Clear();

		SV.m_tempCursorNoneCursor.Clear();
	}



	int m_inlineDialogResult = -1;
	DialogTree m_inlineDialogPrevDialog = null;
	public Coroutine WaitForInlineDialog(params string[] options)
	{
		if ( options != null && options.Length >  0 )
			return StartQuestCoroutine(CoroutineWaitForInlineDialog(options));
		else 
			return null;
	}

	IEnumerator CoroutineWaitForInlineDialog(string[] options)
	{
		bool wasCutscene = m_cutscene;
		if ( wasCutscene )
			EndCutscene();

		m_inlineDialogResult = -1;
		m_inlineDialogPrevDialog = m_currentDialog;
		DialogTree dialogTree = new DialogTree();
		int i =  0;
		foreach( string optionText in options )
		{
			DialogOption option = new DialogOption();
			option.InlineId = i;
			option.Text = optionText;
			dialogTree.Options.Add(option);
			i++;
		}

		m_currentDialog = dialogTree;
		GetGui(DialogTreeGui).Visible = true;
		bool hideCursor = GetCursor().HideWhenBlocking;
		GetCursor().HideWhenBlocking = false;
		GetCursor().Visible = true;
		yield return WaitWhile(()=> m_inlineDialogResult < 0 );
		GetCursor().HideWhenBlocking = hideCursor;

		m_currentDialog = m_inlineDialogPrevDialog;

		if (wasCutscene)
			StartCutscene();

		yield return Break;
	}

	public int InlineDialogResult {get { return m_inlineDialogResult; } }


	#endregion
	#region Functions: Implementing IQuestScriptable

	public string GetScriptName() { return "PowerQuest"; }
	public string GetScriptClassName() { return GLOBAL_SCRIPT_NAME; }
	public QuestScript GetScript() { return m_globalScript; }
	public IQuestScriptable GetScriptable() { return this; }
	public void HotLoadScript(Assembly assembly) { QuestUtils.HotSwapScript( ref m_globalScript, GLOBAL_SCRIPT_NAME, assembly ); }
	public void EditorRename(string name) {}



	#endregion
	#region Functions: ISerializationCallbackReceiver

	public bool GetSerializationComplete() { return m_serializeComplete; }

	public void OnBeforeSerialize()
	{
		m_serializeComplete = false;
	}
	public void OnAfterDeserialize()
	{
		m_serializeComplete = true;
	}

	//
	// Save/Load 
	//  Moved to "PowerQuestSave.cs"
	//

	#endregion
	#region Functions: Public System (advanced functions)

	/// Called when dialog is skipped
	public System.Action CallbackOnDialogSkipped = null;
	/// Called when a character collects an inventory item. EG:  OnInventoryCollected(ICharacter character, IInventory item);
	public System.Action<ICharacter, IInventory> CallbackOnInventoryCollected = null;
	// Callback when cutscene's skipped
	public System.Action CallbackOnEndCutscene = null;
	// Callback when cutscene's skipped, CallbackOnProcessClick(bool interactionFound)
	public System.Action<bool> CallbackOnProcessClick = null;
	// Callback called whenver script is set to "Block". May be multiple times in a single frame
	public System.Action CallbackOnBlock = null;
	// Callback called whenver script is set to "Unblock". May be multiple times in a single frame
	public System.Action CallbackOnUnblock = null;

	//  Used by characters to check if should set autoavance flag on dialog
	public bool GetShouldSayTextAutoAdvance() { return m_sayTextAutoAdvance; }

	public bool GetStopWalkingToTalk() { return m_stopWalkingToTalk; }

	// Wait for current dialog 
	public Coroutine WaitForDialog(float time, AudioHandle audioSource, bool autoAdvance, bool skippable, QuestText textComponent = null)	{	return StartCoroutine(CoroutineWaitForDialog(time, skippable, autoAdvance, audioSource, textComponent)); }

	// Hides display box. Access provided incase want to cancel background dialog
	public void CancelDisplayBG()
	{
		EndDisplay();
	}

	public bool GetBlocked() 
	{
		return (m_blocking && m_sequenceIsCancelable == false) || m_transitioning;
	}

	public void InterruptNextLine(float bySeconds)
	{
		m_interruptNextLine = true;
		m_interruptNextLineTime = bySeconds;
	}

	public void ResetInterruptNextLine()
	{
		m_interruptNextLine = false;
	}

	// Used for custom/external controller skipping dialog
	public void SkipDialog(bool useNoSkipTime = true)
	{
		if ( useNoSkipTime == false || (Time.timeSinceLevelLoad-m_timeLastTextShown) > m_textNoSkipTime )
			m_skipDialog = true;	
	}

	// Returns true if a cutscene was skipped
	public bool SkipCutscene()
	{
		// Incase cutscene skipping takes multiple frames, check we're not already skipping
		if ( m_skipCutscene )
			return true;

		if ( m_cutscene )
		{
			SystemAudio.Play("SkipCutscene");
			FadeOutBG(0,"CUTSCENE");

			// Todo: Skip background stuff, like if a WalkToBG is halfway through completing, it needs to be completed.
			foreach ( Character character in m_characters )
			{
				if ( character.Instance != null ) (character.Instance as CharacterComponent).OnSkipCutscene();
			}

			m_skipCutscene = true;
		}

		return m_cutscene;
	}

	public bool GetSkippingCutscene() 
	{
		return m_skipCutscene;
	}

	// Public for character/room script access only.
	public bool HandleSkipDialogKeyPressed()
	{
		// When dialog is skipped, we want the mouse click to be marked as handled. For now just hack the left/right click buttons to do that. Could probably set a flag instead though
		bool result = m_skipDialog;
		m_skipDialog = false;
		m_leftClickPrev = true;
		m_rightClickPrev = true;
		if ( CallbackOnDialogSkipped != null )
			CallbackOnDialogSkipped.Invoke();
		ExHandleSkipDialogKeyPressed();			
		
		return result;		
	}

	public QuestCamera GetCamera() { return m_cameraData; }
	public QuestCursor GetCursor() { return m_cursor; }


	public Pathfinder Pathfinder { get{  return GetCurrentRoom() != null ? GetCurrentRoom().GetInstance().GetPathfinder() : null; } }

	// NB: Potential pitfall, if modify items from this list, they aren't marked as dirty for save system. To flag dirty, use PowerQuest.GetSavable(item);
	public List<Character> GetCharacters_SaveFlagNotDirtied() { return m_characters; }

	public Character GetCharacter(int id) 
	{ 
		List<Character> list = m_characters;
		if ( id >= 0 && id < list.Count )
			return GetSavable(list[id]);
		return null;
	}
	public int GetCharacterId(Character character) { return m_characters.FindIndex(ch=>ch==character); }

	public Inventory GetInventory(int id) 
	{
		List<Inventory> list = m_inventoryItems;
		if ( id >= 0 && id < list.Count )
			return GetSavable(list[id]);
		return null;
	}
		
	// NB: Potential pitfall, modified items in the list aren't marked as dirty, so changes won't be saved automatically. To flag dirty, use item.SaveDirty = true
	public List<DialogTree> GetDialogTrees_SaveFlagNotDirtied() { return m_dialogTrees; }

	public DialogTree GetDialogTree(int id) 
	{
		List<DialogTree> list = m_dialogTrees;
		if ( id >= 0 && id < list.Count )
			return GetSavable(list[id]);
		return null;
	}

	public Gui GetGui(int id) 
	{
		List<Gui> list = m_guis;
		if ( id >= 0 && id < list.Count )
			return list[id];
		return null;
	}
	public QuestText GetDialogTextPrefab() { return m_dialogTextPrefab; }
	public Vector2 GetDialogTextOffset() { return m_dialogTextOffset; }

	public void SetMouseOverDescriptionOverride( string description ) { m_mouseOverDescriptionOverride = description; }
	public void ResetMouseOverDescriptionOverride() { m_mouseOverDescriptionOverride = null; }

	public float GetTextDisplayTime(string text)	{ return Mathf.Max(TEXT_DISPLAY_TIME_MIN, text.Length * TEXT_DISPLAY_TIME_CHARACTER) * Settings.TextSpeedMultiplier; }

	public QuestScript GetGlobalScript() { return m_globalScript; }

	/// Returns true between ChangeRoom starting, and false again just before OnEnterAfterFade is called
	public bool GetRoomLoading() { return m_levelLoadedCalled == false; } 

	// Start the transition to a new room
	public void StartRoomTransition( Room room, bool force = false )
	{

		if ( m_levelLoadedCalled == false )
		{
			Debug.LogError("Attempted to change rooms while already changing rooms!");
			return;
		}

		if ( m_initialised == false ) // HACK-Because room changes can happen when setting up characters initially, and we don't wanna change scene for these
			return;

		m_levelLoadedCalled = false;
		
		// When changing room, need to cancel background interactions, so they can't be set back to the "current" later. This fixes the case where you click a hotspot, player walks towards it, and enters region which changes the room.
		CancelCurrentInteraction();
		m_backgroundSequence = null; // This effectively kills any background interaction so it can't be reset back to current
		
		if ( room != null && (SceneManager.GetActiveScene().name != room.GetSceneName() || force ))
		{	
			// Fade out then change room
			StartCoroutine( CoroutineRoomTransition(room, force) ) ;
		}
	}


	public void StartDialog(string dialogName)
	{
		DialogTree dialog = GetDialogTree(dialogName);		
		if ( dialog == null )
		{
			Debug.LogWarning("Couldn't start Dialog: "+dialogName+", it doesn't exist!");
			return;
		}
		
		dialog.OnStart();
		m_previousDialog = m_currentDialog;
		m_currentDialog = dialog;
		if ( StartScriptInteraction(dialog, SCRIPT_FUNCTION_DIALOG_START, null, false ) == false )
			StartScriptInteraction(dialog, SCRIPT_FUNCTION_DIALOG_START_OLD, null, false );
	}

	public void StopDialog()
	{
		if ( m_currentDialog !=  null )
		{
			StartScriptInteraction(m_currentDialog, SCRIPT_FUNCTION_DIALOG_STOP, null, false );
			m_currentDialog = null;
		}
		GetGui(DialogTreeGui).Visible = false;
	}


	// not currently used
	public void SetDialogOptionSelected(DialogOption option) { m_dialogOptionSelected = option; }

	// Proces a click on a gui dialog item- Returns true if dialog should unpause or hide to allow a sequence to run
	public bool OnDialogOptionClick(DialogOption option)//, PointerEventData.InputButton button)
	{

		if ( m_currentDialog == null )
			return false;

		m_guiConsumedClick = true;

		//if ( button != PointerEventData.InputButton.Left )
		//	return false;
		if ( option == null )
			return false;		
		if ( option.InlineId >= 0 )
		{
			m_inlineDialogResult = option.InlineId;
			option.Used = true;
			GetGui(DialogTreeGui).Visible = false;
			StopDialog();
			return true;
		}
		else 
		{
		
			// Increment 'used' status- will be undone if option isn't handled.
			bool wasUsed = option.Used;
			option.Used=true;
			option.TimesUsed++;

			if ( StartScriptInteraction(m_currentDialog, SCRIPT_FUNCTION_DIALOG_OPTION + option.Name, new object[]{option}, false )
				|| StartScriptInteraction(m_currentDialog, SCRIPT_FUNCTION_DIALOG_OPTION + option.Name, null, false ) )
			{
				GetGui(DialogTreeGui).Visible = false;
				return true;
			}
				
			// Undo Used and TimesUsed- This ensures these properties are consistant no matter where in the script coroutine they're checked.
			option.Used = wasUsed;
			option.TimesUsed--;
		
		}
		return false;		
	}

	// Processes a click on a gui inventory item- returns true if the inventory should unpause or hide to allow a sequence to run
	public bool OnInventoryClick()
	{		
		if ( Paused || GetModalGuiActive() )
		{		
			if ( m_inventoryClickStyle != eInventoryClickStyle.OnMouseClick )
				Debug.LogWarning("InventoryClickStyle should be set to OnMouseClick, other modes are no longer in use");

			// Do some special handling to check for process click
			System.Reflection.MethodInfo method = null;
			if ( m_globalScript != null )
			{
				method = m_globalScript.GetType().GetMethod( "OnMouseClick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				if ( method != null ) 
					method.Invoke(m_globalScript, new object[]{Input.GetMouseButton(0), Input.GetMouseButton(1)});

				// If any script interactions were queued, return 'true' to unpause the inventory
				if ( m_queuedScriptInteractions.Count > 0 )
				{
					m_leftClickPrev = Input.GetMouseButton(0);
					m_rightClickPrev = Input.GetMouseButton(1);
					return true;
				}
			}
		}
		return false;

	}
	public bool OnInventoryClick(string item, PointerEventData.InputButton button)
	{		
		if ( m_inventoryClickStyle == eInventoryClickStyle.OnMouseClick )
		{
			if ( Paused )
			{
				// Do some special handling to check for process click
				System.Reflection.MethodInfo method = null;
				if ( m_globalScript != null )
				{
					method = m_globalScript.GetType().GetMethod( SCRIPT_FUNCTION_ONMOUSECLICK, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					if ( method != null ) 
						method.Invoke(m_globalScript,new object[]{ button == PointerEventData.InputButton.Left,  button == PointerEventData.InputButton.Right });

					// If any script interactions were queued, return 'true' to unpause the inventory
					if ( m_queuedScriptInteractions.Count > 0 )
						return true;
				}
				
			}
			return false;
		}

		bool clickHandled = false;
		bool shouldUnpause = false;
		m_guiConsumedClick = true;

		Inventory inv = GetInventory(item);
		if ( inv == null )
			return false;

		// If clicked item on itself, drop unselect it
		if ( inv == m_player.ActiveInventory )
		{
			m_player.ActiveInventory = null;
			return false;
		}

		if ( button == PointerEventData.InputButton.Left )
		{

			//m_lastClickable = component.GetData() as IQuestClickable;
			if ( m_player.HasActiveInventory )
			{
				// Use inventory item on inventory item

				Inventory activeInv = m_player.ActiveInventory.Data;
				clickHandled = StartScriptInteraction( inv, PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, new object[] {inv, activeInv}, true );
				if ( clickHandled == false )
				{
					// If click wasn't handled, try using inventory item in opposite order, if it works one way it should work the other
					clickHandled = StartScriptInteraction( activeInv, PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, new object[] {activeInv, inv}, true );
				}

				bool hadSpecificInteraction = clickHandled;

				if ( clickHandled  == false )
				{
					// Still not handled, so try "unhandled event" function
					clickHandled = StartScriptInteraction( this, "UnhandledUseInvInv", new object[] {inv, activeInv}, true );
					if ( clickHandled == false )
					{
						// If click wasn't handled, try using inventory item in opposite order, if it works one way it should work the other
						clickHandled = StartScriptInteraction( this, "UnhandledUseInvInv", new object[] {activeInv, inv}, true );
					}
				}

				if ( clickHandled )
				{
					shouldUnpause = true;
					if ( hadSpecificInteraction )
					{
						// Used the inventory item, so clear active inventory (only if no longer has inventory)
						if ( m_player.HasInventory(m_player.ActiveInventory) == false )
							m_player.ActiveInventory = null;
					}
				}
			}
			else if ( m_inventoryClickStyle == eInventoryClickStyle.UseInventory )
			{

				clickHandled = StartScriptInteraction( inv, PowerQuest.SCRIPT_FUNCTION_INTERACT_INVENTORY, new object[] {inv}, true );

				if ( clickHandled  == false )
				{
					// Still not handled, so try "unhandled event" function
					clickHandled = StartScriptInteraction( this, "UnhandledInteractInventory", new object[] {inv}, true );
				}

				if ( clickHandled )
					shouldUnpause = true;
			}
			else 
			{
				// Set active inventory	item
				m_player.ActiveInventory = inv;
				SystemAudio.Play("InventoryCursorSet");
			}

		}
		else if ( button == PointerEventData.InputButton.Right )
		{
			if ( m_player.HasActiveInventory )
			{
				// clear active inventory
				m_player.ActiveInventory = null;
			}
			else 
			{
				// Examine inventory
				clickHandled = StartScriptInteraction( inv, PowerQuest.SCRIPT_FUNCTION_LOOKAT_INVENTORY, new object[] {inv}, true );

				if ( clickHandled == false )
				{
					// Unhandled use inv on inv event						
					clickHandled = StartScriptInteraction( this, "UnhandledLookAtInv", new object[] {inv}, true );
				}

				if ( clickHandled )
				{
					shouldUnpause = true;
				}

			}				
		}

		// Returns true if the inventory needs to unpause to play a sequence
		return shouldUnpause;
	}

	// Returns true if game is in process of being restored from a save file
	public bool GetRestoringGame() { return m_restoring; }

	//
	// Misc utilities
	//

	/// Starts a coroutine, adding it to the list of current cancelable sequences
	public Coroutine StartQuestCoroutine( IEnumerator routine, bool cancelable = false )
	{		
		Coroutine result = StartCoroutine(routine);
		if ( cancelable )
		{
			if ( m_sequenceIsCancelable )
				m_backgroundSequences.Add(result); // currently in cancelable sequence, so put in background
			else
				m_currentSequences.Add(result);
		}
		/* Can't do this or when characters turn to face in BG it breaks skipping... No sure why it was necessary /
		else 
		{
			EndCancelableSection();
		}
		/**/
		return result;
	}

	/*
	/// Testing being able to start blocking routine at any point. 
	public Coroutine StartQuestCoroutineBlocking( IEnumerator routine )
	{
		Coroutine result = StartCoroutine(routine);
		if ( m_sequenceIsCancelable )
		{
			m_backgroundSequences.Add(result); // currently in cancelable sequence, so put in background
		}
		else 
		{
			m_currentSequences.Add(result);
		}

		// This will cause it to block main loop once reached. NB: not sure if this should only happen if "m_sequenceIsCancelable" is false...?
		m_queuedScriptInteractions.Add(result);
		return result;
	}*/
	
	/// Queues a coroutine to start on the next Blocking Update
	public Coroutine QueueCoroutine( IEnumerator routine )
	{
		Coroutine result = StartQuestCoroutine(routine);
		// This will cause it to block main loop once reached. NB: not sure if this should only happen if "m_sequenceIsCancelable" is false...?
		m_queuedScriptInteractions.Add(result);
		return result;
	}

	public bool UseCustomKBShortcuts => m_customKbShortcuts;
	public static bool GetDebugKeyHeld() { return PowerQuest.Get.IsDebugBuild && (Input.GetKey(KeyCode.BackQuote) || Input.GetKey(KeyCode.Backslash)); }

	#endregion
	#region Functions: Public Editor
	//
	// public functions for editor
	//
	public bool GetActionEnabled(eQuestVerb action)
	{
		switch(action)
		{
			case eQuestVerb.Use: return m_enableUse;
			case eQuestVerb.Look: return m_enableLook;
			case eQuestVerb.Inventory: return m_enableInventory;
			case eQuestVerb.Parser: return m_enableParser;
		}
		return true;
	}
	public bool GetSnapToPixel() { return m_snapToPixel; }
	public bool GetPixelCamEnabled() { return m_snapToPixel && m_pixelCamEnabled; }	
	public float SnapAmount {get{ return m_snapToPixel ? 1.0f : 0.0f; }}
	public float EditorGetDefaultPixelsPerUnit() { return m_defaultPixelsPerUnit; }

	public List<RoomComponent> GetRoomPrefabs() { return m_roomPrefabs; }
	public List<CharacterComponent> GetCharacterPrefabs() { return m_characterPrefabs; }
	public List<InventoryComponent> GetInventoryPrefabs() { return m_inventoryPrefabs; }
	public List<DialogTreeComponent> GetDialogTreePrefabs() { return m_dialogTreePrefabs; }
	public List<GuiComponent> GetGuiPrefabs() { return m_guiPrefabs; }
	public List<Gui> GetGuis() { return m_guis; }
	public QuestCursorComponent GetCursorPrefab() { return m_cursorPrefab; }
	public QuestCameraComponent GetCameraPrefab() { return m_cameraPrefab; }
	public QuestText GetDialogTextPrefabEditor() { return m_dialogTextPrefab; }
	public List<AnimationClip> GetInventoryAnimations() { return m_inventoryAnimations; }
	public AnimationClip GetInventoryAnimation(string animName) { return QuestUtils.FindByName(m_inventoryAnimations, animName); }
	public List<Sprite> GetInventorySprites() { return m_inventorySprites; }
	public Sprite GetInventorySprite(string animName) { return FindSpriteInList( m_inventorySprites, animName); }
	public List<AnimationClip> GetGuiAnimations() { return m_guiAnimations; }
	public AnimationClip GetGuiAnimation(string animName) { return QuestUtils.FindByName(m_guiAnimations, animName); }
	public List<Sprite> GetGuiSprites() { return m_guiSprites; }
	
	public Sprite GetGuiSprite(string animName) { return FindSpriteInList(m_guiSprites, animName); }
	
	public Material TextSpriteMaterial => m_textSpriteMaterial;
	public QuestText.TextSpriteData[] TextSprites => m_textSprites;

	// Searches a list of sprites for either the animation name, or the animation name with the post-fix _0. So that searching for "Tree" will still find sprite imported as "Tree_0.png"
	public static Sprite FindSpriteInList(List<Sprite> list, string animName)
	{
		if ( list == null )
			return null;
		string animName_0 = animName+PowerQuest.SPRITE_NUM_POSTFIX_0; // search for sprites with _0 on the end too- so when using importer to import single frame sprites it only imports the one.
		return list.Find( item=>item != null 
			&& (string.Equals(animName, item.name, System.StringComparison.OrdinalIgnoreCase)
				|| string.Equals(animName_0, item.name, System.StringComparison.OrdinalIgnoreCase)) );  
	}
	
	// NB: Potential pitfall, if modify items from this list, they aren't marked as dirty for save system. To flag dirty, use PowerQuest.GetSavable(item);	
	public List<IQuestScriptable> GetAllScriptables()
	{
		List<IQuestScriptable> scriptables = new List<IQuestScriptable>();
		scriptables.Add(this as IQuestScriptable);
		m_characters.ForEach( item => scriptables.Add( item as IQuestScriptable ) );
		m_rooms.ForEach( item => scriptables.Add( item as IQuestScriptable ) );
		m_dialogTrees.ForEach( item => scriptables.Add( item as IQuestScriptable ) );
		m_inventoryItems.ForEach( item => scriptables.Add( item as IQuestScriptable ) );
		m_guis.ForEach( item => scriptables.Add( item as IQuestScriptable ) );
		return scriptables;
	}


	/// Returns true if there are any modal guis active (ie: ones that take control from game
	public bool GetModalGuiActive() 
	{ 
		return m_guis.Exists( item=> item.Modal && item.Visible ) || m_guiConsumedClick; 
	}
	public Gui GetTopModalGui()
	{
		// Finds the topmost modal gui		
		Gui result = null;
		foreach( Gui item in m_guis )
		{
			if ( item.Modal && item.Visible && (result == null || item.Baseline < result.Baseline) )
				result = item;
		}
		return result;
	}

	/// Use when you want something else to capture input so it won't be handled in-game (eg: when mouse is over a GUI)
	public void CaptureInputOn(string source)
	{
		SV.m_captureInputSources.Add(source);
		Gui gui = GetGuis().Find( item=>item.ScriptName == "Source");
		if (gui != null )
			m_mouseOverClickable = gui;
	}
	/// Use when you want something else to capture input so it won't be handled in-game (eg: when mouse is over a GUI)
	public void CaptureInputOff(string source)
	{
		SV.m_captureInputSources.RemoveAll(item=>string.Equals(item,source,System.StringComparison.OrdinalIgnoreCase));
	}

	public int EditorGetVersion() { return m_version; }
	public void EditorSetVersion(int version) { m_version = version; }
	public int EditorNewVersion { get { return m_newVersion; } set { m_newVersion = value; } }

	public IQuestScriptable EditorGetAutoLoadScriptable() { return m_autoLoadScriptable; }
	//public List<IQuestScriptable> EditorGetHotloadedScriptables() { return m_hotLoadedScriptables; } // For now just recompiling all scriptables
	public void EditorSetHotLoadAssembly(Assembly assembly/*, List<IQuestScriptable> hotLoadedScriptables*/) 
	{ 
		m_hotLoadAssembly = assembly;
		// m_hotLoadedScriptables = hotLoadedScriptables;
	}
	public Assembly EditorGetHotLoadAssembly() { return m_hotLoadAssembly; }

	public string EditorGetAutoLoadFunction() { return m_autoLoadFunction; }


	#endregion
	#region Functions: Monobehaviour Functions

	//
	// Private internal functions
	//

	public bool GetRegainedFocus() 
	{ 
		bool result = m_lostFocus && m_hasFocus;
		if ( result )
			m_lostFocus = false;
		return result; 
	}
	public bool GetLostFocus() { return m_lostFocus; }
	public void SetLostFocus(bool value) { m_lostFocus = value; }
	bool m_lostFocus = false;
	bool m_hasFocus = false;
	void OnApplicationFocus(bool hasfocus)
	{
		m_hasFocus = hasfocus;
		if  ( hasfocus )
		{
			if ( Application.isEditor == false )
			{			
				UnityEngine.Cursor.visible = false;
				//UnityEngine.Cursor.lockState = CursorLockMode.Confined; // I wonder if this is necessary
			}
			else 
			{
				//UnityEngine.Cursor.lockState = CursorLockMode.None;
			}
		}
		else 
		{
			m_lostFocus = true;
		}
	}

	void OnEnable()
	{
		SpriteAtlasManager.atlasRequested += RequestAtlas;
	}

	void OnDisable()
	{
		SpriteAtlasManager.atlasRequested -= RequestAtlas;
	}

	// Made this static, since even when resstarting PQ, the atlas
	public static Dictionary<string, System.Action<SpriteAtlas>> s_roomAtlasCallbacks = new Dictionary<string, System.Action<SpriteAtlas>>();
	
	void RequestAtlas(string tag, System.Action<SpriteAtlas> callback)
	{				
		if (s_roomAtlasCallbacks.ContainsKey(tag) )
			s_roomAtlasCallbacks[tag] += callback;
		else
			s_roomAtlasCallbacks.Add(tag,callback); 
		
		if ( m_currentRoom != null && tag.Equals($"Room{m_currentRoom.ScriptName}Atlas") )
			LoadAtlas(m_currentRoom.ScriptName);
	}

	void Awake()
	{
		// Ensure there's only 1 PowerQuest
		if ( HasInstance() )
		{
			Destroy(gameObject);
			return;	
		}	

		SetSingleton();
		DontDestroyOnLoad(this);

		if ( LAYER_UI < 0 )
			LAYER_UI = LayerMask.NameToLayer("UI");

		// init menu manager
		m_menuManager.Awake();

		//
		// Ensure there's no null quest objects lingering in lists. This may be due to unity doing wierd things with the list, or them being deleted incorrectly. 
		//
		m_characterPrefabs.RemoveAll(item=>item==null);
		m_roomPrefabs.RemoveAll(item=>item==null);
		m_dialogTreePrefabs.RemoveAll(item=>item==null);
		m_guiPrefabs.RemoveAll(item=>item==null);
		m_inventoryPrefabs.RemoveAll(item=>item==null);


		// Check for existing SystemAudio and remove it if found. Since it'll be one left over from previewing in the editor
		if ( SystemAudio.HasInstance() )
		{
			Destroy(SystemAudio.Get.gameObject);
		}

		//
		// Instantiate other systems (audio, time, etc)
		//
		foreach( Component obj in m_systems )
		{
			Transform trans = (GameObject.Instantiate(obj.gameObject) as GameObject).transform;
			trans.name = obj.name;
			trans.parent = transform;
		}

		// Set language
		if ( string.IsNullOrEmpty(Settings.Language) == false )
			Settings.Language = Settings.Language;


		//
		// initialise Data classes ( rooms, characters, etc.
		//		- Copies the default data from their prefabs into permanent data that's kept between scenes.
		//		- Instantiates "Scripts" classes
		//

		// Create the game script
		m_globalScript = QuestUtils.ConstructByName<QuestScript>(GLOBAL_SCRIPT_NAME);
		Debug.Assert( m_globalScript != null, "Couldn't find global script! Add GlobalScript.cs to the Game folder" );
			

		// Initialise Cursor
		if ( m_cursorPrefab != null )
		{
			m_cursor = new QuestCursor();
			QuestUtils.CopyFields(m_cursor, m_cursorPrefab.GetData());
			m_cursor.Initialise( m_cursorPrefab.gameObject );
			
			UnityEngine.Cursor.visible = false;
			if ( Application.isEditor == false )
				UnityEngine.Cursor.lockState = CursorLockMode.Confined;
		}

		// Initialise Camera
		if ( m_cameraPrefab != null )
		{
			QuestUtils.CopyFields(m_cameraData,m_cameraPrefab.GetData());
		}

		// Initialise quest objects (which can be-reset before restoring a game)
		InitialiseQuestObjects();		


		// Set first character in list as default player character
		m_player = m_characters[0];

		// Initialise Guis
		foreach (GuiComponent prefab in m_guiPrefabs) 
		{
			Gui data = new Gui();
			QuestUtils.CopyFields(data, prefab.GetData());
			m_guis.Add(data);
			data.Initialise( prefab.gameObject );
		}

	}

	// Clears and initialises quest objects from initial prefab state. Done on game start and again on Restore
	void InitialiseQuestObjects()
	{	
		// Initialise Rooms
		m_rooms.Clear();
		foreach (RoomComponent prefab in m_roomPrefabs) 
		{
			Room data = new Room();
			QuestUtils.CopyFields(data, prefab.GetData());
			m_rooms.Add(data);
			data.Initialise( prefab.gameObject );
		}

		// Initialise inventory
		m_inventoryItems.Clear();
		foreach (InventoryComponent prefab in m_inventoryPrefabs) 
		{
			Inventory data = new Inventory();
			QuestUtils.CopyFields(data, prefab.GetData());
			m_inventoryItems.Add(data);
			data.Initialise( prefab.gameObject );
		}

		// Initialise Characters
		m_characters.Clear();
		foreach (CharacterComponent prefab in m_characterPrefabs) 
		{
			Character data = new Character();
			QuestUtils.CopyFields(data, prefab.GetData());
			m_characters.Add(data);
			data.Initialise( prefab.gameObject );
		}

		// Initialise Dialogs
		m_dialogTrees.Clear();
		foreach (DialogTreeComponent prefab in m_dialogTreePrefabs) 
		{
			DialogTree data = new DialogTree();
			QuestUtils.CopyFields(data, prefab.GetData());
			m_dialogTrees.Add(data);
			data.Initialise( prefab.gameObject );
		}
	}

	// Use this for initialization
	void Start() 
	{		
		//
		// Init settings
		//
		RestoreSettings();
		m_settings.OnInitialise();

		// Test hack for save/restoring menu manager.. wasn't working though, maybe try again later
		//m_saveManager.AddSaveData("MenuMan",m_menuManager);
		AddSaveData("BgSeq", m_bgSequenceData, OnPostRestoreBgSequence);

		ExOnGameStart();
		ExtentionOnGameStart(); // For back compatability

		OnSceneLoaded();

		// Register for OnSceneLoaded AFTER loading the scene first time (So we don't get things out of order. We want to call OnGameStart first)
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnSceneLoaded( Scene scene, LoadSceneMode loadSceneMode ) { OnSceneLoaded(); }

	void OnSceneLoaded()
	{
		if ( m_levelLoadedCalled == false )
		{			
			m_transitioning = true;
			m_levelLoadedCalled = true;
			string sceneName = SceneManager.GetActiveScene().name;
			FadeOutBG(0,"RoomChange");		
				
			// Debug Restart (pressing ~+f9)- hotload scripts again
			if ( Application.isEditor && s_restartAssembly != null )
			{			
				m_hotLoadAssembly = s_restartAssembly;
				s_restartAssembly = null;
				List<IQuestScriptable> scriptables = GetAllScriptables();
				foreach ( IQuestScriptable scriptable in scriptables )
				{
					scriptable.HotLoadScript(m_hotLoadAssembly);
				}
			}

			m_coroutineMainLoop = StartCoroutine(LoadRoomSequence(sceneName));

		}
	}

	#endregion
	#region Functions: Monobehaviour Update

	// Update is called once per frame
	void Update() 
	{
		UpdateCameraLetterboxing();

		// When restoring a save game, update the fade-in & regions, but nopthing else until room is loaded.
		if ( m_restoring )
		{
			m_menuManager.Update();
			return;
		}	

		// Call partial update fucntion for extentions 
		ExUpdate();

		//
		// Update region collision (tinting, etc)
		//
		UpdateRegions();
					

		// Pathfinder.DrawDebugLines();

		//
		// Update guis visiblity
		//
		UpdateGuiVisibility();


		//
		// Update input
		//
		
		UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
		if ( m_overrideMousePos == false )
		{
			m_mousePos = Vector2.zero;

			if ( mainCamera != null )
			{				
				m_mousePos = mainCamera.ScreenToWorldPoint( Input.mousePosition.WithZ(0) );
			}
		}
		if ( mainCamera != null )
			m_mousePosGui = m_cameraGui.ScreenToWorldPoint(mainCamera.WorldToScreenPoint(m_mousePos));

		UpdateGuiFocus();

		UpdateDebugKeys();
				
		// In editor, set hardware cursor visible when outside the game view			
		if ( Application.isEditor && mainCamera != null)
		{	
			Vector2 mousePos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
			UnityEngine.Cursor.visible = mousePos.x < 0.0f || mousePos.x > 1.0f || mousePos.y < 0.0f || mousePos.y > 1.0f || Cursor.Visible == false;
		}

		if ( m_customKbShortcuts == false )
		{
			if ( Input.GetMouseButtonDown(0) )
				SkipDialog(true); // Skip dialog with click if it's been up for long enough
			else if ( Input.GetMouseButtonDown(1) || (GameHasKeyboardFocus && Input.GetKeyDown(KeyCode.Space)) )
				SkipDialog(false); // Alternate skip buttons don't have the delay built in
			else if (GameHasKeyboardFocus && Input.GetKey(KeyCode.Escape) && m_skipCutsceneButtonConsumed == false)
				SkipDialog(false); // Skip dialog while esc's held, as long as it wasn't consumed by the skip cutscene

			if ( GameHasKeyboardFocus == false || Input.GetKey(KeyCode.Escape) == false )
				m_skipCutsceneButtonConsumed = false;
		}
		
			
		if ( m_globalScript != null )
		{
			System.Reflection.MethodInfo method = m_globalScript.GetType().GetMethod( FUNC_UPDATE_NOPAUSE, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			if ( method != null ) method.Invoke(m_globalScript,null);
		}

		if ( SystemTime.Paused == false )
		{

			// Update timers
			for (int i = 0; i < SV.m_timers.Count; i++)
			{
				SV.m_timers[i].t -= Time.deltaTime;
			}

			if ( m_customKbShortcuts == false && m_cutscene && GameHasKeyboardFocus && Input.GetKeyDown(KeyCode.Escape) )
			{
				SkipCutscene();
				m_skipCutsceneButtonConsumed = true;
			}			
			
			if ( m_roomLoopStarted || m_transitioning == false ) // Don't call update between the RoomChange and OnEnter (so only if the loops started, or no longer transitioning)
			{

				//
				// Game Update (non blocking)
				// 
				if ( m_globalScript != null )
				{
					System.Reflection.MethodInfo method = m_globalScript.GetType().GetMethod( FUNC_UPDATE, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					if ( method != null ) method.Invoke(m_globalScript,null);
				}

				//
				// Room Update (non blocking)
				//
				if ( m_currentRoom != null && m_currentRoom.GetScript() != null )
				{
					System.Reflection.MethodInfo method = m_currentRoom.GetScript().GetType().GetMethod( FUNC_UPDATE, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					if ( method != null )
					{							
						method.Invoke(m_currentRoom.GetScript(),null);
					}
				}
			}
		}
		 
		{
		
			//
			// Gui Update (non blocking)
			//
			foreach ( Gui gui in m_guis )
			{
				if ( gui.Instance != null && gui.Instance.isActiveAndEnabled && gui.GetScript() != null )
				{
					System.Reflection.MethodInfo method = gui.GetScript().GetType().GetMethod( FUNC_UPDATE, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
					if ( method != null )
					{							
						method.Invoke(gui.GetScript(),null);
					}						
				}
			}

		}

		//
		// Update gui
		//
		m_menuManager.Update();
		
	}

	void LateUpdate()
	{
		if ( m_restartOnUpdate )
		{
			StopAllCoroutines();
			m_restartOnUpdate = false;
			
			SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
			SceneManager.MoveGameObjectToScene(m_cameraGui.gameObject, SceneManager.GetActiveScene());			
			if ( string.IsNullOrEmpty(s_restartScene) == false )
			{				
				SceneManager.LoadScene(s_restartScene);
				s_restartScene = null;
			}
			else 			
				SceneManager.LoadScene(0); 
		}
	}


	void UpdateDebugKeys()
	{
		if ( m_customKbShortcuts )
			return;
		if ( Input.GetKey(KeyCode.LeftControl) == false && Input.GetKeyDown(KeyCode.F5) && GetBlocked() == false )
		{
			// Save slot 1
			Save(1, "QuickSave");
		}
		// Quickload	
		if (  Input.GetKeyDown(KeyCode.F7) && GetBlocked() == false  )	
			RestoreSave(1);

		if ( Input.GetKeyDown(KeyCode.F9) && GetBlocked() == false )
		{
			// Restart
			if ( GetDebugKeyHeld() ) // Holding ~ + F9 sets flag to restart at current room
				Restart( m_currentRoom, m_currentRoom.Instance.m_debugStartFunction );
			else
				Restart();
		}

		if ( GetDebugKeyHeld() )
		{
			// debug cheat keys
			if ( Input.GetKeyDown(KeyCode.F10) && GetBlocked() == false  )
			{
				// Print slot info
				string dbgstr = "";
				foreach ( QuestSaveSlotData data in m_saveManager.GetSaveSlotData())
				{				
					dbgstr += string.Format("{0}: {1}\n", data.m_slotId, data.m_description);
				}
				Debug.Log(dbgstr);
			}
			if ( Input.GetKeyDown(KeyCode.I) ) // inventory test
			{
				foreach (Inventory item in m_inventoryItems)
				{
					if ( m_player.HasInventory(item) == false )
						m_player.AddInventory(item);
				}
			}
		}
	}
	
	
	#endregion
	#region Functions: Partial for extending functionality

	// Partial function to implement for a hook on Start, just before "OnGameStart" is called in global script
	partial void ExOnGameStart();
	// Partial function to implement for a hook on Update
	partial void ExUpdate();
	// Partial function to implement for a hook in the main blocking loop. After queued sequences are resumed, but before interactions are processed, etc.
	partial void ExOnMainLoop();		
	partial void ExHandleSkipDialogKeyPressed();
	partial void ExBlock();
	partial void ExUnblock();	
	partial void ExProcessClick(eQuestVerb verb, IQuestClickable clickable, Vector2 mousePosition, bool interactionFound);
	partial void ExOnEndCutscene();
	partial void ExOnRoomLoad(); // room scene loaded, before first Update and any script functions are called
	partial void ExOnPostRestore(int version); // When post restore called

	partial void ExOnCharacterEnterRegion(Character character, RegionComponent region);
	partial void ExOnCharacterExitRegion(Character character, RegionComponent region);
	
	partial void ExtentionOnGameStart(); // back compatability
	partial void ExtentionOnMainLoop(); // back compatability
	
	#endregion
	#region Functions: Private functions

	void Block()
	{
		m_blocking = true;
		CallbackOnBlock?.Invoke();
		ExBlock();
	}

	void Unblock()
	{
		m_blocking = false;		
		CallbackOnUnblock?.Invoke();
		ExUnblock();
	}

	// Show display box
	void StartDisplay(string text, int id, out QuestText textComponent)
	{
		textComponent=null;
		Gui gui = GetGui(DisplayBoxGui);
		if ( gui == null || gui.GetInstance() == null )
			return;

		m_displayActive = true;
		// Get tranlated string
		text = SystemText.GetDisplayText(text, id, PowerQuest.STR_NARR);

		// Start audio
		SystemAudio.Stop(m_dialogAudioSource);
		m_dialogAudioSource = null;
		if ( Settings.DialogDisplay != QuestSettings.eDialogDisplay.TextOnly )
			m_dialogAudioSource = SystemText.PlayAudio(id, PowerQuest.STR_NARR, null, SystemAudio.Get.NarratorMixerGroup);
		
		if ( Settings.DialogDisplay != QuestSettings.eDialogDisplay.SpeechOnly || PowerQuest.Get.AlwaysShowDisplayText )
		{
			QuestText guiText = gui.GetInstance().GetComponentInChildren<QuestText>(true);
			textComponent=guiText;			
			if ( guiText != null )
			{
				guiText.SetText(text);
				gui.Visible = true;
			}
		}
	}

	// hide display box
	void EndDisplay()
	{
		SystemAudio.Stop(m_dialogAudioSource);

		Gui gui = GetGui(DisplayBoxGui);
		if ( gui == null || gui.GetInstance() == null )
			return;
		gui.Visible = false;
		m_displayActive = false;
	}
	
	// Adjusts the camera viewport to letterbox for unsupported aspect ratios
	public void UpdateCameraLetterboxing()
	{
		if ( Camera.GetInstance() == null )
			return;

		RectCentered rect = new RectCentered(Camera.GetInstance().Camera.rect);
		float resY = DefaultVerticalResolution;
		
		float currWidth = (((float)Screen.width/(float)Screen.height) * resY);		
		float newWidth = Mathf.Clamp(currWidth, HorizontalResolution.Min, HorizontalResolution.Max );

		//Debug.Log($"Screen: {Screen.width},{Screen.height}. Res: {currWidth},{resY}");
		if ( newWidth < currWidth   )
		{
			// Letterbox
			rect.Width = newWidth/currWidth;
			rect.Height = 1;
		}
		else if ( newWidth > currWidth ) 
		{
			// Pillarbox
			rect.Width = 1;
			rect.Height = currWidth/newWidth;
		}
		else
		{
			// Neither
			rect.Width = 1;
			rect.Height = 1;
		}
		Camera.GetInstance().Camera.rect = rect;
		GetCameraGui().rect = rect;
	}

	void UpdateRegions()
	{
		// not using collision system, just looping over characters in the room and triggers in the room

		// Very slightly faster doing it here than in each region's update function, since don't have to re-check each character's room, etc.
		List<RegionComponent> regionComponents = m_currentRoom.GetInstance().GetRegionComponents();
		int regionCount = regionComponents.Count;
		
		// Start by setting all region character flags false
		for ( int regionId = 0; regionId < regionCount; ++regionId )
		{
			RegionComponent regionComponent = regionComponents[regionId];
			regionComponent.GetData().GetCharacterOnRegionMask().SetAll(false);
		}

		for ( int charId = 0; charId < m_characters.Count; ++charId )
		{
			Character character = m_characters[charId];
			bool characterActive = character.Enabled && (character.Room == m_currentRoom || character.IsPlayer); // NB: player's .Room will not be current room during fadeout on change room.
			if ( characterActive ) // Only process characters that are Enabled and active in the current room
			{			
				Vector2 characterPos = character.Position;
				Color tint = new Color(1,1,1,0);
				float scale = 1;

				for ( int regionId = 0; regionId < regionCount; ++regionId )
				{
					RegionComponent regionComponent = regionComponents[regionId];
					Region region = regionComponent.GetData();
					if ( regionComponent.UpdateCharactersOnRegion(charId, characterActive, characterPos) )
					{	
						if ( character.UseRegionScaling )
						{
							float tmpScale = regionComponent.GetScaleAt(characterPos);
							if ( tmpScale != 1 )
								scale = tmpScale;
						}						
						if ( character.UseRegionTinting )
						{
							if ( region.Tint.a > 0 )
							{
								float ratio = regionComponent.GetFadeRatio(characterPos);
								if ( tint.a <= 0 )
								{
									tint = region.Tint;
									tint.a *= ratio;
								}
								else 
								{
									Color newCol = region.Tint;
									tint = Color.Lerp(tint, newCol, ratio);
								}
							}
						}
					}
					
					// Call Enter/Exit region background functions
					RegionComponent.eTriggerResult result = regionComponent.UpdateCharacterOnRegionState(charId, true);					
					if ( region.Enabled && m_currentRoom != null && m_currentRoom.GetScript() != null && (region.PlayerOnly == false || character == m_player) )
					{
						if ( result == RegionComponent.eTriggerResult.Enter )
						{		
							System.Reflection.MethodInfo method = m_currentRoom.GetScript().GetType().GetMethod( SCRIPT_FUNCTION_ENTER_REGION_BG + region.ScriptName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
							if ( method != null ) method.Invoke(m_currentRoom.GetScript(),new object[] {region, character});
						}
						else if ( result == RegionComponent.eTriggerResult.Exit )
						{
							System.Reflection.MethodInfo method = m_currentRoom.GetScript().GetType().GetMethod(SCRIPT_FUNCTION_EXIT_REGION_BG + region.ScriptName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
							if ( method != null ) method.Invoke(m_currentRoom.GetScript(),new object[] {region, character});
						}
					}

				}

				if ( /*characterActive &&*/ character.GetInstance() != null )
				{
					CharacterComponent charComponent = character.GetInstance().GetComponent<CharacterComponent>();
				
					// Apply scale to character
					charComponent.transform.localScale = new Vector3(scale*Mathf.Sign(charComponent.transform.localScale.x), scale, scale);
						
					// Apply tint to character
					//PowerSprite spriteComponent = character.GetInstance().GetComponent<PowerSprite>();
					PowerSprite[] spriteComponents = character.GetInstance().GetComponentsInChildren<PowerSprite>(true);
					foreach ( PowerSprite spriteComponent in spriteComponents )						
					{
						if ( spriteComponent != null )
							spriteComponent.Tint = tint;
					}
				}	

			}
		}		
	}

	// Called when a cutscene ends (either by skipping or when it naturally ends
	void OnEndCutscene()
	{
		// Snap the camera (only if we actually skipped a cutscene, and it didn't just naturally end)
		if ( m_cutscene && m_skipCutscene )
		{
			if ( m_cameraData.GetInstance() != null )
				m_cameraData.GetInstance().Snap();

			if ( CallbackOnEndCutscene != null )
				CallbackOnEndCutscene.Invoke();
			ExOnEndCutscene();
			
			m_menuManager.FadeSkip();


			// "Flash" to fade in (if definitely in the 
			FadeInBG(0.15f, "CUTSCENE");

		}
		m_cutscene = false;
		m_skipCutscene = false;
	}

	public void OnPlayerWalkComplete() { EndCancelableSection(); }
	
	public void EndCancelableSection()
	{
		if ( m_sequenceIsCancelable && m_backgroundSequence != null )
		{
			// swap background sequenct to be the current sequence
			m_currentSequence = m_backgroundSequence;
			m_backgroundSequence = null;
		}
		m_sequenceIsCancelable = false; 	
	}

	public void OnSay()
	{
		// any character talking stops the "Display" dialog
		if ( m_coroutineSay != null )
		{
			StopCoroutine(m_coroutineSay);
			EndDisplay();
		}
	}

	// NB: When this is called, any routine waiting on the dialog will hang and wait forever. But can use with SayBG if not yielding to it
	public void CancelSay()
	{
		// any character talking stops the "Display" dialog
		if ( m_coroutineSay != null )
		{
			StopCoroutine(m_coroutineSay);
			EndDisplay();
		}
		foreach ( Character character in m_characters )
		{
			character.CancelSay();
		}
	}

	IQuestClickable GetObjectAt( Vector2 pos, int layerMask, out GameObject pickedGameObject )
	{
		IQuestClickable result = null;

		int overlapCount = Physics2D.OverlapPointNonAlloc(pos, m_tempPicked, layerMask);
		pickedGameObject = null;

		if ( m_tempPicked != null && overlapCount > 0 )
		{
			float lowestBaseline = float.MaxValue;
			for ( int i = 0; i < overlapCount; ++i )
			{
				IQuestClickable clickable = null;
				bool baselineFixed = false;
				GameObject pickedObj = m_tempPicked[i].gameObject;
				{
					GuiDialogOption component = pickedObj.GetComponent<GuiDialogOption>();
					if ( component != null ) clickable = component.Clickable;
				}
				if ( clickable == null && m_inventoryClickStyle == eInventoryClickStyle.OnMouseClick)
				{
					InventoryComponent component = pickedObj.GetComponent<InventoryComponent>();					
					if ( component != null ) clickable = component.GetData() as IQuestClickable;
				}
				if ( clickable == null )
				{
					GuiControl component = pickedObj.GetComponent<GuiControl>();
					if ( component != null ) clickable = component as IQuestClickable;
				}
				if ( clickable == null )
				{
					GuiComponent component = pickedObj.GetComponent<GuiComponent>();
					if ( component != null ) clickable = component.GetData() as IQuestClickable;					
				}
				if ( clickable == null )
				{
					HotspotComponent component = pickedObj.GetComponent<HotspotComponent>();
					if ( component != null ) clickable = component.GetData() as IQuestClickable;
				}
				if ( clickable == null )
				{
					PropComponent component = pickedObj.GetComponent<PropComponent>();
					if ( component != null ) 
					{
						clickable = component.GetData() as IQuestClickable;
						baselineFixed = component.GetData().BaselineFixed;
					}
				}
				if ( clickable == null )
				{
					CharacterComponent component = pickedObj.GetComponent<CharacterComponent>();
					if ( component != null ) clickable = component.GetData() as IQuestClickable;
				}
				if ( clickable != null )
				{
					if ( clickable.Clickable )
					{
						// Default baseline is the clickable baseline + the vertical offset of the object
						float baseline = clickable.Baseline;
						if ( baselineFixed == false )
							baseline += pickedObj.transform.position.y;

						if ( clickable.ClickableType == eQuestClickableType.Gui || clickable.ClickableType == eQuestClickableType.Inventory )
						{
							if ( pickedObj.GetComponent<GuiComponent>() != null )
							{
								// Gui baseline doesn't have vertical offset added
								baseline = clickable.Baseline;							
							}
							else 
							{
								// Control baseline uses it's gui baseline, with an offset
								GuiComponent gui = pickedObj.GetComponentInParent<GuiComponent>();
								if ( gui != null )
								{
									baseline = gui.GetData().Baseline - 0.5f + (clickable.Baseline/1000.0f); // Add gui baseline, with offset.
								}
							}
						}

						if ( baseline < lowestBaseline )
						{
							pickedGameObject = pickedObj; // this is solely for inventory items, since they're kidna hacky. Should really just be gui elements... I dunno ha ha
							lowestBaseline = baseline;
							result = clickable;
						}
					}
				}
			}
		}
		return result;
	}


	// Turns on sequence cancelling, but doesn't restart main loop
	void EnableCancelInternal()
	{
		if ( m_allowEnableCancel )
		{
			m_sequenceIsCancelable = true;

			// Swap coroutines, set backgroundCoroutine flag so future coroutines go into the BG one
			m_backgroundSequences.Clear();
			m_backgroundSequences = m_currentSequences;
			m_currentSequences = new List<Coroutine>();

			// Set current sequence to background sequence
			m_backgroundSequence = m_currentSequence;

			// Return empty coroutine so main loop doesn't block
			m_currentSequence = StartCoroutine(CoroutineEmpty());
		}

	}

	// Used by clickables to check if usecount should show as incremented yet (makes more sense for it to be 0 until interaction is finished)
	public bool GetInteractionInProgress( IQuestClickable clickable, eQuestVerb verb )
	{		
		// Here we need to be careful, since interaction could have started, but not actually be blocking yet. (Say for 'prop.FirstUse' is used right at start, or after WalkToClicked)
		if ( clickable == null || (m_backgroundSequence == null && m_blocking == false && m_currentSequence == null ) )
			return false;

		// note: it's a list to cope with interactions that call 'HandleInteract()'
		for ( int i = 0; i < m_currentInteractionClickables.Count; ++i ) 
		{
			if ( m_currentInteractionClickables[i] == clickable && m_currentInteractionVerbs[i] == verb )
				return true;
		}
		return false;
	}
	
	// Called when player interaction starts (ie: player clicked something)
	void OnInteraction( IQuestClickable clickable, eQuestVerb verb )
	{
		// Cancel the current interaction before starting new one
		CancelCurrentInteraction();

		// Clear the list of things the current interaction has changed
		SV.m_currentInteractionOccurrences.Clear();
		m_currentInteractionClickables.Clear();
		m_currentInteractionVerbs.Clear();

		if ( clickable != null )
		{

			// Add this clickable & verb so we can roll back 'use count' later
			m_currentInteractionClickables.Add(clickable);
			m_currentInteractionVerbs.Add(verb);	
		
			// call 'clickable.OnInteraction' which increments the usecount
			clickable.OnInteraction(verb);
		}
	}

	// Called when HandleInteract/HandleLookAt is called (simulating player clicking something from another script)
	void OnHandleInteraction( IQuestClickable clickable, eQuestVerb verb )
	{
		// When HandleInteract is used, we want to increment use count for what it was done, without 'cancelling' the interaction that called HandleInteract
		m_currentInteractionClickables.Add(clickable);
		m_currentInteractionVerbs.Add(verb);

		// call 'clickable.OnInteraction' which increments the usecount
		clickable.OnInteraction(verb);
	}

	// Starts a MAIN script interaction, setting it to be the "current sequence" if there was a script to start, and enabling canceling if started with a walk
	// DON'T use for interactions triggered from other interactions (eg. not used for "onEnterRoom" since ChangeRoom could be in a sequence itself). If you do that it will mess up "canceling" and think you want to "queue" the interaction
	bool StartScriptInteraction( IQuestScriptable scriptable, string methodName, object[] parameters = null, bool stopPlayerMoving = false, bool cancelCurrentInteraction = false )
	{	
		QuestScript scriptClass = scriptable.GetScript();

		if ( stopPlayerMoving )
			CancelCurrentInteraction(); // Cancel if will stop player moving
		
		
		m_allowEnableCancel = true;// (regionHack == false); // always start with enableCancel on, unless it's a region, region coroutines can't be cancelled

		// Start the coroutine
		Coroutine result = null;
		try
		{
			m_ignoreAutoLoadFuncName = methodName;
			result = StartScriptInteractionCoroutine(scriptClass, methodName, parameters, stopPlayerMoving, cancelCurrentInteraction);
			if ( result != null && result != m_consumedInteraction)
			{
				// Mark object as dirty
				if ( scriptable is IQuestSaveCachable )
					(scriptable as IQuestSaveCachable).SaveDirty=true;

				if ( m_currentSequence == null )
				{					
					// no sequence running, so set this as the current one
					m_currentSequence = result;

					// If first yield is walk, enable cancel. NOTE: This can cause issues if plr is already walking when script is started
					if ( m_player.Walking && stopPlayerMoving && m_currentDialog == null && cancelCurrentInteraction == false )
					{
						EnableCancelInternal();
					}
				}
				else 
				{				
					// When trying to start a sequence while another is already running, we queue it. This just happens with events like "StopDialog" that don't yield, but can trigger other yielding events
					m_queuedScriptInteractions.Add(result);
				}
			}

			// update the script to auto-load (ignored when not in editor)
			SetAutoLoadScript( scriptable, methodName, result != null, false );
		}
		catch
		{
			
		}
		return result != null;
	}

	// Starts a script interaction, returning the coroutine
	Coroutine StartScriptInteractionCoroutine( QuestScript scriptClass, string methodName, object[] parameters = null, bool stopPlayerMoving = false, bool cancelCurrentInteraction = false )
	{
		Coroutine result = null;
		if ( scriptClass != null )
		{
			System.Reflection.MethodInfo method = scriptClass.GetType().GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			if ( method != null && ((parameters == null && method.GetParameters().Length == 0) || (method.GetParameters().Length == parameters.Length)) )
			{	
				if ( stopPlayerMoving )
					m_player.StopWalking();	

				// Start sequence
				//	m_ignoreAutoLoadFuncName = methodName; // moved this to StartScriptInteraction, so handleInteracts still load
				IEnumerator currentSequenceEnumerator = method.Invoke(scriptClass,parameters) as IEnumerator;					
				if ( currentSequenceEnumerator != null )
				{	
					// Cache some things to see if anything changed when invoked (to see if we should fallback to unhandled)
					bool wasTransitioning = m_transitioning;
					DialogTree prevDialog = m_currentDialog;

					if ( cancelCurrentInteraction && m_sequenceIsCancelable )
					{
						// OnEnterRegion and OnUpdateBlocking need this Hacky cancelable stuff to prevent their own functions from being added to the m_backgroundSequences and then cancelled
						m_sequenceIsCancelable = false; //m_regionHack = true; // If it ends up being necessary to use E.CancelCurrentInteraction() in UpdateBlocking/OnEnterRegion without blocking the script, we might have to change this to a flag, and check for it in StartQuestCoroutine()
						result = StartCoroutine(currentSequenceEnumerator);		
						m_sequenceIsCancelable = true; //m_regionHack = false;
					}
					else 
					{
						result = StartCoroutine(currentSequenceEnumerator);
					}		
					
					// If the enumerator.Current is the yield break one, return false so we don't have to wait an extra frame
					if ( currentSequenceEnumerator.Current == EMPTY_YIELD_INSTRUCTION || result == null)
					{						
						if ( m_transitioning && wasTransitioning == false || prevDialog != m_currentDialog )
						{
							// Check if there was a room change, or dialog started- which should count as 'consumed' interaction even if it doesn't yield
							m_consumedInteraction = StartCoroutine(CoroutineEmpty());
							result = m_consumedInteraction;
						}						
						else
						{
							result = null;
						}
					}
					else if ( currentSequenceEnumerator.Current == CONSUME_YIELD_INSTRUCTION )
					{
						m_consumedInteraction = StartCoroutine(CoroutineEmpty());
						result = m_consumedInteraction;
					}
					else if ( result != null )
					{
						if ( cancelCurrentInteraction )
							CancelCurrentInteraction(); // This cancels the "previous" interaction. Used for "OnEnterRegion" to cancel any current sequence. Needs to be called before the result is added to the currentSequences
						
						m_currentSequences.Add(result); // Add to list of current coroutines (for canceling)
					}
					
				}
			}
		}
		return result;
	}

	// NB- Since WaitFor and HandleInteract functions may call this BEFORE their calling function, we use skipCallingFunction to ensure these can still be auto-loaded
	public void SetAutoLoadScript( IQuestScriptable questScriptable, string functionName, bool functionBlocked, bool skipCallingFunction )
	{
		if ( Application.isEditor == false )
			return;
		
		// Hacky fix for "WaitFor" functions have their calling functions "auto-load" after them due to order of ops.
		{
			if ( skipCallingFunction )
			{
				m_ignoreAutoLoadFunc = true; // Set ot ignore the next auto-loaded function (if it's the one called before the "WaitFor" function)
			}
			else if ( m_ignoreAutoLoadFunc )
			{ 
				if ( m_ignoreAutoLoadFuncName == functionName) // AutoLoadFunc should be the most recently called function
				{
					m_ignoreAutoLoadFuncName = string.Empty;
					m_ignoreAutoLoadFunc = false;
					return;
				}
				else 
				{
					m_ignoreAutoLoadFuncName = string.Empty;
					m_ignoreAutoLoadFunc = false;
				}
			}
		}

		bool unhandled = functionName.StartsWith(STR_UNHANDLED);
		if ( functionBlocked )
		{
			if ( unhandled && m_autoLoadUnhandledScriptable != null )
			{
				// This was an unhandled event- we want to auto-load the event that was unhandled.
				m_autoLoadScriptable = m_autoLoadUnhandledScriptable;
				m_autoLoadFunction = m_autoLoadUnhandledFunction;
				return;
			}
			m_autoLoadScriptable = questScriptable;
			m_autoLoadFunction = functionName;
		}
		else // Function didn't block
		{
			if ( unhandled == false )
			{
				// Function didn't block, so store it incase there's an unhandled event next, so can auto-load the non-unhanled event function
				m_autoLoadUnhandledScriptable = questScriptable;
				m_autoLoadUnhandledFunction = functionName;		
			}
		}
		
	}

	#endregion
	#region Coroutines

	static IEnumerator CoroutineEmpty()
	{
		if ( PowerQuest.Get.GetSkippingCutscene() )
			yield break;
		yield return null;
	}

	static IEnumerator CoroutineWaitForTime(float time, bool skippable)
	{		
		int frame = 0; // first frame the mouse will always be down, so don't skip until 2nd. When time starts as 0, we still want to pause for a single frame.
		while ( (time > 0.0f || frame<2)
			&& PowerQuest.Get.GetSkippingCutscene() == false
			&& ( skippable == false || PowerQuest.Get.HandleSkipDialogKeyPressed() == false || frame<1 ) )
		{
			++frame;
			yield return new WaitForEndOfFrame();
			if ( SystemTime.Paused == false )
			{
				time -= Time.deltaTime;
			}
		}
	}

	static IEnumerator CoroutineWaitForTimer(string timerName, bool skippable)
	{		
		bool first = true; // first frame the mouse will always be down, so don't skip until 2nd. When time starts as 0, we still want to pause for a single frame.
		while ( (PowerQuest.Get.GetTimer(timerName) > 0 || first)
			&& PowerQuest.Get.GetSkippingCutscene() == false
			&& ( skippable == false || PowerQuest.Get.HandleSkipDialogKeyPressed() == false || first ) )
		{
			first = false;
			yield return new WaitForEndOfFrame();
		}
		PowerQuest.Get.SetTimer(timerName,-1);
	}

	IEnumerator CoroutineDelayedInvoke( float time, System.Action functionToInvoke)
	{
		yield return Wait(time);
		if ( functionToInvoke != null )
			functionToInvoke.Invoke();
	}

	IEnumerator CoroutineDisplay(string text, int id = -1)
	{
		if ( GetSkippingCutscene() )
			yield break;
		QuestText textComponent;
		StartDisplay(text,id, out textComponent);
		yield return WaitForDialog(PowerQuest.Get.GetTextDisplayTime(text), m_dialogAudioSource, m_displayTextAutoAdvance, true, textComponent);	
		EndDisplay();
	}

	IEnumerator CoroutineDisplayBG(string text, int id = -1)
	{
		if ( GetSkippingCutscene() )
			yield break;
		QuestText textComponent;
		StartDisplay(text,id, out textComponent);
		yield return WaitForDialog(PowerQuest.Get.GetTextDisplayTime(text), m_dialogAudioSource, true, false, textComponent);
		EndDisplay();
	}
	
	IEnumerator CoroutineWaitForDialog(float time, bool skippable, bool autoAdvance, AudioHandle audioSource, QuestText textComponent = null )
	{		
		m_timeLastTextShown = Time.timeSinceLevelLoad;
		bool first = true; // first frame the mouse will always be down, so don't skip until 2nd		
		while ( ShouldContinueDialog(first, ref time, skippable || m_waitingForBGDialogSkip, autoAdvance, audioSource, textComponent) )
		{
			first = false;
			yield return new WaitForEndOfFrame();
			if ( SystemTime.Paused == false )
			{
				time -= Time.deltaTime;
			}
		}
	}

	/** Now way overcomplicated function to calc whether dialog should continue.  
		'time' is ticked down in the calling function, but passed as ref so it can be reset here when audio finishes, so we can have optional delay on end of dialog lines.
	*/
	public bool ShouldContinueDialogOld( bool firstCall, ref float time, bool skippable, bool autoAdvance, AudioHandle audioSource, QuestText textComponent = null, float endTime = 0 )
	{
		bool result = true;
		
		// Check if time is up (when no audiosource, and auto-advance is on)
		if ( autoAdvance && audioSource == null )
			result &= (time > endTime);

		// Check if skipping cutscene
		result &= PowerQuest.Get.GetSkippingCutscene() == false;

		// Check if skip dialog button is pressed		
		if ( textComponent != null && textComponent.GetTyping() && PowerQuest.Get.HandleSkipDialogKeyPressed() && firstCall == false )
		{
			textComponent.SkipTyping();
			return true;
		}

		result &= ( skippable == false || PowerQuest.Get.HandleSkipDialogKeyPressed() == false || firstCall );

		if ( audioSource != null )
		{
			bool audioPlaying = true;
			// Check if has audio and if it's finished playing
			#if UNITY_SWITCH 
			audioPlaying &= ( audioSource == null || audioSource.isPlaying || PowerQuest.Get.Paused );
			#else
			audioPlaying &= ( audioSource == null || audioSource.isPlaying || Application.isFocused == false || PowerQuest.Get.Paused );			
			#endif
						
			// hack test... should really start the timer when audio is stopped though, not this way...
			if ( audioPlaying )
				time = 0;
			else 
				result &= time > -m_textAutoAdvanceDelay; // Add extra time to wait after audio clip finishes

			result &= ( endTime <= 0 || audioSource == null || audioSource.clip == null || audioSource.time < audioSource.clip.length - endTime );
		}

		return result;
	}

	
	public bool ShouldContinueDialog( bool firstCall, ref float time, bool skippable, bool autoAdvance, AudioHandle audioSource, QuestText textComponent = null, float endTime = 0 )
	{
		
		// Check if time is up (when no audiosource, and auto-advance is on)
		if ( autoAdvance && audioSource == null && time <= endTime )
			return false;

		// Check if skipping cutscene
		if ( PowerQuest.Get.GetSkippingCutscene() )
			return false;

		// Check if skip dialog button is pressed		
		if ( textComponent != null && textComponent.GetTyping() && PowerQuest.Get.HandleSkipDialogKeyPressed() && firstCall == false )
		{
			textComponent.SkipTyping();
			return true;
		}

		if ( skippable && PowerQuest.Get.HandleSkipDialogKeyPressed() && firstCall == false )
			return false;

		if ( audioSource != null )
		{			
			// Check if has audio and if it's finished playing
			#if UNITY_SWITCH 
			bool audioPlaying = ( audioSource.isPlaying || PowerQuest.Get.Paused );
			#else
			bool audioPlaying = ( audioSource.isPlaying || Application.isFocused == false || PowerQuest.Get.Paused );			
			#endif
			
			if ( m_textAutoAdvanceDelay > 0 ) // check if we should wait a bit after audio stops
			{						
				if ( audioPlaying )				
					time = 0; // set time to zero the whole time audio is playing, so we know its at zero when it finishes				
				else if ( time <= -m_textAutoAdvanceDelay ) // Add extra time to wait after audio clip finishes		
					return false;				
			}
			else if ( audioPlaying == false )
			{
				return false; // audio stopped so we're done
			}

			// End early when endtime is set
			if ( endTime > 0 && audioSource.clip != null && audioSource.time >= audioSource.clip.length - endTime )
				return false;
		}

		return true;
	}

	IEnumerator CoroutineFadeIn( string source, float time, bool skippable = false )
	{
		m_menuManager.FadeIn(time, source);		
		yield return skippable ? WaitSkip(time) : Wait(time);		
		if ( skippable )
			m_menuManager.FadeSkip();
	}

	IEnumerator CoroutineFadeOut( string source, float time, bool skippable = false )
	{
		m_menuManager.FadeOut(time, source);		
		yield return skippable ? WaitSkip(time) : Wait(time);		
		m_menuManager.FadeSkip();
		yield return null; // extra frame to render completely black screen
	}

	IEnumerator CoroutineRoomTransition( Room room, bool instant )
	{
		/* ORDER OF OPERATIONS FOR TRANSITION
			- m_transitioning = true
			- Wait for fade out
			- Main loop stopped
			- m_roomLoopStarted = false ( Update will no longer be called)
			- OnExitRoom
			- Wait a frame
			- Remove fades
			- Change Scene
		Then, after scene Loaded, in CoroutineRoomTransition
			- Create Room
			- Set m_currentRoom
			- Create/Setup Camera, Guis, Cursor
			- Spawn and position characters
			- m_initialised = true
			- Room instance OnLoadComplete
			- Debug Play-From function
			- Block
			- Wait a frame
			- OnEnterRoom
			- OnEnterRoomAfterFade (initial call)
			- Update regions
			- Camera onEnterRoom
			- start fading in
			- m_transitioning = false (allows Update to be called again)
			- Yield to OnEnterRoomAfterFade
			- Update starts being called
			- Unblock
			- m_roomLoopStarted = true;
			- Start MainLoop()
		*/
		
		bool wasBlocking = m_blocking;
		if ( wasBlocking == false )
			Block();

		string sceneName = room.GetSceneName();
		m_transitioning = true;
		if ( m_restoring == false )
			SV.m_callEnterOnRestore = true;
			
		// Load the rooms atlas if it's in resources
		StartCoroutine(LoadAtlasAsync(room.ScriptName));

		AsyncOperation operation = null;
		if ( instant )
		{
			FadeOutBG(0,"RoomChange");
		}
		else 
		{		
			operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			operation.allowSceneActivation = false;		
			yield return FadeOut(TransitionFadeTime/2.0f, "RoomChange");
		}
		
		
		if ( m_coroutineMainLoop != null )
		{
			StopCoroutine(m_coroutineMainLoop);
			m_coroutineMainLoop = null;
			m_roomLoopStarted = false;
		}

		if ( m_restoring == false )
		{
			// Call OnExitRoom functions
			Coroutine onExit = StartScriptInteractionCoroutine(GetScript(), "OnExitRoom", new object[] { m_currentRoom, room } );
			SetAutoLoadScript( this, "OnExitRoom", onExit != null, false );
			if ( onExit != null )
				yield return onExit;

			if ( m_currentRoom != null && m_currentRoom.GetScript() != null )
			{
				onExit = StartScriptInteractionCoroutine(m_currentRoom.GetScript(), "OnExitRoom", new object[] { m_currentRoom, room } );
				SetAutoLoadScript( m_currentRoom, "OnExitRoom", onExit != null, false );
				if ( onExit != null )
					yield return onExit;
			}
			
			RestoreAllClickables();
			RestoreAllClickableCursors();

		}
				

		if ( instant == false )
		{
			// Finish loading scene	
			while( m_loadingAtlas)
				yield return null;	
			operation.allowSceneActivation = true;
			while( operation.isDone == false )			
				yield return null;					
		}
		else 
		{
			yield return new WaitForEndOfFrame();
			while( m_loadingAtlas )
				yield return null;
				
			if ( wasBlocking == false )
				Unblock();				
			SceneManager.LoadScene(sceneName);
		}

	}

	IEnumerator CoroutineWaitUntil(System.Func<bool> condition, bool skippable = false) 
	{ 
		bool first = true; // first frame the mouse will always be down, so don't skip until 2nd
		while ( condition != null && condition() == false
			&& ( /*skippable == false ||*/ PowerQuest.Get.GetSkippingCutscene() == false ) // (Changed 25/9/22) Note, previsouly- unlike other skippable methods, WaitUntil required skippable flag to be skipped in cutscene.
			&& ( skippable == false || PowerQuest.Get.HandleSkipDialogKeyPressed() == false || first ) )
		{
			first = false;
			yield return null;
		}
	}
	IEnumerator CoroutineWaitWhile(System.Func<bool> condition, bool skippable = false)
	{ 
		bool first = true; // first frame the mouse will always be down, so don't skip until 2nd
		while ( condition != null && condition() == true 
			&& ( /*skippable == false ||*/ PowerQuest.Get.GetSkippingCutscene() == false ) // (Changed 25/9/22) Note, previsouly- unlike other skippable methods, WaitUntil required skippable flag to be skipped in cutscene.
			&& ( skippable == false || PowerQuest.Get.HandleSkipDialogKeyPressed() == false || first ) )
		{
			first = false;
			yield return null;
		}
	}

	IEnumerator CoroutineWaitForDialog() { yield return WaitWhile(()=> {		
			// Check if any players are currently talking
			return m_displayActive || m_characters.Exists( item => item.Talking );
	} ); } 
	
	IEnumerator CoroutineWaitForDialogSkip()
	{ 		
		m_skipDialog = false; // clear this flag when we start waiting here
		m_waitingForBGDialogSkip = true;
		yield return WaitWhile(()=> {		
			// Check if any players are currently talking
			return m_displayActive || m_characters.Exists( item => item.Talking );
		} ); 
		m_waitingForBGDialogSkip = false;
	}

	IEnumerator CoroutineChangeRoom( IRoom room )
	{		
		GetPlayer().Room = room;
		// Don't use WaitUntil since we don't want to skip waiting even when skipping cutscene
		while ( (m_levelLoadedCalled && m_roomLoopStarted) == false )
			yield return null;
	}

	#endregion
	#region Atlas loading

	//
	// Cached atlases. 
	//
	SpriteAtlas m_lastAtlas = null;
	SpriteAtlas m_atlasToUnload = null;
	bool m_loadingAtlas = false;

	void LoadAtlas(string roomName)
	{
		string atlasName = $"Room{roomName}Atlas";
		System.Action<SpriteAtlas> callback;
		if ( s_roomAtlasCallbacks.TryGetValue(atlasName, out callback) )		
		{
			SpriteAtlas atlas = Resources.Load<SpriteAtlas>(atlasName);			
			OnAtlasLoadComplete(atlasName,atlas,callback);
		}
	}	
	IEnumerator LoadAtlasAsync(string roomName)
	{
		// Load atlas
		m_loadingAtlas=true;
		string atlasName = $"Room{roomName}Atlas";
		System.Action<SpriteAtlas> callback;
		if ( s_roomAtlasCallbacks.TryGetValue(atlasName, out callback) )		
		{
			ResourceRequest req = Resources.LoadAsync<SpriteAtlas>(atlasName);
			while ( req.isDone == false )
				yield return null;
			SpriteAtlas atlas = req.asset as SpriteAtlas;
			OnAtlasLoadComplete(atlasName,atlas,callback);

		}
		m_loadingAtlas=false;
		yield break;

	}

	void OnAtlasLoadComplete(string atlasName, SpriteAtlas atlas, System.Action<SpriteAtlas> callback)
	{
		if ( atlas )
		{		
			/* Experiencing unity editor hanging sometimes at this point, so disabling atlas de-loading for now. It was unclear whether this was working anyway. /
			if ( m_atlasToUnload != null && m_atlasToUnload.name != atlas.name )
			{
				if ( Debug.isDebugBuild )
					Debug.Log("Unloading atlas "+m_atlasToUnload);	
				Resources.UnloadAsset(m_atlasToUnload);						
				if ( Debug.isDebugBuild )
					Debug.Log("...Success");

			}
			/* */

			// We want to remove atlases 2 rooms ago, so we can async load the next room while current is active
			m_atlasToUnload = m_lastAtlas;
			m_lastAtlas = atlas;
			//if ( Debug.isDebugBuild )
			//	Debug.Log("Loaded atlas "+atlasName);
			callback(atlas);
		}
		else 
		{
			Debug.Log("Failed to find atlas "+atlasName);
		}
	}
}


#endregion
}
