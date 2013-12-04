using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Xml;
using System.IO;

namespace ShaderForge {

	public enum ConnectionLineStyle { Bezier, Linear, Rectilinear };

	[System.Serializable]
	public class SF_EditorNodeView : ScriptableObject {

		SF_Editor editor;
		const int TOOLBAR_HEIGHT = 18;
		[SerializeField]
		Vector2 cameraPos = Vector3.zero;

		[SerializeField]
		bool panCamera = false;

		[SerializeField]
		Vector2 mousePosStart;
		public Rect rect;
		public GUIStyle toolbarStyle;



		public SF_SelectionManager selection;

		public SF_NodeTreeStatus treeStatus;



		// Settings:
		public bool autoRecompile = true;
		public bool hierarcyMove = false;
		public ConnectionLineStyle connectionLineStyle = ConnectionLineStyle.Bezier;


		public SF_EditorNodeView() {

		}

		public void OnEnable() {
			base.hideFlags = HideFlags.HideAndDontSave;
		}




		public SF_EditorNodeView Initialize( SF_Editor editor ) {
			this.editor = editor;
			selection = ScriptableObject.CreateInstance<SF_SelectionManager>().Initialize( editor );
			treeStatus = ScriptableObject.CreateInstance<SF_NodeTreeStatus>().Initialize(editor);
			rect = new Rect();
			cameraPos = new Vector2( 32768 - 400, 32768 - 300 );
			toolbarStyle = new GUIStyle( EditorStyles.toolbar );
			toolbarStyle.fixedHeight = TOOLBAR_HEIGHT;
			return this;
		}







		public void OnLocalGUI( Rect r ) {

			selection.OnGUI(); // To detect if you press things

			editor.mousePosition = Event.current.mousePosition;

			rect = r;

			

			// TOOLBAR
			DrawToolbar( new Rect( rect.x, rect.y, rect.width, TOOLBAR_HEIGHT ) );

			

			Rect localRect = new Rect( r );
			localRect.x = 0;
			localRect.y = 0;

			rect.y += TOOLBAR_HEIGHT;
			rect.height -= TOOLBAR_HEIGHT;


			// VIEW
			Rect rectInner = new Rect( rect );
			rectInner.width = float.MaxValue / 2f;
			rectInner.height = float.MaxValue / 2f;


			

			

			//GUI.BeginGroup( rect );
			cameraPos = GUI.BeginScrollView( rect, cameraPos, rectInner, GUIStyle.none, GUIStyle.none );
			//cameraPos = GUI.BeginScrollView(nodeAreaRect, cameraPos, viewRect, true, true );
			{

				

				//GUIStyle gs = EditorStyles.label;
				//gs.margin = new RectOffset( int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue );
				//gs.contentOffset = new Vector2( float.MaxValue / -2f, float.MaxValue / -2f ); 

				//GUI.Label( new Rect(0,0,2048,2048), string.Empty);
				if(Event.current.type == EventType.layout)
					GUILayout.Label( string.Empty, GUILayout.Width( float.MaxValue ), GUILayout.Height( float.MaxValue ) );

				// GUI.Box( new Rect( Screen.width, Screen.height, 32, 32 ), "BottomRight" );
				// GUI.Box( new Rect( Screen.width/2f, Screen.height/2f, 32, 32 ), "Center" );
				GUI.Box( new Rect( 0f, 0f, 32, 32 ), "TopLeft" );

				// NODES
				if( editor.nodes != null ) {

					//Event e = Event.current;
					//editor.BeginWindows();
					
					for(int i=0;i<editor.nodes.Count;i++) {
						if( !editor.nodes[i].Draw() )
							break;
					}
					

					//editor.EndWindows();
					
					for(int i=0;i<editor.nodes.Count;i++)
						editor.nodes[i].DrawConnectors();
				}

				

				

				// END NODES
				if( SF_Node.DEBUG )
					UpdateDebugInput();
				UpdateCameraPanning();

				

			}
			GUI.EndScrollView();







			if( Event.current.type == EventType.ContextClick ) {
				Vector2 mousePos = Event.current.mousePosition;
				if( rect.Contains( mousePos ) ) {
					// Now create the menu, add items and show it
					GenericMenu menu = new GenericMenu();
					for( int i = 0; i < editor.nodeTemplates.Count; i++ ) {
						menu.AddItem( new GUIContent( editor.nodeTemplates[i].fullPath ), false, ContextClick, editor.nodeTemplates[i] );
					}
					editor.ResetRunningOutdatedTimer();
					menu.ShowAsContext();
					Event.current.Use();
				}
			}






			if( Event.current.type == EventType.DragPerform ) {
				if( DragAndDrop.objectReferences[0] is Texture2D ) {
					SFN_Tex2d texNode = editor.nodeBrowser.OnStopDrag() as SFN_Tex2d;
					texNode.textureAsset = DragAndDrop.objectReferences[0] as Texture2D;
					texNode.OnAssignedTexture();
					Event.current.Use();
				}
			}

			if( Event.current.type == EventType.dragUpdated && Event.current.type != EventType.DragPerform ) {
				if( DragAndDrop.objectReferences.Length > 0 ) {
					if( DragAndDrop.objectReferences[0].GetType() == typeof( Texture2D ) ) {
						DragAndDrop.visualMode = DragAndDropVisualMode.Link;
						if( !editor.nodeBrowser.IsPlacing() )
							editor.nodeBrowser.OnStartDrag( editor.GetTemplate<SFN_Tex2d>() );
						else
							editor.nodeBrowser.UpdateDrag();
					} else {
						DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
					}
				} else {
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
				}
			}



			


			// If release
			if( MouseInsideNodeView( false ) && Event.current.type == EventType.mouseUp) {
				bool ifCursorStayed = Vector2.SqrMagnitude( mousePosStart - Event.current.mousePosition ) < SF_Tools.stationaryCursorRadius;

				if( ifCursorStayed && !SF_GUI.MultiSelectModifierHeld() )
					selection.DeselectAll();


				//editor.Defocus( deselectNodes: ifCursorStayed );
			}

			if( SF_GUI.ReleasedRawLMB() ) {
				SF_NodeConnection.pendingConnectionSource = null;
			}

			// If press
			if( Event.current.type == EventType.mouseDown && MouseInsideNodeView( false ) ) {
				//bool ifNotHoldingModifier = !SF_GUI.MultiSelectModifierHeld();
				mousePosStart = Event.current.mousePosition;
				editor.Defocus();
			}

		}

		



		


		public void CenterCamera() {

			// Find midpoint of all nodes
			Vector2 stp = editor.materialOutput.rect.center;
			Rect r = new Rect( stp.x, stp.y, 1f, 1f );
			foreach( SF_Node n in editor.nodes ) {
				r = SF_Tools.Encapsulate( r, n.rect );
			}
			
			// Move Camera
			cameraPos = r.center - new Vector2(Screen.width*0.5f,Screen.height*0.5f) -Vector2.right * editor.separatorLeft.rect.x;
		}




		public void ContextClick( object o ) {
			SF_EditorNodeData nodeData = o as SF_EditorNodeData;
			editor.AddNode( nodeData );
		}



		public void UpdateDebugInput() {

			if( Event.current.type != EventType.keyDown )
				return;

			if( Event.current.keyCode == KeyCode.UpArrow ) {
				HierarchalRefresh();
			}


			if( Event.current.keyCode == KeyCode.DownArrow ) {
				Debug.Log( GetNodeDataSerialized() );
			}


		}


		public void AssignDepthValuesToNodes() {
			foreach( SF_Node n in editor.nodes ) {
				n.depth = 0;
			}
			// Recurse some depth!
			// TODO: Run this for disconnected islands of nodes too
			//Debug.Log("SFN_FINAL exists = " + (editor.materialOutput != null));
			AddDepthToChildrenOf( editor.materialOutput, 0 );
		}

		void AddDepthToChildrenOf( SF_Node n, int carry ) {
			carry++;
			n.depth = Mathf.Max( carry, n.depth ); ;
			for( int i = 0; i < n.connectors.Length; i++ ) {
				if( n.connectors[i].conType == ConType.cOutput ) // Ignore outputs, we came from here!
					continue;
				if( !n.connectors[i].IsConnected() ) // Ignore unconnected inputs
					continue;
				AddDepthToChildrenOf( n.connectors[i].inputCon.node, carry );
			}
		}

		public void HierarchalRefresh() {

			AssignDepthValuesToNodes();

			int maxDepth = 0; // Deepest level
			foreach( SF_Node n in editor.nodes ) {
				if( maxDepth < n.depth )
					maxDepth = n.depth;
			}

			
			// Relink everything
			int depth = maxDepth;
			while( depth > 0 ) {
				for(int i=0; i<editor.nodes.Count; i++){
					SF_Node n = editor.nodes[i];
					if( n.depth == depth ) {
						n.OnUpdateNode(NodeUpdateType.Soft, cascade:true);
					}
				}
				depth--;
			}
			

			// Soft Update node previews
			/*
			depth = maxDepth;
			while( depth > 0 ) {
				foreach( SF_Node n in editor.nodes ) {
					if( n.depth == depth ) {
						//n.RefreshValue();
						//n.OnUpdateNode( NodeUpdateType.Soft );
					}
						
				}
				depth--;
			}
			 * */

		}


		public void ReconnectConnectedPending() {
			AssignDepthValuesToNodes();

			int maxDepth = 0; // Deepest level
			foreach( SF_Node n in editor.nodes ) {
				if( maxDepth < n.depth )
					maxDepth = n.depth;
			}


			int depth = maxDepth;
			while( depth > 0 ) {
				//foreach( SF_Node n in editor.nodes ) {
				for( int i = 0; i < editor.nodes.Count; i++ ) {
					SF_Node n = editor.nodes[i];
					if( n.depth == depth ) {
						foreach( SF_NodeConnection con in n.connectors ) {
							if( con.conType == ConType.cOutput )
								continue;
							if( !con.IsConnectedAndEnabled() )
								continue;
							if( con.valueType != ValueType.VTvPending )
								continue;
							con.inputCon.LinkTo( con, LinkingMethod.Default );
						}
					}

				}
				depth--;
			}
		}




		public string GetNodeDataSerialized() {

			// TODO; move parts of this to their respective places

			string header = "";
			header += "// Shader created with " + SF_Tools.versionString + " \n";
			header += "// Shader Forge (c) Joachim 'Acegikmo' Holmer\n";
			header += "// Note: Manually altering this data may prevent you from opening it in Shader Forge\n";
			header += "/" + "*"; // Hurgh!

			string sData = "";
			sData += "SF_DATA;"; // TODO: Multi-pass, shader settings etc
			sData += "ver:" + SF_Tools.version + ";";
			sData += "sub:START;";
			sData += "pass:START;";
			sData += editor.ps.Serialize() + ";";

			foreach( SF_Node node in editor.nodes )
				sData += node.Serialize(false,useSuffixPrefix:true);

			if(editor.nodeView.treeStatus.propertyList.Count > 0)
				sData += editor.nodeView.treeStatus.SerializeProps() + ";";

			string footer = "pass:END;sub:END;";
			footer += "*" + "/";
			return ( header + sData + footer );
		}

		public float lastChangeTime;

		float GetTime(){
			return (float)EditorApplication.timeSinceStartup;
		}

		public float GetTimeSinceChanged(){
			return GetTime() - lastChangeTime;
		}
		
		private void DrawRecompileTimer(Rect r){

			float delta = GetTimeSinceChanged();

			if(delta > 1.12f)
				return;

			r.width *= Mathf.Clamp01(delta);
			GUI.Box(r,string.Empty);
			GUI.Box(r,string.Empty);
			GUI.Box(r,string.Empty);
		}

		void DrawToolbar( Rect r ) {
			GUI.color = Color.white;
			GUI.Box( r, "", toolbarStyle );
			r.x += 6;

			r.width = 108;

		
			GUI.color = SF_GUI.outdatedStateColors[(int)editor.ShaderOutdated];
			if( GUI.Button( r, "Compile shader", EditorStyles.toolbarButton ) ) {
				if(treeStatus.CheckCanCompile())
					editor.shaderEvaluator.Evaluate();
			}
			GUI.color = Color.white;

			DrawRecompileTimer(r);

			r.x += r.width + 4;
			r.width = 100;
			autoRecompile = GUI.Toggle( r, autoRecompile, "Auto-compile" );
			
			r.x += r.width + 20;
			r.width = 140;
			hierarcyMove = GUI.Toggle( r, hierarcyMove, "Hierarchal Node Move" );
			r.x += r.width + 20;
			r.width = 60;
			//GUI.Label( r, "Con. style:", EditorStyles.miniLabel );
			//r.x += r.width + 2;
			connectionLineStyle = (ConnectionLineStyle)EditorGUI.EnumPopup( r, connectionLineStyle, EditorStyles.toolbarPopup);


			//GUILayout.FlexibleSpace();

			r.x += r.width + 20;
			GUI.color = new Color(0.8f,1f,0.8f,1f);
			r.width = 110;
			SF_Tools.LinkButton( r, SF_Tools.manualLabel, SF_Tools.manualURL, EditorStyles.toolbarButton );
			r.x += r.width + 2;
			r.width = 120;
			SF_Tools.LinkButton( r, SF_Tools.bugReportLabel, SF_Tools.bugReportURL, EditorStyles.toolbarButton );
			r.x += r.width + 2;
			r.width = 80;
			SF_Tools.LinkButton( r, SF_Tools.featureListLabel, SF_Tools.featureListURL, EditorStyles.toolbarButton );
			GUI.color = Color.white;

		}

		void UpdateCameraPanning() {



			if( SF_GUI.ReleasedCameraMove() ) {
				panCamera = false;
			}

			bool insideNodeView = MouseInsideNodeView( true );
			bool dragging = ( Event.current.type == EventType.MouseDrag && panCamera );
			bool connecting = SF_NodeConnection.IsConnecting();
			bool rotatingPreview = editor.preview.isDragging;
			bool placingNode = editor.nodeBrowser.IsPlacing();
			bool draggingSeparators = editor.DraggingAnySeparator();


			bool doingSomethingElse = connecting || rotatingPreview || placingNode || draggingSeparators;
			bool dragInside = dragging && insideNodeView;

			if( dragInside && !doingSomethingElse ) {

				//if( !SF_GUI.MultiSelectModifierHeld() )
				//	selection.DeselectAll();
				//Debug.Log("Delta: " + Event.current.delta);
				cameraPos -= Event.current.delta;
				editor.Defocus();
				//Debug.Log( "USING" );
				Event.current.Use();
			}


			if( SF_GUI.PressedCameraMove() ) {
				panCamera = true;
			}

		

		}

		public Vector2 GetNodeSpaceMousePos() {
			return SubtractNodeWindowOffset( Event.current.mousePosition );
		}


		bool MouseInsideNodeView( bool offset = false ) {

			if( offset ) {
				return rect.Contains( AddNodeWindowOffset( Event.current.mousePosition ) );
			} else {
				return rect.Contains( Event.current.mousePosition );
			}

		}


		public Vector2 AddNodeWindowOffset( Vector2 in_vec ) {
			return in_vec - cameraPos;
		}
		public Rect AddNodeWindowOffset( Rect in_rect ) {
			in_rect.x += -cameraPos.x;
			in_rect.y += -cameraPos.y;
			return in_rect;
		}
		public Vector2 SubtractNodeWindowOffset( Vector2 in_vec ) {
			return in_vec + cameraPos;
		}
		public Rect SubtractNodeWindowOffset( Rect in_rect ) {
			in_rect.x -= -cameraPos.x;
			in_rect.y -= -cameraPos.y;
			return in_rect;
		}


		/*
		float zoomAbsolute = 1f;
		float zoom = 1f;
		public void UpdateCameraZoomInput() {
			if( Event.current.type == EventType.ScrollWheel ) {
				zoomAbsolute = Mathf.Clamp(zoomAbsolute - Event.current.delta.y*0.025f, 0.1f,1f );
			}
		}

		public void UpdateCameraZoomValue() {
			zoom = Mathf.Lerp( zoom, zoomAbsolute, 0.05f);
		}
		*/


	}

}

