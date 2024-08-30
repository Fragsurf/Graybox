using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Graybox.Editor.Brushes;

public class TorusBrush : IBrush
{
	public string Name => "Torus";
	public string EditorIcon => "assets/icons/brush_torus.png";

	public class Settings : BrushSettings
	{
		[Range( 3, 64 )]
		public int RingSides { get; set; } = 16;

		[Range( 3, 64 )]
		public int TubeSides { get; set; } = 8;

		[Range( 16, 1024 )]
		public int RingRadius { get; set; } = 64;

		[Range( 16, 1024 )]
		public int TubeRadius { get; set; } = 16;

		public bool MakeHollow { get; set; } = false;

		[Range( 1, 64 )]
		public int HollowThickness { get; set; } = 4;
	}

	public BrushSettings GetSettingsInstance() => new Settings();

	private const float EPSILON = 1e-5f;

	public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
	{
		if ( brushSettings is not Settings settings )
			yield break;

		var center = box.Center;
		var vertices = GenerateVertices( settings, center, roundDecimals );
		var solids = CreateRingSectionSolids( generator, settings, vertices, texture );

		var group = new Group( generator.GetNextObjectID() );

		foreach ( var solid in solids )
		{
			solid.SetParent( group );
		}

		yield return group;
	}

	private List<Vector3[]> GenerateVertices( Settings settings, Vector3 center, int roundDecimals )
	{
		var vertices = new List<Vector3[]>();

		for ( int i = 0; i <= settings.RingSides; i++ )
		{
			var ringAngle = 2 * Math.PI * i / settings.RingSides;
			var ringPoint = new Vector3(
				center.X + settings.RingRadius * (float)Math.Cos( ringAngle ),
				center.Y + settings.RingRadius * (float)Math.Sin( ringAngle ),
				center.Z
			);

			var tubeVertices = new List<Vector3>();
			for ( int j = 0; j <= settings.TubeSides; j++ )
			{
				var tubeAngle = 2 * Math.PI * j / settings.TubeSides;
				var x = (float)(Math.Cos( ringAngle ) * Math.Cos( tubeAngle ));
				var y = (float)(Math.Sin( ringAngle ) * Math.Cos( tubeAngle ));
				var z = (float)Math.Sin( tubeAngle );

				var point = new Vector3(
					ringPoint.X + settings.TubeRadius * x,
					ringPoint.Y + settings.TubeRadius * y,
					ringPoint.Z + settings.TubeRadius * z
				).Round( roundDecimals );

				tubeVertices.Add( point );
			}
			vertices.Add( tubeVertices.ToArray() );
		}

		return vertices;
	}

	private List<Solid> CreateRingSectionSolids( IDGenerator generator, Settings settings, List<Vector3[]> vertices, ITexture texture )
	{
		var solids = new List<Solid>();
		var color = ColorUtility.GetRandomBrushColour();

		for ( int i = 0; i < settings.RingSides; i++ )
		{
			var currentRing = vertices[i];
			var nextRing = vertices[i + 1];

			var solid = new Solid( generator.GetNextObjectID() )
			{
				Colour = color
			};

			// Outer face
			for ( int j = 0; j < settings.TubeSides; j++ )
			{
				TryAddQuadToSolid( solid, generator,
					currentRing[j], currentRing[j + 1],
					nextRing[j + 1], nextRing[j],
					texture );
			}

			if ( settings.MakeHollow )
			{
				float hollowRatio = Math.Min( 0.9f, (float)settings.HollowThickness / settings.TubeRadius );
				var innerCurrentRing = currentRing.Select( v => Vector3.Lerp( v, vertices[i][0], hollowRatio ) ).ToArray();
				var innerNextRing = nextRing.Select( v => Vector3.Lerp( v, vertices[i + 1][0], hollowRatio ) ).ToArray();

				// Inner face
				for ( int j = 0; j < settings.TubeSides; j++ )
				{
					TryAddQuadToSolid( solid, generator,
						innerCurrentRing[j + 1], innerCurrentRing[j],
						innerNextRing[j], innerNextRing[j + 1],
						texture );
				}

				// Side faces
				for ( int j = 0; j < settings.TubeSides; j++ )
				{
					TryAddQuadToSolid( solid, generator,
						currentRing[j], innerCurrentRing[j],
						innerNextRing[j], nextRing[j],
						texture );
				}

				// End caps
				TryAddPolygonToSolid( solid, generator, currentRing.Reverse().Concat( innerCurrentRing ).ToArray(), texture );
				TryAddPolygonToSolid( solid, generator, nextRing.Concat( innerNextRing.Reverse() ).ToArray(), texture );
			}
			else
			{
				// End caps for solid torus
				TryAddPolygonToSolid( solid, generator, currentRing.Reverse().ToArray(), texture );
				TryAddPolygonToSolid( solid, generator, nextRing.ToArray(), texture );
			}

			solid.UpdateBoundingBox();
			solids.Add( solid );
		}

		return solids;
	}

	private void TryAddQuadToSolid( Solid solid, IDGenerator generator, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, ITexture texture )
	{
		if ( ArePointsDegenerate( new[] { v1, v2, v3, v4 } ) )
			return;

		var face = new Face( generator.GetNextFaceID() )
		{
			Parent = solid,
			Plane = new Plane( v1, v2, v3 ),
			Colour = solid.Colour,
			TextureRef = { Texture = texture }
		};

		face.Vertices.AddRange( new[] { v1, v2, v3, v4 }.Select( v => new Vertex( v, face ) ) );
		face.UpdateBoundingBox();
		face.AlignTextureToWorld();
		solid.Faces.Add( face );
	}

	private void TryAddPolygonToSolid( Solid solid, IDGenerator generator, Vector3[] vertices, ITexture texture )
	{
		if ( vertices.Length < 3 || ArePointsDegenerate( vertices ) )
			return;

		var face = new Face( generator.GetNextFaceID() )
		{
			Parent = solid,
			Plane = new Plane( vertices[0], vertices[1], vertices[2] ),
			Colour = solid.Colour,
			TextureRef = { Texture = texture }
		};

		face.Vertices.AddRange( vertices.Select( v => new Vertex( v, face ) ) );
		face.UpdateBoundingBox();
		face.AlignTextureToWorld();
		solid.Faces.Add( face );
	}

	private bool ArePointsDegenerate( Vector3[] points )
	{
		if ( points.Length < 3 )
			return true;

		var v1 = points[1] - points[0];
		var v2 = points[2] - points[0];
		var normal = Vector3.Cross( v1, v2 );

		if ( normal.LengthSquared < EPSILON )
			return true;

		var plane = new Plane( points[0], normal );

		for ( int i = 3; i < points.Length; i++ )
		{
			if ( Math.Abs( plane.DistanceToPoint( points[i] ) ) > EPSILON )
				return false;
		}

		// Check if all points are collinear
		for ( int i = 2; i < points.Length; i++ )
		{
			var vi = points[i] - points[0];
			if ( Vector3.Cross( v1, vi ).LengthSquared > EPSILON )
				return false;
		}

		return true;
	}
}
