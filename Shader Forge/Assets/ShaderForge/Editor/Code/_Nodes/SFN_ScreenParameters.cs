using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ShaderForge {

	[System.Serializable]
	public class SFN_ScreenParameters : SF_Node {


		public SFN_ScreenParameters() {

		}

		public override void Initialize() {
			base.Initialize( "Scrn. Params" );
			base.SearchName = "Screen Parameters";
			base.showColor = true;
			base.UseLowerPropertyBox( false );
			base.texture.icon = Resources.LoadAssetAtPath( SF_Paths.pInterface+"Nodes/screen_parameters.png", typeof( Texture2D ) ) as Texture2D;
			base.texture.CompCount = 4;
			connectors = new SF_NodeConnection[]{
				SF_NodeConnection.Create(this,"PXW","pxW",ConType.cOutput,ValueType.VTv1,false).Outputting(OutChannel.R),
				SF_NodeConnection.Create(this,"PXH","pxH",ConType.cOutput,ValueType.VTv1,false).Outputting(OutChannel.G),
				SF_NodeConnection.Create(this,"RCW","1+1/W",ConType.cOutput,ValueType.VTv1,false).Outputting(OutChannel.B),
				SF_NodeConnection.Create(this,"RCH","1+1/H",ConType.cOutput,ValueType.VTv1,false).Outputting(OutChannel.A)
			};
			base.extraWidthOutput = 12;
		}

		public override Color NodeOperator( int x, int y ) {
			return new Color( 0f, 0.7071068f, 0.7071068f, 0f );
		}

		public override string Evaluate( OutChannel channel = OutChannel.All ) {
			return "_ScreenParams";
		}

	}
}