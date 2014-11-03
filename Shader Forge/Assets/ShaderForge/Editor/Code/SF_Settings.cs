﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;


namespace ShaderForge {

	public enum SF_Setting{
		CurveShape,				// int	Bezier/Linear/etc
		AutoCompile, 			// bool	True/False
		HierarchalNodeMove, 	// bool	True/False
		DrawNodePreviews,		// bool	True/False
		QuickPickScrollWheel,	// bool	True/False
		ControlMode,			// int	Shader Forge / Unity / Unreal
		ShowVariableSettings,	// bool True/False
		ShowNodeSidebar			// bool True/False
	};

	public enum ControlMode{ShaderForge, UnityMaya, Unreal};

	public class SF_Settings {

		public const string prefix = "shaderforge_";
		public const string suffixDefault = "_default";

		public SF_Settings() {

		}

		public static void InitializeSettings() {
			// Set up all defaults
			SetDefaultInt ( SF_Setting.CurveShape, 			 (int)ConnectionLineStyle.Bezier 	);
			SetDefaultBool( SF_Setting.AutoCompile, 		 true 								);
			SetDefaultBool( SF_Setting.HierarchalNodeMove, 	 false 								);
			SetDefaultBool( SF_Setting.DrawNodePreviews, 	 true 								);
			SetDefaultBool( SF_Setting.QuickPickScrollWheel, true 								);
			SetDefaultInt ( SF_Setting.ControlMode, 		 (int)ControlMode.ShaderForge 		);
			SetDefaultBool( SF_Setting.ShowVariableSettings, false								);
			SetDefaultBool( SF_Setting.ShowNodeSidebar, 	 true								);
		}


		// Settings:
		public static bool AutoRecompile {
			get { return LoadBool(SF_Setting.AutoCompile); }
			set { SetBool(SF_Setting.AutoCompile, value); }
		}
		
		public static bool HierarcyMove {
			get { return LoadBool(SF_Setting.HierarchalNodeMove); }
			set { SetBool(SF_Setting.HierarchalNodeMove, value); }
		}

		public static bool DrawNodePreviews {
			get { return LoadBool(SF_Setting.DrawNodePreviews); }
			set { SetBool(SF_Setting.DrawNodePreviews, value); }
		}

		public static ConnectionLineStyle ConnectionLineStyle {
			get { return ConnectionLineStyle.Bezier;/*return (ConnectionLineStyle)SF_Settings.LoadInt(SF_Setting.CurveShape);*/ }
			set { SF_Settings.SetInt(SF_Setting.CurveShape, (int)value); }
		}

		public static ControlMode ControlMode {
			get { return (ControlMode)SF_Settings.LoadInt(SF_Setting.ControlMode);}
			set { SF_Settings.SetInt(SF_Setting.ControlMode, (int)value); }
		}

		public static bool QuickPickWithWheel {
			get { return LoadBool(SF_Setting.QuickPickScrollWheel); }
			set { SetBool(SF_Setting.QuickPickScrollWheel, value); }
		}

		public static bool ShowVariableSettings {
			get { return LoadBool(SF_Setting.ShowVariableSettings); }
			set { SetBool(SF_Setting.ShowVariableSettings, value); }
		}

		public static bool ShowNodeSidebar {
			get { return LoadBool(SF_Setting.ShowNodeSidebar); }
			set { SetBool(SF_Setting.ShowNodeSidebar, value); }
		}







		// --------------------------------------------------
		public static bool LoadBool( SF_Setting setting ) {
			string key = KeyOf(setting);
			return EditorPrefs.GetBool( key, EditorPrefs.GetBool( key + suffixDefault ) );
		}
		public static string LoadString( SF_Setting setting ) {
			string key = KeyOf(setting);
			return EditorPrefs.GetString( key, EditorPrefs.GetString( key + suffixDefault ) );
		}
		public static int LoadInt( SF_Setting setting ) {
			string key = KeyOf(setting);
			return EditorPrefs.GetInt( key, EditorPrefs.GetInt( key + suffixDefault) );
		}
		public static float LoadFloat( SF_Setting setting ) {
			string key = KeyOf(setting);
			return EditorPrefs.GetFloat( key, EditorPrefs.GetFloat( key + suffixDefault) );
		}
		// --------------------------------------------------
		private static string KeyOf( SF_Setting setting ){
			return prefix + setting.ToString();
		}
		// --------------------------------------------------
		private static void SetDefaultBool( SF_Setting setting, bool value ){
			string key = KeyOf(setting);
			EditorPrefs.SetBool(key + suffixDefault, value);
			if(!EditorPrefs.HasKey(key)){
				SetBool(setting, value);
			}
		}
		private static void SetDefaultString(SF_Setting setting, string value){
			string key = KeyOf(setting);
			EditorPrefs.SetString(key + suffixDefault, value);
			if(!EditorPrefs.HasKey(key)){
				SetString(setting, value);
			}
		}
		private static void SetDefaultInt(SF_Setting setting, int value){
			string key = KeyOf(setting);
			EditorPrefs.SetInt(key + suffixDefault, value);
			if(!EditorPrefs.HasKey(key)){
				SetInt(setting, value);
			}
		}
		private static void SetDefaultFloat(SF_Setting setting, float value){
			string key = KeyOf(setting);
			EditorPrefs.SetFloat(key + suffixDefault, value);
			if(!EditorPrefs.HasKey(key)){
				SetFloat(setting, value);
			}
		}
		// --------------------------------------------------
		public static void SetBool( SF_Setting setting, bool value ){
			string key = KeyOf(setting);
			EditorPrefs.SetBool(key, value);
		}
		public static void SetString(SF_Setting setting, string value){
			string key = KeyOf(setting);
			EditorPrefs.SetString(key, value);
		}
		public static void SetInt(SF_Setting setting, int value){
			string key = KeyOf(setting);
			EditorPrefs.SetInt(key, value);
		}
		public static void SetFloat(SF_Setting setting, float value){
			string key = KeyOf(setting);
			EditorPrefs.SetFloat(key, value);
		}

	}

}
