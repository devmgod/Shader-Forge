// Shader created with Shader Forge Alpha 0.15 
// Shader Forge (c) Joachim 'Acegikmo' Holmer
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:0.15;sub:START;pass:START;ps:lgpr:1,nrmq:1,limd:3,blpr:0,bsrc:0,bdst:0,culm:0,dpts:2,wrdp:True,uamb:True,ufog:True,aust:False,igpj:False,qofs:0,lico:1,qpre:1,flbk:,rntp:1,lmpd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True;n:type:ShaderForge.SFN_Final,id:0,x:32640,y:32624|diff-123-RGB,spec-46-OUT,gloss-42-OUT,normal-47-RGB;n:type:ShaderForge.SFN_Multiply,id:35,x:33552,y:32718|A-37-UVOUT,B-36-OUT;n:type:ShaderForge.SFN_Vector1,id:36,x:33749,y:32810,v1:8;n:type:ShaderForge.SFN_TexCoord,id:37,x:33749,y:32664,uv:0;n:type:ShaderForge.SFN_Vector1,id:42,x:32891,y:32729,v1:10;n:type:ShaderForge.SFN_Vector1,id:46,x:32891,y:32673,v1:0.8;n:type:ShaderForge.SFN_Tex2d,id:47,x:32891,y:32821,ptlb:Normal,tex:cf20bfced7e912046a9ce991a4d775ec|UVIN-110-UVOUT;n:type:ShaderForge.SFN_Parallax,id:110,x:33151,y:32714|UVIN-35-OUT,HEI-111-A,DEP-112-OUT;n:type:ShaderForge.SFN_Tex2d,id:111,x:33355,y:32608,tex:5fb7986dd6d0a8e4093ba82369dd6a4d|UVIN-35-OUT,TEX-113-TEX;n:type:ShaderForge.SFN_Vector1,id:112,x:33355,y:32793,v1:0.15;n:type:ShaderForge.SFN_Tex2dAsset,id:113,x:33552,y:32571,ptlb:AO (RGB) Height (A),tex:5fb7986dd6d0a8e4093ba82369dd6a4d;n:type:ShaderForge.SFN_Tex2d,id:123,x:32891,y:32544,tex:5fb7986dd6d0a8e4093ba82369dd6a4d|UVIN-110-UVOUT,TEX-113-TEX;proporder:113-47;pass:END;sub:END;*/

Shader "Shader Forge/Examples/Parallax" {
    Properties {
        _AORGBHeightA ("AO (RGB) Height (A)", 2D) = "white" {}
        _Normal ("Normal", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        LOD 128
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
            #pragma multi_compile_fwdbase_fullshadows
            #pragma exclude_renderers gles xbox360 ps3 flash 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _Normal;
            uniform sampler2D _AORGBHeightA;
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
                float2 node_35 = (i.uv0.rg*8.0);
                float2 node_110 = (0.15*(tex2D(_AORGBHeightA,node_35).a - 0.5)*mul(tangentTransform, viewDirection).xy + node_35);
                float3 normalLocal = UnpackNormal(tex2D(_Normal,node_110.rg)).rgb;
                float3 normalDirection = normalize( mul( normalLocal, tangentTransform ) );
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float3 diffuse = max( 0.0, dot(normalDirection,lightDirection )) * attenColor;
///////// Gloss:
                float gloss = exp2(10.0*10.0+1.0);
////// Specular:
                float node_46 = 0.8;
                float3 specular = attenColor * float3(node_46,node_46,node_46) * pow(max(0,dot(reflect(-lightDirection, normalDirection),viewDirection)),gloss);
                float3 lightFinal = diffuse * tex2D(_AORGBHeightA,node_110.rg).rgb + specular;
/// Final Color:
                return fixed4(lightFinal,1);
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
            #pragma multi_compile_fwdadd_fullshadows
            #pragma exclude_renderers gles xbox360 ps3 flash 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _Normal;
            uniform sampler2D _AORGBHeightA;
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
                float2 node_35 = (i.uv0.rg*8.0);
                float2 node_110 = (0.15*(tex2D(_AORGBHeightA,node_35).a - 0.5)*mul(tangentTransform, viewDirection).xy + node_35);
                float3 normalLocal = UnpackNormal(tex2D(_Normal,node_110.rg)).rgb;
                float3 normalDirection = normalize( mul( normalLocal, tangentTransform ) );
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float3 diffuse = max( 0.0, dot(normalDirection,lightDirection )) * attenColor;
///////// Gloss:
                float gloss = exp2(10.0*10.0+1.0);
////// Specular:
                float node_46 = 0.8;
                float3 specular = attenColor * float3(node_46,node_46,node_46) * pow(max(0,dot(reflect(-lightDirection, normalDirection),viewDirection)),gloss);
                float3 lightFinal = diffuse * tex2D(_AORGBHeightA,node_110.rg).rgb + specular;
/// Final Color:
                return fixed4(lightFinal,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
