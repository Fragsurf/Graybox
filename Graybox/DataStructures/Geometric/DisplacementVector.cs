
namespace Graybox.DataStructures.Geometric;

public class DisplacementVector 
{

	public Vector3 Position { get; set; }
	public Vector3 Normal { get; set; }
	public float Distance { get; set; }

	public DisplacementVector( Vector3 normal, float distance )
	{
		Normal = normal.Normalized();
		Distance = distance;
		Position = Normal * Distance;
	}

	public DisplacementVector( Vector3 offsets )
	{
		Set( offsets );
	}

	public void SetToZero()
	{
		Position = Vector3.Zero;
		Distance = 0;
	}

	public void Set( Vector3 offsets )
	{
		Distance = offsets.VectorMagnitude();
		if ( Distance == 0 )
		{
			Position = Vector3.Zero;
		}
		else
		{
			Position = offsets / Distance;
		}
	}

}
