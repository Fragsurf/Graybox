
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Documents;
using Graybox.Graphics.Immediate;
using Graybox.Scenes;
using Graybox.Scenes.Drawing;
using Graybox.Utility;
using ImGuiNET;
using static Aardvark.Base.MultimethodTest;

namespace Graybox.Editor.Tools
{

	public enum PivotModes
	{
		Center,
		FirstSelected,
		LastSelected
	}

	public enum PickingModes
	{
		Vertex,
		Face,
		Edge
	}

	public enum PivotSpaces
	{
		Global,
		Local
	}

	internal class SelectTool3 : BaseTool
	{

		public PickingModes Mode { get; set; }
		public PivotModes PivotMode { get; set; } = PivotModes.FirstSelected;
		public SelectTool2.WidgetTools Widget { get; set; }

		List<MeshWrapper> Editables = new();
		List<ISelectable> selectedElements = new List<ISelectable>();
		Dictionary<string, ISelectable> selectedElementsDict = new Dictionary<string, ISelectable>();
		List<ISelectable> hoveredElements = new();

		static Color4 NormalColor => Color4.White;
		static Color4 HoveredColor => Color4.Cyan;
		static Color4 SelectedColor => Color4.Yellow;

		public override void Render( Scene scene )
		{
			base.Render( scene );

			if ( selectedElements.Count > 0 )
			{
				CalculatePivot( out var pivotCenter, out var pivotNormal );
				RenderTranslateGizmo( pivotCenter, pivotNormal.ForwardToRotation(), scene );
			}

			RenderSelection( scene );

			switch ( Mode )
			{
				case PickingModes.Vertex:
					RenderVertices();
					break;
				case PickingModes.Face:
					RenderFaces();
					break;
				case PickingModes.Edge:
					RenderEdges();
					break;
			}
		}

		void SetSelection( IEnumerable<ISelectable> elements, bool addToSelection = false )
		{
			if ( !addToSelection )
			{
				selectedElements.Clear();
				selectedElementsDict.Clear();
			}

			foreach ( var element in elements )
			{
				if ( !selectedElementsDict.ContainsKey( element.ID ) )
				{
					selectedElements.Add( element );
					selectedElementsDict[element.ID] = element;
				}
			}
		}

		void CalculatePivot( out Vector3 position, out Vector3 normal )
		{
			position = Vector3.Zero;
			normal = Vector3.UnitX; // Default forward direction in your coordinate system

			if ( selectedElements.Count == 0 )
				return;

			ISelectable pivotElement = null;
			List<Vector3> positions = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();

			switch ( PivotMode )
			{
				case PivotModes.FirstSelected:
					pivotElement = selectedElements.First();
					break;
				case PivotModes.LastSelected:
					pivotElement = selectedElements.Last();
					break;
			}

			foreach ( var meshWrapper in Editables )
			{
				switch ( Mode )
				{
					case PickingModes.Vertex:
						foreach ( var vertex in meshWrapper.Vertices.Where( IsSelected ) )
						{
							AddElementToLists( vertex, positions, normals, pivotElement );
						}
						break;
					case PickingModes.Edge:
						foreach ( var edge in meshWrapper.Edges.Where( IsSelected ) )
						{
							AddElementToLists( edge, positions, normals, pivotElement );
						}
						break;
					case PickingModes.Face:
						foreach ( var face in meshWrapper.Faces.Where( IsSelected ) )
						{
							AddElementToLists( face, positions, normals, pivotElement );
						}
						break;
				}
			}

			if ( positions.Count == 0 )
				return;

			switch ( PivotMode )
			{
				case PivotModes.FirstSelected:
				case PivotModes.LastSelected:
					position = positions[0];
					normal = normals[0];
					break;
				case PivotModes.Center:
					position = positions.Aggregate( Vector3.Zero, ( sum, pos ) => sum + pos ) / positions.Count;
					normal = normals.Aggregate( Vector3.Zero, ( sum, n ) => sum + n ).Normalized();
					break;
			}
		}

		private void AddElementToLists( ISelectable element, List<Vector3> positions, List<Vector3> normals, ISelectable pivotElement )
		{
			if ( element == pivotElement || pivotElement == null )
			{
				switch ( element )
				{
					case UniqueVertex vertex:
						positions.Add( vertex.Position );
						normals.AddRange( vertex.References.Select( r => r.Face.Face.Plane.Normal ) );
						break;
					case UniqueEdge edge:
						positions.Add( (edge.Start.Position + edge.End.Position) * 0.5f );
						normals.AddRange( edge.Faces.Select( f => f.Face.Plane.Normal ) );
						break;
					case UniqueFace face:
						positions.Add( face.Face.Vertices.Aggregate( Vector3.Zero, ( sum, v ) => sum + v.Position ) / face.Face.Vertices.Count );
						normals.Add( face.Face.Plane.Normal );
						break;
				}
			}
		}

		protected override void OnTranslate( SceneGizmos.TranslateEvent e )
		{
			base.OnTranslate( e );

			var translationVector = e.NewPosition - e.StartPosition;

			if ( !e.TranslateMatrix.Determinant.IsNearlyZero() )
			{
				var translateMatrix = e.TranslateMatrix;
				var translateAxis = e.TranslateAxis;
				var translateNormal = Vector3.TransformNormal( translateAxis, translateMatrix );
				translationVector = SnapIfNeeded( translationVector, translateNormal );
			}
			else
			{
				translationVector = SnapIfNeeded( translationVector );
			}

			if ( e.Completed )
			{
				TranslateSelectedElements( translationVector );
			}
			else
			{
				var clones = Editables.Select( x => x.Solid.Clone() ).Where( x => x is Solid );
				var editables = clones.Select( x => new MeshWrapper( x as Solid ) ).ToList();
				var uniqueVerts = new List<UniqueVertex>();

				switch ( Mode )
				{
					case PickingModes.Face:
						var uniqueFaces = editables.SelectMany( x => x.Faces ).Where( IsSelected ).ToList();
						uniqueVerts.AddRange( uniqueFaces.SelectMany( x => x.Vertices ) );
						break;
					case PickingModes.Edge:
						var uniqueEdges = editables.SelectMany( x => x.Edges ).Where( IsSelected ).ToList();
						foreach ( var edge in uniqueEdges )
						{
							uniqueVerts.Add( edge.Start );
							uniqueVerts.Add( edge.End );
						}
						break;
					case PickingModes.Vertex:
						uniqueVerts.AddRange( editables.SelectMany( x => x.Vertices ).Where( IsSelected ) );
						break;
				}

				uniqueVerts.ForEach( x => x.UpdatePosition( x.Position + translationVector ) );
				var faces = editables.SelectMany( x => x.Faces.Select( y => y.Face ) );
				//var data = CreateTranslationPreviewData();
				//data.Item1.ForEach( x => x.Clone.UpdatePosition( x.Original.Position + translationVector ) );
				MapObjectRenderer.DrawFilled( faces, new ColorPulse( System.Drawing.Color.FromArgb( 125, System.Drawing.Color.DodgerBlue ) ), false, false );
				GL.Color3( 1f, 1, 0 );
				MapObjectRenderer.DrawWireframe( faces, true, false );
			}
		}

		//private (List<(UniqueVertex Original, UniqueVertex Clone)>, List<Face>) CreateTranslationPreviewData()
		//{
		//	var selectedVertices = GetSelectedVertices().ToList();
		//	var clonedVertices = new List<(UniqueVertex Original, UniqueVertex Clone)>();
		//	var clonedFaces = new List<Face>();
		//	var vertexMap = new Dictionary<UniqueVertex, Vertex>();

		//	// First, clone affected faces
		//	var affectedFaces = selectedVertices
		//		.SelectMany( v => v.References.Select( r => r.Face ) )
		//		.Distinct();

		//	foreach ( var face in affectedFaces )
		//	{
		//		var clonedFace = face.Clone();
		//		clonedFaces.Add( clonedFace );

		//		for ( int i = 0; i < face.Vertices.Count; i++ )
		//		{
		//			var originalVertex = face.Vertices[i];
		//			var uniqueV = selectedVertices.FirstOrDefault( uv => uv.References.Any( r => r.OriginalVertex == originalVertex ) );

		//			if ( uniqueV != null )
		//			{
		//				if ( !vertexMap.ContainsKey( uniqueV ) )
		//				{
		//					var clonedVertex = clonedFace.Vertices[i];
		//					vertexMap[uniqueV] = clonedVertex;
		//					var uniqueClonedVertex = new UniqueVertex( clonedVertex, uniqueV.ID + "_clone" );
		//					clonedVertices.Add( (uniqueV, uniqueClonedVertex) );

		//					// Update the cloned vertex's references to point to the cloned face
		//					uniqueClonedVertex.References.Add( (clonedFace, clonedVertex) );
		//				}
		//				else
		//				{
		//					// If we've already created a cloned vertex for this unique vertex,
		//					// update the cloned face to use it
		//					clonedFace.Vertices[i] = vertexMap[uniqueV];
		//				}
		//			}
		//		}
		//	}

		//	return (clonedVertices, clonedFaces);
		//}

		IEnumerable<UniqueVertex> GetSelectedVertices()
		{
			var result = new List<UniqueVertex>();

			foreach ( var meshWrapper in Editables )
			{
				switch ( Mode )
				{
					case PickingModes.Vertex:
						result.AddRange( meshWrapper.Vertices.Where( IsSelected ) );
						break;
					case PickingModes.Edge:
						foreach ( var edge in meshWrapper.Edges.Where( IsSelected ) )
						{
							result.Add( meshWrapper.Vertices.First( v => v.Position == edge.Start.Position ) );
							result.Add( meshWrapper.Vertices.First( v => v.Position == edge.End.Position ) );
						}
						break;
					case PickingModes.Face:
						foreach ( var face in meshWrapper.Faces.Where( IsSelected ) )
						{
							result.AddRange( face.Face.Vertices.Select( v => meshWrapper.Vertices.First( uv => uv.Position == v.Position ) ) );
						}
						break;
				}
			}

			return result.Distinct();
		}

		void TranslateSelectedElements( Vector3 delta )
		{
			var verticesToTranslate = GetSelectedVertices().ToList();
			var faces = verticesToTranslate.SelectMany( x => x.References.Select( y => y.Face ) ).Distinct().ToList();
			var objects = faces.Select( x => x.Face.Parent ).Distinct().ToList();

			var action = new VertexEditAction( verticesToTranslate, delta, objects );
			Document.PerformAction( "Edit Vertices", action );
		}

		public override void UpdateFrame( Scene scene, FrameInfo frame )
		{
			base.UpdateFrame( scene, frame );

			EnsureSelection();
		}

		public override void UpdateWidget()
		{
			base.UpdateWidget();

			var objectIcon = EditorResource.Image( "assets/icons/picker_object.png" );
			var vertexIcon = EditorResource.Image( "assets/icons/picker_vertex.png" );
			var faceIcon = EditorResource.Image( "assets/icons/picker_face.png" );
			var edgeIcon = EditorResource.Image( "assets/icons/picker_edge.png" );

			var options = new List<(string Tooltip, int Icon, PickingModes Mode)>()
			{
				new ( "Vertex", vertexIcon, PickingModes.Vertex ),
				new ( "Face", faceIcon, PickingModes.Face ),
				new ( "Edge", edgeIcon, PickingModes.Edge ),
			};

			ImGuiEx.Header( "Picking Mode" );

			foreach ( var opt in options )
			{
				var primary = Mode == opt.Mode;
				if ( primary )
					ImGuiEx.PushButtonPrimary();

				if ( ImGui.ImageButton( "##" + opt.Tooltip, opt.Icon, new SVector2( 32, 32 ) ) )
					Mode = opt.Mode;

				if ( primary )
					ImGuiEx.PopButtonPrimary();

				if ( ImGui.IsItemHovered() ) ImGui.SetTooltip( opt.Tooltip );
				if ( opt != options.Last() ) ImGui.SameLine();
			}
		}

		public override void MouseDown( Scene scene, ref InputEvent e )
		{
			base.MouseDown( scene, ref e );
			if ( !e.Control )
			{
				SetSelection( hoveredElements );
			}
			else
			{
				SetSelection( hoveredElements, true );
			}
		}

		public override void MouseMove( Scene scene, ref InputEvent e )
		{
			base.MouseMove( scene, ref e );
			if ( Document?.Selection == null ) return;
			hoveredElements = GetHoveredElements( scene, e.LocalMousePosition ).ToList();
		}

		void RenderFaces()
		{
			var selectedFaces = Editables.SelectMany( e => e.Faces ).Where( IsSelected );
			var hoveredFaces = Editables.SelectMany( e => e.Faces ).Where( f => hoveredElements.Contains( f ) );
			var normalFaces = Editables.SelectMany( e => e.Faces ).Except( selectedFaces ).Except( hoveredFaces );

			// Render normal faces
			//MapObjectRenderer.DrawFilledNoFucks( normalFaces.Select( x => x.Face ),
			//	System.Drawing.Color.FromArgb( 75, (byte)(NormalColor.R * 255), (byte)(NormalColor.G * 255), (byte)(NormalColor.B * 255) ), false, false, 0.1f );

			// Render hovered faces
			var hoveredFinal = hoveredFaces.Select( x => x.Face );
			MapObjectRenderer.DrawFilledNoFucks( hoveredFinal, System.Drawing.Color.FromArgb( 75, (byte)(HoveredColor.R * 255), (byte)(HoveredColor.G * 255), (byte)(HoveredColor.B * 255) ), false, false, 0.1f );
			MapObjectRenderer.DrawWireframe( hoveredFinal, false, false );

			// Render selected faces
			var selectedfinal = selectedFaces.Select( x => x.Face );
			MapObjectRenderer.DrawFilledNoFucks( selectedfinal, System.Drawing.Color.FromArgb( 75, (byte)(SelectedColor.R * 255), (byte)(SelectedColor.G * 255), (byte)(SelectedColor.B * 255) ), false, false, 0.1f );
			MapObjectRenderer.DrawWireframe( selectedfinal, false, false );
		}

		void RenderVertices()
		{
			GL.PointSize( 15 );
			GL.Begin( PrimitiveType.Points );

			foreach ( var e in Editables )
			{
				foreach ( var v in e.Vertices )
				{
					Color4 vertexColor;

					if ( IsSelected( v ) )
					{
						vertexColor = SelectedColor;
					}
					else if ( hoveredElements.Contains( v ) )
					{
						vertexColor = HoveredColor;
					}
					else
					{
						vertexColor = NormalColor;
					}

					GL.Color4( vertexColor );
					GL.Vertex3( v.Position );
				}
			}

			GL.End();
		}

		void RenderEdges()
		{
			GL.LineWidth( 2f );
			GL.Begin( PrimitiveType.Lines );

			int edgeCount = 0;

			foreach ( var meshWrapper in Editables )
			{
				foreach ( var edge in meshWrapper.Edges )
				{
					edgeCount++;
					Color4 edgeColor;

					if ( IsSelected( edge ) )
					{
						edgeColor = SelectedColor;
					}
					else if ( hoveredElements.Contains( edge ) )
					{
						edgeColor = HoveredColor;
					}
					else
					{
						edgeColor = NormalColor;
					}

					GL.Color4( edgeColor );
					GL.Vertex3( edge.Start.Position );
					GL.Vertex3( edge.End.Position );
				}
			}

			GL.End();
			GL.LineWidth( 1f ); // Reset line width to default
		}

		IEnumerable<ISelectable> GetHoveredElements( Scene scene, Vector2 screenPos )
		{
			var ray = scene.ScreenToRay( screenPos );
			var line = new Line( ray.Origin, ray.Origin + ray.Direction * 100000 );

			foreach ( var e in Editables )
			{
				switch ( Mode )
				{
					case PickingModes.Vertex:
						var vertex = e.TraceForVertex( line );
						if ( vertex != null ) yield return vertex;
						break;
					case PickingModes.Face:
						var face = e.TraceForFace( scene, line );
						if ( face != null ) yield return face;
						break;
					case PickingModes.Edge:
						var edge = e.TraceForEdge( line );
						if ( edge != null ) yield return edge;
						break;
				}
			}
		}

		public bool IsSelected( ISelectable element )
		{
			return selectedElementsDict.ContainsKey( element.ID );
		}

		int selectionHash = 0;
		void EnsureSelection()
		{
			if ( Document == null )
			{
				selectionHash = 0;
				return;
			}

			int newHash = 0;
			foreach ( var obj in Document.Selection.GetSelectedObjects() )
			{
				if ( obj is not Solid s )
					continue;
				newHash = HashCode.Combine( newHash, s.ID, s.UpdateCounter );
			}

			if ( selectionHash == newHash )
				return;

			selectionHash = newHash;
			RebuildSelection();
		}

		void RebuildSelection()
		{
			Editables.Clear();

			var newlySelected = new List<ISelectable>();

			foreach ( var obj in Document.Selection.GetSelectedObjects() )
			{
				if ( obj is not Solid s )
					continue;

				Editables.Add( new MeshWrapper( s ) );

				switch ( Mode )
				{
					case PickingModes.Edge:
						foreach ( var e in Editables.Last().Edges )
						{
							if ( selectedElementsDict.ContainsKey( e.ID ) )
								newlySelected.Add( e );
						}
						break;
					case PickingModes.Vertex:
						foreach ( var v in Editables.Last().Vertices )
						{
							if ( selectedElementsDict.ContainsKey( v.ID ) )
								newlySelected.Add( v );
						}
						break;
					case PickingModes.Face:
						foreach ( var f in Editables.Last().Faces )
						{
							if ( selectedElementsDict.ContainsKey( f.ID ) )
								newlySelected.Add( f );
						}
						break;
				}

				SetSelection( newlySelected );
			}

		}

	}

	internal class MeshWrapper
	{
		public readonly Solid Solid;
		private List<UniqueVertex> uniqueVertices;
		private List<UniqueEdge> uniqueEdges;
		private List<UniqueFace> uniqueFaces;

		public MeshWrapper( Solid s )
		{
			Solid = s;
			InitializeUniqueElements();
		}

		private void InitializeUniqueElements()
		{
			uniqueVertices = new List<UniqueVertex>();
			uniqueEdges = new List<UniqueEdge>();
			uniqueFaces = new List<UniqueFace>();
			var vertexMap = new Dictionary<Vector3, UniqueVertex>( new Vector3Comparer( .01f ) );
			var edgeMap = new Dictionary<(Vector3, Vector3), UniqueEdge>( new EdgeComparer( .01f ) );

			// First pass: Create all UniqueVertex instances
			foreach ( var face in Solid.Faces )
			{
				foreach ( var vertex in face.Vertices )
				{
					if ( !vertexMap.TryGetValue( vertex.Position, out var _ ) )
					{
						var uniqueVertex = new UniqueVertex( vertex, GenerateVertexId( vertex, face ) );
						vertexMap[vertex.Position] = uniqueVertex;
						uniqueVertices.Add( uniqueVertex );
					}
				}
			}

			// Second pass: Create UniqueFace and UniqueEdge instances
			foreach ( var face in Solid.Faces )
			{
				var uniqueFace = new UniqueFace( face, GenerateFaceId( face ) );
				uniqueFaces.Add( uniqueFace );
				var faceUniqueVertices = new List<UniqueVertex>();

				for ( int i = 0; i < face.Vertices.Count; i++ )
				{
					var vertex = face.Vertices[i];
					var uniqueVertex = vertexMap[vertex.Position];
					uniqueVertex.References.Add( (uniqueFace, vertex) );
					faceUniqueVertices.Add( uniqueVertex );

					var nextVertex = face.Vertices[(i + 1) % face.Vertices.Count];
					var edgeKey = GetEdgeKey( vertex.Position, nextVertex.Position );

					if ( !edgeMap.TryGetValue( edgeKey, out var uniqueEdge ) )
					{
						var nextUniqueVertex = vertexMap[nextVertex.Position];
						uniqueEdge = new UniqueEdge( uniqueVertex, nextUniqueVertex, GenerateEdgeId( vertex, nextVertex, face ) );
						edgeMap[edgeKey] = uniqueEdge;
						uniqueEdges.Add( uniqueEdge );
					}

					uniqueEdge.Faces.Add( uniqueFace );
					uniqueFace.Edges.Add( uniqueEdge );
				}

				uniqueFace.Vertices.AddRange( faceUniqueVertices );
			}

			// Update edges with their vertices
			foreach ( var edge in uniqueEdges )
			{
				edge.Start.Edges.Add( edge );
				edge.End.Edges.Add( edge );
			}
		}

		private (Vector3, Vector3) GetEdgeKey( Vector3 v1, Vector3 v2 )
		{
			return v1.X < v2.X || (v1.X == v2.X && (v1.Y < v2.Y || (v1.Y == v2.Y && v1.Z < v2.Z)))
				? (v1, v2)
				: (v2, v1);
		}

		private string GenerateVertexId( Vertex vertex, Face face )
		{
			return $"V_{Solid.ID}_{face.ID}_{face.Vertices.IndexOf( vertex )}_{vertex.Position.GetHashCode()}";
		}

		private string GenerateEdgeId( Vertex v1, Vertex v2, Face face )
		{
			return $"E_{Solid.ID}_{face.ID}_{Math.Min( v1.GetHashCode(), v2.GetHashCode() )}_{Math.Max( v1.GetHashCode(), v2.GetHashCode() )}";
		}

		private string GenerateFaceId( Face face )
		{
			return $"F_{Solid.ID}_{face.ID}";
		}

		public IReadOnlyList<UniqueVertex> Vertices => uniqueVertices;
		public IReadOnlyList<UniqueEdge> Edges => uniqueEdges;
		public IReadOnlyList<UniqueFace> Faces => uniqueFaces;

		public UniqueFace TraceForFace( Scene scene, Line line )
		{
			var ray = new Ray()
			{
				Origin = line.Start,
				Direction = (line.End - line.Start).Normalized()
			};

			var trace = scene.Physics.Trace<Solid>( ray );
			if ( trace.Tag is Face f )
			{
				return uniqueFaces.FirstOrDefault( uf => uf.Face == f );
			}

			return null;
		}

		public UniqueVertex TraceForVertex( Line line )
		{
			UniqueVertex closestVertex = null;
			float closestDistance = float.MaxValue;

			foreach ( var v in uniqueVertices )
			{
				var size = 4f;
				var mins = v.Position - new Vector3( size, size, size );
				var maxs = v.Position + new Vector3( size, size, size );
				var box = new Box( mins, maxs );
				if ( box.IntersectsWith( line ) )
				{
					float distance = Vector3.Distance( line.Start, v.Position );
					if ( distance < closestDistance )
					{
						closestVertex = v;
						closestDistance = distance;
					}
				}
			}
			return closestVertex;
		}

		public UniqueEdge TraceForEdge( Line line )
		{
			UniqueEdge closestEdge = null;
			float closestDistance = float.MaxValue;

			foreach ( var e in uniqueEdges )
			{
				var edgeLine = new Line( e.Start.Position, e.End.Position );
				Vector3? intersectionPoint = line.GetIntersectionPointFinite( edgeLine, 4f );

				if ( intersectionPoint.HasValue )
				{
					float distance = Vector3.Distance( line.Start, intersectionPoint.Value );
					if ( distance < closestDistance )
					{
						closestEdge = e;
						closestDistance = distance;
					}
				}
			}
			return closestEdge;
		}
	}

	public interface ISelectable
	{
		string ID { get; }
	}

	public class UniqueVertex : ISelectable
	{
		public Vector3 Position { get; private set; }
		public List<(UniqueFace Face, Vertex OriginalVertex)> References { get; }
		public HashSet<UniqueEdge> Edges { get; }
		public string ID { get; }

		public UniqueVertex( Vertex originalVertex, string id )
		{
			Position = originalVertex.Position;
			References = new List<(UniqueFace, Vertex)>();
			Edges = new HashSet<UniqueEdge>();
			ID = id;
		}

		public void UpdatePosition( Vector3 newPosition )
		{
			Position = newPosition;
			foreach ( var (_, vertex) in References )
			{
				vertex.Position = newPosition;
			}
		}
	}

	public class UniqueEdge : ISelectable
	{
		public UniqueVertex Start { get; }
		public UniqueVertex End { get; }
		public string ID { get; }
		public List<UniqueFace> Faces { get; } = new List<UniqueFace>();

		public UniqueEdge( UniqueVertex start, UniqueVertex end, string id )
		{
			Start = start;
			End = end;
			ID = id;
		}
	}

	public class UniqueFace : ISelectable
	{
		public Face Face { get; }
		public string ID { get; }
		public List<UniqueVertex> Vertices { get; } = new List<UniqueVertex>();
		public List<UniqueEdge> Edges { get; } = new List<UniqueEdge>();

		public UniqueFace( Face face, string id )
		{
			Face = face;
			ID = id;
		}
	}

	public class EdgeComparer : IEqualityComparer<(Vector3, Vector3)>
	{
		private readonly float _tolerance;

		public EdgeComparer( float tolerance )
		{
			_tolerance = tolerance;
		}

		public bool Equals( (Vector3, Vector3) x, (Vector3, Vector3) y )
		{
			return (x.Item1.AlmostEqual( y.Item1, _tolerance ) && x.Item2.AlmostEqual( y.Item2, _tolerance )) ||
				   (x.Item1.AlmostEqual( y.Item2, _tolerance ) && x.Item2.AlmostEqual( y.Item1, _tolerance ));
		}

		public int GetHashCode( (Vector3, Vector3) obj )
		{
			return obj.Item1.GetHashCode() ^ obj.Item2.GetHashCode();
		}
	}

	public class VertexEditAction : IAction
	{
		private List<UniqueVertex> vertices;
		private Vector3 delta;
		private List<Solid> affectedObjects;
		private Dictionary<UniqueVertex, Vector3> originalPositions;

		public VertexEditAction( List<UniqueVertex> vertices, Vector3 delta, List<Solid> affectedObjects )
		{
			this.vertices = vertices;
			this.delta = delta;
			this.affectedObjects = affectedObjects;
			this.originalPositions = vertices.ToDictionary( v => v, v => v.Position );
		}

		public void Perform( Document document )
		{
			foreach ( var vertex in vertices )
			{
				vertex.UpdatePosition( vertex.Position + delta );
			}

			RefreshObjects( document );
		}

		public void Reverse( Document document )
		{
			foreach ( var vertex in vertices )
			{
				vertex.UpdatePosition( originalPositions[vertex] );
			}

			RefreshObjects( document );
		}

		private void RefreshObjects( Document document )
		{
			foreach ( var obj in affectedObjects )
			{
				obj.Refresh();
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
