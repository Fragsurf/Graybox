
using Graybox.DataStructures.MapObjects;
using Graybox.Providers.Map.RMap;
using System.Drawing;

namespace Graybox.Providers.Map
{
	public class RMapProvider : MapProvider
	{

		protected override DataStructures.MapObjects.Map GetFromStream( Stream stream )
		{
			var result = new DataStructures.MapObjects.Map();
			var idGen = result.IDGenerator;
			var world = result.WorldSpawn;

			var sr = new StreamReader( stream );
			var contents = sr.ReadToEnd();
			var document = RMapDocument.FromJson( contents );

			LoadSolids( document, world, idGen );
			LoadEntities( document, world, idGen );

			result.WorldSpawn.UpdateBoundingBox( false );

			return result;
		}

		protected override bool IsValidForFileName( string filename )
		{
			return filename.EndsWith( ".rmap", StringComparison.OrdinalIgnoreCase );
		}

		protected override void SaveToFile( string filename, DataStructures.MapObjects.Map map, AssetSystem assetSystem )
		{
			var mapDoc = RMapDocument.FromMap( map );
			mapDoc.PackageRoots = assetSystem.Packages.Select( x => x.Directory.FullName ).ToList();

			using var stream = new FileStream( filename, FileMode.Create, FileAccess.Write );
			using var sw = new StreamWriter( stream );

			sw.Write( mapDoc.ToJson() );
		}

		void LoadSolids( RMapDocument document, World world, IDGenerator idGen )
		{
			var solids = document.Root.GetAllDescendants<Graybox.Providers.Map.RMap.Solid>();

			foreach ( var solid in solids )
			{
				var newSolid = new Graybox.DataStructures.MapObjects.Solid( idGen.GetNextObjectID() );
				newSolid.Colour = ColorUtility.GetRandomBrushColour();
				foreach ( var face in solid.Faces )
				{
					if ( face.Indices == null || face.Indices.Count == 0 ) continue;
					var newFace = new Face( idGen.GetNextFaceID() );
					newFace.Vertices = new List<Vertex>();
					newFace.Parent = newSolid;

					for ( int i = face.Indices.Count - 1; i >= 0; i-- )
					{
						var ind = face.Indices[i];
						var pos = solid.VertexPositions[ind.Index];
						newFace.Vertices.Add( new Vertex( new OpenTK.Mathematics.Vector3( pos.x, pos.z, pos.y ), newFace )
						{
							TextureU = ind.DiffuseUv.x,
							TextureV = ind.DiffuseUv.y,
						} );
					}

					var uaxis = face.TextureMapping.UAxis;
					var vaxis = face.TextureMapping.VAxis;

					newFace.TextureRef.UAxis = new OpenTK.Mathematics.Vector3( uaxis.x, uaxis.z, uaxis.y ).Normalized();
					newFace.TextureRef.VAxis = new OpenTK.Mathematics.Vector3( vaxis.x, vaxis.z, vaxis.y ).Normalized();
					newFace.TextureRef.XScale = face.TextureMapping.UScale;
					newFace.TextureRef.YScale = face.TextureMapping.VScale;
					newFace.TextureRef.XShift = face.TextureMapping.UShift;
					newFace.TextureRef.YShift = face.TextureMapping.VShift;
					newFace.TextureRef.AssetPath = face.Texture?.Identifier ?? "";

					var pointOnPlane = newFace.Vertices[0].Position;
					var normalX = face.Normal.x;
					var normalZ = face.Normal.y;
					var normalY = face.Normal.z;

					var distFromOrigin = pointOnPlane.X * normalX + pointOnPlane.Y * normalY + pointOnPlane.Z * normalZ;

					newFace.Plane = new Plane( new OpenTK.Mathematics.Vector3( normalX, normalY, normalZ ), distFromOrigin );
					newFace.Colour = newSolid.Colour;
					newFace.UpdateBoundingBox();

					newSolid.Faces.Add( newFace );
				}

				var mins = solid.Bounds.Min;
				var maxs = solid.Bounds.Max;

				newSolid.BoundingBox = new Box( new OpenTK.Mathematics.Vector3( mins.x, mins.z, mins.y ), new OpenTK.Mathematics.Vector3( maxs.x, maxs.z, maxs.y ) );
				newSolid.SetParent( world, false );

				if ( solid.Entity != null )
				{
					var entity = new Entity( idGen.GetNextObjectID() );
					entity.SetParent( world );
					entity.ClassName = solid.Entity.ClassName;
					foreach ( var kvp in solid.Entity.KeyValuePairs )
					{
						entity.EntityData.SetPropertyValue( kvp.Key, kvp.Value );
					}
					entity.Colour = ColorUtility.GetDefaultEntityColour();
					newSolid.SetParent( entity, false );
				}
			}
		}

		private void LoadEntities( RMapDocument document, World world, IDGenerator idGen )
		{
			var entities = document.Root.GetAllDescendants<EntityMapObject>();

			foreach ( var ent in entities )
			{
				var entity = new Entity( idGen.GetNextObjectID() );
				entity.SetParent( world );
				entity.ClassName = ent.ClassName;
				foreach ( var kvp in ent.KeyValuePairs )
				{
					entity.EntityData.SetPropertyValue( kvp.Key, kvp.Value );
				}
				entity.Colour = ColorUtility.GetDefaultEntityColour();
				entity.Origin = new OpenTK.Mathematics.Vector3( ent.Bounds.Center.x, ent.Bounds.Center.z, ent.Bounds.Center.y );
				entity.UpdateBoundingBox( false );
			}
		}

	}
}
