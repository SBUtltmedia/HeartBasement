using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.QuestGui
{

[CanEditMultipleObjects]
[CustomEditor(typeof(GuiControl), true)]
public class ControlEditor : Editor 
{	
	/*
	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		DrawDefaultInspector();
	}
	*/

	public void OnSceneGUI()
	{		
		GuiControl component = (GuiControl)target;
		GuiComponent guiComponent = component.GetComponentInParent<GuiComponent>();
		if ( component == null || guiComponent == null )
			return;
		if ( QuestClickableEditorUtils.OnSceneGUIBaseline( component, component, guiComponent.transform.position ) ) // show baselines relative to gui
		{
			// Gui baselines are multiplied by 100, and added to control baselines multiplied by 10. So each control should sort within itself
			component.UpdateBaseline();
		}
	}
}

public class ControlEditorBase : Editor
{
	public enum eAlignHorizontal { None, Left, Center, Right, Zero }
	public enum eAlignVertical { None, Bottom, Middle, Top, Zero }
	protected static bool m_showSetup = false;
	protected static bool m_showAlign = true;
		

	public virtual void OnSceneGUI()
	{		
		
		GuiControl component = (GuiControl)target;
		GuiComponent guiComponent = component.GetComponentInParent<GuiComponent>();
		if ( component == null || guiComponent == null )
			return;
		if ( QuestClickableEditorUtils.OnSceneGUIBaseline( component, component, guiComponent.transform.position ) ) // show baselines relative to gui
		{
			// Gui baselines are multiplied by 1000, and added to control baselines multiplied by 10. So each control should sort within itself
			component.UpdateBaseline();
		}
	}

	public void UpdateCollider()
	{
		Button component = (Button)target;
		component.UpdateHotspot();
	}
	
	protected void LayoutAlignment(string title)
	{	
		m_showAlign = EditorGUILayout.Foldout(m_showAlign,title,true);
		if ( m_showAlign )
		{
			// EditorGUILayout.LabelField(title);
			GUILayout.BeginHorizontal();
				if ( GUILayout.Button("None"))   AlignContents(eAlignHorizontal.Zero,eAlignVertical.None);
				if ( GUILayout.Button("Left"))   AlignContents(eAlignHorizontal.Left,eAlignVertical.None);
				if ( GUILayout.Button("Center")) AlignContents(eAlignHorizontal.Center,eAlignVertical.None);
				if ( GUILayout.Button("Right"))  AlignContents(eAlignHorizontal.Right,eAlignVertical.None);		
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				if ( GUILayout.Button("None"))   AlignContents(eAlignHorizontal.None,eAlignVertical.Zero);
				if ( GUILayout.Button("Bottom")) AlignContents(eAlignHorizontal.None,eAlignVertical.Bottom);
				if ( GUILayout.Button("Middle")) AlignContents(eAlignHorizontal.None,eAlignVertical.Middle);
				if ( GUILayout.Button("Top"))    AlignContents(eAlignHorizontal.None,eAlignVertical.Top);
			GUILayout.EndHorizontal();		
			GUILayout.Space(5);
		}

	}


	protected void AlignContents(eAlignHorizontal horizontal, eAlignVertical vertical)
	{
		MonoBehaviour component = (target as MonoBehaviour);
		SpriteRenderer spriteComponent = component.GetComponentInChildren<SpriteRenderer>();		
		TextMesh textComponent = component.GetComponentInChildren<TextMesh>();
		FitToObject stretchComponent = component.GetComponentInChildren<FitToObject>(true);
		eAlignHorizontal newX = horizontal;
		eAlignVertical newY = vertical;

		if ( textComponent )
		{			
			if ( horizontal != eAlignHorizontal.None )
				textComponent.alignment = (TextAlignment)( (int)horizontal-1 );
			textComponent.anchor = ToAnchor(textComponent.anchor, horizontal, vertical);
		}
		if ( spriteComponent )
		{	
			// TODO: don't do if sprite is stretched to text, since it'll just get undone.
			RectCentered bounds = GuiUtils.CalculateGuiRectFromSprite( spriteComponent.transform, false, spriteComponent );			
			Vector3 newPos = spriteComponent.transform.localPosition;
			switch (horizontal)
			{
				case eAlignHorizontal.Zero: newPos.x = 0; break;
				case eAlignHorizontal.Left: newPos.x = -bounds.MinX; break;
				case eAlignHorizontal.Center: newPos.x = -bounds.CenterX; break;
				case eAlignHorizontal.Right: newPos.x = -bounds.MaxX; break;
			}
			switch (vertical)
			{
				case eAlignVertical.Zero: newPos.y = 0; break;
				case eAlignVertical.Top: newPos.y = -bounds.MinY; break;
				case eAlignVertical.Middle: newPos.y = -bounds.CenterY; break;
				case eAlignVertical.Bottom: newPos.y = -bounds.MaxY; break;
			}
			spriteComponent.transform.localPosition = newPos;

			
			// Control specitic stuff
			if( component is Button )
			{
				Button button = (component as Button);			
				if ( button.SizeSetting == Button.eSizeSetting.ResizableImage )
				{
					button.CustomSize = GuiUtils.CalculateGuiRectFromSprite( component.transform, false, spriteComponent );	
					UpdateCollider();				
				}	
				if ( button.SizeSetting == Button.eSizeSetting.Image )
				{
					UpdateCollider();		
				}
			}
		}

		EditorUtility.SetDirty(target);
	}

	// converts between seperate horiz, and vert to a single anchor
	protected TextAnchor ToAnchor( TextAnchor current, eAlignHorizontal h, eAlignVertical v )
	{
		// If none, copy existing
		if ( h == eAlignHorizontal.None )
			h = (eAlignHorizontal)((int)current%3)+1;
		if ( v == eAlignVertical.None )
			v = (eAlignVertical)((int)current/3)+1;
		// Copy to TextAnchor
		return (TextAnchor)((int)h-1) + (((int)v-1)*3);
	}

	protected bool UpdateSprite( GuiControl button, string anim, SpriteRenderer spriteComponent )
	{
		GuiComponent gui = button.GetComponentInParent<GuiComponent>();
		if ( gui == null )
			return false;
		AnimationClip clip = gui.GetAnimation(anim);
		if ( clip == null )
			clip = PowerQuestEditor.GetPowerQuest().GetGuiAnimation(anim);		
		Sprite sprite = null;
		if ( clip != null )
		{
			// Get first sprite in animation			
			sprite = PowerQuestEditor.GetFirstSpriteInAnim(clip);
		}
		
		// Try setting sprite
		if ( sprite == null )
			sprite = gui.GetSprite(anim);
		if ( sprite == null )
			sprite = PowerQuestEditor.GetPowerQuest().GetGuiSprite(anim);

		if ( sprite != null )
		{
			spriteComponent.sprite = sprite;
			return true;
		}
		return false;

	}

	protected void UpdateSize(Button button, FitToObject stretchComponent, Collider2D collider2D)
	{
		if ( button.SizeSetting == Button.eSizeSetting.FitText || button.SizeSetting == Button.eSizeSetting.ResizableImage )
		{
			if ( stretchComponent != null && stretchComponent.enabled )
				stretchComponent.UpdateSize();
			if ( collider2D != null )
				UpdateCollider();
		}
	}	 
	/*
	// Shows controls to edit image size. Returns true if it changed
	protected bool OnSceneGuiCustomSize(QuiControl component)
	{			
		RectCentered bounds = QuiRectEditor.OnSceneGuiRectCenter( component.transform, component.CustomSize );
		if ( bounds != component.CustomSize )
		{
			component.CustomSize = bounds;
			
			// update sprite
			SpriteRenderer spriteComponent = component.GetComponentInChildren<SpriteRenderer>();
			if ( spriteComponent != null )
			{
				spriteComponent.size = bounds.Size;
				spriteComponent.transform.localPosition = bounds.Center;
			}
				
			EditorUtility.SetDirty(target);
			return true;
		}
		return false;
	}*/

	public static void OnSceneDrawPivot(Transform transform)
	{
		Handles.color = Color.green;
		Handles.DrawSolidDisc(transform.position,Vector3.back,1);
	}
	
	// Draws bounds of an object. NB: assumes the transform is the child of the object drawing it
	public static RectCentered OnSceneGuiRectCenter(RectCentered bounds, bool canEdit, Transform childTransform = null )
	{
	
		GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel);
		// apply scale
		//bounds.Size = bounds.Size.Scaled(component.transform.lossyScale);
		
		bounds.Min = Utils.SnapRound(bounds.Min);
		bounds.Max = Utils.SnapRound(bounds.Max);

		Vector2 pos = Vector2.zero;
		if ( childTransform != null )
		{
			pos = (Vector2)childTransform.position;

			bounds.MinX =  (bounds.MinX * childTransform.lossyScale.x);
			bounds.MaxX =  (bounds.MaxX * childTransform.lossyScale.x);
			bounds.MinY = (bounds.MinY * childTransform.lossyScale.y);
			bounds.MaxY = (bounds.MaxY * childTransform.lossyScale.y);
		
			bounds.Center = bounds.Center + pos;	
		}		

		// apply width/height
		{	
			Handles.color = Color.yellow;
			GUI.color = Color.yellow;	
			if ( canEdit )
			{
				textStyle.normal.textColor = GUI.color;
				Vector3 handleOffset = new Vector3(-1,1,0);
				{
					Vector3 position =  new Vector3( bounds.MaxX, bounds.MinY, 0);
					position = Handles.FreeMoveHandle( position+handleOffset, Quaternion.identity,1.0f,new Vector3(0,1,0),Handles.DotHandleCap)-handleOffset;
					//Handles.Label(position + new Vector3(5,0,0), "Bounds", textStyle );
					//Handles.color = Color.yellow.WithAlpha(0.5f);
					position.x = Mathf.Max(position.x,bounds.MinX);
					position.y = Mathf.Min(position.y,bounds.MaxY);
					Vector2 corner = Utils.SnapRound(position,PowerQuestEditor.SnapAmount);
					bounds.MaxX = corner.x;
					bounds.MinY = corner.y;

				}
				{
					Vector3 position =  new Vector3( bounds.MinX, bounds.MaxY, 0);
					position = Handles.FreeMoveHandle( position-handleOffset, Quaternion.identity,1.0f,new Vector3(0,1,0),Handles.DotHandleCap)+handleOffset;

					position.x = Mathf.Min(position.x,bounds.MaxX);
					position.y = Mathf.Max(position.y,bounds.MinY);
					Vector2 corner = Utils.SnapRound(position,PowerQuestEditor.SnapAmount);
					bounds.MinX = corner.x;
					bounds.MaxY = corner.y;
				}
			}
			// If haven't changed by full unit, then don't change
			Handles.DrawLine( bounds.Min, new Vector2(bounds.Min.x,bounds.Max.y));
			Handles.DrawLine( bounds.Min, new Vector2(bounds.Max.x,bounds.Min.y));
			Handles.DrawLine( bounds.Max, new Vector2(bounds.Min.x,bounds.Max.y));
			Handles.DrawLine( bounds.Max, new Vector2(bounds.Max.x,bounds.Min.y));			
		}		

		// Unscale, revert pos			
		if ( childTransform != null )
		{
			bounds.Center = bounds.Center - pos;

			bounds.MinX = (bounds.MinX / childTransform.lossyScale.x);
			bounds.MaxX = (bounds.MaxX / childTransform.lossyScale.x);
			bounds.MinY = (bounds.MinY / childTransform.lossyScale.y);
			bounds.MaxY = (bounds.MaxY / childTransform.lossyScale.y);
		}
		
		bounds.Min = Utils.SnapRound(bounds.Min);
		bounds.Max = Utils.SnapRound(bounds.Max);
		
		return bounds;

	}
}

[CanEditMultipleObjects]
[CustomEditor(typeof(Button))]
public class ButtonEditor : ControlEditorBase 
{	
	public override void OnInspectorGUI()
	{
		//EditorGUILayout.LabelField("Initial Setup", EditorStyles.boldLabel);			

		Button component = (Button)target;				
		if ( component == null ) return;

		BoxCollider2D collider2D = component.GetComponent<BoxCollider2D>();
		SpriteRenderer spriteComponent = component.GetComponentInChildren<SpriteRenderer>();		
		QuestText textComponent = component.GetComponentInChildren<QuestText>();
		FitToObject stretchComponent = component.GetComponentInChildren<FitToObject>(true);
		
		
		GuiComponent guiComponent = component.GetComponentInParent<GuiComponent>();
		
		/////////////////////////////////////////////////////////////////////////////////////
		// Script functions

		//GUILayout.Space(5);
		GUILayout.Label("Script Functions",EditorStyles.boldLabel);
		if (  GUILayout.Button("On Click") )
		{
			QuestScriptEditor.Open( guiComponent, PowerQuest.SCRIPT_FUNCTION_CLICKGUI+component.ScriptName, PowerQuestEditor.SCRIPT_PARAMS_ONCLICK_GUI ); 
		}

		GUILayout.Space(5);

		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);


		m_showSetup = EditorGUILayout.Foldout(m_showSetup,"Button Type: "+ component.SizeSetting,true);
		if ( m_showSetup )
		{
			GUILayout.Label("Change Button Type:",EditorStyles.boldLabel);
			if ( GUILayout.Button("Custom") )
			{
				component.SizeSetting = Button.eSizeSetting.Custom;
				// Don't do anything else.
				EditorUtility.SetDirty(target);
			}
			if ( GUILayout.Button("Image") )
			{
				// Setup hotspot, don't touch image	
				component.SizeSetting = Button.eSizeSetting.Image;
		

				// disbale stretch component
				if ( stretchComponent != null )
					stretchComponent.enabled = false;
					
				// reset image scale
				if ( spriteComponent != null )
				{		
					spriteComponent.drawMode = SpriteDrawMode.Simple;
					spriteComponent.transform.localPosition = Vector2.zero;
					spriteComponent.transform.localScale = Vector3.one;		
				}

				if ( collider2D == null )
					collider2D = component.gameObject.AddComponent<BoxCollider2D>();
				collider2D.isTrigger = true;
				
				UpdateCollider();
				EditorUtility.SetDirty(target);
			}
			if ( GUILayout.Button("Resizable Image") )
			{
				// Set the scale/size of the image to match a "size" field
				component.SizeSetting = Button.eSizeSetting.ResizableImage;

				if ( collider2D == null )
					collider2D = component.gameObject.AddComponent<BoxCollider2D>();
				collider2D.isTrigger = true;
			
				// disbale stretch component
				if ( stretchComponent != null )
					stretchComponent.enabled = false;
						
				// Set image to sliced
				if ( spriteComponent != null )
				{
					spriteComponent.transform.localScale = Vector3.one;				
					spriteComponent.drawMode = SpriteDrawMode.Sliced;
				
					// Set customsize from collider
					component.CustomSize = new RectCentered( collider2D.offset.x, collider2D.offset.y, collider2D.size.x, collider2D.size.y);	
					component.CustomSize.RemovePadding(component.HotspotPadding);
					spriteComponent.size= component.CustomSize.Size;
					spriteComponent.transform.localPosition = component.CustomSize.Center;
				}
				EditorUtility.SetDirty(target);
			
			}
			if ( GUILayout.Button("Fit Text") )
			{
				// Add Stretch to image
				// Setup hotspot
				component.SizeSetting = Button.eSizeSetting.FitText;

				if ( stretchComponent == null && spriteComponent != null )
				{
					stretchComponent = spriteComponent.gameObject.AddComponent<FitToObject>();
					stretchComponent.FitToObjectWidth(textComponent.gameObject);
					stretchComponent.FitToObjectHeight(textComponent.gameObject); // Not sure if should do X as well as Y...
					stretchComponent.Padding = component.HotspotPadding;
				}			
				// enable stretch component if it was simply disabled
				if ( stretchComponent != null )
				{
					stretchComponent.enabled = true;
				}
				if ( collider2D == null )
					collider2D = component.gameObject.AddComponent<BoxCollider2D>();
				collider2D.isTrigger = true;
				if ( spriteComponent != null )
				{
					spriteComponent.drawMode = SpriteDrawMode.Sliced;
					spriteComponent.transform.localScale = Vector3.one;
				}

				UpdateSize(component, stretchComponent,collider2D);
				EditorUtility.SetDirty(target);
			}
		}

		if ( component.SizeSetting == Button.eSizeSetting.ResizableImage || component.SizeSetting == Button.eSizeSetting.FitText )
		{
			if ( spriteComponent != null && spriteComponent.sprite != null )
			{			
				// Todo: Check sprite is set to rect
			}
		}		

		
		/////////////////////////////////////////////////////////////////////////////////////
		// Text field
		//
		if( textComponent != null )
		{
			string text = textComponent.text;
			EditorGUILayout.LabelField("Button Text");
			text = EditorGUILayout.TextArea(textComponent.text);
			if ( string.Equals(text,textComponent.text) == false )
			{
				textComponent.text = text;
				UpdateSize(component, stretchComponent,collider2D);
				EditorUtility.SetDirty(target);
			}
		}

		//
		// Manual size
		//
		if ( component.SizeSetting == Button.eSizeSetting.ResizableImage && collider2D != null )
		{			
			// show manual image sizer
			//RectCentered sizeOverride = new RectCentered( collider2D.offset.x, collider2D.offset.y, collider2D.size.x, collider2D.size.y);

			SerializedObject serializedObj = new SerializedObject(component);
			SerializedProperty prop = serializedObj.FindProperty("m_customSize");
			if ( prop == null )
				Debug.LogError("Didn't find property");
			EditorGUILayout.PropertyField(prop,new GUIContent("Size"),true);
			if ( serializedObj.ApplyModifiedProperties() )
			{
				// On change, update collider
				RectCentered customSizeRect = component.CustomSize;
				customSizeRect.AddPadding(component.HotspotPadding);
				collider2D.size = component.CustomSize.Size;
				collider2D.offset = component.CustomSize.Center;

				if ( spriteComponent != null )
				{
					spriteComponent.size = component.CustomSize.Size;
					spriteComponent.transform.localPosition = component.CustomSize.Center;
				}
				EditorUtility.SetDirty(target);
			}
		}

		GUILayout.Space(5);
				
		// Alignment buttons
		LayoutAlignment("Align Button Contents");

		string anim = component.Anim;
		Color col = component.Color;
		DrawDefaultInspector();

		if ( anim != component.Anim )
		{
			// update default sprite
			UpdateSprite(component, component.Anim, spriteComponent);
		}
		
		if ( col != component.Color )
		{
			// update default sprite				
			if ( textComponent != null && (component.ColorWhat == Button.eColorUse.Text || component.ColorWhat == Button.eColorUse.Both))
				textComponent.color = component.Color;
			if ( spriteComponent != null && (component.ColorWhat == Button.eColorUse.Image || component.ColorWhat == Button.eColorUse.Both))
				spriteComponent.color = component.Color;
		}

		
		GUILayout.Label("Utils",EditorStyles.boldLabel);		
		
		if ( GUILayout.Button("Set hotspot From Contents") )
		{
			UpdateCollider();
			EditorUtility.SetDirty(target);
		}
		/* NB: Can't rename while prefab staged, it's a nightmare to figure out, so giving up for now
		if ( GUILayout.Button("Rename") )
		{			
			// Need to use gui
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.GuiControl, component.ScriptName, RenameGuiControl );
		}
		
		void RenameGuiControl( GameObject prefab, eQuestObjectType questType, string newName )
		{
			PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject(prefab,questType,newName);
		}*/
	}
	

	

	public override void OnSceneGUI()
	{		
		// Call up to parent for baseline
		base.OnSceneGUI();
		
		Button component = (Button)target;
		OnSceneDrawPivot(component.transform);
		BoxCollider2D collider2D = component.GetComponent<BoxCollider2D>();		
		if ( component.SizeSetting == Button.eSizeSetting.ResizableImage )
		{		
			RectCentered bounds = OnSceneGuiRectCenter( component.CustomSize, true,component.transform );
			if ( bounds != component.CustomSize )
			{
				component.CustomSize = bounds;

				// update sprite
				SpriteRenderer spriteComponent = component.GetComponentInChildren<SpriteRenderer>();
				if ( spriteComponent != null )
				{
					spriteComponent.size = bounds.Size;
					spriteComponent.transform.localPosition = bounds.Center;
				}

				// update hotspot (with added padding)
				bounds.AddPadding(component.HotspotPadding);
				collider2D.size = bounds.Size;
				collider2D.offset = bounds.Center;
				
				EditorUtility.SetDirty(target);
			}
		}
		else 
		{
			//SpriteRenderer spriteComponent = component.GetComponentInChildren<SpriteRenderer>();
			OnSceneGuiRectCenter( GuiUtils.CalculateGuiRectFromSprite(component.transform,true), false, component.transform  );
		}

	}
}


}
