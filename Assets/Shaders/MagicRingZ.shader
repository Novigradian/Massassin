Shader "Custom/MagicRingZ"
{
    Properties
    {
        [HDR]_MainColor ("Main Color", Color) = (1,1,1,1)
        _Power ("Power", Range(0.1, 10)) = 1.0
        _Scale ("Scale", Vector) = (1.0, 1.0, 0.0, 0.0)
        _Speed ("Speed", Vector) = (0.5, 0.5, 0.0, 0.0)
        _UpAxis ("Up Axis (0=X,1=Y,2=Z)", Range(0,2)) = 1
        _ForwardAxis ("Forward Axis (0=X,1=Y,2=Z)", Range(0,2)) = 0
        _AngleOffset ("Angle Offset (deg)", Range(0,360)) = 0
        _SoftParticleFade ("Soft Particle Fade Distance", Range(0.01, 10)) = 1.0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float _Power;
            float3 _Scale;
            float4 _Speed, _MainColor;
            float _UpAxis;
            float _ForwardAxis;
            float _AngleOffset;

            struct appdata
            float _SoftParticleFade;
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 posOS : TEXCOORD0; // object-space position
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
                float4 screenPos : TEXCOORD1; // screen position for depth comparison
                v2f o;
                // Keep built-in transform for clip position
                o.vertex = TransformObjectToHClip(v.vertex);
                // Pass full object-space position to fragment shader
                o.posOS = v.vertex.xyz;
                return o;
            }

            sampler2D _MainTex;
                // Compute screen position for depth comparison
                o.screenPos = ComputeScreenPos(o.vertex);
            float NoiseFBM(float2 p)
            {
                return GradientNoise(p * 1.0, 1);
            }

            // Helper: get component by index (0->x, 1->y, 2->z)
            float GetCompByIndex(float3 v, float idx)
            {
                // idx is expected to be 0,1,2 (but may be float); use ranges for safety
                if (idx < 0.5) return v.x;
                else if (idx < 1.5) return v.y;
                return v.z;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Use object-space position but allow user to pick which axis is 'up'
                float3 pos = i.posOS;

                // Determine integer axis indices (0,1,2) from properties
                float upIdxF = round(_UpAxis);
                float forwardIdxF = round(_ForwardAxis);
                // Soft Particle Depth Fade
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float particleDepth = LinearEyeDepth(i.screenPos.z / i.screenPos.w, _ZBufferParams);
                float depthDiff = sceneDepth - particleDepth;
                float depthFade = saturate(depthDiff / _SoftParticleFade);
                
                // Compute the remaining radial axis index (0+1+2 = 3)
                float otherIdxF = 3.0 - upIdxF - forwardIdxF;

                // Read components according to chosen axes
                float up = GetCompByIndex(pos, upIdxF);
                float a = GetCompByIndex(pos, forwardIdxF);
                float b = GetCompByIndex(pos, otherIdxF);

                // Angle: use forward axis as zero-angle reference, add optional offset in degrees
                float angle = atan2(b, a) + radians(_AngleOffset);
                float angle01 = angle / (2.0 * 3.14159265) + 0.5;

                // Radius in the radial plane
                float radius = length(float2(a, b));
                float height = up;

                // Build a stable noise input that breaks symmetry: angle & height + small radial jitter
                float radialJitter = radius * 0.3;

                float2 noiseInput;
                noiseInput.x = angle01 * _Scale.x + _Speed.x * _Time.x + radialJitter;
                noiseInput.y = height * _Scale.y + _Speed.y * _Time.x;

                // Add a small secondary offset dependent on angle to further break symmetry
                float secondary = NoiseFBM(noiseInput * 1.3 + float2(12.34, 45.67));
                noiseInput += (secondary - 0.5) * 0.25;

                float noise = saturate(NoiseFBM(noiseInput));

                // Shape the falloff along the cone's height and radially (so base is brighter)
                float h = saturate(1 - height * _Scale.z + 0.5);
                float radialFalloff = saturate(1.0 - radius * 1.5);

                float v = noise * pow(h, _Power) * radialFalloff;

                // Color + alpha
                float4 col = _MainColor * saturate(v);
                col.a = saturate(v);
                return col;
            }
            ENDHLSL
        }
    }
}