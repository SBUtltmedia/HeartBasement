using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTools
{

[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("PowerTools/PowerSprite")]
public class PowerSprite : MonoBehaviour 
{
	#region Definitions
	public static readonly Color COLOR_DISABLED = new Color(1,1,1,0);
	static readonly string STR_SHADER = "Sprites/PowerSprite";
	static readonly string STR_SHADER_OUTLINE= "Sprites/PowerSpriteOutline";
	static readonly string STR_SPROP_TINT = "_Tint";
	static readonly string STR_SPROP_OUTLINE = "_Outline";
	static readonly string STR_SPROP_OFFSET = "_Offset";

	#endregion
	#region vars: Editor

	[SerializeField] Color m_tint = COLOR_DISABLED;
	[SerializeField] Color m_outline = COLOR_DISABLED;
	[SerializeField] Vector2 m_offset = Vector2.zero;
	[SerializeField] Shader m_shaderOverride = null;

	#endregion
	#region vars: Private

	SpriteRenderer m_renderer = null;
	static Shader s_shader = null;
	static Shader s_shaderOutline = null;
	Material m_materialCached = null;
	bool m_outlineEnabled = false;	
	SpriteAnimNodes m_nodes = null;
	MaterialPropertyBlock m_block = null;

	#endregion
	#region Funcs: Public

	public Color Tint 
	{ 
		get { return m_tint; } 
		set 
		{
			if ( m_tint != value )
			{
				m_tint = value;
				UpdateOutline();
				UpdateTint();
			}
		}
	}


	public Color Outline 
	{ 
		get { return m_outline; } 
		set 
		{
			if ( m_outline != value )
			{
				m_outline = value;
				UpdateOutline();
				UpdateTint();
			}
		}
	}

	public Vector2 Offset
	{ 
		get { return m_offset; } 
		set 
		{
			if ( m_offset != value )
			{
				m_offset = value;
				UpdateOffset();
			}
		}
	}

	SpriteRenderer Renderer {get
	{
		if ( m_renderer == null )
			m_renderer = GetComponent<SpriteRenderer>();
		return m_renderer;
	} }


	public void AlignLeft()
	{
		CheckMaterial();
		m_offset.x = Renderer.sprite.bounds.size.x*0.5f;
		UpdateOffset();
	}
	public void AlignRight()
	{
		CheckMaterial();
		m_offset.x = -Renderer.sprite.bounds.size.x*0.5f;
		UpdateOffset();
	}
	public void AlignTop()
	{
		CheckMaterial();
		m_offset.y = -Renderer.sprite.bounds.size.y*0.5f;
		UpdateOffset();
	}
	public void AlignBottom()
	{
		CheckMaterial();
		m_offset.y = Renderer.sprite.bounds.size.y*0.5f;

		UpdateOffset();
	}

	public void AlignCenter()
	{
		CheckMaterial();
		m_offset.x = 0;
		UpdateOffset();
	}

	public void AlignMiddle()
	{
		CheckMaterial();
		m_offset.y = 0;
		UpdateOffset();
	}


	public void Snap()
	{
		CheckMaterial();
		if ( Renderer.sprite != null )
			m_offset = Snap(m_offset,1.0f/Renderer.sprite.pixelsPerUnit);
		UpdateOffset();
	}

	// Gets the world position of a sprite anim node, with the sprite offset included.
	public Vector2 GetNodePosition(int node)
	{
		// Lazy get the nodes component
		if ( m_nodes == null )
			m_nodes = GetComponent<SpriteAnimNodes>();
			
		Vector2 spriteOffset = Offset;
		spriteOffset.Scale(transform.localScale);
		Vector2 result = spriteOffset;

		if ( m_nodes == null )
		{
			// Just return position with sprite offset
			result += (Vector2)transform.position;
		}
		else 
		{
			result += (Vector2)m_nodes.GetPosition(node);
		}
		return result;
	}

	public float GetNodeAngle(int node)
	{
		// Lazy get the nodes component
		if ( m_nodes == null )
			m_nodes = GetComponent<SpriteAnimNodes>();
		return m_nodes.GetAngle(node);		
	}
	
	public Shader GetShaderOverride() { return m_shaderOverride; }
	public void SetShaderOverride(Shader shader)
	{
		m_shaderOverride = shader;
		m_materialCached = null;
		CheckMaterial();
	
	}

	#endregion
	#region Funcs: Private/Internal

	void Reset()
	{
		CheckMaterial();
	}

	public void OnValidate()
	{
		// DL TODO: Apparently OnValidate can cause slowdowns on saving in the editor? 
		if ( Application.isPlaying == false )
		{		
			CheckMaterial(true);
			UpdateTint();
			UpdateOutline();
			UpdateOffset();
		}
	}


	// Use this for initialization
	void Start() 
	{
		UpdateAll();
	}

	bool CheckMaterial(bool onValidate = false)
	{

		// Apply outline if it hadn't been previously enabled, but is set
		bool shouldApplyOutline = ( m_outlineEnabled == false && m_outline.a > 0 );

		if ( s_shader == null )
			s_shader = Shader.Find( STR_SHADER );		
		if ( s_shaderOutline == null )
			s_shaderOutline = Shader.Find( STR_SHADER_OUTLINE );
		
		if ( s_shader == null )
			return false;
		
		if ( m_materialCached != null && shouldApplyOutline == false && m_shaderOverride == null )
			return true;

		Shader shader = m_shaderOverride != null ? m_shaderOverride : shouldApplyOutline ? s_shaderOutline : s_shader;
				
		if ( Renderer != null && shader != null)
		{			
			if ( Application.isPlaying == false || (Application.isEditor && onValidate))
				m_materialCached = Renderer.sharedMaterial;
			else 
				m_materialCached = Renderer.material;
			
			if ( m_materialCached == null || m_materialCached.shader != shader )
			{
				m_materialCached = new Material(shader);
				if ( Application.isPlaying == false )
					Renderer.sharedMaterial = m_materialCached;
				else 
					Renderer.material = m_materialCached;
			}
			
		}

		if ( shouldApplyOutline && m_materialCached != null)
			m_outlineEnabled = true;
		return m_materialCached != null;
	}


	void UpdateAll()
	{
		CheckMaterial(false);

		var block = StartPropertyBlock();
		block.SetColor(STR_SPROP_TINT, m_tint);
		block.SetColor(STR_SPROP_OUTLINE, m_outline);
		block.SetVector(STR_SPROP_OFFSET, m_offset);
		EndPropertyBlock();
	}

	void UpdateTint()
	{
		StartPropertyBlock().SetColor(STR_SPROP_TINT, m_tint);
		EndPropertyBlock();
		/*
		if ( CheckMaterial() == false )
			return;		
		m_materialCached.SetColor(STR_SPROP_TINT, m_tint);	
		*/
	}

	void UpdateOutline()
	{
		// Check material for outline since it might need to change the shader
		if ( CheckMaterial() == false )
			return;
		StartPropertyBlock().SetColor(STR_SPROP_OUTLINE, m_outline);
		EndPropertyBlock();
		/*
		if ( CheckMaterial() == false )
			return;
		m_materialCached.SetColor(STR_SPROP_OUTLINE, m_outline);	
		*/
	}

	void UpdateOffset()
	{
		StartPropertyBlock().SetVector(STR_SPROP_OFFSET, m_offset);
		EndPropertyBlock();
				/*
		if ( CheckMaterial() == false )
			return;
		m_materialCached.SetVector(STR_SPROP_OFFSET, m_offset);	
		*/

		
	}

	MaterialPropertyBlock StartPropertyBlock() 
	{ 
		if ( m_block == null )
			m_block = new MaterialPropertyBlock();
		Renderer.GetPropertyBlock(m_block); 
		return m_block; 
	}
	void EndPropertyBlock() { Renderer.SetPropertyBlock(m_block); }

	
	#endregion
	#region Util methods

	static Vector2 Snap( Vector2 pos, float snapTo )
	{	
		return new Vector2( Snap(pos.x, snapTo), Snap(pos.y, snapTo) );
	}

	static float Snap( float pos, float snapTo = 1)
	{		
		if ( snapTo < 0.001f )
			return pos;
		return Mathf.Round(pos / snapTo) * snapTo;
	}

	#endregion

}

}
