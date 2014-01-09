using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ShaderForge {

	[System.Serializable]
	public class SFN_ValueProperty : SF_Node {


		public SFN_ValueProperty() {

		}

		public override void Initialize() {
			node_height = 24;
			//node_width = (int)(NODE_WIDTH*1.25f);
			base.Initialize( "Value" );
			lowerRect.y -= 8;
			lowerRect.height = 28;
			base.showColor = false;
			base.UseLowerPropertyBox( true );
			base.texture.uniform = true;
			base.texture.CompCount = 1;

			property = ScriptableObject.CreateInstance<SFP_ValueProperty>().Initialize( this );

			connectors = new SF_NodeConnector[]{
				SF_NodeConnector.Create(this,"OUT","",ConType.cOutput,ValueType.VTv1,false)
			};
		}

		public override bool IsUniformOutput() {
			return true;
		}

		public override string Evaluate( OutChannel channel = OutChannel.All ) {
			return property.GetVariable();
		}


		public override void DrawLowerPropertyBox() {
			PrepareWindowColor();
			float vecPrev = texture.dataUniform[0];
			//int strWidth = (int)SF_Styles.GetLargeTextField().CalcSize( new GUIContent( texture.dataUniform[0].ToString() ) ).x;
			//lowerRect.width = Mathf.Max( 32, strWidth );
			Rect r = new Rect( lowerRect );
			r.width -= 32;
			r.yMin += 4;
			r.yMax -= 2;
			r.xMin += 2;
			float fVal = EditorGUI.FloatField( r, texture.dataUniform[0], SF_Styles.LargeTextField );
			r.x += r.width + 6;
			r.width = r.height;
			Rect texCoords = new Rect( r );
			texCoords.width /= 7;
			texCoords.height /= 3;
			texCoords.x = texCoords.y = 0;
			GUI.DrawTextureWithTexCoords( r, SF_GUI.Handle_drag, texCoords, alphaBlend:true );

			texture.dataUniform = new Color( fVal, fVal, fVal, fVal );
			if( texture.dataUniform[0] != vecPrev ) {
				OnUpdateNode( NodeUpdateType.Soft );
				editor.shaderEvaluator.ApplyProperty( this );
			}

			ResetWindowColor();
				
		}

		public override string SerializeSpecialData() {
			return "v1:" + texture.dataUniform[0];
		}

		public override void DeserializeSpecialData( string key, string value ) {
			switch( key ) {
				case "v1":
					float fVal = float.Parse( value );
					texture.dataUniform = new Color( fVal, fVal, fVal, fVal );
					break;
			}
		}



	}
}