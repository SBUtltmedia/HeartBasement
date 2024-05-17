// Adapted from http://www.shaderslab.com/demo-15---sprite-outline.html
// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Sprites/PowerSpriteOutline"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[PerRendererData] _Tint ("Fade", Color) = (1,1,1,0)
		[PerRendererData] _Outline ("Outline", Color) = (1,1,1,0)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _Offset ("Offset", Vector) = (0,0,0,0)
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

			//fixed4 _Color;
			fixed4 _Tint;
			float4 _Offset; 
			float4 _Outline;
			float4 _MainTex_TexelSize;

			v2f VertTinted (appdata_t IN) 
			{
				// Apply offset
				IN.vertex += _Offset;
				return SpriteVert(IN);
			}

			fixed4 FragTinted(v2f IN) : SV_Target
			{
				fixed4 c = SampleSpriteTexture (IN.texcoord);
				// Apply tint
				c.rgb = lerp(_Tint.rgb, c.rgb* IN.color.rgb, 1.0-_Tint.a);
				c.a *= IN.color.a;
				c.rgb *=  c.a;

				// Apply outline. The most expensive bit. Should probably use different shader if outline not enabled
				half4 outlineC = _Outline;
               	outlineC.rgb *= outlineC.a;

               	fixed outlineAlpha = SampleSpriteTexture( IN.texcoord + fixed2(0, _MainTex_TexelSize.y)).a;
                outlineAlpha += SampleSpriteTexture( IN.texcoord - fixed2(0, _MainTex_TexelSize.y)).a;
                outlineAlpha += SampleSpriteTexture( IN.texcoord + fixed2(_MainTex_TexelSize.x, 0)).a;
                outlineAlpha += SampleSpriteTexture( IN.texcoord - fixed2(_MainTex_TexelSize.x, 0)).a;

                // Is the below faster? Should check what that sample sprite texture thing actually does
                //fixed outlineAlpha = tex2D(_MainTex,  IN.texcoord + fixed2(0, _MainTex_TexelSize.y)).a;
                //outlineAlpha += tex2D(_MainTex,  IN.texcoord - fixed2(0, _MainTex_TexelSize.y)).a;
                //outlineAlpha += tex2D(_MainTex,  IN.texcoord + fixed2(_MainTex_TexelSize.x, 0)).a;
                //outlineAlpha += tex2D(_MainTex,  IN.texcoord - fixed2(_MainTex_TexelSize.x, 0)).a;

                outlineC.a *= clamp( ceil(outlineAlpha),0,IN.color.a); 
               	outlineC.rgb *= outlineC.a;
                return lerp(outlineC, c, ceil(c.a));
			}


		ENDCG
		}
	}
}
