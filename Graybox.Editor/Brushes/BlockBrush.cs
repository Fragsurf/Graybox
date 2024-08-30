
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Brushes;

public class BlockBrush : IBrush
{

	public string Name => "Cube";
	public string EditorIcon => "assets/icons/brush_cube.png";

	public OverlayInfo GetOverlayInfo()
	{
		return new OverlayInfo()
		{
			Icon = Graybox.Interface.MaterialIcons.CropSquare
		};
	}

	public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
	{
		var solid = new Solid( generator.GetNextObjectID() ) { Colour = ColorUtility.GetRandomBrushColour() };
		box = box.EnsurePositive();

		foreach ( var arr in box.GetBoxFaces() )
		{
			Face face = new Face( generator.GetNextFaceID() )
			{
				Parent = solid,
				Plane = new Plane( arr[0], arr[1], arr[2] ),
				Colour = solid.Colour,
				TextureRef = { Texture = texture }
			};
			face.Vertices.AddRange( arr.Select( x => new Vertex( x.Round( roundDecimals ), face ) ) );
			face.UpdateBoundingBox();
			face.AlignTextureToFace();
			solid.Faces.Add( face );
		}
		solid.UpdateBoundingBox();
		yield return solid;
	}
}
