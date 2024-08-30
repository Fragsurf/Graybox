using Graybox.Graphics;
using Graybox.DataStructures.MapObjects;
using System.ComponentModel.DataAnnotations;

namespace Graybox.Editor.Brushes;

public class StairsBrush : IBrush
{
	public string Name => "Stairs";
	public string EditorIcon => "assets/icons/brush_stairs.png";

	public OverlayInfo GetOverlayInfo()
	{
		return new OverlayInfo()
		{
			Icon = Graybox.Interface.MaterialIcons.Stairs
		};
	}

	public class Settings : BrushSettings
	{
		[Range( 4, 128 )]
		public int StepHeight { get; set; } = 16;

		public bool FullHeightSteps { get; set; } = true;
	}

	public BrushSettings GetSettingsInstance() => new Settings();

	private const float EPSILON = 1e-5f;

	public IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals )
	{
		if ( brushSettings is not Settings settings )
			yield break;

		var group = new Group( generator.GetNextObjectID() );
		var color = ColorUtility.GetRandomBrushColour();

		// Determine stair direction based on box dimensions
		bool stairsAlongX = box.Width < box.Length;
		float stairLength = stairsAlongX ? box.Width : box.Length;

		// Calculate number of steps based on box height and step height
		int stepCount = Math.Max( 2, (int)Math.Floor( box.Height / settings.StepHeight ) );

		var stepDepth = stairLength / stepCount;

		for ( int i = 0; i < stepCount; i++ )
		{
			Vector3 stepStart, stepEnd;
			if ( stairsAlongX )
			{
				// X is forward
				stepStart = new Vector3( box.Start.X + i * stepDepth, box.End.Y, box.Start.Z );
				stepEnd = new Vector3( box.Start.X + (i + 1) * stepDepth, box.Start.Y, box.Start.Z );
			}
			else
			{
				// Y is left
				stepStart = new Vector3( box.Start.X, box.End.Y - i * stepDepth, box.Start.Z );
				stepEnd = new Vector3( box.End.X, box.End.Y - (i + 1) * stepDepth, box.Start.Z );
			}

			// Adjust Z for current step, ensuring the first step has height
			stepStart.Z = box.Start.Z + i * settings.StepHeight;
			stepEnd.Z = box.Start.Z + (i + 1) * settings.StepHeight;

			if ( settings.FullHeightSteps )
			{
				stepStart.Z = box.Start.Z;
			}
			else if ( i == 0 )
			{
				// Ensure the first step always has some height
				stepEnd.Z = box.Start.Z + settings.StepHeight;
			}

			stepStart = stepStart.Round( roundDecimals );
			stepEnd = stepEnd.Round( roundDecimals );

			var stepBox = new Box( stepStart, stepEnd ).EnsurePositive();
			var stepSolid = CreateStepSolid( generator, stepBox, texture, color, roundDecimals );

			if ( stepSolid != null )
			{
				stepSolid.SetParent( group );
			}
		}

		yield return group;
	}

	private Solid CreateStepSolid( IDGenerator generator, Box stepBox, ITexture texture, Color4 color, int roundDecimals )
	{
		var solid = new Solid( generator.GetNextObjectID() ) { Colour = color };
		bool addedFace = false;

		foreach ( var faceVertices in stepBox.GetBoxFaces() )
		{
			if ( AddFaceToSolid( solid, generator, faceVertices, texture, roundDecimals ) )
			{
				addedFace = true;
			}
		}

		if ( !addedFace )
		{
			return null; // Return null if no faces were added
		}

		solid.UpdateBoundingBox();
		return solid;
	}

	private bool AddFaceToSolid( Solid solid, IDGenerator generator, Vector3[] vertices, ITexture texture, int roundDecimals )
	{
		if ( vertices.Length < 3 || ArePointsDegenerate( vertices ) )
			return false;

		var face = new Face( generator.GetNextFaceID() )
		{
			Parent = solid,
			Plane = new Plane( vertices[0], vertices[1], vertices[2] ),
			Colour = solid.Colour,
			TextureRef = { Texture = texture }
		};

		face.Vertices.AddRange( vertices.Select( v => new Vertex( v.Round( roundDecimals ), face ) ) );
		face.UpdateBoundingBox();
		face.AlignTextureToFace();
		solid.Faces.Add( face );

		return true;
	}

	private bool ArePointsDegenerate( Vector3[] points )
	{
		if ( points.Length < 3 )
			return true;

		var v1 = points[1] - points[0];
		var v2 = points[2] - points[0];
		var normal = Vector3.Cross( v1, v2 );

		return normal.LengthSquared < EPSILON;
	}
}
