
namespace Graybox.Utility;

public class Vector3Comparer : IEqualityComparer<Vector3>
{
	private readonly float _epsilon;

	public Vector3Comparer( float epsilon )
	{
		_epsilon = epsilon;
	}

	public bool Equals( Vector3 x, Vector3 y )
	{
		return Vector3.DistanceSquared( x, y ) < _epsilon * _epsilon;
	}

	public int GetHashCode( Vector3 obj )
	{
		return HashCode.Combine(
			Math.Round( obj.X / _epsilon ),
			Math.Round( obj.Y / _epsilon ),
			Math.Round( obj.Z / _epsilon )
		);
	}
}
