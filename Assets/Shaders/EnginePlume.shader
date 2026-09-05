Shader "Betelgeuse/EnginePlume"
{
    Properties
    {
        _MainTex ("Ramp", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 16)) = 3
        _Flicker ("Flicker", Range(0, 2)) = 1
        _NoiseAmt ("Noise", Range(0, 1)) = 0.45
        _Scroll ("Scroll", Float) = 0
        _Softness ("Softness", Range(0.2, 3)) = 1.35
        _EdgePower ("Edge Softness", Range(0.4, 4)) = 1.8
        _TipSmoke ("Tip Smoke", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _Intensity;
            float _Flicker;
            float _NoiseAmt;
            float _Scroll;
            float _Softness;
            float _EdgePower;
            float _TipSmoke;

            struct Attr
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            struct V2F
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            float hash(float n) { return frac(sin(n) * 43758.5453); }
            float n2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i.x + i.y * 57.0);
                float b = hash(i.x + 1.0 + i.y * 57.0);
                float c = hash(i.x + (i.y + 1.0) * 57.0);
                float d = hash(i.x + 1.0 + (i.y + 1.0) * 57.0);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            V2F vert(Attr v)
            {
                V2F o;
                float3 pos = v.positionOS.xyz;
                float along = v.uv.y;
                float around = v.uv.x * 6.2831853;
                float t = _Scroll;

                // Multi-scale turbulence / heat shimmer — stronger toward tip
                float tip = along * along;
                float w = sin(along * 17.0 - t * 22.0 + around) * 0.12 * tip;
                w += sin(along * 8.0 + t * 31.0 + around * 2.0) * 0.10 * tip;
                w += sin(along * 31.0 - t * 41.0 + around * 3.0) * 0.055 * tip;
                w += (n2(float2(around * 3.1, along * 6.0 - t * 9.0)) - 0.5) * 0.16 * tip;
                // Subtle lengthwise heat shimmer (axial jitter)
                float shimmer = (n2(float2(around * 2.0 + t * 4.0, along * 14.0 - t * 18.0)) - 0.5) * 0.035 * tip;
                pos.x *= 1.0 + w;
                pos.z *= 1.0 + w * 0.82 + 0.06 * sin(around + t * 24.0) * tip;
                pos.y += shimmer;

                float3 worldPos = TransformObjectToWorld(pos);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(worldPos);
                return o;
            }

            half4 frag(V2F i) : SV_Target
            {
                float along = saturate(i.uv.y);
                float around = i.uv.x;
                float t = _Scroll;

                float turb = n2(float2(around * 12.0, along * 20.0 - t * 15.0));
                float turb2 = n2(float2(around * 26.0 + t * 5.0, along * 8.0 - t * 22.0));
                float turb3 = n2(float2(around * 48.0 - t * 3.0, along * 36.0 - t * 28.0));
                float n = lerp(1.0 - _NoiseAmt, 1.0 + _NoiseAmt * 0.85, turb);
                n *= lerp(0.88, 1.14, turb2);
                // Fine grain breaks solid "tube" silhouette
                n *= lerp(0.92, 1.08, turb3);

                // Longitudinal streaks (shock / shear)
                float streak = 0.72 + 0.28 * saturate(0.5 + 0.5 * sin(around * 18.85 + t * 13.0 + turb * 2.0));
                streak *= 0.85 + 0.15 * saturate(0.5 + 0.5 * sin(around * 41.0 - t * 19.0));

                // Soft entry at nozzle + soft tip into translucent smoke
                float entry = saturate(along * 28.0);
                float body = saturate(1.15 - along * 1.05);
                body = pow(body, _Softness);
                float tipCut = 1.0 - smoothstep(0.55, 1.0, along) * (0.35 + 0.65 * _TipSmoke);
                float fade = entry * body * tipCut * n * streak;

                // View-soft edges (less hard silhouette)
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);
                float ndv = abs(dot(N, V));
                float rimSoft = pow(saturate(ndv), _EdgePower);
                fade *= lerp(0.35, 1.0, rimSoft);

                // Sample color ramp; bias hotter near nozzle, cooler/smoke at tip
                float rampU = saturate(along * 0.92 + 0.04 * turb);
                half4 ramp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(rampU, 0.5));

                // Tip shifts toward cooler translucent exhaust
                float smokeK = smoothstep(0.45, 0.95, along) * _TipSmoke;
                float3 smokeCol = float3(0.55, 0.42, 0.28) * 0.35;
                float3 col = lerp(ramp.rgb, smokeCol, smokeK * 0.55);

                float inten = _Intensity * _Flicker;
                // Slight core punch near nozzle
                inten *= lerp(1.18, 0.72, along);

                float3 rgb = col * inten * fade;
                // Premultiplied-ish additive feel with soft alpha for smoke tips
                float a = saturate(fade * (0.55 + 0.45 * (1.0 - smokeK)));
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Lighting Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Intensity, _Flicker, _NoiseAmt, _Scroll, _Softness, _EdgePower, _TipSmoke;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 viewDir : TEXCOORD1; float3 normal : TEXCOORD2; };
            float hash(float n) { return frac(sin(n) * 43758.5453); }
            float n2(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i.x + i.y * 57.0);
                float b = hash(i.x + 1.0 + i.y * 57.0);
                float c = hash(i.x + (i.y + 1.0) * 57.0);
                float d = hash(i.x + 1.0 + (i.y + 1.0) * 57.0);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz;
                float along = v.uv.y;
                float around = v.uv.x * 6.2831853;
                float t = _Scroll;
                float tip = along * along;
                float w = sin(along * 17.0 - t * 22.0 + around) * 0.12 * tip;
                w += sin(along * 8.0 + t * 31.0 + around * 2.0) * 0.10 * tip;
                w += (n2(float2(around * 3.1, along * 6.0 - t * 9.0)) - 0.5) * 0.16 * tip;
                pos.x *= 1.0 + w;
                pos.z *= 1.0 + w * 0.82;
                pos.y += (n2(float2(around * 2.0 + t * 4.0, along * 14.0 - t * 18.0)) - 0.5) * 0.035 * tip;
                o.pos = UnityObjectToClipPos(float4(pos, 1));
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float along = saturate(i.uv.y);
                float around = i.uv.x;
                float t = _Scroll;
                float turb = n2(float2(around * 12.0, along * 20.0 - t * 15.0));
                float turb2 = n2(float2(around * 26.0 + t * 5.0, along * 8.0 - t * 22.0));
                float n = lerp(1.0 - _NoiseAmt, 1.0 + _NoiseAmt * 0.85, turb) * lerp(0.88, 1.14, turb2);
                float streak = 0.72 + 0.28 * saturate(0.5 + 0.5 * sin(around * 18.85 + t * 13.0));
                float entry = saturate(along * 28.0);
                float body = pow(saturate(1.15 - along * 1.05), _Softness);
                float tipCut = 1.0 - smoothstep(0.55, 1.0, along) * (0.35 + 0.65 * _TipSmoke);
                float fade = entry * body * tipCut * n * streak;
                float ndv = abs(dot(normalize(i.normal), normalize(i.viewDir)));
                fade *= lerp(0.35, 1.0, pow(saturate(ndv), _EdgePower));
                float rampU = saturate(along * 0.92 + 0.04 * turb);
                fixed4 ramp = tex2D(_MainTex, float2(rampU, 0.5));
                float smokeK = smoothstep(0.45, 0.95, along) * _TipSmoke;
                float3 smokeCol = float3(0.55, 0.42, 0.28) * 0.35;
                float3 col = lerp(ramp.rgb, smokeCol, smokeK * 0.55);
                float inten = _Intensity * _Flicker * lerp(1.18, 0.72, along);
                float3 rgb = col * inten * fade;
                float a = saturate(fade * (0.55 + 0.45 * (1.0 - smokeK)));
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
