using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Reflection;
using PowerTools;


//
// PowerQuest Partial Class: Public facing functions, etc go here.
//

namespace PowerTools.Quest
{

#region Global Enums

// Face Directions
[QuestAutoCompletable]
public enum eFace
{	
	None = -1,Left, Right, Down, Up, DownLeft, DownRight, UpLeft, UpRight
};

// Type of QuestObject - Used as prefix to classname
[QuestAutoCompletable]
public enum eQuestObjectType
{
	Room,
	Character,
	Inventory,
	Dialog,
	Gui,
	Prop,
	Hotspot,
	Region
};

// Type of clickable
[QuestAutoCompletable]
public enum eQuestClickableType
{
	None = -1,
	Character,
	Prop,
	Hotspot,
	Gui,
	Inventory
};

[QuestAutoCompletable]
public enum eQuestVerb
{
	None,
	Walk,
	Use,
	Look,
	Inventory,
	Parser
}

[QuestAutoCompletable]
public enum eSpeechStyle
{	
	AboveCharacter,
	Portrait,
	Caption,
	Custom,
};

// TODO: other location styles
public enum eSpeechPortraitLocation
{
	Left,
	TODORight,
	TODOAlternating,
	TODOCharacterPosition,
	TODOCharacterFacing,
};

/// Easing curves. See https://easings.net for visualisation of most of them. "Smooth" is a light easing and a good default. IT's like InOutSin but more efficient (it's what powers perlin noise)
[QuestAutoCompletable]
public enum eEaseCurve 
{ 
	None, Linear=None, 
	InSmooth,  OutSmooth, InOutSmooth, Smooth = InOutSmooth, 
	InSine,    OutSine,   InOutSine, 
	InQuad,    OutQuad,   InOutQuad, 
	InCubic,   OutCubic,  InOutCubic, 
	InQuart,   OutQuart,  InOutQuart,
	InQuint,   OutQuint,  InOutQuint, 
	InExp,     OutExp,    InOutExp, 
	InElastic, OutElastic,InOutElastic }

// Enum for keyboard/controller navigation of menus
public enum eGuiNav { Left, Right, Up, Down, Ok, Cancel }

#endregion
#region Interfaces

//
// Interface to Interactive objects (players, hotpots, props)
//
public partial interface IQuestClickable
{
	eQuestClickableType ClickableType { get; }
	MonoBehaviour Instance { get; }
	string Description { get; set; }
	string ScriptName { get; }
	Vector2 WalkToPoint { get; set; }
	Vector2 LookAtPoint { get; set; }
	float Baseline { get; set; }
	bool Clickable { get; set; }
	string Cursor { get; set; }
	Vector2 Position {get;}
	// Called when player interacts with object
	void OnInteraction( eQuestVerb verb );
	// Called when current player interaction with object is canced (eg: during walk-to)
	void OnCancelInteraction( eQuestVerb verb );
	QuestScript GetScript();
	IQuestScriptable GetScriptable();

}

//
// Interface to Scriptable objects (players, rooms)
//
public interface IQuestScriptable
{
	string GetScriptName();
	string GetScriptClassName();
	QuestScript GetScript();
	void HotLoadScript(Assembly assembly);
	void EditorRename(string name);
}

public partial interface IQuestClickableInterface
{
	IQuestClickable IClickable {get;}
}

// TODO: flexible interaction system using this editable data structure
[System.Serializable]
public class QuestAction
{
	public string m_editorName = "On Interact";
	public string m_editorNameLong = "Use";
	public string m_scriptName = "Interact";
	public eQuestVerb m_verb = eQuestVerb.None;
}

// NB: you can add to these by adding a line liek this at the top of your singleton system 
//  namespace PowerTools.Quest { public static partial class Systems {	public static SystemMyStuff MyStuff { get{return SystemMyStuff.Get; } } } }
public static partial class Systems
{
	public static PowerQuest Quest { get{return PowerQuest.Get; } }
	public static SystemAudio Audio { get{return SystemAudio.Get; } }
	public static SystemTime Time { get{return SystemTime.Get; } }
	public static SystemDebug Debug { get{return SystemDebug.Get; } }
	public static SystemText Text { get { return SystemText.Get; } }	

	/// Returns true when systems have been initialised
	public static bool Valid { get { return PowerQuest.GetValid() && SystemAudio.GetValid() && SystemTime.GetValid(); } }
}

#endregion
#region PowerQuest Typedefs
/// PowerQuestDefinitions- Extra interfaces, static definitions, etc
public partial class PowerQuest
{	
	public static readonly string GLOBAL_SCRIPT_NAME = "GlobalScript";
	public static readonly string SCRIPT_FUNCTION_INTERACT = "OnInteract";
	public static readonly string SCRIPT_FUNCTION_LOOKAT = "OnLookAt";
	public static readonly string SCRIPT_FUNCTION_USEINV = "OnUseInv";
	public static readonly string SCRIPT_FUNCTION_DIALOG_START_OLD = "Start";
	public static readonly string SCRIPT_FUNCTION_DIALOG_START = "OnStart";
	public static readonly string SCRIPT_FUNCTION_DIALOG_STOP = "OnStop";
	public static readonly string SCRIPT_FUNCTION_DIALOG_OPTION = "Option";
	public static readonly string STR_HOTSPOT = "Hotspot";
	public static readonly string STR_PROP = "Prop";
	public static readonly string STR_REGION = "Region";
	public static readonly string STR_CHARACTER = "Character";
	public static readonly string STR_INVENTORY = "Inventory";
	public static readonly string SCRIPT_FUNCTION_INTERACT_PROP = SCRIPT_FUNCTION_INTERACT+STR_PROP;
	public static readonly string SCRIPT_FUNCTION_INTERACT_HOTSPOT = SCRIPT_FUNCTION_INTERACT+STR_HOTSPOT;
	public static readonly string SCRIPT_FUNCTION_INTERACT_INVENTORY = SCRIPT_FUNCTION_INTERACT+STR_INVENTORY;
	public static readonly string SCRIPT_FUNCTION_INTERACT_CHARACTER = SCRIPT_FUNCTION_INTERACT+STR_CHARACTER;
	public static readonly string SCRIPT_FUNCTION_LOOKAT_PROP = SCRIPT_FUNCTION_LOOKAT+STR_PROP;
	public static readonly string SCRIPT_FUNCTION_LOOKAT_HOTSPOT = SCRIPT_FUNCTION_LOOKAT+STR_HOTSPOT;
	public static readonly string SCRIPT_FUNCTION_LOOKAT_INVENTORY = SCRIPT_FUNCTION_LOOKAT+STR_INVENTORY;
	public static readonly string SCRIPT_FUNCTION_LOOKAT_CHARACTER = SCRIPT_FUNCTION_LOOKAT+STR_CHARACTER;
	public static readonly string SCRIPT_FUNCTION_USEINV_PROP = SCRIPT_FUNCTION_USEINV+STR_PROP;
	public static readonly string SCRIPT_FUNCTION_USEINV_HOTSPOT = SCRIPT_FUNCTION_USEINV+STR_HOTSPOT;
	public static readonly string SCRIPT_FUNCTION_USEINV_INVENTORY = SCRIPT_FUNCTION_USEINV+STR_INVENTORY;
	public static readonly string SCRIPT_FUNCTION_USEINV_CHARACTER = SCRIPT_FUNCTION_USEINV+STR_CHARACTER;
	public static readonly string SCRIPT_FUNCTION_ENTER_REGION = "OnEnterRegion";
	public static readonly string SCRIPT_FUNCTION_EXIT_REGION = "OnExitRegion";
	public static readonly string SCRIPT_FUNCTION_ENTER_REGION_BG = "OnEnterRegionBG";
	public static readonly string SCRIPT_FUNCTION_EXIT_REGION_BG = "OnExitRegionBG";
	public static readonly string SCRIPT_FUNCTION_GETCURSOR = "GetCursor";
	public static readonly string SCRIPT_FUNCTION_ONMOUSECLICK= "OnMouseClick";
	public static readonly string SCRIPT_FUNCTION_ONWALKTO = "OnWalkTo";
	public static readonly string SCRIPT_FUNCTION_ONANYCLICK= "OnAnyClick";
	public static readonly string SCRIPT_FUNCTION_AFTERANYCLICK= "AfterAnyClick";
	public static readonly string SCRIPT_FUNCTION_CLICKGUI = "OnClick";
	public static readonly string SCRIPT_FUNCTION_DRAGGUI = "OnDrag";
	public static readonly string SCRIPT_FUNCTION_ONKBFOCUS = "OnKeyboardFocus";
	public static readonly string SCRIPT_FUNCTION_ONKBDEFOCUS = "OnKeyboardDefocus";
	public static readonly string SCRIPT_FUNCTION_ONTEXTEDIT = "OnTextEdit";
	public static readonly string SCRIPT_FUNCTION_ONTEXTCONFIRM = "OnTextConfirm";
	public static readonly string SCRIPT_FUNCTION_ONPARSER = "OnParser";

	static readonly YieldInstruction EMPTY_YIELD_INSTRUCTION = new YieldInstruction(); // Used to prevent having to wait a frame in UpdateBlocking which happens when a routine returns yield break
	static readonly YieldInstruction CONSUME_YIELD_INSTRUCTION = new YieldInstruction(); // Used to prevent falling through to default interaction in an empty function
	static readonly float TEXT_DISPLAY_TIME_MIN = 1.0f;
	static readonly float TEXT_DISPLAY_TIME_CHARACTER = 0.1f;
	static readonly string DEFAULT_FADE_SOURCE = "";

	static int LAYER_UI = -1; 

	class ExtraSaveData
	{
		//public bool m_paused = false;
		public string m_player = string.Empty;
		public string m_currentDialog = string.Empty;
		
		public string m_displayBoxGui;
		public string m_dialogTreeGui;
		public string m_customSpeechGui;
		public eSpeechStyle m_speechStyle;
		public eSpeechPortraitLocation m_speechPortraitLocation;
		public float m_transitionFadeTime;
	}

	public delegate IEnumerator DelegateWaitForFunction();
	public delegate void DelegateDelayedFunction();
}

#endregion


}
