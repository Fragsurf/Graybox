
using System;
using System.Collections.Generic;

namespace Graybox;

public struct Bounds
{

	public Vector3 Mins { get; set; }
	public Vector3 Maxs { get; set; }

	public Bounds( Vector3 mins, Vector3 maxs )
	{
		Mins = mins;
		Maxs = maxs;
	}

	public Bounds( Vector3 center, float size )
	{
		var halfSize = (Vector3.One * size) / 2;
		Mins = center - halfSize;
		Maxs = center + halfSize;
	}

	public Vector3 Size => Maxs - Mins;
	public Vector3 Center => (Mins + Maxs) / 2;

	public IEnumerable<Vector3> GetCorners()
	{
		yield return new Vector3( Mins.X, Mins.Y, Mins.Z );
		yield return new Vector3( Maxs.X, Mins.Y, Mins.Z );
		yield return new Vector3( Maxs.X, Maxs.Y, Mins.Z );
		yield return new Vector3( Mins.X, Maxs.Y, Mins.Z );
		yield return new Vector3( Mins.X, Maxs.Y, Maxs.Z );
		yield return new Vector3( Mins.X, Mins.Y, Maxs.Z );
		yield return new Vector3( Maxs.X, Mins.Y, Maxs.Z );
		yield return new Vector3( Maxs.X, Maxs.Y, Maxs.Z );
	}

	public IEnumerable<Vector3> GetFaceCenters()
	{
		yield return new Vector3( Center.X, Center.Y, Mins.Z );
		yield return new Vector3( Center.X, Center.Y, Maxs.Z );
		yield return new Vector3( Center.X, Mins.Y, Center.Z );
		yield return new Vector3( Center.X, Maxs.Y, Center.Z );
		yield return new Vector3( Mins.X, Center.Y, Center.Z );
		yield return new Vector3( Maxs.X, Center.Y, Center.Z );
	}

	public IEnumerable<(Vector3 Center, Vector3 Normal)> GetFaceCentersAndNormals()
	{
		// Bottom face
		yield return (new Vector3( Center.X, Center.Y, Mins.Z ), new Vector3( 0, 0, -1 ));

		// Top face
		yield return (new Vector3( Center.X, Center.Y, Maxs.Z ), new Vector3( 0, 0, 1 ));

		// Front face
		yield return (new Vector3( Center.X, Mins.Y, Center.Z ), new Vector3( 0, -1, 0 ));

		// Back face
		yield return (new Vector3( Center.X, Maxs.Y, Center.Z ), new Vector3( 0, 1, 0 ));

		// Left face
		yield return (new Vector3( Mins.X, Center.Y, Center.Z ), new Vector3( -1, 0, 0 ));

		// Right face
		yield return (new Vector3( Maxs.X, Center.Y, Center.Z ), new Vector3( 1, 0, 0 ));
	}

	public IEnumerable<(Vector3 Center, Vector3 Normal, Vector2 Size)> GetFaceCentersNormalsAndSizes()
	{
		// Bottom face
		yield return (new( Center.X, Center.Y, Mins.Z ), new( 0, 0, -1 ), new( Maxs.X - Mins.X, Maxs.Y - Mins.Y ));

		// Top face
		yield return (new( Center.X, Center.Y, Maxs.Z ), new( 0, 0, 1 ), new( Maxs.X - Mins.X, Maxs.Y - Mins.Y ));

		// Front face
		yield return (new( Center.X, Mins.Y, Center.Z ), new( 0, -1, 0 ), new( Maxs.X - Mins.X, Maxs.Z - Mins.Z ));

		// Back face
		yield return (new( Center.X, Maxs.Y, Center.Z ), new( 0, 1, 0 ), new( Maxs.X - Mins.X, Maxs.Z - Mins.Z ));

		// Left face
		yield return (new( Mins.X, Center.Y, Center.Z ), new( -1, 0, 0 ), new( Maxs.Y - Mins.Y, Maxs.Z - Mins.Z ));

		// Right face
		yield return (new( Maxs.X, Center.Y, Center.Z ), new( 1, 0, 0 ), new( Maxs.Y - Mins.Y, Maxs.Z - Mins.Z ));
	}

	public void ExpandFaceCenter( int faceIndex, Vector3 newCenter )
	{
		var newMins = Mins;
		var newMaxs = Maxs;

		switch ( faceIndex )
		{
			case 0: // Bottom face
				newMins.Z = newCenter.Z;
				break;

			case 1: // Top face
				newMaxs.Z = newCenter.Z;
				break;

			case 2: // Front face
				newMins.Y = newCenter.Y;
				break;

			case 3: // Back face
				newMaxs.Y = newCenter.Y;
				break;

			case 4: // Left face
				newMins.X = newCenter.X;
				break;

			case 5: // Right face
				newMaxs.X = newCenter.X;
				break;

			default:
				throw new ArgumentOutOfRangeException( nameof( faceIndex ), "Invalid face index" );
		}

		Mins = newMins;
		Maxs = newMaxs;
	}

	public Bounds Encapsulate( Vector3 point )
	{
		Vector3 newMins = new Vector3(
			Math.Min( Mins.X, point.X ),
			Math.Min( Mins.Y, point.Y ),
			Math.Min( Mins.Z, point.Z )
		);

		Vector3 newMaxs = new Vector3(
			Math.Max( Maxs.X, point.X ),
			Math.Max( Maxs.Y, point.Y ),
			Math.Max( Maxs.Z, point.Z )
		);

		return new Bounds( newMins, newMaxs );
	}

	public Bounds Encapsulate( Bounds bounds )
	{
		Vector3 newMins = new Vector3(
			Math.Min( Mins.X, bounds.Mins.X ),
			Math.Min( Mins.Y, bounds.Mins.Y ),
			Math.Min( Mins.Z, bounds.Mins.Z )
		);

		Vector3 newMaxs = new Vector3(
			Math.Max( Maxs.X, bounds.Maxs.X ),
			Math.Max( Maxs.Y, bounds.Maxs.Y ),
			Math.Max( Maxs.Z, bounds.Maxs.Z )
		);

		return new Bounds( newMins, newMaxs );
	}

	public void SetFaceCenter( int faceIndex, Vector3 newCenter )
	{
		Vector3 size = Size;
		Vector3 halfSize = size / 2;
		var newMins = Mins;
		var newMaxs = Maxs;

		switch ( faceIndex )
		{
			case 0: // Bottom face
				newMins.Z = newCenter.Z - halfSize.Z;
				newMaxs.Z = newCenter.Z + halfSize.Z;
				break;

			case 1: // Top face
				newMaxs.Z = newCenter.Z + halfSize.Z;
				newMins.Z = newCenter.Z - halfSize.Z;
				break;

			case 2: // Front face
				newMins.Y = newCenter.Y - halfSize.Y;
				newMaxs.Y = newCenter.Y + halfSize.Y;
				break;

			case 3: // Back face
				newMaxs.Y = newCenter.Y + halfSize.Y;
				newMins.Y = newCenter.Y - halfSize.Y;
				break;

			case 4: // Left face
				newMins.X = newCenter.X - halfSize.X;
				newMaxs.X = newCenter.X + halfSize.X;
				break;

			case 5: // Right face
				newMaxs.X = newCenter.X + halfSize.X;
				newMins.X = newCenter.X - halfSize.X;
				break;
		}

		Mins = newMins;
		Maxs = newMaxs;
	}

	public bool LineIntersects( Vector3 a, Vector3 b )
	{
		Vector3 dir = b - a;
		float tmin = 0.0f, tmax = 1.0f;

		for ( int i = 0; i < 3; i++ )
		{
			if ( Math.Abs( dir[i] ) < float.Epsilon )
			{
				// Line is parallel to this axis.
				if ( a[i] < Mins[i] || a[i] > Maxs[i] )
					return false;
			}
			else
			{
				// Compute intersection of the line segment with the two planes of this axis.
				float ood = 1.0f / dir[i];
				float t1 = (Mins[i] - a[i]) * ood;
				float t2 = (Maxs[i] - a[i]) * ood;
				if ( t1 > t2 ) (t1, t2) = (t2, t1);  // Ensure t1 <= t2

				if ( t1 > tmin ) tmin = t1;
				if ( t2 < tmax ) tmax = t2;
				if ( tmin > tmax ) return false;
			}
		}

		return true;
	}

}
