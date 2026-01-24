Shader "FuzzPhyte/FP_SkyboxCubemapBlend"
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
        }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            samplerCUBE _CubeA;
            samplerCUBE _CubeB;
            float _Blend;
            fixed4 _Tint;
            float _Exposure;
            float _RotationY;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            float3 RotateYDegrees(float3 v, float degrees)
            {
                float rad = degrees * 0.01745329252; // PI/180
                float s = sin(rad);
                float c = cos(rad);

                float3 r;
                r.x =  v.x * c + v.z * s;
                r.y =  v.y;
                r.z = -v.x * s + v.z * c;
                return r;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float3 dir = v.vertex.xyz;
                dir = RotateYDegrees(dir, _RotationY);

                o.dir = dir;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);

                fixed4 a = texCUBE(_CubeA, dir);
                fixed4 b = texCUBE(_CubeB, dir);

                fixed4 col = lerp(a, b, saturate(_Blend));
                col.rgb *= _Tint.rgb * _Exposure;
                col.a = 1;

                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
