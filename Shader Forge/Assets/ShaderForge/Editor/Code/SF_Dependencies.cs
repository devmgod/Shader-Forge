using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace ShaderForge {

	



	public class SF_Dependencies {

		private int vert_out_texcoordNumber = 0;

		public bool uv0 = false;
		public bool uv0_frag = false;
		public bool uv1 = false;
		public bool uv1_frag = false;
		public bool vertexColor = false;
		public bool lightColor = false;
		public bool time = false;
		public bool grabPass = false;
		public bool tessellation = false;
		public bool displacement = false;

		public bool vert_out_worldPos = false;
		public bool vert_out_screenPos = false;
		public bool vert_out_normals = false;
		public bool vert_out_tangents = false;
		public bool vert_out_binormals = false;
		public bool vert_in_normals = false;
		public bool vert_in_tangents = false;
		public bool vert_in_vertexColor = false;
		public bool vert_out_vertexColor = false;
		public bool frag_viewReflection = false;
		public bool frag_viewDirection = false;
		public bool frag_normalDirection = false;
		public bool frag_lightDirection = false;
		public bool frag_halfDirection = false;
		public bool frag_attenuation = false;
		public bool frag_tangentTransform = false;
		public bool frag_screenPos = false;
		public bool vert_screenPos = false;



		public bool vert_viewReflection = false;
		public bool vert_viewDirection = false;
		public bool vert_normalDirection = false;
		public bool vert_lightDirection = false;
		public bool vert_halfDirection = false;
		public bool vert_tangentTransform = false;

		public bool frag_objectPos = false;
		
		

		public bool const_pi = false;
		public bool const_tau = false;
		public bool const_root2 = false;
		public bool const_e = false;
		public bool const_phi = false;



		int shaderTarget = 3; // Shader target: #pragma target 3.0
		public List<RenderPlatform> excludeRenderers;

		public SF_Dependencies(SF_PassSettings ps) {
			excludeRenderers = new List<RenderPlatform>();
			for( int i = 0; i < ps.usedRenderers.Length; i++ ) {
				if( !ps.usedRenderers[i] ) {
					excludeRenderers.Add( ( RenderPlatform )i );
				}
			}
			


			//excludeRenderers.Add( RenderPlatform.flash ); // Always exclude flash // TODO: Maybe not always... Read from config
			//excludeRenderers.Add( RenderPlatform.gles );
			//excludeRenderers.Add( RenderPlatform.xbox360 );
			//excludeRenderers.Add( RenderPlatform.ps3 );
		}

		public void IncrementTexCoord( int num ) {
			vert_out_texcoordNumber += num;
		}

		public bool UsesLightNodes() {
			return frag_attenuation || frag_lightDirection || frag_halfDirection || lightColor;
		}

		public void NeedFragVertexColor() {
			vert_in_vertexColor = true;
			vert_out_vertexColor = true;
		}

		public void NeedFragScreenPos() {
			NeedVertScreenPos();
			vert_out_screenPos = true;
			frag_screenPos = true;
		}

		public void NeedFragObjPos() {
			frag_objectPos = true;
		}

		public void NeedVertScreenPos() {
			vert_screenPos = true;
		}

		public void NeedLightColor() {
			lightColor = true;
		}

		public void NeedFragAttenuation(){
			frag_attenuation = true;
		}

		public void NeedRefraction() {
			NeedGrabPass();
			NeedFragScreenPos();
		}

		public void NeedGrabPass() {
			grabPass = true;
		}

		public void NeedTessellation(){
			shaderTarget = Mathf.Max( shaderTarget, 5);
			vert_in_tangents = true;
			vert_in_normals = true;
			tessellation = true;
		}


		public void NeedDisplacement() {
			displacement = true;
		}

		public void NeedFragWorldPos() {
			vert_out_worldPos = true;
		}
		public void NeedVertWorldPos() {
			vert_out_worldPos = true; // TODO ?
		}

		public void NeedFragHalfDir() {
			frag_halfDirection = true;
			NeedFragLightDir();
			NeedFragViewDirection();
		}
		public void NeedVertHalfDir() {
			vert_halfDirection = true;
			NeedVertLightDir();
			NeedVertViewDirection();
		}

		public void NeedFragLightDir() {
			frag_lightDirection = true;
			NeedFragWorldPos();
		}
		public void NeedVertLightDir() {
			vert_lightDirection = true;
			NeedVertWorldPos();
		}

		public void NeedFragViewDirection() {
			frag_viewDirection = true;
			NeedFragWorldPos();
		}
		public void NeedVertViewDirection() {
			vert_viewDirection = true;
			NeedVertWorldPos();
		}


	

		public void NeedFragViewReflection() {
			NeedFragViewDirection();
			NeedFragNormals();
			frag_viewReflection = true;
		}

		public void NeedFragNormals() {
			vert_in_normals = true;
			vert_out_normals = true;
			frag_normalDirection = true;
		}

		public void NeedFragTangents() {
			vert_in_tangents = true;
			vert_out_tangents = true;
		}
		public void NeedFragBinormals() {
			vert_in_normals = true;
			vert_out_normals = true;
			vert_in_tangents = true;
			vert_out_tangents = true;
			vert_out_binormals = true;
		}

		public void NeedFragTangentTransform() {
			frag_tangentTransform = true;
			vert_in_normals = true;
			vert_out_normals = true;
			vert_in_tangents = true;
			vert_out_tangents = true;
			vert_out_binormals = true;
		}



		public void ExcludeRenderPlatform( RenderPlatform plat ) {
			if( !excludeRenderers.Contains( plat ) ) {
				excludeRenderers.Add( plat );
			}
		}

		public bool DoesExcludePlatforms() {
			return excludeRenderers.Count > 0;
		}

		public bool IsTargetingAboveDefault() {
			return ( shaderTarget > 2 );
		}

		public string GetExcludePlatforms() {
			string s = "";
			foreach( RenderPlatform plat in excludeRenderers )
				s += plat.ToString() + " ";
			return s;
		}

		public void SetMinimumShaderTarget( int x ) {
			if( x > shaderTarget )
				shaderTarget = x;
		}
		public string GetShaderTarget() {
			return ( shaderTarget + ".0" );
		}

		public string GetVertOutTexcoord() {
			string s = vert_out_texcoordNumber.ToString();
			vert_out_texcoordNumber++;
			return s;
		}

		public void ResetTexcoordNumbers() {
			//vert_in_texcoordNumber = 0;
			vert_out_texcoordNumber = 0;
		}

	}
}