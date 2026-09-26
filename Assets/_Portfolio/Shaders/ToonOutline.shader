// Low-poly toon shading with an inverted-hull ink outline, for URP.
// Pass 1 (UniversalForward): flat, banded lighting from the main light + main-light shadows.
// Pass 2 (SRPDefaultUnlit): back faces pushed out along normals, drawn in the outline color.
Shader "Portfolio/ToonOutline"
{
    Properties
    {
        _BaseMap ("Base Map (texture / color atlas)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Color", Color) = (0.55, 0.5, 0.65, 1)
        _Bands ("Light Bands", Range(1, 5)) = 2
        _OutlineColor ("Outline Color", Color) = (0.08, 0.07, 0.1, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.06
        [Enum(Normals,0,FromPivot,1,SmoothedNormals,2)] _OutlineSource ("Outline Direction", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _Bands;
            half4 _OutlineColor;
            half _OutlineWidth;
            half _OutlineSource;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(normalize(i.normalWS), light.direction));
                half lit = ndl * light.shadowAttenuation;
                half bands = max(_Bands, 1);
                lit = saturate(floor(lit * bands + 0.5) / bands);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half3 col = lerp(_ShadeColor.rgb * albedo, albedo * light.color, lit);
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float3 smoothNormalOS : TEXCOORD3; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                // Hard-edged low-poly meshes have split normals, which tear the hull at corners.
                //  0: raw normals (smooth meshes)
                //  1: away from the pivot (convex primitives)
                //  2: smoothed normals baked into UV3 by WorkshopModelPostprocessor (imported art)
                float3 dir = TransformObjectToWorldNormal(v.normalOS);
                if (_OutlineSource > 1.5)
                {
                    dir = TransformObjectToWorldNormal(v.smoothNormalOS);
                }
                else if (_OutlineSource > 0.5)
                {
                    float3 fromCenter = posWS - TransformObjectToWorld(float3(0, 0, 0));
                    dir = dot(fromCenter, fromCenter) > 1e-8 ? fromCenter : float3(0, 1, 0);
                }
                float3 nWS = normalize(dir);
                o.positionCS = TransformWorldToHClip(posWS + nWS * _OutlineWidth);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
