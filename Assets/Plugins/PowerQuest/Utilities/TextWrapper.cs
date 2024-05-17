using UnityEngine;
using System.Collections;

namespace PowerTools
{


/**
 
 TextSize for Unity3D by thienhaflash (thienhaflash@gmail.com)
 
 Version	: 0.1
 Update		: 18.Jun.2012
 Features	:
	Return perfect size for any TextMesh
 	Cache the size of each character to speed up the size
	Evaluate and cache only when there are requirements
 
 Sample 	:
		
		//declare it locally, so we can have access anywhere from the script
		TextSize ts;
		
		//put this on the Start function
	 	ts = new TextSize(gameObject.GetComponent<TextMesh>());
		
		//anywhere, after you change the text :
		print(ts.width);
		
		//or get the length of an abitrary text (that is not assign to TextMesh)
		print(ts.GetTextWidth("any abitrary text goes here"));

 You are free to use the code or modify it any way you want (even remove this block of comments) but if it's good
 please give it back to the community.
 
 */
 
public class TextWrapper 
{
	private Hashtable dict; //map character -> width
	
	private TextMesh textMesh;
	private Renderer renderer;
	
	public TextWrapper(TextMesh tm)
	{
		textMesh = tm;
		renderer = tm.GetComponent<Renderer>();
		dict = new Hashtable();
		GetSpace();
	}
	
	private void GetSpace()
	{
		//the space can not be got alone
		string oldText = textMesh.text;
		
		textMesh.text = "a";
		float aw = renderer.bounds.size.x;
		textMesh.text = "a a";
		float cw = renderer.bounds.size.x - 2* aw;
		
		// MonoBehaviour.print("char< > " + cw);
		dict.Add(' ', cw);
		dict.Add('a', aw);
		
		textMesh.text = oldText;
	}
	
	public float GetTextWidth(string s) 
	{
		char[] charList = s.ToCharArray();
		float w = 0;
		char c;
		string oldText = textMesh.text;
		//bool wasSpace = true;
		
		bool inRichTextElement = false;
		for (int i=0; i<charList.Length; i++){
			c = charList[i];
			
			if ( c == '<' )
				inRichTextElement = true;
				
			if ( inRichTextElement == false )
			{
				if (dict.ContainsKey(c))
				{
					w += (float)dict[c];					
				}
				else 
				{
					textMesh.text = c.ToString();
					float cw = renderer.bounds.size.x;
					dict.Add(c, cw);
					w += cw;
					//MonoBehaviour.print("char<" + c +"> " + cw);
				}
			}	
			
			if ( c == '>' )
				inRichTextElement = false;		
		}
		
		textMesh.text = oldText;
		return w;
	}
	
	public float Width { get { return GetTextWidth(textMesh.text); } }
	public float Height { get { return renderer.bounds.size.y; } }
	public Bounds Bounds { get { return renderer.bounds; } }
	
	static readonly string STRING_SPACE = " ";
	
	public string WrapText( string input, float width )
	{
		// Prepare result
		string result = string.Empty;
		
		string[] lines = input.Split ('\n');
		for ( int lineNum = 0; lineNum < lines.Length; ++lineNum )
		{ 
			
			if ( lineNum > 0 )
				result += '\n';
			
			// Split string by char " "    
			string[] words = lines[lineNum].Split(' ');			
			
			// Temp line string
			string line = string.Empty;
			
			string temp = words[0];
			
			// for each all words     
			for ( int wordNum = 0; wordNum < words.Length; ++wordNum )
			{
				// Append current word into line
				if ( wordNum > 0 )
					temp = line + STRING_SPACE + words[wordNum];
				
				// If line length is bigger than lineLength
				if( GetTextWidth(temp) > width )
				{ 
					// Append current line into result
					result += line + '\n';
					// Remain word append into new line
					line = words[wordNum];
				}
				// Append current word into current line
				else 
				{
					line = temp;
				}
			}
	 
			// Append last line into result   
			result += line;
		
		}		
 
		return result;
	}

	// More expensive word wrap. Does it multiple times to get an even width between lines without increasing number of lines
	public string WrapTextMinimiseWidth( string input, float width, float minWidth = 0 )
	{
		int targetLines = 0;
		string result = string.Empty;
		float decrement = GetTextWidth("ABC"); // Decrement by the width of a small word. Close enough is good enough.
		string wrapped = WrapTextNicerInternal(input, width, out targetLines);
		if ( targetLines <= 1 )
			return wrapped;
		int numLines = targetLines;
		while ( numLines == targetLines && (minWidth <= 0 || width > minWidth) )
		{
			result = wrapped;
			width -= decrement;
			wrapped = WrapTextNicerInternal(input, width, out numLines);
		}

		return result;
	}

	string WrapTextNicerInternal( string input, float width, out int numLines )
	{

		// Prepare result
		string result = string.Empty;
		numLines = 0;


		string[] lines = input.Split ('\n');


		if ( width <= 0 )
		{
			numLines = lines.Length;
			return input;
		}

		for ( int lineNum = 0; lineNum < lines.Length; ++lineNum )
		{ 
			numLines++;
			if ( lineNum > 0 )
				result += '\n';

			// Split string by char " "    
			string[] words = lines[lineNum].Split(' ');			

			// Temp line string
			string line = string.Empty;

			string temp = words[0];

			// for each all words     
			for ( int wordNum = 0; wordNum < words.Length; ++wordNum )
			{
				// Append current word into line
				if ( wordNum > 0 )
					temp = line + STRING_SPACE + words[wordNum];

				// If line length is bigger than lineLength
				float tempTextWidth = GetTextWidth(temp);
				if( tempTextWidth > width )
				{ 
					// Append current line into result
					result += line + '\n';
					numLines++;
					// Remain word append into new line
					line = words[wordNum];
				}
				// Append current word into current line
				else 
				{
					line = temp;
				}
			}

			// Append last line into result   
			result += line;

		}		

		return result;
	}

	public string Truncate( string input, float width )
	{
		// Calculate the size of ellipsis a single time
    	float difference = GetTextWidth("...");
		// Prepare result
		string result = string.Empty;

		string[] lines = input.Split ('\n');
		for ( int lineNum = 0; lineNum < lines.Length; ++lineNum )
		{
			if ( lineNum > 0 )
				result += '\n';
			
			if ( GetTextWidth(lines[lineNum]) <= width ) {
				result += lines[lineNum];
				continue; // nothing to do, break out of this cycle early
			}
			
			// If we are here the string is looong
			// Split string by char " "
			string[] words = lines[lineNum].Split(' ');
			int wordNum = 0;
			
			// Temp line string (adding the first word so we don't get
			// a space at the beginning of the string)
			string line = string.Empty + words[wordNum];

			// go until there's space (taking the ellipsis into account)
			// we already added the first word so increment wordNum immediately
			while (GetTextWidth(line) + difference <= width && wordNum++ < words.Length)
			{
				// Append current word into line
				line = line + STRING_SPACE + words[wordNum];
			}
			// Append ellipsis (unless we ran to the end of the origianl string already)
			if (wordNum < words.GetUpperBound(0))
				line += "...";
	 
			// Append last line into result   
			result += line; 
		}
 
		return result;
	}

}
}