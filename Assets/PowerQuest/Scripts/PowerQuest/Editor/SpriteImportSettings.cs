using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using PowerTools.Quest;

class PowerQuestAssetPostProcessor : AssetPostprocessor
{

	void OnPostprocessTexture(Texture2D texture)
	{
		if ( PowerQuestEditor.Get != null && PowerQuestEditor.GetPowerQuest() != null && PowerQuestEditor.GetPowerQuest().GetSnapToPixel() )
		{
			// Check texture resolution is divisible by 2 and give warning if its not
			if ( (texture.width%2.0f != 0.0f || texture.height % 2.0f != 0.0f) && string.IsNullOrEmpty(texture.name) == false )
			{
				Debug.LogWarning($"Imported sprite {texture.name} size ({texture.width}, {texture.height}) isn't even/divisible by 2. This may result in wibbly-wobbly visuals, since when they're centered they'll be off-grid by half a pixel.");
			}
		}
	}

	void OnPreprocessTexture()
	{

		TextureImporter importer = (TextureImporter)assetImporter;
		if ( importer == null )
			return;

		bool pixelSnap = ( PowerQuestEditor.IsReady() && PowerQuestEditor.GetPowerQuest().GetSnapToPixel() );
				
		#if UNITY_2019_3_OR_NEWER		
			// Ok, so the check to avoid forcing settings is broken for unity 2019.3 and above. Great.
			// In leau of that, lets check the various settings, and only apply if it's default.				
			if ( importer.spritePixelsPerUnit != 100 || string.IsNullOrEmpty(importer.spritePackingTag) == false || importer.textureCompression != TextureImporterCompression.Compressed )
				return;
		#else
		// If the metafile exists, don't reimport
		if ( File.Exists(AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath) ) )
			return;
		#endif

		importer.textureType = TextureImporterType.Sprite;
		importer.mipmapEnabled = false;
		importer.spritePixelsPerUnit = PowerQuestEditor.IsReady() ? PowerQuestEditor.GetPowerQuest().EditorGetDefaultPixelsPerUnit() : 1;
		importer.textureCompression = TextureImporterCompression.Uncompressed;
		if (pixelSnap)
			importer.filterMode = FilterMode.Point;

	}



}
