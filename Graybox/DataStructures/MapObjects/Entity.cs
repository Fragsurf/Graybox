
using Graybox.DataStructures.GameData;
using Graybox.DataStructures.Transformations;
using Graybox.Utility;

namespace Graybox.DataStructures.MapObjects;

public class Entity( long id ) : MapObject( id )
{

	public override string ClassName 
	{ 
		get => EntityData.Name; 
		set => EntityData.Name = value; 
	}

	public GameDataObject GameData { get; set; }
	public EntityData EntityData { get; set; } = new EntityData();

	public Vector3 Origin
	{
		get => ParseUtility.StringToVector3( EntityData.GetPropertyValue( "position" ), Vector3.Zero );
		set => EntityData.SetPropertyValue( "position", ParseUtility.Vector3ToString( value ) );
	}

	public Vector3 Angles
	{
		get => ParseUtility.StringToVector3( EntityData.GetPropertyValue( "angles" ), Vector3.Zero );
		set => EntityData.SetPropertyValue( "angles", ParseUtility.Vector3ToString( value ) );
	}

	public override MapObject Copy( IDGenerator generator )
	{
		Entity e = new Entity( generator.GetNextObjectID() )
		{
			GameData = GameData,
			EntityData = EntityData.Clone()
		};
		CopyBase( e, generator );
		return e;
	}

	public override void Paste( MapObject o, IDGenerator generator )
	{
		PasteBase( o, generator );
		Entity e = o as Entity;
		if ( e == null ) return;
		GameData = e.GameData;
		EntityData = e.EntityData.Clone();
	}

	public override MapObject Clone()
	{
		Entity e = new Entity( ID ) { GameData = GameData, EntityData = EntityData.Clone() };
		CopyBase( e, null, true );
		return e;
	}

	public override void Unclone( MapObject o )
	{
		PasteBase( o, null, true );
		Entity e = o as Entity;
		if ( e == null ) return;
		GameData = e.GameData;
		EntityData = e.EntityData.Clone();
	}

	public override void UpdateBoundingBox( bool cascadeToParent = true )
	{
		if ( GameData == null && !Children.Any() )
		{
			var sub = new Vector3( -16, -16, -16 );
			var add = new Vector3( 16, 16, 16 );
			BoundingBox = new Box( Origin + sub, Origin + add );
		}
		else if ( MetaData.Has<Box>( "BoundingBox" ) )
		{
			var scale = EntityData.GetPropertyCoordinate( "scale", Vector3.One );
			scale = new Vector3( scale.X, scale.Z, scale.Y );
			var angles = EntityData.GetPropertyCoordinate( "angles", Vector3.Zero );
			var pitch = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( MathHelper.DegreesToRadians( angles.X ), 0, 0 ) );
			var yaw = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( 0, 0, -MathHelper.DegreesToRadians( angles.Y ) ) );
			var roll = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( 0, MathHelper.DegreesToRadians( angles.Z ), 0 ) );
			var tform = ((yaw * pitch * roll) * Matrix4.CreateScale( scale )).Translate( Origin );
			if ( MetaData.Has<bool>( "RotateBoundingBox" ) && !MetaData.Get<bool>( "RotateBoundingBox" ) ) tform = Matrix4.CreateTranslation( Origin );
			BoundingBox = MetaData.Get<Box>( "BoundingBox" ).Transform( new UnitMatrixMult( tform ) );
		}
		else if ( GameData != null && GameData.ClassType == ClassType.Point )
		{
			var sub = new Vector3( -16, -16, -16 );
			var add = new Vector3( 16, 16, 16 );
			Behaviour behav = GameData.Behaviours.SingleOrDefault( x => x.Name == "size" );
			if ( behav != null && behav.Values.Count >= 6 )
			{
				sub = behav.GetCoordinate( 0 );
				add = behav.GetCoordinate( 1 );
			}
			else if ( GameData.Name == "infodecal" )
			{
				sub = Vector3.One * -4;
				add = Vector3.One * 4;
			}
			BoundingBox = new Box( Origin + sub, Origin + add );
		}
		else if ( Children.Any() )
		{
			BoundingBox = new Box( GetChildren().SelectMany( x => new[] { x.BoundingBox.Start, x.BoundingBox.End } ) );
		}
		else
		{
			BoundingBox = new Box( Origin, Origin );
		}
		base.UpdateBoundingBox( cascadeToParent );
	}

	public new Color4 Colour
	{
		get
		{
			if ( GameData != null && GameData.ClassType == ClassType.Point )
			{
				Behaviour behav = GameData.Behaviours.LastOrDefault( x => x.Name == "color" );
				if ( behav != null && behav.Values.Count == 3 )
				{
					return behav.GetColour( 0 );
				}
			}
			return base.Colour;
		}
		set { base.Colour = value; }
	}

	public override void Transform( IUnitTransformation transform, TransformFlags flags )
	{
		Origin = transform.Transform( Origin );

		base.Transform( transform, flags );
	}

	/// <summary>
	/// Returns the intersection point closest to the start of the line.
	/// </summary>
	/// <param name="line">The intersection line</param>
	/// <returns>The closest intersecting point, or null if the line doesn't intersect.</returns>
	public override Vector3 GetIntersectionPoint( Line line )
	{
		var faces = GetBoxFaces().Union( MetaData.GetAll<List<Face>>().SelectMany( x => x ) );
		return faces.Select( x => x.GetIntersectionPoint( line ) )
			.Where( x => x != default )
			.OrderBy( x => (x - line.Start).VectorMagnitude() )
			.FirstOrDefault();
	}

	public override Box GetIntersectionBoundingBox()
	{
		return new Box( new[] { BoundingBox }.Union( MetaData.GetAll<Box>() ) );
	}

	public override EntityData GetEntityData()
	{
		return EntityData;
	}

}
