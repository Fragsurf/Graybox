

namespace Graybox;

public static class VectorExtensions
{

	public static Vector3 Absolute( this Vector3 input ) => new Vector3( MathF.Abs( input.X ), MathF.Abs( input.Y ), MathF.Abs( input.Z ) );
	public static Vector3 Cross( this Vector3 input, Vector3 other ) => Vector3.Cross( input, other );
	public static Vector3 ComponentMultiply( this Vector3 input, Vector3 other ) => Vector3.Multiply( input, other );
	public static Vector3 ComponentDivide( this Vector3 input, Vector3 other ) => Vector3.Divide( input, other );
	public static bool EquivalentTo( this Vector3 input, Vector3 other, float threshold = 0.0001f )
	{
		return MathF.Abs( input.X - other.X ) < threshold &&
		   MathF.Abs( input.Y - other.Y ) < threshold &&
		   MathF.Abs( input.Z - other.Z ) < threshold;
	}
	public static float VectorMagnitude( this Vector3 input ) => MathF.Sqrt( MathF.Pow( input.X, 2 ) + MathF.Pow( input.Y, 2 ) + MathF.Pow( input.Z, 2 ) );
	public static float Dot( this Vector3 input, Vector3 other ) => Vector3.Dot( input, other );
	public static Vector3 WithX( this Vector3 input, float x ) => new Vector3( x, input.Y, input.Z );
	public static Vector3 WithY( this Vector3 input, float y ) => new Vector3( input.X, y, input.Z );
	public static Vector3 WithZ( this Vector3 input, float z ) => new Vector3( input.X, input.Y, z );
	public static Vector3 Round( this Vector3 input, int num = 6 )
	{
		num = MathHelper.Clamp( num, 0, 6 );
		return new Vector3( MathF.Round( input.X, num ), MathF.Round( input.Y, num ), MathF.Round( input.Z, num ) );
	}

	public static Vector3 Project( this Vector3 a, Vector3 b )
	{
		float dotProduct = Vector3.Dot( a, b );
		float magnitudeSquared = Vector3.Dot( b, b );
		return (dotProduct / magnitudeSquared) * b;
	}

	public static Vector3 ProjectOntoPlane( this Vector3 vector, Vector3 axis )
	{
		Vector3 projection = vector - Vector3.Dot( vector, axis ) / Vector3.Dot( axis, axis ) * axis;
		return Vector3.Normalize( projection );
	}

	public static bool IsParallel( this Vector3 a, Vector3 b )
	{
		float tolerance = 1e-6f;
		Vector3 crossProduct = Vector3.Cross( a, b );
		return crossProduct.LengthSquared < tolerance * tolerance;
	}

	public static Vector3 Snap( this Vector3 input, float snapTo )
	{
		return new Vector3(
			MathF.Round( input.X / snapTo ) * snapTo,
			MathF.Round( input.Y / snapTo ) * snapTo,
			MathF.Round( input.Z / snapTo ) * snapTo
		);
	}


	public static System.Numerics.Vector3 ToNumerics( this Vector3 input ) => new System.Numerics.Vector3( input.X, input.Y, input.Z );
	public static Vector3 FromNumerics( this System.Numerics.Vector3 input ) => new Vector3( input.X, input.Y, input.Z );

	public static System.Numerics.Quaternion ToNumerics( this Quaternion input ) => new System.Numerics.Quaternion( input.X, input.Y, input.Z, input.W );
	public static Quaternion FromNumerics( this System.Numerics.Quaternion input ) => new Quaternion( input.X, input.Y, input.Z, input.W );

	public static string ToDataString( this Vector3 input )
	{
		Func<float, string> toStringNoTrailing = ( v ) =>
		{
			v = MathF.Round( v, 5 );
			string retVal = v.ToString( "F7" );
			while ( retVal.Contains( '.' ) && (retVal.Last() == '0' || retVal.Last() == '.') )
			{
				retVal = retVal.Substring( 0, retVal.Length - 1 );
			}
			return retVal;
		};
		return toStringNoTrailing( input.X ) + " " + toStringNoTrailing( input.Y ) + " " + toStringNoTrailing( input.Z );
	}

	public static Matrix4 Translate( this Matrix4 input, Vector3 translation )
	{
		return new Matrix4( input[0, 0], input[0, 1], input[0, 2], input[0, 3] + translation.X,
						  input[1, 0], input[1, 1], input[1, 2], input[1, 3] + translation.Y,
						  input[2, 0], input[2, 1], input[2, 2], input[2, 3] + translation.Z,
						  input[3, 0], input[3, 1], input[3, 2], input[3, 3] );
	}

	public static bool IsNearlyZero( this Vector3 v, float tolerance = 0.0001f )
	{
		return v.X * v.X + v.Y * v.Y + v.Z * v.Z <= tolerance * tolerance;
	}

	public static bool IsNaN( this Vector3 v )
	{
		return float.IsNaN( v.X ) || float.IsNaN( v.Y ) || float.IsNaN( v.Z );
	}

	public static bool IsNaN( this Vector2 v )
	{
		return float.IsNaN( v.X ) || float.IsNaN( v.Y );
	}

	public static bool IsNaN( this float f )
	{
		return float.IsNaN( f );
	}

	public static bool AlmostEqual( this Vector3 v, Vector3 other, float tolerance = 0.0001f )
	{
		return (v - other).LengthSquared <= tolerance * tolerance;
	}

	public static Vector3 Perpendicular( this Vector3 vector )
	{
		var result = Vector3.Cross( vector, Vector3.UnitZ );

		if ( result.LengthSquared < 1e-6f )
			result = Vector3.Cross( vector, -Vector3.UnitY );

		return result.Normalized();
	}

	public static Vector3 EulerToForward( this Vector3 euler )
	{
		float yaw = MathHelper.DegreesToRadians( euler.Z );
		float pitch = MathHelper.DegreesToRadians( euler.Y );

		// Calculate forward vector components
		float cp = (float)Math.Cos( pitch );
		float sp = (float)Math.Sin( pitch );
		float cy = (float)Math.Cos( yaw );
		float sy = (float)Math.Sin( yaw );

		// Create forward vector (X is forward, Y is left, Z is up)
		Vector3 forward = new Vector3( cp * cy, -cp * sy, -sp );

		return forward;
	}

	public static Vector3 ForwardToEuler( this Vector3 forward )
	{
		float pitch = (float)Math.Asin( -forward.Z );
		float yaw = (float)Math.Atan2( -forward.Y, forward.X );

		// Convert radians to degrees
		return new Vector3(
			0, // Roll is not determined from forward vector alone
			MathHelper.RadiansToDegrees( pitch ),
			MathHelper.RadiansToDegrees( yaw )
		);
	}

	public static Quaternion ForwardToRotation( this Vector3 forward )
	{
		forward = forward.Normalized();
		Vector3 up;

		// Choose a suitable up vector that's not parallel to forward
		if ( Math.Abs( Vector3.Dot( forward, Vector3.UnitZ ) ) < 0.9999f )
		{
			up = Vector3.UnitZ;
		}
		else
		{
			// If forward is too close to Z-axis, use Y-axis as temporary up
			up = Vector3.UnitY;
		}

		// Create rotation directly from forward and up vectors
		Vector3 left = Vector3.Cross( up, forward ).Normalized();
		up = Vector3.Cross( forward, left ).Normalized();

		// Create a rotation matrix
		// Note: In a left-handed system, we use forward, left, up instead of right, up, forward
		Matrix3 rotationMatrix = new Matrix3(
			forward.X, left.X, up.X,
			forward.Y, left.Y, up.Y,
			forward.Z, left.Z, up.Z
		);

		return Quaternion.FromMatrix( rotationMatrix );
	}

	public static int CountNonZeroComponents( this Vector3 v )
	{
		return (v.X != 0 ? 1 : 0) + (v.Y != 0 ? 1 : 0) + (v.Z != 0 ? 1 : 0);
	}

}
