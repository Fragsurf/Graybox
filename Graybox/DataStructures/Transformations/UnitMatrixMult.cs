
namespace Graybox.DataStructures.Transformations;

public class UnitMatrixMult : IUnitTransformation
{

	public Matrix4 Matrix { get; set; }

	public UnitMatrixMult( Matrix4 matrix )
	{
		Matrix = matrix;
	}

	public Vector3 Transform( Vector3 c )
	{
		return Transform( c, 1 );
	}

	public Vector3 Transform( Vector3 c, float w )
	{
		var v = new Vector4( c.X, c.Y, c.Z, w );
		var r = Vector4.TransformRow( v, Matrix );
		return new( r.X, r.Y, r.Z );
	}

}
