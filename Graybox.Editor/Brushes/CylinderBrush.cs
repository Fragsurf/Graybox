
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;
using System.ComponentModel.DataAnnotations;

namespace Graybox.Editor.Brushes;

public class CylinderBrush : IBrush
{

	public class Settings : BrushSettings
	{
		[Range( 4, 64 )]
		public int NumberOfSides { get; set; } = 8;
	}

	public string Name => "Cylinder";
	public string EditorIcon => "assets/icons/brush_cylinder.png";
	public BrushSettings GetSettingsInstance() => new Settings();


	public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
	{
		if ( brushSettings is not Settings settings )
			settings = new();

		int numSides = settings.NumberOfSides;
		if ( numSides < 3 ) yield break;

		// Cylinders can be elliptical so use both major and minor rather than just the radius
		// NOTE: when a low number (< 10ish) of faces are selected this will cause the cylinder to not touch all the edges of the box.
		float width = box.Width;
		float length = box.Length;
		float height = box.Height;
		float major = width / 2;
		float minor = length / 2;
		float angle = 2 * MathF.PI / numSides;

		// Calculate the X and Y points for the ellipse
		OpenTK.Mathematics.Vector3[] points = new OpenTK.Mathematics.Vector3[numSides];
		for ( int i = 0; i < numSides; i++ )
		{
			float a = i * angle;
			float xval = box.Center.X + major * MathF.Cos( a );
			float yval = box.Center.Y + minor * MathF.Sin( a );
			float zval = box.Start.Z;
			points[i] = new OpenTK.Mathematics.Vector3( xval, yval, zval ).Round( roundDecimals );
		}

		List<OpenTK.Mathematics.Vector3[]> faces = new List<OpenTK.Mathematics.Vector3[]>();

		// Add the vertical faces
		OpenTK.Mathematics.Vector3 z = new OpenTK.Mathematics.Vector3( 0, 0, height ).Round( roundDecimals );
		for ( int i = 0; i < numSides; i++ )
		{
			int next = (i + 1) % numSides;
			faces.Add( new[] { points[i], points[i] + z, points[next] + z, points[next] } );
		}
		// Add the elliptical top and bottom faces
		faces.Add( points.ToArray() );
		faces.Add( points.Select( x => x + z ).Reverse().ToArray() );

		// Nothing new here, move along
		Solid solid = new Solid( generator.GetNextObjectID() ) { Colour = ColorUtility.GetRandomBrushColour() };
		foreach ( OpenTK.Mathematics.Vector3[] arr in faces )
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
