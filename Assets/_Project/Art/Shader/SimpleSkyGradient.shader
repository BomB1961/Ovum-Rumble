Shader "Custom/SimpleSkyGradient"
{
    Properties
    {
        _TopColor ("위쪽 색 (하늘)", Color) = (0.0, 0.46, 1.0, 1)
        _BottomColor ("아래쪽 색 (수평선)", Color) = (1.0, 0.56, 0.0, 1)
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
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;

                // 1. 3D 좌표를 화면 좌표로 변환
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);

                // 2. 월드 좌표(월드 공간 위치)를 저장해서 넘김
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 월드 위치를 정규화(방향 벡터로 만듦)
                float3 worldDir = normalize(i.positionWS);

                // Y축(위/아래)을 0~1로 변환 (-1 아래 → +1 위)
                float t = saturate(worldDir.y * 0.5 + 0.5);

                // 아래쪽 색과 위쪽 색을 섞음
                // (t=0이면 아래색, t=1이면 위쪽색)
                return lerp(_BottomColor, _TopColor, t);
            }

            ENDHLSL
        }
    }
}