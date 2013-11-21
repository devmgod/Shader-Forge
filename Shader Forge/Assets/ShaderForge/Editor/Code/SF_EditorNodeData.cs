using UnityEngine;
using System.Collections;
using System;

namespace ShaderForge {

	[System.Serializable]
	public class SF_EditorNodeData : ScriptableObject {

		[SerializeField]
		KeyCode key;
		[SerializeField]
		bool holding = false; 
		[SerializeField]
		public string nodeName;
		[SerializeField]
		public string type;
		[SerializeField]
		public bool isNew = false;
		[SerializeField]
		public string fullPath;
		[SerializeField]
		public string category;
		[SerializeField]
		public bool isProperty = false;

		public SF_EditorNodeData() {

		}

		public void OnEnable() {
			base.hideFlags = HideFlags.HideAndDontSave;
		}

		public SF_EditorNodeData Initialize( string type, string fullPath, KeyCode key = KeyCode.None ) {
			holding = false;
			this.type = type;
			ParseCategoryAndName( fullPath );
			this.key = key;

			if( type.Contains( "SFN_Color" ) ||
				type.Contains( "SFN_Cubemap" ) ||
				type.Contains( "SFN_Slider" ) ||
				type.Contains( "SFN_Tex2d" ) ||
				type.Contains( "SFN_Tex2dAsset" ) ||
				type.Contains( "SFN_Vector4Property" ) ||
				type.Contains( "SFN_ValueProperty" )
				)
					isProperty = true;

			return this;
		}

		public void ParseCategoryAndName(string fullPath) {

			this.fullPath = fullPath;

			string[] split = fullPath.Split( '/' );
			if( split.Length > 1 ) {
				this.category = split[0];
				this.nodeName = split[1];
			} else {
				this.nodeName = fullPath;
			}

		}


		public SF_Node CreateInstance() {
			SF_Node node = (SF_Node)ScriptableObject.CreateInstance( Type.GetType( type ) );
			node.Initialize();
			return node;
		}

		public SF_EditorNodeData MarkAsNewNode() {
			isNew = true;
			return this;
		}


		public bool CheckHotkeyInput() {

			if( key == KeyCode.None )
				return false;

			if( Event.current.keyCode == key ) {
				if( Event.current.type == EventType.keyDown && !SF_GUI.HoldingControl() )
					holding = true;
				if( Event.current.type == EventType.keyUp )
					holding = false;
			}
			bool clicked = Event.current.type == EventType.mouseDown;
			return ( holding && clicked );
		}
	}
}