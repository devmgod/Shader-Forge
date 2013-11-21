// Shader created with Shader Forge Alpha 0.15 
// Shader Forge (c) Joachim 'Acegikmo' Holmer
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:0.15;sub:START;pass:START;ps:lgpr:1,nrmq:1,limd:3,blpr:0,bsrc:3,bdst:7,culm:0,dpts:2,wrdp:True,uamb:False,ufog:True,aust:True,igpj:False,qofs:0,lico:1,qpre:1,flbk:,rntp:1,lmpd:True,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True;n:type:ShaderForge.SFN_Final,id:1,x:32359,y:32584|diff-2-RGB,spec-10-OUT,gloss-12-OUT,normal-15-RGB;n:type:ShaderForge.SFN_Tex2d,id:2,x:33088,y:32427,ptlb:MainTex,tex:b66bceaf0cc0ace4e9bdc92f14bba709|UVIN-5-OUT;n:type:ShaderForge.SFN_TexCoord,id:4,x:33479,y:32524,uv:0;n:type:ShaderForge.SFN_Multiply,id:5,x:33281,y:32582|A-4-UVOUT,B-6-OUT;n:type:ShaderForge.SFN_Vector1,id:6,x:33479,y:32668,v1:8;n:type:ShaderForge.SFN_Power,id:8,x:32913,y:32489|VAL-2-R,EXP-9-OUT;n:type:ShaderForge.SFN_Vector1,id:9,x:33088,y:32545,v1:5;n:type:ShaderForge.SFN_Multiply,id:10,x:32744,y:32583|A-8-OUT,B-11-OUT;n:type:ShaderForge.SFN_Vector1,id:11,x:32913,y:32617,v1:8;n:type:ShaderForge.SFN_Vector1,id:12,x:32637,y:32636,v1:32;n:type:ShaderForge.SFN_Tex2d,id:15,x:32913,y:32702,ptlb:BumpMap,tex:bbab0a6f7bae9cf42bf057d8ee2755f6|UVIN-5-OUT;proporder:2-15;pass:END;sub:END;*/

Shader "Shader Forge/Examples/Lightmapped" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _BumpMap ("BumpMap", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Tags {
                "LightMode"="ForwardBase"
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma exclude_renderers gles xbox360 ps3 flash 
            #pragma target 3.0
            #pragma glsl
            #ifndef LIGHTMAP_OFF
                sampler2D unity_Lightmap;
                float4 unity_LightmapST;
                #ifndef DIRLIGHTMAP_OFF
                    sampler2D unity_LightmapInd;
                #endif
            #endif
            uniform sampler2D _MainTex;
            uniform sampler2D _BumpMap;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                LIGHTING_COORDS(5,6)
                #ifndef LIGHTMAP_OFF
                    float2 uvLM : TEXCOORD7;
                #endif
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.uv0;
                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
                o.tangentDir = normalize( mul( _Object2World, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(_Object2World, v.vertex);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                #ifndef LIGHTMAP_OFF
                    o.uvLM = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float2 node_5 = (i.uv0.rg*8.0);
                float3 normalLocal = UnpackNormal(tex2D(_BumpMap,node_5)).rgb;
                float3 normalDirection = normalize( mul( normalLocal, tangentTransform ) );
                #ifndef LIGHTMAP_OFF
                    float4 lmtex = tex2D(unity_Lightmap,i.uvLM);
                    #ifndef DIRLIGHTMAP_OFF
                        float3 lightmap = DecodeLightmap(lmtex);
                        float3 scalePerBasisVector = DecodeLightmap(tex2D(unity_LightmapInd,i.uvLM));
                        UNITY_DIRBASIS
                        half3 normalInRnmBasis = saturate (mul (unity_DirBasis, normalLocal));
                        lightmap *= dot (normalInRnmBasis, scalePerBasisVector);
                    #else
                        float3 lightmap = DecodeLightmap(tex2D(unity_Lightmap,i.uvLM));
                    #endif
                #endif
                #ifndef LIGHTMAP_OFF
                    #ifdef DIRLIGHTMAP_OFF
                        float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                    #else
                        float3 lightDirection = normalize (scalePerBasisVector.x * unity_DirBasis[0] + scalePerBasisVector.y * unity_DirBasis[1] + scalePerBasisVector.z * unity_DirBasis[2]);
                        lightDirection = mul(lightDirection,tangentTransform); // Tangent to world
                    #endif
                #else
                    float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                #endif
//////// DEBUG - Lighting()
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
//////// DEBUG - CalcDiffuse()
                #ifndef LIGHTMAP_OFF
                    float3 diffuse = lightmap;
                #else
                    float3 diffuse = max( 0.0, dot(normalDirection,lightDirection )) * attenColor;
                #endif
                float gloss = exp2(32.0*10.0+1.0);
//////// DEBUG - CalcSpecular()
                float4 node_2 = tex2D(_MainTex,node_5);
                float node_10 = (pow(node_2.r,5.0)*8.0);
                float3 specular = attenColor * float3(node_10,node_10,node_10) * pow(max(0,dot(reflect(-lightDirection, normalDirection),viewDirection)),gloss);
                float3 lightFinal = diffuse + specular;
//////// DEBUG - Final output color
                return fixed4(lightFinal * node_2.rgb,1);
            }
            ENDCG
        }
        Pass {
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma exclude_renderers gles xbox360 ps3 flash 
            #pragma target 3.0
            #pragma glsl
            #ifndef LIGHTMAP_OFF
                sampler2D unity_Lightmap;
                float4 unity_LightmapST;
                #ifndef DIRLIGHTMAP_OFF
                    sampler2D unity_LightmapInd;
                #endif
            #endif
            uniform sampler2D _MainTex;
            uniform sampler2D _BumpMap;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                LIGHTING_COORDS(5,6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.uv0;
                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
                o.tangentDir = normalize( mul( _Object2World, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(_Object2World, v.vertex);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float2 node_5 = (i.uv0.rg*8.0);
                float3 normalLocal = UnpackNormal(tex2D(_BumpMap,node_5)).rgb;
                float3 normalDirection = normalize( mul( normalLocal, tangentTransform ) );
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
//////// DEBUG - Lighting()
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
//////// DEBUG - CalcDiffuse()
                float3 diffuse = max( 0.0, dot(normalDirection,lightDirection )) * attenColor;
                float gloss = exp2(32.0*10.0+1.0);
//////// DEBUG - CalcSpecular()
                float4 node_2 = tex2D(_MainTex,node_5);
                float node_10 = (pow(node_2.r,5.0)*8.0);
                float3 specular = attenColor * float3(node_10,node_10,node_10) * pow(max(0,dot(reflect(-lightDirection, normalDirection),viewDirection)),gloss);
                float3 lightFinal = diffuse + specular;
//////// DEBUG - Final output color
                return fixed4(lightFinal * node_2.rgb,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
