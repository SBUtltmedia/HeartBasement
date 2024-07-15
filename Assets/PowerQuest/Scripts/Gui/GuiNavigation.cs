using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTools.Quest
{

/// Add this component to override how left/right/up/down buttons navigate between this control and others
[AddComponentMenu("Quest Gui Layout/Navigation")]
public class GuiNavigation : MonoBehaviour
{
	[SerializeField] GuiControl[] m_left = null;
	[SerializeField] GuiControl[] m_right = null;
	[SerializeField] GuiControl[] m_up = null;
	[SerializeField] GuiControl[] m_down = null;

	[SerializeField] bool m_autoLeft = true;
	[SerializeField] bool m_autoRight = true;
	[SerializeField] bool m_autoUp = true;
	[SerializeField] bool m_autoDown = true;
		
	public GuiControl Left =>  GetResult(m_left);
	public GuiControl Right => GetResult(m_right);
	public GuiControl Up =>    GetResult(m_up);
	public GuiControl Down =>  GetResult(m_down);

	public bool AutoLeft =>  m_autoLeft;
	public bool AutoRight => m_autoRight;
	public bool AutoUp =>    m_autoUp;
	public bool AutoDown =>  m_autoDown;
	
	public IGuiControl GetNextNavControl(eGuiNav dir, Gui gui)
	{ 
		switch(dir)
		{ 
			case eGuiNav.Left:  return GetResult(m_left);
			case eGuiNav.Right: return GetResult(m_right);
			case eGuiNav.Up:    return GetResult(m_up);
			case eGuiNav.Down:  return GetResult(m_down);
		}
		
		// Get Parent control
		switch(dir)
		{ 
			case eGuiNav.Left:  return m_autoLeft ?  gui.GetNextNavControl(dir) : null;
			case eGuiNav.Right: return m_autoRight ? gui.GetNextNavControl(dir) : null;
			case eGuiNav.Up:    return m_autoUp ?    gui.GetNextNavControl(dir) : null;
			case eGuiNav.Down:  return m_autoDown ?  gui.GetNextNavControl(dir) : null;
		}

		return null;
	}

	GuiControl GetResult(GuiControl[] list)
	{ 
		foreach (GuiControl item in list )
		{ 
			if ( item.Clickable )
				return item;
		}
		return null;
	}

	

}

}