using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;
using UnityEditor.VersionControl;
using System.Linq;

namespace ShaderForge {


	public enum PassType { 
		FwdBase, FwdAdd, ShadColl, ShadCast,
		Outline,
		PrePassBase, PrePassFinal
	
	
	};
	public enum ShaderProgram { Any, Vert, Frag, Tess };

	[System.Serializable]
	public class SF_Evaluator {

		public SF_Editor editor;
		public List<SF_Node> cNodes;
		public List<SF_Node> ghostNodes;
		public int scope = 0;
		public string shaderString = "";

		public SF_PassSettings ps;
		public SF_Dependencies dependencies;
		public SF_ShaderProperty properties;

		public SFN_Final mOut;

		const bool DEBUG = true;


		
		

		public static PassType currentPass = PassType.FwdBase;
		public static ShaderProgram currentProgram = ShaderProgram.Vert;

		public static bool inFrag {
			get {
				return SF_Evaluator.currentProgram == ShaderProgram.Frag;
			}
		}
		public static bool inVert {
			get {
				return SF_Evaluator.currentProgram == ShaderProgram.Vert;
			}
		}
		public static bool inTess {
			get {
				return SF_Evaluator.currentProgram == ShaderProgram.Tess;
			}
		}

		public static string WithProgramPrefix( string s ) {
			if( SF_Evaluator.inFrag )
				return "i." + s;
			if( SF_Evaluator.inVert )
				return "o." + s;
			if( SF_Evaluator.inTess )
				return "v." + s;
			Debug.Log( "Invalid program" );
			return null;
		}



		// TODO: SHADER MODEL
		public SF_Evaluator() {


		}

		public SF_Evaluator( SF_Editor editor ) {
			this.editor = editor;
			this.ps = editor.ps;
		}


		public void PrepareEvaluation() {
			ps.UpdateAutoSettings();

			mOut = editor.mainNode;
		}

		public void RemoveGhostNodes() {
			if(ghostNodes == null)
				return;

			if(SF_Debug.ghostNodes)
				Debug.Log( "Removing ghost nodes. Count: " + ghostNodes.Count );
			for( int i = ghostNodes.Count - 1; i >= 0; i-- ) {
				editor.nodes.Remove( ghostNodes[i] );
				ghostNodes[i].DeleteGhost();
				ghostNodes.Remove( ghostNodes[i] );
			}
			//Debug.Log( "Done removing ghost nodes. Count: " + ghostNodes.Count );
		}


		public void UpdateDependencies() {

			dependencies = new SF_Dependencies( editor.ps );

			if(SF_Debug.evalFlow)
				Debug.Log("UPDATING DEPENDENCIES: Pass = " + currentPass + " Prog = " + currentProgram);
			cNodes = editor.nodeView.treeStatus.GetListOfConnectedNodesWithGhosts( out ghostNodes, passDependent:true );
			if(SF_Debug.evalFlow)
				Debug.Log("Found " + cNodes.Count + " nodes");


			for( int i = 0; i < cNodes.Count; i++ ) {
				cNodes[i].PrepareEvaluation();
			}

		
			// Dependencies
			if( ps.catLighting.IsLit() && !IsShadowOrOutlinePass() && currentPass != PassType.PrePassFinal && currentPass != PassType.PrePassBase ) {
				dependencies.NeedLightColor();
				dependencies.NeedFragNormals();
				dependencies.NeedFragLightDir();
				
				if( ( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.BlinnPhong || ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL) && ps.mOut.specular.IsConnectedEnabledAndAvailableInThisPass(currentPass) ) {
					dependencies.NeedFragHalfDir();
				}
			}

			if( currentPass == PassType.PrePassBase){
				dependencies.NeedFragNormals();
			}

			if( DoPassLightAccumulation() ){
				dependencies.frag_projPos = true;
			}

			if( ps.catLighting.reflectprobed && ps.HasSpecular() && ( currentPass == PassType.PrePassFinal || currentPass == PassType.FwdBase ) ) {
				dependencies.NeedFragViewReflection();
				dependencies.reflection_probes = true;
			}



			if(ps.catLighting.lightprobed  && !IsShadowOrOutlinePass()){
				dependencies.vert_in_normals = true;
				if(ps.catQuality.highQualityLightProbes)
					dependencies.NeedFragNormals();
			}

			if( ps.IsOutlined() && currentPass == PassType.Outline ){
				dependencies.vert_in_normals = true;
			}

			if(ps.catLighting.IsVertexLit() && ps.catLighting.IsLit() && !IsShadowOrOutlinePass() ){
				if(ps.catLighting.lightMode == SFPSC_Lighting.LightMode.BlinnPhong || ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL)
					dependencies.NeedVertHalfDir();
				dependencies.NeedVertLightDir();
			}


			if( ps.catLighting.lightmapped && !IsShadowOrOutlinePass() ) {
				dependencies.NeedFragTangentTransform(); // Directional LMs
				dependencies.uv1 = true; // Lightmap UVs
			}

			//if( ps.HasAnisotropicLight() && !IsShadowPass() ) {
			//	dependencies.NeedFragTangents();
			//	dependencies.NeedFragBinormals();
			//}

			

			if( ps.catLighting.IsFragmentLit() && !IsShadowOrOutlinePass() ) {
				dependencies.vert_in_normals = true;
				dependencies.vert_out_normals = true;
				dependencies.vert_out_worldPos = true;
				dependencies.frag_normalDirection = true;
				if( ps.HasNormalMap() || ps.catLighting.HasSpecular() )
					dependencies.NeedFragViewDirection();
			}

			if( ps.HasNormalMap() && !IsShadowOrOutlinePass() ) {
				dependencies.frag_normalDirection = true;
				dependencies.NeedFragTangentTransform();
			}

			if( ps.HasRefraction() && !IsShadowOrOutlinePass() ) {
				dependencies.NeedRefraction();
			}

			if( ps.HasTessellation() ) {
				dependencies.NeedTessellation();
			}

			if( ps.HasDisplacement() ) {
				dependencies.NeedDisplacement();
			}






			foreach( SF_Node n in cNodes ) {

				if( n is SFN_Time ) {
					//Debug.Log("TIME DEPENDENCY");
					dependencies.time = true;
				}

				if( n is SFN_SceneColor){
					if((n as SFN_SceneColor).AutoUV())
						dependencies.NeedFragScreenPos();
					dependencies.NeedGrabPass();
				}

				if( n is SFN_ObjectPosition ) {
					if(currentProgram == ShaderProgram.Frag)
						dependencies.NeedFragObjPos();
					else
						dependencies.NeedVertObjPos();
				}

				if( n is SFN_Fresnel ) {
					dependencies.NeedFragViewDirection();
					if(!n.GetInputIsConnected("NRM")) // Normal. If it's not connected, make sure we have the dependency for normals
						dependencies.NeedFragNormals();
				}

				if( n is SFN_FragmentPosition ) {
					dependencies.NeedFragWorldPos();
				}

				if( n is SFN_SceneDepth ){
					dependencies.NeedSceneDepth();
				}

				if( n is SFN_DepthBlend ){
					dependencies.NeedSceneDepth();
					dependencies.frag_pixelDepth = true;
				}

				if(n is SFN_Depth){
					// (mul( UNITY_MATRIX_V, float4((_WorldSpaceCameraPos.rgb-i.posWorld.rgb),0) ).b - _ProjectionParams.g)
					dependencies.NeedFragPixelDepth();
				}

				if( n is SFN_ObjectScale ) {
					if( ( n as SFN_ObjectScale ).reciprocal )
						dependencies.objectScaleReciprocal = true;
					else
						dependencies.objectScale = true;
				}

				/*
				if( n is SFN_Rotator ) {
					if(!n.GetInputIsConnected("ANG"))
						dependencies.time = true;
				}*/

				/*
				if( n is SFN_Panner ) {
					if( !n.GetInputIsConnected( "DIST" ) )
						dependencies.time = true;
				}
				*/

				if( n is SFN_ScreenPos ) {
					dependencies.NeedFragScreenPos();
					if((n as SFN_ScreenPos).currentType == SFN_ScreenPos.ScreenPosType.SceneUVs){
						dependencies.NeedSceneUVs();
					}
				}

				if( n.GetType() == typeof( SFN_Tex2d ) ) {
					if( n.GetInputIsConnected( "MIP" ) ) { // MIP connection
						//dependencies.ExcludeRenderPlatform( RenderPlatform.opengl ); // TODO: Find workaround!
						dependencies.SetMinimumShaderTarget( 3 );
					}
				}

				if( n.GetType() == typeof( SFN_Cubemap ) ) {
					if( n.GetInputIsConnected( "MIP" ) ) { // MIP connection
						//dependencies.ExcludeRenderPlatform( RenderPlatform.opengl ); // TODO: Find workaround!
						dependencies.SetMinimumShaderTarget( 3 );
					}
				}

				/*
				if( n is SFN_Tex2d ) {
					if( !n.GetInputIsConnected( "UVIN" ) ) { // Unconnected UV input
						dependencies.uv0 = true;
						dependencies.uv0_frag = true;
					}
				}*/

				if( n.GetType() == typeof( SFN_VertexColor ) ) {
					dependencies.NeedFragVertexColor(); // TODO: Check if it really needs to be frag
				}

				if( n.GetType() == typeof( SFN_TexCoord ) ) {
					switch( ( (SFN_TexCoord)n ).currentUV ) {
						case SFN_TexCoord.UV.uv0:
							dependencies.uv0 = true;
							dependencies.uv0_frag = true;
							break;
						case SFN_TexCoord.UV.uv1:
							dependencies.uv1 = true;
							dependencies.uv1_frag = true;
							break;
					}
				}
				if( n.GetType() == typeof( SFN_Pi ) ) {
					dependencies.const_pi = true;
				}
				if( n.GetType() == typeof( SFN_Phi ) ) {
					dependencies.const_phi = true;
				}
				if( n.GetType() == typeof( SFN_E ) ) {
					dependencies.const_e = true;
				}
				if( n.GetType() == typeof( SFN_Root2 ) ) {
					dependencies.const_root2 = true;
				}
				if( n.GetType() == typeof( SFN_Tau ) ) {
					dependencies.const_tau = true;
				}

				if( n.GetType() == typeof( SFN_HalfVector ) ) {
					dependencies.NeedFragHalfDir();
				}
				if( n.GetType() == typeof( SFN_LightColor ) ) {
					dependencies.NeedLightColor();
				}


				if( n is SFN_Parallax ) {
					dependencies.NeedFragViewDirection();
					dependencies.NeedFragTangentTransform();
					if( !( n as SFN_Parallax ).GetInputIsConnected( "UVIN" ) ) {
						dependencies.uv0 = true;
					}
				}

				if( n.GetType() == typeof( SFN_Cubemap ) ) {
					if( !n.GetInputIsConnected( "DIR" ) ) { // DIR connection, if not connected, we need default reflection vector
						dependencies.NeedFragViewReflection();
					}
				}


				
				if(SF_Editor.NodeExistsAndIs(n, "SFN_SkyshopSpec")){
					if( !n.GetInputIsConnected( "REFL" ) ) { // Reflection connection, if not connected, we need default reflection vector
						dependencies.NeedFragViewReflection();
					}
				}

				if( n.GetType() == typeof( SFN_LightAttenuation ) ) {
					dependencies.NeedFragAttenuation();
				}

				if( n.GetType() == typeof( SFN_ViewReflectionVector ) ) {
					dependencies.NeedFragViewReflection();
				}

				if( n.GetType() == typeof( SFN_LightVector ) ) {
					dependencies.NeedFragLightDir();
				}

				if( n.GetType() == typeof( SFN_ViewVector ) ) {
					dependencies.NeedFragViewDirection();
				}

				if( n is SFN_Tangent ) {
					dependencies.NeedFragTangents();
				}
				if( n is SFN_Binormal ) {
					dependencies.NeedFragBinormals();
				}
				if( n is SFN_NormalVector ) {
					dependencies.NeedFragNormals();
				}

				

				if( n.GetType() == typeof( SFN_Transform ) ) {
					if( ( n as SFN_Transform ).spaceSelFrom == SFN_Transform.Space.Tangent || (n as SFN_Transform).spaceSelTo == SFN_Transform.Space.Tangent ) {
						dependencies.NeedFragTangentTransform();
					}
				}


				// This has to be done afterwards
				if( dependencies.frag_normalDirection && ps.catBlending.IsDoubleSided() && !IsShadowOrOutlinePass() ) {
					dependencies.NeedFragViewDirection();
				}


			}

			//RemoveGhostNodes(); // TODO: Maybe not here?

			if(SF_Debug.evalFlow)
				Debug.Log( "DONE UPDATING DEPENDENCIES" );
		}





		void BeginShader() {
			App( "Shader \"" + editor.currentShaderPath + "\" {" );
			scope++;
		}
		void BeginProperties() {
			App( "Properties {" );
			scope++;
		}

		void PropertiesShaderLab() {

			BeginProperties();

			//Debug.Log("Printing properties, count = " + editor.nodeView.treeStatus.propertyList.Count);

			for( int i = 0; i < editor.nodeView.treeStatus.propertyList.Count; i++ ) {
				if(editor.nodeView.treeStatus.propertyList[i] == null){
					editor.nodeView.treeStatus.propertyList.RemoveAt(i);
					i = -1; // restart
				}
				if( editor.nodeView.treeStatus.propertyList[i].IsProperty() ) {
					string line = editor.nodeView.treeStatus.propertyList[i].property.GetInitializationLine();
					App( line );
				}
			}

			bool transparency = ps.mOut.alphaClip.IsConnectedEnabledAndAvailable() || ps.mOut.alpha.IsConnectedEnabledAndAvailable();

			if(transparency)
				App ("[HideInInspector]_Cutoff (\"Alpha cutoff\", Range(0,1)) = 0.5"); // Hack, but, required for transparency to play along with depth etc

			End();

		}
		void PropertiesCG() {
			for( int i = 0; i < cNodes.Count; i++ ) {
				AppIfNonEmpty (cNodes[i].GetPrepareUniformsAndFunctions());
				if( cNodes[i].IsProperty() ) {
					string propName = cNodes[i].property.nameInternal;
					if(!(IncludeLightingCginc() && propName == "_SpecColor")) // SpecColor already defined in Lighting.cginc
						App( cNodes[i].property.GetFilteredVariableLine() );
				}
			}
		}
		void BeginSubShader() {
			App( "SubShader {" );
			scope++;
		}
		void BeginTags() {
			App( "Tags {" );
			scope++;
		}
		void BeginCG() {
			App( "CGPROGRAM" );

			if( dependencies.tessellation ) {
				App( "#pragma hull hull" );
				App( "#pragma domain domain" );
				App( "#pragma vertex tessvert" );
			} else {
				App( "#pragma vertex vert" );
			}
			App( "#pragma fragment frag" );



			switch( currentPass ) {
				case PassType.FwdBase:
					App( "#define UNITY_PASS_FORWARDBASE" );
					break;
				case PassType.FwdAdd:
					App( "#define UNITY_PASS_FORWARDADD" );
					break;
				case PassType.PrePassBase:
					App( "#define UNITY_PASS_PREPASSBASE" );
					break;
				case PassType.PrePassFinal:
					App( "#define UNITY_PASS_PREPASSFINAL" );
					break;
				case PassType.ShadColl:
					App( "#define UNITY_PASS_SHADOWCOLLECTOR" );
					App( "#define SHADOW_COLLECTOR_PASS" );
					break;
				case PassType.ShadCast:
					App( "#define UNITY_PASS_SHADOWCASTER" );
					break;
			}

			


			App( "#include \"UnityCG.cginc\"" );

			if( ShouldUseLightMacros()  )
				App( "#include \"AutoLight.cginc\"" );
			if( IncludeLightingCginc() )
				App( "#include \"Lighting.cginc\"" );
			if( dependencies.tessellation )
				App ("#include \"Tessellation.cginc\"");

			if( ps.catLighting.lightprobed ) {
				App( "#define SHOULD_SAMPLE_SH_PROBE ( defined (LIGHTMAP_OFF) && defined(DYNAMICLIGHTMAP_OFF) )" ); // TODO: Might not work properly in 4.x
			}

			if( ps.catLighting.reflectprobed || Unity5PBL() ) {
				App( "#include \"UnityUniversalBRDF.cginc\"" );
			}

			if(currentPass == PassType.PrePassFinal){
				App( "#pragma multi_compile_prepassfinal" );
			} else if( currentPass == PassType.FwdBase ){
				App( "#pragma multi_compile_fwdbase" + ps.catBlending.GetShadowPragmaIfUsed() );
			} else if( currentPass == PassType.FwdAdd ) {
				App( "#pragma multi_compile_fwdadd" + ps.catBlending.GetShadowPragmaIfUsed() );
			} else if(currentPass != PassType.PrePassBase){
				App( "#pragma fragmentoption ARB_precision_hint_fastest");
				if(currentPass == PassType.ShadColl)
					App( "#pragma multi_compile_shadowcollector" );
				else
					App( "#pragma multi_compile_shadowcaster" );
			}

			if( UseUnity5Fog() )
				App( "#pragma multi_compile_fog" );
			

			List<int> groups = new List<int>();
			foreach(SF_Node n in cNodes){
				int group;
				string[] mcPrags = n.TryGetMultiCompilePragmas(out group);
				if(!groups.Contains(group) && mcPrags != null){
					groups.Add(group);
					for(int i=0;i<mcPrags.Length;i++){
						App("#pragma multi_compile " + mcPrags[i]);
					}
				}
				// Old branching tests
				//if(n.IsProperty() && n.property is SFP_Branch){
				//	App(n.property.GetMulticompilePragma ());
				//}
			}


			
			if( dependencies.DoesExcludePlatforms() )
				App( "#pragma exclude_renderers " + dependencies.GetExcludePlatforms() );
			if( dependencies.IsTargetingAboveDefault() ){
				if( ps.catExperimental.force2point0 )
					App( "#pragma target 2.0" );
				else
					App( "#pragma target " + dependencies.GetShaderTarget() );
			}
			if( editor.nodeView.treeStatus.mipInputUsed || editor.nodeView.treeStatus.texturesInVertShader)
				App ("#pragma glsl"); // Kills non DX instruction counts
		}
		void EndCG() {
			App( "ENDCG" );
		}

		bool UseUnity5Fog() {
			return ps.catBlending.useFog && SF_Tools.CurrentUnityVersion >= 5.0;
		}

		bool UseUnity5FogInThisPass() {
			return ps.catBlending.useFog && SF_Tools.CurrentUnityVersion >= 5.0 && (currentPass == PassType.FwdBase || currentPass == PassType.Outline || currentPass == PassType.PrePassFinal) ;
		}



		void AppTag(string k, string v) {
			App( "\""+ k +"\"=\""+ v +"\"" );
		}

		void PassTags() {
			BeginTags();
			if( currentPass == PassType.FwdBase )
				AppTag( "LightMode", "ForwardBase" );
			else if( currentPass == PassType.FwdAdd )
				AppTag( "LightMode", "ForwardAdd" );
			else if( currentPass == PassType.ShadColl )
				AppTag( "LightMode", "ShadowCollector" );
			else if(currentPass == PassType.ShadCast)
				AppTag( "LightMode", "ShadowCaster" );
			else if(currentPass == PassType.PrePassBase)
				AppTag( "LightMode", "PrePassBase" );
			else if(currentPass == PassType.PrePassFinal)
				AppTag( "LightMode", "PrePassFinal" );
			End();
		}


		void SubShaderTags() {

			bool ip = ps.catBlending.ignoreProjector;
			bool doesOffset = ps.catBlending.queuePreset != Queue.Geometry || ps.catBlending.queueOffset != 0;
			bool hasRenderType = ps.catBlending.renderType != RenderType.None;

			if( !ip && !doesOffset && !hasRenderType )
				return; // No tags!

			BeginTags();
			if(ip)
				AppTag( "IgnoreProjector", "True" );
			if( doesOffset ) {
				string bse = ps.catBlending.queuePreset.ToString();
				string ofs = "";
				if( ps.catBlending.queueOffset != 0 )
					ofs = ps.catBlending.queueOffset > 0 ? ( "+" + ps.catBlending.queueOffset ) : (ps.catBlending.queueOffset.ToString()) ;
				AppTag( "Queue", ( bse + ofs ).ToString() );
			}
			if( hasRenderType )
				AppTag("RenderType",ps.catBlending.renderType.ToString());
				

				
			End();
		}

		void RenderSetup() {

			if( currentPass == PassType.FwdAdd )
				App("Blend One One");
			else if( currentPass == PassType.FwdBase && ps.catBlending.UseBlending() ) // Shadow passes and outlines use default blending
				App( ps.catBlending.GetBlendString() );
			

			if( currentPass == PassType.ShadCast ){
				App( "Cull Off" );
				App( "Offset 1, 1" );
			} else if (currentPass == PassType.Outline){
				App ("Cull Front");
			} else if( ps.catBlending.UseCulling())
				App( ps.catBlending.GetCullString() );

			if( ps.catBlending.UseDepthTest() && !IsShadowOrOutlinePass() ) // Shadow passes and outlines use default
				App( ps.catBlending.GetDepthTestString() );

			if( !IsShadowOrOutlinePass() ) {
				if(currentPass == PassType.PrePassFinal)
					App ("ZWrite Off");
				else
					App( ps.catBlending.GetZWriteString() );
			}

			App (ps.catBlending.GetOffsetString());


			// Fog was changed in Unity 5
			if( SF_Tools.CurrentUnityVersion < 5 ) {
				if( currentPass == PassType.PrePassBase ) {
					App( "Fog {Mode Off}" );
				} else if( currentPass == PassType.FwdAdd ) {
					App( "Fog { Color (0,0,0,0) }" ); // Shadow cast, Shadow collect, PrePassBase
				} else if( !ps.catBlending.useFog || !( currentPass == PassType.FwdBase || currentPass == PassType.Outline || currentPass == PassType.PrePassFinal ) ) {
					App( "Fog {Mode Off}" ); // Turn off fog is user doesn't want it
				} else {
					// Fog overrides!
					if( ps.catBlending.fogOverrideMode )
						App( "Fog {Mode " + ps.catBlending.fogMode.ToString() + "}" );
					if( ps.catBlending.fogOverrideColor )
						App( "Fog { Color (" + ps.catBlending.fogColor.r + "," + ps.catBlending.fogColor.g + "," + ps.catBlending.fogColor.b + "," + ps.catBlending.fogColor.a + ") }" );
					if( ps.catBlending.fogOverrideDensity )
						App( "Fog {Density " + ps.catBlending.fogDensity + "}" );
					if( ps.catBlending.fogOverrideRange )
						App( "Fog {Range " + ps.catBlending.fogRange.x + "," + ps.catBlending.fogRange.y + "}" );
				}
			}
			


			/*
			if( ps.catBlending.useStencilBuffer ){
				App ("Stencil {");
				scope++;

				App ( ps.catBlending.GetStencilContent() );

				scope--;
				App ("}");

			}*/

		}

		void CGvars() {

			if( dependencies.lightColor && !ps.catLighting.lightmapped && !IsShadowPass() ) // Lightmap and shadows include Lighting.cginc, which already has this
				App( "uniform float4 _LightColor0;" );

			if( currentPass == PassType.PrePassFinal ){
				App ( "uniform sampler2D _LightBuffer;" );
				App ("#if defined (SHADER_API_XBOX360) && defined (HDR_LIGHT_PREPASS_ON)");
				scope++;
				App("sampler2D _LightSpecBuffer;");
				scope--;
				App("#endif");
			}

			if(ps.catLighting.useAmbient && ( currentPass == PassType.PrePassBase || currentPass == PassType.PrePassFinal ) ){
				App ( "uniform fixed4 unity_Ambient;" );
			}

			if( dependencies.grabPass )
				App( "uniform sampler2D _GrabTexture;" );

			if( dependencies.frag_sceneDepth )
				App( "uniform sampler2D _CameraDepthTexture;");

			if( dependencies.time ) {
				//App( "uniform float4 _Time;" ); // TODO: _Time too. Maybe replace at the end?
				App( "uniform float4 _TimeEditor;" );
			}

			if( dependencies.fog_color ) {
				App( "uniform half4 unity_FogColor;" );
			}
				

			if( ps.catLighting.lightmapped ) {
				App("#ifndef LIGHTMAP_OFF");
				scope++;{
					App( "float4 unity_LightmapST;" );
					App( "sampler2D unity_Lightmap;" );
					
					if(!InDeferredPass()){
						App( "#ifndef DIRLIGHTMAP_OFF" );
						scope++;
					}
					App( "sampler2D unity_LightmapInd;" );
					if(!InDeferredPass()){
						scope--;
						App( "#endif" );
					} else {
						App ("float4 unity_LightmapFade;");
					}
				scope--;}
				App( "#endif" );
			}



			if( dependencies.reflection_probes ) {

				App("#define _GLOSSYENV");
				App( "#if !defined(SHADER_API_MOBILE) && !defined(SHADER_API_SM2) && !defined(SHADER_API_D3D11_9X)" );
				scope++;
				App("#define _GLOSSYENV_BOX_PROJECTION 1");
				scope--;
				App("#endif");

				App( "" );
				App( "#ifdef _GLOSSYENV" );
				scope++;
				App( "#ifdef _GLOSSYENV_BOX_PROJECTION" );
				scope++;
				App( "float4 unity_SpecCube_BoxMax;" );
				App( "float4 unity_SpecCube_BoxMin;" );
				App( "float4 unity_SpecCube_ProbePosition;" );
				App( "inline void BoxProject(inout float3 cubeDir, float3 worldPos, float4 probePos, float4 boxMin, float4 boxMax ){" );
				scope++;
				App( "if (probePos.w > 0.0){" );
				scope++;
				App( "half3 nrdir = normalize(cubeDir);" );
				App( "half3 rbmax = (boxMax.xyz - worldPos) / nrdir;" );
				App( "half3 rbmin = (boxMin.xyz - worldPos) / nrdir;" );
				App( "half3 rbminmax = (nrdir > 0.0f) ? rbmax : rbmin;" );
				App( "half fa = min(min(rbminmax.x, rbminmax.y), rbminmax.z);" );
				App( "float3 aabbCenter = (boxMax.xyz + boxMin.xyz) * 0.5;" );
				App( "float3 offset = aabbCenter - probePos.xyz;" );
				App( "float3 posonbox = offset + worldPos + nrdir * fa;" );
				App( "cubeDir = posonbox - aabbCenter;" );
				scope--;
				App( "}" );
				scope--;
				App( "}" );
				scope--;
				App( "#endif" );
				scope--;
				App( "#endif" );
			}




			PropertiesCG();

		}

		void InitViewDirVert() {
			if( dependencies.vert_viewDirection )
				App( "float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - mul(_Object2World, v.vertex).xyz);" );
		}	
		void InitViewDirFrag() {
			if( dependencies.frag_viewDirection )
				App( "float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);" );
		}
		void InitTangentTransformFrag() {
			if((dependencies.frag_tangentTransform && currentProgram == ShaderProgram.Frag) || (dependencies.vert_tangentTransform && currentProgram == ShaderProgram.Vert) )
				App( "float3x3 tangentTransform = float3x3( "+WithProgramPrefix("tangentDir")+", "+WithProgramPrefix("binormalDir")+", "+WithProgramPrefix("normalDir")+");" );
		}

		


		string LightmapNormalDir() {
			if( editor.mainNode.normal.IsConnectedAndEnabled() ) {
				return "normalLocal";		
			}
			return "float3(0,0,1)";	
		}

		void PrepareLightmapVars() {
			if( !LightmapThisPass() )
				return;
			App( "#ifndef LIGHTMAP_OFF" );
			scope++;
				App( "float4 lmtex = tex2D(unity_Lightmap,i.uvLM);" );
				App("#ifndef DIRLIGHTMAP_OFF");
				scope++;
					App("float3 lightmap = DecodeLightmap(lmtex);");
					App("float3 scalePerBasisVector = DecodeLightmap(tex2D(unity_LightmapInd,i.uvLM));");
					App("UNITY_DIRBASIS");
					App( "half3 normalInRnmBasis = saturate (mul (unity_DirBasis, " + LightmapNormalDir() + "));" );
					App( "lightmap *= dot (normalInRnmBasis, scalePerBasisVector);" );
				scope--;
				App("#else");
				scope++;
				App( "float3 lightmap = DecodeLightmap(lmtex);" );
				scope--;
				App("#endif");
			scope--;
			App( "#endif" );
		}

		void InitLightDir() {

			if(IsShadowPass())
				return;

			if((currentProgram == ShaderProgram.Frag && !dependencies.frag_lightDirection) || (currentProgram == ShaderProgram.Vert && !dependencies.vert_lightDirection))
				return;

			if(currentPass == PassType.FwdBase ){
				if( ps.catLighting.lightmapped ) {
					App( "#ifndef LIGHTMAP_OFF" );
					scope++;
						App( "#ifdef DIRLIGHTMAP_OFF" );
						scope++;
				}
				App( "float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);" );
				if( ps.catLighting.lightmapped ) {
						scope--;
						App( "#else" );
						scope++;
							App( "float3 lightDirection = normalize (scalePerBasisVector.x * unity_DirBasis[0] + scalePerBasisVector.y * unity_DirBasis[1] + scalePerBasisVector.z * unity_DirBasis[2]);" );
							App( "lightDirection = mul(lightDirection,tangentTransform); // Tangent to world" );
						scope--;
						App( "#endif" );
					scope--;
					App( "#else" );
					scope++;
						App( "float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);" );
					scope--;
					App( "#endif" );
				}

				return;
			}

			// Point vs directional
			App ("float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - "+WithProgramPrefix("posWorld.xyz")+",_WorldSpaceLightPos0.w));");
			
		}

		void InitHalfVector() {
			if( (!dependencies.frag_halfDirection && currentProgram == ShaderProgram.Frag) || (!dependencies.vert_halfDirection && currentProgram == ShaderProgram.Vert) )
				return;
			App( "float3 halfDirection = normalize(viewDirection+lightDirection);" );
		}

		void InitAttenuation() {

			if(SF_Evaluator.inVert && ps.catLighting.IsVertexLit() && ShouldUseLightMacros())
				App( "TRANSFER_VERTEX_TO_FRAGMENT(o)" );

			string atten = "LIGHT_ATTENUATION(" + ((currentProgram == ShaderProgram.Frag) ? "i" : "o") + ")";
			if( ps.catLighting.doubleIncomingLight )
				atten += "*2";

			string inner = ( ShouldUseLightMacros() ? atten : "1" );
			App( "float attenuation = " + inner + ";" );
			if(ps.catLighting.lightMode != SFPSC_Lighting.LightMode.Unlit)
				App( "float3 attenColor = attenuation * _LightColor0.xyz;" );
		}


		string GetWithDiffPow(string s){
			if(ps.HasDiffusePower()){
				return "pow(" + s + ", " + ps.n_diffusePower + ")";
			}
			return s;
		}



		void CalcDiffuse() {

			//App( "float atten = 1.0;" );
			AppDebug("Diffuse");



			//InitAttenuation();


			string lmbStr;

			

			



			if( ps.HasTransmission() || ps.HasLightWrapping() ) {

				if( !InDeferredPass() ) {
					if( !ps.HasSpecular() ) {
						App( "float NdotL = dot( " + VarNormalDir() + ", lightDirection );" );
					} else {
						App( "NdotL = dot( " + VarNormalDir() + ", lightDirection );" );
					}
				}

				string fwdLight = "float3 forwardLight = "; // TODO
				string backLight = "float3 backLight = "; // TODO


				if(ps.HasLightWrapping()){
					App( "float3 w = " + ps.n_lightWrap + "*0.5; // Light wrapping" );
					App( "float3 NdotLWrap = NdotL * ( 1.0 - w );" );
					App( fwdLight + GetWithDiffPow("max(float3(0.0,0.0,0.0), NdotLWrap + w )") + ";" );
					if(ps.HasTransmission()){
						App( backLight + GetWithDiffPow("max(float3(0.0,0.0,0.0), -NdotLWrap + w )") + " * " + ps.n_transmission + ";" );
					}
						
				} else {
					App( fwdLight + GetWithDiffPow("max(0.0, NdotL )") + ";" );
					if(ps.HasTransmission()){
						App( backLight + GetWithDiffPow( "max(0.0, -NdotL )") + " * " + ps.n_transmission + ";" );
					}
				}

				lmbStr = "forwardLight";

				if(ps.HasTransmission()){
					lmbStr += "+backLight";
					lmbStr = "("+lmbStr+")";
				}

			} else if(currentPass == PassType.PrePassFinal){
				lmbStr = "";
			} else {

				if( Unity5PBL() ) {
					if( ps.HasTransmission() || ps.HasLightWrapping())
						App( "NdotL = max(0.0,NdotL);" );
					App("float LdotH = max(0.0,dot(lightDirection, halfDirection));");
					App("half fd90 = 0.5 + 2 * LdotH * LdotH * (1-gloss);");
					lmbStr = "((1 +(fd90 - 1)*pow((1.00001-NdotL), 5)) * (1 + (fd90 - 1)*pow((1.00001-NdotV), 5)) * NdotL)";
				} else {
					lmbStr = GetWithDiffPow( "max( 0.0, NdotL)" );
				}


				
			}

			if( ps.catLighting.IsEnergyConserving() && currentPass != PassType.PrePassFinal ) {
				if( ps.HasLightWrapping() ) {
					lmbStr += "/(Pi*(dot(w,float3(0.3,0.59,0.11))+1))";
				} else {
					if( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL && SF_Tools.CurrentUnityVersion >= 5f ) {
						// Don't!
					} else {
						lmbStr += "*InvPi";
					}
					
				}
			}

			if( currentPass == PassType.PrePassFinal )
				lmbStr = "float3 directDiffuse = lightAccumulation.rgb" + ( ps.catLighting.doubleIncomingLight ? "" : " * 0.5" );
			else
				lmbStr = "float3 directDiffuse = " + lmbStr + " * attenColor";



			bool ambDiff = ps.mOut.ambientDiffuse.IsConnectedEnabledAndAvailableInThisPass( currentPass );
			bool shLight = DoPassSphericalHarmonics();
			bool diffAO = ps.mOut.diffuseOcclusion.IsConnectedEnabledAndAvailableInThisPass( currentPass );
			bool ambLight = ps.catLighting.useAmbient && ( currentPass == PassType.FwdBase || currentPass == PassType.PrePassFinal ) && !ps.catLighting.lightprobed; // Ambient is already in light probe data

			bool hasIndirectLight = ambDiff || shLight || ambLight; // TODO: Missing lightmaps




			if( hasIndirectLight ) {
				App( "float3 indirectDiffuse = float3(0,0,0);" );
			}

			lmbStr += ";";
			App( lmbStr );




			if( LightmapThisPass() ) {
				App( "#ifndef LIGHTMAP_OFF" );
				scope++;
				if(InDeferredPass()){
					App( "directDiffuse += lightAccumulation.rgb + lightmapAccumulation.rgb;" );
				} else {
					App( "directDiffuse += lightmap.rgb;" ); // TODO: Auto-light too!
				}
				scope--;
				App( "#endif" );
				//scope++;
			}


			




			


			// Direct light done, now let's do indirect light


			if( hasIndirectLight ) {
				//App( " indirectDiffuse = float3(0,0,0);" );

				if( ambLight )
					App( "indirectDiffuse += " + GetAmbientStr() + "; // Ambient Light" );
				if( ambDiff )
					App( "indirectDiffuse += " + ps.n_ambientDiffuse + "; // Diffuse Ambient Light" );

				if( shLight ) {


					App( "#ifdef SHOULD_SAMPLE_SH_PROBE" );
					scope++;
					if( ps.catQuality.highQualityLightProbes )
						App( "indirectDiffuse += ShadeSH9(float4(normalDirection,1))" + ( ps.catLighting.doubleIncomingLight ? ";" : " * 0.5; // Per-Pixel Light Probes / Spherical harmonics" ) );
					else
						App( "indirectDiffuse += i.shLight; // Per-Vertex Light Probes / Spherical harmonics" );
					scope--;
					App( "#endif" );

				}

				// Diffuse AO
				if( diffAO ) {
					App( "indirectDiffuse *= " + ps.n_diffuseOcclusion + "; // Diffuse AO" );
				}


			}


			//if( LightmapThisPass() ) {
			//	scope--;
				//App( "#endif" );
		//	}

			if( hasIndirectLight ) {
				App( "float3 diffuse = (directDiffuse + indirectDiffuse) * " + ps.n_diffuse + ";" );
			} else {
				App( "float3 diffuse = directDiffuse * " + ps.n_diffuse + ";" );
			}


			// To make diffuse/spec tradeoff better
			if( DoPassDiffuse() && DoPassSpecular() ) {
				if( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL ) {
					App( "diffuse *= 1-specularMonochrome;" );
				} else if( ps.catLighting.energyConserving ) {
					App( "diffuse *= 1-specularMonochrome;" );
				}
			}


		}

		bool LightmapThisPass() {
			return ps.catLighting.lightmapped && (currentPass == PassType.FwdBase || currentPass == PassType.PrePassFinal);
		}

		void InitNormalDirVert() {
			if( dependencies.vert_out_normals ) {
				App( "o.normalDir = mul(_Object2World, float4(" + ps.catBlending.GetNormalSign() + "v.normal,0)).xyz;" );
			}
		}

		void InitTangentDirVert() {
			App( "o.tangentDir = normalize( mul( _Object2World, float4( v.tangent.xyz, 0.0 ) ).xyz );" );
		}

		void InitBinormalDirVert() {
			App( "o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);" );
		}

		void InitObjectPos() {
			if(dependencies.frag_objectPos || dependencies.vert_objectPos)
				App( "float4 objPos = mul ( _Object2World, float4(0,0,0,1) );" );
		}
		void InitObjectScale() {
			if( dependencies.objectScaleReciprocal || dependencies.objectScale)
				App( "float3 recipObjScale = float3( length(_World2Object[0].xyz), length(_World2Object[1].xyz), length(_World2Object[2].xyz) );" );
			if( dependencies.objectScale )
				App( "float3 objScale = 1.0/recipObjScale;" );
		}

		void InitNormalDirFrag() {

			if(IsShadowOrOutlinePass())
				return;

			if( (!dependencies.frag_normalDirection && currentProgram == ShaderProgram.Frag) )
				return;
			
			


			//if(ps.normalQuality == SF_PassSettings.NormalQuality.Normalized){
			//	App ("i.normalDir = normalize(i.normalDir);");
			//}

			


			if( ps.HasNormalMap() ) {
				App( "float3 normalLocal = " + ps.n_normals + ";" );
				App( "float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals" );
			} else {
				App( "float3 normalDirection = i.normalDir;" );
			}

			if( ps.catBlending.IsDoubleSided() ) {
				App( "" );
				App( "float nSign = sign( dot( viewDirection, i.normalDir ) ); // Reverse normal if this is a backface" );
				App( "i.normalDir *= nSign;" );
				App( "normalDirection *= nSign;" );
				App( "" );
			}



		}


		void CalcGloss(){
			AppDebug("Gloss");
			App( "float gloss = " + ps.n_gloss + ";");
			if( ps.catLighting.remapGlossExponentially ) {
				App( "float specPow = exp2( gloss * 10.0+1.0);" );
			} else {
				App( "float specPow = gloss;" );
			}
		}

		bool DoAmbientSpecThisPass(){
			return (mOut.ambientSpecular.IsConnectedEnabledAndAvailable() && ( currentPass == PassType.FwdBase || currentPass == PassType.PrePassFinal));
		}


		void CalcSpecular() {



			AppDebug("Specular");

			if( currentPass != PassType.PrePassFinal ) {
				App( "float NdotL = max(0, dot( " + VarNormalDir() + ", lightDirection ));" );
			}


			//if(DoAmbientSpecThisPass() && ps.IsPBL())
				//App ("float NdotR = max(0, dot(viewReflectDirection, normalDirection));"); // WIP

			string directSpecular = "float3 directSpecular = ";

			string attColStr;
			if(ps.catLighting.maskedSpec && currentPass == PassType.FwdBase){
				attColStr = "(floor(attenuation) * _LightColor0.xyz)";
			}else{
				attColStr = "attenColor";
			}








/*
 * float3 specular = pow(max(0.0,dot(halfDirection, normalDirection)),specPow) * specularColor;
				#ifndef LIGHTMAP_OFF
					#ifndef DIRLIGHTMAP_OFF
						specular *= lightmap;
					#else
						specular *= floor(attenuation) * _LightColor0.xyz;
					#endif
				#else
					specular = floor(attenuation) * _LightColor0.xyz;
				#endif
 * */

			if(currentPass == PassType.PrePassFinal){
				if(ps.catLighting.useAmbient)
					directSpecular += "(lightAccumulation.rgb * 2)*lightAccumulation.a";
				else
					directSpecular += "lightAccumulation.rgb*lightAccumulation.a";
			} else if(!(currentPass == PassType.FwdBase && ps.catLighting.lightmapped)){
				directSpecular += attColStr; /* * " + ps.n_specular;*/ // TODO: Doesn't this double the spec? Removed for now. Shouldn't evaluate spec twice when using PBL
			} else {
				directSpecular += "1";
			}




			//if( mOut.ambientSpecular.IsConnectedEnabledAndAvailable() && currentPass == PassType.FwdBase){
			//	s += "(attenColor + " + ps.n_ambientSpecular + ")";
			//} else {
			//	s += "attenColor";
			//}



			bool occluded = ps.mOut.specularOcclusion.IsConnectedEnabledAndAvailableInThisPass( currentPass );
			bool ambSpec = DoAmbientSpecThisPass();
			bool reflProbed = dependencies.reflection_probes;
			bool hasIndirectSpecular = ambSpec || reflProbed;
			string indirectSpecular = "";

			if( hasIndirectSpecular ) {

				if(occluded){
					App("float3 specularAO = " + ps.n_specularOcclusion + ";");
				}

				indirectSpecular = "float3 indirectSpecular = ";

				if( reflProbed ) {
					App("float3 reflectionProbes = float3(0,0,0);");
					App("#ifdef _GLOSSYENV");
					scope++;
						App( "half3 cubeDir = viewReflectDirection;" );
						App("#ifdef _GLOSSYENV_BOX_PROJECTION");
						scope++;
							App("BoxProject(cubeDir, i.posWorld, unity_SpecCube_ProbePosition, unity_SpecCube_BoxMin, unity_SpecCube_BoxMax );");
						scope--;
						App("#endif");
						App( "reflectionProbes = Unity_GlossyEnvironment (cubeDir, 1-gloss);" );
					scope--;
					App("#endif");
				}

				indirectSpecular += "(reflectionProbes";

				if( ambSpec ) {
					indirectSpecular += " + " + ps.n_ambientSpecular + ")";
				} else {
					indirectSpecular += ")";
				}

				if( occluded ) {
					indirectSpecular += " * specularAO";
				}

			}



			if( ps.catLighting.IsPBL() ) {
				//s += "*NdotL"; // TODO: Really? Is this the cosine part?

				//if(DoAmbientSpecThisPass())
					//sAmb += " * NdotR";

			}
			
			if(!InDeferredPass()){
				if( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.Phong )
					directSpecular += " * pow(max(0,dot(reflect(-lightDirection, " + VarNormalDir() + "),viewDirection))";
				if( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.BlinnPhong || ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL ) {
					directSpecular += " * pow(max(0,dot(halfDirection," + VarNormalDir() + "))";
				}
				directSpecular += ",specPow)";
			}

			bool initialized_NdotV = false;
			bool initialized_NdotH = false;
			bool initialized_VdotH = false;

			App( "float3 specularColor = " + ps.n_specular + ";" );
			if((ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL || ps.catLighting.energyConserving) && DoPassDiffuse() && DoPassSpecular())
				App ("float specularMonochrome = dot(specularColor,float3(0.3,0.59,0.11));");


			string specularPBL = "";

			// PBL SHADING, normalization term comes after this
			if( ps.catLighting.IsPBL() ) {

				
				
				// FRESNEL TERM
				//App( "float3 specularColor = " + ps.n_specular + ";" );
				if( ps.catLighting.fresnelTerm ) {

					if( SF_Tools.CurrentUnityVersion >= 5f ) {
						if( !initialized_VdotH ) {
							App( "float VdotH = max(0.0,dot( viewDirection, halfDirection ));" );
							initialized_VdotH = true;
						}

						App( "float fresnelTerm = FresnelTerm(specularMonochrome, VdotH);" );

						specularPBL += "*fresnelTerm";

					} else {
						App( "float HdotL = max(0.0,dot(halfDirection,lightDirection));" );
						string fTermDef = "float3 fresnelTerm = specularColor + ( 1.0 - specularColor ) * pow((1.0 - HdotL),5);";
						App( fTermDef );
						directSpecular += "*fresnelTerm";
					}

					
					

					// TODO: U5 PBL
					if( hasIndirectSpecular && SF_Tools.CurrentUnityVersion < 5f ) {
						if( !initialized_NdotV ){
							App( "float NdotV = max(0.0,dot( "+VarNormalDir()+", viewDirection ));" );
							initialized_NdotV = true;
						}
						//App (fTermDef.Replace("HdotL","NdotV").Replace("fresnelTerm","fresnelTermAmb"));

						string fTermAmbDef = "float3 fresnelTermAmb = specularColor + ( 1.0 - specularColor ) * ( pow((1.0 - NdotV),5) / (4-3*gloss) );";
						App( fTermAmbDef );

						indirectSpecular += " * fresnelTermAmb";
					}

				} else {
					directSpecular += "*specularColor";
				}

				
				// VISIBILITY TERM / GEOMETRIC TERM?
				if( ps.catLighting.visibilityTerm ) {

					if( !initialized_NdotV ) {
						App( "float NdotV = max(0.0,dot( " + VarNormalDir() + ", viewDirection ));" );
						initialized_NdotV = true;
					}

					if( SF_Tools.CurrentUnityVersion >= 5 ) {
						if( !initialized_NdotH ) {
							App( "float NdotH = max(0.0,dot( " + VarNormalDir() + ", halfDirection ));" );
							initialized_NdotH = true;
						}
						if( !initialized_VdotH ) {
							App( "float VdotH = max(0.0,dot( viewDirection, halfDirection ));" );
							initialized_VdotH = true;
						}
							
						App( "float visTerm = GeometricTerm( NdotL, NdotH, NdotV, VdotH );" );

						specularPBL += "*visTerm";

					} else {
						App( "float alpha = 1.0 / ( sqrt( (Pi/4.0) * specPow + Pi/2.0 ) );" );
						string vTermDef = "float visTerm = ( NdotL * ( 1.0 - alpha ) + alpha ) * ( NdotV * ( 1.0 - alpha ) + alpha );";
						App( vTermDef );
						App( "visTerm = 1.0 / visTerm;" );
						directSpecular += "*visTerm";
						
					}

					
				}


				
			} else {
				//sAmb += " * specularColor";
				//directSpecular += " * specularColor";
			}
			
			
			
			if( ps.catLighting.IsEnergyConserving() ) {
				// NORMALIZATION TERM
				if( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.Phong ){
					App( "float normTerm = (specPow + 2.0 ) / (2.0 * Pi);" );
					directSpecular += "*normTerm";
				} else if(currentPass == ShaderForge.PassType.PrePassFinal){
					App( "float specPow = max( 2, " + ps.n_gloss + " * 128 );" );
					App( "float normTerm = (specPow + 8.0 ) / (8.0 * Pi);" );
					directSpecular += "*normTerm";
				} else if( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.BlinnPhong || ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL ) {
					if( Unity5PBL() ) {

						if( !initialized_NdotH ) {
							App( "float NdotH = max(0.0,dot( " + VarNormalDir() + ", halfDirection ));" );
							initialized_NdotH = true;
						}

						App( "float normTerm = max(0.0, BlinnPhongNormalizedTerm (NdotH, RoughnessToSpecPower (1.0-gloss)));" );
						specularPBL += "*normTerm";
					} else {
						App( "float normTerm = (specPow + 8.0 ) / (8.0 * Pi);" );
						directSpecular += "*normTerm";
					}
					
				}

				if(DoAmbientSpecThisPass()){
					//sAmb += " * normTerm";
				}

				


			}






			if( Unity5PBL() ) {

				if( !initialized_NdotV ) {
					App( "float NdotV = max(0.0,dot( " + VarNormalDir() + ", viewDirection ));" );
					initialized_NdotV = true;
				}

				specularPBL = specularPBL.Substring( 1 ); // Remove first * symbol
				specularPBL = "float specularPBL = max(0, (" + specularPBL + ") / (4 * NdotV + 1e-5f) );";


				App( specularPBL );
			}

			

			if( ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL ) {
				directSpecular += "*specularPBL";
			}

			directSpecular += ";";
			App( directSpecular );

			string specular = "";

			if( hasIndirectSpecular ) {
				App( indirectSpecular + ";" );
				specular = "float3 specular = (directSpecular + indirectSpecular) * specularColor;";
			} else {
				specular = "float3 specular = directSpecular * specularColor;";
			}

			App(specular); // Specular


			/*
			 *float3 specular = lightAccumulation.a * specularColor; 
				
				#ifndef LIGHTMAP_OFF
					#ifndef DIRLIGHTMAP_OFF
						specular += specColor;
					#endif
				#endif 
			 */

			if(ps.catLighting.lightmapped){
				if(currentPass == PassType.FwdBase){
					App ("#ifndef LIGHTMAP_OFF");
					scope++;
					{
						App ("#ifndef DIRLIGHTMAP_OFF");
						scope++;
						{
							App ("specular *= lightmap;");
						}
						scope--;
						App ("#else");
						scope++;
						{
							App ("specular *= "+attColStr+";");
						}
						scope--;
						App("#endif");
					}
					scope--;
					App ("#else");
					scope++;
					{
						App ("specular *= "+attColStr+";");
					}
					scope--;
					App("#endif");
				} else if(currentPass == PassType.PrePassFinal){


					App ("#ifndef LIGHTMAP_OFF");
					scope++;
					{
						App ("#ifndef DIRLIGHTMAP_OFF");
						scope++;
						{
							App ("specular += specColor;");
						}
						scope--;
						App("#endif");
					}
					scope--;
					App("#endif");


				}

			}





		}
		
		// Spec & emissive
		/*
		void CalcAddedLight() {


			// No added light unless we're using spec or emissive
			if( !ps.HasSpecular() && !ps.HasEmissive() )
				return;

			AppDebug("CalcAddedLight()");

			string s = "";
			//if( ps.HasSpecular() || ps.HasEmissive() && currentPass == PassType.FwdBase )

			if( ps.HasSpecular() ) {

				CalcGloss();
				CalcSpecular();

				if( ps.HasEmissive() && currentPass == PassType.FwdBase )
					s += " + " + ps.n_emissive;

				s += ";";

			} else if( ps.HasEmissive() && currentPass == PassType.FwdBase ) {
				s = "float3 addLight = ";
				s += ps.n_emissive + ";";
			}

			App( s );
		}
		*/


		bool Unity5PBL() {
			return (SF_Tools.CurrentUnityVersion >= 5f && ps.catLighting.lightMode == SFPSC_Lighting.LightMode.PBL);
		}


		void CalcEmissive(){
			AppDebug("Emissive");
			App ("float3 emissive = " + ps.n_emissive + ";");
		}

		bool DoPassLightAccumulation(){
			return (currentPass == PassType.PrePassFinal && ( DoPassDiffuse() || DoPassSpecular() ));
		}

		bool DoPassDiffuse(){
			return ps.HasDiffuse() && (currentPass == PassType.FwdBase || currentPass == PassType.FwdAdd || currentPass == PassType.PrePassFinal);
		}
		bool DoPassEmissive(){ // Emissive should always be in the base pass
			return ps.HasEmissive() && ( currentPass == PassType.FwdBase || currentPass == PassType.PrePassFinal);
		}
		bool DoPassSpecular(){ // Spec only in base and add passes
			return ps.catLighting.HasSpecular() && (currentPass == PassType.FwdBase || currentPass == PassType.FwdAdd || currentPass == PassType.PrePassFinal);
		}



		void CalcFinalLight() {
			//bool addedOnce = false;
			string finalLightStr = "float3 lightFinal = ";
			if( ps.catLighting.IsLit() ) {
				finalLightStr += "diffuse";
				if( ps.catLighting.useAmbient && currentPass == PassType.FwdBase ) {
					finalLightStr += " + UNITY_LIGHTMODEL_AMBIENT.xyz";
					if( ps.catLighting.doubleIncomingLight ) {
						finalLightStr += "*2";
					}
				}
			}

			finalLightStr += ";";
			App( finalLightStr );
			
		}




		void AppFinalOutput(string color, string alpha) {

			string rgbaValue;
			if( ps.HasRefraction() && currentPass == PassType.FwdBase ) {
				rgbaValue = "fixed4(lerp(sceneColor.rgb, " + color + "," + alpha + "),1)";
			} else {
				rgbaValue = "fixed4(" + color + "," + alpha + ")";
			}

			if( UseUnity5FogInThisPass() ) {
				App( "fixed4 finalRGBA = " + rgbaValue + ";" );
				if( ps.catBlending.fogOverrideColor ) {
					App( "UNITY_APPLY_FOG_COLOR(i.fogCoord, finalRGBA, " + GetFogColorAsFixed3Value() + ");" );
				} else {
					App( "UNITY_APPLY_FOG(i.fogCoord, finalRGBA);" );
				}
				App( "return finalRGBA;" );
			} else {
				App( "return " + rgbaValue + ";");
			}
			
		}

		string GetFogColorAsFixed3Value() {
			Color c = ps.catBlending.fogColor;
			return "fixed4(" + c.r + "," + c.g + "," + c.b + "," + c.a + ")";
		}


		string GetAmbientStr(){
			string s;
			if(InDeferredPass())
				s = "unity_Ambient.rgb";
			else
				s = "UNITY_LIGHTMODEL_AMBIENT.rgb";


			if(InDeferredPass()){
				if(!ps.catLighting.doubleIncomingLight){
					s += "*0.5";
				}
			} else if(ps.catLighting.doubleIncomingLight){
				s += "*2";
			}





			return s;

		}


		bool DoPassSphericalHarmonics(){
			return DoPassDiffuse() && ps.catLighting.lightprobed && ( currentPass == PassType.FwdBase || currentPass == PassType.PrePassFinal);
		}

		bool InDeferredPass(){
			return currentPass == PassType.PrePassFinal || currentPass == PassType.PrePassBase;
		}


		void Lighting() {

			if( IsShadowOrOutlinePass() || currentPass == PassType.PrePassBase )
				return;
			AppDebug ("Lighting");

			/*
			if( ps.IsVertexLit() && SF_Evaluator.inFrag ) {
				string finalLightStr = "float3 lightFinal = i.vtxLight";

				if(DoPassDiffuse())
					finalLightStr += " * " + ps.n_diffuse; // TODO: Not ideal, affects both spec and diffuse

				finalLightStr += ";";
				App( finalLightStr ); // TODO: Emissive and other frag effects? TODO: Separate vtx spec and vtx diffuse
				return;
			}
			*/

			bool attenBuiltin = ps.catLighting.IsLit() && ( ps.HasDiffuse() || ps.catLighting.HasSpecular() ) && currentPass != PassType.PrePassFinal;

			if( attenBuiltin || (dependencies.frag_attenuation && SF_Evaluator.inFrag))
				InitAttenuation();

			if( !ps.catLighting.IsLit() && SF_Evaluator.inFrag ) {


				string s = "float3 finalColor = ";



				//bool doAmbient = (currentPass == ShaderForge.PassType.FwdBase && ps.useAmbient);
				bool doEmissive = DoPassEmissive();
				bool doCustomLight = mOut.customLighting.IsConnectedEnabledAndAvailable();

				bool didAddLight = /*doAmbient || */doEmissive || doCustomLight;

				bool somethingAdded = false;
				//if( doAmbient ){
				//	s += somethingAdded ? " + ":"";
				//	s += GetAmbientStr();
				//	somethingAdded = true;
				//}
				if( doEmissive ){
					CalcEmissive();
					s += somethingAdded ? " + ":"";
					s += "emissive";
					somethingAdded = true;
				}
				if( doCustomLight ){
					s += somethingAdded ? " + ":"";
					s += ps.n_customLighting;
					somethingAdded = true;
				}



				if(!didAddLight)
					s += "0"; // TODO: Don't do lighting at all if this is the case


				s += ";";
	
				App( s );

				//if( ps.useAmbient && currentPass == PassType.FwdBase )
				//	App( "float3 lightFinal = " + ps.n_emissive + "+UNITY_LIGHTMODEL_AMBIENT.xyz;"); // TODO; THIS IS SUPER WEIRD
				//else
				//	App( "float3 lightFinal = " + ps.n_emissive + ";"); // Kinda weird, but emissive = light when unlit is on, so it's needed in additional passes too
				return;

			}



			// Else if frag light...

			//InitLightDir();

			//if(SF_Evaluator.inFrag)

			if( DoPassLightAccumulation() ){
				App ("half4 lightAccumulation = tex2Dproj(_LightBuffer, UNITY_PROJ_COORD(i.projPos));");
				App("#if defined (SHADER_API_GLES) || defined (SHADER_API_GLES3)");
				scope++;
				App("lightAccumulation = max(lightAccumulation, half4(0.001));");
				scope--;
				App("#endif");

				App( "#ifndef HDR_LIGHT_PREPASS_ON");
				scope++;
				App("lightAccumulation = -log2(lightAccumulation);");
				scope--;
				App("#endif");

				App("#if defined (SHADER_API_XBOX360) && defined (HDR_LIGHT_PREPASS_ON)");
				scope++;
				App("lightAccumulation.w = tex2Dproj (_LightSpecBuffer, UNITY_PROJ_COORD(i.projPos)).r;");
				scope--;
				App("#endif");

				if(ps.catLighting.lightmapped){
					App("#ifndef LIGHTMAP_OFF");
					scope++;{
						App("half3 lightmapAccumulation = half3(0,0,0);");
						App("#ifdef DIRLIGHTMAP_OFF");
						scope++;{
							App("half lmFade = length (i.lmapFadePos) * unity_LightmapFade.z + unity_LightmapFade.w;");
							App("half3 lmFull = DecodeLightmap (tex2D(unity_Lightmap, i.uvLM));");
							App("half3 lmIndirect = DecodeLightmap (tex2D(unity_LightmapInd, i.uvLM));");
							App("half3 lm = lerp (lmIndirect, lmFull, saturate(lmFade));");
							App("lightmapAccumulation.rgb += lm;");
						scope--;}
						App("#else");
						scope++;{
							//App("fixed4 lmtex = tex2D(unity_Lightmap, i.uvLM);");
							App("fixed4 lmIndTex = tex2D(unity_LightmapInd, i.uvLM);");
							// half4 lmAdd = LightingBlinnPhong_DirLightmap(o, lmtex, lmIndTex, normalize(half3(IN.viewDir)), 1, specColor);
							//App("UNITY_DIRBASIS");
							App("half3 scalePerBasisVectorDiffuse;");
							string normalStr = ps.HasNormalMap() ? "normalLocal" : "half3(0,0,1)";
							App("half3 lm = DirLightmapDiffuse (unity_DirBasis, lmtex, lmIndTex, "+normalStr+", 1, scalePerBasisVectorDiffuse);");
							App("half3 lightDir = normalize (scalePerBasisVectorDiffuse.x * unity_DirBasis[0] + scalePerBasisVectorDiffuse.y * unity_DirBasis[1] + scalePerBasisVectorDiffuse.z * unity_DirBasis[2]);");
							App("lightDir = mul(lightDir, tangentTransform);");
							App("half3 h = normalize (lightDir + viewDirection);");
							App("float nh = max (0, dot (normalDirection, h));");
							App("float lmspec = pow (nh, " + ps.n_gloss + " * 128.0);"); // TODO: Do SF encoding instead? Or, will beast not like it?
							App("half3 specColor = lm * " + ps.n_specular + " * lmspec;");
							App("lightmapAccumulation += half4(lm + specColor, lmspec);");

						scope--;}
						App("#endif");
					scope--;}
					App("#endif");
				}


				//App ("finalColor +;");




			}

			if(DoPassDiffuse() || DoPassSpecular()){
				if( ps.catLighting.IsEnergyConserving() ) {
					App( "float Pi = 3.141592654;" );
					App( "float InvPi = 0.31830988618;" );
				}
			}

			

			

			if( DoPassSpecular() ) { // Specular
				if(!InDeferredPass())
					CalcGloss();
				CalcSpecular();
				//AppDebug("Spec done"); 
			}

			if( DoPassDiffuse() ) // Diffuse + texture (If not vertex lit)
				CalcDiffuse();

			if( DoPassEmissive() ) // Emissive
				CalcEmissive();
			
			/*if(!ps.IsLit() && ps.mOut.customLighting.IsConnectedEnabledAndAvailable() ){

				App("float3 lightFinal = " + ps.n_customLighting );

			}*/
			if( /*!ps.IsVertexLit() &&*/ currentProgram == ShaderProgram.Frag ) {

				AppDebug( "Final Color" );

				bool fresnelIndirectPBL =
					Unity5PBL() &&
					ps.catLighting.fresnelTerm &&
					(ps.catLighting.reflectprobed || ps.HasAmbientSpecular() )
				;

				if( fresnelIndirectPBL ) {
					App( "float3 indirectFresnelPBL = indirectSpecular*(1-specularMonochrome)*gloss*FresnelTerm(0,NdotV);" );
				}



				string s = SumString(
					new bool[] { DoPassDiffuse(), DoPassSpecular(), DoPassEmissive(), fresnelIndirectPBL },
					new string[] { "diffuse", "specular", "emissive", "indirectFresnelPBL" },
					"0"
				);

				App( "float3 finalColor = " + s + ";" );
			}

		}

		string SumString(bool[] bools, string[] strings, string defStr ) {

			int validCount = 0;
			for(int i=0;i<bools.Length;i++){
				if( bools[i] )
					validCount++;
			}

			if( validCount == 0 )
				return defStr;

			string s = "";
			int added = 0;
			for( int i = 0; i < strings.Length; i++ ) {
				if( bools[i] ) {
					s += strings[i];
					added++;
					if( added < validCount )
						s += " + ";
				}
			}
			return s;
		}

		void InitReflectionDir() {
			if( (!dependencies.frag_viewReflection && currentProgram == ShaderProgram.Frag) || (!dependencies.vert_viewReflection && currentProgram == ShaderProgram.Vert) )
				return;
			App( "float3 viewReflectDirection = reflect( -"+VarViewDir()+", "+VarNormalDir()+" );" );
		}

		void InitSceneColorAndDepth(){

			//if(dependencies.frag_pixelDepth){
			//	App ("float pixelDepth = mul( UNITY_MATRIX_V, float4((_WorldSpaceCameraPos.rgb-i.posWorld.rgb), 0) ).b - _ProjectionParams.g");
			//}

			if(dependencies.frag_sceneDepth){
				App("float sceneZ = max(0,LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.g);");
			}
			if(dependencies.frag_pixelDepth){
				App("float partZ = max(0,i.projPos.z - _ProjectionParams.g);");
			}


			if(dependencies.scene_uvs){
				string sUv = "float2 sceneUVs = ";
				
				
				if(ps.HasRefraction() ){
					sUv += "float2(1,grabSign)*i.screenPos.xy*0.5+0.5 + " + ps.n_distortion + ";";
				} else {
					sUv += "float2(1,grabSign)*i.screenPos.xy*0.5+0.5;";
				}

				App (sUv);
			}


			if(dependencies.grabPass){

				string s = "float4 sceneColor = ";
				s += "tex2D(_GrabTexture, sceneUVs);";
				App (s);
			}





		}


		string VarNormalDir(){
			if(currentProgram == ShaderProgram.Vert)
				return "o.normalDir";
			return "normalDirection";
		}

		string VarViewDir(){ // TODO: Define view variable, dependency etc
			if(currentProgram == ShaderProgram.Vert)
				return "normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz)";
			return "viewDirection";
		}

		












		void VertexInputStruct() {
			App( "struct VertexInput {" );
			scope++;
			App( "float4 vertex : POSITION;" );
			CommonVertexData();
			scope--;
			App( "};" );
		}

		void CommonVertexData() {
			if( dependencies.vert_in_normals )
				App( "float3 normal : NORMAL;" );
			if( dependencies.vert_in_tangents )
				App( "float4 tangent : TANGENT;" );
			if( dependencies.uv0 )
				App( "float2 texcoord0 : TEXCOORD0;" );
			if( dependencies.uv1 )
				App( "float2 texcoord1 : TEXCOORD1;" );
			if( dependencies.vert_in_vertexColor )
				App( "float4 vertexColor : COLOR;" );
		}

		void TransferCommonData() {
			App( "o.vertex = v.vertex;" );
			if( dependencies.vert_in_normals )
				App( "o.normal = v.normal;" );
			if( dependencies.vert_in_tangents )
				App( "o.tangent = v.tangent;" );
			if( inTess ) {
				if( dependencies.uv0 )
					App( "o.texcoord0 = v.texcoord0;" );
				if( dependencies.uv1 )
					App( "o.texcoord1 = v.texcoord1;" );
			} else {
				if( dependencies.uv0 )
					App( "o.uv0 = v.texcoord0;" );
				if( dependencies.uv1 )
					App( "o.uv1 = v.texcoord1;" );
			}
			
			if( dependencies.vert_in_vertexColor )
				App( "o.vertexColor = v.vertexColor;" );
		}


		public string GetVertOutTexcoord(bool numberOnly = false) {
			if( numberOnly )
				return dependencies.GetVertOutTexcoord();
			return ( " : TEXCOORD" + dependencies.GetVertOutTexcoord() + ";" );
		}

		void VertexOutputStruct() {
			App( "struct VertexOutput {" );
			scope++;
			{
				if( currentPass == PassType.ShadColl ) {
					App("V2F_SHADOW_COLLECTOR;");
					dependencies.IncrementTexCoord( 5 );
				} else if( currentPass == PassType.ShadCast ) {
					App( "V2F_SHADOW_CASTER;" );
					dependencies.IncrementTexCoord( 1 );
				} else {
					App( "float4 pos : SV_POSITION;" ); // Already included in shadow passes
				}

				if( ps.catLighting.IsVertexLit() )
					App( "float3 vtxLight : COLOR;" );
				//if( DoPassSphericalHarmonics() && !ps.highQualityLightProbes )
				//	App ("float3 shLight" + GetVertOutTexcoord() );
				if( dependencies.uv0_frag )
					App( "float2 uv0" + GetVertOutTexcoord() );
				if( dependencies.uv1_frag )
					App( "float2 uv1" + GetVertOutTexcoord() );
				if( dependencies.vert_out_worldPos )
					App( "float4 posWorld" + GetVertOutTexcoord() );
				if( dependencies.vert_out_normals )
					App( "float3 normalDir" + GetVertOutTexcoord() );
				if( dependencies.vert_out_tangents )
					App( "float3 tangentDir" + GetVertOutTexcoord() );
				if( dependencies.vert_out_binormals )
					App( "float3 binormalDir" + GetVertOutTexcoord() );
				if( dependencies.vert_out_screenPos )
					App( "float4 screenPos" + GetVertOutTexcoord() );
				if( dependencies.vert_in_vertexColor )
					App( "float4 vertexColor : COLOR;" );
				if( dependencies.frag_projPos)
					App ("float4 projPos" + GetVertOutTexcoord() );
				if( ShouldUseLightMacros() )
					App( "LIGHTING_COORDS(" + GetVertOutTexcoord( true ) + "," + GetVertOutTexcoord( true ) + ")" );
				if( UseUnity5FogInThisPass() )
					App( "UNITY_FOG_COORDS(" + GetVertOutTexcoord( true ) + ")" ); // New in Unity 5

				bool sh = DoPassSphericalHarmonics() && !ps.catQuality.highQualityLightProbes;
				bool lm = LightmapThisPass();
				string shlmTexCoord = GetVertOutTexcoord();
				//string shlmTexCoord2 = GetVertOutTexcoord();

				if( lm && sh) {
					App( "#ifndef LIGHTMAP_OFF" );
					scope++;
						App( "float2 uvLM" + shlmTexCoord );


					if(currentPass == PassType.PrePassFinal){
						App("#ifdef DIRLIGHTMAP_OFF");
						scope++;
						App("float4 lmapFadePos" + GetVertOutTexcoord());
						scope--;
						App("#endif");
					}

					scope--;
					App( "#endif" );


				
					App( "#ifdef SHOULD_SAMPLE_SH_PROBE" );
					scope++;
						App ("float3 shLight" + shlmTexCoord );
					scope--;
					App ("#endif");

				} else if(lm){
					App( "#ifndef LIGHTMAP_OFF" );
					scope++;
						App( "float2 uvLM" + shlmTexCoord );
						if(currentPass == PassType.PrePassFinal){
							App("#ifdef DIRLIGHTMAP_OFF");
							scope++;
							App("float4 lmapFadePos" + GetVertOutTexcoord());
							scope--;
							App("#endif");
						}
					scope--;
					App( "#endif" );
				} else if(sh){
					App ("float3 shLight" + shlmTexCoord );
				}

			}
			scope--;
			App( "};" );
		}



		public bool ShouldUseLightMacros() {
			return ((currentPass == PassType.FwdAdd || ( currentPass == PassType.FwdBase && !ps.catBlending.ignoreProjector)) && (dependencies.UsesLightNodes() || ps.catLighting.IsLit()) );
		}

		public bool IsShadowPass() {
			return currentPass == PassType.ShadCast || currentPass == PassType.ShadColl;
		}

		public bool IsShadowOrOutlinePass(){
			return currentPass == PassType.Outline || IsShadowPass();
		}

		public bool IncludeLightingCginc(){
			return ps.catLighting.lightmapped || IsShadowPass() || (cNodes.Where(x=>x is SFN_LightAttenuation).Count() > 0);
		}


		void Vertex() {
			currentProgram = ShaderProgram.Vert;
			App( "VertexOutput vert (VertexInput v) {" );
			scope++;
			App( "VertexOutput o;" );

			

			if( dependencies.uv0_frag )
				App( "o.uv0 = v.texcoord0;" );
			if( dependencies.uv1_frag )
				App( "o.uv1 = v.texcoord1;" );
			if( dependencies.vert_out_vertexColor )
				App("o.vertexColor = v.vertexColor;");
			if( DoPassSphericalHarmonics() && !ps.catQuality.highQualityLightProbes){


				
				App( "#ifdef SHOULD_SAMPLE_SH_PROBE" );
				scope++;
				string nrmStr = SF_Tools.CurrentUnityVersion >= 5 ? "UnityObjectToWorldNorm(v.normal)" : "mul(_Object2World, float4(v.normal,0)).xyz * unity_Scale.w";
				App( "o.shLight = ShadeSH9(float4(" + nrmStr + ",1))" + ( ps.catLighting.doubleIncomingLight ? "" : " * 0.5" ) + ";" );
				scope--;
				App("#endif");
				
			}
			if( dependencies.vert_out_normals )
				InitNormalDirVert();
			if( dependencies.vert_out_tangents )
				InitTangentDirVert();
			if( dependencies.vert_out_binormals )
				InitBinormalDirVert();

			InitObjectPos();
			

			if( editor.mainNode.vertexOffset.IsConnectedAndEnabled() ) {
				InitObjectScale(); // Vertex shader only needs this here if it's used in this input
				App( "v.vertex.xyz += " + ps.n_vertexOffset + ";" );
			}

			if(dependencies.vert_out_worldPos)
				App ("o.posWorld = mul(_Object2World, v.vertex);");

			


			InitTangentTransformFrag();
			InitViewDirVert();
			InitReflectionDir();
			if( dependencies.frag_lightDirection ) {
				InitLightDir();
			}
			InitHalfVector();

			if(currentPass == PassType.Outline){
				App( "o.pos = mul(UNITY_MATRIX_MVP, float4(v.vertex.xyz + v.normal*"+ps.n_outlineWidth+",1));" );
			} else {
				App( "o.pos = mul(UNITY_MATRIX_MVP, v.vertex);" );
			}

			// New in Unity 5
			if( UseUnity5FogInThisPass() ) {
				App( "UNITY_TRANSFER_FOG(o,o.pos);" );
			}


			if( dependencies.frag_projPos ){
				App( "o.projPos = ComputeScreenPos (o.pos);" );
				App( "COMPUTE_EYEDEPTH(o.projPos.z);" );
			}
		

			if( dependencies.vert_out_screenPos ) { // TODO: Select screen pos accuracy etc

				if(ps.catQuality.highQualityScreenCoords){
					App( "o.screenPos = o.pos;" ); // Unpacked per-pixel
				} else {
					App( "o.screenPos = float4( o.pos.xy / o.pos.w, 0, 0 );" );
					App( "o.screenPos.y *= _ProjectionParams.x;" );
				}
			}
			if( LightmapThisPass() ) {
				App( "#ifndef LIGHTMAP_OFF" );
				scope++;
				App( "o.uvLM = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;" );

				if(currentPass == PassType.PrePassFinal){
					App( "#ifdef DIRLIGHTMAP_OFF");
					scope++;
					App( "o.lmapFadePos.xyz = (mul(_Object2World, v.vertex).xyz - unity_ShadowFadeCenterAndType.xyz) * unity_ShadowFadeCenterAndType.w;");
					App( "o.lmapFadePos.w = (-mul(UNITY_MATRIX_MV, v.vertex).z) * (1.0 - unity_ShadowFadeCenterAndType.w);");
					scope--;
					App( "#endif");
				}

				scope--;
				App("#endif");
			}

			

			if( currentPass == PassType.ShadColl ) {
				App( "TRANSFER_SHADOW_COLLECTOR(o)" );
			} else if( currentPass == PassType.ShadCast ) {
				App( "TRANSFER_SHADOW_CASTER(o)" );
			} else {
				if(ps.catLighting.IsVertexLit())
					Lighting();
				else if( ShouldUseLightMacros() )
					App( "TRANSFER_VERTEX_TO_FRAGMENT(o)" );
			}

			App( "return o;" );

			ResetDefinedState();
			End();
		}


		void Fragment() {
			currentProgram = ShaderProgram.Frag;
			App( "fixed4 frag(VertexOutput i) : COLOR {" );
			scope++;

			InitObjectPos();
			InitObjectScale();

			InitGrabPassSign();

			if(ps.catLighting.normalQuality == SFPSC_Lighting.NormalQuality.Normalized && dependencies.frag_normalDirection){
				App ("i.normalDir = normalize(i.normalDir);");
			}

			if( dependencies.vert_out_screenPos && ps.catQuality.highQualityScreenCoords ) {
				App( "i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );" );
				App( "i.screenPos.y *= _ProjectionParams.x;" );
			}
			
			InitSceneColorAndDepth();

			InitTangentTransformFrag();

			AppDebug( "Vectors" );

			InitViewDirFrag();
			InitNormalDirFrag();
			InitReflectionDir();

			CheckClip();

			PrepareLightmapVars();


			if( dependencies.frag_lightDirection ) {
				InitLightDir();
			}
			InitHalfVector();




			if( currentPass == PassType.PrePassBase){

			} else {
				Lighting(); // This is ignored in shadow passes
			}


			if(currentPass == PassType.PrePassBase){
				App ("return fixed4( normalDirection * 0.5 + 0.5, max(" + ps.n_gloss + ",0.0078125) );"); // TODO
			} else if( currentPass == PassType.ShadColl ) {
				App( "SHADOW_COLLECTOR_FRAGMENT(i)" );
			} else if( currentPass == PassType.ShadCast ) {
				App( "SHADOW_CASTER_FRAGMENT(i)" );
			} else if(currentPass == PassType.Outline){
				App ("return fixed4("+ps.n_outlineColor+",0);");
			} else {

				//if(ps.mOut.diffuse.IsConnectedEnabledAndAvailable()){
				//	AppFinalOutput("lightFinal + " + "diffuse", ps.n_alpha); // This is really weird, it should already be included in the light calcs. Do more research // TODO
				//}else
				if(currentPass == PassType.FwdAdd){
					AppFinalOutput("finalColor * " + ps.n_alpha, "0");
				} else {
					AppFinalOutput("finalColor", ps.n_alpha);
				}

				
			}

			End();
		}




		void InitGrabPassSign(){
			if( !dependencies.scene_uvs )
				return;
			App("#if UNITY_UV_STARTS_AT_TOP");
			scope++;
				App("float grabSign = -_ProjectionParams.x;");
			scope--;
			App("#else");
			scope++;
				App("float grabSign = _ProjectionParams.x;");
			scope--;
			App( "#endif" );
		}







		void TessellationVertexStruct() {
			App( "struct TessVertex {" );
			scope++;
			App( "float4 vertex : INTERNALTESSPOS;" );
			CommonVertexData();
			scope--;
			App( "};" );
		}

		void TessellationPatchConstant() {
			App( "struct OutputPatchConstant {" );
			scope++;
			App( "float edge[3]         : SV_TessFactor;" );
			App( "float inside          : SV_InsideTessFactor;" );
			App( "float3 vTangent[4]    : TANGENT;" );
			App( "float2 vUV[4]         : TEXCOORD;" );
			App( "float3 vTanUCorner[4] : TANUCORNER;" );
			App( "float3 vTanVCorner[4] : TANVCORNER;" );
			App( "float4 vCWts          : TANWEIGHTS;" );
			scope--;
			App( "};" );
		}

		void TessellationVertexTransfer() {
			App( "TessVertex tessvert (VertexInput v) {" );
			scope++;
			App( "TessVertex o;" );
			TransferCommonData();
			App( "return o;" );
			scope--;
			App( "}" );
		}

		void TessellationHullConstant() {
			App( "OutputPatchConstant hullconst (InputPatch<TessVertex,3> v) {" );
			scope++;
			App( "OutputPatchConstant o;" );
			App( "float ts = Tessellation( v[0], v[1], v[2] );" );
			App( "o.edge[0] = ts;" );
			App( "o.edge[1] = ts;" );
			App( "o.edge[2] = ts;" );
			App( "o.inside = ts;" );
			App( "return o;" );
			scope--;
			App( "}" );
		}

		void TessellationHull() {
			App( "[domain(\"tri\")]" );
			App( "[partitioning(\"fractional_odd\")]" );
			App( "[outputtopology(\"triangle_cw\")]" );
			App( "[patchconstantfunc(\"hullconst\")]" );
			App( "[outputcontrolpoints(3)]" );
			App( "TessVertex hull (InputPatch<TessVertex,3> v, uint id : SV_OutputControlPointID) {" );
			scope++;
			App( "return v[id];" );
			scope--;
			App( "}" );
		}


		void TessellationDomain() {

			App( "[domain(\"tri\")]" );
			App( "VertexOutput domain (OutputPatchConstant tessFactors, const OutputPatch<TessVertex,3> vi, float3 bary : SV_DomainLocation) {" );
			scope++;
			App( "VertexInput v;" );

			TransferBarycentric( "vertex" );
			if( dependencies.vert_in_normals )
				TransferBarycentric( "normal" );
			if( dependencies.vert_in_tangents )
				TransferBarycentric( "tangent" );
			if( dependencies.uv0 )
				TransferBarycentric( "texcoord0" );
			if( dependencies.uv1 )
				TransferBarycentric( "texcoord1" );
			if( dependencies.vert_in_vertexColor )
				TransferBarycentric( "vertexColor" );
			if( dependencies.displacement )
				App( "displacement(v);" );
			App( "VertexOutput o = vert(v);" );
			App( "return o;" );
			scope--;
			App( "}" );

		}

		void TransferBarycentric(string s) {
			App( "v." + s + " = vi[0]." + s + "*bary.x + vi[1]." + s + "*bary.y + vi[2]." + s + "*bary.z;" );
		}


		void FuncTessellation() {
			App( "float Tessellation(TessVertex v, TessVertex v1, TessVertex v2){" );
			scope++;


			switch (ps.catQuality.tessellationMode) {

				case SFPSC_Quality.TessellationMode.Regular:
					App( "return " + ps.n_tessellation + ";" );
					break;

				case SFPSC_Quality.TessellationMode.EdgeLength:
					App( "return UnityEdgeLengthBasedTess(v.vertex, v1.vertex, v2.vertex, " + ps.n_tessellation + ");" );
					break;

/*				case SFPSC_Quality.TessellationMode.EdgeLengthCulled:
					App( "return UnityEdgeLengthBasedTessCull(v.vertex, v1.vertex, v2.vertex, " + ps.n_tessellation + ", 1.0 );" );
					break;*/

			}

			scope--;
			App( "}" );
		}

		void FuncDisplacement() {
			if( !dependencies.displacement )
				return;
			App( "void displacement (inout VertexInput v){" );
			scope++;
			App( "v.vertex.xyz +=  "+ps.n_displacement+";" );
			scope--;
			App( "}" );
		}

		




		void Tessellation() {
			if( !dependencies.tessellation )
				return;
			currentProgram = ShaderProgram.Tess; // Not really, but almost

			App( "#ifdef UNITY_CAN_COMPILE_TESSELLATION" );
			scope++;
			//------------------------------------------------------------
			TessellationVertexStruct();
			TessellationPatchConstant();
			TessellationVertexTransfer();
			FuncDisplacement();
			FuncTessellation();
			TessellationHullConstant();
			TessellationHull();
			TessellationDomain();
			//------------------------------------------------------------
			scope--;
			App( "#endif" );

			ResetDefinedState();
		}








		// Todo: threshold
		void CheckClip() {
			if( !ps.UseClipping() )
				return;
			App( "clip(" + ps.n_alphaClip + " - 0.5);" );
		}


		
		void Fallback() {
			if( !string.IsNullOrEmpty( ps.catMeta.fallback ) )
				App( "FallBack \"" + ps.catMeta.fallback + "\"" );
			else
				App( "FallBack \"Diffuse\"" ); // Needed for shadows!
		}

		void WriteCustomEditor(){
			App("CustomEditor \"ShaderForgeMaterialInspector\"");
		}


		public void GrabPass() {
			if( !dependencies.grabPass )
				return;
			App("GrabPass{ }"); // TODO: Select if it's per-object or per-frame
		}

		//////////////////////////////////////////////////////////////// DEFERRED

		// Normal, gloss & depth writing
		void DeferredPrePassBasePass() {
			currentPass = PassType.PrePassBase;
			UpdateDependencies();
			ResetDefinedState();
			dependencies.ResetTexcoordNumbers();
			App( "Pass {" );
			scope++;
			{
				App( "Name \"PrePassBase\"" ); // TODO this name is a guess
				PassTags();
				RenderSetup();
				BeginCG();
				{
					CGvars();
					VertexInputStruct();
					VertexOutputStruct();
					Vertex();
					Tessellation();
					Fragment();
				}
				EndCG();
			}
			End();
			RemoveGhostNodes();
		}

		// Render accumulated light, do special shading etc
		void DeferredPrePassFinalPass() {
			currentPass = PassType.PrePassFinal;
			UpdateDependencies();
			ResetDefinedState();
			dependencies.ResetTexcoordNumbers();
			App( "Pass {" );
			scope++;
			{
				App( "Name \"PrePassFinal\"" ); // TODO this name is a guess
				PassTags();
				RenderSetup();
				BeginCG();
				{
					CGvars();
					VertexInputStruct();
					VertexOutputStruct();
					Vertex();
					Tessellation();
					Fragment();
				}
				EndCG();
			}
			End();
			RemoveGhostNodes();
		}

		////////////////////////////////////////////////////////////////
		
		


		void ForwardBasePass() {
			currentPass = PassType.FwdBase;
			UpdateDependencies();
			ResetDefinedState();
			dependencies.ResetTexcoordNumbers();
			App( "Pass {" );
			scope++;
			{
				App( "Name \"ForwardBase\"" );
				PassTags();
				RenderSetup();
				BeginCG();
				{
					CGvars();
					VertexInputStruct();
					VertexOutputStruct();
					Vertex();
					Tessellation();
					Fragment();
				}
				EndCG();
			}
			End();
			RemoveGhostNodes();
		}

		public void ForwardLightPass() {

			// TODO: FIX
			// Only when real-time light things are connected. These are:
			// Diffuse
			// Specular
			// Although could be any D:

			bool customLit = dependencies.UsesLightNodes();
			bool builtinLit = ps.catLighting.IsLit() && (ps.HasDiffuse() || ps.catLighting.HasSpecular());

			bool needsLightPass = ( builtinLit || customLit ) && ps.catLighting.UseMultipleLights();

			if( !needsLightPass )
				return;



			currentPass = PassType.FwdAdd;
			UpdateDependencies();
			ResetDefinedState();
			dependencies.ResetTexcoordNumbers();
			App( "Pass {" );
			scope++;
			{
				App( "Name \"ForwardAdd\"" );
				PassTags();
				RenderSetup();
				BeginCG();
				{
					CGvars();
					VertexInputStruct();
					VertexOutputStruct();
					Vertex();
					Tessellation();
					Fragment();
				}
				EndCG();
			}
			End();
			RemoveGhostNodes();
		}


		// This is a custom shadow thing!
		// Only needed when using alpha clip and/or vertex offset (May be needed with Tessellation as well)
		public void ShadowCollectorPass() {
			bool shouldUse = /*ps.shadowReceive &&*/ ( ps.UseClipping() || mOut.vertexOffset.IsConnectedAndEnabled() || mOut.displacement.IsConnectedAndEnabled() );
			if( !shouldUse )
				return;
			currentPass = PassType.ShadColl;
			UpdateDependencies();
			ResetDefinedState();
			dependencies.ResetTexcoordNumbers();

			App( "Pass {" );
			scope++;
			{
				App( "Name \"ShadowCollector\"" );
				PassTags();
				RenderSetup();
				BeginCG();
				{
					CGvars();
					VertexInputStruct();
					VertexOutputStruct();
					Vertex();
					Tessellation();
					Fragment();
				}
				EndCG();
			}
			End();
			RemoveGhostNodes();
		}
		
		// Only needed when using alpha clip and/or vertex offset (May be needed with Tessellation as well)
		public void ShadowCasterPass() {
			bool shouldUse = /*ps.shadowCast &&*/ (ps.UseClipping() || mOut.vertexOffset.IsConnectedAndEnabled() || mOut.displacement.IsConnectedAndEnabled());
			if( !shouldUse )
				return;
			currentPass = PassType.ShadCast;
			UpdateDependencies();
			ResetDefinedState();
			dependencies.ResetTexcoordNumbers();

			App( "Pass {" );
			scope++;
			{
				App( "Name \"ShadowCaster\"" );
				PassTags();
				RenderSetup();
				BeginCG();
				{
					CGvars();
					VertexInputStruct();
					VertexOutputStruct();
					Vertex();
					Tessellation();
					Fragment();
				}
				EndCG();
			}
			End();
			RemoveGhostNodes();
		}


		public void OutlinePass(){
			if(!mOut.outlineWidth.IsConnectedAndEnabled())
				return;
			currentPass = PassType.Outline;
			UpdateDependencies();
			ResetDefinedState();
			dependencies.ResetTexcoordNumbers();
			App( "Pass {" );
			scope++;
			{
				App( "Name \"Outline\"" );
				PassTags();
				RenderSetup();
				BeginCG();
				{
					CGvars();
					VertexInputStruct();
					VertexOutputStruct();
					Vertex();
					Tessellation();
					Fragment();
				}
				EndCG();
			}
			End();
			RemoveGhostNodes();
		}





		public void ResetDefinedState() {
			for( int i = 0; i < cNodes.Count; i++ ) {
				cNodes[i].varDefined = false;
			}
		}



		public void Evaluate() {

			if(SF_Debug.evalFlow)
				Debug.Log( "SHADER EVALUATING" );

			editor.ps.fChecker.UpdateAvailability();
			if(!editor.nodeView.treeStatus.CheckCanCompile()){
				return;
			}
			ps.UpdateAutoSettings();
			currentPass = PassType.FwdBase;
			PrepareEvaluation();
			UpdateDependencies();
			shaderString = "";
			scope = 0;

			//EditorUtility.UnloadUnusedAssets();
			GC.Collect();


			BeginShader();
			{
				PropertiesShaderLab();
				BeginSubShader();
				{
					SubShaderTags();
					if( ps.catMeta.LOD > 0 )
						App("LOD " + ps.catMeta.LOD);

					GrabPass();
					OutlinePass();
					if(ps.catLighting.renderPath == SFPSC_Lighting.RenderPath.DeferredPrePass){
						DeferredPrePassBasePass();
						DeferredPrePassFinalPass();
					}
					ForwardBasePass();
					ForwardLightPass();
					ShadowCollectorPass();
					ShadowCasterPass();

				}
				End();
				Fallback();
				WriteCustomEditor();
			}
			End();
			
			SaveShaderAsset();
			ApplyPropertiesToMaterial();
			editor.ShaderOutdated = UpToDateState.UpToDate;

		}


		//string GetEditorVersionOfShader() {
			//return shaderString.Replace( "_Time", "_EditorTime" );
			//		shaderString.Replace( "_SinTime", "_SinTimeEditor" );
			//		shaderString.Replace( "_CosTime", "_CosTimeEditor" );
			//		shaderString.Replace( "_SinTime", "_SinTimeEditor" );
			//return shaderString;
		//}




		public void SaveShaderAsset() {

			//Debug.Log("SaveShaderAsset()");
			string fileContent = editor.nodeView.GetNodeDataSerialized() + "\n\n" + shaderString;


			// Version control unlocking
			Asset shaderAsset = UnityEditor.VersionControl.Provider.GetAssetByPath(editor.GetShaderFilePath());
			if(shaderAsset.locked || shaderAsset.readOnly){
				UnityEditor.VersionControl.Provider.Lock( shaderAsset, false );
				UnityEditor.VersionControl.Provider.Checkout( shaderAsset, CheckoutMode.Both );
			}

			StreamWriter sw = new StreamWriter( editor.GetShaderFilePath() );
			sw.Write(fileContent);
			sw.Flush();
			sw.Close();
			try{
				AssetDatabase.Refresh( ImportAssetOptions.DontDownloadFromCacheServer );
			} catch(Exception e){
				e.ToString();
			}

			editor.OnShaderEvaluated();

		}





		public void ApplyPropertiesToMaterial() {
			for( int i = 0; i < cNodes.Count; i++ ) {
				if( !cNodes[i].IsProperty() )
					continue;
				ApplyProperty( cNodes[i] );
			}
		}

		public void ApplyProperty( SF_Node node ) {

			if( !node.IsProperty() )
				return;

			Material m = SF_Editor.instance.preview.InternalMaterial;
			switch( node.GetType().ToString() ) {
				case ( "ShaderForge.SFN_Tex2d" ):
					SFN_Tex2d texNode = (SFN_Tex2d)node;
					m.SetTexture( texNode.property.GetVariable(), texNode.TextureAsset );
					break;
				case ( "ShaderForge.SFN_Tex2dAsset" ):
					SFN_Tex2dAsset texAssetNode = (SFN_Tex2dAsset)node;
					m.SetTexture( texAssetNode.property.GetVariable(), texAssetNode.textureAsset );
					break;
				case ( "ShaderForge.SFN_Cubemap" ):
					SFN_Cubemap cubeNode = (SFN_Cubemap)node;
					m.SetTexture( cubeNode.property.GetVariable(), cubeNode.cubemapAsset );
					break;
				case ( "ShaderForge.SFN_Slider" ):
					SFN_Slider sliderNode = (SFN_Slider)node;
					m.SetFloat( sliderNode.property.GetVariable(), sliderNode.current );
					break;
				case ( "ShaderForge.SFN_Color" ):
					SFN_Color colorNode = (SFN_Color)node;
					m.SetColor( colorNode.property.GetVariable(), colorNode.GetColor() );
					break;
				case ( "ShaderForge.SFN_ValueProperty" ):
					SFN_ValueProperty valueNode = (SFN_ValueProperty)node;
					m.SetFloat( valueNode.property.GetVariable(), valueNode.texture.dataUniform[0] );
					break;
				case ( "ShaderForge.SFN_ToggleProperty" ):
					SFN_ToggleProperty toggleNode = (SFN_ToggleProperty)node;
					m.SetFloat( toggleNode.property.GetVariable(), toggleNode.texture.dataUniform[0] );
					break;
				case ( "ShaderForge.SFN_SwitchProperty" ):
					SFN_SwitchProperty switchNode = (SFN_SwitchProperty)node;
					m.SetFloat( switchNode.property.GetVariable(), switchNode.on ? 1f : 0f );
					break;
				case ( "ShaderForge.SFN_Vector4Property" ):
					SFN_Vector4Property vector4Node = (SFN_Vector4Property)node;
					m.SetVector( vector4Node.property.GetVariable(), vector4Node.texture.dataUniform );
					break;
				case ( "ShaderForge.SFN_StaticBranch" ):
					SFN_StaticBranch sbNode = (SFN_StaticBranch)node;
					
					if(sbNode.on){
						//Debug.Log("Enabling keyword");
						//m.EnableKeyword(sbNode.property.nameInternal);
					} else {
						//Debug.Log("Disabling keyword");
						//m.DisableKeyword(sbNode.property.nameInternal);
					}

					break;
			}
		}




		void End() {
			scope--;
			App( "}" );
		}
		public void AppIfNonEmpty(string s){
			if(!string.IsNullOrEmpty(s))
				App(s);
		}
		public void App( string s ) {

			if(s.Contains("\n")){
				string[] split = s.Split('\n');
				for(int i=0;i<split.Length;i++){
					App(split[i]);
				}
			} else {
				shaderString += GetScopeTabs() + s + "\n";
			}


		}
		public void AppDebug( string s ) {
			//if(DEBUG)
			
			string scopeSlashes = GetScopeTabs().Replace(' ','/');
			
			if( scopeSlashes.Length < 2)
				scopeSlashes = "//";


			shaderString += scopeSlashes.Substring(Mathf.Min(s.Length + 2, scopeSlashes.Length - 2)) + " " + s + ":\n";
		}
		string GetScopeTabs() {
			string s = "";
			for( int i = 0; i < scope; i++ ) {
				s += "    ";
			}
			return s;
		}
		void NewLine() {
			shaderString += "\n";
		}

		//	shaderEvaluator.previewBackgroundColor


	}
}
