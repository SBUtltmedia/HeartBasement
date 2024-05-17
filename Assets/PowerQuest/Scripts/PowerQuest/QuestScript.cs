using UnityEngine;
using System.Collections;
using PowerScript;
using System.Reflection;
using System.Runtime.Serialization;
namespace PowerTools.Quest
{

/// All quest object scripts inherit from this, it provides some convenient functions (along with the PowerScript namespace) 
[System.Serializable]
public partial class QuestScript
{	
	#region Functions: Static functions for convenience

	// Convenient shortcut to PowerQuest interface (E for Engine)
	protected static IPowerQuest E { get {return PowerQuest.Get as IPowerQuest; } }
	protected static ICursor Cursor { get {return PowerQuest.Get.Cursor; } }
	protected static ICamera Camera { get {return PowerQuest.Get.Camera; } }
	protected static QuestSettings Settings { get {return PowerQuest.Get.Settings; } }

	/// Access to the global script.
	protected static GlobalScript Globals { get { return GlobalScript.Script; } }

	/// Get a hotspot in the current room. Shortcut to as R.Current.GetHotspot(...)
	protected static IHotspot Hotspot(string name) { return PowerQuest.Get.GetCurrentRoom().GetHotspot(name); }

	/// Get a prop in the current room. Shortcut to as R.Current.GetProp(...)
	protected static IProp Prop(string name) { return PowerQuest.Get.GetCurrentRoom().GetProp(name); }

	/// Get a region in the current room. Shortcut to as R.Current.GetRegion(...)
	protected static IRegion Region(string name) { return PowerQuest.Get.GetCurrentRoom().GetRegion(name); }

	/// Get a point in the current room. Shortcut to as R.Current.GetPoint(...)
	protected static Vector2 Point(string name) { return PowerQuest.Get.GetCurrentRoom().GetPoint(name); }

	#endregion
	#region Functions: Private utils

	[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}
	#endregion

}

}
