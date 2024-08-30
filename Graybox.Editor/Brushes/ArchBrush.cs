
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Graybox.Editor.Brushes;

public class ArchBrush : IBrush
{

	public class Settings : BrushSettings
	{

		[Range( 3, 64 )]
		public int NumberOfSides { get; set; } = 8;
		[Range( 1, 32 )]
		public int WallWidth { get; set; } = 32;
		[Range( 1, 360 )]
		public int Arc { get; set; } = 360;
		[Range( 0, 359 )]
		public int StartAngle { get; set; } = 0;
		[Range( -1024, 1024 )]
		public int AddHeight { get; set; } = 0;
		[Range( -63, 63 )]
		public float TiltAngle { get; set; } = 0;
		public bool CurvedRamp { get; set; } = false;
		public bool TiltInterp { get; set; } = false;

	}

	public ArchBrush()
	{
	}

	public string Name => "Arch";
	public string EditorIcon => "assets/icons/brush_arch.png";
	public OverlayInfo GetOverlayInfo() => new OverlayInfo() { Icon = Graybox.Interface.MaterialIcons.Hexagon };
	public BrushSettings GetSettingsInstance() => new Settings();

	private Solid MakeSolid( IDGenerator generator, IEnumerable<Vector3[]> faces, ITexture texture, Color col )
	{
		var solid = new Solid( generator.GetNextObjectID() ) { Colour = col };
		foreach ( Vector3[] arr in faces )
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
			face.AlignTextureToWorld();
			solid.Faces.Add( face );
		}
		solid.UpdateBoundingBox();
		return solid;
	}

	public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
	{
		if ( brushSettings is not Settings settings )
			settings = new Settings();

		int numSides = (int)settings.NumberOfSides;
		if ( numSides < 3 ) yield break;
		float wallWidth = settings.WallWidth;
		if ( wallWidth < 1 ) yield break;
		float arc = settings.Arc;
		if ( arc < 1 ) yield break;
		float startAngle = settings.StartAngle;
		if ( startAngle < 0 || startAngle > 359 ) yield break;
		float addHeight = settings.AddHeight;
		bool curvedRamp = settings.CurvedRamp;
		float tiltAngle = curvedRamp ? settings.TiltAngle : 0;
		if ( MathF.Abs( tiltAngle % 180 ) == 90 ) yield break;
		bool tiltInterp = curvedRamp && settings.TiltInterp;

		// Very similar to the pipe brush, except with options for start angle, arc, height and tilt
		var width = box.Width;
		var length = box.Length;
		var height = box.Height;

		var majorOut = width / 2;
		var majorIn = majorOut - wallWidth;
		var minorOut = length / 2;
		var minorIn = minorOut - wallWidth;

		var start = MathHelper.DegreesToRadians( startAngle );
		var tilt = MathHelper.DegreesToRadians( tiltAngle );
		var angle = MathHelper.DegreesToRadians( arc ) / numSides;

		// Calculate the coordinates of the inner and outer ellipses' points
		var outer = new Vector3[numSides + 1];
		var inner = new Vector3[numSides + 1];
		for ( int i = 0; i < numSides + 1; i++ )
		{
			var a = start + i * angle;
			var h = i * addHeight;
			var interp = tiltInterp ? MathF.Cos( MathF.PI / numSides * (i - numSides / 2F) ) : 1;
			var tiltHeight = wallWidth / 2 * interp * MathF.Tan( tilt );

			var xval = box.Center.X + majorOut * MathF.Cos( a );
			var yval = box.Center.Y + minorOut * MathF.Sin( a );
			var zval = box.Start.Z + (curvedRamp ? h + tiltHeight : 0);
			outer[i] = new Vector3( xval, yval, zval ).Round( roundDecimals );

			xval = box.Center.X + majorIn * MathF.Cos( a );
			yval = box.Center.Y + minorIn * MathF.Sin( a );
			zval = box.Start.Z + (curvedRamp ? h - tiltHeight : 0);
			inner[i] = new Vector3( xval, yval, zval ).Round( roundDecimals );
		}

		// Create the solids
		var colour = ColorUtility.GetRandomBrushColour();
		var z = new Vector3( 0, 0, height ).Round( roundDecimals );
		for ( int i = 0; i < numSides; i++ )
		{
			List<Vector3[]> faces = new List<Vector3[]>();

			// Since we are triangulating/splitting each arch segment, we need to generate 2 brushes per side
			if ( curvedRamp )
			{
				// The splitting orientation depends on the curving direction of the arch
				if ( addHeight >= 0 )
				{
					faces.Add( new[] { outer[i], outer[i] + z, outer[i + 1] + z, outer[i + 1] } );
					faces.Add( new[] { outer[i + 1], outer[i + 1] + z, inner[i] + z, inner[i] } );
					faces.Add( new[] { inner[i], inner[i] + z, outer[i] + z, outer[i] } );
					faces.Add( new[] { outer[i] + z, inner[i] + z, outer[i + 1] + z } );
					faces.Add( new[] { outer[i + 1], inner[i], outer[i] } );
				}
				else
				{
					faces.Add( new[] { inner[i + 1], inner[i + 1] + z, inner[i] + z, inner[i] } );
					faces.Add( new[] { outer[i], outer[i] + z, inner[i + 1] + z, inner[i + 1] } );
					faces.Add( new[] { inner[i], inner[i] + z, outer[i] + z, outer[i] } );
					faces.Add( new[] { inner[i + 1] + z, outer[i] + z, inner[i] + z } );
					faces.Add( new[] { inner[i], outer[i], inner[i + 1] } );
				}
				yield return MakeSolid( generator, faces, texture, colour );

				faces.Clear();

				if ( addHeight >= 0 )
				{
					faces.Add( new[] { inner[i + 1], inner[i + 1] + z, inner[i] + z, inner[i] } );
					faces.Add( new[] { inner[i], inner[i] + z, outer[i + 1] + z, outer[i + 1] } );
					faces.Add( new[] { outer[i + 1], outer[i + 1] + z, inner[i + 1] + z, inner[i + 1] } );
					faces.Add( new[] { inner[i + 1] + z, outer[i + 1] + z, inner[i] + z } );
					faces.Add( new[] { inner[i], outer[i + 1], inner[i + 1] } );
				}
				else
				{
					faces.Add( new[] { outer[i], outer[i] + z, outer[i + 1] + z, outer[i + 1] } );
					faces.Add( new[] { inner[i + 1], inner[i + 1] + z, outer[i] + z, outer[i] } );
					faces.Add( new[] { outer[i + 1], outer[i + 1] + z, inner[i + 1] + z, inner[i + 1] } );
					faces.Add( new[] { outer[i] + z, inner[i + 1] + z, outer[i + 1] + z } );
					faces.Add( new[] { outer[i + 1], inner[i + 1], outer[i] } );
				}
				yield return MakeSolid( generator, faces, texture, colour );
			}
			else
			{
				var h = i * addHeight * Vector3.UnitZ;
				faces.Add( new[] { outer[i], outer[i] + z, outer[i + 1] + z, outer[i + 1] }.Select( x => x + h ).ToArray() );
				faces.Add( new[] { inner[i + 1], inner[i + 1] + z, inner[i] + z, inner[i] }.Select( x => x + h ).ToArray() );
				faces.Add( new[] { outer[i + 1], outer[i + 1] + z, inner[i + 1] + z, inner[i + 1] }.Select( x => x + h ).ToArray() );
				faces.Add( new[] { inner[i], inner[i] + z, outer[i] + z, outer[i] }.Select( x => x + h ).ToArray() );
				faces.Add( new[] { inner[i + 1] + z, outer[i + 1] + z, outer[i] + z, inner[i] + z }.Select( x => x + h ).ToArray() );
				faces.Add( new[] { inner[i], outer[i], outer[i + 1], inner[i + 1] }.Select( x => x + h ).ToArray() );
				yield return MakeSolid( generator, faces, texture, colour );
			}
		}
	}

}
