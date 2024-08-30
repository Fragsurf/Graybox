
using Graybox.DataStructures.GameData;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Graybox.Utility;

public static class ParseUtility
{

	public static Vector3 ParseVector3( string x, string y, string z )
	{
		if ( string.IsNullOrEmpty( x ) ) x = "0";
		if ( string.IsNullOrEmpty( y ) ) y = "0";
		if ( string.IsNullOrEmpty( z ) ) z = "0";

		try
		{
			const NumberStyles ns = NumberStyles.Float;
			return new Vector3( float.Parse( x, ns ), float.Parse( y, ns ), float.Parse( z, ns ) );
		}
		catch ( Exception e )
		{
			Debug.LogWarning( $"Failed to parse {x} {y} {z} into Vector3" );
		}

		return default;
	}

	public static Color4 StringToColor( string input, Color4 defaultIfInvalid )
	{
		if ( string.IsNullOrEmpty( input ) )
			return defaultIfInvalid;

		try
		{
			var val = NormalizeValue( VariableType.Color255, input );
			var spl = val.Split( ' ' );

			if ( spl.Length < 3 || spl.Length > 4 )
			{
				return defaultIfInvalid;
			}

			var a = 255;
			if ( int.TryParse( spl[0], out var r ) && int.TryParse( spl[1], out var g ) && int.TryParse( spl[2], out var b ) )
			{
				if ( spl.Length == 4 && !int.TryParse( spl[3], out a ) )
				{
					return defaultIfInvalid;
				}
				return new Color4( (byte)r, (byte)g, (byte)b, (byte)a );
			}
		}
		catch ( Exception e )
		{
			Debug.LogWarning( $"Failed to parse {input} into color" );
		}

		return defaultIfInvalid;
	}

	public static float StringToFloat( string input, float defaultIfInvalid )
	{
		if ( string.IsNullOrEmpty( input ) )
			return defaultIfInvalid;

		try
		{
			if ( float.TryParse( input, out var result ) )
			{
				return result;
			}
		}
		catch ( Exception e )
		{
			Debug.LogWarning( $"Failed to parse {input} into float" );
		}

		return defaultIfInvalid;
	}

	public static Vector3 StringToVector3( string input, Vector3 defaultIfInvalid )
	{
		if ( string.IsNullOrEmpty( input ) )
			return defaultIfInvalid;

		try
		{
			var val = NormalizeValue( VariableType.Vector, input );
			var spl = val.Split( ' ' );

			if ( spl.Length < 3 )
				return defaultIfInvalid;

			if ( float.TryParse( spl[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x )
				&& float.TryParse( spl[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y )
				&& float.TryParse( spl[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z ) )
			{
				return new Vector3( x, y, z );
			}
		}
		catch ( Exception e )
		{
			Debug.LogWarning( $"Failed to parse {input} into Vector3" );
		}

		return defaultIfInvalid;
	}

	public static string ColorToString( Color4 color )
	{
		var r = (int)(color.R * 255);
		var g = (int)(color.G * 255);
		var b = (int)(color.B * 255);
		var a = (int)(color.A * 255);
		return $"{r} {g} {b} {a}";
	}

	public static string Vector3ToString( Vector3 coordinate )
	{
		return $"{coordinate.X} {coordinate.Y} {coordinate.Z}";
	}

	public static string NormalizeValue( VariableType varType, string input )
	{
		if ( string.IsNullOrEmpty( input ) ) 
			return input;

		input = input.Replace( "RGBA", "" );
		input = input.Replace( "(", "" );
		input = input.Replace( ")", "" );
		input = input.Replace( ",", " " );
		input = Regex.Replace( input, @"\s+", " " );
		input = input.Trim();

		return input;
	}

}
