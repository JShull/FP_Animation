Shader "FuzzPhyte/FP_SKyboxCubemapBlend_URP"
{
    Properties
    {
        _CubeA ("Cubemap A", CUBE) = "" {}
        _CubeB ("Cubemap B", CUBE) = "" {}
        _Blend ("Blend", Range(0,1)) = 0

        _Tint ("Tint", Color) = (1,1,1,1)
        _Exposure ("Exposure", Range(0,8)) = 1
        _RotationY ("Rotation Y (Degrees)", Range(0,360)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "PreviewType"="Skybox"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Skybox"
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textures + samplers
            TEXTURECUBE(_CubeA);
            SAMPLER(sampler_CubeA);

            TEXTURECUBE(_CubeB);
            SAMPLER(sampler_CubeB);

            // SRP Batcher-friendly per-material constants
            CBUFFER_START(UnityPerMaterial)
                float _Blend;
                float4 _Tint;
                float _Exposure;
                float _RotationY;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirOS      : TEXCOORD0;
            };

            float3 RotateYDegrees(float3 v, float degrees)
            {
                float rad = radians(degrees);
                float s, c;
                sincos(rad, s, c);

                float3 r;
                r.x =  v.x * c + v.z * s;
                r.y =  v.y;
                r.z = -v.x * s + v.z * c;
                return r;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);

                float3 dir = IN.positionOS;
                dir = RotateYDegrees(dir, _RotationY);

                OUT.dirOS = dir;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.dirOS);

                half4 a = SAMPLE_TEXTURECUBE(_CubeA, sampler_CubeA, dir);
                half4 b = SAMPLE_TEXTURECUBE(_CubeB, sampler_CubeB, dir);

                half t = saturate(_Blend);
                half4 col = lerp(a, b, t);

                col.rgb *= (half3)_Tint.rgb;
                col.rgb *= (half)_Exposure;
                col.a = 1;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
