
using Graybox.DataStructures.Transformations;
using Graybox.Lightmapper;

namespace Graybox.DataStructures.MapObjects;

public class Light( long id ) : MapObject( id )
{

	[HideInEditor]
	public Vector3 Position
	{
		get => LightInfo.Position;
		set
		{
			var le = LightInfo;
			le.Position = value;
			LightInfo = le;
		}
	}

	[HideInEditor]
	public Vector3 Direction
	{
		get => LightInfo.Direction;
		set
		{
			var le = LightInfo;
			le.Direction = value;
			LightInfo = le;
		}
	}

	[HideInEditor]
	public LightTypes LightType
	{
		get => LightInfo.Type;
		set
		{
			var le = LightInfo;
			le.Type = value;
			LightInfo = le;
		}
	}

	public LightInfo LightInfo { get; set; } = new();

	public override MapObject Clone()
	{
		var result = new Light( ID );
		result.LightInfo = LightInfo;
		result.UpdateBoundingBox();
		CopyBase( result, null, true );
		return result;
	}

	public override MapObject Copy( IDGenerator generator )
	{
		var e = new Light( generator.GetNextObjectID() )
		{
			LightInfo = LightInfo
		};
		CopyBase( e, generator );
		e.UpdateBoundingBox();
		return e;
	}

	public override void Paste( MapObject o, IDGenerator generator )
	{
		PasteBase( o, generator );
		var e = o as Light;
		if ( e == null ) return;
		LightInfo = e.LightInfo;
		UpdateBoundingBox();
	}

	public override void Unclone( MapObject o )
	{
		PasteBase( o, null, true );
		var e = o as Light;
		if ( e == null ) return;
		LightInfo = e.LightInfo;
		UpdateBoundingBox();
	}

	public override void UpdateBoundingBox( bool cascadeToParent = true )
	{
		base.UpdateBoundingBox( cascadeToParent );

		var min = new Vector3( -16, -16, -16 );
		var max = new Vector3( 16, 16, 16 );
		BoundingBox = new( Position + min, Position + max );
	}

	public override void Transform( IUnitTransformation transform, TransformFlags flags )
	{
		Position = transform.Transform( Position );

		if ( transform is UnitMatrixMult m )
		{
			Quaternion rot = m.Matrix.ExtractRotation();
			if ( rot != Quaternion.Identity )
			{
				var euler = Direction;

				Vector3 forward = euler.EulerToForward();
				Vector3 rotatedForward = rot * forward;

				// Convert rotated forward vector back to Euler angles
				Direction = rotatedForward.ForwardToEuler();
			}
		}

		base.Transform( transform, flags );
	}

}
