
using System;

namespace Graybox;

/// <summary>
/// Defines a plane in the form Ax + By + Cz + D = 0
/// </summary>
public struct Plane
{

	public Vector3 Normal { get; private set; }
	public float DistanceFromOrigin { get; private set; }
	public float A { get; private set; }
	public float B { get; private set; }
	public float C { get; private set; }
	public float D { get; private set; }
	public Vector3 PointOnPlane { get; private set; }

	public Plane( Vector3 p1, Vector3 p2, Vector3 p3 )
	{
		Vector3 ab = p2 - p1;
		Vector3 ac = p3 - p1;

		Normal = ac.Cross( ab ).Normalized();
		DistanceFromOrigin = Normal.Dot( p1 );
		PointOnPlane = p1;

		A = Normal.X;
		B = Normal.Y;
		C = Normal.Z;
		D = -DistanceFromOrigin;
	}

	public Plane( Vector3 norm, Vector3 pointOnPlane )
	{
		Normal = norm.Normalized();
		DistanceFromOrigin = Normal.Dot( pointOnPlane );
		PointOnPlane = pointOnPlane;
		A = Normal.X;
		B = Normal.Y;
		C = Normal.Z;
		D = -DistanceFromOrigin;
	}

	public Plane( Vector3 norm, float distanceFromOrigin )
	{
		Normal = norm.Normalized();
		DistanceFromOrigin = distanceFromOrigin;
		PointOnPlane = Normal * DistanceFromOrigin;
		A = Normal.X;
		B = Normal.Y;
		C = Normal.Z;
		D = -DistanceFromOrigin;  // This is correct
	}

	///  <summary>Finds if the given point is above, below, or on the plane.</summary>
	///  <param name="co">The coordinate to test</param>
	/// <param name="epsilon">Tolerance value</param>
	/// <returns>
	///  value == -1 if coordinate is below the plane<br />
	///  value == 1 if coordinate is above the plane<br />
	///  value == 0 if coordinate is on the plane.
	/// </returns>
	public int OnPlane( Vector3 co, float epsilon = .5f )
	{
		//eval (s = Ax + By + Cz + D) at point (x,y,z)
		//if s > 0 then point is "above" the plane (same side as normal)
		//if s < 0 then it lies on the opposite side
		//if s = 0 then the point (x,y,z) lies on the plane
		var res = EvalAtPoint( co );
		if ( MathF.Abs( res ) < epsilon ) return 0;
		if ( res < 0 ) return -1;
		return 1;
	}

	public Vector3 GetIntersectionPoint( Line line, bool ignoreDirection = false, bool ignoreSegment = false )
	{
		// http://softsurfer.com/Archive/algorithm_0104/algorithm_0104B.htm#Line%20Intersections
		// http://paulbourke.net/geometry/planeline/

		Vector3 dir = line.End - line.Start;
		var denominator = -Normal.Dot( dir );
		var numerator = Normal.Dot( line.Start - Normal * DistanceFromOrigin );
		if ( MathF.Abs( denominator ) < 0.00001f || (!ignoreDirection && denominator < 0) ) return default;
		var u = numerator / denominator;
		if ( !ignoreSegment && (u < 0 || u > 1) ) return default;
		return line.Start + u * dir;
	}

	public bool Intersect( Ray ray, out Vector3 intersectionPoint )
	{
		intersectionPoint = Vector3.Zero;
		float denominator = Vector3.Dot( Normal, ray.Direction );

		if ( Math.Abs( denominator ) < float.Epsilon )
		{
			return false;
		}

		float t = Vector3.Dot( PointOnPlane - ray.Origin, Normal ) / denominator;

		if ( t < 0 )
		{
			return false;
		}

		intersectionPoint = ray.Origin + t * ray.Direction;
		return true;
	}

	/// <summary>
	/// Project a point into the space of this plane. I.e. Get the point closest
	/// to the provided point that is on this plane.
	/// </summary>
	/// <param name="point">The point to project</param>
	/// <returns>The point projected onto this plane</returns>
	public Vector3 Project( Vector3 point )
	{
		// http://www.gamedev.net/topic/262196-projecting-vector-onto-a-plane/
		// Projected = Point - ((Point - PointOnPlane) . Normal) * Normal
		return point - ((point - PointOnPlane).Dot( Normal )) * Normal;
	}

	public float EvalAtPoint( Vector3 co )
	{
		return A * co.X + B * co.Y + C * co.Z + D;
	}

	/// <summary>
	/// Calculates the signed distance from a point to the plane.
	/// </summary>
	/// <param name="point">The point to calculate the distance to.</param>
	/// <returns>
	/// The signed distance from the point to the plane.
	/// Positive if the point is on the side the normal points to,
	/// negative if it's on the opposite side,
	/// and zero if the point is exactly on the plane.
	/// </returns>
	public float DistanceToPoint( Vector3 point )
	{
		return Vector3.Dot( Normal, point ) - DistanceFromOrigin;
	}

	/// <summary>
	/// Gets the axis closest to the normal of this plane
	/// </summary>
	/// <returns>Coordinate.UnitX, Coordinate.UnitY, or Coordinate.UnitZ depending on the plane's normal</returns>
	public Vector3 GetClosestAxisToNormal()
	{
		// VHE prioritises the axes in order of X, Y, Z.
		Vector3 norm = Normal.Absolute();

		if ( norm.X >= norm.Y && norm.X >= norm.Z ) return Vector3.UnitX;
		if ( norm.Y >= norm.Z ) return Vector3.UnitY;
		return Vector3.UnitZ;
	}

	public Vector3 GetClosestAxisToNormal2()
	{
		Vector3 norm = Normal.Absolute();
		if ( norm.Y >= norm.X && norm.Y >= norm.Z ) return Vector3.UnitY; // If Y is dominant
		if ( norm.X >= norm.Z ) return Vector3.UnitX;  // If X is dominant
		return Vector3.UnitZ;  // Default to Z if it's most aligned
	}

	/// <summary>
	/// Intersects three planes and gets the point of their intersection.
	/// </summary>
	/// <returns>The point that the planes intersect at, or null if they do not intersect at a point.</returns>
	public static Vector3 Intersect( Plane p1, Plane p2, Plane p3 )
	{
		// http://paulbourke.net/geometry/3planes/

		Vector3 c1 = p2.Normal.Cross( p3.Normal );
		Vector3 c2 = p3.Normal.Cross( p1.Normal );
		Vector3 c3 = p1.Normal.Cross( p2.Normal );

		var denom = p1.Normal.Dot( c1 );
		if ( denom < 0.00001f ) return default;

		Vector3 numer = (-p1.D * c1) + (-p2.D * c2) + (-p3.D * c3);
		return numer / denom;
	}

	public readonly bool EquivalentTo( Plane other, float delta = 0.0001f )
	{
		return Normal.EquivalentTo( other.Normal, delta )
			   && MathF.Abs( DistanceFromOrigin - other.DistanceFromOrigin ) < delta;
	}

	public readonly bool Equals( Plane other )
	{
		return Normal.Equals( other.Normal ) && DistanceFromOrigin.Equals( other.DistanceFromOrigin );
	}

	public readonly override bool Equals( object obj )
	{
		if ( obj is Plane otherPlane )
		{
			return Equals( otherPlane );
		}
		return false;
	}

	public readonly override int GetHashCode()
	{
		return HashCode.Combine( Normal, DistanceFromOrigin );
	}

	public static bool operator ==( Plane left, Plane right )
	{
		return Equals( left, right );
	}

	public static bool operator !=( Plane left, Plane right )
	{
		return !Equals( left, right );
	}

}
