// Transparent window glass for URP. It has no ShadowCaster pass, so sunlight goes through the panes
// and only the steel frames cast shadows (the grid pattern on the floor).
Shader "Portfolio/ToonGlass"
{
    Properties
    {
        _BaseColor ("Tint (alpha = opacity)", Color) = (0.74, 0.86, 0.93, 0.22)
        _StreakColor ("Reflection Streak", Color) = (1, 1, 1, 0.35)
        _StreakScale ("Streak Scale", Float) = 0.35
        _StreakWidth ("Streak Width", Range(0, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Glass"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _StreakColor;
                float _StreakScale;
                half _StreakWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Diagonal highlight bands, the classic cartoon "shine" on glass.
                float d = frac((i.positionWS.x + i.positionWS.y + i.positionWS.z) * _StreakScale);
                half streak = step(d, _StreakWidth) + step(abs(d - _StreakWidth * 2.2), _StreakWidth * 0.3);
                half4 col = _BaseColor;
                col.rgb = lerp(col.rgb, _StreakColor.rgb, saturate(streak) * _StreakColor.a);
                col.a = saturate(col.a + saturate(streak) * _StreakColor.a * 0.5);
                return col;
            }
            ENDHLSL
        }
    }
}
