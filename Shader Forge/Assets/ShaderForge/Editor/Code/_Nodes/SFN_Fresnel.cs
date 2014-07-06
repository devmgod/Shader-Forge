using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ShaderForge {

	[System.Serializable]
	public class SFN_Fresnel : SF_Node {


		public SFN_Fresnel() {

		}

		public override void Initialize() { 
			base.Initialize( "Fresnel" , vectorDataTexture:true );
			base.showColor = true;
			base.UseLowerPropertyBox( false );
			base.texture.CompCount = 1;
			connectors = new SF_NodeConnector[]{
				SF_NodeConnector.Create(this,"OUT","",ConType.cOutput,ValueType.VTv1,false),
				SF_NodeConnector.Create(this,"NRM","Nrm",ConType.cInput,ValueType.VTv3,false),
				SF_NodeConnector.Create(this,"EXP","Exp",ConType.cInput,ValueType.VTv1,false)
			};

			this["NRM"].unconnectedEvaluationValue = "normalDirection";


		}

		public override string Evaluate( OutChannel channel = OutChannel.All ) {

			string dot = "1.0-max(0,dot(" + this["NRM"].TryEvaluate() + ", viewDirection))";

			if( GetInputIsConnected( "EXP" ) ) {
				return "pow(" + dot + "," + this["EXP"].TryEvaluate() + ")";
			}
			return "("+dot+")";

		}

	}
}