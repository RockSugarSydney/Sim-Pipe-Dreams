// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DefaultVideoPlayback"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
	}

	SubShader 
	{
		Tags 
		{
			"RenderType" = "Opaque" 
			"Queue" = "Transparent"
		}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			uniform sampler2D _VideoPlaybackTex;
			uniform int _VideoIsPlaying;
			sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_TARGET
			{
				fixed4 col;

				col = tex2D(_MainTex, i.uv) * i.color * (1 - _VideoIsPlaying);
				col += tex2D(_VideoPlaybackTex, i.uv) * _VideoIsPlaying;
				return col;
			}
			ENDCG
		}
	}
}
