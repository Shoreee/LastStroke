Shader "Unlit/SkyShader"
{
    Properties
    {
        _Sky ("Sky Texture", 2D) = "white" {}
        _SkyAlpha ("Sky Transparency", Range(0,1)) = 1.0
        _SkyNoiseTex ("Sky Noise Texture", 2D) = "white" {}
        _SkyNoiseSpeed ("Sky Noise Speed", Float) = 0.1
        _CrackTex ("Crack Texture", 2D) = "black" {}
        _CrackNoiseTex ("Crack Noise Texture", 2D) = "white" {}
        _CrackSpeed ("Crack Speed", Float) = 0.2
        _CrackThreshold ("Crack Threshold", Range(0,1)) = 0.5

        _SunRadius ("Sun Radius", Float) = 0.1
        _MoonRadius ("Moon Radius", Float) = 0.1
        _MoonTex ("Moon Texture", 2D) = "white" {}
        _MoonColor ("Moon Color", Color) = (1, 1, 1, 1)
        _MoonTex_ST ("Moon Texture Scale/Offset", Vector) = (1, 1, 0, 0)
        _DayBottomColor ("Day Bottom Color", Color) = (0.5, 0.7, 1, 1)
        _DayTopColor ("Day Top Color", Color) = (0.1, 0.3, 0.8, 1)
        _NightBottomColor ("Night Bottom Color", Color) = (0, 0, 0.2, 1)
        _NightTopColor ("Night Top Color", Color) = (0.1, 0.1, 0.3, 1)
        _Stars ("Stars Texture", 2D) = "white" {}
        _StarsSpeed ("Stars Speed", Float) = 0.01
        _StarsCutoff ("Stars Cutoff", Float) = 0.5
        _StarTex ("Star Texture", 2D) = "white" {}
        _StarNoise3D ("Star Noise 3D", 3D) = "white" {}
        _StarTex_ST ("StarTex Scale/Offset", Vector) = (1, 1, 0, 0)
        _StarNoise3D_ST ("StarNoise3D Scale/Offset", Vector) = (1, 1, 0, 0)
        _GalaxyNoiseTex ("Galaxy Noise Texture", 2D) = "white" {}
        _GalaxyTex ("Galaxy Texture", 2D) = "white" {}
        _GalaxyTex_ST ("GalaxyTex Scale/Offset", Vector) = (1, 1, 0, 0)
        _GalaxyColor ("Galaxy Color", Color) = (1, 1, 1, 1)
        _GalaxyColor1 ("Galaxy Color 1", Color) = (1, 1, 1, 1)
        _Time ("Time", Float) = 0.0
        _MieStrength ("Mie Scattering Strength", Float) = 0.5
        _MieColor ("Mie Scattering Color", Color) = (1, 1, 1, 1)
        _PlanetRadius ("Planet Radius", Float) = 100 // 地球半径
        _AtmosphereHeight ("Atmosphere Height", Float) = 100 // 大气层高度
        _DensityScaleHeight ("Density Scale Height", Float) = 8.0 // 大气密度高度比例
        _ExtinctionM ("Extinction Coefficient", Float) = 1.0
        _ScatteringM ("Scattering Coefficient", Float) = 1.0
        _MieG ("Mie Phase Function G", Float) = 0.76 // 控制 Mie 相位函数的渐近因子
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 skyUV : TEXCOORD2;
            };
            // 添加新变量声明
            sampler2D _Sky;
            float _SkyAlpha;
            sampler2D _SkyNoiseTex;
            float _SkyNoiseSpeed;
            sampler2D _CrackTex;
            sampler2D _CrackNoiseTex;
            float _CrackSpeed;
            float _CrackThreshold;


            sampler2D _Stars;
            float _StarsSpeed, _StarsCutoff;
            float _SunRadius, _MoonRadius;
            float3 _MoonColor;
            sampler2D _MoonTex;
            float4 _MoonTex_ST;
            float3 _DayBottomColor, _DayTopColor, _NightBottomColor, _NightTopColor;
            float4x4 _LocalToWorldMatrix;
            sampler2D _StarTex;
            sampler3D _StarNoise3D;
            float4 _StarTex_ST;
            float4 _StarNoise3D_ST;
            sampler2D _GalaxyNoiseTex;
            sampler2D _GalaxyTex;
            float4 _GalaxyTex_ST;
            float4 _GalaxyColor;
            float4 _GalaxyColor1;

            float _MieStrength;
            float3 _MieColor;
            float _PlanetRadius;
            float _AtmosphereHeight;
            float _DensityScaleHeight;
            float _ExtinctionM;
            float _ScatteringM;
            float  _MieG;
            

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = normalize(o.worldPos);
                o.skyUV = v.vertex.xz * 0.5 + 0.5;
                return o;
            }
            void ComputeOutLocalDensity(float3 position, float3 lightDir, out float localDPA, out float DPC)
            {
                float3 planetCenter = float3(0,-_PlanetRadius,0);
                float height = distance(position,planetCenter) - _PlanetRadius;
                localDPA = exp(-(height/_DensityScaleHeight));
            
                DPC = 0;
                //DPC = ComputeDensityCP(position,lightDir);
                /*
                float cosAngle = dot(normalize(position - planetCenter), -lightDir.xyz);
                DPC = tex2D(_TestTex,float2(cosAngle,height / _AtmosphereHeight)).r;
                */
            }
            float MiePhaseFunction(float cosAngle)
            {
                float g = _MieG;
                float g2 = g * g;
                float phase = (1.0 / (4.0 * 3.1415)) * ((3.0 * (1.0 - g2)) / (2.0 * (2.0 + g2))) * ((1 + cosAngle * cosAngle) / (pow((1 + g2 - 2 * g*cosAngle), 3.0 / 2.0)));
                return phase;
            }
            
            float4 IntegrateInscattering(float3 rayStart,float3 rayDir,float rayLength, float3 lightDir,float sampleCount)
            {
                float3 stepVector = rayDir * (rayLength / sampleCount);
                float stepSize = length(stepVector);
            
                float scatterMie = 0;
            
                float densityCP = 0;
                float densityPA = 0;
                float localDPA = 0;
            
                float prevLocalDPA = 0;
                float prevTransmittance = 0;
                
                ComputeOutLocalDensity(rayStart,lightDir, localDPA, densityCP);
                
                densityPA += localDPA*stepSize;
                prevLocalDPA = localDPA;
            
                float Transmittance = exp(-(densityCP + densityPA)*_ExtinctionM)*localDPA;
                
                prevTransmittance = Transmittance;
                
            
                for(float i = 1.0; i < sampleCount; i += 1.0)
                {
                    float3 P = rayStart + stepVector * i;
                    
                    ComputeOutLocalDensity(P,lightDir,localDPA,densityCP);
                    densityPA += (prevLocalDPA + localDPA) * stepSize/2;
            
                    Transmittance = exp(-(densityCP + densityPA)*_ExtinctionM)*localDPA;
            
                    scatterMie += (prevTransmittance + Transmittance) * stepSize/2;
                    
                    prevTransmittance = Transmittance;
                    prevLocalDPA = localDPA;
                }
            
                scatterMie = scatterMie * MiePhaseFunction(dot(rayDir,-lightDir.xyz));
            
                float3 lightInscatter = _ScatteringM*scatterMie;
            
                return float4(lightInscatter,1);
            }
            float2 RaySphereIntersection(float3 rayOrigin, float3 rayDir, float3 sphereCenter, float sphereRadius)
            {
                float3 oc = rayOrigin - sphereCenter;
                
                float a = dot(rayDir, rayDir);
                float b = 2.0 * dot(oc, rayDir);
                float c = dot(oc, oc) - sphereRadius * sphereRadius;
            
                float discriminant = b * b - 4.0 * a * c;
            
                if (discriminant < 0.0)
                {
                    return float2(-1.0, -1.0);
                }
                else
                {
                    float t1 = (-b - sqrt(discriminant)) / (2.0 * a);
                    float t2 = (-b + sqrt(discriminant)) / (2.0 * a);
                    return float2(t1, t2); 
                }
            }

            float3 ACESFilm(float3 x)
            {
                float a = 2.51;
                float b = 0.03;
                float c = 2.43;
                float d = 0.59;
                float e = 0.14;
                
                // Apply ACES color grading formula
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }
            

            float4 frag(v2f i) : SV_Target
            {
                
                // 将天空球的UV转换到方向光的空间
                float3 sunUV = mul(i.uv.xyz, _LocalToWorldMatrix);
                float2 moonUV = sunUV.xy * _MoonTex_ST.xy + _MoonTex_ST.zw;
                float4 moonTex = tex2D(_MoonTex, moonUV);
                float3 finalMoonColor = (_MoonColor * moonTex.rgb * moonTex.a) * step(0, sunUV.z);

                // 太阳与月亮的光晕（sun flare, moon flare）
                float sunDist = distance(i.uv, _WorldSpaceLightPos0);
                float sunDisc = saturate(1 - sunDist / _SunRadius);
                sunDisc = saturate(sunDisc * 100);

                // 天空颜色渐变
                float3 dayGradient = lerp(_DayBottomColor, _DayTopColor, saturate(i.uv.y));
                float3 nightGradient = lerp(_NightBottomColor, _NightTopColor, saturate(i.uv.y));
                float3 skyColor = lerp(nightGradient, dayGradient, saturate(_WorldSpaceLightPos0.y));

                // 星星
                float2 skyUV = i.worldPos.xz / clamp(i.worldPos.y, 0, 500);
                float3 stars = tex2D(_Stars, skyUV + float2(_StarsSpeed, _StarsSpeed) * _Time);
                stars = step(_StarsCutoff, stars);

                // 处理星星和银河部分
                // 星星部分
                float4 starTex = tex2D(_StarTex, i.uv.xz * _StarTex_ST.xy + _StarTex_ST.zw);
                float4 starNoiseTex = tex3D(_StarNoise3D, i.uv.xyz * _StarNoise3D_ST.x + _Time.x * 0.12);
                float starPos = smoothstep(0.21, 0.31, starTex.r);
                float starBright = smoothstep(0.613, 0.713, starNoiseTex.r);
                float starColor = starPos * starBright;
            

                // 银河部分
                float4 galaxyNoiseTex = tex2D(_GalaxyNoiseTex, i.uv.xz * _GalaxyTex_ST.xy + _GalaxyTex_ST.zw + float2(0, _Time.x * 0.67));
                
                // 添加 galaxy 声明
                float4 galaxy = tex2D(_GalaxyTex, (i.uv.xz + (galaxyNoiseTex - 0.5) * 0.3) * _GalaxyTex_ST.xy + _GalaxyTex_ST.zw);
                starColor = starColor * galaxy.r + starColor * (1 - galaxy.r) * 0;//设置银河内部和外部亮度
                float starMask = lerp((1 - smoothstep(-0.7, -0.2, -i.uv.y)), 0, sunDisc);

                float4 galaxyColor = (_GalaxyColor * (-galaxy.r + galaxy.g) + _GalaxyColor1 * galaxy.r) * smoothstep(0, 0.2, 1 - galaxy.g);
                galaxyNoiseTex = tex2D(_GalaxyNoiseTex, i.uv.xz * _GalaxyTex_ST.xy + _GalaxyTex_ST.zw - float2(_Time.x * 0.5, _Time.x * 0.67));
                galaxyColor += (_GalaxyColor * (-galaxy.r + galaxy.g) + _GalaxyColor1 * galaxy.r) * smoothstep(0, 0.3, 1 - galaxy.g);

                //大气散射
                float3 scatteringColor = 0;

                float3 rayStart = float3(0,10,0);
                rayStart.y = saturate(rayStart.y);
                float3 rayDir = normalize(i.uv.xyz);
                
                float3 planetCenter = float3(0, -_PlanetRadius, 0);
                float2 intersection = RaySphereIntersection(rayStart, rayDir, planetCenter, _PlanetRadius + _AtmosphereHeight);
                float rayLength = intersection.y;
                
                intersection = RaySphereIntersection(rayStart, rayDir, planetCenter, _PlanetRadius);
                if (intersection.x > 0)
                    rayLength = min(rayLength, intersection.x * 100);
                
                float4 inscattering = IntegrateInscattering(rayStart, rayDir, rayLength, -_WorldSpaceLightPos0.xyz, 16);
                scatteringColor = _MieColor * _MieStrength * ACESFilm(inscattering);

                float3  existingColor = skyColor + sunDisc + finalMoonColor + stars + starColor * starMask + galaxyColor + scatteringColor;

                 // 噪声扰动天空UV
                 float2 skyNoiseUV = i.skyUV + _Time.x * _SkyNoiseSpeed;
                 float4 skyNoise = tex2D(_SkyNoiseTex, skyNoiseUV);
                 float2 distortedUV = i.skyUV + (skyNoise.rg - 0.5) * 0.1;
                 
                 // 基础天空颜色
                 float4 sky0Color = tex2D(_Sky, distortedUV);
                 
                 // 裂缝噪声
                 float2 crackNoiseUV = i.skyUV * 2.0 + _Time.x * _CrackSpeed;
                 float4 crackNoise = tex2D(_CrackNoiseTex, crackNoiseUV);
                 
                 // 生成裂缝遮罩
                 float crackMask = step(_CrackThreshold, crackNoise.r);
                 
                 // 裂缝颜色
                 float4 crackColor = tex2D(_CrackTex, distortedUV);
                 
                 // 混合天空和裂缝
                 float4 finalSky = lerp(sky0Color, crackColor, crackMask);
                 
                 float3 blendedColor = lerp(existingColor, finalSky.rgb, _SkyAlpha);
                 float4 finalColor = float4(blendedColor, 1.0);

                return finalColor;

            }
            ENDCG
        }
    }
}
