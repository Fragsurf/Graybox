
using Graybox.Utility;
using SkiaSharp;

namespace Graybox.DataStructures.MapObjects;

public partial class Solid : MapObject
{
	public List<Face> Faces { get; private set; }

	public override Color4 Colour
	{
		get { return base.Colour; }
		set
		{
			base.Colour = value;
			Faces.ForEach( x => x.Colour = value );
		}
	}

	public Solid( long id ) : base( id )
	{
		Faces = new List<Face>();
	}

	public override MapObject Copy( IDGenerator generator )
	{
		Solid e = new Solid( generator.GetNextObjectID() );
		foreach ( Face f in Faces.Select( x => x.Copy( generator ) ) )
		{
			f.Parent = e;
			e.Faces.Add( f );
			f.UpdateBoundingBox();
			f.CalculateTextureCoordinates( true );
		}
		e.Refresh();
		CopyBase( e, generator );
		return e;
	}

	public override void Paste( MapObject o, IDGenerator generator )
	{
		PasteBase( o, generator );
		Solid e = o as Solid;
		if ( e == null ) return;
		Faces.Clear();
		foreach ( Face f in e.Faces.Select( x => x.Copy( generator ) ) )
		{
			f.Parent = this;
			Faces.Add( f );
			f.UpdateBoundingBox();
		}
	}

	public override MapObject Clone()
	{
		Solid e = new Solid( ID );
		foreach ( Face f in Faces.Select( x => x.Clone() ) )
		{
			f.Parent = e;
			e.Faces.Add( f );
			f.UpdateBoundingBox();
		}
		CopyBase( e, null, true );
		return e;
	}

	public override void Unclone( MapObject o )
	{
		PasteBase( o, null, true );
		Solid e = o as Solid;
		if ( e == null ) return;
		Faces.Clear();
		foreach ( Face f in e.Faces.Select( x => x.Clone() ) )
		{
			f.Parent = this;
			Faces.Add( f );
			f.UpdateBoundingBox();
		}
		UpdateBoundingBox();
	}

	public override void UpdateBoundingBox( bool cascadeToParent = true )
	{
		BoundingBox = new Box( Faces.Select( x => x.BoundingBox ) );
		base.UpdateBoundingBox( cascadeToParent );
	}

	public override void Transform( Transformations.IUnitTransformation transform, TransformFlags flags )
	{
		var newStart = transform.Transform( BoundingBox.Start );
		var newEnd = transform.Transform( BoundingBox.End );

		if ( (newStart - newEnd).VectorMagnitude() > 1000000f ) { return; }

		Faces.ForEach( f => f.Transform( transform, flags ) );

		var origin = CalculateWorldCenter();
		if ( Faces.All( x => x.Plane.OnPlane( origin ) >= 0 ) )
		{
			Faces.ForEach( x => x.Flip() );
		}

		base.Transform( transform, flags );
	}

	/// <summary>
	/// Returns the intersection point closest to the start of the line.
	/// </summary>
	/// <param name="line">The intersection line</param>
	/// <returns>The closest intersecting point, or null if the line doesn't intersect.</returns>
	public override Vector3 GetIntersectionPoint( Line line )
	{
		return Faces.Select( x => x.GetIntersectionPoint( line ) )
			.Where( x => x != default )
			.OrderBy( x => (x - line.Start).VectorMagnitude() )
			.FirstOrDefault();
	}

	public void Split( Plane plane, IDGenerator idgen, out Solid front, out Solid back )
	{
		var frontFaces = new List<Face>();
		var backFaces = new List<Face>();
		var intersectionPoints = new HashSet<Vector3>( new Vector3Comparer( .5f ) );

		foreach ( var face in Faces )
		{
			var poly = new Polygon( face.Vertices.Select( v => v.Position ) );
			var classification = poly.ClassifyAgainstPlane( plane );

			switch ( classification )
			{
				case PlaneClassification.Front:
					frontFaces.Add( face.Clone() );
					break;
				case PlaneClassification.Back:
					backFaces.Add( face.Clone() );
					break;
				case PlaneClassification.Spanning:
					SplitFace( face, plane, idgen, frontFaces, backFaces, intersectionPoints );
					break;
			}
		}

		if ( intersectionPoints.Count >= 3 )
		{
			var newFace = CreateNewFace( plane, intersectionPoints, idgen );
			frontFaces.Add( newFace );

			var flippedNewFace = newFace.Copy( idgen );
			flippedNewFace.Flip();
			backFaces.Add( flippedNewFace );
		}

		front = CreateSolidFromFaces( frontFaces, idgen );
		back = CreateSolidFromFaces( backFaces, idgen );
	}

	public void RecalculateNormals()
	{
		foreach ( var f in Faces )
		{
			f.RecalculateNormal();
		}
	}

	public void Refresh()
	{
		foreach ( var f in Faces )
		{
			f.CalculateTextureCoordinates( true );
			f.UpdateBoundingBox();
		}
		UpdateBoundingBox();
		RecalculateNormals();
		IncrementUpdateCounter();
	}

	private Solid CreateSolidFromFaces( List<Face> faces, IDGenerator idgen )
	{
		if ( faces.Count == 0 )
			return null;

		var solid = new Solid( idgen.GetNextObjectID() );

		foreach ( var face in faces )
		{
			face.Parent = solid;
			solid.Faces.Add( face );
		}

		CopyBase( solid, idgen );
		solid.Refresh();

		return solid;
	}

	private void SplitFace( Face face, Plane plane, IDGenerator idgen, List<Face> frontFaces, List<Face> backFaces, HashSet<Vector3> intersectionPoints )
	{
		var poly = new Polygon( face.Vertices.Select( v => v.Position ) );
		if ( poly.Split( plane, out var backPoly, out var frontPoly ) )
		{
			if ( frontPoly != null && frontPoly.Vertices.Count >= 3 )
			{
				var frontFace = CreateFaceFromPolygon( frontPoly, idgen, face.TextureRef );
				frontFaces.Add( frontFace );
			}
			if ( backPoly != null && backPoly.Vertices.Count >= 3 )
			{
				var backFace = CreateFaceFromPolygon( backPoly, idgen, face.TextureRef );
				backFaces.Add( backFace );
			}

			// Collect intersection points
			var intersectionEdges = frontPoly.Vertices.Intersect( backPoly.Vertices, new Vector3Comparer( 1e-5f ) );
			foreach ( var point in intersectionEdges )
			{
				intersectionPoints.Add( point );
			}
		}
	}

	private Face CreateNewFace( Plane plane, HashSet<Vector3> intersectionPoints, IDGenerator idgen )
	{
		var orderedPoints = OrderPointsAlongPlane( plane, intersectionPoints.ToList() );
		var poly = new Polygon( orderedPoints );
		return CreateFaceFromPolygon( poly, idgen, null );
	}

	private List<Vector3> OrderPointsAlongPlane( Plane plane, List<Vector3> points )
	{
		var center = points.Aggregate( Vector3.Zero, ( acc, v ) => acc + v ) / points.Count;
		var up = (Math.Abs( plane.Normal.Y ) < 0.9f) ? Vector3.UnitY : Vector3.UnitZ;
		var right = Vector3.Cross( up, plane.Normal ).Normalized();
		up = Vector3.Cross( plane.Normal, right );

		return points.OrderBy( p =>
		{
			var v = p - center;
			return Math.Atan2( Vector3.Dot( v, up ), Vector3.Dot( v, right ) );
		} ).ToList();
	}

	private Face CreateFaceFromPolygon( Polygon poly, IDGenerator idgen, TextureReference textureRef )
	{
		var face = new Face( idgen.GetNextFaceID() );
		face.Plane = poly.Plane;
		face.Vertices = poly.Vertices.Select( v => new Vertex( v, face ) ).ToList();
		face.TextureRef = textureRef?.Clone() ?? GuessBestTexture( face );
		face.UpdateBoundingBox();
		face.AlignTextureToWorld();
		face.CalculateTextureCoordinates( true );

		return face;
	}

	public bool IsTrigger()
	{
		return Parent is Entity e && e.ClassName.StartsWith( "trigger_", StringComparison.OrdinalIgnoreCase );
	}

	public bool Split( Plane plane, out Solid back, out Solid front, IDGenerator generator )
	{
		throw new NotImplementedException( "Use the other splitting method" );
	}

	public static Solid CreateFromIntersectingPlanes( IEnumerable<Plane> planes, IDGenerator generator )
	{
		Solid solid = new Solid( generator.GetNextObjectID() );
		List<Plane> list = planes.ToList();
		for ( int i = 0; i < list.Count; i++ )
		{
			// Split the polygon by all the other planes
			Polygon poly = new Polygon( list[i] );
			for ( int j = 0; j < list.Count; j++ )
			{
				if ( i != j ) poly.Split( list[j] );
			}

			// The final polygon is the face
			Face face = new Face( generator.GetNextFaceID() ) { Plane = poly.Plane, Parent = solid };
			face.Vertices.AddRange( poly.Vertices.Select( x => new Vertex( x, face ) ) );
			face.UpdateBoundingBox();
			face.AlignTextureToWorld();
			solid.Faces.Add( face );
		}

		// Ensure all the faces point outwards
		var origin = solid.CalculateWorldCenter();
		foreach ( Face face in solid.Faces )
		{
			if ( face.Plane.OnPlane( origin ) >= 0 ) face.Flip();
		}

		solid.UpdateBoundingBox();
		return solid;
	}

	public IEnumerable<Face> GetCoplanarFaces()
	{
		return Faces.Where( f1 => Faces.Where( f2 => f2 != f1 ).Any( f2 => f2.Plane == f1.Plane ) );
	}

	public IEnumerable<Face> GetBackwardsFaces( float epsilon = 0.001f )
	{
		var origin = CalculateWorldCenter();
		return Faces.Where( x => x.Plane.OnPlane( origin, epsilon ) > 0 );
	}

	public bool IsValid( float epsilon = 0.5f )
	{
		return !GetCoplanarFaces().Any() // Check coplanar faces
			   && !GetBackwardsFaces( epsilon ).Any() // Check faces are pointing outwards
			   && !Faces.Any( x => x.GetNonPlanarVertices( epsilon ).Any() ) // Check face vertices are all on the plane
			   && Faces.All( x => x.IsConvex() ); // Check all faces are convex
	}

	public bool IsPointInside( Vector3 point )
	{
		foreach ( var face in Faces )
		{
			if ( face.Plane.OnPlane( point ) > 0 )
			{
				return false;
			}
		}
		return true;
	}

	public Vector3 CalculateWorldCenter()
	{
		var result = Vector3.Zero;
		var count = 0;

		foreach ( var face in Faces )
		{
			foreach ( var vert in face.Vertices )
			{
				result += vert.Position;
				count++;
			}
		}

		if ( count == 0 )
			return default;

		return result / count;
	}

	private TextureReference GuessBestTexture( Face newFace )
	{
		var adjacentFaces = Faces.Where( f => f != newFace &&
			f.Vertices.Any( v => newFace.Vertices.Any( nv => nv.Position == v.Position ) ) );

		if ( adjacentFaces.Any() )
		{
			var mostSimilarFace = adjacentFaces
				.OrderByDescending( f => Vector3.Dot( f.Plane.Normal, newFace.Plane.Normal ) )
				.First();
			return mostSimilarFace.TextureRef.Clone();
		}

		return Faces.First().TextureRef.Clone();
	}

	public bool IsConvex( float epsilon = 0.001f )
	{
		// this is probably okay for now.
		// it doesn't actually prevent concave solids, but it's a good start.
		return Faces.All( x => x.IsConvex( epsilon ) );
	}

}
