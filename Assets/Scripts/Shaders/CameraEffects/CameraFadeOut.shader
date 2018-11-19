Shader "CameraEffect/CameraFadeOut"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TargetTex ("Target Texture", 2D) = "white" {}
		_NewAlpha ("New Alpha", Range (0, 1)) = 0
	}

	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off Fog { Mode Off }

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

			sampler2D _MainTex;
			sampler2D _TargetTex;
			float _NewAlpha;

			fixed4 frag(v2f i) : SV_TARGET
			{
				fixed4 base, col, result;

				base = tex2D(_TargetTex, i.uv) * i.color;
				base.a = 1 - _NewAlpha;
				col = tex2D(_MainTex, i.uv) * i.color;
				result = fixed4((base.rgb * base.a) + (col.rgb * _NewAlpha), 1.0);
				return result;
			}
			ENDCG
		}
	}
}
