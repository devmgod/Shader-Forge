using UnityEngine;
using System.Collections;


namespace ShaderForge {
	public class SF_ErrorEntry {

		public SF_Node node;
		public SF_NodeConnector con;
		public string error;


		public SF_ErrorEntry(string error, SF_Node target) {
			node = target;
			con = null;
			this.error = error;
		}

		public SF_ErrorEntry( string error, SF_NodeConnector target ) {
			con = target;
			node = target.node;
			this.error = error;
		}

	}

}
