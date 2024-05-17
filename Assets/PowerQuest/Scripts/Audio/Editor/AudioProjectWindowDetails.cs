using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PowerTools;
using System.Reflection;


namespace PowerTools.Quest
{

[InitializeOnLoad]
public static class AudioProjectWindowDetails
{
    static AudioProjectWindowDetails()
	{
		EditorApplication.projectWindowItemOnGUI += DrawAssetDetails;
	}

	static void DrawAssetDetails(string guid, Rect rect)
	{
		Event ev = Event.current;
		if (Application.isPlaying || (ev.type != EventType.Repaint && ev.type != EventType.MouseUp && ev.type != EventType.MouseDown/*  && ev.type != EventType.ExecuteCommand*/ ) || !IsMainListAsset(rect))		
			return;
		
		string assetPath = AssetDatabase.GUIDToAssetPath(guid);
		if (AssetDatabase.IsValidFolder(assetPath))		
			return;
		
		Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
		if (asset == null)	// this entry could be Favourites or Packages. Ignore it.
			return;

		if ( asset is AudioClip )
		{
			if ( DrawAudioClip(rect, asset as AudioClip) )
				return;
		}

		GameObject gobj = (asset as GameObject);
		if ( gobj == null )
			return;
		if ( DrawAudioCue(rect,gobj) )
			return;
	}

	static bool DrawAudioClip(Rect rect, AudioClip clip)
	{		
		// Draw "New Cue" button, or "Add Cue" if there's an selected audiocue selected
		if ( clip == null )
			return false;
		
		EditorLayouter layout = new EditorLayouter(rect);
		layout
			.Stretched // name
			.Fixed(80) // Add
			//.Space
			.Fixed(40); // Play
		layout.Skip();

		GUIStyle style = new GUIStyle(EditorStyles.miniButton);
		style.fixedHeight = style.fixedHeight-4f;
		
		if ( Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<AudioCue>() != null)
		{
			if ( GUI.Button(layout,"Add to Cue", style) )
			{
				AudioCue cue = Selection.activeGameObject.GetComponent<AudioCue>();				
				cue.m_sounds.Add( new AudioCue.Clip() { m_sound = clip, m_weight = 100 } );
				cue.GetShuffledIndex().SetWeights( cue.m_sounds, (item)=>item.m_weight );				
				EditorUtility.SetDirty(Selection.activeGameObject);
			}
		}
		else 
		{
			if ( GUI.Button(layout,"New Cue", style) )
			{
				if ( Selection.objects.Length > 0 && System.Array.Find(Selection.objects, item=>item == clip) )
					AudioCueEditor.ContextCreateAudioCue(new MenuCommand(null)); // create all seelected into single cue
				else 
					AudioCueEditor.CreateAudioCue(new AudioClip[]{clip}); // create new cue from single item
			}
		}
		// Play button
		if ( GUI.Button(layout,"Play",style))
		{
			StopAllClips();
			PlayClip(clip,0,false);
		}
		return true;
	
	}

	public static void StopAllClips()
	{
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod(
			"StopAllPreviewClips",
			BindingFlags.Static | BindingFlags.Public
		);
		method?.Invoke(null, null);
	}

	public static void PlayClip(AudioClip clip, int startSample, bool loop)
	{
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod(
			"PlayPreviewClip",
			BindingFlags.Static | BindingFlags.Public,
			null,
			new System.Type[] {
			typeof(AudioClip),
			typeof(System.Int32),
			typeof(System.Boolean)
		},
		null
		);
		method?.Invoke(
			null,
			new object[] { clip,startSample,loop }
		);
	
		//SetClipSamplePosition(clip, startSample);
	}

	static bool DrawAudioCue(Rect rect, GameObject gobj)
	{
		AudioCue cue = gobj.GetComponent<AudioCue>();
		if ( cue == null )
			return false;
		
		const int width = 40;
		rect.x += rect.width-width;
		rect.width = width;

		GUIStyle style = new GUIStyle(EditorStyles.miniButton);
		style.fixedHeight = style.fixedHeight-4f;
		
		if ( SystemAudio.GetValid() && SystemAudio.IsPlaying(gobj.name) )
		{
			if ( GUI.Button(rect,"Stop", style) )
			{
				StopAllClips();
				if ( Application.isEditor && SystemAudio.GetValid() )
						GameObject.DestroyImmediate( SystemAudio.Get.gameObject );
				SystemAudio.Stop(gobj.name,0.1f);
			}			
		}
		else if ( GUI.Button(rect,"Play", style) )
		{			
			if ( Application.isEditor && SystemAudio.GetValid() )
					GameObject.DestroyImmediate( SystemAudio.Get.gameObject );
					
			StopAllClips();
			SystemAudio.Play(cue);//(asset as GameObject).GetComponent<AudioCue>());
		}
		return true;
	}

	static bool IsMainListAsset(Rect rect)
	{
		// Don't draw details if project view shows large preview icons:
		if (rect.height > 20)
		{
			return false;
		}
		// Don't draw details if this asset is a sub asset:
		if (rect.x > 16)
		{
			return false;
		}
		return true;
	}
	
}

}
