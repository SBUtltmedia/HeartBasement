using UnityEngine;
using System.Collections;

namespace PowerTools.Quest
{

public class ScreenShakeTester : MonoBehaviour 
{
	[Header("Add to scene, then run & press SPACE to shake")]
	[SerializeField] float m_intensity = 1.0f;
	[SerializeField] float m_duration = 0.0f;
	[SerializeField] float m_falloff = 0.15f;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
	{

		if ( PowerQuest.Get.GameHasKeyboardFocus && Input.GetKeyDown(KeyCode.Space))
		{
			PowerQuest.Get.GetCamera().Shake(m_intensity,m_duration, m_falloff);
		}
	}
}
}
