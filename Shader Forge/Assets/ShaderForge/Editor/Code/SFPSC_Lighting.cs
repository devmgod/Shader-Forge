﻿using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ShaderForge {
	
	
	[System.Serializable]
	public class SFPSC_Lighting : SFPS_Category {


		public RenderPath renderPath = RenderPath.Forward;
		public LightPrecision lightPrecision = LightPrecision.Fragment;
		public NormalQuality normalQuality = NormalQuality.Normalized;
		public NormalSpace normalSpace = NormalSpace.Tangent;
		public LightMode lightMode = LightMode.BlinnPhong;
		public LightCount lightCount = LightCount.Multi;

		public bool useAmbient = true;
		public bool maskedSpec = true;
		//public bool shadowCast = true;
		//public bool shadowReceive = true;
		public bool bakedLight = false;
		public bool reflectprobed = false;
		public bool energyConserving = false;
		public bool remapGlossExponentially = true;

		public enum RenderPath { Forward, DeferredPrePass };
		public string[] strRenderPath = new string[] { "Forward", "Deferred Pre-Pass" };


		public enum LightPrecision { Vertex, Fragment };
		public string[] strLightPrecision = new string[] { "Per-Vertex", "Per-Fragment" };
		public enum NormalQuality { Interpolated, Normalized };
		public string[] strNormalQuality = new string[] { "Interpolated", "Normalized" };
		public enum NormalSpace { Tangent, Object, World };
		public string[] strNormalSpace = new string[] { "Tangent", "Object", "World" };
		public enum LightMode { Unlit, BlinnPhong, Phong, PBL };
		public string[] strLightMode = new string[] { "Unlit/Custom", "Blinn-Phong", "Phong", "PBL" };
		public enum LightCount { Single, Multi };
		public string[] strLightCount = new string[] { "Single Directional", "Multi-light"};


		public override string Serialize(){
			string s = "";
			s += Serialize( "lico", ( (int)lightCount ).ToString() );
			s += Serialize( "lgpr", ( (int)lightPrecision ).ToString() );
			s += Serialize( "nrmq", ( (int)normalQuality ).ToString() );
			s += Serialize( "nrsp", ( (int)normalSpace ).ToString() );
			s += Serialize( "limd", ( (int)lightMode ).ToString() );
			s += Serialize( "uamb", useAmbient.ToString() );
			s += Serialize( "mssp", maskedSpec.ToString() );
			s += Serialize( "bkdf", bakedLight.ToString() );
			s += Serialize( "rprd", reflectprobed.ToString() );
			s += Serialize( "enco", energyConserving.ToString());
			s += Serialize( "rmgx", remapGlossExponentially.ToString());
			s += Serialize( "rpth", ((int)renderPath).ToString() );
			
			//s += Serialize( "shdc", shadowCast.ToString() );
			//s += Serialize( "shdr", shadowReceive.ToString() );
			return s;
		}

		public override void Deserialize(string key, string value){


			switch( key ) {
			case "lgpr":
				lightPrecision = (LightPrecision)int.Parse( value );
				break;
			case "nrmq":
				normalQuality = (NormalQuality)int.Parse( value );
				break;
			case "nrsp":
				normalSpace = (NormalSpace)int.Parse( value );
				break;
			case "limd":
				lightMode = (LightMode)int.Parse( value );
				break;
			case "uamb":
				useAmbient = bool.Parse( value );
				break;
			case "mssp":
				maskedSpec = bool.Parse( value );
				break;
			case "bkdf":
				bakedLight = bool.Parse( value );
				break;
			/*case "shdc":
				shadowCast = bool.Parse( value );
				break;
			case "shdr":
				shadowReceive = bool.Parse( value );
				break;*/
			case "lico":
				lightCount = (LightCount)int.Parse( value );
				break;
			case "lmpd":
				bakedLight |= bool.Parse( value );
				break;
			case "lprd":
				bakedLight |= bool.Parse( value );
				break;
			case "rprd":
				reflectprobed = bool.Parse( value );
				break;
			case "enco":
				energyConserving = bool.Parse( value );
				break;
				
				
			case "rmgx":
				remapGlossExponentially = bool.Parse( value );
				break;
			case "rpth":
				renderPath = (RenderPath)int.Parse( value );
				break;
			}

		}

	

		public override float DrawInner(ref Rect r){

			float prevYpos = r.y;
			r.y = 0;

			
			r.xMin += 20;
			r.y += 20;
		
			renderPath = (RenderPath)UndoableContentScaledToolbar( r, "Render Path", (int)renderPath, strRenderPath, "render path" );
			

			if(renderPath == RenderPath.DeferredPrePass){
				if(lightMode != LightMode.BlinnPhong)
					lightMode = LightMode.BlinnPhong;
				if(ps.catBlending.autoSort == false){
					ps.catBlending.autoSort = true;
				}
				if(ps.catBlending.blendModePreset != BlendModePreset.Opaque){
					ps.catBlending.blendModePreset = BlendModePreset.Opaque;
					ps.catBlending.ConformBlendsToPreset();
				}
			}
			r.y += 20;
			if(renderPath == RenderPath.DeferredPrePass){
				GUI.enabled = false;
				UndoableContentScaledToolbar( r, "Light Mode", (int)LightMode.BlinnPhong, strLightMode, "light mode" );
				GUI.enabled = true;
			} else {
				lightMode = (LightMode)UndoableContentScaledToolbar( r, "Light Mode", (int)lightMode, strLightMode, "light mode" );
			}
			r.y += 20;


			if( ps.catLighting.IsPBL() == false ) {
				UndoableConditionalToggle( r, ref remapGlossExponentially,
									 usableIf: ps.HasGloss() && renderPath != RenderPath.DeferredPrePass,
									 disabledDisplayValue: renderPath == RenderPath.DeferredPrePass ? true : false,
									 label: "Remap gloss from [0-1] to " + ( ( renderPath == RenderPath.DeferredPrePass ) ? "[0-128]" : "[1-2048]" ),
									 undoSuffix: "gloss remap"
									 );
				r.y += 20;
			}
			
			
			
			if( lightMode == LightMode.Unlit || lightMode == LightMode.PBL )
				GUI.enabled = false;
			{
				
				//bool b = energyConserving;
				if( lightMode == LightMode.PBL )
					GUI.Toggle( r, true, "Energy Conserving" ); // Dummy display of a checked energy conserve
				else
					energyConserving = UndoableToggle( r, energyConserving, "Energy Conserving", "energy conservation", null );
					//energyConserving = GUI.Toggle( r, energyConserving, "Energy Conserving" );
				
				r.y += 20;
				GUI.enabled = true;
			}
			
			
			GUI.enabled = renderPath == RenderPath.Forward;
			lightCount = (LightCount)UndoableContentScaledToolbar(r, "Light Count", (int)lightCount, strLightCount, "light count" );
			GUI.enabled = true;
			r.y += 20;
			
			
			//lightPrecision = (LightPrecision)ContentScaledToolbar(r, "Light Quality", (int)lightPrecision, strLightPrecision ); // TODO: Too unstable for release
			//r.y += 20;	
			GUI.enabled = renderPath == RenderPath.Forward;
			normalQuality = (NormalQuality)UndoableContentScaledToolbar(r, "Normal Quality", (int)normalQuality, strNormalQuality, "normal quality" );
			GUI.enabled = true;
			r.y += 20;

			GUI.enabled = ps.mOut.normal.IsConnectedEnabledAndAvailable();
			normalSpace = (NormalSpace)UndoableContentScaledToolbar( r, "Normal Space", (int)normalSpace, strNormalSpace, "normal space" );
			r.y += 20;
			GUI.enabled = true;
			
			UndoableConditionalToggle(r, ref bakedLight,
			                         usableIf: 				ps.HasDiffuse() && lightMode != LightMode.Unlit,
			                         disabledDisplayValue: 	false,
			                         label: 				"Lightmap & light probes",
									 undoSuffix:			"lightmap & light probes"
			                         );
			r.y += 20;


		
			UndoableConditionalToggle( r, ref reflectprobed,
									usableIf: ps.HasSpecular() && lightMode != LightMode.Unlit,
									disabledDisplayValue: false,
									label: "Reflection probe support",
									undoSuffix: "reflection probe support"
									);
			r.y += 20;
			
			
			
			
			/*shadowCast = GUI.Toggle( r, shadowCast, "Cast shadows" );
			r.y += 20;
			shadowReceive = GUI.Toggle( r, shadowReceive, "Receive shadows" );
			r.y += 20;*/
			
			
			
			
			//GUI.enabled = IsLit();
			
			
			
			UndoableConditionalToggle(r, ref useAmbient,
									 usableIf:				!bakedLight && ps.catLighting.IsLit(),
									 disabledDisplayValue:	bakedLight,
			                         label: 				"Receive Ambient Light",
			                         undoSuffix:			"receive ambient light"
			                         );
			r.y += 20;
			
			/*
			if(lightprobed){
				GUI.enabled = false;
				GUI.Toggle( r, true, "Receive Ambient Light" );
				GUI.enabled = true;
			}else{
				useAmbient = GUI.Toggle( r, useAmbient, "Receive Ambient Light" );
			}*/
			
			
			//r.y += 20;
			if(ps.catLighting.HasSpecular() && renderPath == RenderPath.Forward){
				maskedSpec = UndoableToggle( r, maskedSpec, "Mask directional light specular by shadows", "directional light specular shadow masking", null );
			} else {
				GUI.enabled = false;
				GUI.Toggle( r, false, "Mask directional light specular by shadows" );
				GUI.enabled = true;
			}
			r.y += 20;
		
			r.y += prevYpos;

			return (int)r.yMax;
		}







		public bool UseMultipleLights() {
			return lightCount == LightCount.Multi;
		}

		public bool IsVertexLit() {
			return ( IsLit() && ( lightPrecision == LightPrecision.Vertex ) );
		}
		
		public bool IsFragmentLit() {
			return ( IsLit() && ( lightPrecision == LightPrecision.Fragment ) );
		}

		public bool IsLit() {
			return ( lightMode != LightMode.Unlit && ( ps.HasDiffuse() || HasSpecular()) );
		}
		
		public bool IsEnergyConserving() {
			return IsLit() && (energyConserving || lightMode == LightMode.PBL);
		}
		
		public bool IsPBL() {
			return lightMode == LightMode.PBL;
		}
		
		public bool HasSpecular() {
			return ( lightMode == LightMode.BlinnPhong || lightMode == LightMode.Phong || lightMode == LightMode.PBL ) && ( ps.mOut.specular.IsConnectedAndEnabled() );
		}





	



	}
}