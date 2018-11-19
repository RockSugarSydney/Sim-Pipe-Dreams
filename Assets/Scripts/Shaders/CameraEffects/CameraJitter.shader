Shader "CameraEffect/CameraJitter" 
{
	Properties 
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			fixed4 frag(v2f i) : SV_TARGET
			{
				float noise = frac(sin(dot(float2(5.123, 1.659), float2(8.246, 4.310)) * _Time.y));
				float rand = frac(sin(dot(float2(1.123, 9.12) ,float2(12.9898,78.233))) * 43758.5453 * _Time.y);
				float jitter = 1 - step(rand, 0.5);
				float offset = (noise - 0.5) * 2.0 * jitter;
				fixed4 col;

				col.r = tex2D(_MainTex, i.uv + offset * 0.05).r;
				col.g = tex2D(_MainTex, i.uv + float2(offset, 0.0) * 0.02).g;
				col.b = tex2D(_MainTex, i.uv + float2(0.0, -offset) * 0.03).b;
				col.a = 1;
				return col;
			}
			ENDCG
		}
	}
}
