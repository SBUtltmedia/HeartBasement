using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerTools.Quest
{
	public class CharacterDialogLine
	{
		public bool IsRecorded { get; private set; } = false;
		
		public string DialogLine { get; }
		
		public int Id { get; }
		
		public string[] Translations { get; }

		public CharacterDialogLine(int id, string dialogLine, string[] translations)
		{
			Id = id;
			DialogLine = dialogLine;
			Translations = translations;
		}

		public void MarkAsRecorded()
		{
			IsRecorded = true;
		}

		public string LineIdentifier(string character)
		{
			return $"{character}{Id}";
		}

		public bool IsEmpty(bool skipFullyRecordedSections)
		{
			return skipFullyRecordedSections && IsRecorded;
		}
	}
	
	public class CharacterBlock
	{
		public string CharacterName { get; }

		public List<CharacterDialogLine> Lines { get; } = new List<CharacterDialogLine>();

		public bool HasUnrecordedLines => Lines.Exists(line => !line.IsRecorded);

		public bool HasRecordedLines => Lines.Exists(line => line.IsRecorded);

		public bool HasBothRecordedAndUnrecordedLines => HasUnrecordedLines && HasRecordedLines;

		public CharacterBlock(string characterName)
		{
			CharacterName = characterName;
		}

		public bool IsEmpty(string[] exportCharacters, bool skipFullyRecordedSections)
		{
			if (exportCharacters != null && exportCharacters.Length > 0)
			{
				if (!exportCharacters.Contains(CharacterName.ToLower()))
				{
					return true;
				}
			}
			
			foreach (CharacterDialogLine line in Lines)
			{
				if (!line.IsEmpty(skipFullyRecordedSections))
				{
					return false;
				}
			}
			
			return true;
		}

		public bool SatisifiesFilter(string[] exportCharacters)
		{
			return exportCharacters == null || exportCharacters.Length == 0 || exportCharacters.Contains(CharacterName.ToLower());
		}
	}
	
	public class DialogSubsection
	{
		public string Name { get; }

		public List<CharacterBlock> CharacterBlocks { get; } = new List<CharacterBlock>();

		public DialogSubsection(string name)
		{
			Name = name;
		}

		public bool IsEmpty(string[] exportCharacters, bool skipFullyRecordedSections)
		{
			foreach (CharacterBlock block in CharacterBlocks)
			{
				if (!block.IsEmpty(exportCharacters, skipFullyRecordedSections))
				{
					return false;
				}
			}
			return true;
		}

		public bool HasRecordedLines => CharacterBlocks.Exists(block => block.HasRecordedLines);
		public bool HasUnrecordedLines => CharacterBlocks.Exists(block => block.HasUnrecordedLines);

		public bool HasBothRecordedAndUnrecordedLines => HasRecordedLines && HasUnrecordedLines;
	}
	
	public class DialogSection
	{
		public string Name { get; }
		public List<DialogSubsection> Subsections { get; } = new List<DialogSubsection>();

		public DialogSection(string name)
		{
			Name = name;
		}

		public bool IsEmpty(string[] exportCharacters, bool skipFullyRecordedSections)
		{
			foreach (DialogSubsection subsection in Subsections)
			{
				if (!subsection.IsEmpty(exportCharacters, skipFullyRecordedSections))
				{
					return false;
				}
			}
			return true;
		}
	}
	
	public class DialogScript
	{
		public List<DialogSection> Sections { get; } = new List<DialogSection>();
	}
}
