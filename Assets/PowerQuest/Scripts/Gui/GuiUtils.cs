using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using PowerTools;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{

public static class GuiUtils
{

	// Returns rect around the sprite if one exists, otherwise any mesh renderer (eg: text). Local to the transform of the object.
	// TODO: These calculate rect classes are SUUUUPER confusing. Can't tell what's local, what's world space. It loops in on itself for reasons... etc etc
	public static RectCentered CalculateGuiRect(Transform transform, bool includeChildren, SpriteRenderer spriteRenderer = null, MeshRenderer textMesh = null, Transform excludeChildren = null )
	{
		// First check if it's a control, which specifies it's own Rect
		if ( spriteRenderer == null && textMesh == null )
		{
			bool found = false;
			RectCentered result = new RectCentered();
			GuiControl[] controls = includeChildren ? transform.GetComponentsInChildren<GuiControl>(false) : new GuiControl[]{transform.GetComponent<GuiControl>()};
			foreach ( GuiControl control in controls )
			{ 
				if ( control != null && (transform == control.transform || control.transform != excludeChildren) )
				{					
					 RectCentered rect = control.GetRect(transform);
					 rect.UndoTransform(transform);
					 if ( found == false )
						result = rect;
					else 
						result.Encapsulate(rect);
					found = true;
				}
			}
			if ( found )
				return result;
		}

		// If it's not a control, get the size from the sprite renderer or textmesh
		return CalculateGuiRectInternal(transform, includeChildren,spriteRenderer,textMesh,excludeChildren);
	}

	public static RectCentered CalculateGuiRectInternal(Transform transform, bool includeChildren, SpriteRenderer spriteRenderer = null, MeshRenderer textMesh = null, Transform excludeChildren = null )
	{	

		if ( spriteRenderer == null )
			spriteRenderer = includeChildren ? transform.GetComponentInChildren<SpriteRenderer>(false) : transform.GetComponent<SpriteRenderer>();

		if ( spriteRenderer != null && spriteRenderer.sprite != null )
			return CalculateGuiRectFromSprite(transform, includeChildren, spriteRenderer, excludeChildren);
		

		// Fallback to renderer if no sprite found
		return CalculateGuiRectFromRenderer(transform, includeChildren, textMesh, excludeChildren);
	}

	public static RectCentered CalculateGuiRectFromSprite(Transform transform, bool includeChildren, SpriteRenderer spriteRenderer = null, Transform excludeChildren = null)
	{
		RectCentered bounds = RectCentered.zero;
		if ( spriteRenderer == null )
			spriteRenderer = includeChildren ? transform.GetComponentInChildren<SpriteRenderer>() : transform.GetComponent<SpriteRenderer>();
		
		if ( spriteRenderer != null && (transform != spriteRenderer.transform && spriteRenderer.transform == excludeChildren) )
			return bounds;

		Sprite sprite = spriteRenderer == null ? null : spriteRenderer.sprite;
		if ( spriteRenderer == null || sprite == null )
			return bounds;
			
		if ( spriteRenderer.drawMode != SpriteDrawMode.Simple )
		{
			// Sliced sprites
			bounds.Center = spriteRenderer.bounds.center - transform.position;
			bounds.Size=spriteRenderer.size;
		}
		else 
		{
			// Non-sliced sprites
			
			bool first = true;
			bool phys = false;
			if ( sprite.bounds.size.x < 32 || sprite.bounds.size.y < 32)
			{		
				phys = true;
				List<Vector2> shape = new List<Vector2>();
				for (int i = 0; i < sprite.GetPhysicsShapeCount(); ++i )
				{
					int count = sprite.GetPhysicsShape(i,shape);		
					for ( int j = 0; j < count; ++j )					
					{
						Vector2 point = shape[j];
						if ( first )
							bounds = new RectCentered(point,point);
						else 
							bounds.Encapsulate( point );
						first = false;
					}
				
				}
			}
			else 
			{
				foreach ( Vector2 point in sprite.vertices )
				{
					if ( first )
						bounds = new RectCentered(point,point);
					else 
						bounds.Encapsulate( point );
					first = false;
				}
			}

			if ( sprite.textureRectOffset != Vector2.zero )
			{
				// NB: non-trimmed sprites will have extra padding				
				bounds.Width = bounds.Width - 4;
				bounds.Height = bounds.Height - 4;
			}
			else if ( phys )
			{	
				bounds.Width = bounds.Width - 2;
				bounds.Height = bounds.Height - 2;
			}

			
			
			// If transform is'nt teh renderer's transform, we need to offset by that local tranform
			if ( transform != spriteRenderer.transform )
			{
				// Ok, now trying it! // NB: not worrying about scale here for now. that'd get hairy...? 
				bounds.Size = bounds.Size.Scaled(spriteRenderer.transform.localScale);

				bounds.Center += (Vector2)(spriteRenderer.transform.position-transform.position);
			}

		}
					
		return bounds;		
	}

	public static RectCentered CalculateGuiRectFromRenderer(Transform transform, bool includeChildren, MeshRenderer renderer = null, Transform exclude = null)
	{
		RectCentered bounds = new RectCentered();
		
		if ( renderer == null )
			renderer = includeChildren ? transform.GetComponentInChildren<MeshRenderer>() : transform.GetComponent<MeshRenderer>();
		
		if ( renderer != null && transform != renderer.transform && renderer.transform == exclude )
			return RectCentered.zero;

		if ( renderer == null )
			return bounds;


		bounds = new RectCentered(renderer.bounds);
		bounds.UndoTransform(transform);
		//bounds.Center = bounds.Center - (Vector2)transform.position;

		return bounds;
	}

	public static Camera FindGuiCamera()
	{
		Camera[] cameras = new Camera[10];
			
		int count = Camera.GetAllCameras(cameras);

		// Take a guess at which is a gui camera			
		for ( int i = 0; i < count && i < cameras.Length; ++i )
		{
			Camera cam = cameras[i];
			if ( cam.gameObject.layer == 5 || cam.gameObject.name.Contains("GUI") )
				return cam;
		}

		// Fall back to first camera
		if ( cameras.Length > 0 )
			return cameras[0];

		return null;
	}
}

}
