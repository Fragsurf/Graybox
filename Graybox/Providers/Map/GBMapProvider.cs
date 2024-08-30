
using Graybox.Format;
using Graybox.DataStructures.MapObjects;

namespace Graybox.Providers.Map;

public class GBMapProvider : MapProvider
{

	protected override DataStructures.MapObjects.Map GetFromFile( string filename )
	{
		var gbmap = GBMap.FromFile( filename );
		var idValidator = new IDValidator();
		idValidator.ValidateAndCorrectIds( gbmap.World );

		var result = ConvertToDataStructuresMap( gbmap );
		var (largestObjectId, largestFaceId) = FindLargestIds( gbmap.World );
		result.IDGenerator.Reset( largestObjectId, largestFaceId );

		return result;
	}

	protected override void SaveToFile( string filename, DataStructures.MapObjects.Map map, AssetSystem assetSystem )
	{
		var gbmap = ToGrayboxFormat( map );
		gbmap.Assets = CollectAssets( map, assetSystem );

		GBMap.ToFile( gbmap, filename );
	}

	List<GBAsset> CollectAssets( DataStructures.MapObjects.Map map, AssetSystem assetSystem )
	{
		var result = new List<GBAsset>();

		var textures = map.GetAllTextures();
		foreach ( var tex in textures )
		{
			var asset = assetSystem.FindAsset( tex );
			if ( asset == null ) continue;

			var gbAsset = ToGBAsset( asset );
			if ( gbAsset != null )
			{
				result.Add( gbAsset );
			}
		}

		var entities = map.WorldSpawn.GetAllDescendants<Entity>();
		foreach ( var ent in entities )
		{
			foreach ( var prop in ent.GameData.Properties )
			{
				if ( prop.VariableType == DataStructures.GameData.VariableType.Asset )
				{
					var assetPath = ent.EntityData.GetPropertyValue( prop.Name );
					if ( string.IsNullOrEmpty( assetPath ) ) continue;
					var asset = assetSystem.FindAsset( assetPath );
					if ( asset == null ) continue;

					var gbAsset = ToGBAsset( asset );
					if ( gbAsset != null )
					{
						result.Add( gbAsset );
					}
				}
			}
		}

		if ( !string.IsNullOrEmpty( map.EnvironmentData?.Skybox ) )
		{
			var skyAsset = assetSystem.FindAsset( map.EnvironmentData.Skybox );
			if ( skyAsset != null )
			{
				var gbAsset = ToGBAsset( skyAsset );
				if ( gbAsset != null )
				{
					result.Add( gbAsset );
				}
			}
		}

		return result;
	}

	private GBAsset ToGBAsset( Asset asset )
	{
		if ( asset is TextureAsset texture )
		{
			if ( !File.Exists( asset.AbsolutePath ) )
				return null;

			var gbAsset = new GBTextureAsset
			{
				RelativePath = texture.RelativePath,
				Width = texture.Width,
				Height = texture.Height,
				Data = File.ReadAllBytes( texture.AbsolutePath ),
				Transparent = texture.Flags.HasFlag( Graphics.TextureFlags.Transparent )
			};

			return gbAsset;
		}

		if ( asset is MaterialAsset material )
		{
			var gbAsset = new GBMaterialAsset()
			{
				Properties = material.Properties.ToDictionary()
			};

			return gbAsset;
		}

		return null;
	}

	protected override bool IsValidForFileName( string filename )
	{
		return filename.EndsWith( ".graybox", StringComparison.OrdinalIgnoreCase )
			|| filename.EndsWith( ".graybox_2", StringComparison.OrdinalIgnoreCase );
	}

	private GBMap ToGrayboxFormat( DataStructures.MapObjects.Map map )
	{
		var result = new GBMap
		{
			LightmapData = new GBLightmapData
			{
				Lightmaps = map.LightmapData.Lightmaps.Select( lm => new GBLightmap
				{
					Width = lm.Width,
					Height = lm.Height,
					Data = lm.ImageData,
					DirectionalData = lm.DirectionalData,
					ShadowMaskData = lm.ShadowMaskData,
					DirectionalSize = ConvertToGBVec2( lm.DirectionalSize ),
					ShadowMaskSize = ConvertToGBVec2( lm.ShadowMaskSize ),
				} ).ToList(),
			},
			EnvironmentData = new GBEnvironmentData()
			{
				AmbientColor = ConvertToGBVec4( map.EnvironmentData.AmbientColor ),
				FogColor = ConvertToGBVec4( map.EnvironmentData.FogColor ),
				FogDensity = map.EnvironmentData.FogDensity,
				FogEnabled = map.EnvironmentData.FogEnabled,
				Skybox = map.EnvironmentData.Skybox,
				ClearColor = ConvertToGBVec4( map.EnvironmentData.SkyColor )
			},
			World = ConvertToGBObject( map.WorldSpawn )
		};


		return result;
	}

	public static GBObject ConvertToGBObject( MapObject obj )
	{
		GBObject result = null;

		if ( obj is Solid solid )
		{
			result = new GBSolid
			{
				Type = GBObjectTypes.Solid,
				Faces = solid.Faces.Select( ConvertToGBFace ).ToList(),
			};
		}
		else if ( obj is World world )
		{
			result = new GBWorld()
			{
				Type = GBObjectTypes.World
			};
		}
		else if ( obj is Group group )
		{
			result = new GBGroup()
			{
				Type = GBObjectTypes.Group
			};
		}
		else if ( obj is Entity ent )
		{
			var gbEnt = new GBEntity()
			{
				ClassName = ent.ClassName,
				Type = GBObjectTypes.Entity
			};
			foreach ( var prop in ent.EntityData.Properties )
			{
				gbEnt.Properties.Add( prop.Key, prop.Value );
			}
			result = gbEnt;
		}
		else if ( obj is Light light )
		{
			var gbLight = new GBLight()
			{
				LightInfo = new()
				{
					Color = ConvertToGBVec4( light.LightInfo.Color ),
					Direction = ConvertToGBVec3( light.LightInfo.Direction ),
					Intensity = light.LightInfo.Intensity,
					Position = ConvertToGBVec3( light.LightInfo.Position ),
					Range = light.LightInfo.Range,
					Type = (GBLightTypes)(int)light.LightInfo.Type
				},
			};
			result = gbLight;
		}

		if ( result != null )
		{
			result.ID = obj.ID;
			result.Name = obj.Name;
			result.Children = obj.Children.Select( ConvertToGBObject ).ToList();
		}

		return result;
	}

	private static GBFace ConvertToGBFace( Face face )
	{
		return new GBFace
		{
			ID = face.ID,
			Normal = ConvertToGBVec3( face.Plane.Normal ),
			Texture = ConvertToGBTextureReference( face.TextureRef ),
			Vertices = face.Vertices.Select( ConvertToGBVertex ).ToList(),
			DisableInLightmap = face.DisableInLightmap,
			TexelSize = face.TexelSize
		};
	}

	private static GBTextureReference ConvertToGBTextureReference( TextureReference texture )
	{
		return new GBTextureReference
		{
			AssetPath = texture.AssetPath,
			UAxis = ConvertToGBVec3( texture.UAxis ),
			VAxis = ConvertToGBVec3( texture.VAxis ),
			ShiftScale = new( texture.XShift, texture.YShift, texture.XScale, texture.YScale ),
			Rotation = texture.Rotation
		};
	}

	private static GBVertex ConvertToGBVertex( Vertex vertex )
	{
		return new GBVertex
		{
			Position = ConvertToGBVec3( vertex.Position ),
			UV0 = new( vertex.TextureU, vertex.TextureV ),
			UV1 = new( vertex.LightmapU, vertex.LightmapV ),
		};
	}

	private static GBVec4 ConvertToGBVec4( Color4 color ) => new GBVec4 { X = color.R, Y = color.G, Z = color.B, W = color.A };
	private static GBVec4 ConvertToGBVec4( Vector4 vector ) => new GBVec4 { X = vector.X, Y = vector.Y, Z = vector.Z, W = vector.W };
	private static GBVec3 ConvertToGBVec3( Vector3 vector ) => new GBVec3 { X = vector.X, Y = vector.Y, Z = vector.Z };
	private static GBVec2 ConvertToGBVec2( Vector2 vector ) => new GBVec2 { X = vector.X, Y = vector.Y };

	private DataStructures.MapObjects.Map ConvertToDataStructuresMap( GBMap gbmap )
	{
		var result = new DataStructures.MapObjects.Map();
		result.LightmapData = new();

		List<Lightmapper.Lightmap> lightmaps = new();
		foreach ( var lm in gbmap.LightmapData.Lightmaps )
		{
			lightmaps.Add( new Lightmapper.Lightmap
			{
				Width = lm.Width,
				Height = lm.Height,
				ImageData = lm.Data,
				DirectionalData = lm.DirectionalData,
				ShadowMaskData = lm.ShadowMaskData,
				DirectionalSize = new( lm.DirectionalSize.X, lm.DirectionalSize.Y ),
				ShadowMaskSize = new( lm.ShadowMaskSize.X, lm.ShadowMaskSize.Y )
			} );
		}
		result.LightmapData.Set( lightmaps );
		result.WorldSpawn = ConvertToMapObject( gbmap.World ) as World;

		result.EnvironmentData = new()
		{
			AmbientColor = ConvertToColor4( gbmap.EnvironmentData.AmbientColor ),
			FogEnabled = gbmap.EnvironmentData.FogEnabled,
			FogColor = ConvertToColor4( gbmap.EnvironmentData.FogColor ),
			FogDensity = gbmap.EnvironmentData.FogDensity,
			Skybox = gbmap.EnvironmentData.Skybox,
			SkyColor = ConvertToColor4( gbmap.EnvironmentData.ClearColor )
		};

		return result;
	}

	public static MapObject ConvertToMapObject( GBObject obj )
	{
		MapObject result = null;

		if ( obj is GBSolid solid )
		{
			var mapSolid = new Solid( obj.ID );
			mapSolid.Faces.AddRange( solid.Faces.Select( ConvertToFace ) );
			foreach ( var face in mapSolid.Faces )
			{
				face.Parent = mapSolid;
			}
			mapSolid.Refresh();
			result = mapSolid;
		}
		else if ( obj is GBWorld world )
		{
			result = new World( obj.ID );
		}
		else if ( obj is GBGroup group )
		{
			result = new Group( obj.ID );
		}
		else if ( obj is GBEntity ent )
		{
			var mapEnt = new Entity( obj.ID );
			mapEnt.ClassName = ent.ClassName;

			foreach ( var prop in ent.Properties )
			{
				mapEnt.EntityData.SetPropertyValue( prop.Key, prop.Value );
			}

			result = mapEnt;
		}
		else if ( obj is GBLight light )
		{
			var lightObj = new Light( obj.ID );
			lightObj.LightInfo = new()
			{
				Color = ConvertToColor4( light.LightInfo.Color ),
				Direction = ConvertToVector3( light.LightInfo.Direction ),
				Intensity = light.LightInfo.Intensity,
				Position = ConvertToVector3( light.LightInfo.Position ),
				Range = light.LightInfo.Range,
				Type = (Lightmapper.LightTypes)(int)light.LightInfo.Type
			};
			result = lightObj;
		}

		if ( result != null )
		{
			foreach ( var child in obj.Children )
			{
				var childResult = ConvertToMapObject( child );
				childResult?.SetParent( result );
			}

			result.Name = obj.Name;
			result.ID = obj.ID;
			result.Colour = ColorUtility.GetRandomBrushColour();
		}

		return result;
	}

	private static Face ConvertToFace( GBFace face )
	{
		var result = new Face( face.ID );
		result.TextureRef = ConvertToTextureReference( face.Texture );
		result.Vertices = face.Vertices.Select( x => ConvertToVertex( x, result ) ).ToList();
		result.TexelSize = face.TexelSize;
		result.DisableInLightmap = face.DisableInLightmap;
		result.UpdateBoundingBox();

		var normal = ConvertToVector3( face.Normal );
		var point = result.Vertices.FirstOrDefault()?.Position ?? default;
		var d = -Vector3.Dot( normal, point );

		result.Plane = new Plane( normal, d );

		return result;
	}

	private static TextureReference ConvertToTextureReference( GBTextureReference texture )
	{
		return new TextureReference
		{
			AssetPath = texture.AssetPath,
			UAxis = ConvertToVector3( texture.UAxis ),
			VAxis = ConvertToVector3( texture.VAxis ),
			XShift = texture.ShiftScale.X,
			YShift = texture.ShiftScale.Y,
			XScale = texture.ShiftScale.Z,
			YScale = texture.ShiftScale.W,
			Rotation = texture.Rotation
		};
	}

	private static Vertex ConvertToVertex( GBVertex vertex, Face face )
	{
		return new Vertex( ConvertToVector3( vertex.Position ), face )
		{
			TextureU = vertex.UV0.X,
			TextureV = vertex.UV0.Y,
			LightmapU = vertex.UV1.X,
			LightmapV = vertex.UV1.Y
		};
	}

	private static Vector2 ConvertToVector2( GBVec2 vector ) => new Vector2( vector.X, vector.Y );
	private static Vector3 ConvertToVector3( GBVec3 vector ) => new Vector3( vector.X, vector.Y, vector.Z );
	private static Vector4 ConvertToVector4( GBVec4 vector ) => new Vector4( vector.X, vector.Y, vector.Z, vector.W );
	private static Color4 ConvertToColor4( GBVec4 vector ) => new Color4( vector.X, vector.Y, vector.Z, vector.W );

	private static (long largestObjectId, long largestFaceId) FindLargestIds( GBObject root )
	{
		long largestObjectId = root.ID;
		long largestFaceId = 0;

		// Check if the current object is a GBSolid and update largestFaceId
		if ( root is GBSolid solid )
		{
			largestFaceId = solid.Faces.Max( face => face.ID );
		}

		// Recursively check children
		foreach ( var child in root.Children )
		{
			var (childObjectId, childFaceId) = FindLargestIds( child );
			largestObjectId = Math.Max( largestObjectId, childObjectId );
			largestFaceId = Math.Max( largestFaceId, childFaceId );
		}

		return (largestObjectId, largestFaceId);
	}

	class IDValidator
	{
		private HashSet<long> usedObjectIds = new HashSet<long>();
		private HashSet<long> usedFaceIds = new HashSet<long>();
		private long nextObjectId = 1;
		private long nextFaceId = 1;

		public void ValidateAndCorrectIds( GBObject root )
		{
			ValidateAndCorrectObjectId( root );

			if ( root is GBSolid solid )
			{
				foreach ( var face in solid.Faces )
				{
					ValidateAndCorrectFaceId( face );
				}
			}

			foreach ( var child in root.Children )
			{
				ValidateAndCorrectIds( child );
			}
		}

		private void ValidateAndCorrectObjectId( GBObject obj )
		{
			if ( !usedObjectIds.Add( obj.ID ) )
			{
				// ID is a duplicate, assign a new one
				while ( !usedObjectIds.Add( nextObjectId ) )
				{
					nextObjectId++;
				}
				obj.ID = nextObjectId;
				nextObjectId++;
			}
		}

		private void ValidateAndCorrectFaceId( GBFace face )
		{
			if ( !usedFaceIds.Add( face.ID ) )
			{
				// ID is a duplicate, assign a new one
				while ( !usedFaceIds.Add( nextFaceId ) )
				{
					nextFaceId++;
				}
				face.ID = nextFaceId;
				nextFaceId++;
			}
		}
	}

}
