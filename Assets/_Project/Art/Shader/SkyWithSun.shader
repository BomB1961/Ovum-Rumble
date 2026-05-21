Shader "Custom/SkyWithSun"
{
    Properties
    {
        _TopColor ("위쪽 색 (하늘)", Color) = (0.0, 0.46, 1.0, 1)
        _BottomColor ("아래쪽 색 (수평선)", Color) = (1.0, 0.56, 0.0, 1)
        _SunDirection ("태양 방향", Vector) = (-0.26, 0.12, -0.96, 0)
        _SunColor ("태양 색", Color) = (1, 0.9, 0.2, 1)
        _SunSize ("태양 크기", Range(0, 0.5)) = 0.05
        _SunGlow ("태양 테두리 퍼짐", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            // ⭐⭐⭐ 반드시 여기에 넣으세요! HLSLPROGRAM 위, Pass { 바로 아래!
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;
                float4 _SunDirection;
                float4 _SunColor;
                float _SunSize;
                float _SunGlow;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 worldDir = normalize(i.positionWS);

                // 하늘 그라데이션
                float t = saturate(worldDir.y * 0.5 + 0.5);
                float3 skyColor = lerp(_BottomColor, _TopColor, t);

                // 태양
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDist = distance(worldDir, sunDir);
                float sun = 1.0 - smoothstep(_SunSize, _SunSize + _SunGlow, sunDist);

                skyColor += _SunColor.rgb * sun;

                return float4(skyColor, 1);
            }

            ENDHLSL
        }
    }
}