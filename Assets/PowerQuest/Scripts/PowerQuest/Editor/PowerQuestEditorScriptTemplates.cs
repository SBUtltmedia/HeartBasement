using PowerTools.Quest;
using PowerTools;

namespace PowerTools.Quest
{

public partial class PowerQuestEditor
{

	#region Variables: Static definitions
	
	public static readonly string TEMPLATE_GLOBAL_FILE = 	
@"using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerScript;
using PowerTools.Quest;

///	Global Script: The home for your game specific logic
/**		
 * - The functions in this script are used in every room in your game.
 * - Add your own variables and functions in here and you can access them with `Globals.` (eg: `Globals.m_myCoolInteger`)
 * - If you've used Adventure Game Studio, this is equivalent to the Global Script in that
*/
public partial class GlobalScript : GlobalScriptBase<GlobalScript>
{

}";

	public static readonly string TEMPLATE_ROOM_FILE = 	
@"using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class Room#NAME# : RoomScript<Room#NAME#>
{

}";

	public static readonly string TEMPLATE_CHARACTER_FILE = 
@"using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class Character#NAME# : CharacterScript<Character#NAME#>
{

}";

	public static readonly string TEMPLATE_INVENTORY_FILE = 
@"using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class Inventory#NAME# : InventoryScript<Inventory#NAME#>
{

}";

	public static readonly string TEMPLATE_DIALOGTREE_FILE =
@"using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class Dialog#NAME# : DialogTreeScript<Dialog#NAME#>
{
	public IEnumerator OnStart()
	{
		yield return E.Break;
	}

	public IEnumerator OnStop()
	{
		yield return E.Break;
	}
}";

	public static readonly string TEMPLATE_GUI_FILE =		
@"using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class Gui#NAME# : GuiScript<Gui#NAME#>
{

}";



	#endregion
	
}

}