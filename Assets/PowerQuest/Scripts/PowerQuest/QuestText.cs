#define SET_ALL_FONTS
#define FONT_SNAPPING 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using PowerTools.Quest.Text;
using System.Text.RegularExpressions;

namespace PowerTools.Quest
{


[System.Serializable]
public class TextOutline
{
	public enum eDirection
	{
		Top = 1<<0,
		Bottom = 1<<1,
		Left = 1<<2,
		Right = 1<<3,
		TopLeft = 1<<4,
		TopRight = 1<<5,
		BottomLeft = 1<<6,
		BottomRight = 1<<7
	};

	[BitMask(typeof(eDirection))]
	public int m_directions = 0;
	public float m_width = 1.0f;
	public Color m_color = Color.black;	
}

[ExecuteInEditMode]
[RequireComponent(typeof(TextMesh))]
public class QuestText : MonoBehaviour 
{
	#if FONT_SNAPPING
	static readonly string STR_SHADER_PIXEL = "Powerhoof/Pixel Text Shader";
	#else
	static readonly string STR_SHADER_PIXEL = "Powerhoof/Sharp Text Shader";
	#endif
	static readonly string STR_SHADER = "GUI/Text Shader";

	static readonly Vector3[] SHADOW_OFFSETS = new Vector3[]	
	{
		Vector3.up, 
		Vector3.down, 
		Vector3.left, 
		Vector3.right,
		Vector3.up + Vector3.left,
		Vector3.up + Vector3.right,
		Vector3.down + Vector3.left,
		Vector3.down + Vector3.right,
	};
	
	static readonly string STR_COLOR_START = "<color=#fff0>";
	static readonly string STR_COLOR_END = "</color>";

	[System.Serializable]	
	public class TextSpriteData
	{ 
		public string m_tag = string.Empty;
		public float m_offsetY = 0;
		public Sprite m_sprite = null;
		public string m_platform = string.Empty;

		public override string ToString(){ return m_tag; }
	}

	[Multiline]
	[SerializeField] string m_text = "";
	[SerializeField] bool m_localize = false;
	//[Header("Sorting")]
	[SerializeField] string m_sortingLayer = "Default";
	[SerializeField] int m_orderInLayer = 0;
	//[Header("Wrap/Truncate Settings")]
	[Tooltip("Width in game units to wrap text (0 = disabled)")]
	[SerializeField, Delayed] float m_wrapWidth = 0.0f;
	[Tooltip("Whether to cut the text (adding ellipsis) instead of wrapping it")]
	[SerializeField] bool m_truncate = false;
	[Tooltip("Whether to use more expensive word wrap that tries to keep uniform line width")]
	[SerializeField] bool m_wrapUniformLineWidth = false;
	[Tooltip("Min line width when uniform line width is used, or Keep On Screen is set and the width is shortened. (0 = disabled)")]
	[SerializeField, Delayed] float m_wrapWidthMin = 0.0f;
	[Tooltip("Ensures the dialog is on screen when created (eg. offscreen character dialog)")]
	[SerializeField] bool m_keepOnScreen = false;
	[SerializeField] Padding m_screenPadding = new Padding(8,8,8,8);
	//[Header("Appearance")]
	[SerializeField] Shader m_shaderOverride = null;
	[Tooltip("If true, pixel filtering is applied for pixel art games, otherwise bilinear.")]
	[SerializeField] bool m_setFiltering = true;
	[SerializeField] TextOutline m_outline = null;	
	[Tooltip("Set for a typewriter effect, in characters per second. 0.05 is a good starting value.")]
	[SerializeField] float m_typeSpeed = 0;
	
	static Shader s_shader = null;
	bool m_materialSet = false;

	TextMesh m_mesh = null;
	TextWrapper m_textWrapper = null;
	MeshRenderer m_meshRenderer = null;

	Transform m_attachObject = null;          // If set, text will follow this object
	Vector2 m_attachObjOffset = Vector2.zero; // If m_attachObject is set, this will be offset from that transform. Different than "offset" which is the camera-space offset.
	Vector2 m_attachWorldPos = Vector2.zero;  // Used to attach to a world pos but show on a gui camera. 
	Vector2 m_attachOffset = Vector2.zero;    // Offset from the "camera space", to make text map smoothly from pixel to non-pixel cameras.

	bool m_editorRefresh = false; // Required, since can't delete outline text in OnValidate. Maybe better not to delete, just to hide (unless not in editor)

	// Use the saved unlocalized text to update the text when translation changes
	[SerializeField, HideInInspector] string m_unlocalizedText = null;
	string m_wrappedText = null;

	[SerializeField, HideInInspector] List<TextMesh> m_outlineMeshes = null;
	bool m_wasRendererEnabled = true;
	
	Vector2 m_rectSize = Vector2.zero;

	int m_typeChar = 0;

	static string s_textSpritePlatform = string.Empty;

	// Callback if you want to add sound to typing in another component or something.
	public System.Action CallbackOnTypeCharacter = null;	

	public string text { get { return m_text; } set { SetText(value); } }
	public Color color 
	{	
		get { return CheckTextMesh() ? m_mesh.color : Color.white; } 
		set 
		{ 
			if ( CheckTextMesh() == false )
				return;
			
			//bool alphaChange = m_mesh.color.a != value.a;
			m_mesh.color = value;			
			
			// update alpha of new color
			foreach( TextMesh item in m_outlineMeshes) 
			{
				if ( item != null)
					item.color = item.color.WithAlpha(color.a * m_outline.m_color.a);
			}
		}
	}

	public TextAlignment alignment { get { return m_mesh?.alignment ?? TextAlignment.Center; } 
		set 
		{
			if ( m_mesh != null )
				m_mesh.alignment = value;
			RefreshText();
		}
	}
	public TextAnchor anchor { get { return m_mesh?.anchor ?? TextAnchor.MiddleCenter; } 
		set 
		{
			if ( m_mesh != null )
				m_mesh.anchor = value;
			RefreshText();
		}
	}

	public float WrapWidth { get { return m_wrapWidth; } set { m_wrapWidth = value; } }
	public bool Truncate { get { return m_truncate; } set { m_truncate = value; } }
	public bool WrapUniformLineWidth { get { return m_wrapUniformLineWidth; } set { m_wrapUniformLineWidth = value; } }

	public string SortingLayer { get{ return m_sortingLayer; } set{ m_sortingLayer = value; } }
	public int OrderInLayer { get{ return m_orderInLayer; } set
	{ 
		if ( m_orderInLayer == value )
			return;
		m_orderInLayer = value; 
		if (m_meshRenderer)
			m_meshRenderer.sortingOrder = m_orderInLayer;
		// Update any background text too!
		if ( m_outlineMeshes != null )
		{

			foreach( TextMesh mesh in m_outlineMeshes )
			{
				if ( mesh != null ) 
					mesh.GetComponent<Renderer>().sortingOrder = m_orderInLayer;				
			}
		}
	} }
	public bool GetShouldLocalize() { return m_localize; }
	public float GetWrapWidth() { return m_wrapWidth; }
	public void SetWrapWidth(float width)
	{ 
		m_wrapWidth = width; 		
		RefreshText();
	}

	public TextOutline GetOutline() { return m_outline; }
	public void SetOutline( TextOutline outline ) 
	{ 
		if ( m_outline != null && (outline == null || m_outline.m_directions != outline.m_directions) && m_outlineMeshes != null )
		{
			// remove existing outline meshes
			for ( int i = 0; i < m_outlineMeshes.Count; ++i ) 
			{
				if ( m_outlineMeshes[i] != null )
				{
					if ( Application.isPlaying )
						GameObject.Destroy(	m_outlineMeshes[i].gameObject );
					else 
						GameObject.DestroyImmediate( m_outlineMeshes[i].gameObject );
				}
			}
			m_outlineMeshes.Clear();
		}
		m_outline = outline;
		RefreshText();
	}

	public string GetUnlocalizedText() { return (m_localize == false || string.IsNullOrEmpty(m_unlocalizedText)) ? m_text : m_unlocalizedText; }

	/// The text sprite platform is for having text sprites that change depending on platform or controller
	public static void SetTextSpritePlatform(string platform) { s_textSpritePlatform = platform; }

	// Use this for initialization
	void Start() 
	{
		if ( CheckTextMesh() )
		{
			CheckMaterial();
			RefreshText();
		}
	}


	public void OnLanguageChange()
	{
		RefreshText();
	}

	public void SetText(string text)
	{
		// Don't bother if text is the same
		if (text == null) 
			text = string.Empty;
		m_unlocalizedText = text;		
		text = SystemText.Localize( text );		
		text = ParseImages(text);
		m_text = text;
		m_typeChar = 0;

		if ( CheckMaterial() )
		{
			// Set sorting layer
			m_meshRenderer.sortingOrder = m_orderInLayer;
			int id = UnityEngine.SortingLayer.NameToID(m_sortingLayer);
			if ( UnityEngine.SortingLayer.IsValid(id) )
				m_meshRenderer.sortingLayerID = id;
			
			if ( m_wrapWidth > 0.0f )
			{
				if ( m_textWrapper == null  )
					m_textWrapper = new TextWrapper(m_mesh);
				
				if ( m_keepOnScreen == false )
				{
					if ( m_truncate )
						text = m_textWrapper.Truncate(m_text, m_wrapWidth);
					else if ( m_wrapUniformLineWidth )
						text = m_textWrapper.WrapTextMinimiseWidth(m_text, m_wrapWidth, m_wrapWidthMin);
					else 
						text = m_textWrapper.WrapText(m_text, m_wrapWidth);
				}
				else
				{
					// Work out how to best keep the text on screen

					float finalWidth = m_wrapWidth;
					RectCentered bounds = new RectCentered( transform.position, transform.position );
					RectCentered cameraBounds = new RectCentered();

					if ( Application.isPlaying && PowerQuest.Exists )
					{
						// First, adjust wrap width so that it's on screen if possible
						Camera camera = PowerQuest.Get.GetCameraGui();
						bounds.Width = m_wrapWidth;
						if ( m_mesh.anchor == TextAnchor.LowerLeft || m_mesh.anchor == TextAnchor.MiddleLeft || m_mesh.anchor == TextAnchor.UpperLeft )
							bounds.CenterX = bounds.MaxX;
						else if ( m_mesh.anchor == TextAnchor.LowerRight || m_mesh.anchor == TextAnchor.MiddleRight || m_mesh.anchor == TextAnchor.UpperRight )
							bounds.CenterX = bounds.MaxX;

						cameraBounds = new RectCentered(camera.transform.position.x, camera.transform.position.y, camera.orthographicSize * 2.0f * camera.aspect, camera.orthographicSize * 2.0f);
						cameraBounds.RemovePadding(m_screenPadding);

						// If wrap width is adjustable, shrink it 
						if ( m_wrapWidthMin > 0 )
						{
							if ( bounds.MinX < cameraBounds.MinX )
								finalWidth -= cameraBounds.MinX -  bounds.MinX;
							if ( bounds.MaxX > cameraBounds.MaxX )
								finalWidth -= bounds.MaxX - cameraBounds.MaxX;
							finalWidth = Mathf.Max(m_wrapWidthMin+1, finalWidth);
						}
					}

					//
					// Now offset bounds so that they're on screen
					//
					if ( m_truncate )
						text = m_textWrapper.Truncate(m_text, finalWidth);
					else if ( m_wrapUniformLineWidth )
						text = m_textWrapper.WrapTextMinimiseWidth(m_text, finalWidth, m_wrapWidthMin);
					else 
						text = m_textWrapper.WrapText(m_text, finalWidth);
					
					m_mesh.text = text;

					if ( Application.isPlaying )
					{
						bounds = new RectCentered(m_textWrapper.Bounds);

						// Get the text height from the textwrapper
						Vector2 offset = Vector2.zero;
						if ( bounds.MinX < cameraBounds.MinX )
							offset.x += cameraBounds.MinX - bounds.MinX;
						if ( bounds.MaxX > cameraBounds.MaxX )
							offset.x += cameraBounds.MaxX - bounds.MaxX;
						if ( bounds.MinY < cameraBounds.MinY )
							offset.y += cameraBounds.MinY - bounds.MinY;
						if ( bounds.MaxY > cameraBounds.MaxY)
							offset.y += cameraBounds.MaxY - bounds.MaxY;

						if ( m_attachWorldPos != Vector2.zero && PowerQuest.Exists)
						{
							/* Disabled: This was stopping text from scrolling correctly when camera pans to talking character / 
							#if FONT_SNAPPING
							m_attachOffset = Utils.SnapRound(offset, PowerQuest.Get.SnapAmount);
							#else
							m_attachOffset = offset;
							#endif
							/**/
							LateUpdate();
						}
						else 
						{
							transform.Translate(offset.WithZ(0));				
						}
					}
					
					m_rectSize = bounds.Size;

				}
			}
			m_wrappedText = text;
			text = ProcessTypedText(text);
			UpdateOutline(text);
			m_mesh.text = text;
		}

	}

	void UpdateOutline(string text)
	{

		// Update outline
		if ( m_outline != null && m_outline.m_directions != 0 && gameObject.scene.IsValid() )
		{

			// Meshes may have been deleted, so remove them if they have
			for ( int i = m_outlineMeshes.Count - 1; i >= 0; i-- ) 
			{
				if ( m_outlineMeshes[i] == null )
					m_outlineMeshes.RemoveAt(i);
			}

			// loop through all potential outline angles
			int meshId = 0;
			for ( int i = 0; i < 8; ++i ) 
			{
				if ( PowerTools.Quest.Text.BitMask.IsSet(m_outline.m_directions, i)  )
				{
					if ( m_outlineMeshes == null )
						m_outlineMeshes = new List<TextMesh>();

					if ( meshId >= m_outlineMeshes.Count )
					{
						GameObject obj = new GameObject(((TextOutline.eDirection)(1<<i)).ToString(), m_mesh.GetType() ) as GameObject;
						obj.hideFlags = HideFlags.HideAndDontSave;
						obj.transform.SetParent(transform,false);
						
						obj.transform.localPosition = SHADOW_OFFSETS[i] * m_outline.m_width;
						obj.layer = gameObject.layer;

						MeshRenderer newShadowRenderer = obj.GetComponent<MeshRenderer>();
						newShadowRenderer.sharedMaterial = m_meshRenderer.sharedMaterial;//new Material(m_meshRenderer.material);
						newShadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						newShadowRenderer.receiveShadows = false;
						newShadowRenderer.sortingLayerID = m_meshRenderer.sortingLayerID;
						newShadowRenderer.sortingLayerName = m_meshRenderer.sortingLayerName;
						newShadowRenderer.sortingOrder = m_meshRenderer.sortingOrder;// - 1;

						TextMesh newShadowMesh = obj.GetComponent<TextMesh>();
						newShadowMesh.offsetZ = m_mesh.offsetZ + 0.1f;


						m_outlineMeshes.Add(newShadowMesh);
					}
					TextMesh shadowMesh = m_outlineMeshes[meshId];
					
					shadowMesh.text = text;
					shadowMesh.color = m_outline.m_color.WithAlpha(m_outline.m_color.a * color.a);

					// used to only do this first time, but sometimes you want to chnages any or all of these. hopefully not slow!
					shadowMesh.alignment = m_mesh.alignment;
					shadowMesh.anchor = m_mesh.anchor;
					shadowMesh.characterSize = m_mesh.characterSize;
					shadowMesh.font = m_mesh.font;
					shadowMesh.fontSize = m_mesh.fontSize;
					shadowMesh.fontStyle = m_mesh.fontStyle;
					shadowMesh.richText = m_mesh.richText;
					shadowMesh.lineSpacing = m_mesh.lineSpacing;
					shadowMesh.tabSize= m_mesh.tabSize;

					meshId++;
				}
			}
		}
	}

	public void AttachTo(Vector2 worldPosition)
	{	
		m_attachOffset = Vector2.zero;
		m_attachWorldPos = worldPosition;
		m_attachObject = null;	
		m_attachObjOffset = Vector2.zero;
		LateUpdate();
	}


	public void AttachTo(Transform obj, Vector2 worldPosition)
	{
		if (obj == null)
		{
			AttachTo(worldPosition);
			return;
		}
		m_attachOffset = Vector2.zero;	
		m_attachWorldPos = worldPosition;

		m_attachObject = obj;	
		m_attachObjOffset = worldPosition-(Vector2)obj.transform.position;
		LateUpdate();
	}
	
	void OnValidate()
	{
		// TODO: this is touched on unity save, can potentially cause slow downs
		if ( gameObject.activeInHierarchy )
		{
			m_editorRefresh = true;			
		}
	}
	
	void RefreshText() 
	{ 
		SetText(GetUnlocalizedText());	
	}

	// Message sent to quest text when editor has changed the text
	void EditorUpdate()
	{ 
		SetText(m_text);
	}

	void Update()
	{
		UpdateTyping();

		if ( m_editorRefresh && gameObject.activeInHierarchy && Application.isEditor && Application.isPlaying == false )
		{
			if ( m_outline != null && m_outlineMeshes != null )
			{
				// remove existing outline meshes
				for ( int i = 0; i < m_outlineMeshes.Count; ++i ) 
				{
					if ( m_outlineMeshes[i] != null )
						GameObject.DestroyImmediate( m_outlineMeshes[i].gameObject );
				}
				m_outlineMeshes.Clear();
			}
			EditorUpdate();
			m_editorRefresh = false;
		}
	}

	void LateUpdate()
	{
		//if ( Application.isPlaying == false )
		//	return;
		if ( CheckMaterial() == false )
			return;
		if ( m_attachObject != null || m_attachWorldPos != Vector2.zero )
		{
			if ( PowerQuest.Exists == false || PowerQuest.Get.GetCamera() == null || PowerQuest.Get.GetCameraGui() == null )
				return;
			QuestCamera questCam = PowerQuest.Get.GetCamera();
			Camera cam = questCam.GetInstance().GetComponent<Camera>();
			Camera guiCam = PowerQuest.Get.GetCameraGui();
			
			if ( m_attachObject != null )
				m_attachWorldPos = m_attachObjOffset + Utils.SnapRound((Vector2)m_attachObject.position, PowerQuest.Get.SnapAmount);

			// Need to add the difference between camera offset and actual position to account for the amount the camera has snapped.
			Vector2 camSnapOffset = ((Vector2)cam.transform.position - questCam.GetPosition());
			Vector2 guiSpacePosition = (Vector2)guiCam.ViewportToWorldPoint( cam.WorldToViewportPoint(m_attachWorldPos) ) + m_attachOffset + camSnapOffset;
			
			// Keep gametext on-screen
			guiSpacePosition = GetOnScreenPosition(guiSpacePosition);			

			#if FONT_SNAPPING

			if ( PowerQuest.Get.GetSnapToPixel() )
			{
				// finally we snap so that the text verts don't jiggle around
				// The snap amount is modified by the current camera zoom to account for resolution differences in the game camera and the gui camera (stops pixel text jiggling)
				float snapAmount = PowerQuest.Get.SnapAmount * Mathf.Max(questCam.GetZoom(),1);
				transform.position = Utils.SnapRound(guiSpacePosition, snapAmount )+new Vector2(0.001f,0.001f);
			
				// Offset by amount camera has moved so vertexes snap internally, but text still scrolls smoothly
				Vector2 offset = guiSpacePosition-(Vector2)transform.position;
				m_meshRenderer.material.SetVector("_Offset", offset);			
			}
			else 
			{
				transform.position = guiSpacePosition;
			}

			#else
			transform.position = guiSpacePosition;

			#endif
			
		}

		// Sync shadow renderer 'enabled' property with the main renderer
		if ( m_wasRendererEnabled != m_meshRenderer.enabled )
		{
			foreach( TextMesh item in m_outlineMeshes)
			{
				if ( item != null)
					item.GetComponent<Renderer>().enabled = m_meshRenderer.enabled;
			}
			m_wasRendererEnabled = m_meshRenderer.enabled;
		}
	}

	public void StartTyping(float speedOverride = -1)
	{
		m_typeChar = 0;
		if ( speedOverride > 0 )
			m_typeSpeed = speedOverride;
	}

	public bool GetTyping()
	{
		return m_typeSpeed > 0 && m_typeChar < m_wrappedText.Length;
	}

	string ProcessTypedText(string text)
	{
		if ( m_typeSpeed <= 0 || m_typeChar >= text.Length || Application.isPlaying == false )
			return text;
		return text.Insert(m_typeChar, STR_COLOR_START) + STR_COLOR_END;		
	}

	public void SkipTyping()
	{
		m_typeChar = int.MaxValue;
		string text = ProcessTypedText(m_wrappedText);
		UpdateOutline(text);
		m_mesh.text = text;
	}

	void UpdateTyping()
	{
		if ( m_typeSpeed <= 0 || m_typeChar >= m_wrappedText.Length )
			return;

		float speed = m_typeSpeed;		
		char currChar = m_wrappedText[m_typeChar];
		if (currChar == '.' || currChar == ',' || currChar == '?' || currChar == '!' )
			speed *= 2f;

		if ( Utils.GetTimeIncrementPassed(speed) && m_typeChar < m_wrappedText.Length )
		{	
			m_typeChar++;		
			while ( m_typeChar < m_wrappedText.Length && (currChar == ' ' || currChar == '\r' || currChar == '\n') )	
			{		
				m_typeChar++;	
				currChar = m_wrappedText[m_typeChar];
			}
			CallbackOnTypeCharacter?.Invoke();			
			
			string text = ProcessTypedText(m_wrappedText);
			UpdateOutline(text);
			m_mesh.text = text;
		}
	}
	
	// Calcs offset to keep text on screen, applied in LateUpdate
	Vector2 GetOnScreenPosition(Vector2 position)
	{
		if ( Application.isPlaying == false || m_rectSize.x <= 0 || PowerQuest.Exists == false)
			return position;

		Camera camera = PowerQuest.Get.GetCameraGui();

		RectCentered bounds = new RectCentered(position.x,  position.y, m_rectSize.x, m_rectSize.y );
		RectCentered cameraBounds = new RectCentered(camera.transform.position.x, camera.transform.position.y, camera.orthographicSize * 2.0f * camera.aspect, camera.orthographicSize * 2.0f);
		cameraBounds.RemovePadding(m_screenPadding);

		// Get the text height from the textwrapper
		Vector2 offset = Vector2.zero;
		if ( bounds.MinX < cameraBounds.MinX )
			offset.x += cameraBounds.MinX - bounds.MinX;
		if ( bounds.MaxX > cameraBounds.MaxX )
			offset.x += cameraBounds.MaxX - bounds.MaxX;
		if ( bounds.MinY < cameraBounds.MinY )
			offset.y += cameraBounds.MinY - bounds.MinY;
		if ( bounds.MaxY > cameraBounds.MaxY)
			offset.y += cameraBounds.MaxY - bounds.MaxY;

		return position+offset;
	}

	bool CheckTextMesh() 
	{
		if ( m_mesh == null )
			m_mesh = GetComponent<TextMesh>();		
		return m_mesh != null;		    
	}

	bool CheckMaterial()
	{		
		if ( m_materialSet && Application.isPlaying )
			return true;

		if ( m_mesh == null )
			m_mesh = GetComponent<TextMesh>();
		
		if ( m_meshRenderer == null )
			m_meshRenderer = GetComponent<MeshRenderer>();

		if ( m_mesh == null || m_meshRenderer == null )
			return false;
			
		bool snap = true;
		if ( Application.isPlaying && PowerQuest.Exists )
			snap = PowerQuest.Get.GetSnapToPixel();

		if ( s_shader == null )
			s_shader = Shader.Find(snap ? STR_SHADER_PIXEL : STR_SHADER);		
		
		if ( s_shader == null )
			return false;
		
		Material mat = Application.isPlaying ? m_meshRenderer.material : m_meshRenderer.sharedMaterial;
		
		if ( m_shaderOverride != null && mat != null && mat.shader != m_shaderOverride )
		{
			if ( mat.shader != m_shaderOverride )
			{
				mat = new Material(mat);
				mat.shader = m_shaderOverride;
				mat.mainTexture.filterMode = snap && m_setFiltering ? FilterMode.Point : FilterMode.Bilinear;
				mat.mainTexture. anisoLevel = snap && m_setFiltering ? 0 : 1;
				if ( Application.isPlaying )
					m_meshRenderer.material = mat;

			}
			if ( Application.isPlaying == false )
				m_materialSet = mat != null;
			return m_materialSet;
		}
		else 
		{
			
			#if SET_ALL_FONTS
				// Set all fonts to use the shader
				if ( m_mesh.font.material.shader != s_shader || Application.isPlaying == false )
				{					
					m_mesh.font.material.shader = s_shader;
					m_mesh.font.material.mainTexture.filterMode = snap && m_setFiltering ? FilterMode.Point : FilterMode.Bilinear;
					m_mesh.font.material.mainTexture.anisoLevel = snap && m_setFiltering ? 0 : 1;
				}
				if ( Application.isPlaying == false )
					m_materialSet = true;
			return true;
			#else			
				Material mat = m_meshRenderer.sharedMaterial;
				if ( mat.shader != s_shader )
				{
					mat = new Material(mat);
					mat.shader = s_shader;
					mat.mainTexture.filterMode = snap ? FilterMode.Point : FilterMode.Bilinear;
					mat.mainTexture. anisoLevel = snap ? 0 : 1;
					m_meshRenderer.sharedMaterial = mat;
					
				}			
				if ( Application.isPlaying == false )
					m_materialSet = mat != null;
				return m_materialSet;
			#endif
		}
	}

	private List<Material> m_tempMaterials = new List<Material>();
	static readonly Regex REGEX_TAG = new Regex(@"\[(\w*)\]");

	string ParseImages(string text)
	{
		if ( Application.isPlaying == false )
			return text;

		// Remove other materials first. Ideally this wouldn' tbe necessary, but I'd need to put more work in to get it working without clearing materials first
		MeshRenderer renderer = GetComponent<MeshRenderer>();
		renderer.GetMaterials(m_tempMaterials);
		if ( m_tempMaterials.Count > 1 )
		{
			for ( int i = 1; i < renderer.materials.Length; ++i )
			{
				Destroy(renderer.materials[i]);
			}
			renderer.materials = new Material[] { renderer.materials[0] };
		}

		// Use regex to find cases of [Whatever] so we can add the image
		// TODO(arcnor): This (just the call to Replace with an evaluator) causes 112 bytes per frame of garbage, even when there are no matches
		return REGEX_TAG.Replace(text, EvaluateImageTagMatch);
	}

	static readonly string TAG_QUAD = @"<quad material={0} size={1} x={2} y={3} width={4} height={5} />";

	private static TextSpriteData FindByTag(TextSpriteData[] textSpriteData, string tag) 
	{
		TextSpriteData bestMatch = null;
		foreach (TextSpriteData spriteData in textSpriteData) 
		{
			if (string.Equals(spriteData.m_tag, tag,System.StringComparison.OrdinalIgnoreCase)) 
			{
				if ( spriteData.m_platform.EqualsIgnoreCase(s_textSpritePlatform) )
					return spriteData; // found perfect match, we're done.
				else if ( IsString.Empty(spriteData.m_platform) )
					bestMatch = spriteData; // found default, use as fallback
			}
		}
		return bestMatch;
	}

	string EvaluateImageTagMatch( Match match ) 
	{
		string result = string.Empty;
		if ( match.Groups == null || match.Groups.Count < 2 || PowerQuest.Exists== false)
			return result;

		string tag = match.Groups[1].Value;
		TextSpriteData data = null;
		/*  Append platform tag - Now done in PowerQuest /
		tag += "PS";
		TextSpriteData data = FindByTag(PowerQuest.Get.m_textSprites, tag);
		// if not found with platform tag, try without
		if ( data == null )
		/**/
		{
			tag = match.Groups[1].Value;
			data = FindByTag(PowerQuest.Get.TextSprites, tag);
		}

		if ( data == null || data.m_sprite == null )
			return result;
		Sprite sprite = data.m_sprite;

		Material material = null;
		// Check if material exists in guitext already

		MeshRenderer renderer = GetComponent<MeshRenderer>();
		int matIndex = -1;

		for (int index = 0; index < renderer.materials.Length; index++) 
		{
			var m = renderer.materials[index];

			if (m.mainTexture == sprite.texture) 
			{
				matIndex = index;
				break;
			}
		}

		if ( matIndex < 0 )
		{
			matIndex = renderer.materials.Length;
			Material[] materials = new Material[matIndex+1];
			System.Array.Copy(renderer.materials, materials, matIndex);
			material = Instantiate(PowerQuest.Get.TextSpriteMaterial);
			material.mainTexture = sprite.texture;
			materials[matIndex] = material;
			renderer.materials = materials;
		}
		material = renderer.materials[matIndex];
		material.SetVector("_Offset",new Vector2(0,data.m_offsetY));
		result = string.Format(TAG_QUAD, matIndex, sprite.textureRect.height, sprite.textureRect.x/sprite.texture.width, sprite.textureRect.y/sprite.texture.height, sprite.textureRect.width/sprite.texture.width, sprite.textureRect.height/sprite.texture.height);
		//result = string.Format(TAG_QUAD, matIndex, m_mesh.fontSize*2, sprite.textureRect.x/sprite.texture.width, sprite.textureRect.y/sprite.texture.height, sprite.textureRect.width/sprite.texture.width, sprite.textureRect.height/sprite.texture.height);

		return result;

	}
		

}
}

namespace PowerTools.Quest.Text
{

	public struct BitMask
	{	
		public BitMask( int mask )
		{
			m_value = mask;
		}


		public BitMask( params int[] bitsSet )
		{
			m_value = 0;
			for( int i = 0; i< bitsSet.Length; ++i )
			{
				m_value |= 1 <<  bitsSet[i];
			}
		}

		public static implicit operator int(BitMask m) 
		{
			return m.m_value;	
		}

		public int Value { get{ return m_value; } set { m_value = value; } }
		public void SetAt(int index) { m_value |= 1 << index; }
		public void SetAt<T>(T index) { m_value |= 1 << (int)(object)index; }
		public void UnsetAt(int index) { m_value &= ~(1 << index); }
		public void UnsetAt<T>(T index) { m_value &= ~(1 << (int)(object)index); }
		public bool IsSet(int index) { return (m_value & 1 << index) != 0; }
		public bool IsSet<T>(T index) { return (m_value & 1 << (int)(object)index) != 0; }
		public void Clear() { m_value = 0; }

		// And some static functions if you don't wanna construt the bitmask  and just wanna pass in/out an int
		public static int SetAt(int mask, int index) { return mask | 1 << index; }
		public static int UnsetAt(int mask, int index)  { return mask & ~(1 << index); }
		public static bool IsSet(int mask, int index) { return (mask & 1 << index) != 0; }


		public static uint GetNumberOfSetBits(uint i)
		{
			// From http://stackoverflow.com/questions/109023/how-to-count-the-number-of-set-bits-in-a-32-bit-integer
			i = i - ((i >> 1) & 0x55555555);
			i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
			return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
		}

		int m_value;


	}


	public class BitMaskAttribute : PropertyAttribute
	{
		public System.Type propType;
		public BitMaskAttribute(System.Type aType)
		{
			propType = aType;
		}
	}

}
