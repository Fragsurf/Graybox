
using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Graybox.Editor.Brushes;

public class SphereBrush : IBrush
{

	public class Settings : BrushSettings
	{

		[Range( 10, 64 )]
		public int NumberOfSides { get; set; } = 24;

	}

	public string Name => "Sphere";
	public string EditorIcon => "assets/icons/brush_sphere.png";
	public BrushSettings GetSettingsInstance() => new Settings();


	public OverlayInfo GetOverlayInfo()
	{
		return new OverlayInfo()
		{
			Icon = Graybox.Interface.MaterialIcons.RadioButtonUnchecked
		};
	}

	private Solid MakeSolid( IDGenerator generator, IEnumerable<OpenTK.Mathematics.Vector3[]> faces, ITexture texture, Color col )
	{
		var solid = new Solid( generator.GetNextObjectID() ) { Colour = col };
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
			face.AlignTextureToWorld();
			solid.Faces.Add( face );
		}
		solid.UpdateBoundingBox();
		return solid;
	}

	public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
	{
		if ( brushSettings is not Settings settings )
			settings = new();

		var numSides = settings.NumberOfSides;
		if ( numSides < 3 ) yield break;

		roundDecimals = 2; // don't support rounding

		float width = box.Width;
		float length = box.Length;
		float height = box.Height;
		float major = width / 2;
		float minor = length / 2;
		float heightRadius = height / 2;

		float angleV = MathHelper.DegreesToRadians( 180 ) / numSides;
		float angleH = MathHelper.DegreesToRadians( 360 ) / numSides;

		var faces = new List<Vector3[]>();
		var bottom = new Vector3( box.Center.X, box.Center.Y, box.Start.Z ).Round( roundDecimals );
		var top = new Vector3( box.Center.X, box.Center.Y, box.End.Z ).Round( roundDecimals );

		for ( int i = 0; i < numSides; i++ )
		{
			// Top -> bottom
			float zAngleStart = angleV * i;
			float zAngleEnd = angleV * (i + 1);
			float zStart = heightRadius * MathF.Cos( zAngleStart );
			float zEnd = heightRadius * MathF.Cos( zAngleEnd );
			float zMultStart = MathF.Sin( zAngleStart );
			float zMultEnd = MathF.Sin( zAngleEnd );
			for ( int j = 0; j < numSides; j++ )
			{
				// Go around the circle in X/Y
				float xyAngleStart = angleH * j;
				float xyAngleEnd = angleH * ((j + 1) % numSides);
				float xyStartX = major * MathF.Cos( xyAngleStart );
				float xyStartY = minor * MathF.Sin( xyAngleStart );
				float xyEndX = major * MathF.Cos( xyAngleEnd );
				float xyEndY = minor * MathF.Sin( xyAngleEnd );
				var one = (new Vector3( xyStartX * zMultStart, xyStartY * zMultStart, zStart ) + box.Center).Round( roundDecimals );
				var two = (new Vector3( xyEndX * zMultStart, xyEndY * zMultStart, zStart ) + box.Center).Round( roundDecimals );
				var three = (new Vector3( xyEndX * zMultEnd, xyEndY * zMultEnd, zEnd ) + box.Center).Round( roundDecimals );
				var four = (new Vector3( xyStartX * zMultEnd, xyStartY * zMultEnd, zEnd ) + box.Center).Round( roundDecimals );
				if ( i == 0 )
				{
					// Top faces are triangles
					faces.Add( new[] { top, three, four } );
				}
				else if ( i == numSides - 1 )
				{
					// Bottom faces are also triangles
					faces.Add( new[] { bottom, one, two } );
				}
				else
				{
					// Inner faces are quads
					faces.Add( new[] { one, two, three, four } );
				}
			}
		}
		yield return MakeSolid( generator, faces, texture, ColorUtility.GetRandomBrushColour() );
	}
}
