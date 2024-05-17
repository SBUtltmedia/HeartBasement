//-----------------------------------------
//          PowerText
// More info/instructions:
//  http://powerhoof.com/PowerText
//  dave@powerhoof.com
//  @DuzzOnDrums
//----------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using PowerTools;
using PowerTools.PowerTextUtils;

namespace PowerTools
{

#region Class: PowerText

/** <summary>
Usage: 
- Create a new PowerText(). 
- Call Parse(string inputText); to parse the input text.
- Call GetString(string groupName); to generate a string from the name of a group.

See http://tools.powerhoof.com for examples
*/
public class PowerText
{
    #endregion
    #region Definitions

	// Matches any nonspace character between two semicolons, eg. :ANIMAL:
    static readonly Regex s_regexGroup = new Regex(@"^:(?<group> \S+?):", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);	

	// Matches a stuff on a line within a group. This is probably insantely inefficient :D 
    static readonly Regex s_regexLine = new Regex(
			@"(^//.*) |"                        // Comments are a line starting with '//'
			+ @"(^\[(?<weight>\d*\.?\d+)\]) | " // Weight is a group that's just got a number in it at the start of a line (this controls how often that string will be used)
			+ @"(?<empty>\[\s*\]) | "           // Empty line is ignored unless it has [] on it
			+ @"(?<ps> \[\#\]) | "              // Start of pluralized denoted by [#] (eg: I have [#]40 hat[s])
			+ @"(?<pe> \[\\\#\]) | "             // End of pluralized denoted by [\#] (eg: I have [#]40 hat[s])
			+ @"(?<s>\[s\]) | (?<es>\[es\]) | (\[/(?<plr> \w+)\]) | "  // Plurals are [s], [es] or [/PluralOfPreviousWord]
			+ @"(\[(?<ref> \S+?)\]) | "          // Ref (reference to another group) is nonspace character between brackets eg. [ANIMAL] // TODO: Check why that 2nd ? is in there...
			+ @"(?<text> [^\[\r]+)",             // Text is anything that's not a new line.
        RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase); 

    // Matches first letter of line, or first letter after a full stop or equivalent
    static readonly Regex s_regexCapital = new Regex(@"((?<=^\W*)\w) | ((?<=[.:!?]\s*)\w)", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

    // Matches a followed by space and a vowel (unless directly after non-whitespace, ie the end of a word)
    static readonly Regex s_regexAn = new Regex(@"(?<!\S)a(?=\s+[aeiou])", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

	static readonly char[] LINEDELIM = {'\n'};

	static readonly string MATCH_GROUP = "group";
	static readonly string MATCH_EMPTY = "empty";
	static readonly string MATCH_WEIGHT = "weight";
	static readonly string MATCH_REF = "ref";
	static readonly string MATCH_TEXT = "text";
	static readonly string MATCH_PLURALISE_START = "ps";
	static readonly string MATCH_PLURALISE_END = "pe";
	static readonly string MATCH_PLURAL_S = "s";
	static readonly string MATCH_PLURAL_ES = "es";
	static readonly string MATCH_PLURAL = "plr";

    #endregion
    #region Vars: private

	Dictionary<string, PowerTextNodeGroup> m_groups = new Dictionary<string, PowerTextNodeGroup>();

    #endregion
    #region Funcs: public

	/// Find a string from the group name (the thing between the colons. eg. :WeaponName: )
	public string GetString(string groupName)
	{
		string result = string.Empty;
		PowerTextNodeGroup group = null;
		if ( m_groups.TryGetValue(groupName.ToUpper(), out group) )
		{
            bool plural = false;
            StringBuilder builder = new StringBuilder();
            group.Build(builder, ref plural);
            result = builder.ToString();	
            result = PostProcess(result);
		}
		return result;
	}

	public List<string> GetStringList(string groupName)
	{
		List<string> result = new List<string>();
		PowerTextNodeGroup group = null;
		if ( m_groups.TryGetValue(groupName.ToUpper(), out group) )
		{
			bool plural = false;
			group.Build(result, ref plural);
			//result = PostProcess(result);
		}
		return result;		
	}

	public string[] GetAllParts()
	{
		HashSet<string> set = new HashSet<string>();
		List<string> partList = new List<string>(256);
		foreach ( PowerTextNodeGroup group in m_groups.Values )
		{		
			if ( group == null || group.m_options == null )
				continue;
			foreach (IPowerTextNode node in group.m_options )
			{	
				node.GetParts(partList);
			}
		}  

		foreach( string part in partList )
		{
			set.Add(part.ToLower());
		}
		string[] result = new string[set.Count];
		set.CopyTo(result);
		return result;
	}

	/// Takes the input text and parses it. Do this before "GetString"
	public void Parse(string text)
	{
		string[] textLines = text.Split(LINEDELIM,10000);

		PowerTextNodeGroup currentGroup = null;

		for (int i = 0; i < textLines.Length; ++i )
		{
			string lineText = textLines[i];            
            Match groupMatch = s_regexGroup.Match(lineText);
			if ( groupMatch.Success )
			{
				// Parse group name
				string groupName = groupMatch.Groups[MATCH_GROUP].Value;
				currentGroup = FindOrCreateGroup(groupName);
			}
			else if ( currentGroup != null )
			{
				// Add node for line
				ParseLine(currentGroup, lineText);
			}
		}
	}

	PowerTextNodeLine ParseLine(PowerTextNodeGroup currentGroup, string lineText)
	{
		// Add node for line
		PowerTextNodeLine currentLine = new PowerTextNodeLine();

		bool lineHasContent = false;
		PowerTextNodeString lastStringNode = null;

		MatchCollection lineMatches = s_regexLine.Matches(lineText);
		foreach( Match lineMatch in lineMatches )
		{
			if ( lineMatch.Success && lineMatch.Groups == null )
				continue;					

			if ( lineMatch.Groups[MATCH_EMPTY].Success)
			{
				lineHasContent = true;
				currentLine.Append( (PowerTextNodeString)(string.Empty) );
			}
			if ( lineMatch.Groups[MATCH_WEIGHT].Success)
			{
				lineHasContent = true;
				float.TryParse(lineMatch.Groups[MATCH_WEIGHT].Value, out currentLine.m_weight);
			}
			if ( lineMatch.Groups[MATCH_REF].Success)
			{
				lineHasContent = true;
				currentLine.Append( FindOrCreateGroup(lineMatch.Groups[MATCH_REF].Value) );
			}
			if ( lineMatch.Groups[MATCH_TEXT].Success )
			{
				string value = lineMatch.Groups[MATCH_TEXT].Value;
				if ( string.IsNullOrEmpty(value) == false )
				{
					lineHasContent = true;
					lastStringNode = (PowerTextNodeString)value;
					currentLine.Append( lastStringNode );
				}
			}
			if ( lineMatch.Groups[MATCH_PLURALISE_START].Success )
			{
				lineHasContent = true;
				currentLine.Append( new PowerTextNodePluralFlag(true) );
			}
			if ( lineMatch.Groups[MATCH_PLURALISE_END].Success )
			{
				currentLine.Append( new PowerTextNodePluralFlag(false) );
			}

			// Parse "plural" stuff
			if ( lastStringNode != null )
			{
				if ( lineMatch.Groups[MATCH_PLURAL_S].Success ) lastStringNode.SetPlural(PowerTextNodeString.ePluralType.s);
				if ( lineMatch.Groups[MATCH_PLURAL_ES].Success ) lastStringNode.SetPlural(PowerTextNodeString.ePluralType.es);
				if ( lineMatch.Groups[MATCH_PLURAL].Success ) 
				{
					string value = lineMatch.Groups[MATCH_PLURAL].Value;
					if ( string.IsNullOrEmpty(value) == false )
					{
						lastStringNode.SetPlural(PowerTextNodeString.ePluralType.Custom, value);
					}								
				}
			}
		}      

		if ( lineHasContent )
			currentGroup.AddOption(currentLine);    

		return currentLine;

	}

    /// Sets a text variable by name (in the script, write [name] to use the variable)
    public void SetVariable(string name, string value)
	{		
		PowerTextNodeGroup group = FindOrCreateGroup(name);
		if ( group.m_options != null )
			group.m_options.Clear();
		ParseLine(group,value);
    }

    /// Sets an int variable by name, marking it as plural if it's not 1 (in the script, write [name] to use the variable)
    public void SetVariable(string name, int value)
    {
        if ( value != 1 )            
			SetVariable(name, value.ToString()+"[#]");
        else 
			SetVariable(name, value.ToString());
    }


    #endregion
    #region Funcs: Private

	PowerTextNodeGroup FindOrCreateGroup(string name)
	{
		PowerTextNodeGroup node = null;
		name = name.ToUpper();
		if ( m_groups.TryGetValue(name, out node) )
		{
			return node;
		}
		node = new PowerTextNodeGroup();
		m_groups.Add(name, node);
		return node;
	}
 

    string PostProcess( string text )
    {
        // Capitalise first alphanumeric of sentance, or after . ; - ! ?
        text = s_regexCapital.Replace(text, ReplaceCapital);

        // Change a to an
        return s_regexAn.Replace(text,ReplaceAn);
    }
     
    string ReplaceAn(Match match)
    {
        return string.Concat(match.ToString(),'n');
    }

    string ReplaceCapital(Match match)
    {
        return match.ToString().ToUpper();
    }
    #endregion
}



#region Class: PowerTextNodes

// Interface to PowerTextNode with generic function to generate the string
public interface IPowerTextNode
{
    void Build( StringBuilder builder, ref bool pluralize );
	void Build( List<string> parts, ref bool pluralize ); // build string into list of strings
	void GetParts( List<string> parts );	// Get all potential strings
    float GetWeight();
}

// Node that returns random node from list
public class PowerTextNodeGroup: IPowerTextNode
{   
    public List<IPowerTextNode> m_options = null;
    ShuffledIndex m_shuffledIndex;
    float m_maxWeight = 1;

    public void Build( StringBuilder builder, ref bool plural )
    {
        if ( m_options == null || m_options.Count <= 0 )
            return;

        if ( m_shuffledIndex == null ) m_shuffledIndex = new ShuffledIndex(m_options.Count);

        for ( int i = 0; i <= m_shuffledIndex.Length*2; ++i ) // Loop through shuffled list until find an entry that satisfies weight constraint (only necessary if all have weight of zero).
        {
            m_shuffledIndex.Next();
            float weight = m_options[m_shuffledIndex].GetWeight();
			if ( weight >= m_maxWeight || (weight > 0.0f && weight >= Random.value * m_maxWeight) )
            {
                m_options[m_shuffledIndex].Build(builder, ref plural);
                break;
            }
        }
    }

	public void Build( List<string> parts, ref bool plural )
	{
		if ( m_options == null || m_options.Count <= 0 )
			return;

		if ( m_shuffledIndex == null ) m_shuffledIndex = new ShuffledIndex(m_options.Count);

		for ( int i = 0; i <= m_shuffledIndex.Length*2; ++i ) // Loop through shuffled list until find an entry that satisfies weight constraint (only necessary if all have weight of zero).
		{
			m_shuffledIndex.Next();
			float weight = m_options[m_shuffledIndex].GetWeight();
			if ( weight >= m_maxWeight || (weight > 0.0f && weight >= Random.value * m_maxWeight) )
			{
				m_options[m_shuffledIndex].Build(parts, ref plural);
				break;
			}
		}		
	}

	public void GetParts( List<string> parts )
	{
		if ( m_options == null || m_options.Count <= 0 )
			return;
		for ( int i = 0; i < m_options.Count; ++i )
			m_options[i].GetParts(parts);
	}

    public float GetWeight() { return 1.0f; }

    public void AddOption(IPowerTextNode node) 
    { 
        if ( m_options == null )
            m_options = new List<IPowerTextNode>();
        m_options.Add(node); 
        m_maxWeight = Mathf.Max(m_maxWeight, node.GetWeight());
    }

}

// Node that returns a bunch of nodes concatenated (eg: "hello " + [name] + " how are you today?" is 3 PowerTextNodeLines)
public class PowerTextNodeLine: IPowerTextNode
{
    public List<IPowerTextNode> m_strings;

    public float m_weight = 1;

    public void Build( StringBuilder builder, ref bool plural )
    {
        if ( m_strings == null )
            return;
        for ( int i = 0; i < m_strings.Count; ++i )                
            m_strings[i].Build(builder, ref plural);                
	}

	public void Build( List<string> parts, ref bool plural )
	{
		if ( m_strings == null )
			return;
		for ( int i = 0; i < m_strings.Count; ++i )                
			m_strings[i].Build(parts, ref plural);  

	}

	public void GetParts( List<string> parts )
	{
		if ( m_strings == null )
			return;
		for ( int i = 0; i < m_strings.Count; ++i )                
			m_strings[i].GetParts(parts);  

	}

    public float GetWeight() { return m_weight; }

    public void Append(IPowerTextNode node)
    {
        if ( m_strings == null )
            m_strings = new List<IPowerTextNode>();
        m_strings.Add(node);        
    }
}

public class PowerTextNodeString : IPowerTextNode
{
	// Text nodes can have an alternate plural, migth j ust append 's' or 'es' or might replace the whole word
    public enum ePluralType
    {
        None,
        s,
        es,
        Custom
    }

    string m_string = null;
    ePluralType m_pluralType = ePluralType.None;
    string m_pluralString = null;

    public PowerTextNodeString(string str) {m_string = str;}
    public static implicit operator PowerTextNodeString(string str) 
    {
        return new PowerTextNodeString(str);
    }

    public void SetPlural( ePluralType type, string customString = null )
    {
        m_pluralType = type;
        m_pluralString = customString;
    }

    public void Build( StringBuilder builder, ref bool plural )
    {
        // Check if it should make the text end with a plural
        if ( m_pluralType != ePluralType.None && plural )
        {
            if ( m_pluralType == ePluralType.Custom )
            {
                // Custom plural means it'll replace the entire last word (eg:  life/lives)
                int pos = m_string.LastIndexOf(' ');
                if ( pos > 0 && pos < m_string.Length )
                    builder.Append(m_string.Substring(0,pos));
                builder.Append(m_pluralString);
            }
            else if ( m_pluralType == ePluralType.s )
            {
                builder.Append(m_string);
                builder.Append('s');
            }
            else if ( m_pluralType == ePluralType.es )
            {
                builder.Append(m_string);
                builder.Append("es");
            }
        }
        else 
        {
            builder.Append(m_string);
        }
	}

	public void Build( List<string> parts, ref bool plural )
	{
		if ( m_pluralType == ePluralType.None )
		{
			parts.Add(m_string);			
		}
		else
		{
			// Add string up to the last word, which is included seperately
			int pos = m_string.LastIndexOf(' ');
			string lastWord = m_string;
			if ( pos > 0 && pos < m_string.Length )
			{
				parts.Add(m_string.Substring(0,pos));			
				lastWord = m_string.Substring(pos);
			}
			// And add pluralized version of last word
			if ( plural == false )
			{
				parts.Add(lastWord);
			}
			else 
			{
				if ( m_pluralType == ePluralType.Custom )
				{
					parts.Add(m_pluralString);
				}
				else if ( m_pluralType == ePluralType.s )
				{
					parts.Add(lastWord+'s');
				}
				else if ( m_pluralType == ePluralType.es )
				{
					parts.Add(lastWord+"es");
				}
			}
		}
	}

	public void GetParts( List<string> parts )
	{
		if ( m_pluralType == ePluralType.None )
		{
			parts.Add(m_string);			
		}
		else
		{
			// Add string up to the last word, which is included seperately
			int pos = m_string.LastIndexOf(' ');
			string lastWord = m_string;
			if ( pos > 0 && pos < m_string.Length )
			{
				parts.Add(m_string.Substring(0,pos));			
				lastWord = m_string.Substring(pos);
			}

			// Add plural and non-plural alternatives to the last word
			parts.Add(lastWord);

			if ( m_pluralType == ePluralType.Custom )
			{
				parts.Add(m_pluralString);
			}
			else if ( m_pluralType == ePluralType.s )
			{
				parts.Add(lastWord+'s');
			}
			else if ( m_pluralType == ePluralType.es )
			{
				parts.Add(lastWord+"es");
			}
		}

	}

    public float GetWeight() { return 1.0f; }
}

public class PowerTextNodePluralFlag : IPowerTextNode
{
	bool m_pluralize = false;
	public PowerTextNodePluralFlag(bool pluralize) { m_pluralize = pluralize; }
	public void Build( StringBuilder builder, ref bool pluralize ) { pluralize = m_pluralize; }

	public void Build ( List<string> parts, ref bool pluralize ) { pluralize = m_pluralize; }

	public void GetParts( List<string> parts )
	{
	}
    public float GetWeight() { return 1.0f; }
}


#endregion 

}

#region Class: Utils

// Utils- Normally these would be in seperate file.

namespace PowerTools.PowerTextUtils
{

/// <summary>
/// Shuffled index.
/// Usage: 
///  - Instantiate the shuffled index with desired count. 
///  - Cast to "int" to get the value (eg. (int)shuffledIndex), increment with shuffledIndex++);
///  - Increment with ++ operator to get next shuffled value (unless you pass autoIncrement as TRUE)
/// </summary>
public class ShuffledIndex
{
	static Dictionary<int, ShuffledIndex> s_premadeShuffledIndexes = new Dictionary<int, ShuffledIndex>();

    int m_current = -2; // -2 so if next() is called first, it'll still reshuffle the first time because the index will still be invalid
    int[] m_ids = null;


	/// Create with the max index. If auto-increment is true, casting to "int" will increment the index, otherwise do shuffledList++;
    public ShuffledIndex( int count, bool autoIncrement = false )
    {
        m_ids = new int[count];
        for (int i = 0; i < count; ++i )
        {
            m_ids[i] = i;
        }
    }

    /// Returns shuffled index with max range (inclusive). This is not a unique list, any callers will have the same. Useful when just want to shuffle something that's happening a few times in a row without creating a list first
	public static int Random(int max)
	{
		int count = max+1; // increment so that it represents "count" rather than "max"
		ShuffledIndex shuf = null;
		if ( s_premadeShuffledIndexes.TryGetValue(count, out shuf) == false )
		{
			shuf = new ShuffledIndex(count);
			s_premadeShuffledIndexes.Add(count,shuf);
		}
		return shuf.Next();
	}
    
    public int Next()
    {
        m_current++;
        return (int)this;
    }

    public static implicit operator int(ShuffledIndex m)
    {
        if ( m.m_ids.Length == 0 )
            return -1;
        if ( m.m_current < 0 || m.m_current >= m.m_ids.Length )
        {           
            int previousValue = m.m_ids[m.m_ids.Length - 1];
            
            m.m_ids.Shuffle();           
            m.m_current = 0;
            
            // Check if it's repeating the last value and if so, swap the first elements around so you don't get 2 in a row (only if more than 1 element)
            if ( m.m_ids.Length > 1 && previousValue == m.m_ids[0] )
                Utils.Swap(ref m.m_ids[0], ref m.m_ids[1]);
        }
        return m.m_ids[m.m_current];
	}

    public static ShuffledIndex operator ++(ShuffledIndex m)
    {
    	m.Next();
    	return m;
	}

	public int Length { get{ return m_ids.Length; } }
	public int Count { get{ return m_ids.Length; } }

	/// Moves current id to the used 
	public void SetCurrent(int id)
	{
		if ( m_ids == null || m_ids.Length == 0 || id < 0 || id >= m_ids.Length )
			return;

		// if haven't shuffled yet, do so
		if ( m_current < 0 ) 
			this.Next();

		// find the index of the id we want
		int newCurrentIndex = System.Array.FindIndex(m_ids, (item)=>item == id);

		// if we're already there, do nothing
		if ( newCurrentIndex == m_current )
			return; // dont need to do anything
		
		if ( newCurrentIndex < m_current)
		{			
			// If already passed that id, swap the value with the current index
			Utils.Swap(ref m_ids[newCurrentIndex], ref m_ids[m_current]);
		}
		else if ( newCurrentIndex > m_current )
		{			
			// If haven't passed the index yet, go to next index and swap with current index (unless we're on that one)

			// swap new current index with next
			Next();
			if ( m_current != newCurrentIndex )
				Utils.Swap(ref m_ids[newCurrentIndex], ref m_ids[m_current]);
		}
	}
}

public static class Utils
{
    // Extention method to shuffle an array
   public static void Shuffle<T>( this T[] list )
   {
        T temp;
        int j = 0;
        int count = list.Length;
        for ( int i = count - 1; i >= 1; --i )
        {
            j = Random.Range(0,i+1);
            // swap
            temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // Usually I have this in a util class it's pretty useful :)
    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }
}

}
#endregion
