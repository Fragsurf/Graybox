
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Brushes
{
	public class TetrahedronBrush : IBrush
	{

		public class Settings : BrushSettings
		{

			public bool TopVertexAtCenter { get; set; } = false;

		}

		public string Name => "Tetrahedron";
		public string EditorIcon => "assets/icons/brush_tetrahedron.png";
		public BrushSettings GetSettingsInstance() => new Settings();


		public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
		{
			if ( brushSettings is not Settings settings )
				settings = new();

			bool useCentroid = settings.TopVertexAtCenter;

			// The lower Z plane will be the triangle, with the lower Y value getting the two corners
			var c1 = new Vector3( box.Start.X, box.Start.Y, box.Start.Z ).Round( roundDecimals );
			var c2 = new Vector3( box.End.X, box.Start.Y, box.Start.Z ).Round( roundDecimals );
			var c3 = new Vector3( box.Center.X, box.End.Y, box.Start.Z ).Round( roundDecimals );
			var centroid = new Vector3( (c1.X + c2.X + c3.X) / 3, (c1.Y + c2.Y + c3.Y) / 3, box.End.Z );
			var c4 = (useCentroid ? centroid : new Vector3( box.Center.X, box.Center.Y, box.End.Z )).Round( roundDecimals );

			var faces = new[] {
				new[] { c1, c2, c3 },
				new[] { c4, c1, c3 },
				new[] { c4, c3, c2 },
				new[] { c4, c2, c1 }
			};

			var solid = new Solid( generator.GetNextObjectID() ) { Colour = ColorUtility.GetRandomBrushColour() };
			foreach ( var arr in faces )
			{
				var face = new Face( generator.GetNextFaceID() )
				{
					Parent = solid,
					Plane = new Plane( arr[0], arr[1], arr[2] ),
					Colour = solid.Colour,
					TextureRef = { Texture = texture }
				};
				face.Vertices.AddRange( arr.Select( x => new Vertex( x, face ) ) );
				face.UpdateBoundingBox();
				face.AlignTextureToFace();
				solid.Faces.Add( face );
			}
			solid.UpdateBoundingBox();
			yield return solid;
		}
	}
}
