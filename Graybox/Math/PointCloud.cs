
using System.Collections.Generic;

namespace Graybox;

public class PointCloud
{

	public List<Vector3> Points { get; private set; }
	public Box BoundingBox { get; private set; }

	public Vector3 MinX { get; private set; }
	public Vector3 MinY { get; private set; }
	public Vector3 MinZ { get; private set; }
	public Vector3 MaxX { get; private set; }
	public Vector3 MaxY { get; private set; }
	public Vector3 MaxZ { get; private set; }

	public PointCloud( IEnumerable<Vector3> points )
	{
		Points = new List<Vector3>( points );
		BoundingBox = new Box( points );
		MinX = MinY = MinZ = MaxX = MaxY = MaxZ = default;
		foreach ( Vector3 p in points )
		{
			if ( MinX == default || p.X < MinX.X ) MinX = p;
			if ( MinY == default || p.Y < MinY.Y ) MinY = p;
			if ( MinZ == default || p.Z < MinZ.Z ) MinZ = p;
			if ( MaxX == default || p.X > MaxX.X ) MaxX = p;
			if ( MaxY == default || p.Y > MaxY.Y ) MaxY = p;
			if ( MaxZ == default || p.Z > MaxZ.Z ) MaxZ = p;
		}
	}

	public IEnumerable<Vector3> GetExtents()
	{
		return new[]
				   {
					   MinX, MinY, MinZ,
					   MaxX, MaxY, MaxZ
					};
	}

}
