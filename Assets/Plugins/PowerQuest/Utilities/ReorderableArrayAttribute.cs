using UnityEngine;
using System.Collections;

namespace PowerTools.Quest
{

public class ReorderableArrayAttribute : PropertyAttribute 
{
    // Intentionally blank
}


public class WeightedArrayAttribute : PropertyAttribute 
{	
	public string m_weightPropertyName = "m_weight";
	public string m_dataPropertyName = null;

	public WeightedArrayAttribute() 
	{}
	public WeightedArrayAttribute(string propertyName) 
	{
		m_weightPropertyName = propertyName;
	}

	public WeightedArrayAttribute(string weightPropertyName, string dataPropertyName) 
	{
		m_weightPropertyName = weightPropertyName;
		m_dataPropertyName = dataPropertyName;
	}
}

}