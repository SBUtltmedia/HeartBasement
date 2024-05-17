using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiPrompt : GuiScript<GuiPrompt>
{
	System.Action m_onOk = null;
	System.Action m_onCancel = null;
	
	bool m_result = true;
	
	public bool Result =>m_result;
	public bool OkClicked => m_result;
	public bool CancelClicked => m_result==false;
	

	void OnShow()
	{
	}

	void Update()
	{
	}

	// This would probably be better as a more system function. But I guess can try this for now.
	public void Show(string text, string buttonOk, string buttonCancel, System.Action onOk = null, System.Action onCancel = null )
	{
		if ( Data.Visible )
			Debug.LogWarning("Another prompt is already showing!");
		
		
		Label("Text").Text = text;
		
		m_onOk = onOk;
		Button("BtnOk").Text = buttonOk;
		if ( buttonCancel != null )
		{
			m_onCancel = onCancel;
			Button("BtnCancel").Text = buttonCancel;
			Button("BtnCancel").Show();
		}
		else
		{
			Button("BtnCancel").Hide();
		}
		
		Data.ShowAtFront();
	}
	
	public void Show(string text, string buttonOk, System.Action onOk = null )
	{
		Show(text, buttonOk, null, onOk, null);
	}

	public IEnumerator WaitForPrompt(string text, string buttonOk)
	{
		Show( text, buttonOk );		
		yield return E.WaitForGui(Data);
	}
	public IEnumerator WaitForPrompt(string text, string buttonOk, string buttonCancel)
	{
		Show( text, buttonOk, buttonCancel );		
		yield return E.WaitForGui(Data);
	}

	IEnumerator OnClickBtnOk( IGuiControl control )
	{
		
		m_result = true;

		// Clear old data before hiding/invoking callback, incase that pushes another prompt.
		System.Action onOk = m_onOk;
		m_onOk = null;
		m_onCancel = null;

		Data.Hide();

		if ( onOk != null )
			onOk.Invoke();		
		
		yield return E.Break;
	}

	IEnumerator OnClickBtnCancel( IGuiControl control )
	{
		m_result = false;
	
		// Clear old data before hiding/invoking callback, incase that pushes another prompt.
		System.Action onCancel = m_onCancel;
		m_onOk = null;
		m_onOk = null;
		m_onCancel = null;

		Data.Hide();

		if ( onCancel != null )
			onCancel.Invoke();
				
		yield return E.Break;
	}
	
}

/* Trying things to see most convenient way to open prompts. 
	eg Prompt.Show("This is a prompt"); 
	Can also use the gui's script, eg.
		G.Prompt.Script.Show(...);
	Could potentially add something to powerquest, eg.
		E.ShowPrompt(...);
*/
[QuestAutoCompletable]
public class Prompt
{
	
	static void Show(string text, string buttonOk, System.Action onOk = null)
	{
		GuiPrompt.Script.Show(text,buttonOk,onOk);
	}
	
	static void Show(string text, string buttonOk, string buttonCancel, System.Action onOk = null, System.Action onCancel = null)
	{
		GuiPrompt.Script.Show(text,buttonOk,buttonCancel,onOk,onCancel);
	}
}
