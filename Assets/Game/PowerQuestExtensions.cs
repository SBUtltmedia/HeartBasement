using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;

namespace PowerTools.Quest
{

/// If you want to add your own functions/variables to PowerQuest stuff, add them here. 
/**
 * Variables added to these classes is automatically saved, and will show up in the "Data" of objects in the inspecor
 * Adding functions to the interfaces will make them accessable in QuestScript editor.
 */

/// Functions/Properties added here are accessable from the 'E.' object in quest script
public partial interface IPowerQuest
{
	
}

public partial class PowerQuest
{
	public void ResetWalkClickDown()
    {
        m_walkClickDown = false;
    }
}

/// Functions/Properties added here are accessable from the 'C.<characterName>.' object in quest script
public partial interface ICharacter
{
	/** Example: Adding health variable to characters /
	bool IsDead();
	float HealthPoints { get; set; }
	/**/
}

public partial class Character
{
	/** Example: Adding health to characters /
	[SerializeField] float m_healthPoints = 0;
	public bool IsDead() { return m_healthPoints <= 0; }
	public float HealthPoints { get { return m_healthPoints; } set { m_healthPoints = value; } }
	/**/
}

/// Functions/Properties added here are accessable from the 'R.<RoomName>.' object in quest script
public partial interface IRoom
{
}

public partial class Room
{
}

/// Functions/Properties added here are accessable from the 'Props.<name>.' object in quest script
public partial interface IProp
{
}

public partial class Prop
{
}

/// Functions/Properties added here are accessable from the 'Hotspots.<name>.' object in quest script
public partial interface IHotspot
{
}

public partial class Hotspot
{
}

/// Functions/Properties added here are accessable from the 'Regions.<name>.' object in quest script
public partial interface IRegion
{
}

public partial class Region
{
}

/// Functions/Properties added here are accessable from the 'I.<itemName>.' object in quest script
public partial interface IInventory
{
//	public bool isHandle;
//	public bool isHose;

//	public enum size {small, medium, large};
}

public partial class Inventory
{
}

}