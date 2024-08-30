
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Graybox.Editor.Brushes
{
	public class PipeBrush : IBrush
	{

		public class Settings : BrushSettings
		{

			[Range( 3, 16 )]
			public int NumberOfSides { get; set; } = 4;
			[Range( 1, 1024 )]
			public int WallWidth { get; set; } = 32;

		}

		public string Name => "Pipe";
		public string EditorIcon => "assets/icons/brush_pipe.png";
		public BrushSettings GetSettingsInstance() => new Settings();


		private Solid MakeSolid( IDGenerator generator, IEnumerable<Vector3[]> faces, ITexture texture, Color col )
		{
			var solid = new Solid( generator.GetNextObjectID() ) { Colour = col };
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
			return solid;
		}

		public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
		{
			if ( brushSettings is not Settings settings )
				settings = new Settings();

			float wallWidth = settings.WallWidth;
			if ( wallWidth < 1 ) yield break;
			int numSides = settings.NumberOfSides;
			if ( numSides < 3 ) yield break;

			// Very similar to the cylinder, except we have multiple solids this time
			float width = box.Width;
			float length = box.Length;
			float height = box.Height;
			float majorOut = width / 2;
			float majorIn = majorOut - wallWidth;
			float minorOut = length / 2;
			float minorIn = minorOut - wallWidth;
			float angle = 2 * MathF.PI / numSides;

			// Calculate the X and Y points for the inner and outer ellipses
			var outer = new Vector3[numSides];
			var inner = new Vector3[numSides];
			for ( int i = 0; i < numSides; i++ )
			{
				float a = i * angle;
				float xval = box.Center.X + majorOut * MathF.Cos( a );
				float yval = box.Center.Y + minorOut * MathF.Sin( a );
				float zval = box.Start.Z;
				outer[i] = new Vector3( xval, yval, zval ).Round( roundDecimals );
				xval = box.Center.X + majorIn * MathF.Cos( a );
				yval = box.Center.Y + minorIn * MathF.Sin( a );
				inner[i] = new Vector3( xval, yval, zval ).Round( roundDecimals );
			}

			// Create the solids
			var colour = ColorUtility.GetRandomBrushColour();
			var z = new Vector3( 0, 0, height ).Round( roundDecimals );
			for ( int i = 0; i < numSides; i++ )
			{
				List<Vector3[]> faces = new List<Vector3[]>();
				int next = (i + 1) % numSides;
				faces.Add( new[] { outer[i], outer[i] + z, outer[next] + z, outer[next] } );
				faces.Add( new[] { inner[next], inner[next] + z, inner[i] + z, inner[i] } );
				faces.Add( new[] { outer[next], outer[next] + z, inner[next] + z, inner[next] } );
				faces.Add( new[] { inner[i], inner[i] + z, outer[i] + z, outer[i] } );
				faces.Add( new[] { inner[next] + z, outer[next] + z, outer[i] + z, inner[i] + z } );
				faces.Add( new[] { inner[i], outer[i], outer[next], inner[next] } );
				yield return MakeSolid( generator, faces, texture, colour );
			}
		}
	}
}
