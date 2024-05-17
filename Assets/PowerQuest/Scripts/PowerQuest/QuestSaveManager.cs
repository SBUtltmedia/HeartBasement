//#define LOG_TIME

//#define LOG_DATA
#if LOG_DATA 
	//#define LOG_DATA_SERIALIZEABLE // More verbose, shows all serializables, not just custom classes
	#define LOG_DATA_SURROGATE
#endif

// Cache data speeds up consecutive saves, at cost of restore being slower, and save file being bigger
#define CACHE_SAVE_DATA 
//#define LOG_CACHE_DATA

// Checking the QuestDontSave attribute potentially makes save/load slower. Though testing with/without this  and logging time, it didn't seem dramatically differnt
//#define ENABLE_DONTSAVE_ATTRIB

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// for saving
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;
using PowerTools;

namespace PowerTools.Quest
{

// Attribute used for including global enums in autocomplete
[AttributeUsage(AttributeTargets.All)]
public class QuestSaveAttribute : System.Attribute
{
	public QuestSaveAttribute(){}
}

/// NB: This attribute is disabled for now
[AttributeUsage(AttributeTargets.All)]
public class QuestDontSaveAttribute : System.Attribute
{
	public QuestDontSaveAttribute(){}
}

#region Class: Save Slot Data

[System.Serializable]
public class QuestSaveSlotData
{
	// The header of each 
	public int m_slotId = -1;
	public int m_version = -1;
	public int m_timestamp = int.MinValue;
	public string m_description = null;	
	public Texture2D m_image = null;
}

#endregion
#region Class: Save Manager

public interface IQuestSaveCachable
{
	bool SaveDirty {get;set;}
}

public class QuestSaveManager
{

	#endregion
	#region Definitions

	// Used so can easily add data to save system
	class CustomSaveData
	{
		public string m_name = null;
		public object m_data = null;

		public System.Action CallbackOnPostRestore = null;
	}
	/*
	class CustomSaveVars
	{
		public string m_name = null;
		public object m_owner = null;
		public FieldInfo m_data = null;

		public System.Action CallbackOnPostRestore = null;
	}*/

	// used	https://www.random.org/bytes/ (or http://randomkeygen.com/ and https://www.branah.com/ascii-converter)

	// These should be set per game probably, or at least have that be an option. could do a hash of the game name or something i guess...
	static readonly byte[] NOTHING_TO_SEE_HERE = {0xdd, 0x2a, 0xdc, 0x58, 0xa6, 0xc4, 0xca, 0x10};
	static readonly byte[] JUST_A_REGULAR_VARIABLE = {0x47, 0xa1, 0x6d, 0xc1, 0xc6, 0x67, 0xd9, 0xed};

	static readonly string FILE_NAME_START = "Save";
	static readonly string FILE_NAME_EXTENTION = ".sav";
	static readonly string FILE_NAME_WILDCARD = FILE_NAME_START+"*"+FILE_NAME_EXTENTION;

	// Version and version requirement for the save manager. There's a seperate one used for the "game"
	#if CACHE_SAVE_DATA
		static readonly int VERSION_CURRENT = 4;
	#else 
		static readonly int VERSION_CURRENT = 4;
	#endif
	static readonly int VERSION_REQUIRED = 4;
	
	#endregion
	#region Variables

	List<QuestSaveSlotData> m_saveSlots = new List<QuestSaveSlotData>();
	string m_log = string.Empty;	// Debug text log thing, might remove if it's not going to be useful for getting load error messages to display to user
	bool m_loadedSaveSlots = false; // Flag set true when save slots have been loaded

	List< CustomSaveData > m_customSaveData = new List< CustomSaveData >();
	//List< CustomSaveVars > m_customSaveVars = new List< CustomSaveVars >();
	
	// Serialized bytes, cached so they don't have to be serialized again (since that's so slow)
	Dictionary<string, byte[]> m_cachedSaveData = new Dictionary<string, byte[]>();

	#endregion
	#region Public Functions
	/*
	public void AddSaveDataAttribute(string name, object owner, System.Action OnPostRestore = null )
	{
		string finalName = name+'%'+data.name;
		if ( m_customSaveVars.Exists( item => string.Equals( item.m_name, name ) ) )
		{
			Debug.LogWarning("Save data already exists for "+name+", Call UnregisterSaveData first for safety. Item will be overwritten");
			m_customSaveVars.RemoveAll( item=> string.Equals( item.m_name, name ) );
		}
		CustomSaveVars newData = new CustomSaveVars()
		{
			m_name = name,
			m_owner = owner,
			m_data = data,
			CallbackOnPostRestore = OnPostRestore
		};
		m_customSaveVars.Add(newData);

	}*/

	public void AddSaveData(string name, object data, System.Action OnPostRestore = null )
	{		
		if ( Debug.isDebugBuild && data.GetType().IsValueType )
		{
			Debug.LogError("Error in AddSaveData( \""+name+"\", ... ): Value types cannot be used for custom save data. You need to save the containing class, or put them in one to be saved");
		}
		else if ( Debug.isDebugBuild && QuestSaveSurrogateSelector.IsIgnoredType(data.GetType()) && Attribute.IsDefined(data.GetType(), TYPE_QUESTSAVE) == false )
		{			
			Debug.LogError("Error in AddSaveData( \""+name+"\", ... ): When saving a component, use the [QuestSave] attribute on the class, and any variables you wish to save");
		}
		if ( m_customSaveData.Exists( item => string.Equals( item.m_name, name ) ) )
		{
			Debug.LogWarning("Save data already exists for "+name+", Call UnregisterSaveData first for safety. Item will be overwritten");
			m_customSaveData.RemoveAll( item=> string.Equals( item.m_name, name ) );
		}
		CustomSaveData newData = new CustomSaveData()
		{
			m_name = name,
			m_data = data,
			CallbackOnPostRestore = OnPostRestore
		};
		m_customSaveData.Add(newData);
	}

	public void RemoveSaveData(string name)
	{
		m_customSaveData.RemoveAll( item=> string.Equals( item.m_name, name ) );
	}

	// Retrieves save data for all slots, loads it if it doesn't already exist
	public List<QuestSaveSlotData> GetSaveSlotData() 
	{ 
		if ( m_loadedSaveSlots == false )
			LoadSaveSlotData();
		return m_saveSlots; 
	}

	public QuestSaveSlotData GetSaveSlot(int id)
	{
		if ( m_loadedSaveSlots == false )
			LoadSaveSlotData();
		return m_saveSlots.Find(slot=> slot.m_slotId == id);
	}

	public bool Save(int slot, string displayName, int version, Dictionary<string, object> data, Texture2D image = null)
	{
		bool result = Save(FILE_NAME_START+slot+FILE_NAME_EXTENTION, displayName,version,data,image);
		ReloadSaveSlotData(slot);
		return result;
	}

	public bool Save(string fileName, string displayName, int version, Dictionary<string, object> data, Texture2D image = null)
	{	

		#if UNITY_SWITCH
		// TODO: Implement save/load on switch
		bool isSwitch = true;
		if ( isSwitch )
			return false;
		#endif

		bool success = false;

		// Add the registered data
		foreach( CustomSaveData customSaveData in m_customSaveData )
			data.Add(customSaveData.m_name+'%', customSaveData.m_data); // adding '%' to mostly ensure it's unique
				
		Stream fStream = null;
		Stream cryptoStream = null;
		
		QuestSaveSurrogateSelector.StartLogSave();

		try
		{
		
			#if LOG_TIME
				QuestUtils.StopwatchStart();
			#endif		
					
			fStream = File.Open(GetSaveDirectory()+fileName, FileMode.Create);
			
			BinaryFormatter bformatter = new BinaryFormatter();
			bformatter.Binder = new VersionDeserializationBinder(); 	

			// Serialize 'header' (unencrypted version and slot information)
			bformatter.Serialize(fStream, VERSION_CURRENT); // QuestSaveManager version
			bformatter.Serialize(fStream, version);			// Game Version
			bformatter.Serialize(fStream, displayName);
			bformatter.Serialize(fStream, Utils.GetUnixTimestamp());			
			{
				// Save image				
				if ( image == null )
				{
					bformatter.Serialize(fStream, false);	// no image
				}
				else 
				{
					bformatter.Serialize(fStream, true);	// Set flag to show there's an image

					// from https://docs.unity3d.com/ScriptReference/ImageConversion.EncodeToPNG.html
					{
						byte[] bytes = image.EncodeToPNG();
						//bformatter.Serialize(stream,bytes.Length);
						bformatter.Serialize(fStream,bytes);
					}
				}
			}

			// Construct SurrogateSelectors object to serialize unity structs
			
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = NOTHING_TO_SEE_HERE;
			des.IV = JUST_A_REGULAR_VARIABLE;
			
			cryptoStream = new CryptoStream(fStream, des.CreateEncryptor(), CryptoStreamMode.Write);

			SurrogateSelector surrogateSelector = new SurrogateSelector();					
			surrogateSelector.ChainSelector( new QuestSaveSurrogateSelector() );
			bformatter.SurrogateSelector = surrogateSelector;
			
			// Serialize encrypted data

			#if CACHE_SAVE_DATA
				#if LOG_CACHE_DATA
					string dbg = "";
					int resaved=0;
				#endif

				using ( MemoryStream mStream = new MemoryStream(128) )
				{
					// Serialise the number of items in the list
					bformatter.Serialize(cryptoStream, data.Count);
					foreach ( KeyValuePair<string, object> pair in data )	
					{
						// For each item, serialise the key, and the object.
						// They're done separately so we can avoid re-serialising things that definitely haven't changed (since it's kinda slow)
						// This gets save time from 0.7 to 0.2 sec in debug

						// Serialise the key
						bformatter.Serialize(cryptoStream, pair.Key as string);
						byte[] bytes = null;
						if ( pair.Value is IQuestSaveCachable )
						{
							IQuestSaveCachable cachable = pair.Value as IQuestSaveCachable;
							if ( cachable.SaveDirty || m_cachedSaveData.ContainsKey(pair.Key) == false )
							{
								//using ( MemoryStream mStream = new MemoryStream() ) // moved to outside so not reallocing so much
								{
									bformatter.Serialize(mStream, pair.Value as object);
									bytes = mStream.ToArray();
									m_cachedSaveData[pair.Key] = bytes;
									cachable.SaveDirty = false;		
									#if LOG_CACHE_DATA
										dbg += "\n"+pair.Key;
										resaved++;
									#endif
								}
								mStream.SetLength(0); // reset stream
							}
							else 
							{
								bytes = m_cachedSaveData[pair.Key];
							}
						}
						else
						{					
							//using ( MemoryStream mStream = new MemoryStream() ) // moved to outside so not reallocing so much
							{
								bformatter.Serialize(mStream, pair.Value);														
								bytes = mStream.ToArray();
							}
							mStream.SetLength(0); // reset stream
						}						

						bformatter.Serialize(cryptoStream, bytes);

					}

				}
				#if LOG_CACHE_DATA
					Debug.Log($"Re-saving {resaved} items:\n{dbg}");	
				#endif

			#else
				
				// The old way to save was just to save the whole dictionary as one thing
				bformatter.Serialize(cryptoStream, data);

			#endif
			
			#if LOG_TIME
				QuestUtils.StopwatchStop("Save: ");
			#endif
			cryptoStream.Close();
			success = true;	
		}
		catch( Exception e )
		{
			m_log = "Save failed: "+e.ToString ();	
			success = false;	
		}
		finally
		{
			if ( cryptoStream != null )
				cryptoStream.Close();
			if ( fStream != null )
				fStream.Close();		
		}
		TempPrintLog();

		return success;
	}


	// Restore save from a slot. 
	// (slot 4 = save4.sav)
	// Data gets inserted into the string,object dictionary
	// If the version required is bigger than the loaded version, the save file won't load. Avoid for released games!
	// You can use the retrieved version to work out if you need to do specific translation to stuff if things have changed
	public bool RestoreSave(int slot, int versionRequired, out int version, out Dictionary<string, object> data )
	{
		return RestoreSave(FILE_NAME_START+slot+FILE_NAME_EXTENTION, versionRequired, out version, out data, slot);
	}
	// Restore save from a file name
	public bool RestoreSave(string fileName, int versionRequired, out int version, out Dictionary<string, object> data )
	{
		return RestoreSave(fileName, versionRequired,out version, out data, -1);
	}

	bool RestoreSave(string fileName, int versionRequired, out int version, out Dictionary<string, object> data, int slot )
	{
		bool success = false;
		data = null;
		version = -1;
		int saveVersion = -1;

		#if UNITY_SWITCH 
		// TODO: Implement save/load on switch
		bool isSwitch = true;
		if ( isSwitch )
			return false;
		#endif
			
		QuestSaveSurrogateSelector.StartLogLoad();
		
		// Get the save slot. If it doesn't exist, try to load anyway (for settings)
		QuestSaveSlotData slotData = new QuestSaveSlotData();
		if ( slot >= 0 )
		{
			slotData = GetSaveSlot(slot);
			if ( slotData == null )
				slotData = new QuestSaveSlotData() { m_slotId = slot };			
		}

		Stream stream = null;
		Stream cryptoStream = null;
		try
		{
			stream = File.Open(GetSaveDirectory()+fileName, FileMode.Open);		    
			
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = NOTHING_TO_SEE_HERE;
			des.IV = JUST_A_REGULAR_VARIABLE;

			cryptoStream = new CryptoStream(stream, des.CreateDecryptor(), CryptoStreamMode.Read);
			
			BinaryFormatter bformatter = new BinaryFormatter();
			bformatter.Binder = new VersionDeserializationBinder(); 

			// Deserialize unencrtypted version and slot information (not encrypted)
			saveVersion = (int)bformatter.Deserialize(stream); // QuestSaveManager version
			if ( saveVersion < VERSION_REQUIRED )
			{
				throw new Exception("Incompatible save version. Required: " + VERSION_REQUIRED + ", Found: " + saveVersion);
			}

			DeserializeSlotData(slotData, bformatter, stream, saveVersion);
			
			version = slotData.m_version;
			if ( version < versionRequired )
			{
				throw new Exception("Incompatible game save version. Required: " + versionRequired + ", Found: " + version);
			}

			#if LOG_TIME
				QuestUtils.StopwatchStart();
			#endif
						
			SurrogateSelector ss = new SurrogateSelector();
			ss.ChainSelector( new QuestSaveSurrogateSelector() );
			bformatter.SurrogateSelector = ss;
			
			// deserialize the data
			if ( saveVersion < 3 )					
			{
				// Older version saved all objects in single dictionary. But that meant couldn't cache anything, so consecutive saves were slower.
				data = bformatter.Deserialize(cryptoStream) as Dictionary<string, object>;
			}
			else 
			{			
				// The new way of saving, where we have each object serialised separately			
				int dictionarySize = (int)bformatter.Deserialize(cryptoStream);
				data = new Dictionary<string, object>(dictionarySize);
				/* #Optimisation test /
				using ( MemoryStream memStream = new MemoryStream() )
				/**/
				{
					for ( int i = 0; i < dictionarySize; ++i )
					{
						string key = bformatter.Deserialize(cryptoStream) as string;
						byte[] bytes = bformatter.Deserialize(cryptoStream) as byte[];
												
						/* #Optimisation test: Write bytes to memstream, then reset position (try ing to minimise allocs, but maybe just as good to new memory streams each time...) /
						memStream.SetLength(0);
						memStream.Write(bytes,0,bytes.Length);
						memStream.Position = 0;
						/**/
						using (MemoryStream memStream = new MemoryStream(bytes) )
						/**/
						{					
							object value = bformatter.Deserialize(memStream) as object;
							data.Add(key,value);	

							// Mark data as no longer dirty since we just loaded it
							if ( value is IQuestSaveCachable )
							{
								(value as IQuestSaveCachable).SaveDirty=false;
								m_cachedSaveData[key] = bytes;
							}
						}
					}
				}
			}
			
			// Pull out the custom data we want
			object loadedCustomSaveData;
			foreach( CustomSaveData customSaveData in m_customSaveData )
			{	
				if ( data.TryGetValue(customSaveData.m_name+'%', out loadedCustomSaveData) )
				{	
					CopyCustomSaveDataFields(customSaveData.m_data, loadedCustomSaveData);				
				}
			}
			
			#if LOG_TIME
				QuestUtils.StopwatchStop("Load: ");
			#endif

			// Call post restore callback - NB: this is BEFORE save is restored in PQ! Wrong place to do it!
			/* Now moved to OnPostRestore, which is called from PowerQuestDrifter
			foreach( CustomSaveData customSaveData in m_customSaveData )
			{
				if ( customSaveData.CallbackOnPostRestore != null )
					customSaveData.CallbackOnPostRestore.Invoke();
			}
			*/

			success = true;
			 
		}
		catch( Exception e )
		{
			if ( (e is FileNotFoundException) == false )
				m_log = "Load failed: "+e.ToString ();
			success = false;
		}
		finally
		{
			try 
			{
				if ( cryptoStream != null )
					cryptoStream.Close();
			}
			catch( Exception e )
			{
				m_log += "\nLoad failed: "+e.ToString ();
				success = false;
			}
			
			if ( stream != null )
				stream.Close();			
		}
		TempPrintLog();

		return success;
	}
	

	void DeserializeSlotData( QuestSaveSlotData slotData, BinaryFormatter bformatter, Stream stream, int saveVersion )
	{	
		if ( slotData == null )
			return;
			
		// Save file metadata- Description, timestamp, image		
		slotData.m_version = (int)bformatter.Deserialize(stream);		
		slotData.m_description = (string)bformatter.Deserialize(stream);
		slotData.m_timestamp = (int)bformatter.Deserialize(stream);
		bool hasImage = saveVersion >= 2 && (bool)bformatter.Deserialize(stream); // images added in save version 2
		if ( hasImage )
		{
			byte[] bytes = (byte[])bformatter.Deserialize(stream);	// read in texture bytes
			if ( bytes != null && bytes.Length > 0 )
			{
				if ( slotData.m_image == null )
					slotData.m_image = new Texture2D(2,2);
				slotData.m_image.LoadImage(bytes,false);
			}
		}			
	}

	//  This function must be called after restoring data, from the caller of RestoreSave
	public void OnPostRestore()
	{
		// Call post restore callback
		foreach( CustomSaveData customSaveData in m_customSaveData )
		{
			if ( customSaveData.CallbackOnPostRestore != null )
				customSaveData.CallbackOnPostRestore.Invoke();
		}
	}

	static readonly BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;	
	static readonly Type TYPE_QUESTSAVE = typeof(QuestSaveAttribute);
	static readonly Type TYPE_QUESTDONTSAVE = typeof(QuestDontSaveAttribute);

	// Copies properties and variables from one class to another
	public static void CopyCustomSaveDataFields<T>(T to, T from)
	{
		System.Type type = to.GetType();
		if (type != from.GetType()) return; // type mis-match

		
		FieldInfo[] finfos = type.GetFields(BINDING_FLAGS);
		
		bool manualSaveType = Attribute.IsDefined(type, TYPE_QUESTSAVE);
		foreach (var finfo in finfos) 
		{
			
			#if ENABLE_DONTSAVE_ATTRIB
			if ( Attribute.IsDefined(finfo, TYPE_QUESTDONTSAVE)	&& (manualSaveType == false || Attribute.IsDefined(finfo, TYPE_QUESTSAVE) ) )
			#else
			if ( (manualSaveType == false || Attribute.IsDefined(finfo, TYPE_QUESTSAVE) ) )
			#endif
			{
				finfo.SetValue(to, finfo.GetValue(from));
			}
		
		}
	}

	public bool DeleteSave(int slot)
	{
		#if UNITY_SWITCH 
		// TODO: Implement save/load on switch
		bool isSwitch = true;
		if ( isSwitch )
			return false;
		#endif

		bool result = true;
		//
		try
		{
			File.Delete(GetSaveDirectory()+FILE_NAME_START+slot+FILE_NAME_EXTENTION);
		}
		catch (Exception e)
		{
			m_log = "Delete failed: "+e.ToString ();	
			result = false;
		}

		// Remove the save slot
		m_saveSlots.RemoveAll(item=>item.m_slotId == slot);

		TempPrintLog();
		return result;
	}


	#endregion
	#region Private Functions

	bool LoadHeader(QuestSaveSlotData slotData)
	{
	
		#if UNITY_SWITCH 
		// TODO: Implement save/load on switch
		bool isSwitch = true;
		if ( isSwitch )
			return;
		#endif

		bool result = false;

		if ( slotData == null )
			return false;
		int slotId = slotData.m_slotId;

		string path = GetSaveDirectory()+FILE_NAME_START+slotId+FILE_NAME_EXTENTION;
		Stream stream = null;
		try
		{
			stream = File.Open(path, FileMode.Open);		    

			BinaryFormatter bformatter = new BinaryFormatter();
			bformatter.Binder = new VersionDeserializationBinder();

			int saveVersion = (int)bformatter.Deserialize(stream);	// NB: save version not encrypted
			if ( saveVersion >= VERSION_REQUIRED )
			{
				DeserializeSlotData(slotData,bformatter,stream,saveVersion);
				result = true;
			}
			else
			{
				m_log = "Incompatible save version. Required: " + VERSION_REQUIRED + ", Found: " + saveVersion;
			}
		}
		catch( Exception e )
		{
			m_log = "Load failed: "+e.ToString ();
		}
		finally
		{
			if ( stream != null )
			{
				stream.Close();
			}
		}
		return result;
	}

	void ReloadSaveSlotData(int slotId)
	{
		if ( m_loadedSaveSlots == false )
		{
			LoadSaveSlotData();
			return;
		}

		QuestSaveSlotData slotData = GetSaveSlot(slotId);
		bool newSlot = slotData == null;
		if ( newSlot )
			slotData = new QuestSaveSlotData(){m_slotId=slotId};
		bool success = LoadHeader(slotData);
		
		if ( newSlot && success )
			m_saveSlots.Add(slotData);
		if ( newSlot == false && success == false ) 
			m_saveSlots.Remove(slotData); // Remove slot since it didn't reload
	}

	// Loads first bit of each save
	// Searches for file names of format save*.sav
	// Creates slot, and reads in displayname, timestamp, version information
	// If zero or greater is passed as specificSlotOnly, only that slot will be loaded
	void LoadSaveSlotData()
	{	
		#if UNITY_SWITCH 
		// TODO: Implement save/load on switch
		bool isSwitch = true;
		if ( isSwitch )
			return;
		#endif
	
		if ( m_loadedSaveSlots )
			Debug.LogWarning("Save slots should only be loaded once. Use ReloadSaveSlotData()");

		string[] sourceFileNames = Directory.GetFiles(Path.GetFullPath(GetSaveDirectory()),FILE_NAME_WILDCARD);
		foreach ( string path in sourceFileNames )
		{
			QuestSaveSlotData slotData = new QuestSaveSlotData();
			
			string idString = Path.GetFileNameWithoutExtension(path).Substring(4);
			if ( int.TryParse(idString, out  slotData.m_slotId ) == false )
			{
				m_log = "Couldn't parse id from path: "+path;
			}
			else 
			{
				if ( LoadHeader(slotData) )
					m_saveSlots.Add(slotData);
			}
		}

		m_loadedSaveSlots = true;
	}

	string GetSaveDirectory()
	{		
		// For OSX - point to persistent data path (eg: a place on osx where we can store svae files)
		#if UNITY_2017_1_OR_NEWER
		if ( Application.platform == RuntimePlatform.OSXPlayer )
		#else
		if ( Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXDashboardPlayer )
		#endif
		{		
			return Application.persistentDataPath+"/";
		}
		// For Other platforms, store saves in game directory
		return "./";
	}


	// Can get rid of this later.
	void TempPrintLog()
	{
		if( string.IsNullOrEmpty(m_log) == false )
		{
			Debug.Log(m_log);
			m_log = null;
		}
		QuestSaveSurrogateSelector.PrintLog();
	}

	// === This is required to guarantee a fixed serialization assembly name, which Unity likes to randomize on each compile
	// Do not change this
	public sealed class VersionDeserializationBinder : SerializationBinder 
	{ 
		public override Type BindToType( string assemblyName, string typeName )
		{ 
			if ( !string.IsNullOrEmpty( assemblyName ) && !string.IsNullOrEmpty( typeName ) ) 
			{ 
				Type typeToDeserialize = null; 
				
				assemblyName = Assembly.GetExecutingAssembly().FullName; 
				
				// The following line of code returns the type. 
				typeToDeserialize = Type.GetType( String.Format( "{0}, {1}", typeName, assemblyName ) ); 
				
				return typeToDeserialize; 
			} 
			
			return null; 
		} 
	}


}


#endregion
#region Class: QuestSaveSurrogateSelector

// Adapted from http://codeproject.cachefly.net/Articles/32407/A-Generic-Method-for-Deep-Cloning-in-C
sealed class QuestSaveSurrogateSelector  : ISerializationSurrogate , ISurrogateSelector
{
	/*
		This class is used to generically serialise classes that don't have a specific serialisation method.
		It has specific stuff for the way I want to save stuff in this unity quest system thing.
			It ignores (doesn't serialise) some types: GameObject, MonoBehaviour, Behaviour (for now)
			It ignores exceptions when deserialising so if a variable has been added/deleted, it'll still serialize the rest of the data

		Implementing ISurrogateSelector means that we can choose what things we are able to serialise
			Otherwise you'd have to specifically say you can serialise each class/struct (eg: Vector2, Vector3, CharcterBob, CharacterJon)
			If fields can be serialised already without our help,  we return null from GetSurrogate

		Implementing ISerializationSurrogate means that we can do the actual serialisation (save and load) for each of the types.
			We're using reflection to serialize most fields
			We can choose to ignore serializing certain types (set in IsIgnoredType())
				For QuestSaveMAnager, we're not serialising any gameobjects/components or unity behaviour things. Those should be re-created on room/scene changes anyway.
	*/

	// These binding flags mean public and non-public data is copied including instance data, but not including data in base classes (eg: object)
	static readonly BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;	

	static readonly Type TYPE_QUESTSAVE = typeof(QuestSaveAttribute);
	static readonly Type TYPE_QUESTDONTSAVE = typeof(QuestDontSaveAttribute);
	static readonly Type TYPE_COMPILERGENERATED = typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute);

	public static System.Text.StringBuilder s_log = new StringBuilder();

	public static void StartLogSave()
	{
		#if LOG_DATA		
			QuestSaveSurrogateSelector.s_log.Clear();
			QuestSaveSurrogateSelector.s_log.Append("**Saving**\n\n");
		#endif
	}
	
	public static void StartLogLoad()
	{
		#if LOG_DATA
			QuestSaveSurrogateSelector.s_log.Clear();
			QuestSaveSurrogateSelector.s_log.Append("**Loading**\n\n");
		#endif
	}
	public static void PrintLog()
	{		
		#if LOG_DATA
		if ( s_log.Length > 1 )		
		{
			File.WriteAllText("SaveLog.txt",QuestSaveSurrogateSelector.s_log.ToString());
			Debug.Log(QuestSaveSurrogateSelector.s_log.ToString());
		}
		QuestSaveSurrogateSelector.s_log.Clear();
		#endif
	}

	//
	// Implementing ISurrogateSelector
	//

	// This is what we'll use to hold the nextSelector in the chain
	ISurrogateSelector m_nextSelector;

	// Sets the selector
	public void ChainSelector( ISurrogateSelector selector)
	{
		  this.m_nextSelector = selector;
	}

	// Gets the next selectr from the chain
	public ISurrogateSelector GetNextSelector()
	{
		  return m_nextSelector;
	}

	public ISerializationSurrogate GetSurrogate( Type type, StreamingContext context, out ISurrogateSelector selector)
	{			
		if ( IsIgnoredType(type) )
		{
				
			#if LOG_DATA_SURROGATE
				s_log.Append("\nIgnored: ");
				s_log.Append(type.ToString());
			#endif			
			selector = this;
			return this;
		}
		else if (IsKnownType(type))
		{
			#if LOG_DATA_SURROGATE
				s_log.Append("\nKnown: ");				
				s_log.Append(type.ToString());
			#endif
			selector = null;
			return null;
		}
		else if (type.IsClass )
		{
			#if LOG_DATA_SURROGATE		
				s_log.Append("\nClass: ");
				s_log.Append(type.ToString());		
			#endif
			selector = this;
			return this;
		}
		else if (type.IsValueType)
		{
			#if LOG_DATA_SURROGATE
				s_log.Append("\nValue: ");
				s_log.Append(type.ToString());		
			#endif
			selector = this;
			return this;
		}
		else
		{
			#if LOG_DATA_SURROGATE
				s_log.Append("\nUnknown: ");
				s_log.Append(type.ToString());		
			#endif
			selector = null;
			return null;
		}
	}

	//
	// Implementing ISerializationSurrogate
	//

	// Save	
	public void GetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
	{

		try 
		{
			Type type = obj.GetType();

			// Handle Vectors/colors seperately, since we do thousands of them, so doing this whole thing just for those is quite expensive
			if ( type == typeof(Vector2) || type == typeof(Color) )
			{
				FieldInfo[] fis = type.GetFields( BINDING_FLAGS );
				foreach (var fi in fis)
				{
					info.AddValue(fi.Name, fi.GetValue(obj));
				}			
				return;
			}

			// If the type has the "QuestSave" attribute, then only serialize fields with it that also have that attribute
			bool manualType = Attribute.IsDefined(type, TYPE_QUESTSAVE);

			
			// Don't deep copy ignored classes
			if ( IsIgnoredType(type) && manualType == false ) 
			{
				#if LOG_DATA					
					s_log.Append("\n\nIgnored: ");
					s_log.Append(obj.ToString());	
				#endif
				return;
			}

			#if LOG_DATA					
				s_log.Append("\n\nObject: ");
				s_log.Append(obj.ToString());	
				if (manualType) s_log.Append("  (Manual)");
			#endif
				
			FieldInfo[] fieldInfos = type.GetFields( BINDING_FLAGS );

			foreach (var fi in fieldInfos)
			{
				if ( manualType && Attribute.IsDefined(fi, TYPE_QUESTSAVE) == false ) // Some fields have the [QuestSave] attribute, but not this one.
				{
					// NO-OP
					// Debug.Log("Ignored Manual: "+fi.Name);
					#if LOG_DATA					
						s_log.Append("\n        Ignored Manual ");
						s_log.Append(fi.Name.ToString());
					#endif
				}
				#if ENABLE_DONTSAVE_ATTRIB
				else if ( IsIgnoredType(fi.FieldType) || Attribute.IsDefined(fi, TYPE_QUESTDONTSAVE) )
				#else 
				else if ( IsIgnoredType(fi.FieldType) /*|| Attribute.IsDefined(fi, TYPE_QUESTDONTSAVE)*/ )
				#endif
				{
					// NO-OP
					#if LOG_DATA					
						s_log.Append("\n        Ignored ");
						s_log.Append(fi.Name.ToString());
					#endif
				}
				else if (IsKnownType(fi.FieldType) )
				{
					if ( fi.Name.Length > 0 && fi.Name[0] == '$' )
						return;
					// Debug.Log("Known: "+fi.Name);
					#if LOG_DATA					
						s_log.Append("\n        ");
						s_log.Append(fi.Name.ToString());
					#endif
					info.AddValue(fi.Name, fi.GetValue(obj));
				}
				else if (fi.FieldType.IsClass || fi.FieldType.IsValueType)
				{
					// Debug.Log("Unknown class/value: "+fi.Name);
					#if LOG_DATA					
						s_log.Append("\n        ");
						s_log.Append(fi.Name.ToString());
					#endif
					info.AddValue(fi.Name, fi.GetValue(obj));
				}
				else 
				{
					//Debug.Log("Unknown: "+fi.Name);
					#if LOG_DATA					
						s_log.Append("\n        Unknown ");
						s_log.Append(fi.Name.ToString());
					#endif
				}
			}
		}
		#if LOG_DATA
		catch ( Exception e )
		{			
			QuestSaveSurrogateSelector.s_log.Append("\n    Exception: ");
			QuestSaveSurrogateSelector.s_log.Append( e.ToString() );			
		}
		#else
		catch
		{
			// Gracefully newly added values, they just get ignored.
		}
		#endif
	}


	// Load
	public object SetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.ISurrogateSelector selector)
	{
		try 
		{
			Type type = obj.GetType();

			// Handle Vectors/colors seperately, since we do thousands of them, so doing this whole thing just for those is quite expensive
			if ( type == typeof(Vector2) || type == typeof(Color) )
			{
				FieldInfo[] fis = type.GetFields( BINDING_FLAGS );
				foreach (var fi in fis)
				{
					fi.SetValue(obj, info.GetValue(fi.Name, fi.FieldType));
				}			
				return obj;
			}

			// If the type has the "QuestSave" attribute, then only serialize fields with it that also have that attribute
			bool manualType = Attribute.IsDefined(type, TYPE_QUESTSAVE);			

			// Don't deep copy ignored classes
			if ( IsIgnoredType(type) && manualType == false )
			{
				return obj;
			}
			//Debug.Log(type.ToString());

			FieldInfo[] fieldInfos = type.GetFields( BINDING_FLAGS );

			foreach (var fi in fieldInfos)
			{
				if ( manualType && Attribute.IsDefined(fi, TYPE_QUESTSAVE) == false ) // Some fields have the [QuestSave] attribute, but not this one.
				{
					// NO-OP
					//Debug.Log("Ignored Manual: "+fi.Name);
				}
				else if ( IsIgnoredType(fi.FieldType) ) 
				{
					// NO-OP
					 //Debug.Log("Ignored: "+fi.Name);
				}
				else if (IsKnownType(fi.FieldType))
				{
					//var value = info.GetValue(fi.Name, fi.FieldType);

					if (IsNullableType(fi.FieldType))
					{
						//Debug.Log("Known Nullifiable: "+fi.Name);
						// Nullable<argumentValue>
						Type argumentValueForTheNullableType = GetFirstArgumentOfGenericType( fi.FieldType);//fi.FieldType.GetGenericArguments()[0];
						fi.SetValue(obj, info.GetValue(fi.Name, argumentValueForTheNullableType));
					}
					else
					{
						//Debug.Log("Known non-Nullifiable: "+fi.Name);
						fi.SetValue(obj, info.GetValue(fi.Name, fi.FieldType));
					}

				}
				else if (fi.FieldType.IsClass || fi.FieldType.IsValueType)
				{
					//Debug.Log("class: "+fi.Name);
					fi.SetValue(obj, info.GetValue(fi.Name, fi.FieldType));
				}
			}
		}
		#if LOG_DATA
		catch ( System.Exception e )
		{
			QuestSaveSurrogateSelector.s_log.Append("\n    Exception: ");
			QuestSaveSurrogateSelector.s_log.Append( e.ToString() );
		}
		#else
		catch
		{
			// Gracefully handle missing values, they just get ignored.
		}
		#endif

		return obj;
	}

	//
	// Helper functions
	//
	
	static readonly Type STRING_TYPE = typeof(string);

	// Determines whether this instance is ignored type the specified type. Ignored types aren't serialised in or out.
	public static bool IsIgnoredType(Type type)
	{
		/* 
			The save system assumes that references to game objects and other components will be recreated after loading
		 	But other data will all automatically be saved
		 	Other items might need to be added to this list
		*/
		return type == typeof(IEnumerator) 	
		   || ( type != STRING_TYPE && type.IsClass
				&&  ( type == typeof(GameObject)
			       || type == typeof(Coroutine)
			       || type == typeof(AudioHandle)
			       || type.IsSubclassOf(typeof(Component))
				   || type.IsSubclassOf(typeof(Texture))
			       || type.IsSubclassOf(typeof(MulticastDelegate)) // Eg: Action, Action<object>, Action<object, object> etc etc
			       || Attribute.IsDefined(type, TYPE_COMPILERGENERATED) 
				   /*#if ENABLE_DONTSAVE_ATTRIB
				   || Attribute.IsDefined(type, TYPE_QUESTDONTSAVE)
				   #endif*/
			   )
			);
	}


	// Known types can be serialised already and don't need this serializationSurrogate to be saved/loaded (primitive classes, things marked serialisable)
	bool IsKnownType(Type type)
	{	
		#if LOG_DATA_SERIALIZEABLE
			return type == STRING_TYPE || type.IsPrimitive; // don't treat serializables as "known" so they're handled manually and can be logged.
		#endif
		return type == STRING_TYPE || type.IsPrimitive || type.IsSerializable;
	
	}

	// Determines whether this instance is nullable type the specified type.
	// I think this is used because if something's null it's hard to tell it's type, so that has to be serialized... or something...
	bool IsNullableType(Type type)
	{
		if (type.IsGenericType)
			return type.GetGenericTypeDefinition() == typeof(Nullable<>);
		return false;
	}

	// Dont know the function of this :)
	Type GetFirstArgumentOfGenericType(Type type)
	{
		return type.GetGenericArguments()[0];
	}

}

}

#endregion
