using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;


namespace PowerTools.Quest
{

public partial interface IParser
{ 
	void ParseText(string input, bool callOnParserScripts=true);
	void RunParserScripts();
	bool Said(string query);
	bool Said(string query, IQuestClickableInterface target);
	string Text { get; }
	bool HasUnknownWord { get;}
	string UnknownWord {get;}
	bool HasWildcard { get; }
	string WildcardWord {get;}
	bool HasTarget { get; }	
	IQuestClickable Target { get; }
	/// Call this if a line was not handled, it will copy the line to clipboard for pasting into your script if you want to add it, and save out a list of what people typed too.
	void OnUnhandledEvent();
}

public partial class SystemParser : PowerTools.Singleton<SystemParser>, IParser
{ 
	///////////////////////////////////////////////////////////////////////
	// Definitions

	[System.Serializable] public class WordGroup : IEnumerable
	{
		public string[] m_words = null;
		
		int m_group = -1;

		public void InitGroupId(int groupId) { m_group = groupId; }

		public int Length => m_words==null ? 0: m_words.Length;

		public int Group => m_group;
		public int Id => m_group;
		public string Word => m_words[0];

		// Constructor/implicit cast to string array stuff
		public WordGroup() { }
		public WordGroup(string word, int group)
		{ 
			// used when adding words at runtime
			m_words = new string[1]{word};
			m_group = group;
		}
		public WordGroup(string[] array) { m_words=array;}		

		public static implicit operator string[](WordGroup self){ return self.m_words; }
		public static implicit operator WordGroup(string[] words){ return new WordGroup(words); }		
		public static implicit operator int(WordGroup self){ return self.Group; }
		public static implicit operator string(WordGroup self){ return self.Word; }
		//public string this[int index] => value[index];
		
		public IEnumerator GetEnumerator() { return m_words.GetEnumerator(); } // Implementing IEnumerable so can use foreach
	}

	static readonly char[] SPLIT = {' '};

	static readonly char SPACE = ' ';
	static readonly char REST_OF_LINE = '>';
	static readonly char WILDCARD = '*';
	static readonly char COMMA = ',';
	static readonly char OPTIONAL_START = '(';
	static readonly char OPTIONAL_END = ')';
	
	enum eCondition { Word, Wildcard, RestOfLine, End }

	
	///////////////////////////////////////////////////////////////////////
	// Serialised vars

	[SerializeField] WordGroup[] m_words = null;	
	[SerializeField] WordGroup m_ignoredWords = new WordGroup{};
	
	///////////////////////////////////////////////////////////////////////
	// Private vars
	
	HashSet<string> m_ignoredSet = new HashSet<string>();
	Dictionary<string,WordGroup> m_stringToGroup = new Dictionary<string, WordGroup>(); 
	//Dictionary<int, string> m_groupToString = new Dictionary<int, string>();
	
	string m_inputOriginal = string.Empty;
	string m_input = string.Empty;
	string[] m_inputWords = null;
	int[] m_inputGroups = null;
	bool[] m_inputIgnore = null;
	//List<WordGroup> m_tokens = new List<WordGroup>();
	
	bool m_unknownDirty = false;
	string m_unknownWord = null;
	string m_wildcardWord = null;
	IQuestClickable m_target = null;

	bool m_parserReady = false;
	
	List<string> m_unhandledPhrases = new List<string>();
	
	///////////////////////////////////////////////////////////////////////
	// Public funcs

	void OnDestroy()
	{ 
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			if ( m_unhandledPhrases.Count > 0 )
			{
				try { 
				using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter("./UnhandledParser.txt", true))
				{				
					foreach (string item in m_unhandledPhrases )
						outputFile.WriteLine(item);
				}
				} catch{ }
			}
		#endif
	}
	
	// checked at same time as mouseclick
	public bool ParserReady => m_parserReady;
	public void OnParserHandled() { m_parserReady=false;}

	public WordGroup[] EditorWords { get{ return m_words; } set{ m_words=value; } }
	public WordGroup EditorIgnoredWords { get{ return m_ignoredWords; } set{ m_ignoredWords=value; } }
	
	public void ParseText(string input, bool runParserScripts = true)
	{				
		// Clear unknown word
		m_unknownWord = null;
		m_unknownDirty = false;

		m_inputOriginal = input; // original input isn't lower cased.
		m_input = input.ToLower();
		
		if ( string.IsNullOrWhiteSpace(m_input) )
			return;

		// Tokenize into base words?
		m_inputWords = m_input.Split(SPLIT, System.StringSplitOptions.RemoveEmptyEntries);

		m_inputGroups = new int[m_inputWords.Length];
		m_inputIgnore = new bool[m_inputWords.Length];
		for ( int i = 0; i < m_inputGroups.Length; ++i)
		{ 
			string word = m_inputWords[i];
			int group = ToGroup(word);
			bool ignore = m_ignoredSet.Contains(word);
			m_inputGroups[i] = group;			
			m_inputIgnore[i] = ignore;			

			if ( group < 0 && ignore == false)
				m_unknownWord = word;
		}
		if ( runParserScripts )
			RunParserScripts();
	}

	public void RunParserScripts()
	{	
		// Call process click with parser action
		//PowerQuest.Get.ProcessClick(eQuestVerb.Parser);			
		if ( string.IsNullOrWhiteSpace(m_input) )
			return;

		m_parserReady = true;
	}

	
	public bool Said(string query)
	{
		m_target = null;
		return SaidInternal(query);
	}
	
	public bool Said(string query, IQuestClickableInterface target)
	{
		m_target = target.IClickable;
		PowerQuest.Get.SetLastClickable(m_target);
		return SaidInternal(query + ' '+m_target.Description);
	}

	
	public string Text => m_inputOriginal;
	public bool HasUnknownWord  { get{ if ( m_unknownDirty) ParseText(m_inputOriginal,false); return IsString.Valid(m_unknownWord); } }  // hackily checking ParseText again if word was added
	public string UnknownWord { get{ if ( m_unknownDirty) ParseText(m_inputOriginal,false); return m_unknownWord; } } // hackily checking ParseText again if word was added
	public bool HasWildcard  => IsString.Valid(m_wildcardWord);
	public string WildcardWord => m_wildcardWord;
	public bool HasTarget  => m_target != null;
	public IQuestClickable Target => m_target;	
	public List<string> UnhandledPhrases => m_unhandledPhrases;

	public void OnUnhandledEvent()
	{ 		
		// Save it to clipboard for pasting into game
		//m_unhandledPhrases.Add(string.Format( "{0,-40}{1}",$"~{m_input}~", $"in {PowerQuest.Get.GetCurrentRoom().ScriptName}\n")); // format just so i can align "Forest" bit to right
		m_unhandledPhrases.Add($"// In {PowerQuest.Get.GetCurrentRoom().ScriptName}\n~{m_input}~\n");
		
		if ( Application.isEditor )
		{ 
			// Editor only
			#if UNITY_EDITOR
				UnityEditor.EditorGUIUtility.systemCopyBuffer = $"\n~{m_input}~\n    "; //adding line breaks to make it easier to past into other ones
				Debug.Log($"Unhandled parser copied to clipboard: ~{m_input}~");
				// Open parser thing?
			#endif

			PowerQuest.Get.SetAutoLoadScript( PowerQuest.Get.GetCurrentRoom(), "OnParser", true, true); // using onwait flag so it does it a frame later

		}
	}

	bool SaidInternal(string conditions)
	{		
		/*
		Quagmire ahead!

		This works by checking word by word both the "input" and the "conditions"
		- Input is already parsed into parallel arrays of "words", their "group" and whether they're "ignored"
			- The list of inputs in incremented with inputIdx++
		- Conditions are a string passed in here, containing words, spaces, and other special characters: * , ( ) > 
			- The list of conditions is incremented with the NextCondition() function, which increments conditionIdx, an index to the conditions string
		*/

		if ( IsString.Empty(conditions) )
			return false;
		conditions=conditions.ToLower();				

		// clear 'wildcard word'
		m_wildcardWord = null; 
		
		bool optional = false;		
		List<string> conditionWords = new List<string>();		
		bool lastWasOptionalWildcard = false;
		
		// Set up input and condition indexes. Note input is an array index, condition is index of character in the input string
		int inputIdx = 0;
		int conditionIdx = 0;
						
		// find first query words
		eCondition foundCondition = NextCondition(conditions, ref conditionIdx, ref conditionWords, ref optional);			
	
		for ( int i = 0; i <= 1000; ++i )
		{ 
			Debug.Assert(i < 1000, "Infinite loop hit in parser");

			// ignore everything after "rest of line" is hit
			if ( foundCondition == eCondition.RestOfLine )
				return true;

			// See if we've run out of conditions
			if ( foundCondition == eCondition.End )
			{ 
				// Ran out of checks to run. Rest of input must be ignorable
				for (; inputIdx < m_inputIgnore.Length; ++inputIdx )
				{ 
					if ( m_inputIgnore[inputIdx] == false )
						return false;
				}
				return true;
			}

			// See if we've run out of input
			if ( inputIdx >= m_inputWords.Length )
			{ 
				// Ran out of input. Rest of checks must be optional/ROL
				while ( foundCondition != eCondition.End )
				{ 
					if ( optional == false && foundCondition != eCondition.RestOfLine)
						return false;
					 foundCondition = NextCondition(conditions, ref conditionIdx, ref conditionWords, ref optional);
				}
				return true;
			}

			// See if the next condition matches the next input word

			int inputGroup = m_inputGroups[inputIdx];
			string inputWord = m_inputWords[inputIdx];
			bool inputIgnore = m_inputIgnore[inputIdx];

			if ( foundCondition == eCondition.Wildcard && optional )
			{	// if wilcard was optional, we might be better ignoring it rather than using the match...
				lastWasOptionalWildcard = true;
				foundCondition = NextCondition(conditions, ref conditionIdx, ref conditionWords, ref optional);	
			}
			else if ( foundCondition == eCondition.Wildcard && inputIgnore == false ) 
			{   // Found wildcard- increment both input word and condition word
				if ( m_wildcardWord == null ) // store first 'wildcard'
					m_wildcardWord = inputWord;
				lastWasOptionalWildcard = false;
				inputIdx++;
				foundCondition = NextCondition(conditions, ref conditionIdx, ref conditionWords, ref optional);	
			}
			else if ( foundCondition == eCondition.Word && IsMatch(inputWord, inputGroup, conditionWords) )
			{   // Found match- increment both input word and condition word
				lastWasOptionalWildcard = false;
				inputIdx++;
				foundCondition = NextCondition(conditions, ref conditionIdx, ref conditionWords, ref optional);	
			}
			else if ( (foundCondition == eCondition.Word || foundCondition == eCondition.Wildcard) && inputIgnore )
			{   // No match, but input ignored, increment the input
				inputIdx++;
			}
			else if ( lastWasOptionalWildcard )
			{  // Not match, but we used up an optional wildcard, so use it now to skip input :P
				lastWasOptionalWildcard = false;
				inputIdx++;
			}
			else if ( optional )
			{   // no match, but condition was optional- increment the condition
				foundCondition = NextCondition(conditions, ref conditionIdx, ref conditionWords, ref optional);	
			}
			else
			{   // no match- FAIL 
				return false; 
			}
		}
		return false;
		
	}


	// returns true if found another condition
	eCondition NextCondition(string query, ref int pos, ref List<string> words, ref bool optional)
	{		

		// Loop through characters
		while ( pos < query.Length )
		{ 
			char curr = query[pos];
			
			if ( curr == SPACE )     
			{	// Space- ignore
			}
			else if ( curr == WILDCARD )  
			{
				// skip 'wildcard'- add to words so it can be picked up
				pos++;
				return eCondition.Wildcard;
			} 
			else if ( curr == REST_OF_LINE )   // rest of line - return
			{ 			
				pos++;
				return eCondition.RestOfLine;
			} 
			else if ( curr == OPTIONAL_START )   // start optional
			{ 
				optional = true;
			} 
			else if ( curr == OPTIONAL_END )  // end optional
			{ 
				optional = false;
			} 
			else if ( char.IsLetterOrDigit(curr) ) // word
			{ 
				// found start of next word
				NextConditionWords(query, ref pos, ref words);
				return eCondition.Word;
			}

			// next char
			++pos;
		}
		return eCondition.End;	
	}

	void NextConditionWords(string query, ref int pos, ref List<string> words)
	{
		words.Clear();
		while (pos < query.Length)
		{
			// Start of word- find the length
			int endPos = pos;
			while ( endPos < query.Length && char.IsLetterOrDigit(query[endPos]) )
				++endPos;							
				
			string word = query.Substring(pos,endPos-pos);
			words.Add(word);

			// increment pos
			pos=endPos;

			// check for comma, ignoring space
			bool foundComma = false;
			while ( pos < query.Length )
			{ 
				char curr = query[pos];
				if ( curr == COMMA)
				{ 
					foundComma = true; // found comma
					++pos;
				}
				else if ( curr == SPACE )
					++pos; // skip space
				else
					break; // found non-comma/space, so break from loop
			}
			if ( foundComma == false )				
				break; // no comma, we're finished
		}
	}

	
	bool IsMatch(string inputWord, int inputGroup, List<string> checkWords )
	{ 
		foreach (string checkWord in checkWords)
		{
			int checkGroup = ToGroup(checkWord);
			if ( checkGroup < 0 && m_ignoredSet.Contains(checkWord) == false )
			{ 
				checkGroup = AddWord(checkWord);				
			}
			
			// check the word matches			
			if ( checkWord == inputWord )
				return true; 
						
			// check synonyms from group if input is in one
			if ( inputGroup >= 0 && inputGroup == ToGroup(checkWord) ) 
				return true; 
		}
		return false;
	}

	public string SaidUnknownWord()
	{
		return m_unknownWord;
	}

	public string SaidAnyword()
	{
		return m_wildcardWord;
	}
	
	///////////////////////////////////////////////////////////////////////
	// Unity funcs

	// Use this for initialization
	void Awake() 
	{
		SetSingleton();
		DontDestroyOnLoad(this);
		InitDictionaries();
	}

	///////////////////////////////////////////////////////////////////////
	// Private funcs

	int m_lastGroupId = 0;

	// public for editor only
	public void InitDictionaries()
	{ 	
		m_ignoredSet.Clear();
		//m_groupToString.Clear();
		m_stringToGroup.Clear();

		m_lastGroupId = 0;

		foreach ( string value in m_ignoredWords )
			m_ignoredSet.Add(value);
		/*
		m_ignoredWords.InitGroupId(groupId);
		m_groupToString.Add(groupId,m_ignoredWords);
		foreach ( string value in m_ignoredWords )
			m_stringToGroup.Add(value,m_ignoredWords);
			++m_groupIdMax;
		*/
		
		foreach ( WordGroup group in m_words )
		{ 
			group.InitGroupId(m_lastGroupId);
			//m_groupToString.Add(m_lastGroupId,group);
			foreach ( string value in group )			
				m_stringToGroup.Add(value,group);

			++m_lastGroupId;
		}
	}

	// Use for adding groups at runtime
	int AddWord(string word)
	{ 
		m_unknownDirty=true;
		WordGroup group = new WordGroup(word, m_lastGroupId);
		m_stringToGroup.Add(word,group); // nb: these words aren't in the editable list- just in the internal one		
		++m_lastGroupId;
		return group.Id;
	}

	int ToGroup(string word)
	{ 
		WordGroup result;
		if ( m_stringToGroup.TryGetValue(word,out result) )
			return result == null ? -1 : result.Group;
		return -1;
	}
	/*
	string ToWord(int group)
	{
		string result;
		if ( m_groupToString.TryGetValue(group,out result) )
			return result;
		return null;
	}
	*/	
}

}
