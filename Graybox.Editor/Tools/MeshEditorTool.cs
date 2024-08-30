
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Editor.Documents;
using Graybox.Graphics.Immediate;
using Graybox.Scenes;
using Graybox.Scenes.Drawing;
using ImGuiNET;

namespace Graybox.Editor.Tools;

internal class MeshEditorTool : BaseTool
{

	public override string Name => "Mesh Editor Tool";
	public override string EditorIcon => "assets/icons/tool_mesheditor.png";

	public PivotModes PivotMode { get; set; } = PivotModes.FirstSelected;
	public PickingModes PickingMode { get; set; } = PickingModes.Face;
	public PivotSpaces PivotSpace { get; set; } = PivotSpaces.Global;

	static Color4 NormalColor => Color4.White;
	static Color4 HoveredColor => Color4.Cyan;
	static Color4 SelectedColor => Color4.Yellow;

	bool WantsToExtrude => PickingMode == PickingModes.Face && Input.ShiftModifier;

	bool TransformationIsValid;
	Vector3 PivotOrigin;
	Vector3 PivotForward;
	Solid.UniqueElement hoveredElement;
	List<Solid> selectedSolids = new();
	HashSet<Solid.UniqueElement> selectedElements = new();
	bool isDragging;
	bool mouseIsDown;
	Vector2 dragBegin;
	Vector2 dragEnd;
	int selectionHash;

	Vector3 multiExtrudeAngle = default;
	int multiExtrudeSegments = 4;
	int multiExtrudeDistance = 64;

	public override void ToolSelected( bool preventHistory )
	{
		base.ToolSelected( preventHistory );

		selectionHash = 0;
	}

	public override void UpdateFrame( Scene scene, FrameInfo frame )
	{
		base.UpdateFrame( scene, frame );

		if ( Document == null ) return;

		var newHash = Document.Selection.GetSelectionHash();
		if ( selectionHash != newHash )
		{
			selectionHash = newHash;
			RefreshSelectedSolids();
			UpdatePivot();
		}
	}

	public override void MouseDown( Scene scene, ref InputEvent e )
	{
		base.MouseDown( scene, ref e );

		mouseIsDown = true;
		dragBegin = e.LocalMousePosition;

		var ray = scene.ScreenToRay( e.LocalMousePosition );
		var trace = scene.Physics.Trace<Solid>( ray );
		if ( trace.Hit && trace.Object is Solid s )
		{
			if ( hoveredElement == null )
			{
				Select( new[] { s }, e.Modifiers );
				return;
			}
		}

		if ( hoveredElement == null && !e.Control )
		{
			ClearSelection();
			e.Handled = true;
			return;
		}

		if ( hoveredElement != null )
		{
			if ( !e.Control )
			{
				ClearSelection();
			}
			SelectElement( hoveredElement );
			e.Handled = true;
			return;
		}
	}

	public override void KeyDown( Scene scene, ref InputEvent e )
	{
		base.KeyDown( scene, ref e );

		if ( e.Key == Key.Escape && selectedElements.Count > 0 )
		{
			ClearSelection();
			mouseIsDown = false;
			isDragging = false;
			e.Handled = true;
		}
	}

	public override void MouseDoubleClick( Scene scene, ref InputEvent e )
	{
		base.MouseDoubleClick( scene, ref e );

	}

	public override void MouseMove( Scene scene, ref InputEvent e )
	{
		base.MouseMove( scene, ref e );

		hoveredElement = GetHoveredElement( scene, e );

		if ( mouseIsDown )
		{
			var dragDist = Vector2.Distance( e.LocalMousePosition, dragBegin );
			if ( dragDist > 5 )
			{
				isDragging = true;
			}
			dragEnd = e.LocalMousePosition;
		}
	}

	public override void MouseUp( Scene scene, ref InputEvent e )
	{
		base.MouseUp( scene, ref e );

		mouseIsDown = false;
		isDragging = false;
	}

	public override void Render( Scene scene )
	{
		base.Render( scene );

		if ( scene.Gizmos.IsHovered || scene.Gizmos.IsCaptured )
		{
			hoveredElement = null;
		}

		UpdatePivot();

		switch ( PickingMode )
		{
			case PickingModes.Vertex:
				RenderVertices( scene );
				break;
			case PickingModes.Edge:
				RenderEdges();
				break;
			case PickingModes.Face:
				RenderFaces();
				break;
		}

		//if ( isDragging )
		//{
		//	using ( var scr = new DrawToScreen( scene.Width, scene.Height ) )
		//	{
		//		var scrMin = dragBegin;
		//		var scrMax = dragEnd;

		//		scrMin.Y = scene.Height - scrMin.Y;
		//		scrMax.Y = scene.Height - scrMax.Y;

		//		GL.Color4( System.Drawing.Color.FromArgb( 128, System.Drawing.Color.DodgerBlue ) ); // Set color with transparency
		//		GL.Begin( PrimitiveType.Quads ); // Draw filled rectangle
		//		GL.Vertex2( scrMin.X, scrMin.Y );
		//		GL.Vertex2( scrMax.X, scrMin.Y );
		//		GL.Vertex2( scrMax.X, scrMax.Y );
		//		GL.Vertex2( scrMin.X, scrMax.Y );
		//		GL.End();

		//		GL.Color4( System.Drawing.Color.DodgerBlue ); // Set color for outline
		//		GL.Begin( PrimitiveType.LineLoop ); // Draw outline
		//		GL.Vertex2( scrMin.X, scrMin.Y );
		//		GL.Vertex2( scrMax.X, scrMin.Y );
		//		GL.Vertex2( scrMax.X, scrMax.Y );
		//		GL.Vertex2( scrMin.X, scrMax.Y );
		//		GL.End();
		//	}
		//}

		if ( selectedElements.Count > 0 )
		{
			if ( !scene.Camera.Orthographic )
			{
				RenderTranslateGizmo( PivotOrigin, PivotForward.ForwardToRotation(), scene );
			}
		}
	}

	public override void UpdateWidget()
	{
		base.UpdateWidget();

		var objectIcon = EditorResource.Image( "assets/icons/picker_object.png" );
		var vertexIcon = EditorResource.Image( "assets/icons/picker_vertex.png" );
		var faceIcon = EditorResource.Image( "assets/icons/picker_face.png" );
		var edgeIcon = EditorResource.Image( "assets/icons/picker_edge.png" );

		var localIcon = EditorResource.Image( "assets/icons/widget_local.png" );
		var globalIcon = EditorResource.Image( "assets/icons/widget_global.png" );

		var options2 = new List<(string Label, PivotSpaces Space)>()
		{
			new ( "Global", PivotSpaces.Global ),
			new ( "Local", PivotSpaces.Local ),
		};
		ImGuiEx.Header( "Picking Space" );
		ImGuiEx.TextButtonGroup( options2, () => PivotSpace, x => PivotSpace = x );

		var options = new List<(string Tooltip, nint Icon, PickingModes Mode)>()
		{
			//new ( "Object", objectIcon, PickingModes.Object ),
			new ( "Vertex", vertexIcon, PickingModes.Vertex ),
			new ( "Face", faceIcon, PickingModes.Face ),
			new ( "Edge", edgeIcon, PickingModes.Edge ),
		};

		ImGuiEx.Header( "Picking Mode", true );
		ImGuiEx.ImageButtonGroup( options, () => PickingMode, x => PickingMode = x );

		var options3 = new List<(string Label, PivotModes Mode)>()
		{
			new ( "Center", PivotModes.Center ),
			new ( "First", PivotModes.FirstSelected ),
			new ( "Last", PivotModes.LastSelected ),
		};
		ImGuiEx.Header( "Pivot Mode", true );
		ImGuiEx.TextButtonGroup( options3, () => PivotMode, x => PivotMode = x );

		ImGuiEx.Header( "Operations", true );
		if ( PickingMode == PickingModes.Face )
		{
			if ( ImGui.BeginChild( "MultiExtrudeSection" ) )
			{
				ImGui.Text( "Multi-Extrude" );
				ImGui.Separator();
				ImGui.Spacing();

				ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 5, 5 ) );

				// Segments input
				ImGui.AlignTextToFramePadding();
				ImGui.Text( "Segments:" );
				ImGui.SameLine( 100 );
				if ( ImGui.InputInt( "##Segments", ref multiExtrudeSegments, 1, 5 ) )
				{
					multiExtrudeSegments = Math.Max( 1, multiExtrudeSegments );
				}

				// Distance input
				ImGui.AlignTextToFramePadding();
				ImGui.Text( "Distance:" );
				ImGui.SameLine( 100 );
				if ( ImGui.InputInt( "##Distance", ref multiExtrudeDistance, 1, 1 ) )
				{
					multiExtrudeDistance = MathHelper.Clamp( multiExtrudeDistance, 16, 512 );
				}

				// Angle input (Vector3)
				var angle = new SVector3( multiExtrudeAngle.X, multiExtrudeAngle.Y, multiExtrudeAngle.Z );
				ImGui.AlignTextToFramePadding();
				ImGui.Text( "Angle:" );
				ImGui.SameLine( 100 );
				ImGui.SetNextItemWidth( 200 );
				if ( ImGui.DragFloat3( "##Angle", ref angle, .1f, 0, 180 ) )
				{
					multiExtrudeAngle = new( angle.X, angle.Y, angle.Z );
				}

				ImGui.PopStyleVar();

				ImGui.Spacing();

				// Extrude button
				if ( ImGui.Button( "Multi-Extrude" ) )
				{
					ApplyMultiExtrude();
				}

				ImGui.Spacing();
				ImGui.Separator();

				ImGui.EndChild();
			}
		}
	}

	private void RenderTransientSolids( Matrix4 transformation )
	{
		var transientSolids = ApplyTransientTransformation( transformation );

		if ( transientSolids != null )
		{
			var concaveSolids = transientSolids.Where( s => !s.IsConvex( 0.5f ) );
			var convexSolids = transientSolids.Where( s => !concaveSolids.Contains( s ) );

			if ( concaveSolids.Any() )
			{
				var faces = concaveSolids.SelectMany( s => s.Faces );
				MapObjectRenderer.DrawFilledNoFucks( faces, new ColorPulse( System.Drawing.Color.FromArgb( 255, System.Drawing.Color.Salmon ) ), false, false, 0.5f );
				GL.Color3( 1.0f, 0f, 0f );
				MapObjectRenderer.DrawWireframe( faces, true, false );
				TransformationIsValid = true;
			}
			else
			{
				TransformationIsValid = true;
			}

			if ( convexSolids.Any() )
			{
				var faces = convexSolids.SelectMany( s => s.Faces );
				MapObjectRenderer.DrawFilledNoFucks( faces, new ColorPulse( System.Drawing.Color.FromArgb( 125, System.Drawing.Color.DodgerBlue ) ), false, false, 0.1f );
				GL.Color3( 1.0f, 1.0f, 1.0f );
				MapObjectRenderer.DrawWireframe( faces, true, false );
			}
		}
	}

	private void RenderExtrudedSolid( Matrix4 transformation )
	{
		var face = selectedElements.First()?.Face;
		if ( face?.Parent == null ) return;

		var extrusionVector = transformation.ExtractTranslation();
		var extrusionDistance = extrusionVector.Length;
		var extrudedSolid = face.Parent.Extrude( face, extrusionDistance, default, new() );
		var extrudedFaces = extrudedSolid.Item1.Faces;

		MapObjectRenderer.DrawFilledNoFucks( extrudedFaces, new ColorPulse( System.Drawing.Color.FromArgb( 125, System.Drawing.Color.Yellow ) ), false, false, 0.1f );
		GL.Color3( 1.0f, 1.0f, 1.0f );
		MapObjectRenderer.DrawWireframe( extrudedFaces, true, false );
	}

	protected override void OnTranslate( SceneGizmos.TranslateEvent e )
	{
		base.OnTranslate( e );

		var translationVector = e.NewPosition - e.StartPosition;

		if ( !e.TranslateMatrix.Determinant.IsNearlyZero() )
		{
			var translateMatrix = e.TranslateMatrix;
			var translateAxis = e.TranslateAxis;
			var translateNormal = Vector3.TransformNormal( translateAxis, translateMatrix ).Normalized();
			translationVector = SnapIfNeeded( translationVector, translateNormal );
		}
		else
		{
			translationVector = SnapIfNeeded( translationVector );
		}

		var transformation = Matrix4.CreateTranslation( translationVector );

		if ( !e.Completed )
		{
			if ( WantsToExtrude )
			{
				RenderExtrudedSolid( transformation );
			}
			else
			{
				RenderTransientSolids( transformation );
			}
		}
		else
		{
			if ( WantsToExtrude )
			{
				ApplyExtrude( translationVector.Length, default );
			}
			else
			{
				ApplyTransformation( transformation );
			}
		}
	}

	public void SelectElement( Solid.UniqueElement element )
	{
		if ( element != null && element.Type == GetCurrentElementType() )
		{
			selectedElements.Add( element );
		}

		UpdatePivot();
	}

	void UpdatePivot()
	{
		if ( selectedElements.Count == 0 ) return;

		if ( PivotMode == PivotModes.FirstSelected )
		{
			SetPivot( selectedElements.First() );
		}

		if ( PivotMode == PivotModes.LastSelected )
		{
			SetPivot( selectedElements.Last() );
		}

		if ( PivotMode == PivotModes.Center )
		{
			if ( selectedElements.Count == 1 )
			{
				SetPivot( selectedElements.First() );
				return;
			}
			var origin = Vector3.Zero;
			foreach ( var element in selectedElements )
			{
				origin += element.CalculateCenter();
			}
			origin /= selectedElements.Count;
			PivotOrigin = origin;
			PivotForward = Vector3.UnitX;
		}
	}

	void SetPivot( Solid.UniqueElement e )
	{
		switch ( e.Type )
		{
			case Solid.UniqueElement.ElementType.Vertex:
				PivotOrigin = e.Vertices[0].Position;
				PivotForward = e.Face.Plane.Normal;
				break;
			case Solid.UniqueElement.ElementType.Face:
				PivotOrigin = e.Face.CalculateCenter();
				PivotForward = e.Face.Plane.Normal;
				break;
			case Solid.UniqueElement.ElementType.Edge:
				PivotOrigin = (e.Vertices[0].Position + e.Vertices[1].Position) / 2;
				PivotForward = e.Face.Plane.Normal;
				break;
		}

		if ( PivotSpace == PivotSpaces.Global )
		{
			PivotForward = Vector3.UnitX;
		}
	}

	public void DeselectElement( Solid.UniqueElement element )
	{
		selectedElements.Remove( element );
	}

	public void ClearSelection()
	{
		selectedElements.Clear();
	}

	public void ApplyTransformation( Matrix4 transformation )
	{
		if ( selectedElements.Count == 0 || selectedSolids.Count == 0 )
		{
			return;
		}
		var action = new VertexEditAction( selectedElements, transformation, selectedSolids );
		Document?.PerformAction( "Edit Mesh", action );
		UpdatePivot();
	}

	public void ApplyExtrude( float distance, Vector3 euler )
	{
		var face = selectedElements.First()?.Face;
		if ( face?.Parent == null ) return;

		var extrudedSolid = face.Parent.Extrude( face, distance, euler, Document.Map.IDGenerator );

		var create = new Create( face.Parent.Parent.ID, extrudedSolid.Item1 );
		var select = new Select( new[] { extrudedSolid.Item1 } );
		var ac = new ActionCollection( create, select );

		Document.PerformAction( "Extrude Face", ac );

		extrudedSolid.Item1.InitializeUniqueVertices();
		var element = extrudedSolid.Item1.GetUniqueElements( Solid.UniqueElement.ElementType.Face ).FirstOrDefault( x => x.Face == extrudedSolid.Item2 );
		if ( element != null )
		{
			ClearSelection();
			SelectElement( element );
		}
	}

	void ApplyMultiExtrude()
	{
		var face = selectedElements.First()?.Face;
		if ( face?.Parent == null ) return;

		var segments = multiExtrudeSegments;
		var angle = multiExtrudeAngle;
		var dist = multiExtrudeDistance;

		for ( int i = 0; i < segments; i++ )
		{
			ApplyExtrude( dist, angle );
		}
	}

	public List<Solid> ApplyTransientTransformation( Matrix4 transformation )
	{
		var transientSolids = new List<Solid>();
		var processedVertices = new HashSet<Solid.UniqueVertex>();

		foreach ( var originalSolid in selectedSolids )
		{
			var transientSolid = originalSolid.CreateTransientCopy();
			transientSolids.Add( transientSolid );

			var selectedElementsForSolid = selectedElements.Where( e => e.Face?.Parent == originalSolid );

			foreach ( var element in selectedElementsForSolid )
			{
				foreach ( var originalVertex in element.Vertices )
				{
					// Find the corresponding vertex in the transient solid
					var transientVertex = transientSolid.GetAllUniqueVertices()
						.FirstOrDefault( v => v.Position == originalVertex.Position );

					if ( transientVertex != null && processedVertices.Add( transientVertex ) )
					{
						var newPosition = Vector3.TransformPosition( transientVertex.Position, transformation );
						transientSolid.UpdateUniqueVertexPosition( transientVertex, newPosition );
					}
				}
			}

			transientSolid.Refresh();
		}

		return transientSolids;
	}

	private void RefreshSelectedSolids()
	{
		selectedSolids = Document.Selection.GetSelectedObjects().OfType<Solid>().ToList();
		foreach ( var solid in selectedSolids )
		{
			solid.InitializeUniqueVertices();
		}
	}

	private Solid.UniqueElement.ElementType GetCurrentElementType()
	{
		return PickingMode switch
		{
			PickingModes.Vertex => Solid.UniqueElement.ElementType.Vertex,
			PickingModes.Edge => Solid.UniqueElement.ElementType.Edge,
			PickingModes.Face => Solid.UniqueElement.ElementType.Face,
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	void RenderVertices( Scene scene )
	{
		var verts = selectedSolids.SelectMany( s => s.GetUniqueElements( Solid.UniqueElement.ElementType.Vertex ) );

		GL.Enable( EnableCap.DepthTest );
		GL.Enable( EnableCap.CullFace );

		foreach ( var vert in verts )
		{
			var sz = GetScaledSize( scene, vert.Vertices[0].Position, 8.0f );
			var isSelected = selectedElements.Contains( vert );
			var isHovered = hoveredElement == vert;
			var color = isSelected ? SelectedColor : isHovered ? HoveredColor : NormalColor;

			GL.Color4( color );
			RenderCube( vert.Vertices[0].Position, sz );
		}
	}

	void RenderEdges()
	{
		var edges = selectedSolids.SelectMany( s => s.GetUniqueElements( Solid.UniqueElement.ElementType.Edge ) );

		GL.LineWidth( 3.0f );
		GL.Color4( NormalColor );
		GL.Begin( PrimitiveType.Lines );

		foreach ( var edge in edges )
		{
			var isSelected = selectedElements.Contains( edge );
			var isHovered = hoveredElement == edge;
			var color = isSelected ? SelectedColor : isHovered ? HoveredColor : NormalColor;

			GL.Color4( color );
			GL.Vertex3( edge.Vertices[0].Position );
			GL.Vertex3( edge.Vertices[1].Position );
		}

		GL.End();
	}

	Solid.UniqueElement GetHoveredElement( Scene scene, InputEvent e )
	{
		switch ( PickingMode )
		{
			case PickingModes.Edge:
				return TraceForEdge( scene, e );
			case PickingModes.Vertex:
				return TraceForVertex( scene, e );
			case PickingModes.Face:
				return TraceForFace( scene, e );
		}

		return null;
	}

	Solid.UniqueElement TraceForFace( Scene scene, InputEvent e )
	{
		var ray = scene.ScreenToRay( e.LocalMousePosition );
		var line = new Line( ray.Origin, ray.Origin + ray.Direction * 100000 );
		var trace = scene.Physics.Trace<Solid>( ray );

		if ( trace.Tag is Face hitFace )
		{
			foreach ( var face in selectedSolids.SelectMany( s => s.GetUniqueElements( Solid.UniqueElement.ElementType.Face ) ) )
			{
				if ( face.Face == hitFace )
					return face;
			}
		}

		return null;
	}

	Solid.UniqueElement TraceForEdge( Scene scene, InputEvent e )
	{
		var ray = scene.ScreenToRay( e.LocalMousePosition );
		var line = new Line( ray.Origin, ray.Origin + ray.Direction * 100000 );
		var closestEdge = (Solid.UniqueElement)null;
		var closestDistance = float.MaxValue;

		foreach ( var edge in selectedSolids.SelectMany( x => x.GetUniqueElements( GetCurrentElementType() ) ) )
		{
			var edgeLine = new Line( edge.Vertices[0].Position, edge.Vertices[1].Position );
			var sz = GetScaledSize( scene, edge.Vertices[0].Position, 5.0f );
			Vector3? intersectionPoint = line.GetIntersectionPointFinite( edgeLine, sz );

			if ( intersectionPoint.HasValue )
			{
				float distance = Vector3.Distance( line.Start, intersectionPoint.Value );
				if ( distance < closestDistance )
				{
					closestEdge = edge;
					closestDistance = distance;
				}
			}
		}

		return closestEdge;
	}

	Solid.UniqueElement TraceForVertex( Scene scene, InputEvent e )
	{
		var ray = scene.ScreenToRay( e.LocalMousePosition );
		var line = new Line( ray.Origin, ray.Origin + ray.Direction * 100000 );
		var closestVertex = (Solid.UniqueElement)null;
		var closestDistance = float.MaxValue;

		foreach ( var solid in selectedSolids )
		{
			foreach ( var v in solid.GetUniqueElements( Solid.UniqueElement.ElementType.Vertex ) )
			{
				var size = GetScaledSize( scene, v.Vertices[0].Position, 10.0f );
				var mins = v.Vertices[0].Position - new Vector3( size, size, size );
				var maxs = v.Vertices[0].Position + new Vector3( size, size, size );
				var box = new Box( mins, maxs );
				if ( box.IntersectsWith( line ) )
				{
					float distance = Vector3.Distance( line.Start, v.Vertices[0].Position );
					if ( distance < closestDistance )
					{
						closestVertex = v;
						closestDistance = distance;
					}
				}
			}
		}
		return closestVertex;
	}

	private float GetScaledSize( Scene scene, Vector3 position, float baseSize )
	{
		if ( scene.Camera.Orthographic )
		{
			return baseSize * scene.Camera.OrthographicZoom;
		}
		else
		{
			float distanceToCamera = Vector3.Distance( scene.Camera.Position, position );
			return baseSize * distanceToCamera / 500f;
		}
	}

	void RenderFaces()
	{
		var faces = selectedSolids.SelectMany( s => s.GetUniqueElements( Solid.UniqueElement.ElementType.Face ) );
		var mapFaces = new List<Face>();
		var hoveredFace = (Face)null;
		var selectedFaces = new List<Face>();

		foreach ( var s in selectedSolids )
		{
			mapFaces.AddRange( s.Faces );
		}

		foreach ( var f in mapFaces )
		{
			if ( f == hoveredElement?.Face )
			{
				hoveredFace = f;
			}

			if ( selectedElements.Any( x => x.Face?.ID == f.ID ) )
			{
				selectedFaces.Add( f );
			}
		}

		GL.Color4( NormalColor );
		MapObjectRenderer.DrawWireframe( mapFaces, true, false );

		if ( selectedFaces.Count > 0 )
		{
			GL.Color4( SelectedColor );
			MapObjectRenderer.DrawFilledNoFucks( selectedFaces, System.Drawing.Color.FromArgb( 75, (byte)(SelectedColor.R * 255), (byte)(SelectedColor.G * 255), (byte)(SelectedColor.B * 255) ), false, false, 0.1f );
		}

		if ( hoveredFace != null )
		{
			GL.Color4( HoveredColor );
			MapObjectRenderer.DrawFilledNoFucks( new[] { hoveredFace }, System.Drawing.Color.FromArgb( 75, (byte)(HoveredColor.R * 255), (byte)(HoveredColor.G * 255), (byte)(HoveredColor.B * 255) ), false, false, 0.1f );
		}
	}

	private void RenderCube( Vector3 position, float scale )
	{
		float x = position.X;
		float y = position.Y;
		float z = position.Z;
		float s = scale * 0.5f;  // Half the scale since our cube is defined from -0.5 to 0.5

		GL.Begin( PrimitiveType.Quads );

		// Front face
		GL.Vertex3( x - s, y + s, z + s );
		GL.Vertex3( x + s, y + s, z + s );
		GL.Vertex3( x + s, y - s, z + s );
		GL.Vertex3( x - s, y - s, z + s );

		// Back face
		GL.Vertex3( x + s, y - s, z - s );
		GL.Vertex3( x + s, y + s, z - s );
		GL.Vertex3( x - s, y + s, z - s );
		GL.Vertex3( x - s, y - s, z - s );

		// Top face
		GL.Vertex3( x + s, y + s, z - s );
		GL.Vertex3( x + s, y + s, z + s );
		GL.Vertex3( x - s, y + s, z + s );
		GL.Vertex3( x - s, y + s, z - s );

		// Bottom face
		GL.Vertex3( x - s, y - s, z + s );
		GL.Vertex3( x + s, y - s, z + s );
		GL.Vertex3( x + s, y - s, z - s );
		GL.Vertex3( x - s, y - s, z - s );

		// Right face
		GL.Vertex3( x + s, y - s, z + s );
		GL.Vertex3( x + s, y + s, z + s );
		GL.Vertex3( x + s, y + s, z - s );
		GL.Vertex3( x + s, y - s, z - s );

		// Left face
		GL.Vertex3( x - s, y + s, z - s );
		GL.Vertex3( x - s, y + s, z + s );
		GL.Vertex3( x - s, y - s, z + s );
		GL.Vertex3( x - s, y - s, z - s );

		GL.End();
	}

	public class VertexEditAction : IAction
	{
		private HashSet<Solid.UniqueElement> selectedElements;
		private Matrix4 transformation;
		private List<Solid> affectedSolids;

		public VertexEditAction( HashSet<Solid.UniqueElement> selectedElements, Matrix4 transformation, List<Solid> affectedSolids )
		{
			this.selectedElements = new HashSet<Solid.UniqueElement>( selectedElements );
			this.transformation = transformation;
			this.affectedSolids = new List<Solid>( affectedSolids );
		}

		public void Perform( Document document )
		{
			ApplyTransformation( transformation );
			RefreshObjects( document );
		}

		public void Reverse( Document document )
		{
			Matrix4 inverseTransformation = transformation.Inverted();
			ApplyTransformation( inverseTransformation );
			RefreshObjects( document );
		}

		private void ApplyTransformation( Matrix4 matrix )
		{
			foreach ( var solid in affectedSolids )
			{
				solid.ApplyTransformation( matrix, selectedElements );
			}
		}

		private void RefreshObjects( Document document )
		{
			foreach ( var solid in affectedSolids )
			{
				solid.Refresh();
			}
		}

		public bool SkipInStack => false;
		public bool ModifiesState => true;

		public void Dispose()
		{
			// No unmanaged resources to dispose
		}
	}

}
