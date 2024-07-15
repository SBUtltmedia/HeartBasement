using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;

namespace PowerTools.Quest
{

//
// Hotspot Data and functions. Persistant between scenes, as opposed to HotspotComponent which lives on a GameObject in a scene.
//
[System.Serializable] 
public partial class Hotspot : IQuestClickable, IHotspot, IQuestScriptable, IQuestSaveCachable
{

	#region Hotspot: Editor data
	//
	// Default values set in inspector
	//	
	[Header("Mouse-over Defaults")]
	[TextArea(1,10)]
	[SerializeField] string m_description = "New Hotspot";
	[Tooltip("If set, changes the name of the cursor when moused over")]
	[SerializeField] string m_cursor = "";
	[Header("Starting State")]
	[Tooltip("Whether clicking on hotspot triggers an event")]
	[SerializeField] bool m_clickable = true;
	[Header("Editable in Scene")]
	[Tooltip("Handles the picking order of the hotspot (lower is picked first, as it's infront, same as objects/characters")]
	[SerializeField] float m_baseline = 0;
	[SerializeField] Vector2 m_walkToPoint = Vector2.zero;
	[SerializeField] Vector2 m_lookAtPoint = Vector2.zero;
	[ReadOnly, SerializeField] string m_scriptName = "HotspotNew";

	#endregion
	#region Hotspot: Vars: private
	//
	// Private variables
	//
	HotspotComponent m_instance = null;
	int m_useCount = 0;
	int m_lookCount = 0;

	#endregion
	#region Hotspot: properties
	//
	//  Properties
	//
	public eQuestClickableType ClickableType { get {return eQuestClickableType.Hotspot; } }
	public string Description { get{ return m_description;} set{m_description = value;} }
	public string ScriptName { get{ return m_scriptName;} }
	public MonoBehaviour Instance { get{ return m_instance; } }
	public Hotspot Data {get {return this;} }
	public IQuestClickable IClickable { get{ return this; } }
	public bool Clickable { get{ return m_clickable;} set{m_clickable = value;} }
	/// Set's visible & clickable (same as Enable)
	public void Show() { Enable(); }
	/// Set's invisible & non-clickable (same as Disable)
	public void Hide() { Disable(); }
	/// Set's visible & clickable
	public void Enable() { Clickable = true; }
	/// Set's invisible & non-clickable
	public void Disable() { Clickable = false; }
	public float Baseline { get{ return m_baseline;} set{m_baseline = value;} }
	public Vector2 WalkToPoint { get{ return m_walkToPoint;} set{m_walkToPoint = value;} }
	public Vector2 LookAtPoint { get{ return m_lookAtPoint;} set{m_lookAtPoint = value;} }
	public string Cursor { get { return m_cursor; } set { m_cursor = value; } }
	public bool FirstUse { get { return UseCount == 0; } } 
	public bool FirstLook { get { return LookCount == 0; } }
	public int UseCount { get { return m_useCount - (PowerQuest.Get.GetInteractionInProgress(this,eQuestVerb.Use) ? 1 : 0); } }
	public int LookCount { get { return m_lookCount - (PowerQuest.Get.GetInteractionInProgress(this,eQuestVerb.Look) ? 1 : 0); } }

	public HotspotComponent GetInstance() { return m_instance; }
	public void SetInstance(HotspotComponent instance) 
	{ 
		m_instance = instance; 
		instance.SetData(this);
	}
	// Return room's script
	public QuestScript GetScript() { return (PowerQuest.Get.GetCurrentRoom() == null) ? null : PowerQuest.Get.GetCurrentRoom().GetScript(); } 
	public IQuestScriptable GetScriptable() { return this; }

	// Hotspots don't have a position, but IQuestClickable requires access to one, just reutrn zero
	public Vector2 Position { get{ return Vector2.zero; } }
		
	#endregion
	#region Partial functions for extentions
	
	partial void ExOnInteraction(eQuestVerb verb);
	partial void ExOnCancelInteraction(eQuestVerb verb);

	//
	// Public Functions
	//
	#endregion
	#region Hotspot: Functions: Public 

	public void OnInteraction( eQuestVerb verb )
	{				
		if ( verb == eQuestVerb.Look ) ++m_lookCount;
		else if ( verb == eQuestVerb.Use) ++m_useCount;
		ExOnInteraction(verb);
	}
	public void OnCancelInteraction( eQuestVerb verb )
	{		
		if ( verb == eQuestVerb.Look ) --m_lookCount;
		else if ( verb == eQuestVerb.Use) --m_useCount;
		ExOnCancelInteraction(verb);
	}

	public void EditorInitialise( string name )
	{
		m_description = name;
		m_scriptName = name;
	}
	public void EditorRename(string name)
	{
		m_scriptName = name;
	}

	#endregion
	#region Hotspot Functions: Implementing IQuestScriptable

	// Doesn't use all functions
	public string GetScriptName() { return m_scriptName; }
	public string GetScriptClassName() { return PowerQuest.STR_HOTSPOT+m_scriptName; }
	public void HotLoadScript(System.Reflection.Assembly assembly) { /*No-op*/ }
	
	#endregion
	#region Hotspot Functions: Implementing IQuestSaveCachable
	//
	// Implementing IQuestSaveCachable
	//	
	bool m_saveDirtyEver = false;
	public bool SaveDirty { get=>m_saveDirtyEver; set {
		if (value) 
		{
			//if ( m_saveDirtyEver == false )	Debug.Log($"Hotspot {ScriptName} set dirty");
			m_saveDirtyEver=true; 
		} } }
	public bool SaveDirtyEver => m_saveDirtyEver;

	#endregion
	#region Hotspot: Functions: Private 

	//
	// Internal Functions
	//


	// Handles setting up defaults incase items have been added or removed since last loading a save file
	[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}
	#endregion
}

//
// The component on the hotspot in scene
//
public partial class HotspotComponent : MonoBehaviour 
{

	[SerializeField] Hotspot m_data = new Hotspot();

	public Hotspot GetData() { return m_data; }
	public void SetData(Hotspot data) { m_data = data; }

	// Called once room and everything in it has been created and PowerQuest has initialised references. After Start, Before OnEnterRoom.
	public void OnLoadComplete()
	{

	}

}

}
