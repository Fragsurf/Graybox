using System;
using System.Drawing;

namespace Graybox;

public static class ColorUtility
{

	private static readonly Random Rand = new();

	public static Color GetRandomColour()
	{
		return Color.FromArgb( 255, Rand.Next( 0, 256 ), Rand.Next( 0, 256 ), Rand.Next( 0, 256 ) );
	}
	public static Color GetRandomBrushColour()
	{
		return Color.FromArgb( 255, 0, Rand.Next( 128, 256 ), Rand.Next( 128, 256 ) );
	}

	public static Color GetRandomGroupColour()
	{
		return Color.FromArgb( 255, Rand.Next( 128, 256 ), Rand.Next( 128, 256 ), 0 );
	}

	public static Color GetRandomLightColour()
	{
		return Color.FromArgb( 255, Rand.Next( 128, 256 ), Rand.Next( 128, 256 ), Rand.Next( 128, 256 ) );
	}

	public static Color GetRandomDarkColour()
	{
		return Color.FromArgb( 255, Rand.Next( 0, 128 ), Rand.Next( 0, 128 ), Rand.Next( 0, 128 ) );
	}

	public static Color GetDefaultEntityColour()
	{
		return Color.FromArgb( 255, 255, 0, 255 );
	}

	public static Color4 Vary( this Color4 color, int by = 10 )
	{
		by = Rand.Next( -by, by );
		return new Color4(
			Math.Clamp( color.R + by / 255f, 0f, 1f ),
			Math.Clamp( color.G + by / 255f, 0f, 1f ),
			Math.Clamp( color.B + by / 255f, 0f, 1f ),
			color.A
		);
	}


	public static Color Darken( this Color color, int by = 20 )
	{
		return Color.FromArgb( color.A, Math.Max( 0, color.R - by ), Math.Max( 0, color.G - by ), Math.Max( 0, color.B - by ) );
	}

	public static Color Lighten( this Color color, int by = 20 )
	{
		return Color.FromArgb( color.A, Math.Min( 255, color.R + by ), Math.Min( 255, color.G + by ), Math.Min( 255, color.B + by ) );
	}

	public static Color4 Blend( this Color4 color, Color4 other )
	{
		return new Color4(
			color.R * other.R,
			color.G * other.G,
			color.B * other.B,
			color.A * other.A
		);
	}


}
