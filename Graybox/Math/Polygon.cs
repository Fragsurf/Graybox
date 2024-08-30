
using Graybox.DataStructures.Transformations;

namespace Graybox;

/// <summary>
/// Represents a coplanar, directed polygon with at least 3 vertices.
/// </summary>
public class Polygon
{

	public List<Vector3> Vertices { get; set; }
	public Plane Plane { get; set; }

	/// <summary>
	/// Creates a polygon from a list of points
	/// </summary>
	/// <param name="vertices">The vertices of the polygon</param>
	public Polygon( IEnumerable<Vector3> vertices )
	{
		Vertices = vertices.ToList();
		Plane = new Plane( Vertices[0], Vertices[1], Vertices[2] );
		Simplify();
	}

	/// <summary>
	/// Creates a polygon from a plane and a radius.
	/// Expands the plane to the radius size to create a large polygon with 4 vertices.
	/// </summary>
	/// <param name="plane">The polygon plane</param>
	/// <param name="radius">The polygon radius</param>
	public Polygon( Plane plane, float radius = 1000000f )
	{
		Plane = plane;

		Vector3 normal = plane.Normal;
		Vector3 point = plane.PointOnPlane;

		// Create three points far away in the positive direction of the plane
		Vector3 v1 = point + normal.Perpendicular() * radius + normal.Cross( normal.Perpendicular() ) * radius;
		Vector3 v2 = point - normal.Perpendicular() * radius + normal.Cross( normal.Perpendicular() ) * radius;
		Vector3 v3 = point + normal.Perpendicular() * radius - normal.Cross( normal.Perpendicular() ) * radius;
		Vector3 v4 = point - normal.Perpendicular() * radius - normal.Cross( normal.Perpendicular() ) * radius;

		Vertices = new() { v1, v2, v3, v4 };

		//// Get aligned up and right axes to the plane
		//Vector3 direction = Plane.GetClosestAxisToNormal();
		//Vector3 tempV = direction == Vector3.UnitZ ? -Vector3.UnitY : -Vector3.UnitZ;
		//Vector3 up = tempV.Cross( Plane.Normal ).Normalized();
		//Vector3 right = Plane.Normal.Cross( up ).Normalized();

		//Vertices = new List<Vector3>
		//			   {
		//				   plane.PointOnPlane + right + up, // Top right
		//                             plane.PointOnPlane - right + up, // Top left
		//                             plane.PointOnPlane - right - up, // Bottom left
		//                             plane.PointOnPlane + right - up, // Bottom right
		//                         };
		//Expand( radius );
	}

	public float Perimeter()
	{
		float perimeter = 0;
		for ( int i = 0; i < Vertices.Count; i++ )
		{
			var v1 = Vertices[i];
			var v2 = Vertices[(i + 1) % Vertices.Count];
			perimeter += (v2 - v1).Length;
		}
		return perimeter;
	}

	public Polygon Clone()
	{
		return new Polygon( new List<Vector3>( Vertices ) );
	}

	public void Unclone( Polygon polygon )
	{
		Vertices = new List<Vector3>( polygon.Vertices );
		Plane = polygon.Plane;
	}

	public Vector3 GetCenter()
	{
		if ( Vertices.Count <= 0 )
			return Vector3.Zero;

		var result = Vector3.Zero;

		foreach ( var point in Vertices )
		{
			result += point;
		}

		return result / Vertices.Count;
	}

	public IEnumerable<Line> GetLines()
	{
		for ( int i = 1; i < Vertices.Count; i++ )
		{
			yield return new Line( Vertices[i - 1], Vertices[i] );
		}
	}

	/// <summary>
	/// Checks that all the points in this polygon are valid.
	/// </summary>
	/// <returns>True if the plane is valid</returns>
	public bool IsPlanar()
	{
		return Vertices.All( x => Plane.OnPlane( x ) == 0 );
	}

	/// <summary>
	/// Removes any colinear vertices in the polygon
	/// </summary>
	public void Simplify()
	{
		// Remove colinear vertices
		for ( int i = 0; i < Vertices.Count - 2; i++ )
		{
			Vector3 v1 = Vertices[i];
			Vector3 v2 = Vertices[i + 2];
			Vector3 p = Vertices[i + 1];
			Line line = new Line( v1, v2 );
			// If the midpoint is on the line, remove it
			if ( line.ClosestPoint( p ).EquivalentTo( p ) )
			{
				Vertices.RemoveAt( i + 1 );
			}
		}
	}

	/// <summary>
	/// Transforms all the points in the polygon.
	/// </summary>
	/// <param name="transform">The transformation to perform</param>
	public void Transform( IUnitTransformation transform )
	{
		Vertices = Vertices.Select( transform.Transform ).ToList();
		Plane = new Plane( Vertices[0], Vertices[1], Vertices[2] );
	}

	public bool IsConvex( float epsilon = 0.001f )
	{
		for ( int i = 0; i < Vertices.Count; i++ )
		{
			Vector3 v1 = Vertices[i];
			Vector3 v2 = Vertices[(i + 1) % Vertices.Count];
			Vector3 v3 = Vertices[(i + 2) % Vertices.Count];
			Vector3 l1 = (v1 - v2).Normalized();
			Vector3 l2 = (v3 - v2).Normalized();
			Vector3 cross = l1.Cross( l2 );
			if ( Plane.OnPlane( v2 + cross, epsilon ) < 0.0001f ) return false;
		}
		return true;
	}

	/// <summary>
	/// Expands this plane's points outwards from the origin by a radius value.
	/// </summary>
	/// <param name="radius">The distance the points will be from the origin after expanding</param>
	public void Expand( float radius )
	{
		// 1. Center the polygon at the world origin
		// 2. Normalise all the vertices
		// 3. Multiply them by the radius
		// 4. Move the polygon back to the original origin
		Vector3 origin = GetCenter();
		Vertices = Vertices.Select( x => (x - origin).Normalized() * radius + origin ).ToList();
		Plane = new Plane( Vertices[0], Vertices[1], Vertices[2] );
	}

	/// <summary>
	/// Determines if this polygon is behind, in front, or spanning a plane.
	/// </summary>
	/// <param name="p">The plane to test against</param>
	/// <returns>A PlaneClassification value.</returns>
	public PlaneClassification ClassifyAgainstPlane( Plane p )
	{
		int front = 0, back = 0, onplane = 0, count = Vertices.Count;

		foreach ( int test in Vertices.Select( x => p.OnPlane( x ) ) )
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

	/// <summary>
	/// Splits this polygon by a clipping plane, discarding the front side.
	/// The original polygon is modified to be the back side of the split.
	/// </summary>
	/// <param name="clip">The clipping plane</param>
	public void Split( Plane clip )
	{
		Polygon front, back;
		if ( Split( clip, out back, out front ) )
		{
			Unclone( back );
		}
	}

	/// <summary>
	/// Splits this polygon by a clipping plane, returning the back and front planes.
	/// The original polygon is not modified.
	/// </summary>
	/// <param name="clip">The clipping plane</param>
	/// <param name="back">The back polygon</param>
	/// <param name="front">The front polygon</param>
	/// <returns>True if the split was successful</returns>
	public bool Split( Plane clip, out Polygon back, out Polygon front )
	{
		Polygon cFront, cBack;
		return Split( clip, out back, out front, out cBack, out cFront );
	}

	/// <summary>
	/// Splits this polygon by a clipping plane, returning the back and front planes.
	/// The original polygon is not modified.
	/// </summary>
	/// <param name="clip">The clipping plane</param>
	/// <param name="back">The back polygon</param>
	/// <param name="front">The front polygon</param>
	/// <param name="coplanarBack">If the polygon rests on the plane and points backward, this will not be null</param>
	/// <param name="coplanarFront">If the polygon rests on the plane and points forward, this will not be null</param>
	/// <returns>True if the split was successful</returns>
	public bool Split( Plane clip, out Polygon back, out Polygon front, out Polygon coplanarBack, out Polygon coplanarFront )
	{
		// If the polygon doesn't span the plane, return false.
		PlaneClassification classify = ClassifyAgainstPlane( clip );
		if ( classify != PlaneClassification.Spanning )
		{
			back = front = null;
			coplanarBack = coplanarFront = null;
			if ( classify == PlaneClassification.Back ) back = this;
			else if ( classify == PlaneClassification.Front ) front = this;
			else if ( Plane.Normal.Dot( clip.Normal ) > 0 ) coplanarFront = this;
			else coplanarBack = this;
			return false;
		}

		// Get the new front and back vertices
		List<Vector3> backVerts = new List<Vector3>();
		List<Vector3> frontVerts = new List<Vector3>();
		int prev = 0;

		for ( int i = 0; i <= Vertices.Count; i++ )
		{
			Vector3 end = Vertices[i % Vertices.Count];
			int cls = clip.OnPlane( end );

			// Check plane crossing
			if ( i > 0 && cls != 0 && prev != 0 && prev != cls )
			{
				// This line end point has crossed the plane
				// Add the line intersect to the 
				Vector3 start = Vertices[i - 1];
				Line line = new Line( start, end );
				Vector3 isect = clip.GetIntersectionPoint( line, true );
				if ( isect == default ) throw new Exception( "Expected intersection, got null." );
				frontVerts.Add( isect );
				backVerts.Add( isect );
			}

			// Add original points
			if ( i < Vertices.Count )
			{
				// OnPlane points get put in both polygons, doesn't generate split
				if ( cls >= 0 ) frontVerts.Add( end );
				if ( cls <= 0 ) backVerts.Add( end );
			}

			prev = cls;
		}

		back = new Polygon( backVerts );
		front = new Polygon( frontVerts );
		coplanarBack = coplanarFront = null;

		return true;
	}

	public void Flip()
	{
		Vertices.Reverse();
		Plane = new Plane( -Plane.Normal, Plane.PointOnPlane );
	}

	public bool IsPointInside( Vector3 point )
	{
		if ( Vertices.Count < 3 )
		{
			throw new InvalidOperationException( "A polygon must have at least three vertices." );
		}

		// First, ensure the point is coplanar to the polygon's plane
		if ( Math.Abs( Plane.OnPlane( point ) ) > 1e-5f )
		{
			return false; // The point is not coplanar to the polygon's plane
		}

		// Project all points (polygon vertices and the test point) onto a 2D plane
		Vector3 referencePoint = Vertices[0];
		Vector3 basis1 = (Vertices[1] - referencePoint).Normalized();
		Vector3 basis2 = Vector3.Cross( Plane.Normal, basis1 ).Normalized();

		// Convert 3D points to 2D
		var projectedPoint = new Vector2( Vector3.Dot( point - referencePoint, basis1 ), Vector3.Dot( point - referencePoint, basis2 ) );
		List<Vector2> projectedVertices = Vertices.Select( vertex =>
			new Vector2( Vector3.Dot( vertex - referencePoint, basis1 ), Vector3.Dot( vertex - referencePoint, basis2 ) ) ).ToList();

		// Use the ray-casting algorithm to determine if the point is inside the polygon
		int crossings = 0;
		for ( int i = 0; i < projectedVertices.Count; i++ )
		{
			int j = (i + 1) % projectedVertices.Count;
			Vector2 p1 = projectedVertices[i];
			Vector2 p2 = projectedVertices[j];

			// Check if the ray can intersect the line segment from p1 to p2
			if ( ((p1.Y <= projectedPoint.Y && projectedPoint.Y < p2.Y) || (p2.Y <= projectedPoint.Y && projectedPoint.Y < p1.Y)) &&
				(projectedPoint.X < (p2.X - p1.X) * (projectedPoint.Y - p1.Y) / (p2.Y - p1.Y) + p1.X) )
			{
				crossings++;
			}
		}

		// Point is inside the polygon if the number of crossings is odd
		return (crossings % 2 != 0);
	}

}
