
namespace Graybox.DataStructures.Transformations;

public class UnitRotate : IUnitTransformation
{
	public float Rotation { get; set; }
	public Line Axis { get; set; }

	public UnitRotate( float scalar, Line axis )
	{
		Rotation = scalar;
		Axis = axis;
	}

	/**
         * http://paulbourke.net/geometry/rotate/
         */
	public Vector3 Transform( Vector3 c )
	{
		var p = c - Axis.Start;
		var r = (Axis.End - Axis.Start).Normalized();

		var costheta = MathF.Cos( Rotation );
		var sintheta = MathF.Sin( Rotation );

		float x = 0, y = 0, z = 0;

		x += (costheta + (1 - costheta) * r.X * r.X) * p.X;
		x += ((1 - costheta) * r.X * r.Y - r.Z * sintheta) * p.Y;
		x += ((1 - costheta) * r.X * r.Z + r.Y * sintheta) * p.Z;

		y += ((1 - costheta) * r.X * r.Y + r.Z * sintheta) * p.X;
		y += (costheta + (1 - costheta) * r.Y * r.Y) * p.Y;
		y += ((1 - costheta) * r.Y * r.Z - r.X * sintheta) * p.Z;

		z += ((1 - costheta) * r.X * r.Z - r.Y * sintheta) * p.X;
		z += ((1 - costheta) * r.Y * r.Z + r.X * sintheta) * p.Y;
		z += (costheta + (1 - costheta) * r.Z * r.Z) * p.Z;

		return new Vector3( x, y, z ) + Axis.Start;
	}

}
