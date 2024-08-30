
using System.Drawing;

namespace Graybox.Graphics;

public class GridArray : VBO<object, MapObjectVertex>
{

	private const int Grid = 0;

	private Quaternion rotation = Quaternion.Identity;

	public GridArray()
		: base( new object[0] )
	{
	}

	public void Render()
	{
		GL.LineWidth( 1.0f );
		Begin();
		foreach ( Subset subset in GetSubsets( Grid ) )
		{
			Render( PrimitiveType.Lines, subset );
		}
		End();
	}

	private float _step = 64;
	private int _low = -4096;
	private int _high = 4096;

	// Old default settings, might want to make this a configurable struct
	private Color GridLines = Color.FromArgb( 75, 75, 75 );
	private Color ZeroLines = Color.FromArgb( 0, 100, 100 );
	private Color BoundaryLines = Color.Red;

	private bool Highlight1On = true;
	private int Highlight1LineNum = 8;
	private Color Highlight1 = Color.FromArgb( 115, 115, 115 );

	private bool Highlight2On = true;
	private int Highlight2UnitNum = 1024;
	private Color Highlight2 = Color.FromArgb( 100, 46, 0 );

	private bool HideSmallerOn = true;
	private int HideSmallerThan = 4;
	private int HideFactor = 8;

	public void Update( int low, int high, float gridSpacing, float zoom, bool force = false )
	{
		float actualDist = gridSpacing * zoom;
		if ( HideSmallerOn )
		{
			while ( actualDist < HideSmallerThan )
			{
				gridSpacing *= HideFactor;
				actualDist *= HideFactor;
			}
		}
		if ( gridSpacing == _step && !force && low == _low && high == _high ) return; // This grid is the same as before
		_step = gridSpacing;
		_low = low;
		_high = high;
		Update( new object[0] );
	}

	protected override void CreateArray( IEnumerable<object> objects )
	{
		StartSubset( Grid );
		for ( float i = _low; i <= _high; i += _step )
		{
			Color c = GridLines;
			if ( i == 0 ) c = ZeroLines;
			else if ( i % Highlight2UnitNum == 0 && Highlight2On ) c = Highlight2;
			else if ( i % (_step * Highlight1LineNum) == 0 && Highlight1On ) c = Highlight1;
			float ifloat = (float)i;
			MakePoint( c, _low, ifloat );
			MakePoint( c, _high, ifloat );
			MakePoint( c, ifloat, _low );
			MakePoint( c, ifloat, _high );
		}

		// Top
		MakePoint( BoundaryLines, _low, _high );
		MakePoint( BoundaryLines, _high, _high );
		// Left
		MakePoint( BoundaryLines, _low, _low );
		MakePoint( BoundaryLines, _low, _high );
		// Right
		MakePoint( BoundaryLines, _high, _low );
		MakePoint( BoundaryLines, _high, _high );
		// Bottom
		MakePoint( BoundaryLines, _low, _low );
		MakePoint( BoundaryLines, _high, _low );

		PushSubset( Grid, (object)null );
	}

	private void MakePoint( Color4 color, float x, float y, float z = 0 )
	{
		Vector3 position = new Vector3( x, y, z );
		position = Vector3.Transform( position, rotation );

		PushIndex( Grid, PushData( new[]
		{
			new MapObjectVertex
			{
				Position = position,
				Color = color,
				Normal = Vector3.Zero,
				TexCoords = Vector2.Zero,
				TexCoordsLM = new Vector2(-500.0f, -500.0f),
				IsSelected = 0
			}
		} ), new uint[] { 0 } );
	}

	public void SetForwardVector( Vector3 forward )
	{
		rotation = GetQuaternionFromForward( forward );
	}

	private Quaternion GetQuaternionFromForward( Vector3 forward )
	{
		forward = forward.Normalized();

		// Default up vector
		Vector3 up = Vector3.UnitZ;

		if ( Math.Abs( Vector3.Dot( forward, up ) ) > 0.9999f )
		{
			return Quaternion.FromAxisAngle( Vector3.UnitZ, MathHelper.PiOver2 );
		}
		else
		{
			// Compute right vector
			Vector3 right = Vector3.Cross( up, forward ).Normalized();

			// Recompute orthogonal up vector
			up = Vector3.Cross( forward, right );

			Matrix4 rotationMatrix = new Matrix4(
				right.X, right.Y, right.Z, 0,
				up.X, up.Y, up.Z, 0,
				forward.X, forward.Y, forward.Z, 0,
				0, 0, 0, 1
			);

			return rotationMatrix.ExtractRotation();
		}
	}
}
