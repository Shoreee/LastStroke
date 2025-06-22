Shader "Custom/LeafSwingURP_WithWind_GlobalUpCelShading_SpecularEmission_Adjusted"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Pos ("Pivot Position", Vector) = (0, 0, 0, 0)
        _Direction ("Swing Direction", Vector) = (0, 0, 0, 0)
        _TimeScale ("Swing Speed", Float) = 1
        _TimeDelay ("Swing Delay", Float) = 1
        _WindStrength ("Wind Strength", Float) = 0.01
        _WindFrequency ("Wind Frequency", Float) = 0.1
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
        _LeafHeight ("Leaf Height", Float) = 1.0
        _BaseHeight ("Base Height", Float) = 0.0

        _ColorBase ("Base Color", Color) = (1, 1, 1, 1)
        _ColorMidDark ("Mid-Dark Color", Color) = (0.6, 0.6, 0.6, 1)
        _ColorShadow ("Shadow Color", Color) = (0.3, 0.3, 0.3, 1)
        _ColorSpecular ("Specular Highlight Color", Color) = (1, 0.95, 0.8, 1)

        _SpecularPower ("Specular Sharpness", Float) = 32.0
        _SpecularThreshold ("Specular Threshold", Float) = 0.95

        _EmissionColor ("Emission Color", Color) = (0.2, 0.2, 0.2, 1)
        _RimLightColor ("Rim Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _RimLight_Power ("Rim Light Power", Float) = 1.0
        _AmbientIntensity ("Ambient Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer assumeuniformscaling maxcount:500
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Pos;
            float4 _Direction;
            float _TimeScale;
            float _TimeDelay;
            float _WindStrength;
            float _WindFrequency;
            float4 _WindDirection;
            float _LeafHeight;
            float _BaseHeight;

            float4 _ColorBase;
            float4 _ColorMidDark;
            float4 _ColorShadow;
            float4 _ColorSpecular;

            float _SpecularPower;
            float _SpecularThreshold;

            float4 _EmissionColor;
            float4 _RimLightColor;
            float _RimLight_Power;
            float _AmbientIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : NORMAL;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float smoothNoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                float3 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), u.x),
                                 lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), u.x), u.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), u.x),
                                 lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), u.x), u.y), u.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                float3 posWS = TransformObjectToWorld(posOS);

                float heightFactor = saturate((posWS.y - _BaseHeight) / _LeafHeight);

                float randomSeed = dot(posWS, float3(12.9898, 78.233, 37.719)) + _Time.y;
                float3 randomDirection = normalize(float3(
                    sin(randomSeed * 1.2),
                    sin(randomSeed * 1.5),
                    sin(randomSeed * 1.7)
                ));

                float swingTime = (_Time.y + _TimeDelay) * _TimeScale;
                float selfSwing = sin(swingTime) * heightFactor;
                posOS += selfSwing * randomDirection * heightFactor;

                float windPhase = dot(posWS.xz, _WindDirection.xz) * _WindFrequency + _Time.y;
                float windSin = sin(windPhase);
                float windNoise = smoothNoise(posWS * 0.5 + _Time.y * 0.3);
                float windOffset = (windSin * 0.6 + windNoise * 0.4) * _WindStrength * heightFactor;
                posOS += windOffset * _WindDirection.xyz * heightFactor;

                float3 finalWS = TransformObjectToWorld(posOS);
                OUT.positionHCS = TransformWorldToHClip(finalWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.worldPos = finalWS;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                Light mainLight = GetMainLight(shadowCoord);

                half3 directDiffuse = LightingLambert(mainLight.color, mainLight.direction, normalWS) * mainLight.shadowAttenuation;
                half3 ambientSH = SampleSH(normalWS) * _AmbientIntensity;

                float NdotUp = dot(normalWS, float3(0, 1, 0));

                float4 baseColor;
                if (NdotUp > 0.5)
                    baseColor = _ColorBase;
                else if (NdotUp > 0.2)
                    baseColor = _ColorMidDark;
                else
                    baseColor = _ColorShadow;

                float3 reflectDir = reflect(-viewDir, normalWS);
                float specularFactor = pow(saturate(dot(reflectDir, viewDir)), _SpecularPower);
                float4 specularColor = (specularFactor > _SpecularThreshold) ? _ColorSpecular : float4(0, 0, 0, 0);

                float rimFactor = 1.0 - saturate(dot(normalWS, viewDir));
                float4 rimColor = _RimLightColor * pow(rimFactor, _RimLight_Power);

                half4 texColor = tex2D(_MainTex, IN.uv);

                // ★调整后：适度降低 EmissionColor 权重★
                float emissionStrength = 0.2; // 原为1，改为0.2
                float3 lit = baseColor.rgb * directDiffuse + ambientSH + specularColor.rgb + rimColor.rgb;
                float4 finalColor = float4(texColor.rgb * lit + _EmissionColor.rgb * emissionStrength, texColor.a);

                float noiseVal = smoothNoise(IN.worldPos * 0.5);
                float brightnessFactor = lerp(0.9, 1.1, noiseVal);
                finalColor.rgb *= brightnessFactor;

                return finalColor;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
