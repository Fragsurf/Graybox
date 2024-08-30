
using Graybox.DataStructures.Transformations;

namespace Graybox.DataStructures.MapObjects;

public class Displacement : Face
{
	public int Power { get; private set; }
	public Vector3 StartPosition { get; set; }
	public float Elevation { get; set; }
	public bool SubDiv { get; set; }
	public DisplacementPoint[,] Points { get; set; }
	public int Resolution { get { return (int)Math.Pow( 2, Power ); } }

	public Displacement( long id ) : base( id )
	{
		SetPower( 3 );
		Elevation = 0;
		SubDiv = false;
	}

	/// <summary>
	/// Safe version of array indexing into Points[,].
	/// Returns null when the index is out of bounds.
	/// </summary>
	/// <param name="x">The X position of the point</param>
	/// <param name="y">The Y position of the point</param>
	/// <returns>The point at the specified position</returns>
	public DisplacementPoint GetPoint( int x, int y )
	{
		int max = Resolution;
		if ( x < 0 || y < 0 || x > Resolution || y > Resolution ) return null;
		return Points[x, y];
	}

	/// <summary>
	/// Get the points array in an easy-to-iterate enumerable form.
	/// </summary>
	/// <returns>The points array</returns>
	public IEnumerable<DisplacementPoint> GetPoints()
	{
		// LINQ doesn't seem to like multi-dimensional arrays
		List<DisplacementPoint> list = new List<DisplacementPoint>();
		foreach ( DisplacementPoint p in Points ) list.Add( p );
		return list;
	}

	public void SetPower( int power )
	{
		Power = power;
		int size = Resolution + 1;
		Points = new DisplacementPoint[size, size];
		for ( int i = 0; i < size; i++ )
		{
			for ( int j = 0; j < size; j++ )
			{
				//TODO: approximate new point values instead of zeroing
				Points[i, j] = new DisplacementPoint( this, i, j );
			}
		}
		UpdateBoundingBox();
	}

	/// <summary>
	/// For use in the <code>MapObject.Find</code> method.
	/// Returns solids with displacements that are sewable with this one.
	/// </summary>
	/// <param name="obj">MapObject to test</param>
	/// <returns>True if the given object contains a sewable displacement.</returns>
	private bool HasSewableDisplacement( MapObject obj )
	{
		return (obj is Solid) && ((Solid)obj).Faces.OfType<Displacement>().Any( IsSewableTo );
	}

	/// <summary>
	/// Whether or not this displacement is sewable with the provided displacement.
	/// </summary>
	/// <param name="disp">The displcement to test</param>
	/// <returns>True if these displacements are sewable.</returns>
	public bool IsSewableTo( Displacement disp )
	{
		IEnumerable<Line> otherEdges = disp.GetEdges();
		return GetEdges().Any( x => otherEdges.Any( y => x.EquivalentTo( y ) ) );
	}

	/// <summary>
	/// Get all the displacements that are sewable with this one.
	/// </summary>
	/// <param name="fromList">Optional list to search. If not specified, the entire map will be searched.</param>
	/// <returns>A list of sewable displacements.</returns>
	public IEnumerable<Displacement> GetSewableDisplacements( IEnumerable<Displacement> fromList = null )
	{
		if ( fromList == null )
		{
			fromList = MapObject.GetRoot( Parent )
				.Find( HasSewableDisplacement ).OfType<Solid>()
				.SelectMany( x => x.Faces ).OfType<Displacement>();
		}
		return fromList.Where( IsSewableTo );
	}

	/// <summary>
	/// Sew this displacement to others.
	/// </summary>
	/// <param name="displacements">The displacements to sew</param>
	/// <param name="sewType"></param>
	/// <param name="numPointsFromEdgeToMove"></param>
	/// <param name="easingFunction"></param>
	public void SewTo( IEnumerable<Displacement> displacements, DisplacementSewType sewType,
		int numPointsFromEdgeToMove/*, Easing easingFunction*/ )
	{

	}

	public void CalculatePoints()
	{
		if ( Vertices.Count != 4 ) throw new Exception( "Displacement must have four vertices." );

		Vertex startVertex = Vertices
			.OrderBy( x => (x.Position - StartPosition).VectorMagnitude() )
			.FirstOrDefault( x => x.Position.EquivalentTo( StartPosition, 0.5f ) );
		if ( startVertex == null ) throw new Exception( "Unable to locate displacement start position." );

		int index = Vertices.IndexOf( startVertex );
		var corners = new List<Vector3>();
		for ( int i = 0; i < 4; i++ ) corners.Add( Vertices[(index + i) % 4].Position );

		int res = Resolution;
		int size = res + 1;
		// Get the distance between each point along the two sides of the face
		var dist1 = (corners[1] - corners[0]) / res;
		var dist2 = (corners[2] - corners[3]) / res;
		var elev = Plane.Normal * Elevation;
		for ( int i = 0; i < size; i++ )
		{
			var rowStart = corners[0] + i * dist1;
			var rowEnd = corners[3] + i * dist2;
			// Get the step between each point in this row
			var dist3 = (rowEnd - rowStart) / res;
			for ( int j = 0; j < size; j++ )
			{
				DisplacementPoint point = Points[i, j];
				point.InitialPosition = rowStart + j * dist3;
				point.CurrentPosition.Position = point.InitialPosition
												 + point.OffsetDisplacement.Position
												 + point.Displacement.Position
												 + elev;
			}
		}
		UpdateBoundingBox();
	}

	public void CalculateNormals()
	{
		OpenTK.Mathematics.Vector3 elev = Plane.Normal * Elevation;
		foreach ( DisplacementPoint point in Points )
		{
			point.OffsetDisplacement.SetToZero();
			OpenTK.Mathematics.Vector3 disp = point.CurrentPosition.Position - elev - point.InitialPosition;
			point.Displacement.Set( disp );
		}
	}

	public override IEnumerable<Line> GetLines()
	{
		foreach ( Vertex[] triangle in GetTriangles() )
		{
			yield return new Line( triangle[0].Position, triangle[1].Position );
			yield return new Line( triangle[1].Position, triangle[2].Position );
			yield return new Line( triangle[2].Position, triangle[0].Position );
		}
	}

	public override IEnumerable<Vertex> GetIndexedVertices()
	{
		return Points.OfType<DisplacementPoint>().Select( x => x.CurrentPosition );
	}

	public override IEnumerable<uint> GetTriangleIndices()
	{
		uint res = (uint)Resolution + 1;
		for ( uint i = 0; i < res - 1; i++ )
		{
			bool flip = (i % 2 != 0);
			for ( uint j = 0; j < res - 1; j++ )
			{
				uint t = i;
				uint b = i;
				if ( flip ) t++;
				else b++;

				yield return (i) * res + (j);
				yield return (i + 1) * res + (j);
				yield return (b) * res + (j + 1);

				yield return (t) * res + (j);
				yield return (i + 1) * res + (j + 1);
				yield return (i) * res + (j + 1);

				flip = !flip;
			}
		}
	}

	public override IEnumerable<uint> GetLineIndices()
	{
		List<uint> tv = GetTriangleIndices().ToList();
		for ( int i = 0; i < tv.Count; i += 3 )
		{
			yield return tv[i + 0];
			yield return tv[i + 1];

			yield return tv[i + 1];
			yield return tv[i + 2];

			yield return tv[i + 2];
			yield return tv[i + 0];
		}
	}

	public override IEnumerable<Vertex[]> GetTriangles()
	{
		int res = Resolution;
		for ( int i = 0; i < res; i++ )
		{
			bool flip = (i % 2 != 0);
			for ( int j = 0; j < res; j++ )
			{
				int t = i;
				int b = i;
				if ( flip ) t++;
				else b++;

				yield return new[]
								 {
									 Points[i, j].CurrentPosition,
									 Points[i + 1, j].CurrentPosition,
									 Points[b, j + 1].CurrentPosition
								 };

				yield return new[]
								 {
									 Points[t, j].CurrentPosition,
									 Points[i + 1, j + 1].CurrentPosition,
									 Points[i, j + 1].CurrentPosition
								 };

				flip = !flip;
			}
		}
	}

	public override void CalculateTextureCoordinates( bool minimizeTextureValues )
	{
		List<Vertex> list = new List<Vertex>();
		foreach ( DisplacementPoint p in Points ) list.Add( p.CurrentPosition );
		list.ForEach( c => c.TextureU = c.TextureV = 0 );
		Vertices.ForEach( c => c.TextureU = c.TextureV = 0 );

		if ( TextureRef.Texture == null ) return;

		var udiv = TextureRef.Texture.Width * TextureRef.XScale;
		var uadd = TextureRef.XShift / TextureRef.Texture.Width;
		var vdiv = TextureRef.Texture.Height * TextureRef.YScale;
		var vadd = TextureRef.YShift / TextureRef.Texture.Height;

		foreach ( Vertex v in Vertices )
		{
			v.TextureU = (Vector3.Dot( v.Position, TextureRef.UAxis ) / udiv) + uadd;
			v.TextureV = (Vector3.Dot( v.Position, TextureRef.VAxis ) / vdiv) + vadd;
		}

		foreach ( Vertex v in list )
		{
			v.TextureU = (Vector3.Dot( v.Position, TextureRef.UAxis ) / udiv) + uadd;
			v.TextureV = (Vector3.Dot( v.Position, TextureRef.VAxis ) / vdiv) + vadd;
		}
	}

	public override void UpdateBoundingBox()
	{
		List<OpenTK.Mathematics.Vector3> list = new List<OpenTK.Mathematics.Vector3>();
		// LINQ doesn't seem to like multi-dimensional arrays
		foreach ( DisplacementPoint p in Points ) list.Add( p.CurrentPosition.Position );
		BoundingBox = new Box( list );
	}

	public override void Transform( IUnitTransformation transform, TransformFlags flags )
	{
		foreach ( DisplacementPoint p in Points )
		{
			p.InitialPosition = transform.Transform( p.InitialPosition );
			p.CurrentPosition.Position = transform.Transform( p.CurrentPosition.Position );
		}
		CalculateNormals();
		base.Transform( transform, flags );
	}

	public override Vector3 GetIntersectionPoint( Line line, bool ignoreDirection = false, bool ignoreSegment = false )
	{
		// Return the first intersection we find, don't care where it is
		return GetTriangles()
			.Select( triangle => GetIntersectionPoint( triangle.Select( x => x.Position ).ToList(), line, ignoreDirection, ignoreSegment ) )
			.FirstOrDefault( isect => isect != default );
	}

	public DisplacementPoint GetClosestDisplacementPoint( Line line )
	{
		return GetPoints()
		   .OrderBy( x => (x.Location - line.ClosestPoint( x.Location )).LengthSquared )
		   .FirstOrDefault();

		// This way is more accurate, but too slow.
		/*
            // First, do an intersection test on the triangles and get the closest one
            var tri = GetTriangles()
                .Select(triangle => new { triangle, point = GetIntersectionPoint(triangle.Select(x => x.Location).ToList(), line) })
                .Where(x => x.point != null)
                .OrderBy(x => (line.Start - x.point).LengthSquared())
                .Select(x => x.triangle)
                .FirstOrDefault();

            // If we found an intersect, return the point that is closest to the line
            if (tri != null)
            {
                var coord = tri
                    .OrderBy(x => (x.Location - line.ClosestPoint(x.Location)).LengthSquared())
                    .First();
                var ret = GetPoints().FirstOrDefault(x => x.Location == coord.Location);
                // Null check for sanity - shouldn't ever have a triangle without a matching point
                if (ret != null) return ret;
            }

            // If we didn't find an intersect, just return the point with the shortest distance from the line
            return GetPoints()
                .OrderBy(x => (x.Location - line.ClosestPoint(x.Location)).LengthSquared())
                .FirstOrDefault();
            */
	}
}

public enum DisplacementSewType
{
	ThisFixed,
	OthersFixed,
	MoveBoth
}
