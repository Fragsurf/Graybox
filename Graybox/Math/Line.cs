
using Graybox.DataStructures.Transformations;
using Graybox.Utility;

namespace Graybox;

public class Line
{

	public Vector3 Start { get; set; }
	public Vector3 End { get; set; }

	public static readonly Line AxisX = new( Vector3.Zero, Vector3.UnitX );
	public static readonly Line AxisY = new( Vector3.Zero, Vector3.UnitY );
	public static readonly Line AxisZ = new( Vector3.Zero, Vector3.UnitZ );

	public Line( Vector3 start, Vector3 end )
	{
		Start = start;
		End = end;
	}

	public Line Reverse()
	{
		return new Line( End, Start );
	}

	public Vector3 ClosestPoint( Vector3 point )
	{
		// http://paulbourke.net/geometry/pointline/

		Vector3 delta = End - Start;
		var den = delta.LengthSquared;
		if ( den == 0 ) return Start; // Start and End are the same

		Vector3 numPoint = Vector3.Multiply( point - Start, delta );
		var num = numPoint.X + numPoint.Y + numPoint.Z;
		var u = num / den;

		if ( u < 0 ) return Start; // Point is before the segment start
		if ( u > 1 ) return End;   // Point is after the segment end
		return Start + u * delta;
	}

	/// <summary>
	/// Determines if this line is behind, in front, or spanning a plane.
	/// </summary>
	/// <param name="p">The plane to test against</param>
	/// <returns>A PlaneClassification value.</returns>
	public PlaneClassification ClassifyAgainstPlane( Plane p )
	{
		int start = p.OnPlane( Start );
		int end = p.OnPlane( End );

		if ( start == 0 && end == 0 ) return PlaneClassification.OnPlane;
		if ( start <= 0 && end <= 0 ) return PlaneClassification.Back;
		if ( start >= 0 && end >= 0 ) return PlaneClassification.Front;
		return PlaneClassification.Spanning;
	}

	public Line Transform( IUnitTransformation transform )
	{
		return new Line( transform.Transform( Start ), transform.Transform( End ) );
	}

	public bool EquivalentTo( Line other, float delta = 0.0001f )
	{
		return (Start.AlmostEqual( other.Start, delta ) && End.AlmostEqual( other.End, delta ))
			|| (End.AlmostEqual( other.Start, delta ) && Start.AlmostEqual( other.End, delta ));
	}

	public bool IntersectsWith( Line other, float buffer = 0.0001f )
	{
		Vector3 u = End - Start;
		Vector3 v = other.End - other.Start;
		Vector3 w = Start - other.Start;

		float a = Vector3.Dot( u, u );
		float b = Vector3.Dot( u, v );
		float c = Vector3.Dot( v, v );
		float d = Vector3.Dot( u, w );
		float e = Vector3.Dot( v, w );
		float D = a * c - b * b;

		float sc, tc;

		if ( D < buffer )
		{
			// The lines are almost parallel
			sc = 0.0f;
			tc = (b > c ? d / b : e / c);
		}
		else
		{
			sc = (b * e - c * d) / D;
			tc = (a * e - b * d) / D;
		}

		Vector3 dP = w + (sc * u) - (tc * v);

		// Check if the distance between the closest points is within the buffer
		return dP.Length <= buffer;
	}

	public bool IntersectsWithFinite( Line other, float buffer = 0.0001f )
	{
		Vector3 u = End - Start;
		Vector3 v = other.End - other.Start;
		Vector3 w = Start - other.Start;

		float a = Vector3.Dot( u, u );
		float b = Vector3.Dot( u, v );
		float c = Vector3.Dot( v, v );
		float d = Vector3.Dot( u, w );
		float e = Vector3.Dot( v, w );
		float D = a * c - b * b;

		float sc, sN, sD = D;
		float tc, tN, tD = D;

		if ( D < buffer )
		{
			// The lines are almost parallel
			sN = 0.0f;
			sD = 1.0f;
			tN = e;
			tD = c;
		}
		else
		{
			sN = (b * e - c * d);
			tN = (a * e - b * d);
			if ( sN < 0.0f )
			{
				sN = 0.0f;
				tN = e;
				tD = c;
			}
			else if ( sN > sD )
			{
				sN = sD;
				tN = e + b;
				tD = c;
			}
		}

		if ( tN < 0.0f )
		{
			tN = 0.0f;
			if ( -d < 0.0f )
				sN = 0.0f;
			else if ( -d > a )
				sN = sD;
			else
			{
				sN = -d;
				sD = a;
			}
		}
		else if ( tN > tD )
		{
			tN = tD;
			if ( (-d + b) < 0.0f )
				sN = 0;
			else if ( (-d + b) > a )
				sN = sD;
			else
			{
				sN = (-d + b);
				sD = a;
			}
		}

		sc = (Math.Abs( sN ) < buffer) ? 0.0f : sN / sD;
		tc = (Math.Abs( tN ) < buffer) ? 0.0f : tN / tD;

		Vector3 dP = w + (sc * u) - (tc * v);

		return dP.Length <= buffer;
	}

	public Vector3? GetIntersectionPointFinite( Line other, float buffer = 0.0001f )
	{
		Vector3 u = End - Start;
		Vector3 v = other.End - other.Start;
		Vector3 w = Start - other.Start;
		float a = Vector3.Dot( u, u );
		float b = Vector3.Dot( u, v );
		float c = Vector3.Dot( v, v );
		float d = Vector3.Dot( u, w );
		float e = Vector3.Dot( v, w );
		float D = a * c - b * b;
		float sc, sN, sD = D;
		float tc, tN, tD = D;

		if ( D < buffer )
		{
			// The lines are almost parallel
			sN = 0.0f;
			sD = 1.0f;
			tN = e;
			tD = c;
		}
		else
		{
			sN = (b * e - c * d);
			tN = (a * e - b * d);
			if ( sN < 0.0f )
			{
				sN = 0.0f;
				tN = e;
				tD = c;
			}
			else if ( sN > sD )
			{
				sN = sD;
				tN = e + b;
				tD = c;
			}
		}

		if ( tN < 0.0f )
		{
			tN = 0.0f;
			if ( -d < 0.0f )
				sN = 0.0f;
			else if ( -d > a )
				sN = sD;
			else
			{
				sN = -d;
				sD = a;
			}
		}
		else if ( tN > tD )
		{
			tN = tD;
			if ( (-d + b) < 0.0f )
				sN = 0;
			else if ( (-d + b) > a )
				sN = sD;
			else
			{
				sN = (-d + b);
				sD = a;
			}
		}

		sc = (Math.Abs( sN ) < buffer) ? 0.0f : sN / sD;
		tc = (Math.Abs( tN ) < buffer) ? 0.0f : tN / tD;
		Vector3 dP = w + (sc * u) - (tc * v);

		if ( dP.Length <= buffer )
		{
			// Calculate the intersection point
			return Start + sc * u;
		}
		else
		{
			// No intersection
			return null;
		}
	}

	public bool Equals( Line other )
	{
		if ( ReferenceEquals( null, other ) ) return false;
		if ( ReferenceEquals( this, other ) ) return true;
		return (Equals( other.Start, Start ) && Equals( other.End, End ))
			|| (Equals( other.End, Start ) && Equals( other.Start, End ));
	}

	public override bool Equals( object obj )
	{
		if ( ReferenceEquals( null, obj ) ) return false;
		if ( ReferenceEquals( this, obj ) ) return true;
		if ( obj.GetType() != typeof( Line ) ) return false;
		return Equals( (Line)obj );
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (Start.GetHashCode() * 397) ^ (End.GetHashCode());
		}
	}

	public static bool operator ==( Line left, Line right )
	{
		return Equals( left, right );
	}

	public static bool operator !=( Line left, Line right )
	{
		return !Equals( left, right );
	}

}
