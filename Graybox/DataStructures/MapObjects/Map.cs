
using Graybox.DataStructures.GameData;
using Graybox.Lightmapper;

namespace Graybox.DataStructures.MapObjects;

public class Map
{

	public decimal Version { get; set; }
	public List<Camera> Cameras { get; private set; }
	public Camera ActiveCamera { get; set; }
	public World WorldSpawn { get; set; }
	public IDGenerator IDGenerator { get; private set; }
	public LightmapData LightmapData { get; set; } = new();
	public EnvironmentData EnvironmentData { get; set; } = new();

	public bool Show2DGrid { get; set; }
	public bool Show3DGrid { get; set; }
	public bool SnapToGrid { get; set; }
	public float GridSpacing { get; set; }
	public bool HideFaceMask { get; set; }
	public bool HideDisplacementSolids { get; set; }
	public bool HideToolTextures { get; set; }
	public bool HideEntitySprites { get; set; }
	public bool HideMapOrigin { get; set; }
	public bool IgnoreGrouping { get; set; }
	public bool TextureLock { get; set; }
	public bool TextureScalingLock { get; set; }
	public bool Cordon { get; set; }
	public Box CordonBounds { get; set; }

	public Map()
	{
		Version = 1;
		Cameras = new List<Camera>();
		ActiveCamera = null;
		IDGenerator = new IDGenerator();
		WorldSpawn = new World( IDGenerator.GetNextObjectID() );

		Show2DGrid = SnapToGrid = true;
		TextureLock = true;
		HideDisplacementSolids = true;
		CordonBounds = new Box( Vector3.One * -1024, Vector3.One * 1024 );
	}

	public TransformFlags GetTransformFlags()
	{
		TransformFlags flags = TransformFlags.None;
		if ( TextureLock ) flags |= TransformFlags.TextureLock;
		if ( TextureScalingLock ) flags |= TransformFlags.TextureScalingLock;
		return flags;
	}

	public IEnumerable<string> GetAllTextures()
	{
		return GetAllTexturesRecursive( WorldSpawn ).Distinct();
	}

	private static IEnumerable<string> GetAllTexturesRecursive( MapObject obj )
	{
		if ( obj is Entity && obj.ChildCount == 0 )
		{
			Entity ent = (Entity)obj;
			if ( ent.EntityData.Name == "infodecal" )
			{
				Property tex = ent.EntityData.Properties.FirstOrDefault( x => x.Key == "texture" );
				if ( tex != null ) return new[] { tex.Value };
			}
		}
		else if ( obj is Solid )
		{
			return ((Solid)obj).Faces.Select( f => f.TextureRef.AssetPath );
		}

		return obj.GetChildren().SelectMany( GetAllTexturesRecursive );
	}

	public void PostLoadProcess( GameData.GameData gameData )
	{
		foreach ( var ent in WorldSpawn.GetAllDescendants<Entity>() )
		{
			if ( ent.GameData == null || !string.Equals( ent.GameData.Name, ent.EntityData.Name, StringComparison.OrdinalIgnoreCase ) )
			{
				var gd = gameData.Classes.FirstOrDefault( x => String.Equals( x.Name, ent.EntityData.Name, StringComparison.CurrentCultureIgnoreCase ) && x.ClassType != ClassType.Base );
				ent.GameData = gd;
				ent.UpdateBoundingBox();
			}
		}
	}

}
