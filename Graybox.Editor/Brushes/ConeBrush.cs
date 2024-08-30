
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;
using System.ComponentModel.DataAnnotations;

namespace Graybox.Editor.Brushes;

public class ConeBrush : IBrush
{

	public class Settings : BrushSettings
	{
		[Range( 3, 12 )]
		public int NumberOfSides { get; set; } = 4;

		public Vector2 TestVector2 { get; set; }
		public Vector3 TestVector3 { get; set; }
	}

	public string Name => "Cone";
	public string EditorIcon => "assets/icons/brush_cone.png";
	public BrushSettings GetSettingsInstance() => new Settings();

	public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
	{
		if ( brushSettings is not Settings settings )
			settings = new Settings();

		int numSides = settings.NumberOfSides;
		if ( numSides < 3 ) yield break;

		// This is all very similar to the cylinder brush.
		float width = box.Width;
		float length = box.Length;
		float major = width / 2;
		float minor = length / 2;
		float angle = 2 * MathF.PI / numSides;

		var points = new Vector3[numSides];
		for ( int i = 0; i < numSides; i++ )
		{
			var a = i * angle;
			var xval = box.Center.X + major * MathF.Cos( a );
			var yval = box.Center.Y + minor * MathF.Sin( a );
			var zval = box.Start.Z;
			points[i] = new Vector3( xval, yval, zval ).Round( roundDecimals );
		}

		var faces = new List<Vector3[]>();
		var point = new Vector3( box.Center.X, box.Center.Y, box.End.Z ).Round( roundDecimals );
		for ( int i = 0; i < numSides; i++ )
		{
			int next = (i + 1) % numSides;
			faces.Add( new[] { points[i], point, points[next] } );
		}
		faces.Add( points.ToArray() );

		Solid solid = new Solid( generator.GetNextObjectID() ) { Colour = ColorUtility.GetRandomBrushColour() };
		foreach ( var arr in faces )
		{
			Face face = new Face( generator.GetNextFaceID() )
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
