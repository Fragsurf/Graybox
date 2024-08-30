
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Brushes
{
	public class WedgeBrush : IBrush
	{

		public string Name => "Wedge";
		public string EditorIcon => "assets/icons/brush_wedge.png";

		public OverlayInfo GetOverlayInfo()
		{
			return new OverlayInfo()
			{
				Icon = Graybox.Interface.MaterialIcons.ChangeHistory
			};
		}

		public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
		{
			Solid solid = new Solid( generator.GetNextObjectID() ) { Colour = ColorUtility.GetRandomBrushColour() };
			// The lower Z plane will be base, the x planes will be triangles
			var c1 = new Vector3( box.Start.X, box.Start.Y, box.Start.Z ).Round( roundDecimals );
			var c2 = new Vector3( box.End.X, box.Start.Y, box.Start.Z ).Round( roundDecimals );
			var c3 = new Vector3( box.End.X, box.End.Y, box.Start.Z ).Round( roundDecimals );
			var c4 = new Vector3( box.Start.X, box.End.Y, box.Start.Z ).Round( roundDecimals );
			var c5 = new Vector3( box.Center.X, box.Start.Y, box.End.Z ).Round( roundDecimals );
			var c6 = new Vector3( box.Center.X, box.End.Y, box.End.Z ).Round( roundDecimals );

			var faces = new[]
			{
				new[] { c1, c2, c3, c4 },
				new[] { c2, c1, c5 },
				new[] { c5, c6, c3, c2 },
				new[] { c4, c3, c6 },
				new[] { c6, c5, c1, c4 }
			};

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
