
using Graybox.Scenes;
using System;

namespace Graybox;

public struct Frustum
{

	public Plane TopPlane { get; private set; }
	public Plane BottomPlane { get; private set; }
	public Plane LeftPlane { get; private set; }
	public Plane RightPlane { get; private set; }
	public Plane FarPlane { get; private set; }
	public Plane NearPlane { get; private set; }

	public Frustum( SceneCamera camera )
	{
		float radFov = camera.FieldOfView * MathF.PI / 180;
		float tanFovHalf = MathF.Tan( radFov / 2 );
		float halfVSide = camera.FarClip * tanFovHalf;
		float halfHSide = halfVSide * camera.AspectRatio;

		Vector3 nearCenter = camera.Position + camera.Forward * camera.NearClip;
		Vector3 farCenter = camera.Position + camera.Forward * camera.FarClip;

		// Define planes
		NearPlane = new Plane( camera.Forward, nearCenter );
		FarPlane = new Plane( -camera.Forward, farCenter );

		Vector3 rightOffset = camera.Right * halfHSide;
		Vector3 upOffset = camera.Up * halfVSide;

		RightPlane = new Plane( Vector3.Normalize( Vector3.Cross( camera.Forward * camera.FarClip - rightOffset, camera.Up ) ), camera.Position );
		LeftPlane = new Plane( Vector3.Normalize( Vector3.Cross( camera.Up, camera.Forward * camera.FarClip + rightOffset ) ), camera.Position );

		TopPlane = new Plane( Vector3.Normalize( Vector3.Cross( camera.Right, camera.Forward * camera.FarClip - upOffset ) ), camera.Position );
		BottomPlane = new Plane( Vector3.Normalize( Vector3.Cross( camera.Forward * camera.FarClip + upOffset, camera.Right ) ), camera.Position );
	}

	public bool Contains( Bounds bounds )
	{
		return Contains( this, bounds );
	}

	private static bool Contains( Frustum frustum, Bounds bounds )
	{
		Vector3 min = bounds.Mins;
		Vector3 max = bounds.Maxs;

		if ( AllCornersBehindPlane( frustum.NearPlane, min, max ) )
			return false;
		if ( AllCornersBehindPlane( frustum.FarPlane, min, max ) )
			return false;
		if ( AllCornersBehindPlane( frustum.RightPlane, min, max ) )
			return false;
		if ( AllCornersBehindPlane( frustum.LeftPlane, min, max ) )
			return false;
		if ( AllCornersBehindPlane( frustum.TopPlane, min, max ) )
			return false;
		if ( AllCornersBehindPlane( frustum.BottomPlane, min, max ) )
			return false;

		return true;
	}

	private static bool AllCornersBehindPlane( Plane plane, Vector3 min, Vector3 max )
	{
		// Precompute the plane's normal components to avoid recalculating in the loop
		Vector3 normal = plane.Normal;
		float planeD = plane.D;

		// Check each corner directly
		if ( Vector3.Dot( normal, new Vector3( min.X, min.Y, min.Z ) ) + planeD >= 0 ) return false;
		if ( Vector3.Dot( normal, new Vector3( max.X, min.Y, min.Z ) ) + planeD >= 0 ) return false;
		if ( Vector3.Dot( normal, new Vector3( min.X, max.Y, min.Z ) ) + planeD >= 0 ) return false;
		if ( Vector3.Dot( normal, new Vector3( max.X, max.Y, min.Z ) ) + planeD >= 0 ) return false;
		if ( Vector3.Dot( normal, new Vector3( min.X, min.Y, max.Z ) ) + planeD >= 0 ) return false;
		if ( Vector3.Dot( normal, new Vector3( max.X, min.Y, max.Z ) ) + planeD >= 0 ) return false;
		if ( Vector3.Dot( normal, new Vector3( min.X, max.Y, max.Z ) ) + planeD >= 0 ) return false;
		if ( Vector3.Dot( normal, new Vector3( max.X, max.Y, max.Z ) ) + planeD >= 0 ) return false;

		// All corners are behind the plane
		return true;
	}


}
