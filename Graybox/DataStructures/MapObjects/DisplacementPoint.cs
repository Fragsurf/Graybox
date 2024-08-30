
using Graybox.DataStructures.Geometric;

namespace Graybox.DataStructures.MapObjects;

public class DisplacementPoint
{
	public Displacement Parent { get; set; }

	public Vertex CurrentPosition { get; set; }
	public Vector3 InitialPosition { get; set; }

	public DisplacementVector Displacement { get; set; }
	public DisplacementVector OffsetDisplacement { get; set; }

	public int XIndex { get; set; }
	public int YIndex { get; set; }
	public float Alpha { get; set; }

	public Vector3 Location => CurrentPosition.Position;

	public DisplacementPoint( Displacement parent, int x, int y )
	{
		Parent = parent;
		XIndex = x;
		YIndex = y;
		CurrentPosition = new Vertex( OpenTK.Mathematics.Vector3.Zero, parent );
		InitialPosition = OpenTK.Mathematics.Vector3.Zero;
		Displacement = new DisplacementVector( OpenTK.Mathematics.Vector3.UnitZ, 0 );
		OffsetDisplacement = new DisplacementVector( OpenTK.Mathematics.Vector3.UnitZ, 0 );
		Alpha = 0;
	}

	public IEnumerable<DisplacementPoint> GetAdjacentPoints()
	{
		yield return Parent.GetPoint( XIndex + 1, YIndex + 0 );
		yield return Parent.GetPoint( XIndex - 1, YIndex + 0 );
		yield return Parent.GetPoint( XIndex + 0, YIndex + 1 );
		yield return Parent.GetPoint( XIndex + 0, YIndex - 1 );
		yield return Parent.GetPoint( XIndex - 1, YIndex - 1 );
		yield return Parent.GetPoint( XIndex - 1, YIndex + 1 );
		yield return Parent.GetPoint( XIndex + 1, YIndex - 1 );
		yield return Parent.GetPoint( XIndex + 1, YIndex + 1 );
	}
}
