//#define LOG_TIME
//#define LOG_DATA
//#define LOG_CACHE_DATA
#if LOG_DATA 
	//#define LOG_DATA_SERIALIZEABLE // More verbose, shows all serializables, not just custom classes
	//#define LOG_DATA_SURROGATE
#endif

// Serialize data to memstream before its saved to disk. Necessary for console ports (added in v5)
#define ENABLE_MEMSTREAM
// Compress data instead of encrypting (added in v5)
#define ENABLE_COMPRESSION
// Cache data speeds up consecutive saves, at cost of restore being slower, and save file being bigger (added in v3)
#define CACHE_SAVE_DATA 
// Check if saved objects are IQuestCachable and EverDirty before saving. Applies to props, hotspots, regions, so have to be more careful about marking them as dirty. (added in v5)
#define IGNORE_NONDIRTY_CACHABLES
// Checking for [DontSave] makes saving potentially slightly slower, but its useful so decided its worth it
#define ENABLE_DONTSAVE_ATTRIB

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// for saving

using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;
#if ENABLE_COMPRESSION
using Compression = System.IO.Compression;
#endif
using PowerTools;

namespace PowerTools.Quest
{

// Attribute used for including global enums in autocomplete
[AttributeUsage(AttributeTargets.All)]
public class QuestSaveAttribute : System.Attribute
{
	public QuestSaveAttribute(){}
}

// Attribute used to specifically disable saving variables
[AttributeUsage(AttributeTargets.All)]
public class QuestDontSaveAttribute : System.Attribute
{
	public QuestDontSaveAttribute(){}
}

#region Class: Save Slot Data

[System.Serializable]
public class QuestSaveSlotData
{
	public QuestSaveSlotData(){}
	public QuestSaveSlotData(int slotId) { m_slotId = slotId; }
	public QuestSaveSlotData(int slotId, int version, int timestamp, string description, Texture2D image)
	{
		m_slotId = slotId;
		m_version = version;
		m_timestamp = timestamp;
		m_description = description;
		m_image = image;
	}
	
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
	bool SaveDirtyEver {get;}
}

// Save file strategy interface- gives layer of indirection to file io so can swap it out on different platforms (switch/ps/xbox,etc)
public interface ISaveIoStrategy 
{
	// Retreive a log, in case of error that needs to be reported
	string Log { get; }
	// Retreive status of the save file system. Mainly useful for async saving
	QuestSaveManager.eStatus Status{ get; }	
	string GetSaveDirectory();
	// Fill an array with the save file names from the SaveDirectory
	string[] ReadFileNames();
	// Write bytes to a file. Return success
	bool WriteToFile(string fileName, byte[] bytes);
	// Read  bytes from a file. Return success
	byte[] ReadFromFile(string fileName);
	// Delete a file
	bool DeleteFile(string fileName);
}

public class QuestSaveManager
{

	#endregion
	#region Definitions

	public enum eStatus
	{ 
		Saving,
		Loading,
		None,
		Complete,
		Error
	}

	// Version and version requirement for the save manager. There's a seperate one used for the "game"	
	static readonly int VERSION_CURRENT = 5;	
	static readonly int VERSION_REQUIRED = 5;

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

	public static readonly string FILE_NAME_START = "Save";
	public static readonly string FILE_NAME_EXTENTION = ".sav";
	public static readonly string FILE_NAME_WILDCARD = FILE_NAME_START+"*"+FILE_NAME_EXTENTION;

	
	#endregion
	#region Variables
		
	List<QuestSaveSlotData> m_saveSlots = new List<QuestSaveSlotData>();
	string m_log = string.Empty;	// Debug text log, can query error messages
	bool m_loadedSaveSlots = false; // Flag set true when save slots have been loaded

	List< CustomSaveData > m_customSaveData = new List< CustomSaveData >();
	//List< CustomSaveVars > m_customSaveVars = new List< CustomSaveVars >();
	
	// Serialized bytes, cached so they don't have to be serialized again (since that's so slow)
	Dictionary<string, byte[]> m_cachedSaveData = new Dictionary<string, byte[]>();
	
	ISaveIoStrategy m_ioStrategy = new SaveIoStrategyPC(); // default to PC.
	

	#endregion
	#region Public Functions

	// Future proofing for console ports really, Ps4 at least needs file io to be async...
	public bool Ready => (m_ioStrategy.Status != eStatus.Saving && m_ioStrategy.Status != eStatus.Loading);
	public bool Busy => Ready == false;
	public eStatus Status => m_ioStrategy.Status;
	public string Log => m_log;

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
		return Save(FILE_NAME_START+slot+FILE_NAME_EXTENTION, displayName,version,data,image,slot);
	}

	public bool Save(string fileName, string displayName, int version, Dictionary<string, object> data, Texture2D image = null, int slotId = -1)
	{	
		bool success = false;
		
		// Load save slots first if never did it
		if ( m_loadedSaveSlots == false )
			LoadSaveSlotData();

		// Add the registered data
		foreach( CustomSaveData customSaveData in m_customSaveData )
			data.Add(customSaveData.m_name+'%', customSaveData.m_data); // adding '%' to mostly ensure it's unique
				
		Stream stream = null;
		Stream encoderStream = null;
		
		QuestSaveSurrogateSelector.StartLogSave();

		// Create slot data- even if not using slots (in that case it's just not added to m_slotData list)
		QuestSaveSlotData slotData = new QuestSaveSlotData( slotId, version, Utils.GetUnixTimestamp(), displayName, image );

		try
		{
		
			#if LOG_TIME
				QuestUtils.StopwatchStart();
			#endif		
			
			#if ENABLE_MEMSTREAM
			// Save to memory stream, then disk
			stream = new MemoryStream(32*1024); // start with 30KB block allocated- gets expanded if save file is larger
			#else 
			// Save straight to file 
			stream = File.Open(GetSaveDirectory()+fileName, FileMode.Create);
			#endif
			
			BinaryFormatter bformatter = new BinaryFormatter();
			bformatter.Binder = new VersionDeserializationBinder(); 	

			// Serialize 'header' (unencrypted version and slot information)
			bformatter.Serialize(stream, VERSION_CURRENT);      // QuestSaveManager version
			bformatter.Serialize(stream, slotData.m_version);   // Game Version
			bformatter.Serialize(stream, slotData.m_description);
			bformatter.Serialize(stream, slotData.m_timestamp);			
			{
				// Save image				
				if ( slotData.m_image == null )
				{
					bformatter.Serialize(stream, false);	// no image
				}
				else 
				{
					bformatter.Serialize(stream, true);	// Set flag to show there's an image

					// from https://docs.unity3d.com/ScriptReference/ImageConversion.EncodeToPNG.html
					{
						byte[] bytes = slotData.m_image.EncodeToPNG();
						bformatter.Serialize(stream,bytes);
					}
				}
			}
			
			// Construct SurrogateSelectors object to serialize unity structs
			
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = NOTHING_TO_SEE_HERE;
			des.IV = JUST_A_REGULAR_VARIABLE;
			
			#if ENABLE_COMPRESSION
			encoderStream = new Compression.GZipStream(stream, Compression.CompressionLevel.Fastest,false);
			#else
			encoderStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write);			
			#endif

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
					bformatter.Serialize(encoderStream, data.Count);
					foreach ( KeyValuePair<string, object> pair in data )	
					{
						// For each item, serialise the key, and the object.
						// They're done separately so we can avoid re-serialising things that definitely haven't changed (since it's kinda slow)
						// This gets save time from 0.7 to 0.2 sec in debug

						// Serialise the key
						bformatter.Serialize(encoderStream, pair.Key as string);
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

						bformatter.Serialize(encoderStream, bytes);

					}

				}
				#if LOG_CACHE_DATA
					Debug.Log($"Re-saving {resaved} items:\n{dbg}");	
				#endif

			#else
				
				// The old way to save was just to save the whole dictionary as one thing
				bformatter.Serialize(encoderStream, data);

			#endif

			success = true;	
		}
		catch( Exception e )
		{
			m_log = "Save failed: "+e.ToString ();	
			success = false;	
		}
		finally
		{
			if ( encoderStream != null )
				encoderStream.Close();
			if ( stream != null )
				stream.Close();
		}
		
		#if ENABLE_MEMSTREAM		
		/* Save to memory stream, then disk */
		if ( success )
		{
			byte[] buffer = (stream as MemoryStream).ToArray();
			success = m_ioStrategy.WriteToFile(fileName, buffer);
		}
		#endif
		/**/
		
		#if LOG_TIME
			QuestUtils.StopwatchStop("Save: ");
		#endif

		if ( success && slotId >= 0 )
		{
			// Remove old data and add new
			RemoveSlotData(slotId);
			m_saveSlots.Add(slotData);
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
	public bool RestoreSave(string fileName, int versionRequired, out int version, out Dictionary<string, object> data, int slot = -1 )
	{	

		bool success = false;
		data = null;
		version = -1;
		int saveVersion = -1;
			
		if ( Ready == false )
		{ 
			Debug.LogError("Attempted to restore while saving. Check Save manager's Check Save Manager's 'Ready' property before attempting a restore");			
			return success;
		}
					
		QuestSaveSurrogateSelector.StartLogLoad();
		
		// Get the save slot. If it doesn't exist, try to load anyway (for settings)
		QuestSaveSlotData slotData = new QuestSaveSlotData();
		if ( slot >= 0 )
		{
			slotData = GetSaveSlot(slot);
			if ( slotData == null )
				slotData = new QuestSaveSlotData(slot);			
		}

		Stream stream = null;
		Stream encoderStream = null;
		try
		{	
			#if ENABLE_MEMSTREAM				
				byte[] buffer = m_ioStrategy.ReadFromFile(fileName);
				if ( buffer == null )
					throw new Exception("File not found: " + fileName);
				
				stream = new MemoryStream(buffer,0,buffer.Length);
			#else
				stream = File.Open(GetSaveDirectory()+fileName, FileMode.Open);		    
			#endif

			
			BinaryFormatter bformatter = new BinaryFormatter();
			bformatter.Binder = new VersionDeserializationBinder(); 

			// Deserialize unencrtypted version and slot information (not encrypted)
			saveVersion = (int)bformatter.Deserialize(stream); // QuestSaveManager version
			if ( saveVersion < VERSION_REQUIRED )
				throw new Exception("Incompatible save version. Required: " + VERSION_REQUIRED + ", Found: " + saveVersion);
			

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
			
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = NOTHING_TO_SEE_HERE;
			des.IV = JUST_A_REGULAR_VARIABLE;

			
			#if ENABLE_COMPRESSION
			encoderStream = new Compression.GZipStream(stream, Compression.CompressionMode.Decompress,false);
			#else
			encoderStream = new CryptoStream(stream, des.CreateDecryptor(), CryptoStreamMode.Read);
			#endif

			// deserialize the data
			#if CACHE_SAVE_DATA
			if ( saveVersion < 3 )					
			{
				// Older version saved all objects in single dictionary. But that meant couldn't cache anything, so consecutive saves were slower.
				data = bformatter.Deserialize(encoderStream) as Dictionary<string, object>;
			}
			else 
			{			
				// The new way of saving, where we have each object serialised separately			
				int dictionarySize = (int)bformatter.Deserialize(encoderStream);
				data = new Dictionary<string, object>(dictionarySize);
				/* #Optimisation test /
				using ( MemoryStream memStream = new MemoryStream() )
				/**/
				{
					for ( int i = 0; i < dictionarySize; ++i )
					{
						string key = bformatter.Deserialize(encoderStream) as string;
						byte[] bytes = bformatter.Deserialize(encoderStream) as byte[];
												
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
			#else
			data = bformatter.Deserialize(encoderStream) as Dictionary<string, object>;
			#endif
			
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
				if ( encoderStream != null )
					encoderStream.Close();
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
					slotData.m_image = new Texture2D(2,2); // create new one
				slotData.m_image.LoadImage(bytes,false);
			}
		}			
	}
	
	// Allows overriding file io used in save system. For porting to other platforms that can't use regular file io (switch, etc)
	public void SetSaveIoStrategy(ISaveIoStrategy strategy) { m_ioStrategy = strategy; }

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
			if ( (manualSaveType == false || Attribute.IsDefined(finfo, TYPE_QUESTSAVE) ) && Attribute.IsDefined(finfo, TYPE_QUESTDONTSAVE) == false )
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
		bool result = true;
		//
		try
		{
			m_ioStrategy.DeleteFile(FILE_NAME_START+slot+FILE_NAME_EXTENTION);
		}
		catch (Exception e)
		{
			m_log = "Delete failed: "+e.ToString ();	
			result = false;
		}

		// Remove the save slot
		RemoveSlotData(slot);

		TempPrintLog();
		return result;
	}


	#endregion
	#region Private Functions
	
	bool LoadHeader(QuestSaveSlotData slotData)
	{

		bool result = false;

		if ( slotData == null )
			return false;
		int slotId = slotData.m_slotId;

		string path = FILE_NAME_START+slotId+FILE_NAME_EXTENTION;
		Stream stream = null;
		try
		{
		
			#if ENABLE_MEMSTREAM
				byte[] buffer = m_ioStrategy.ReadFromFile(path);
				stream = new MemoryStream(buffer,0,buffer.Length);
			#else				
				stream = File.Open(GetSaveDirectory()+path, FileMode.Open);		    
			#endif
			

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
			slotData = new QuestSaveSlotData(slotId);
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
		if ( m_loadedSaveSlots )
			Debug.LogWarning("Save slots should only be loaded once. Use ReloadSaveSlotData()");
					
		string[] sourceFileNames = m_ioStrategy.ReadFileNames();
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
	
	void RemoveSlotData(int slotId)
	{
		QuestSaveSlotData oldSlot = GetSaveSlot(slotId);
		if ( oldSlot != null && oldSlot.m_image != null )	
			Texture2D.Destroy(oldSlot.m_image); // destroy old image to avoid potential mem leak
		m_saveSlots.RemoveAll(item=>item.m_slotId == slotId);
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
		else if ( IsKnownType(type))
		{	
			#if IGNORE_NONDIRTY_CACHABLES
			if ( !(type == STRING_TYPE) && !type.IsPrimitive && typeof(IQuestSaveCachable).IsAssignableFrom(type) )
			{
				// Check for items that need to be marked as "dirty" to save (eg: hotspots,regions,props)
				//Debug.Log("QuestSaveCachable surrogate for "+type.Name);
				selector = this;
				return this;
			}
			#endif
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
				
			
			#if IGNORE_NONDIRTY_CACHABLES
			if ( typeof(IQuestSaveCachable).IsAssignableFrom(type) && (obj as IQuestSaveCachable).SaveDirtyEver == false )
			{
				//Debug.Log("Ignored cachable "+type.Name);
				return;				
			}
			#endif

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
					if ( fi.Name.Length > 0 && fi.Name[0] == '$' ) // hrm. do we want to return? or just ingore it?!? is wouldn' thtis break any data after this field?
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

#if !UNITY_SWITCH

public class SaveIoStrategyPC : ISaveIoStrategy
{
	protected string m_log = string.Empty;
	protected QuestSaveManager.eStatus m_status = QuestSaveManager.eStatus.None;
	
	public string Log => m_log;
	public QuestSaveManager.eStatus Status=>m_status;

	public virtual string GetSaveDirectory()
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

	public string[] ReadFileNames()
	{
		return Directory.GetFiles(Path.GetFullPath(GetSaveDirectory()), QuestSaveManager.FILE_NAME_WILDCARD);
	}

	public virtual byte[] ReadFromFile(string fileName)
	{	
		byte[] bytes = null;
		Stream fStream = null;
		try
		{	
			fStream = File.Open(GetSaveDirectory()+fileName, FileMode.Open, FileAccess.Read);
			
			bytes = new byte[fStream.Length];
			int numBytesToRead = (int)fStream.Length;
			int numBytesRead = 0;
			while ( numBytesToRead > 0 )
			{
				int n = fStream.Read(bytes, numBytesRead,numBytesToRead);
				if ( n==0 )
					break; // eof
				
				numBytesRead += n;
				numBytesToRead -= n;
			}
		}
		finally
		{
			if ( fStream != null )
				fStream.Close();		
		}
		return bytes;
	}
	
	public virtual bool WriteToFile(string fileName, byte[] bytes)
	{
		// Now save memstream to disk
		bool success = false;
		Stream fStream = null;
		try
		{
			fStream = File.Open(GetSaveDirectory()+fileName, FileMode.Create);
			fStream.Write(bytes,0, bytes.Length);
			success = true;
		}
		finally
		{
			if ( fStream != null )
				fStream.Close();		
		}
		return success;
	}
	
	public bool DeleteFile(string fileName)
	{
		File.Delete(GetSaveDirectory()+fileName);
		return true;
	}
}

// Trying async saving... not enough benefit to be worth the extra complexity
public class SaveIoStrategyPCAsync : SaveIoStrategyPC
{
	// files currently being saved
	static List<string> s_saving = new List<string>();

	public override bool WriteToFile(string fileName, byte[] bytes)
	{		
		if ( s_saving.Contains(fileName) )
		{ 
			Debug.Log("Already saving");
			return false;
		}

		// Now save memstream to disk
		m_status = QuestSaveManager.eStatus.Saving;
		Task task = Task.Run(()=>WriteAsyncTask(GetSaveDirectory()+fileName, bytes));
		if ( task.IsFaulted || task.IsCanceled )
			return false; // not really reliable since its async		
		return true;
	}


	async Task WriteAsyncTask(string filePath, byte[] bytes)
	{ 
		s_saving.Add(filePath);
		Task task = WriteAsync(filePath, bytes);		
		await task;		
		s_saving.Remove(filePath);
		if ( task.IsCanceled || task.IsFaulted )
			m_status = QuestSaveManager.eStatus.Error;
		else if ( s_saving.Count == 0 )
			m_status = QuestSaveManager.eStatus.Complete;
	}

	static async Task WriteAsync(string filePath, byte[] bytes)
	{	
		using (FileStream fStream = new FileStream(filePath,
			FileMode.Create, FileAccess.Write, FileShare.None,
			bufferSize: 4096, useAsync: true))
		{
			await fStream.WriteAsync(bytes, 0, bytes.Length);
		};
	}
}

#else

public class SaveIoStrategyPCAsync : ISaveIoStrategy
{

	public string Log => null;
	public QuestSaveManager.eStatus Status=>QuestSaveManager.eStatus.None;

	public virtual string GetSaveDirectory() => "./";

	public string[] ReadFileNames() => new string[]{};

	public virtual byte[] ReadFromFile(string fileName) => null;	
	public virtual bool WriteToFile(string fileName, byte[] bytes) => false;

	public bool DeleteFile(string fileName) => false;
}

public class SaveIoStrategyPC : ISaveIoStrategy
{

	public string Log => null;
	public QuestSaveManager.eStatus Status=>QuestSaveManager.eStatus.None;

	public virtual string GetSaveDirectory() => "./";

	public string[] ReadFileNames() => new string[]{};

	public virtual byte[] ReadFromFile(string fileName) => null;	
	public virtual bool WriteToFile(string fileName, byte[] bytes) => false;

	public bool DeleteFile(string fileName) => false;
}
#endif


}

#endregion
