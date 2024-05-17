using UnityEngine;
using System.Collections;
using PowerTools;

namespace PowerTools.Quest
{


public class GuiInfoBarComponent : MonoBehaviour
{

	[SerializeField] bool m_hideWhenDisplayboxActive = false;
	[SerializeField] bool m_hideWhenDialogTreeActive = false;
	[SerializeField] bool m_alignToInventory = true;
	[SerializeField] float m_alignOffset = -30;
	[SerializeField] QuestText m_hoverText = null;

	GuiDropDownBar m_inventory = null;
	ScreenAlign m_alignTo = null;

	// Use this for initialization
	void Start () 
	{
		m_inventory = PowerQuest.Get.GetGui("Inventory").Instance.GetComponent<GuiDropDownBar>();		
		if ( m_alignToInventory )
			m_alignTo = m_hoverText.GetComponent<ScreenAlign>();
	}
	
	// Update is called once per frame
	void Update() 
	{
		if ( m_hoverText != null )
		{			
			m_hoverText.gameObject.SetActive( PowerQuest.Get.GetBlocked() == false 
				&& (m_hideWhenDisplayboxActive == false || PowerQuest.Get.GetGui("DisplayBox").Visible == false)
				&& (m_hideWhenDialogTreeActive == false || PowerQuest.Get.GetGui("DialogTree").Visible == false));
			m_hoverText.SetText(PowerQuest.Get.GetMouseOverDescription());
			if ( m_alignToInventory && m_alignTo != null )
				m_alignTo.Offset = m_alignTo.Offset.WithY( m_inventory.GetOffset().y+m_alignOffset );
			
		}

	}
}
}