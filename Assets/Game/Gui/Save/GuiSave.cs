using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiSave : GuiScript<GuiSave>
{	
	// Variables to control how screenshots are displayed- for quick customisation
	readonly int ScreenshotWidth = 64;
	readonly int ScreenshotHeight = 36;
	readonly float ScreenshotZoom = 1.25f;
	readonly int AutoSaveSlot = 1;
	
	bool m_save = true;
	
	public void ShowSave()
	{
		m_save = true;
		Data.Show();
	}

	public void ShowRestore()
	{
		m_save = false;
		Data.Show();
	}

	void OnShow()
	{
		
		if ( m_save )
			Label("LblSave").Text = SystemText.Localize("Save Game");
		else
			Label("LblSave").Text = SystemText.Localize("Restore Game");
					
		IButton[] slots =
		{
			null, Button("SaveSlot1"),Button("SaveSlot2"),Button("SaveSlot3"),Button("SaveSlot4"),Button("SaveSlot5"),Button("SaveSlot6")
		};
		
		for ( int i = 1; i <= 6; ++i )
		{
			QuestSaveSlotData data = E.GetSaveSlotData(i);		
						
			slots[i].Clickable = true;
		
			if ( data == null )
			{
				slots[i].Text = SystemText.Localize("Empty");
		
				if ( m_save )
				{
					slots[i].Anim = "SaveSlotFree";
				}
				else
				{
					slots[i].Anim = "SaveSlotEmpty";
					slots[i].Clickable = false;
				}
			}
			else
			{
				slots[i].Text = data.m_description;
		
				slots[i].Anim = "SaveSlot";
				SpriteRenderer renderer = slots[i].Instance.transform.Find("Screenshot").GetComponentInChildren<SpriteRenderer>(true);
				if ( data.m_image == null )
				{
					renderer.enabled = false;
				}
				else
				{
					renderer.enabled = true;
		
					// Create textures form the save slot image. We scale the texture using TextureScaler class to preserve pixel-perfectness.
					Texture2D result = TextureScaler.scaled(data.m_image, (int)(ScreenshotWidth*ScreenshotZoom),(int)(ScreenshotHeight*ScreenshotZoom), FilterMode.Point);
					result.filterMode = FilterMode.Point;
					Sprite sprite = Sprite.Create(result, new Rect(0,0, ScreenshotWidth,ScreenshotHeight), new Vector2(0.5f,0.5f),1);
					renderer.sprite = sprite;
				}
			}		
					

			// Special case for auto save slot
			if ( i == AutoSaveSlot )
			{		
				slots[i].Text = SystemText.Localize("Autosave");
				// don't allow save over auto-save slot
				if ( m_save )
					slots[i].Clickable = false;		
			}
			

			// Add "(Latest)" if it's the latest slot to be saved into
			if ( m_save == false && E.GetLastSaveSlotData() == data )
				slots[i].Text += $" ({SystemText.Localize("Latest")})";

		}
		
	}

	
	void OnClickSaveSlot( IGuiControl control, int slot )
	{	
		if ( m_save )
		{
		
			if ( E.GetSaveSlotData(slot) != null )
			{
				GuiPrompt.Script.Show(SystemText.Localize("Overwrite save data?"), SystemText.Localize("Yes"),SystemText.Localize("No"), ()=>Save(slot));
			}
			else
			{
				Save(slot);
			}
		}
		else 
		{
			Load(slot);
		}
	}
	
	void Save(int slot)
	{
		Data.Hide();
		
		// Write a description for the save- Users could enter this in a prompt, but we'll just use the date and time for now
		string description = System.DateTime.Now.ToString("d MMM yy"); // Eg: 1 Jan 22
		
		E.Save(slot, description);
		GuiPrompt.Script.Show( SystemText.Localize("Game Saved"), SystemText.Localize("Ok"));
	}

	void Load(int slot)
	{
		Data.Hide();
		E.RestoreSave(slot);
	}

	IEnumerator OnClickCancel( IGuiControl control )
	{
		G.Save.Hide();
		yield return E.Break;
	}

	IEnumerator OnClickSaveSlot1( IGuiControl control )
	{
		OnClickSaveSlot(control,1);
		yield return E.Break;
	}

	IEnumerator OnClickSaveSlot2( IGuiControl control )
	{
		OnClickSaveSlot(control,2);
		yield return E.Break;
	}

	IEnumerator OnClickSaveSlot3( IGuiControl control )
	{
		OnClickSaveSlot(control,3);
		yield return E.Break;
	}

	IEnumerator OnClickSaveSlot4( IGuiControl control )
	{
		OnClickSaveSlot(control,4);
		yield return E.Break;
	}

	IEnumerator OnClickSaveSlot5( IGuiControl control )
	{
		OnClickSaveSlot(control,5);
		yield return E.Break;
	}

	IEnumerator OnClickSaveSlot6( IGuiControl control )
	{
		OnClickSaveSlot(control,6);
		yield return E.Break;
	}

	
}
