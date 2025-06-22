Shader "Custom/Particle/FlareAdditive"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color",Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"IgnoreProjector"="True" "Queue"="Overlay" }
        LOD 200
        
        Blend SrcAlpha One
        ZWrite Off
        ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_particles
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            // struct v2f
            // {
                //     float2 uv : TEXCOORD0;
                //     float4 vertex : SV_POSITION;
            // };
            struct v2f 
            {
                float4 vertex		: SV_POSITION;
                fixed4 color		: COLOR;
                float2 uv	: TEXCOORD0;		//xy采样AlphaTex,zw采样NoiseTex
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                return col*_Color*i.color;
            }
            ENDCG
        }
    }
}

