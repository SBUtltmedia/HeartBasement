using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using PowerTools;

namespace PowerTools.Quest
{

/// Functions/Properties added here are accessable from the 'E.' object in quest script
public partial interface IPowerQuest
{
}


public partial class PowerQuest
{
	// Serialised data that can be saved/restored
	[System.Serializable]
	public class BgSequenceData
	{
		[SerializeField] public string room = string.Empty;
		[SerializeField] public string function = string.Empty;
	}
	[SerializeField] [HideInInspector] 
	List<BgSequenceData> m_bgSequenceData = new List<BgSequenceData>();

	// Non serialised data needed for each bg sequence running 
	class BgSequenceState
	{ 
		// Has reference to Data for convenience only, it's stored in the serialized list
		public BgSequenceData Data = null;
		public Coroutine Sequence = null;
		public Coroutine SequenceCaller = null;
	}
	List<BgSequenceState> m_bgSequenceState = new List<BgSequenceState>();	
	
	public void StartBackgroundSequence( DelegateWaitForFunction function )
	{
		if ( function != null )
			StopBackgroundSequence(function.Method.Name);
			
		// Set data so we can restore from save game
		string functionName = function.Method.Name;
		
		BgSequenceData data = new BgSequenceData()
		{ 
			room = GetCurrentRoom().ScriptName,
			function = function.Method.Name
		};
		m_bgSequenceData.Add(data);

		BgSequenceState state = new BgSequenceState() {  Data = data };
		m_bgSequenceState.Add(state);
		 
		state.SequenceCaller = StartCoroutine(CoroutineBgSequence(state, function));
	}

	public void StopBackgroundSequence(string name = null)
	{
		if ( name == null )
		{
			// name is null, remove all BgSequences
			while ( m_bgSequenceState.Count > 0 )
				StopBackgroundSequence(m_bgSequenceState[0].Data.function);
			m_bgSequenceState.Clear();
			m_bgSequenceData.Clear();
		}

		BgSequenceState state = GetBgSequenceState(name);		
		if ( state != null && state.SequenceCaller != null )
			StopCoroutine(state.SequenceCaller);
		if ( state != null && state.Sequence != null )
			StopCoroutine(state.Sequence);
		
		m_bgSequenceState.RemoveAll(item=>item.Data.function == name);
		m_bgSequenceData.RemoveAll(item=>item.function == name);
	}

	IEnumerator CoroutineBgSequence(BgSequenceState state, DelegateWaitForFunction function)
	{
		// Wait for the coroutine
		state.Sequence = StartQuestCoroutine(function());

		yield return state.Sequence;
		
		// Cleanup - maybe also want to do this on room exit?		
		StopBackgroundSequence(state.Data.function);
	}
	
	BgSequenceState GetBgSequenceState(string function)
	{ 
		foreach (BgSequenceState state in m_bgSequenceState)
		{ 
			if ( state.Data.function == function)
				return state;
		}
		return null;
	}

	void OnPostRestoreBgSequence()
	{
		// copy before clearing old ones
		List<BgSequenceData> restoredData = new List<BgSequenceData>(m_bgSequenceData);
		StopBackgroundSequence();

		foreach ( BgSequenceData data in restoredData )
		{
			// Need to find the function and run it			
			QuestScript scriptClass = this.GetRoom(data.room).GetScript();
			if  ( scriptClass != null )		
			{
				string function = data.function;
				System.Reflection.MethodInfo method = scriptClass.GetType().GetMethod( function, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				if ( method != null )
				{
					// Start sequence	
					StartBackgroundSequence(method.CreateDelegate(typeof(DelegateWaitForFunction),scriptClass) as DelegateWaitForFunction);
				}
			}
		}
	}
}

/// Functions/Properties added here are accessable from the 'C.<characterName>.' object in quest script
public partial interface ICharacter
{
	/// Start character saying something, unskippable. Useful in background conversations \sa StartBackgroundSequence
	Coroutine SayNoSkip(string dialog, int id = -1);
}

public partial class Character
{
	/// Start charcter saying something, unskippable. Useful in background conversations
	public Coroutine SayNoSkip(string dialog, int id = -1)
	{
		if ( m_coroutineSay != null )
		{			
			PowerQuest.Get.StopCoroutine(m_coroutineSay);
			EndSay();
			PowerQuest.Get.OnSay();
		}

		if ( CallbackOnSay != null )
			CallbackOnSay.Invoke(dialog,id);

		m_coroutineSay = CoroutineSayNoSkip(dialog, id);
		return PowerQuest.Get.StartCoroutine(m_coroutineSay); 
	}

	IEnumerator CoroutineSayNoSkip(string text, int id = -1)
	{
		if ( PowerQuest.Get.GetStopWalkingToTalk() )
			StopWalking(); 
		if ( PowerQuest.Get.GetSkippingCutscene() ) // I guess still skippable if in cutscene
			yield break;

		QuestText sayText = StartSay( text, id );
		yield return PowerQuest.Get.WaitForDialog(PowerQuest.Get.GetTextDisplayTime(text), m_dialogAudioSource, PowerQuest.Get.GetShouldSayTextAutoAdvance(), false, sayText);		
		EndSay();
	}	

}

}
