
namespace Graybox.DataStructures.Transformations;

public class UnitScale : IUnitTransformation
{
	public Vector3 Scalar { get; set; }
	public Vector3 Origin { get; set; }

	public UnitScale( Vector3 scalar, Vector3 origin )
	{
		Scalar = scalar;
		Origin = origin;
	}

	public Vector3 Transform( Vector3 c )
	{
		return (c - Origin).ComponentMultiply( Scalar ) + Origin;
	}

}
