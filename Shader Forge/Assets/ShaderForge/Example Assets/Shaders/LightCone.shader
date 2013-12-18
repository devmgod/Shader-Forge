// Shader created with Shader Forge Beta 0.15 
// Shader Forge (c) Joachim 'Acegikmo' Holmer
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:0.15;sub:START;pass:START;ps:lgpr:1,nrmq:1,limd:0,blpr:2,bsrc:0,bdst:0,culm:0,dpts:2,wrdp:False,uamb:False,ufog:False,aust:True,igpj:True,qofs:0,lico:1,qpre:3,flbk:,rntp:2,lmpd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.1280277,fgcg:0.1953466,fgcb:0.2352941,fgca:1,fgde:0.01,fgrn:0,fgrf:300;n:type:ShaderForge.SFN_Final,id:142,x:32741,y:32432|emission-200-OUT;n:type:ShaderForge.SFN_Tex2d,id:144,x:33576,y:32516,ptlb:Cone Falloff,tex:857a8e9195b715848abbbbb790d378b1,ntxv:0,isnm:False|UVIN-180-OUT;n:type:ShaderForge.SFN_Append,id:180,x:33749,y:32516|A-229-OUT,B-182-OUT;n:type:ShaderForge.SFN_TexCoord,id:181,x:34101,y:32615,uv:0;n:type:ShaderForge.SFN_ComponentMask,id:182,x:33922,y:32615,cc1:1,cc2:-1,cc3:-1,cc4:-1|IN-181-UVOUT;n:type:ShaderForge.SFN_Multiply,id:200,x:33004,y:32581|A-217-OUT,B-233-OUT;n:type:ShaderForge.SFN_Color,id:215,x:33377,y:32490,ptlb:Cone Color,c1:1,c2:1,c3:1,c4:0;n:type:ShaderForge.SFN_Multiply,id:217,x:33204,y:32448|A-219-OUT,B-215-RGB;n:type:ShaderForge.SFN_Slider,id:219,x:33377,y:32388,ptlb:Cone Strength,min:0,cur:0.55,max:2;n:type:ShaderForge.SFN_Vector1,id:226,x:34101,y:32507,v1:0.5;n:type:ShaderForge.SFN_Fresnel,id:229,x:33922,y:32473|EXP-226-OUT;n:type:ShaderForge.SFN_Tex2d,id:230,x:33749,y:32843,ptlb:Smoke,tex:28c7aad1372ff114b90d330f8a2dd938,ntxv:0,isnm:False|UVIN-246-OUT;n:type:ShaderForge.SFN_Multiply,id:233,x:33377,y:32680|A-144-R,B-251-OUT;n:type:ShaderForge.SFN_FragmentPosition,id:244,x:34471,y:32728;n:type:ShaderForge.SFN_Append,id:245,x:34287,y:32728|A-244-X,B-244-Z;n:type:ShaderForge.SFN_Multiply,id:246,x:33922,y:32843|A-255-UVOUT,B-318-OUT;n:type:ShaderForge.SFN_ConstantLerp,id:251,x:33576,y:32843,a:0.4,b:1|IN-230-R;n:type:ShaderForge.SFN_Panner,id:255,x:34101,y:32783,spu:1,spv:1|UVIN-245-OUT,DIST-297-TSL;n:type:ShaderForge.SFN_Time,id:297,x:34287,y:32854;n:type:ShaderForge.SFN_ValueProperty,id:318,x:34101,y:32978,ptlb:Smoke Scale,v1:0.5;proporder:144-215-219-230-318;pass:END;sub:END;*/

Shader "Shader Forge/Examples/LightCone" {
    Properties {
        _ConeFalloff ("Cone Falloff", 2D) = "white" {}
        _ConeColor ("Cone Color", Color) = (1,1,1,0)
        _ConeStrength ("Cone Strength", Range(0, 2)) = 0
        _Smoke ("Smoke", 2D) = "white" {}
        _SmokeScale ("Smoke Scale", Float ) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            ZWrite Off
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers xbox360 ps3 flash 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform sampler2D _ConeFalloff; uniform float4 _ConeFalloff_ST;
            uniform float4 _ConeColor;
            uniform float _ConeStrength;
            uniform sampler2D _Smoke; uniform float4 _Smoke_ST;
            uniform float _SmokeScale;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.uv0;
                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
                o.posWorld = mul(_Object2World, v.vertex);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = normalize(i.normalDir);
////// Lighting:
                float4 node_297 = _Time + _TimeEditor;
                float4 node_244 = i.posWorld;
                float3 lightFinal = ((_ConeStrength*_ConeColor.rgb)*(tex2D(_ConeFalloff,TRANSFORM_TEX(float2(pow(1.0-max(0,dot(normalDirection, viewDirection)),0.5),i.uv0.rg.g), _ConeFalloff)).r*lerp(0.4,1,tex2D(_Smoke,TRANSFORM_TEX(((float2(node_244.r,node_244.b)+node_297.r*float2(1,1))*_SmokeScale), _Smoke)).r)));
/// Final Color:
                return fixed4(lightFinal,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
