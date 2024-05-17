// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Sprites/PowerSpriteAA"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_Tint ("Fade", Color) = (1,1,1,0)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		_Offset ("Offset", Vector) = (0,0,0,0)
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0		
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex VertTinted
			#pragma fragment FragTinted
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile _ PIXELSNAP_ON
			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#include "UnitySprites.cginc"

			fixed4 _Tint;
			float4 _Offset;
			uniform float4 _MainTex_TexelSize;


			// From TylerGlaiel
			float4 texture2DAA( float2 uv)
			{				
				float2 texsize = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 uv_texspace = uv * texsize;
				float2 seam = floor(uv_texspace + .5);
				uv_texspace = (uv_texspace - seam) / fwidth(uv_texspace) + seam;
				uv_texspace = clamp(uv_texspace, seam - .5, seam + .5);
				//return tex2D(tex, uv_texspace / texsize);
				return SampleSpriteTexture(uv_texspace / texsize);
			}

			v2f VertTinted (appdata_t IN) 
			{
				IN.vertex += _Offset;
				return SpriteVert(IN);
			}

			fixed4 FragTinted(v2f IN) : SV_Target
			{
				//fixed4 c = SampleSpriteTexture (IN.texcoord);
				fixed4 c = texture2DAA(IN.texcoord);
				c.rgb = lerp(_Tint.rgb, c.rgb* IN.color.rgb, 1.0-_Tint.a);
				c.a *= IN.color.a;
				c.rgb *=  c.a;
				return c;
			}


		ENDCG
		}
	}
}
