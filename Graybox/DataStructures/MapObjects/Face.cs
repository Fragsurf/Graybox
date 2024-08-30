
using Graybox.DataStructures.Transformations;

namespace Graybox.DataStructures.MapObjects;

public class Face
{

	public long ID { get; set; }
	public Color4 Colour { get; set; }
	public Plane Plane { get; set; }

	public bool IsSelected { get; set; }
	public bool IsHidden { get; set; }
	public float Opacity { get; set; }

	int minTexelSize => 4;
	int maxTexelSize => 128;

	int texelSize = 16;
	public int TexelSize
	{
		get => texelSize;
		set => texelSize = MathHelper.Clamp( value, minTexelSize, maxTexelSize );
	}

	public bool DisableInLightmap { get; set; }

	public TextureReference TextureRef { get; set; }
	public List<Vertex> Vertices { get; set; }

	public Solid Parent { get; set; }

	public Box BoundingBox { get; set; }

	public Face( long id )
	{
		ID = id;
		TextureRef = new TextureReference();
		Vertices = new List<Vertex>();
		IsSelected = false;
		Opacity = 1;
	}

	public virtual Face Copy( IDGenerator generator )
	{
		var result = new Face( generator.GetNextFaceID() )
		{
			Plane = Plane,
			Colour = Colour,
			IsSelected = IsSelected,
			IsHidden = IsHidden,
			Opacity = Opacity,
			TextureRef = TextureRef.Clone(),
			Parent = Parent,
			TexelSize = TexelSize,
			BoundingBox = BoundingBox.Clone(),
		};

		foreach ( var v in Vertices )
		{
			var clone = v.Clone();
			clone.Parent = result;
			result.Vertices.Add( clone );
		}

		return result;
	}

	public virtual Face Clone()
	{
		Face f = Copy( new IDGenerator() );
		f.ID = ID;
		return f;
	}

	public virtual void Paste( Face f )
	{
		Plane = f.Plane;
		Colour = f.Colour;
		IsSelected = f.IsSelected;
		IsHidden = f.IsHidden;
		Opacity = f.Opacity;
		TextureRef = f.TextureRef.Clone();
		Parent = f.Parent;
		TexelSize = f.TexelSize;
		DisableInLightmap = f.DisableInLightmap;
		BoundingBox = f.BoundingBox.Clone();
		Vertices.Clear();

		foreach ( var v in f.Vertices )
		{
			var clone = v.Clone();
			clone.Parent = this;
			Vertices.Add( clone );
		}
	}

	public virtual void Unclone( Face f )
	{
		Paste( f );
		ID = f.ID;
	}

	public virtual IEnumerable<Line> GetLines()
	{
		return GetEdges();
	}

	public virtual IEnumerable<Line> GetEdges()
	{
		for ( int i = 0; i < Vertices.Count; i++ )
		{
			yield return new Line( Vertices[i].Position, Vertices[(i + 1) % Vertices.Count].Position );
		}
	}

	public virtual IEnumerable<Vertex> GetIndexedVertices()
	{
		return Vertices;
	}

	public virtual IEnumerable<uint> GetLineIndices()
	{
		for ( uint i = 0; i < Vertices.Count; i++ )
		{
			uint ni = (uint)((i + 1) % Vertices.Count);
			yield return i;
			yield return ni;
		}
	}

	public virtual IEnumerable<uint> GetTriangleIndices()
	{
		for ( uint i = 1; i < Vertices.Count - 1; i++ )
		{
			yield return 0;
			yield return i;
			yield return i + 1;
		}
	}

	public virtual IEnumerable<Vertex[]> GetTriangles()
	{
		for ( int i = 1; i < Vertices.Count - 1; i++ )
		{
			yield return new[]
							 {
								 Vertices[0],
								 Vertices[i],
								 Vertices[i + 1]
							 };
		}
	}

	public virtual IEnumerable<Vertex[]> GetTrianglesReversed()
	{
		for ( int i = 1; i < Vertices.Count - 1; i++ )
		{
			yield return new[]
							 {
								 Vertices[Vertices.Count - 1],
								 Vertices[Vertices.Count - 2 - i],
								 Vertices[Vertices.Count - 1 - i]
							 };
		}
	}

	public IEnumerable<Vertex> GetNonPlanarVertices( float epsilon = 0.001f )
	{
		return Vertices.Where( x => Plane.OnPlane( x.Position, epsilon ) != 0 );
	}

	public bool IsConvex( float epsilon = 0.001f )
	{
		return new Polygon( Vertices.Select( x => x.Position ) ).IsConvex( epsilon );
	}

	#region Textures

	public enum BoxAlignMode
	{
		Left,
		Right,
		Center,
		Top,
		Bottom
	}

	public virtual void CalculateTextureCoordinates( bool minimizeShiftValues )
	{
		if ( TextureRef.Texture == null ) return;
		if ( TextureRef.Texture.Width == 0 || TextureRef.Texture.Height == 0 ) return;
		if ( TextureRef.XScale == 0 || TextureRef.YScale == 0 ) return;

		if ( minimizeShiftValues ) MinimiseTextureShiftValues();

		var udiv = TextureRef.Texture.Width * TextureRef.XScale;
		var uadd = TextureRef.XShift / TextureRef.Texture.Width;
		var vdiv = TextureRef.Texture.Height * TextureRef.YScale;
		var vadd = TextureRef.YShift / TextureRef.Texture.Height;

		foreach ( var v in Vertices )
		{
			v.TextureU = (Vector3.Dot( v.Position, TextureRef.UAxis ) / udiv) + uadd;
			v.TextureV = (Vector3.Dot( v.Position, TextureRef.VAxis ) / vdiv) + vadd;
		}
	}

	public void AlignTextureToWorld()
	{
		// Set the U and V axes to match the X, Y, or Z axes
		// How they are calculated depends on which direction the plane is facing

		var direction = Plane.GetClosestAxisToNormal();

		// VHE behaviour:
		// U axis: If the closest axis to the normal is the X axis,
		//         the U axis is UnitY. Otherwise, the U axis is UnitX.
		// V axis: If the closest axis to the normal is the Z axis,
		//         the V axis is -UnitY. Otherwise, the V axis is -UnitZ.

		TextureRef.UAxis = direction == Vector3.UnitX ? Vector3.UnitY : Vector3.UnitX;
		TextureRef.VAxis = direction == Vector3.UnitZ ? -Vector3.UnitY : -Vector3.UnitZ;
		TextureRef.Rotation = 0;

		CalculateTextureCoordinates( true );
	}

	public void AlignTextureToFace()
	{
		// Set the U and V axes to match the plane's normal
		// Need to start with the world alignment on the V axis so that we don't align backwards.
		// Then we can calculate U based on that, and the real V afterwards.

		var direction = Plane.GetClosestAxisToNormal();
		var tempV = direction == Vector3.UnitZ ? -Vector3.UnitY : -Vector3.UnitZ;

		TextureRef.UAxis = Plane.Normal.Cross( tempV ).Normalized();
		TextureRef.VAxis = Vector3.Cross( TextureRef.UAxis, Plane.Normal ).Normalized();
		TextureRef.Rotation = 0;

		CalculateTextureCoordinates( true );
	}

	public bool IsTextureAlignedToWorld()
	{
		var direction = Plane.GetClosestAxisToNormal();
		var cp = Vector3.Cross( TextureRef.UAxis, TextureRef.VAxis ).Normalized();
		return cp.EquivalentTo( direction, 0.01f ) || cp.EquivalentTo( -direction, 0.01f );
	}

	public bool IsTextureAlignedToFace()
	{
		var cp = Vector3.Cross( TextureRef.UAxis, TextureRef.VAxis ).Normalized();
		return cp.EquivalentTo( Plane.Normal, 0.01f ) || cp.EquivalentTo( -Plane.Normal, 0.01f );
	}

	public void AlignTextureWithFace( Face face )
	{
		// Get reference values for the axes
		var refU = face.TextureRef.UAxis;
		var refV = face.TextureRef.VAxis;
		// Reference points in the texture plane to use for shifting later on
		var refX = face.TextureRef.UAxis * face.TextureRef.XShift * face.TextureRef.XScale;
		var refY = face.TextureRef.VAxis * face.TextureRef.YShift * face.TextureRef.YScale;

		// Two non-parallel planes intersect at an edge. We want the textures on this face
		// to line up with the textures on the provided face. To do this, we rotate the texture 
		// normal on the provided face around the intersection edge to get the new texture axes.
		// Then we rotate the texture reference point around this edge as well to get the new shift values.
		// The scale values on both faces will always end up being the same value.

		// Find the intersection edge vector
		var intersectionEdge = face.Plane.Normal.Cross( Plane.Normal );
		// Create a plane using the intersection edge as the normal
		var intersectionPlane = new Plane( intersectionEdge, 0 );

		// If the planes are parallel, the texture doesn't need any rotation - just different shift values.
		var intersect = Plane.Intersect( face.Plane, Plane, intersectionPlane );
		if ( intersect != default )
		{
			var texNormal = face.TextureRef.GetNormal();

			// Since the intersection plane is perpendicular to both face planes, we can find the angle
			// between the two planes (the original texture plane and the plane of this face) by projecting
			// the normals of the planes onto the perpendicular plane and taking the cross product.

			// Project the two normals onto the perpendicular plane
			var ptNormal = intersectionPlane.Project( texNormal ).Normalized();
			var ppNormal = intersectionPlane.Project( Plane.Normal ).Normalized();

			// Get the angle between the projected normals
			float dot = MathF.Round( ptNormal.Dot( ppNormal ), 4 );
			float angle = MathF.Acos( dot ); // A.B = cos(angle)

			// Rotate the texture axis by the angle around the intersection edge
			UnitRotate transform = new UnitRotate( angle, new Line( OpenTK.Mathematics.Vector3.Zero, intersectionEdge ) );
			refU = transform.Transform( refU );
			refV = transform.Transform( refV );

			// Rotate the texture reference points as well, but around the intersection line, not the origin
			refX = transform.Transform( refX + intersect ) - intersect;
			refY = transform.Transform( refY + intersect ) - intersect;
		}

		// Convert the reference points back to get the final values
		TextureRef.Rotation = 0;
		TextureRef.UAxis = refU.Normalized();
		TextureRef.VAxis = refV.Normalized();
		TextureRef.XShift = Vector3.Dot( refU, refX ) / face.TextureRef.XScale;
		TextureRef.YShift = Vector3.Dot( refV, refY ) / face.TextureRef.YScale;
		TextureRef.XScale = face.TextureRef.XScale;
		TextureRef.YScale = face.TextureRef.YScale;

		CalculateTextureCoordinates( true );
	}

	private void MinimiseTextureShiftValues()
	{
		if ( TextureRef.Texture == null ) return;
		// Keep the shift values to a minimum
		TextureRef.XShift = TextureRef.XShift % TextureRef.Texture.Width;
		TextureRef.YShift = TextureRef.YShift % TextureRef.Texture.Height;
		if ( TextureRef.XShift < -TextureRef.Texture.Width / 2f ) TextureRef.XShift += TextureRef.Texture.Width;
		if ( TextureRef.YShift < -TextureRef.Texture.Height / 2f ) TextureRef.YShift += TextureRef.Texture.Height;
	}

	public void FitTextureToPointCloud( PointCloud cloud, int tileX, int tileY )
	{
		if ( TextureRef.Texture == null ) return;
		if ( tileX <= 0 ) tileX = 1;
		if ( tileY <= 0 ) tileY = 1;

		// Scale will change, no need to use it in the calculations
		List<float> xvals = cloud.GetExtents().Select( x => Vector3.Dot( x, TextureRef.UAxis ) ).ToList();
		List<float> yvals = cloud.GetExtents().Select( x => Vector3.Dot( x, TextureRef.VAxis ) ).ToList();

		var minU = xvals.Min();
		var minV = yvals.Min();
		var maxU = xvals.Max();
		var maxV = yvals.Max();

		TextureRef.XScale = (maxU - minU) / (TextureRef.Texture.Width * tileX);
		TextureRef.YScale = (maxV - minV) / (TextureRef.Texture.Height * tileY);
		TextureRef.XShift = -minU / TextureRef.XScale;
		TextureRef.YShift = -minV / TextureRef.YScale;

		CalculateTextureCoordinates( true );
	}

	public void AlignTextureWithPointCloud( PointCloud cloud, BoxAlignMode mode )
	{
		if ( TextureRef.Texture == null ) return;

		List<float> xvals = cloud.GetExtents().Select( x => Vector3.Dot( x, TextureRef.UAxis ) / TextureRef.XScale ).ToList();
		List<float> yvals = cloud.GetExtents().Select( x => Vector3.Dot( x, TextureRef.VAxis ) / TextureRef.YScale ).ToList();

		float minU = xvals.Min();
		float minV = yvals.Min();
		float maxU = xvals.Max();
		float maxV = yvals.Max();

		switch ( mode )
		{
			case BoxAlignMode.Left:
				TextureRef.XShift = -minU;
				break;
			case BoxAlignMode.Right:
				TextureRef.XShift = -maxU + TextureRef.Texture.Width;
				break;
			case BoxAlignMode.Center:
				var avgU = (minU + maxU) / 2f;
				var avgV = (minV + maxV) / 2f;
				TextureRef.XShift = -avgU + TextureRef.Texture.Width / 2f;
				TextureRef.YShift = -avgV + TextureRef.Texture.Height / 2f;
				break;
			case BoxAlignMode.Top:
				TextureRef.YShift = -minV;
				break;
			case BoxAlignMode.Bottom:
				TextureRef.YShift = -maxV + TextureRef.Texture.Height;
				break;
		}
		CalculateTextureCoordinates( true );
	}

	/// <summary>
	/// Rotate the texture around the texture normal.
	/// </summary>
	/// <param name="rotate">The rotation angle in degrees</param>
	public void SetTextureRotation( float rotate )
	{
		float rads = MathHelper.DegreesToRadians( TextureRef.Rotation - rotate );
		// Rotate around the texture normal
		OpenTK.Mathematics.Vector3 texNorm = OpenTK.Mathematics.Vector3.Cross( TextureRef.VAxis, TextureRef.UAxis ).Normalized();
		UnitRotate transform = new UnitRotate( rads, new Line( OpenTK.Mathematics.Vector3.Zero, texNorm ) );
		TextureRef.UAxis = transform.Transform( TextureRef.UAxis ).Normalized();
		TextureRef.VAxis = transform.Transform( TextureRef.VAxis ).Normalized();
		TextureRef.Rotation = rotate;

		CalculateTextureCoordinates( false );
	}

	#endregion

	public virtual void UpdateBoundingBox()
	{
		BoundingBox = new Box( Vertices.Select( x => x.Position ) );
	}

	public virtual void Transform( IUnitTransformation transform, TransformFlags flags )
	{
		foreach ( Vertex t in Vertices )
		{
			var newLocation = transform.Transform( t.Position );
			t.Position = newLocation;
		}

		Plane = new Plane( Vertices[0].Position, Vertices[1].Position, Vertices[2].Position );

		if ( flags.HasFlag( TransformFlags.TextureScalingLock ) && TextureRef.Texture != null )
		{
			// Make a best-effort guess of retaining scaling. All bets are off during skew operations.
			// Transform the current texture axes
			var origin = transform.Transform( Vector3.Zero );
			var ua = transform.Transform( TextureRef.UAxis ) - origin;
			var va = transform.Transform( TextureRef.VAxis ) - origin;
			// Multiply the scales by the magnitudes (they were normals before the transform operation)
			TextureRef.XScale *= (float)ua.VectorMagnitude();
			TextureRef.YScale *= (float)va.VectorMagnitude();
		}
		{
			// Transform the texture axes and move them back to the origin
			var origin = transform.Transform( Vector3.Zero );
			var ua = transform.Transform( TextureRef.UAxis ) - origin;
			var va = transform.Transform( TextureRef.VAxis ) - origin;

			// Only do the transform if the axes end up being not perpendicular
			// Otherwise just make a best-effort guess, same as the scaling lock
			if ( Math.Abs( ua.Dot( va ) ) < 0.0001f && MathF.Abs( Plane.Normal.Dot( ua.Cross( va ).Normalized() ) ) > 0.0001f )
			{
				TextureRef.UAxis = ua.Normalized();
				TextureRef.VAxis = va.Normalized();
			}
			else
			{
				AlignTextureToFace();
			}

			if ( flags.HasFlag( TransformFlags.TextureLock ) && TextureRef.Texture != null )
			{
				// Check some original reference points to see how the transform mutates them
				float scaled = (transform.Transform( OpenTK.Mathematics.Vector3.One ) - transform.Transform( OpenTK.Mathematics.Vector3.Zero )).VectorMagnitude();
				float original = (OpenTK.Mathematics.Vector3.One - OpenTK.Mathematics.Vector3.Zero).VectorMagnitude();

				// Ignore texture lock when the transformation contains a scale
				if ( MathF.Abs( scaled - original ) <= 0.01f )
				{
					// Calculate the new shift values based on the UV values of the vertices
					Vertex vtx = Vertices[0];
					TextureRef.XShift = TextureRef.Texture.Width * vtx.TextureU - (Vector3.Dot( vtx.Position, TextureRef.UAxis )) / TextureRef.XScale;
					TextureRef.YShift = TextureRef.Texture.Height * vtx.TextureV - (Vector3.Dot( vtx.Position, TextureRef.VAxis )) / TextureRef.YScale;
				}
			}
		}
		CalculateTextureCoordinates( true );
		UpdateBoundingBox();
	}

	public virtual void Flip()
	{
		Vertices.Reverse();
		Plane = new Plane( Vertices[0].Position, Vertices[1].Position, Vertices[2].Position );
		UpdateBoundingBox();
	}

	/// <summary>
	/// Returns the point that this line intersects with this face.
	/// </summary>
	/// <param name="line">The intersection line</param>
	/// <returns>The point of intersection between the face and the line.
	/// Returns null if the line does not intersect this face.</returns>
	public virtual Vector3 GetIntersectionPoint( Line line, bool ignoreDirection = false, bool ignoreSegment = false )
	{
		return GetIntersectionPoint( Vertices.Select( x => x.Position ).ToList(), line, ignoreDirection, ignoreSegment );
	}

	/// <summary>
	/// Test all the edges of this face against a bounding box to see if they intersect.
	/// </summary>
	/// <param name="box">The box to intersect</param>
	/// <returns>True if one of the face's edges intersects with the box.</returns>
	public bool IntersectsWithLine( Box box )
	{
		// Shortcut through the bounding box to avoid the line computations if they aren't needed
		return BoundingBox.IntersectsWith( box ) && GetLines().Any( box.IntersectsWith );
	}

	/// <summary>
	/// Test this face to see if the given bounding box intersects with it
	/// </summary>
	/// <param name="box">The box to test against</param>
	/// <returns>True if the box intersects</returns>
	public bool IntersectsWithBox( Box box )
	{
		List<OpenTK.Mathematics.Vector3> verts = Vertices.Select( x => x.Position ).ToList();
		return box.GetBoxLines().Any( x => GetIntersectionPoint( verts, x, true ) != default );
	}

	/// <summary>
	/// Determines if this face is behind, in front, or spanning a plane.
	/// </summary>
	/// <param name="p">The plane to test against</param>
	/// <returns>A PlaneClassification value.</returns>
	public PlaneClassification ClassifyAgainstPlane( Plane p )
	{
		int front = 0, back = 0, onplane = 0, count = Vertices.Count;

		foreach ( int test in Vertices.Select( v => v.Position ).Select( x => p.OnPlane( x ) ) )
		{
			// Vertices on the plane are both in front and behind the plane in this context
			if ( test <= 0 ) back++;
			if ( test >= 0 ) front++;
			if ( test == 0 ) onplane++;
		}

		if ( onplane == count ) return PlaneClassification.OnPlane;
		if ( front == count ) return PlaneClassification.Front;
		if ( back == count ) return PlaneClassification.Back;
		return PlaneClassification.Spanning;
	}

	public void IncreaseTexelSize()
	{
		var sz = TexelSize * 2;
		if ( sz > maxTexelSize )
		{
			sz = minTexelSize;
		}
		TexelSize = sz;
	}

	public void DecreaseTexelSize()
	{
		var sz = TexelSize / 2;
		if ( sz < minTexelSize )
		{
			sz = maxTexelSize;
		}
		TexelSize = sz;
	}

	public void RecalculateNormal()
	{
		if ( Vertices.Count < 3 )
		{
			throw new InvalidOperationException( "Face must have at least 3 vertices to calculate a normal." );
		}

		Vector3 v1 = Vertices[1].Position - Vertices[0].Position;
		Vector3 v2 = Vertices[2].Position - Vertices[0].Position;

		Vector3 normal = Vector3.Cross( v1, v2 ).Normalized();

		// Check if the normal is pointing in the same direction as the current normal
		// If not, we need to flip it to maintain consistency
		if ( Vector3.Dot( normal, Plane.Normal ) < 0 )
		{
			normal = -normal;
		}

		// Create a new plane with the recalculated normal and a point from the face
		Plane = new Plane( normal, Vertices[0].Position );

		// Optionally, you might want to check if the face is still planar
		// and warn if it's not (due to vertex modifications)
		if ( !IsPlanar() )
		{
			Debug.LogWarning( "Face is not perfectly planar after recalculating normal." );
		}
	}

	public bool IsPlanar( float tolerance = 0.5f )
	{
		if ( Vertices.Count < 3 )
		{
			return true; // A face with less than 3 vertices is always planar (though it might be invalid geometrically)
		}

		// Check if the first three vertices are not collinear
		Vector3 v1 = Vertices[1].Position - Vertices[0].Position;
		Vector3 v2 = Vertices[2].Position - Vertices[0].Position;
		return Vector3.Cross( v1, v2 ).LengthSquared >= tolerance * tolerance;
	}

	public Vector3 CalculateCenter()
	{
		var result = Vector3.Zero;
		if ( Vertices.Count == 0 )
			return result;

		foreach ( var v in Vertices )
		{
			result += v.Position;
		}

		return result / Vertices.Count;
	}

	public bool ContainsEdge( Line edge, float epsilon = 0.01f )
	{
		for ( int i = 0; i < Vertices.Count; i++ )
		{
			var v1 = Vertices[i].Position;
			var v2 = Vertices[(i + 1) % Vertices.Count].Position;

			if ( (v1.EquivalentTo( edge.Start, epsilon ) && v2.EquivalentTo( edge.End, epsilon )) ||
				(v1.EquivalentTo( edge.End, epsilon ) && v2.EquivalentTo( edge.Start, epsilon )) )
			{
				return true;
			}
		}
		return false;
	}

	public void RecalculateTexelSize()
	{
		var direction = Plane.GetClosestAxisToNormal();
		var tempV = direction == Vector3.UnitZ ? Vector3.UnitY : Vector3.UnitZ;
		var uAxis = Vector3.Cross( Plane.Normal, tempV ).Normalized();
		var vAxis = Vector3.Cross( uAxis, Plane.Normal ).Normalized();

		var minU = float.MaxValue;
		var minV = float.MaxValue;
		float maxU = float.MinValue, maxV = float.MinValue;

		foreach ( var vertex in Vertices )
		{
			var u = Vector3.Dot( vertex.Position, uAxis );
			var v = Vector3.Dot( vertex.Position, vAxis );
			minU = Math.Min( minU, u );
			minV = Math.Min( minV, v );
			maxU = Math.Max( maxU, u );
			maxV = Math.Max( maxV, v );
		}

		minU = (float)Math.Floor( minU );
		minV = (float)Math.Floor( minV );
		maxU = (float)Math.Ceiling( maxU );
		maxV = (float)Math.Ceiling( maxV );

		var width = maxU - minU;
		var height = maxV - minV;
		var area = width * height;

		float minArea = 16 * 16;      // Minimum face area (16x16 units)
		float maxArea = 1024 * 1024;  // Maximum face area (1024x1024 units)

		// Calculate the logarithmic scale factor with a steeper curve
		float logMinArea = MathF.Log( minArea );
		float logMaxArea = MathF.Log( maxArea );
		float logRange = logMaxArea - logMinArea;

		float normalizedLogArea = (MathF.Log( area ) - logMinArea) / logRange;
		float curvedNormalizedArea = MathF.Pow( normalizedLogArea, 4 );
		float texelSizeFloat = minTexelSize + (maxTexelSize - minTexelSize) * curvedNormalizedArea;

		TexelSize = (int)MathF.Pow( 2, MathF.Round( MathF.Log2( texelSizeFloat ) ) );
		TexelSize = Math.Clamp( TexelSize, minTexelSize, maxTexelSize );
	}

	protected static Vector3 GetIntersectionPoint( IList<Vector3> coordinates, Line line, bool ignoreDirection = false, bool ignoreSegment = false )
	{
		var plane = new Plane( coordinates[0], coordinates[1], coordinates[2] );
		var intersect = plane.GetIntersectionPoint( line, ignoreDirection, ignoreSegment );
		if ( intersect == default ) return default;

		// http://paulbourke.net/geometry/insidepoly/

		// The angle sum will be 2 * PI if the point is inside the face
		double sum = 0;
		for ( int i = 0; i < coordinates.Count; i++ )
		{
			int i1 = i;
			int i2 = (i + 1) % coordinates.Count;

			// Translate the vertices so that the intersect point is on the origin
			var v1 = coordinates[i1] - intersect;
			var v2 = coordinates[i2] - intersect;

			var m1 = v1.LengthSquared;
			var m2 = v2.LengthSquared;
			var nom = m1 * m2;
			if ( nom < 0.00001d )
			{
				// intersection is at a vertex
				return intersect;
			}
			nom = MathF.Sqrt( nom );
			sum += MathF.Acos( v1.Dot( v2 ) / nom );
		}

		double delta = Math.Abs( sum - Math.PI * 2 );
		return (delta < 0.001d) ? intersect : default;
	}
}
