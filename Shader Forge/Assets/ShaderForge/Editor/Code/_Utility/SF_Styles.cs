﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System;

namespace ShaderForge {
	public static class SF_Styles {

		private static Texture2D _iconErrorSmall;
		public static Texture2D IconErrorSmall {
			get {
				return ( _iconErrorSmall = _iconErrorSmall ?? EditorGUIUtility.FindTexture( "console.erroricon.sml" ) );
			}
		}
		private static Texture2D _iconWarningSmall;
		public static Texture2D IconWarningSmall {
			get {
				return ( _iconWarningSmall = _iconWarningSmall ?? EditorGUIUtility.FindTexture( "console.warnicon.sml" ) );
			}
		}


		private static GUIStyle miniLabelRight;
		public static GUIStyle MiniLabelRight {
			get {
				if( miniLabelRight == null ) {
					miniLabelRight = new GUIStyle( EditorStyles.miniLabel );
					miniLabelRight.alignment = TextAnchor.MiddleRight;
					
				}
				return miniLabelRight;
			}
		}



		private static GUIStyle largeTextField;
		public static GUIStyle LargeTextField {
			get {
				if( largeTextField == null ) {
					largeTextField = new GUIStyle( EditorStyles.textField );
					//largeTextField.fontStyle = FontStyle.Bold;
					largeTextField.fontSize = 20;
					largeTextField.alignment = TextAnchor.MiddleLeft;

				}
				return largeTextField;
			}
		}


		private static GUIStyle largeTextFieldNoFrame;
		public static GUIStyle LargeTextFieldNoFrame {
			get {
				if( largeTextFieldNoFrame == null ) {
					largeTextFieldNoFrame = new GUIStyle( EditorStyles.label );
					largeTextFieldNoFrame.fontSize = LargeTextField.fontSize;
					largeTextFieldNoFrame.alignment = LargeTextField.alignment;
					
				}
				return largeTextFieldNoFrame;
			}
		}


		private static GUIStyle instructionCountRenderer;
		public static GUIStyle InstructionCountRenderer {
			get {
				if( instructionCountRenderer == null ) {
					instructionCountRenderer = new GUIStyle( EditorStyles.miniLabel );
					InstructionCountRenderer.alignment = TextAnchor.MiddleRight;
				}
				return instructionCountRenderer;
			}
		}


		private static GUIStyle codeTextArea;
		public static GUIStyle CodeTextArea {
			get {
				if( codeTextArea == null ) {
					codeTextArea = new GUIStyle( GUI.skin.textArea );
					codeTextArea.font = (Font)Resources.Load( SF_Paths.pFonts + "VeraMono", typeof(Font) );
					codeTextArea.padding = new RectOffset(3,3,3,0);
					codeTextArea.wordWrap = false;
				}
				return codeTextArea;
			}
		}

		// Bitstream Vera Sans Mono


		private static GUIStyle miniLabelOverflow;
		public static GUIStyle MiniLabelOverflow {
			get {
				if( miniLabelOverflow == null ) {
					miniLabelOverflow = new GUIStyle( EditorStyles.miniLabel );
					miniLabelOverflow.clipping = TextClipping.Overflow;
				}
				return miniLabelOverflow;
			}
		}

		private static GUIStyle creditsLabelText;
		public static GUIStyle CreditsLabelText {
			get {
				if( creditsLabelText == null ) {
					creditsLabelText = new GUIStyle( EditorStyles.label );
					creditsLabelText.alignment = TextAnchor.MiddleLeft;
					creditsLabelText.fixedHeight = 16;
					creditsLabelText.padding = new RectOffset(0,0,6,0);
					creditsLabelText.clipping = TextClipping.Overflow;
				}
				return creditsLabelText;
			}
		}


		private static GUIStyle nodeNameLabelText;
		public static GUIStyle GetNodeNameLabelText() {
			if( nodeNameLabelText == null ) {
				nodeNameLabelText = new GUIStyle( EditorStyles.largeLabel );
				nodeNameLabelText.fontStyle = FontStyle.Bold;
				nodeNameLabelText.fontSize = 11;
				nodeNameLabelText.alignment = TextAnchor.MiddleCenter;
				nodeNameLabelText.normal.textColor = new Color( 0.8f, 0.8f, 0.8f, 1f );
			}
			return nodeNameLabelText;
		}


		private static GUIStyle nodeCommentLabelText;
		public static GUIStyle GetNodeCommentLabelText() {
			if( nodeCommentLabelText == null ) {
				nodeCommentLabelText = new GUIStyle( EditorStyles.largeLabel );
				nodeCommentLabelText.fontStyle = FontStyle.Italic;
				nodeCommentLabelText.fontSize = 16;
				nodeCommentLabelText.alignment = TextAnchor.LowerLeft;
				float col = SF_GUI.ProSkin ? 1f : 0f;
				nodeCommentLabelText.normal.textColor = new Color( col, col, col, 0.3f );
			}
			return nodeCommentLabelText;
		}

		private static GUIStyle nodeScreenshotTitleText;
		public static GUIStyle GetNodeScreenshotTitleText() {
			if( nodeScreenshotTitleText == null ) {
				//nodeScreenshotTitleText = new GUIStyle( NodeStyle );
				nodeScreenshotTitleText = new GUIStyle(EditorStyles.boldLabel);
				nodeScreenshotTitleText.fontSize = 14;
				nodeScreenshotTitleText.alignment = TextAnchor.LowerCenter;
				nodeScreenshotTitleText.clipping = TextClipping.Overflow;
				float col = SF_GUI.ProSkin ? 1f : 0f;
				nodeScreenshotTitleText.normal.textColor = new Color( col, col, col, 0.5f );
			}
			return nodeScreenshotTitleText;
		}



		private static GUIStyle nodeCommentLabelTextField;
		public static GUIStyle GetNodeCommentLabelTextField() {
			if( nodeCommentLabelTextField == null ) {
				nodeCommentLabelTextField = new GUIStyle( EditorStyles.textField );
				nodeCommentLabelTextField.fontStyle = FontStyle.Italic;
				nodeCommentLabelTextField.fontSize = 16;
				nodeCommentLabelTextField.alignment = TextAnchor.LowerLeft;
				nodeCommentLabelTextField.normal.textColor = new Color( 1f, 1f, 1f, 0.3f );
			}
			return nodeCommentLabelTextField;
		}


		private static GUIStyle nodeNameLabelBackground;
		public static Color nodeNameLabelBackgroundColor = new Color( 0.7f, 0.7f, 0.7f );
		public static GUIStyle GetNodeNameLabelBackground() {
			if( nodeNameLabelBackground == null ) {
				nodeNameLabelBackground = new GUIStyle( EditorStyles.textField );
			}
			return nodeNameLabelBackground;
		}

		private static GUIStyle highlightStyle;
		public static GUIStyle HighlightStyle {
			get {
				if( highlightStyle == null ) {
					//if( Application.unityVersion.StartsWith("4") )
					highlightStyle = new GUIStyle( (GUIStyle)"flow node 0 on" );
				}
				return highlightStyle;
			}
		}


		private static GUIStyle selectionStyle;
		public static GUIStyle SelectionStyle {
			get {
				if( selectionStyle == null ) {
					//if( Application.unityVersion.StartsWith("4") )
					selectionStyle = new GUIStyle( (GUIStyle)"SelectionRect" );
				}
				return selectionStyle;
			}
		}

		private static GUIStyle nodeStyle;
		public static GUIStyle NodeStyle {
			get {
				if( nodeStyle == null ) {
					//if( Application.unityVersion.StartsWith( "4" ) )
					nodeStyle = new GUIStyle( (GUIStyle)"flow node 0" );
					nodeStyle.alignment = TextAnchor.UpperCenter;
					nodeStyle.fontSize = 11;
					nodeStyle.font = EditorStyles.standardFont;
					nodeStyle.fontStyle = FontStyle.Bold;
					nodeStyle.padding.top = 23;
					nodeStyle.padding.left = 1;
					//nodeStyle.margin.right = 8;
					//nodeStyle.border.right = 25;
					nodeStyle.border.left = 25;
					if(SF_GUI.ProSkin)
						nodeStyle.normal.textColor = new Color( 1f, 1f, 1f, 0.75f );
					else
						nodeStyle.normal.textColor = new Color( 0f, 0f, 0f, 0.7f );

				}

				return nodeStyle;
			}
		}


		private static GUIStyle nodeStyleDiscrete;
		public static GUIStyle NodeStyleDiscrete{
			get {
				if( nodeStyleDiscrete == null ) {
					nodeStyleDiscrete = new GUIStyle(NodeStyle);
					nodeStyleDiscrete.normal.textColor = SF_GUI.ProSkin ? new Color( 1f, 1f, 1f, 0.75f/5f ) : new Color( 0f, 0f, 0f, 0.7f/5f );
				}
				return nodeStyleDiscrete;
			}
		}
		//NodeStyleDiscrete


		private static GUIStyle toggleDiscrete;
		public static GUIStyle ToggleDiscrete {
			get {
				if( toggleDiscrete == null ) {
					toggleDiscrete = new GUIStyle( GUI.skin.toggle );
					toggleDiscrete.fontSize = 10;
				}
				
				return toggleDiscrete;
			}
		}



		// Thanks to Tenebrous!
		public static void ListStyles() {
			foreach( GUIStyle ss in GUI.skin.customStyles ) {
				GUILayout.Label( ss.name );
				EditorGUILayout.LabelField( ss.name, ss );
			}

			FieldInfo f = typeof( EditorGUIUtility ).GetField( "s_IconGUIContents", BindingFlags.NonPublic | BindingFlags.Static );
			Hashtable ff = (Hashtable)f.GetValue( null );
			foreach( DictionaryEntry fff in ff ) {
				GUILayout.Label( fff.Key.ToString() );
				GUILayout.Label( (GUIContent)fff.Value );
			}
		}



	}
}
