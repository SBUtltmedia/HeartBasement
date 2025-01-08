using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

namespace PowerTools.QuestGui
{

[System.Serializable] 
[AddComponentMenu("Quest Gui/Label")]
public partial class Label : GuiControl, ILabel
{

	#region Vars: Editor	
	
	#endregion
	#region Vars: Private
	
	QuestText m_questText = null;

	#region Functions: ILabel interface	
	
	public QuestText TextComponent	{ get 
	{
		if ( m_questText == null )
			m_questText = GetComponentInChildren<QuestText>();
		return m_questText;
	} }

	public override string Text 
	{
		get { return TextComponent != null ? TextComponent.text : null; }
		set { if ( TextComponent != null ) TextComponent.text = value; }
	}
	
	public override Color Color 
	{
		get { return TextComponent != null ? TextComponent.color : Color.white; }
		set { if ( TextComponent != null ) TextComponent.color = value; }
	}
	

	public IQuestClickable IClickable { get{ return this; } }

	
	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public override RectCentered GetRect(Transform excludeChild = null)
	{
		RectCentered result = GuiUtils.CalculateGuiRectFromRenderer(transform, false, GetComponent<MeshRenderer>(), excludeChild);
		result.Transform(transform); // doesn't need this since the meshRenderer isn't a child of this object
		return result;
	}

	/*
	public bool Visible 
	{ 
		get { return gameObject.activeSelf;} 
		set	 { gameObject.SetActive(value); }
	}
	public void Show() { gameObject.SetActive(true); }
	public void Hide() { gameObject.SetActive(false); }
	
	public void SetPosition(float x, float y) { Position = new Vector2(x,y); }
	*/
	#endregion
	#region Functions: Public (Non interface)
	
	public QuestText GetQuestText() { return m_questText; }	

	
	#endregion
	#region Component: Functions: Unity
	
	// Use this for initialization
	void Awake() 
	{	
		m_questText = GetComponentInChildren<QuestText>();
	}

	#endregion
	#region Funcs: Private Internal
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

	// Handles setting up defaults incase items have been added or removed since last loading a save file
	/*[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}*/
	
	#endregion	
	#region Implementing IQuestClickable
	
	//public override string Description { get{ return m_description;} set{m_description = value;} }
	//public override bool Clickable { get{ return false;} set{} }
	//public override string Cursor { get { return m_cursor; } set { m_cursor = value; }  }
	//public override Vector2 Position { get{ return transform.position;} set{ transform.position = value.WithZ(transform.position.z); } }

	

	#endregion
}

#endregion

}
