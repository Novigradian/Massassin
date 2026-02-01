Shader "Custom/GradientSkybox"{
	Properties{
		_Color1 ("Color 1", Color) = (0.8, 0.8, 0.8, 1)
		_Color2 ("Color 2", Color) = (0.2, 0.2, 0.8, 1)
		[Exponential]_PowN ("Power", Range(0, 5)) = 1
	}
	SubShader{
		Tags{
			"Queue" = "Background" "RenderType" = "Opaque"
		}
		Pass{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata_t{
				float4 vertex : POSITION;
			};

			struct v2f{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
			};

			float _PowN;
			float4 _Color1, _Color2;

			v2f vert(appdata_t v){
				v2f o;
				o.position = TransformObjectToHClip(v.vertex);
				o.uv = abs(v.vertex);
				o.worldPos = TransformObjectToWorld(v.vertex);
				o.screenPos = ComputeScreenPos(o.position);
				return o;
			}

			float3 frag(v2f i) : SV_Target{
				float2 screenUV = i.screenPos.xy / i.screenPos.w;
				float3 col = lerp(_Color1.rgb, _Color2.rgb, pow(screenUV.y, _PowN));
				return col;
				return float3(screenUV,0);
			}
			ENDHLSL
		}
	}
	FallBack "Diffuse"
}