
using Graybox.Utility;

namespace Graybox.DataStructures.MapObjects;

public partial class Solid
{

	private Dictionary<(Face Face, int VertexIndex), UniqueVertex> uniqueVertices;
	private List<UniqueVertex> allUniqueVertices;
	private HashSet<EdgeId> processedEdges;

	public void InitializeUniqueVertices()
	{
		uniqueVertices = new Dictionary<(Face Face, int VertexIndex), UniqueVertex>();
		allUniqueVertices = new List<UniqueVertex>();
		processedEdges = new HashSet<EdgeId>();

		var sharedVertices = new Dictionary<Vector3, UniqueVertex>( new Vector3Comparer( 0.1f ) );

		foreach ( var face in Faces )
		{
			for ( int vertexIndex = 0; vertexIndex < face.Vertices.Count; vertexIndex++ )
			{
				var vertex = face.Vertices[vertexIndex];
				if ( !uniqueVertices.TryGetValue( (face, vertexIndex), out var uniqueVertex ) )
				{
					if ( sharedVertices.TryGetValue( vertex.Position, out var sharedVertex ) )
					{
						uniqueVertex = sharedVertex;
						uniqueVertex.AddSharedVertex( vertex, face, vertexIndex );
					}
					else
					{
						uniqueVertex = new UniqueVertex( vertex, this, face, vertexIndex );
						sharedVertices[vertex.Position] = uniqueVertex;
						allUniqueVertices.Add( uniqueVertex );
					}
					uniqueVertices[(face, vertexIndex)] = uniqueVertex;
				}
			}
		}
	}

	public void ApplyTransformation( Matrix4 transformation, HashSet<UniqueElement> selectedElements )
	{
		var processedVertices = new HashSet<UniqueVertex>();

		foreach ( var element in selectedElements )
		{
			if ( element.Face?.Parent != this )
				continue;

			foreach ( var vertex in element.Vertices )
			{
				if ( processedVertices.Add( vertex ) )
				{
					var newPosition = Vector3.TransformPosition( vertex.Position, transformation );
					UpdateUniqueVertexPosition( vertex, newPosition );
				}
			}
		}

		Refresh();
	}

	public UniqueVertex GetUniqueVertex( Face face, int vertexIndex )
	{
		return uniqueVertices.TryGetValue( (face, vertexIndex), out var uniqueVertex ) ? uniqueVertex : null;
	}

	public IEnumerable<UniqueVertex> GetAllUniqueVertices()
	{
		return allUniqueVertices;
	}

	public IEnumerable<UniqueVertex> GetUniqueVerticesForFace( Face face )
	{
		return face.Vertices
			.Select( ( v, index ) => GetUniqueVertex( face, index ) );
	}

	public IEnumerable<UniqueVertex> GetUniqueVerticesForEdge( Face face, int edgeIndex )
	{
		var v1 = GetUniqueVertex( face, edgeIndex );
		var v2 = GetUniqueVertex( face, (edgeIndex + 1) % face.Vertices.Count );

		return new[] { v1, v2 };
	}

	public void UpdateUniqueVertexPosition( UniqueVertex uniqueVertex, Vector3 newPosition )
	{
		uniqueVertex.UpdatePosition( newPosition );
	}

	public class UniqueVertex
	{
		private List<(Vertex Vertex, Face Face, int VertexIndex)> sharedVertices = new List<(Vertex, Face, int)>();

		public Solid OriginalSolid { get; }
		public Vector3 Position { get; private set; }

		public UniqueVertex( Vertex originalVertex, Solid originalSolid, Face face, int vertexIndex )
		{
			OriginalSolid = originalSolid;
			Position = originalVertex.Position;
			AddSharedVertex( originalVertex, face, vertexIndex );
		}

		public void AddSharedVertex( Vertex vertex, Face face, int vertexIndex )
		{
			sharedVertices.Add( (vertex, face, vertexIndex) );
		}

		public void UpdatePosition( Vector3 newPosition )
		{
			Position = newPosition;
			foreach ( var (vertex, _, _) in sharedVertices )
			{
				vertex.Position = newPosition;
			}
		}

		public IEnumerable<(Vertex Vertex, Face Face, int VertexIndex)> GetSharedVertices()
		{
			return sharedVertices;
		}
	}

	public class UniqueElement
	{

		public enum ElementType { Vertex, Edge, Face }

		public ElementType Type { get; }
		public Face Face { get; }
		public int LocalId { get; }
		public List<UniqueVertex> Vertices { get; set; }

		public UniqueElement( ElementType type, Face face, int localId )
		{
			Type = type;
			Face = face;
			LocalId = localId;
			Vertices = new List<UniqueVertex>();
		}

		public Vector3 CalculateCenter()
		{
			var result = new Vector3();

			if ( Vertices.Count > 0 )
			{
				foreach ( var v in Vertices )
				{
					result += v.Position;
				}
				result /= Vertices.Count;
			}

			return result;
		}

		public bool Equals( UniqueElement other )
		{
			if ( other is null )
				return false;

			if ( ReferenceEquals( this, other ) )
				return true;

			if ( Type != other.Type || Face?.ID != other.Face?.ID || LocalId != other.LocalId )
				return false;

			// Only compare vertices if everything else is equal
			return Vertices.SequenceEqual( other.Vertices );
		}

		public override bool Equals( object obj )
		{
			if ( obj is null )
				return false;

			if ( ReferenceEquals( this, obj ) )
				return true;

			if ( obj.GetType() != this.GetType() )
				return false;

			return Equals( (UniqueElement)obj );
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( Type, Face?.ID, LocalId );
		}

		public static bool operator ==( UniqueElement left, UniqueElement right )
		{
			if ( left is null )
				return right is null;

			return left.Equals( right );
		}

		public static bool operator !=( UniqueElement left, UniqueElement right )
		{
			return !(left == right);
		}

	}

	public List<UniqueElement> GetUniqueElements( UniqueElement.ElementType elementType )
	{
		var elements = new List<UniqueElement>();
		switch ( elementType )
		{
			case UniqueElement.ElementType.Vertex:
				elements.AddRange( allUniqueVertices.Select( v =>
				{
					var firstSharedVertex = v.GetSharedVertices().First();
					return new UniqueElement( UniqueElement.ElementType.Vertex, firstSharedVertex.Face, firstSharedVertex.VertexIndex )
					{
						Vertices = new List<UniqueVertex> { v }
					};
				} ) );
				break;
			case UniqueElement.ElementType.Edge:
				processedEdges.Clear();
				foreach ( var face in Faces )
				{
					for ( int edgeIndex = 0; edgeIndex < face.Vertices.Count; edgeIndex++ )
					{
						var v1 = GetUniqueVertex( face, edgeIndex );
						var v2 = GetUniqueVertex( face, (edgeIndex + 1) % face.Vertices.Count );
						var edgeId = new EdgeId( v1.Position, v2.Position );
						if ( processedEdges.Add( edgeId ) )
						{
							var edgeElement = new UniqueElement( UniqueElement.ElementType.Edge, face, edgeIndex );
							edgeElement.Vertices.Add( v1 );
							edgeElement.Vertices.Add( v2 );
							elements.Add( edgeElement );
						}
					}
				}
				break;
			case UniqueElement.ElementType.Face:
				elements.AddRange( Faces.Select( f => new UniqueElement( UniqueElement.ElementType.Face, f, 0 ) { Vertices = GetUniqueVerticesForFace( f ).ToList() } ) );
				break;
		}
		return elements;
	}

	public void ApplyVertexEdit( List<UniqueVertex> vertices, Vector3 delta )
	{
		foreach ( var vertex in vertices )
		{
			UpdateUniqueVertexPosition( vertex, vertex.Position + delta );
		}
	}

	public Solid CreateTransientCopy()
	{
		var copiedSolid = this.Clone() as Solid;
		copiedSolid.InitializeUniqueVertices();
		return copiedSolid;
	}

	private struct EdgeId : IEquatable<EdgeId>
	{
		private readonly Vector3 SmallerVertex;
		private readonly Vector3 LargerVertex;

		public EdgeId( Vector3 v1, Vector3 v2 )
		{
			if ( CompareVectors( v1, v2 ) <= 0 )
			{
				SmallerVertex = v1;
				LargerVertex = v2;
			}
			else
			{
				SmallerVertex = v2;
				LargerVertex = v1;
			}
		}

		public bool Equals( EdgeId other )
		{
			return SmallerVertex.Equals( other.SmallerVertex ) && LargerVertex.Equals( other.LargerVertex );
		}

		public override bool Equals( object obj )
		{
			return obj is EdgeId other && Equals( other );
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( SmallerVertex, LargerVertex );
		}

		private static int CompareVectors( Vector3 a, Vector3 b )
		{
			if ( a.X != b.X ) return a.X.CompareTo( b.X );
			if ( a.Y != b.Y ) return a.Y.CompareTo( b.Y );
			return a.Z.CompareTo( b.Z );
		}
	}

}
