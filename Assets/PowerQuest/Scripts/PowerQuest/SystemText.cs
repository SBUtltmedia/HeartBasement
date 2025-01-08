//#define RUNTIME_CSV_IMPORT_ENABLED // Uncomment to enable, and also move CSVFile.dll from '/ThirdPary/Editor/' directory into '/ThirdParty/' directory, and tick the "Any Platform" tickbox on it's settings.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
#if RUNTIME_CSV_IMPORT_ENABLED
using System.IO;
#endif

namespace PowerTools.Quest
{

[System.Serializable]
public class TextData
{
	public string m_character = null;
	public int m_id = -1;
	public int m_orderId = 0; // The order that the file appears in the scripts when they were parsed
	public string m_string = null;
	public string m_sourceFile = null;
	public string m_sourceFunction = null;
	public string[] m_translations = null;
	public float[] m_phonesTime = null;
	public char[] m_phonesCharacter = null;
	public bool m_changedSinceImport = true;
}

[System.Serializable]
public class LanguageData
{
	public string m_code = "EN";
	public string m_description = "English";
	public string[] m_customData = null;
}


public partial class SystemText : PowerTools.Singleton<SystemText>
{ 
	public enum ePlayerName
	{
		Character,
		Plr,
		Player,
		Ego
	}


	public enum eDefaultTextSource
	{
		Script, ImportedText
	}

	public class CharacterTextDataList : Dictionary< string, List<TextData> > { }

	[SerializeField] LanguageData[] m_languages = {new LanguageData()};
	
	[SerializeField] eDefaultTextSource m_defaultTextSource = eDefaultTextSource.Script;

	[Tooltip("Optional extended mouth shapes, eg: GHX")]
	[SerializeField] string m_lipSyncExtendedShapes = "X";

	// Master list of all strings
	[SerializeField, HideInInspector] List<TextData> m_strings = new List<TextData>(); 

	[SerializeField] System.Text.Encoding m_csvEncoding = System.Text.Encoding.Default; 
		

	// Dictionary of character to string for quick lookup
	CharacterTextDataList m_characterStrings = null;

	List<TextData> m_stringsCopy = null;
	CharacterTextDataList m_characterStringsCopy = null;

	Dictionary<string, TextData> m_textOnlyStrings = null;

	int m_currLanguage = 0;

	// cache whether the X shape is used for lipsync
	bool m_lipSyncUsesXShape = false;
	
	ePlayerName m_lastPlayerName = ePlayerName.Character;
		
	// For alternate VO lines. Can be set before playing the next line to use an alternate take. Eg: NARR123B
	string m_nextVoiceClipAffix = null;	
	public string NextVoiceClipAffix { get=>m_nextVoiceClipAffix; set { m_nextVoiceClipAffix = value; } }

	public int GetNumLanguages() { return m_languages.Length; }
	// Returns the currently selected language id
	public int GetLanguage() { return m_currLanguage; }
	// REturns the language id for the specified code, -1 if not found
	public int GetLanguageId(string languageCode) { return System.Array.FindIndex(GetLanguages(), item=> string.Equals( item.m_code, languageCode, System.StringComparison.OrdinalIgnoreCase) ); }
	public LanguageData GetLanguageData() { return m_languages[m_currLanguage]; }
	public LanguageData GetLanguageData(int id) { return m_languages[id]; }
	public LanguageData GetLanguageData(string languageCode) { return m_languages[GetLanguageId(languageCode)]; }
	/// NB: You should usually set the language via PowerQuest.Settings so it will be saved.

	public void SetLanguage(int languageId) 
	{
		m_currLanguage = languageId; 

		// Find any quest text that's localised and update it's text		
		QuestText[] textObjects = FindObjectsOfType<QuestText>(true);
		System.Array.ForEach( textObjects, item=>item.OnLanguageChange() );
	}

	/// NB: You should usually set the language via PowerQuest.Settings so it will be saved. Returns false if languge code not found
	public bool SetLanguage(string languageCode) 
	{ 	
		// Find the language code
		int languageId = GetLanguageId(languageCode);
		if ( languageId < 0 )
		{
			Debug.LogWarning("Couldn't find language code: "+languageCode+", The code needs to be added to SystemText");
			return false;
		}
		SystemText.Get.SetLanguage(languageId);
		return true;
	}
	public LanguageData[] GetLanguages() { return m_languages; }

	public bool GetLipsyncUsesXShape() { return m_lipSyncUsesXShape; }
	public string GetLipsyncExtendedMouthShapes()  { return m_lipSyncExtendedShapes; }
	public void SetLipsyncExtendedMouthShapes(string value)  { m_lipSyncExtendedShapes = value; }
	
	public static string Localize( string defaultText, int id = -1, string characterName = null )
	{
		return GetDisplayText(defaultText, id, characterName);
	}

	public static string GetDisplayText(string defaultText, int id = -1, string characterName = null, bool isPlayer = false)
	{
		if ( m_instance == null || defaultText == null )
			return defaultText;
		m_instance.UpdateTextDataLists();

		TextData data = null;
		// first check if can parse the id from the text
		if ( id < 0 )
			id = m_instance.ParseIdFromText(ref defaultText); 
		
		if ( id < 0 )
		{
			// If there's no id, find the id in the "text only" strings
			m_instance.m_textOnlyStrings.TryGetValue(defaultText, out data );
		}
		else 
		{
			// Handle cases where character dialog is mixed between 'Plr', 'Player', 'Ego' and the characters actual name, by overriding the character name based on the function that was used to call it.
			if ( isPlayer && m_instance.LastPlayerName != ePlayerName.Character )
				characterName = m_instance.LastPlayerName.ToString();

			// Otherwise find the string in the character data. If character's null it could be a "Display" string
			data = m_instance.FindTextDataInternal(id, characterName);

			/* This was the old way to check character names, but depended on the imported text matching the text in the script. /
			if (isPlayer) 
			{
				// If scripts have mixed references for dialogs for the main character, like
				// "Dave: ", "Player: ", "Plr: ", etc, and the current character is the player,
				// we may find the wrong string due to overlapping ids.
				// We check the original string matches and try some fallbacks...
				if (data?.m_string != defaultText)
					data = m_instance.FindTextDataInternal(id, "Player");
				if (data?.m_string != defaultText)
					data = m_instance.FindTextDataInternal(id, "Plr");
				if (data?.m_string != defaultText)
					data = m_instance.FindTextDataInternal(id, "Ego");
			}
			// a translation has been found but it's for "someone else". Ignore it.
			if (data?.m_string != defaultText)
				data = null;
			/**/
		}

		if ( data == null )
			return defaultText;

		// Check if there's a translation
		int languageId = m_instance.m_currLanguage-1;

		if ( languageId >= 0 && m_instance.m_currLanguage < m_instance.m_languages.Length && languageId < data.m_translations.Length
			&& string.IsNullOrEmpty(data.m_translations[languageId]) == false )
			return data.m_translations[languageId];	

		// Handle option for using SystemText for the default language, rather than leaving what's in the script.
		if ( m_instance.m_defaultTextSource == eDefaultTextSource.ImportedText && string.IsNullOrEmpty(data.m_string) == false )
			return data.m_string;

		// For now return the default text always, so it always matches what's in the script
		return defaultText;
	}


	public static AudioHandle PlayAudio(int id, string characterName, Transform emitter = null, UnityEngine.Audio.AudioMixerGroup mixerGroupOverride = null )
	{
		TextData data = m_instance.FindTextDataInternal(id, characterName);
		if ( data == null )
		{
			if  ( Debug.isDebugBuild && id >= 0 )
				Debug.LogWarning("Text id "+characterName+id.ToString()+" is missing. You need to run 'Process Text From Scripts' to add ids!");
			return null;
		}
		
		AudioClip clip = m_instance.GetVoiceAudioClip(id, characterName);		
		return SystemAudio.Play( clip, (int)AudioCue.eAudioType.Dialog, emitter, 1,1,false, mixerGroupOverride );
	}

	public static TextData FindTextData(int id, string characterName = null)
	{		
		if ( m_instance == null )
			return null;
		return m_instance.FindTextDataInternal(id,characterName);
	}


	// Parses an id from a line of text that starts with an &<id> , and strips the id from teh text. Eg- turns "&124 Hello" into 124 and "Hello"
	public int ParseIdFromText(ref string text)
	{
		if ( string.IsNullOrEmpty(text) || text[0] != '&')
			return -1;
		int spaceIndex = text.IndexOf(' ',1);
		if ( spaceIndex < 1 )
			return -1;

		string idStr = text.Substring(1,spaceIndex);

		int result;
		if ( int.TryParse(idStr,out result) == false )
			return -1;

		text = text.Substring(spaceIndex+1);
		return result;			
	}

	public void EditorOnBeginAddText()
	{
		// Start new list, keep old copy incase we ahve existing ids to merge across
		UpdateTextDataLists();
		m_stringsCopy = m_strings;
		m_strings = new List<TextData>(m_stringsCopy.Count);
		m_characterStringsCopy = m_characterStrings;
		m_characterStrings = new CharacterTextDataList();//(m_characterStringsCopy.Count);
	} 

	// Adds text line to the system, returning a new id
	public TextData EditorAddText( string line, string sourceFile = null, string sourceFunction = null, string characterName = null, int existingId = -1, bool preserveExistingIds = false )
	{
		List<TextData> characterTextDataList = null;

		if ( characterName == null )
			characterName = string.Empty;

		if ( m_characterStrings.TryGetValue(characterName, out characterTextDataList)  == false )
		{
			characterTextDataList = new List<TextData>();
			m_characterStrings.Add(characterName,characterTextDataList);
		}

		// By default the new id is the next availble
		int newId = characterTextDataList.Count;

		if ( existingId == -1 )
			existingId = ParseIdFromText(ref line);

		if ( preserveExistingIds )  
		{
		    // When preserving existing ids, use the passed in one if it's set
			if ( existingId != -1 )
			{
				newId = existingId;
			}
			else 
			{
			    // if it's not set, iterate through current ids (if there are any) and find one that's not used (in either the old list, or the new one)
				List<TextData> oldCharacterTextDataList = null;
				List<TextData> newCharacterTextDataList = null;
				if ( m_characterStringsCopy != null )
					m_characterStringsCopy.TryGetValue(characterName, out oldCharacterTextDataList);
				m_characterStrings.TryGetValue(characterName, out newCharacterTextDataList);		
				while ( (oldCharacterTextDataList != null && oldCharacterTextDataList.Exists( item => item.m_id == newId ))
					|| (newCharacterTextDataList != null && newCharacterTextDataList.Exists( item => item.m_id == newId )) )
			    {
			        ++newId;
			    }
				
				// NB: if there's no existing character strings, it'll use the default id
			}
		}

		// Add the line
		TextData newData = new TextData() 
		{
			m_id = newId,
			m_character = characterName,
			m_orderId = m_strings.Count,
			m_string = line,
			m_sourceFile = sourceFile,
			m_sourceFunction = sourceFunction
		};

		// If there's an existing id, copy the translations and lip sync data to the new TextData
		if ( existingId >= 0 )
		{
			TextData oldData = FindTextDataCopy(existingId, characterName);
			if ( oldData != null )
			{
				newData.m_translations = oldData.m_translations;
				newData.m_phonesCharacter = oldData.m_phonesCharacter;
				newData.m_phonesTime = oldData.m_phonesTime;
				if ( newData.m_string != oldData.m_string )
					newData.m_changedSinceImport = true;				
			}
		}

		m_strings.Add(newData);
		characterTextDataList.Add( newData);

		return newData;

	}
	
	 // NB: Now always importing default text from CSV
	public bool EditorGetShouldImportDefaultStringFromCSV() { return true; } // m_defaultTextSource == eDefaultTextSource.ImportedText; }
	public eDefaultTextSource EditorDefaultTextSource { get { return m_defaultTextSource; } set{ m_defaultTextSource = value; } }

	public List<TextData> EditorGetTextDataOrdered()
	{
		return m_strings;
		/*
		List<TextData> result = new List<TextData>();
		result.AddRange( m_strings );
		//result.Sort((a,b)=>a.m_orderId.CompareTo(b.m_orderId));
		return result;
		*/
	}

	// Find text data for a line, if Id = -1 and there's no character name, it'll use the default text 
	public TextData EditorFindText( string defaultText, int id = -1, string characterName = null )
	{		
		TextData result = null;
		
		UpdateTextDataLists();

		if ( id < 0 )
			id = ParseIdFromText(ref defaultText); 
		
		if ( id < 0 )
			m_textOnlyStrings.TryGetValue(defaultText,out result);
		else 
			result = FindTextDataInternal(id,characterName);
		return result;

	}

	/// TODO: This is pretty inefficient, it loads the resource instead of just checking it exists. Should replace with check for file existing at path in editor script
	public bool EditorHasAudio(int id, string characterName)
	{
		return GetVoiceAudioClip(id, characterName) != null;
	}
	
	// This is used to specify which "name" a player is using (eg: Plr, Player, Ego, or the character's ScriptName), so it can be retrieved from the text system correctly
	public ePlayerName LastPlayerName { get{ return m_lastPlayerName; } set{ m_lastPlayerName=value; } }

	// Gets the audio clip for a particular id/data.
	AudioClip GetVoiceAudioClip(int id, string characterName)
	{
		string fileName = characterName + id.ToString() + m_nextVoiceClipAffix;	
		m_nextVoiceClipAffix = null; // reset VO affix after consuming it

		// First try path with language code
		string filePath = $"Voice/{GetLanguageData().m_code}/";		

		Object clip = Resources.Load(filePath+fileName);		
		if ( clip == null ) 
		{
			// Fall back to path without language code
			//Debug.Log($"Voice file {fileName} not found for language code '{GetLanguageData().m_code}'");
			filePath = "Voice/";
			clip = Resources.Load(filePath+fileName);
		}			
		return clip as AudioClip;
	}


	TextData FindTextDataInternal(int id, string characterName = null)
	{		
		UpdateTextDataLists();

		if ( characterName == null )
			characterName = string.Empty;

		if (m_characterStrings.TryGetValue(characterName, out List<TextData> dataList)) 
		{
			foreach (var textData in dataList) 
			{
				if (textData.m_id == id)
					return textData;
			}
		}

		return null;
	}


	TextData FindTextDataCopy( int id, string characterName = null )
	{
		if ( characterName == null )
			characterName = string.Empty;

		List<TextData> dataList = null; 
		if ( m_characterStringsCopy != null )
			m_characterStringsCopy.TryGetValue(characterName, out dataList);

		if ( dataList != null )
			return dataList.Find(item=>item.m_id == id);
		
		return null;
	}

	// Use this for initialization
	void Awake() 
	{
		SetSingleton();
		DontDestroyOnLoad(this);

		m_lipSyncUsesXShape = m_lipSyncExtendedShapes.Contains("X");
	}

	// Since the character dictionary deserialises, recreate it if it's null on first use
	void UpdateTextDataLists()
	{
		if ( m_characterStrings == null || m_textOnlyStrings == null )
		{
			m_characterStrings = new CharacterTextDataList();
			m_textOnlyStrings = new Dictionary<string, TextData>();

			int numStrings = m_strings.Count;
			for (int i = 0; i < numStrings; ++i )
			{
				TextData data = m_strings[i];

				if ( data.m_id < 0 )
				{
					m_textOnlyStrings.Add(data.m_string,data);
				}
				else
				{
					List<TextData> textDataList = null;
					string characterName = data.m_character == null ? string.Empty : data.m_character;
					if ( m_characterStrings.TryGetValue(characterName, out textDataList)  == false )
					{
						textDataList = new List<TextData>();
						m_characterStrings.Add(characterName,textDataList);
					}
					textDataList.Add(data);
				}
			}
		}
	}

	#if RUNTIME_CSV_IMPORT_ENABLED
	static readonly int CSV_NUM_HEADERS = 4;
	static readonly int CSV_INDEX_LANGUAGES = CSV_NUM_HEADERS;

	// Import CSV translation file, returns true for success.
	public bool ImportFromCSV(string scriptPath, out string resultMessage)
	{
		bool result = false;
		resultMessage = null;

		if ( string.IsNullOrEmpty(scriptPath) )
		{
			resultMessage = "Invalid Path";
			return result;
		}

		int lineId = -1;
		int numLanguages = GetNumLanguages();

		// Using CSV-Reader https://github.com/tspence/csharp-csv-reader

		FileStream stream = null;
		StreamReader streamReader = null;

		try
		{
			stream = File.OpenRead(scriptPath);
			streamReader = new StreamReader(stream, System.Text.Encoding.Default, true);

			using ( CSVFile.CSVReader reader = new CSVFile.CSVReader(streamReader, new CSVFile.CSVSettings() { HeaderRowIncluded = false }) )
			{
				foreach ( string[] line in reader )
				{
					++lineId;
					if ( lineId == 0 )
					{
						// Check for expected languages
						if ( line.Length < CSV_NUM_HEADERS + numLanguages )
						{
							string error = "Import failed, unexpected columns:\nFound: ";
							for ( int i = 0; i < line.Length; ++i )
								error += line[i] + ", ";
							error += "\nExpected: "
								  + "Character,ID,File,Context";
							foreach (LanguageData language in GetLanguages())
								error+=","+language.m_description;
							resultMessage = error;
							break;
						}
						continue;
					}
					if ( line.Length < CSV_NUM_HEADERS+1 )
						continue; // skipping line, since it doesn't have the right amount of stuff
					string character = line[0];
					int id = -1;
					if ( int.TryParse(line[1], out id) == false )
						id = -1;
					string defaultText = line[CSV_INDEX_LANGUAGES];

					// Find the line
					TextData textData = EditorFindText(defaultText, id, character );
					if ( textData == null )
					{
						Debug.Log("Failed to import line (not found in text system): "+character+id+": "+defaultText);
					}
					else if ( numLanguages > 1 )
					{
						// Import other languages
						textData.m_translations = new string[numLanguages-1];
						for ( int i = 1; i < numLanguages && CSV_INDEX_LANGUAGES+i < line.Length; ++i)
						{
							textData.m_translations[i-1] = line[CSV_INDEX_LANGUAGES+i];
						}
					}

					textData.m_changedSinceImport = false;
					
				}
			}

		}
		catch (System.IO.IOException e )
		{
			result = false;
			resultMessage = "Failed to open CSV file. \nCheck it's not already open elsewhere.\n\nError: "+e.Message;
		}
		catch (System.Exception e)
		{	
			result = false;
			resultMessage = "Failed to import CSV file.\n\nError: "+e.Message;
		}
		finally
		{
			if ( streamReader != null )
				streamReader.Close();
			if ( stream != null )
				stream.Close();
		}
		
		result = true;
		return result;
	}
	#endif
	
}

}
