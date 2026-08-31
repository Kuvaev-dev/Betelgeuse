Shader "Betelgeuse/EnginePlume"
{
    Properties
    {
        _MainTex ("Ramp", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 12)) = 3
        _Flicker ("Flicker", Range(0, 2)) = 1
        _NoiseAmt ("Noise", Range(0, 1)) = 0.4
        _Scroll ("Scroll", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
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

            struct Attr { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V2F { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            V2F vert(Attr v)
            {
                V2F o;
                float3 pos = v.positionOS.xyz;
                float along = v.uv.y;
                float around = v.uv.x * 6.2831853;
                float t = _Scroll;
                float w = sin(along * 17.0 - t * 22.0 + around) * 0.14 * along;
                w += sin(along * 8.0 + t * 31.0 + around * 2.0) * 0.09 * along;
                w += sin(along * 31.0 - t * 41.0 + around * 3.0) * 0.05 * along;
                pos.x *= 1.0 + w;
                pos.z *= 1.0 + w * 0.8 + 0.05 * sin(around + t * 24.0) * along;
                o.positionCS = TransformObjectToHClip(pos);
                o.uv = v.uv;
                return o;
            }

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

            half4 frag(V2F i) : SV_Target
            {
                float along = saturate(i.uv.y);
                float around = i.uv.x;
                float t = _Scroll;
                float turb = n2(float2(around * 14.0, along * 22.0 - t * 16.0));
                float turb2 = n2(float2(around * 28.0 + t * 6.0, along * 9.0 - t * 23.0));
                float n = lerp(1.0 - _NoiseAmt, 1.0 + _NoiseAmt, turb);
                n *= lerp(0.85, 1.15, turb2);
                float streak = 0.75 + 0.25 * saturate(0.5 + 0.5 * sin(around * 18.85 + t * 14.0));
                float fade = saturate(1.2 - along * 1.08) * saturate(along * 22.0) * n * streak;
                half4 ramp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(along, 0.5));
                float3 col = ramp.rgb * (_Intensity * _Flicker) * fade;
                return half4(col, fade);
            }
            ENDHLSL
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            Lighting Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Intensity, _Flicker, _NoiseAmt, _Scroll;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz;
                float along = v.uv.y;
                float around = v.uv.x * 6.2831853;
                float t = _Scroll;
                float w = sin(along * 17.0 - t * 22.0 + around) * 0.14 * along;
                w += sin(along * 8.0 + t * 31.0 + around * 2.0) * 0.09 * along;
                pos.x *= 1.0 + w;
                pos.z *= 1.0 + w * 0.8;
                o.pos = UnityObjectToClipPos(float4(pos, 1));
                o.uv = v.uv;
                return o;
            }
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
            fixed4 frag(v2f i) : SV_Target
            {
                float along = saturate(i.uv.y);
                float around = i.uv.x;
                float t = _Scroll;
                float turb = n2(float2(around * 14.0, along * 22.0 - t * 16.0));
                float n = lerp(1.0 - _NoiseAmt, 1.0 + _NoiseAmt, turb);
                float fade = saturate(1.2 - along) * saturate(along * 22.0) * n;
                fixed4 ramp = tex2D(_MainTex, float2(along, 0.5));
                return fixed4(ramp.rgb * (_Intensity * _Flicker) * fade, fade);
            }
            ENDCG
        }
    }
    FallBack Off
}
