// Shader created with Shader Forge Beta 0.17 
// Shader Forge (c) Joachim 'Acegikmo' Holmer
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:0.17;sub:START;pass:START;ps:lgpr:1,nrmq:1,limd:1,blpr:0,bsrc:3,bdst:7,culm:2,dpts:2,wrdp:True,uamb:True,mssp:True,ufog:True,aust:True,igpj:False,qofs:0,lico:1,qpre:2,flbk:Transparent/Cutout/Diffuse,rntp:3,lmpd:False,lprd:True,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,hqsc:True,hqlp:False,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300;n:type:ShaderForge.SFN_Final,id:0,x:32768,y:32640|diff-322-OUT,spec-3-OUT,gloss-270-OUT,normal-2-RGB,transm-7-OUT,lwrap-6-OUT,clip-1-A,voffset-394-OUT;n:type:ShaderForge.SFN_Tex2d,id:1,x:33266,y:32629,ptlb:Diffuse,tex:66321cc856b03e245ac41ed8a53e0ecc,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:2,x:33266,y:32818,ptlb:Normal,tex:cb6c5165ed180c543be39ed70e72abc8,ntxv:3,isnm:True;n:type:ShaderForge.SFN_Vector1,id:3,x:33061,y:32641,v1:0.2;n:type:ShaderForge.SFN_Vector3,id:6,x:33266,y:33075,v1:0.9,v2:0.9,v3:0.8;n:type:ShaderForge.SFN_Vector3,id:7,x:33266,y:32976,v1:0.9,v2:1,v3:0.5;n:type:ShaderForge.SFN_Vector1,id:270,x:33061,y:32701,v1:0.4;n:type:ShaderForge.SFN_VertexColor,id:321,x:33508,y:32501;n:type:ShaderForge.SFN_Multiply,id:322,x:33061,y:32508|A-330-OUT,B-1-RGB;n:type:ShaderForge.SFN_Lerp,id:330,x:33266,y:32478|A-331-OUT,B-337-OUT,T-321-B;n:type:ShaderForge.SFN_Vector1,id:331,x:33508,y:32356,v1:1;n:type:ShaderForge.SFN_Vector3,id:337,x:33508,y:32410,v1:0.9632353,v2:0.8224623,v3:0.03541304;n:type:ShaderForge.SFN_VertexColor,id:389,x:33952,y:33347;n:type:ShaderForge.SFN_NormalVector,id:391,x:33765,y:33231,pt:False;n:type:ShaderForge.SFN_Time,id:392,x:33765,y:33586;n:type:ShaderForge.SFN_Sin,id:393,x:33359,y:33548|IN-413-OUT;n:type:ShaderForge.SFN_Multiply,id:394,x:33109,y:33420,cmnt:WInd animation|A-562-OUT,B-389-R,C-393-OUT,D-403-OUT;n:type:ShaderForge.SFN_Vector1,id:403,x:33359,y:33699,v1:0.016;n:type:ShaderForge.SFN_Add,id:413,x:33540,y:33548|A-519-OUT,B-392-T;n:type:ShaderForge.SFN_Multiply,id:519,x:33765,y:33457|A-389-B,B-520-OUT;n:type:ShaderForge.SFN_Pi,id:520,x:33952,y:33494;n:type:ShaderForge.SFN_Add,id:561,x:33544,y:33171|A-563-OUT,B-391-OUT;n:type:ShaderForge.SFN_Normalize,id:562,x:33359,y:33280|IN-561-OUT;n:type:ShaderForge.SFN_Vector3,id:563,x:33765,y:33126,cmnt:Wind direction,v1:1,v2:0.5,v3:0.5;proporder:1-2;pass:END;sub:END;*/

Shader "Shader Forge/Examples/ClipAndTransmission" {
    Properties {
        _Diffuse ("Diffuse", 2D) = "white" {}
        _Normal ("Normal", 2D) = "bump" {}
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Pass {
            Tags {
                "LightMode"="ForwardBase"
            }
            Cull Off
            
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
            uniform float4 _TimeEditor;
            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float3 shLight : TEXCOORD0;
                float4 uv0 : TEXCOORD1;
                float4 posWorld : TEXCOORD2;
                float3 normalDir : TEXCOORD3;
                float3 tangentDir : TEXCOORD4;
                float3 binormalDir : TEXCOORD5;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(6,7)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.uv0;
                o.vertexColor = v.vertexColor;
                o.shLight = ShadeSH9(float4(v.normal * unity_Scale.w,1)) * 0.5;
                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
                o.tangentDir = normalize( mul( _Object2World, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                float4 node_389 = o.vertexColor;
                float4 node_392 = _Time + _TimeEditor;
                v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g))*0.016);
                o.posWorld = mul(_Object2World, v.vertex);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float2 node_589 = i.uv0;
                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(node_589.rg, _Diffuse));
                clip(node_1.a - 0.5);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalLocal = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(node_589.rg, _Normal))).rgb;
                float3 normalDirection = normalize( mul( normalLocal, tangentTransform ) );
                
                float nSign = sign( dot( viewDirection, normalDirection ) ); // Reverse normal if this is a backface
                i.normalDir *= nSign;
                normalDirection *= nSign;
                
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 w = float3(0.9,0.9,0.8)*0.5; // Light wrapping
                float3 NdotLWrap = NdotL * ( 1.0 - w );
                float3 forwardLight = max(float3(0.0,0.0,0.0), NdotLWrap + w );
                float3 backLight = max(float3(0.0,0.0,0.0), -NdotLWrap + w ) * float3(0.9,1,0.5);
                float3 diffuse = (forwardLight+backLight) * attenColor;
///////// Gloss:
                float gloss = exp2(0.4*10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float node_3 = 0.2;
                float3 specularColor = float3(node_3,node_3,node_3);
                float3 specular = (floor(attenuation) * _LightColor0.xyz) * pow(max(0,dot(halfDirection,normalDirection)),gloss) * specularColor;
                float node_331 = 1.0;
                float3 finalColor = ( diffuse + i.shLight ) * (lerp(float3(node_331,node_331,node_331),float3(0.9632353,0.8224623,0.03541304),i.vertexColor.b)*node_1.rgb) + specular;
/// Final Color:
                return fixed4(finalColor,1);
            }
            ENDCG
        }
        Pass {
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            Cull Off
            
            Fog { Color (0,0,0,0) }
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
            uniform float4 _TimeEditor;
            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(5,6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.uv0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
                o.tangentDir = normalize( mul( _Object2World, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                float4 node_389 = o.vertexColor;
                float4 node_392 = _Time + _TimeEditor;
                v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g))*0.016);
                o.posWorld = mul(_Object2World, v.vertex);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float2 node_590 = i.uv0;
                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(node_590.rg, _Diffuse));
                clip(node_1.a - 0.5);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalLocal = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(node_590.rg, _Normal))).rgb;
                float3 normalDirection = normalize( mul( normalLocal, tangentTransform ) );
                
                float nSign = sign( dot( viewDirection, normalDirection ) ); // Reverse normal if this is a backface
                i.normalDir *= nSign;
                normalDirection *= nSign;
                
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 w = float3(0.9,0.9,0.8)*0.5; // Light wrapping
                float3 NdotLWrap = NdotL * ( 1.0 - w );
                float3 forwardLight = max(float3(0.0,0.0,0.0), NdotLWrap + w );
                float3 backLight = max(float3(0.0,0.0,0.0), -NdotLWrap + w ) * float3(0.9,1,0.5);
                float3 diffuse = (forwardLight+backLight) * attenColor;
///////// Gloss:
                float gloss = exp2(0.4*10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float node_3 = 0.2;
                float3 specularColor = float3(node_3,node_3,node_3);
                float3 specular = attenColor * pow(max(0,dot(halfDirection,normalDirection)),gloss) * specularColor;
                float node_331 = 1.0;
                float3 finalColor = diffuse * (lerp(float3(node_331,node_331,node_331),float3(0.9632353,0.8224623,0.03541304),i.vertexColor.b)*node_1.rgb) + specular;
/// Final Color:
                return fixed4(finalColor * 1,0);
            }
            ENDCG
        }
        Pass {
            Name "ShadowCollector"
            Tags {
                "LightMode"="ShadowCollector"
            }
            Cull Off
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCOLLECTOR
            #define SHADOW_COLLECTOR_PASS
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcollector
            #pragma exclude_renderers gles xbox360 ps3 flash 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                V2F_SHADOW_COLLECTOR;
                float4 uv0 : TEXCOORD5;
                float3 normalDir : TEXCOORD6;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.uv0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
                float4 node_389 = o.vertexColor;
                float4 node_392 = _Time + _TimeEditor;
                v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g))*0.016);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                TRANSFER_SHADOW_COLLECTOR(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(i.uv0.rg, _Diffuse));
                clip(node_1.a - 0.5);
                SHADOW_COLLECTOR_FRAGMENT(i)
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Cull Off
            Offset 1, 1
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma exclude_renderers gles xbox360 ps3 flash 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float4 uv0 : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.uv0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
                float4 node_389 = o.vertexColor;
                float4 node_392 = _Time + _TimeEditor;
                v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g))*0.016);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(i.uv0.rg, _Diffuse));
                clip(node_1.a - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
