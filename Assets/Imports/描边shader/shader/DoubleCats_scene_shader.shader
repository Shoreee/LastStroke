// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DC/DoubleCats_scene_shader "
{
	Properties
	{
		[NoScaleOffset]_Color_Tex("Color_Tex", 2D) = "white" {}
		_Color_uv("Color_uv", Vector) = (1,1,0,0)
		_Color_speed("Color_speed", Vector) = (1,0,0,0)
		[HDR]_R_light_color2("R_light_color", Color) = (1,0.92775,0.6462264,0)
		[HDR]_R_dark_clolor2("R_dark_clolor", Color) = (0.1509434,0.07064492,0.03203988,0)
		_Constart2("Constart", Range( 0.45 , 3)) = 1
		[HDR]_Color("Color", Color) = (1,1,1,0)
		_Color_power("Color_power", Range( 0 , 4)) = 1
		[NoScaleOffset]_Mask_Tex("Mask_Tex", 2D) = "white" {}
		_Mask_uv("Mask_uv", Vector) = (1,1,0,0)
		[NoScaleOffset][Normal]_Distort_Tex("Distort_Tex", 2D) = "bump" {}
		_Distort_uv1("Distort_uv", Vector) = (1,1,0,0)
		_Distort_speed1("Distort_speed", Vector) = (1,0,0,0)
		_distort_direction("distort_direction", Vector) = (1,1,0,0)
		_Distort("Distort", Range( -2 , 2)) = 0
		[Enum(off,0,on,1)]_Zwrite("Zwrite", Float) = 0
		[IntRange][Enum(UnityEngine.Rendering.CompareFunction)]_Ztest("Ztest", Float) = 4
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 0

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="AlphaTest" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		AlphaToMask Off
		Cull [_Cull]
		ColorMask RGBA
		ZWrite [_Zwrite]
		ZTest [_Ztest]
		Offset 0 , 0
		
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float _Zwrite;
			uniform float _Ztest;
			uniform float _Cull;
			uniform float4 _R_dark_clolor2;
			uniform float4 _R_light_color2;
			uniform sampler2D _Color_Tex;
			uniform float4 _Color_speed;
			uniform float4 _Color_uv;
			uniform float _Constart2;
			uniform float _Color_power;
			uniform float4 _Color;
			uniform sampler2D _Mask_Tex;
			uniform float4 _Mask_uv;
			uniform sampler2D _Distort_Tex;
			uniform float4 _Distort_speed1;
			uniform float4 _Distort_uv1;
			uniform float _Distort;
			uniform float2 _distort_direction;
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord1 = screenPos;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float4 break257 = _Color_speed;
				float mulTime259 = _Time.y * break257.z;
				float2 appendResult258 = (float2(break257.x , break257.y));
				float4 screenPos = i.ase_texcoord1;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float2 appendResult254 = (float2(_Color_uv.x , _Color_uv.y));
				float2 appendResult255 = (float2(_Color_uv.z , _Color_uv.w));
				float2 panner261 = ( mulTime259 * appendResult258 + ( ( (ase_grabScreenPosNorm).xy * appendResult254 ) + appendResult255 ));
				float4 lerpResult322 = lerp( _R_dark_clolor2 , _R_light_color2 , pow( tex2D( _Color_Tex, panner261 ).r , _Constart2 ));
				float2 appendResult292 = (float2(_Mask_uv.x , _Mask_uv.y));
				float2 appendResult295 = (float2(_Mask_uv.z , _Mask_uv.w));
				float2 texCoord300 = i.ase_texcoord2.xy * appendResult292 + appendResult295;
				float4 break311 = _Distort_speed1;
				float mulTime312 = _Time.y * break311.z;
				float2 appendResult313 = (float2(break311.x , break311.y));
				float2 appendResult310 = (float2(_Distort_uv1.x , _Distort_uv1.y));
				float2 appendResult309 = (float2(_Distort_uv1.z , _Distort_uv1.w));
				float2 texCoord314 = i.ase_texcoord2.xy * appendResult310 + appendResult309;
				float2 panner315 = ( mulTime312 * appendResult313 + texCoord314);
				float3 tex2DNode287 = UnpackNormal( tex2D( _Distort_Tex, panner315 ) );
				float2 appendResult290 = (float2(tex2DNode287.r , tex2DNode287.g));
				float2 Distort296 = ( appendResult290 * _Distort * _distort_direction );
				float4 appendResult267 = (float4((( lerpResult322 * _Color_power * _Color )).rgb , tex2D( _Mask_Tex, ( texCoord300 + Distort296 ) ).r));
				
				
				finalColor = appendResult267;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18800
-1920;72;1920;865;-1690.807;1285.957;1.309777;True;True
Node;AmplifyShaderEditor.Vector4Node;307;-411.061,881.7625;Inherit;False;Property;_Distort_uv1;Distort_uv;11;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1.5,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;308;-422.7871,1114.24;Inherit;False;Property;_Distort_speed1;Distort_speed;12;0;Create;True;0;0;0;False;0;False;1,0,0,0;-1,0,0.8,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;251;1161.256,-348.2753;Inherit;False;Property;_Color_uv;Color_uv;1;0;Create;True;0;0;0;False;0;False;1,1,0,0;1.5,1.5,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GrabScreenPosition;250;1048.187,-636.7717;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;309;108.4858,1105.821;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;310;125.4858,887.8215;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;311;-203.7949,1268.476;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.Vector4Node;252;1087.306,58.9292;Inherit;False;Property;_Color_speed;Color_speed;2;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,-1,1,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;254;1420.401,-512.491;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;253;1484.918,-639.0134;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;312;172.2429,1398.598;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;313;65.24292,1294.598;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;314;305.4858,956.8214;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;257;1410.264,-40.59142;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DynamicAppendNode;255;1509.4,-370.4903;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;256;1704.399,-640.4911;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;315;635.2429,1091.598;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;258;1730.301,-67.46837;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;260;1864.195,-465.4423;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;259;1783.976,145.3452;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;287;1123.23,1479.013;Inherit;True;Property;_Distort_Tex;Distort_Tex;10;2;[NoScaleOffset];[Normal];Create;True;0;0;0;False;0;False;-1;None;1d8bcbacf6c709445a935f117f716abf;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;261;2187.845,-393.3198;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;262;2462.757,-472.4198;Inherit;True;Property;_Color_Tex;Color_Tex;0;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;12c39a6ab6f1e1647a833c7c2904922e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;318;3103.699,-647.6522;Inherit;False;Property;_Constart2;Constart;5;0;Create;True;0;0;0;False;0;False;1;1;0.45;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;317;1869.946,1744.837;Inherit;False;Property;_distort_direction;distort_direction;13;0;Create;True;0;0;0;False;0;False;1,1;1,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;285;1426.377,1725.057;Inherit;False;Property;_Distort;Distort;14;0;Create;True;0;0;0;False;0;False;0;0.34;-2;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;290;1594.798,1522.351;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;289;1429.053,549.7963;Inherit;False;Property;_Mask_uv;Mask_uv;9;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1,-0.02,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;320;3242.713,-815.246;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;319;2898.146,-1107.176;Inherit;False;Property;_R_dark_clolor2;R_dark_clolor;4;1;[HDR];Create;True;0;0;0;False;0;False;0.1509434,0.07064492,0.03203988,0;0.2184475,1,0.1462264,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;295;2098.165,716.6702;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;321;2923.801,-1276.331;Inherit;False;Property;_R_light_color2;R_light_color;3;1;[HDR];Create;True;0;0;0;False;0;False;1,0.92775,0.6462264,0;1,0.2101665,0.08018869,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;292;2115.165,498.6704;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;294;2036.194,1498.774;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;264;3101.729,-32.03687;Inherit;False;Property;_Color;Color;6;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;263;3007.624,-229.8578;Inherit;False;Property;_Color_power;Color_power;7;0;Create;True;0;0;0;False;0;False;1;1.8;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;300;2747.293,603.797;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;296;2282.446,1478.016;Inherit;False;Distort;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;322;3358.474,-959.2536;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;305;3188.596,1181.301;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;248;3447.151,-383.0188;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;306;3570.995,568.4688;Inherit;True;Property;_Mask_Tex;Mask_Tex;8;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;f3f3ce48026210a4da40afdcb8a71e2b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SwizzleNode;249;3552.678,-166.3428;Inherit;True;FLOAT3;0;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;213;2652.722,-1986.817;Inherit;False;238.0281;428.5221;Comment;3;216;215;214;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;316;2178.946,1681.837;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;216;2702.722,-1674.295;Inherit;False;Property;_Ztest;Ztest;16;2;[IntRange];[Enum];Create;True;0;0;1;UnityEngine.Rendering.CompareFunction;True;0;False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;215;2706.2,-1776.931;Inherit;False;Property;_Cull;Cull;17;1;[Enum];Create;True;0;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;267;3912.233,50.4339;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;214;2697.75,-1877.817;Inherit;False;Property;_Zwrite;Zwrite;15;1;[Enum];Create;True;0;2;off;0;on;1;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;268;3663.233,137.4339;Inherit;False;Constant;_Float0;Float 0;10;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;265;4321.97,269.6674;Float;False;True;-1;2;ASEMaterialInspector;100;1;DC/DoubleCats_scene_shader ;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;False;False;False;False;False;False;True;0;False;-1;True;0;True;215;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;True;214;True;3;True;216;True;True;0;False;-1;0;False;-1;True;2;RenderType=Transparent=RenderType;Queue=AlphaTest=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;False;0
WireConnection;309;0;307;3
WireConnection;309;1;307;4
WireConnection;310;0;307;1
WireConnection;310;1;307;2
WireConnection;311;0;308;0
WireConnection;254;0;251;1
WireConnection;254;1;251;2
WireConnection;253;0;250;0
WireConnection;312;0;311;2
WireConnection;313;0;311;0
WireConnection;313;1;311;1
WireConnection;314;0;310;0
WireConnection;314;1;309;0
WireConnection;257;0;252;0
WireConnection;255;0;251;3
WireConnection;255;1;251;4
WireConnection;256;0;253;0
WireConnection;256;1;254;0
WireConnection;315;0;314;0
WireConnection;315;2;313;0
WireConnection;315;1;312;0
WireConnection;258;0;257;0
WireConnection;258;1;257;1
WireConnection;260;0;256;0
WireConnection;260;1;255;0
WireConnection;259;0;257;2
WireConnection;287;1;315;0
WireConnection;261;0;260;0
WireConnection;261;2;258;0
WireConnection;261;1;259;0
WireConnection;262;1;261;0
WireConnection;290;0;287;1
WireConnection;290;1;287;2
WireConnection;320;0;262;1
WireConnection;320;1;318;0
WireConnection;295;0;289;3
WireConnection;295;1;289;4
WireConnection;292;0;289;1
WireConnection;292;1;289;2
WireConnection;294;0;290;0
WireConnection;294;1;285;0
WireConnection;294;2;317;0
WireConnection;300;0;292;0
WireConnection;300;1;295;0
WireConnection;296;0;294;0
WireConnection;322;0;319;0
WireConnection;322;1;321;0
WireConnection;322;2;320;0
WireConnection;305;0;300;0
WireConnection;305;1;296;0
WireConnection;248;0;322;0
WireConnection;248;1;263;0
WireConnection;248;2;264;0
WireConnection;306;1;305;0
WireConnection;249;0;248;0
WireConnection;267;0;249;0
WireConnection;267;3;306;1
WireConnection;265;0;267;0
ASEEND*/
//CHKSM=7D7354D73F00B65D0525FBDD9887F2746192D399