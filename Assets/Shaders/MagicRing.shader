Shader "Custom/MagicRing"
{
    Properties
    {
        [HDR]_MainColor ("Main Color", Color) = (1,1,1,1)
        _Power ("Power", Range(0.1, 10)) = 1.0
        _Scale ("Scale", Vector) = (1.0, 1.0, 0.0, 0.0)
        _Speed ("Speed", Vector) = (0.5, 0.5, 0.0, 0.0)
    }
    SubShader
    {
        // No culling or depth
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        Cull Off ZWrite Off ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CommonShaderMethods.hlsl"

            float _Power;
            float2 _Scale, _Speed;
            float4 _MainColor;
        
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = (v.vertex + 1)/2;
                return o;
            }

            sampler2D _MainTex;

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float noise = saturate(GradientNoise(i.uv * _Scale + _Speed * _Time.x, 1));
                float v = noise * pow(1-uv.y, _Power);
                return _MainColor * saturate(v);
            }
            ENDHLSL
        }
    }
}