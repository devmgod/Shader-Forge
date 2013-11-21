Shader "Fragment Exploration/BRDF_plain_beckmann" {
	Properties {
		_specular ("Specular", Float) = 0
		_albedo ("Albedo", Float) = 0
		_roughness ("Gloss (Roughness)", Range (0, 1)) = 0.5
	}

	SubShader {
		Tags {
			"LightMode"="ForwardBase"
		}
		Pass {
			
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag
			uniform float4 _LightColor0;
			uniform float _specular;
		    uniform float _roughness;
			uniform float _albedo;


			struct VertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
			};
			struct VertexOutput {
				float4 pos : SV_POSITION;
				float4 uv0 : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				float3 normalDir : TEXCOORD2;
				float3 tangentDir : TEXCOORD3;
				float3 binormalDir : TEXCOORD4;
			};
			VertexOutput vert (VertexInput v) {
				VertexOutput o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv0 = v.uv0;
				float3 normalDirection = mul(float4(v.normal,0), _World2Object).xyz;
				o.normalDir = normalDirection;
				o.posWorld = mul(_Object2World, v.vertex);
				return o;
			}
			fixed4 frag(VertexOutput i) : COLOR {
				float3 V = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
				float3 N = normalize( i.normalDir );
				float3 L = normalize(_WorldSpaceLightPos0.xyz);
				float3 H = normalize(V+L);
				
				float NdotH = saturate(dot(N,H));

				float bM2 = _roughness * _roughness;
				float beckAngle = acos( NdotH );
				float beckntan2 = -pow(tan(beckAngle),2);
				float beckNum = exp( beckntan2 / bM2 );
				float beckDen = 3.141592653*bM2*pow(cos(beckAngle),4);
				float beckmann = beckNum/beckDen;

				float3 lightFinal = beckmann;
				 

				/*
				// ALBEDO
				float3 albedo = (_albedo)/3.14159; // Diffuse replaces 0.5
				
				// FRESNEL
				float a = pow((_specular)/(2-_specular), 2);
				float fresnel = a+(1-a)*pow(1-dot(halfDirection, viewDirection),5);

				// ROUGHNESS
				float specPow = exp2(8 * _gloss + 1);
				float roughnessContrib = (specPow+2)/(8*3.14159);

				// NORMALS
				float normalContrib = pow(max(0,dot(normalDirection, halfDirection)), specPow);

				float3 lightFinal = (albedo+fresnel*roughnessContrib*normalContrib)*max(0,dot(lightDirection,normalDirection))*_LightColor0+UNITY_LIGHTMODEL_AMBIENT/3.14159;

				//lightFinal = float3(normalContrib);

				*/
				return fixed4(lightFinal,1);
			}
			ENDCG
		}
	}
	FallBack "Specular"
}
