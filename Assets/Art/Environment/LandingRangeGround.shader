Shader "Betelgeuse/LandingRangeGround"
{
    Properties
    {
        [MainTexture] _BaseMap ("Tiled Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _BumpMap ("Tiled Normal", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 0.55
        _MacroMap ("Macro Tint", 2D) = "white" {}
        _MacroStrength ("Macro Strength", Range(0, 1)) = 0.45
        _MacroBright ("Macro Brightness", Range(0.4, 2.5)) = 1.35
        _TileMeters ("Tile Size (m)", Float) = 16
        _Smoothness ("Smoothness", Range(0, 1)) = 0.16
        _TerrainRadius ("Terrain Radius (m)", Float) = 2000
        _RimFadeWidth ("Rim Fade Width (m)", Float) = 80
        _RimFogColor ("Rim Fog Color", Color) = (0.48, 0.56, 0.60, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 200
        Cull Back
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MacroMap);    SAMPLER(sampler_MacroMap);
            // Force tiling even if a bound texture/import sampler is Clamp (avoids square patch on disk).
            SAMPLER(sampler_LinearRepeat);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _MacroStrength;
                half _MacroBright;
                float _TileMeters;
                half _Smoothness;
                float _TerrainRadius;
                float _RimFadeWidth;
                half4 _RimFogColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half3 ApplyMacro(half3 grass, half3 macro)
            {
                macro *= _MacroBright;
                half grassLum = max(Luminance(grass), 0.04h);
                half macroLum = max(Luminance(macro), 0.04h);
                half3 grassChroma = grass / grassLum;
                half3 macroChroma = macro / macroLum;
                half3 chroma = lerp(grassChroma, macroChroma, _MacroStrength * 0.55h);
                half lum = grassLum * lerp(1.0h, saturate(macroLum / 0.38h), _MacroStrength * 0.5h);
                return chroma * lum;
            }

            // Soft rim coverage: 1 in interior, 0 past outer radius. Quintic ease.
            half RimAlpha(float3 positionWS)
            {
                float radius = max(_TerrainRadius, 1.0);
                float fade = max(_RimFadeWidth, 8.0);
                float dist = length(positionWS.xz);
                float t = saturate((radius - dist) / fade);
                // quintic smoothstep
                return t * t * t * (t * (t * 6.0h - 15.0h) + 10.0h);
            }

            // World XZ tiling that always wraps (frac), independent of texture wrapMode.
            float2 WorldTileUV(float3 positionWS)
            {
                float tile = max(_TileMeters, 0.5);
                return frac(positionWS.xz / tile);
            }

            // Macro bake covers world AABB [-R,R]^2 in UV [0,1]^2 — same mapping as mesh UV at origin.
            float2 WorldMacroUV(float3 positionWS)
            {
                float r = max(_TerrainRadius, 1.0);
                return positionWS.xz / (r * 2.0) + 0.5;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half rim = RimAlpha(input.positionWS);
                clip(rim - 0.004h);

                float2 uvTile = WorldTileUV(input.positionWS);
                float2 uvMacro = WorldMacroUV(input.positionWS);

                half3 grass = SAMPLE_TEXTURE2D(_BaseMap, sampler_LinearRepeat, uvTile).rgb;
                half3 macro = SAMPLE_TEXTURE2D(_MacroMap, sampler_MacroMap, uvMacro).rgb;
                half3 albedo = ApplyMacro(grass, macro) * _BaseColor.rgb;

                half3 nMesh = normalize(input.normalWS);
                half3 bumpTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_LinearRepeat, uvTile), _BumpScale);
                // Terrain is mostly +Y: tangent ~ +X, bitangent ~ +Z.
                half3 bumpWS = half3(bumpTS.x, bumpTS.z, bumpTS.y);
                half3 n = normalize(nMesh + bumpWS * half3(1.0h, 0.35h, 1.0h));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndl = saturate(dot(n, mainLight.direction));
                half3 lighting = mainLight.color * (ndl * mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                half3 ambient = SampleSH(n);
                half3 color = albedo * (ambient + lighting);

                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 halfDir = SafeNormalize(mainLight.direction + viewDir);
                half spec = pow(saturate(dot(n, halfDir)), 48.0h) * _Smoothness * mainLight.shadowAttenuation;
                color += spec * mainLight.color * albedo * 0.22h;

                color = MixFog(color, input.fogFactor);

                // Keep grass chroma through the soft alpha dissolve — do not paint fog-gray onto the rim band.
                // (_RimFogColor kept for material API / skirt matching elsewhere.)
                return half4(color, rim);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.5
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _MacroStrength;
                half _MacroBright;
                float _TileMeters;
                half _Smoothness;
                float _TerrainRadius;
                float _RimFadeWidth;
                half4 _RimFogColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                float radius = max(_TerrainRadius, 1.0);
                float fade = max(_RimFadeWidth, 8.0);
                float dist = length(input.positionWS.xz);
                float t = saturate((radius - dist) / fade);
                half rim = t * t * t * (t * (t * 6.0h - 15.0h) + 10.0h);
                clip(rim - 0.35h);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma target 3.5
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _MacroStrength;
                half _MacroBright;
                float _TileMeters;
                half _Smoothness;
                float _TerrainRadius;
                float _RimFadeWidth;
                half4 _RimFogColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                float radius = max(_TerrainRadius, 1.0);
                float fade = max(_RimFadeWidth, 8.0);
                float dist = length(input.positionWS.xz);
                float t = saturate((radius - dist) / fade);
                half rim = t * t * t * (t * (t * 6.0h - 15.0h) + 10.0h);
                clip(rim - 0.35h);
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
    FallBack Off
}