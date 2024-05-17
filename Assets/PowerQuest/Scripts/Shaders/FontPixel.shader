// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Powerhoof/Pixel Text Shader" {
	Properties {
		_MainTex ("Font Texture", 2D) = "white" {}
		_Color ("Text Color", Color) = (1,1,1,1)
		_Offset ("Offset", Vector) = (0,0,0,0)
	}

	SubShader {

		Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
		}
		Lighting Off Cull Off ZTest Always ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Color;
			uniform float4 _Offset;

			/*
			//uniform float4 _MainTex_TexelSize;			

			// From TylerGlaiel
			float4 texture2DAA(sampler2D tex, float2 uv)
			{
				float2 texsize = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);	
				float2 uv_texspace = uv * texsize;
				float2 seam = floor(uv_texspace + .5);
				uv_texspace = (uv_texspace - seam) / fwidth(uv_texspace) + seam;
				uv_texspace = clamp(uv_texspace, seam - .5, seam + .5);
				return tex2D(tex, uv_texspace / texsize);
			}
			*/

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(unity_ObjectToWorld, v.vertex);
				o.vertex.x = floor(o.vertex.x+0.0001);
				o.vertex.y = floor(o.vertex.y+0.0001);				
				o.vertex = mul(UNITY_MATRIX_VP, o.vertex + _Offset);
				o.color = v.color * _Color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{ 
				
				fixed4 col = i.color;
				//col.a = texture2DAA(_MainTex, i.texcoord).a;
				if ( tex2D(_MainTex, i.texcoord).a < 0.48f )
					discard;				
				return col;
			}
			ENDCG
		}
	}
}
