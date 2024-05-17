// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Unlit/TransparentAntiAliased" 
{
	Properties
	{
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata_t 
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			uniform float4 _MainTex_TexelSize;

			// From TylerGlaiel
			float4 texture2DAA(sampler2D tex, float2 uv)
			{
				float2 texsize = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 uv_texspace = uv * texsize;
				float2 seam = floor(uv_texspace + .5);
				uv_texspace = (uv_texspace - seam) / fwidth(uv_texspace) + seam;
				uv_texspace = clamp(uv_texspace, seam - .5, seam + .5);

				float4 result = tex2D(tex, uv_texspace / texsize);
				
				// Since tranparent parts will have a color of "black" multiply any color with inverse of the color to get that back
				float ratio = result.a;
				result /= ratio;
				result.a = ratio;
				return result;
			}

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				//fixed4 col = tex2D(_MainTex, i.texcoord);
				fixed4 col = texture2DAA(_MainTex, i.texcoord);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}

}
