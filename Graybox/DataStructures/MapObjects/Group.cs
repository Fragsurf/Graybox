
namespace Graybox.DataStructures.MapObjects;

public class Group : MapObject
{
	public Group( long id ) : base( id )
	{
	}

	public override MapObject Copy( IDGenerator generator )
	{
		Group group = new Group( generator.GetNextObjectID() );
		CopyBase( group, generator );
		return group;
	}

	public override void Paste( MapObject o, IDGenerator generator )
	{
		PasteBase( o, generator );
	}

	public override MapObject Clone()
	{
		Group group = new Group( ID );
		CopyBase( group, null, true );
		return group;
	}

	public override void Unclone( MapObject o )
	{
		PasteBase( o, null, true );
	}

	public override void UpdateBoundingBox( bool cascadeToParent = true )
	{
		BoundingBox = GetChildren().All( x => x.BoundingBox == null )
						  ? null
						  : new Box( GetChildren().Where( x => x.BoundingBox != null ).Select( x => x.BoundingBox ) );
		base.UpdateBoundingBox( cascadeToParent );
	}
}
