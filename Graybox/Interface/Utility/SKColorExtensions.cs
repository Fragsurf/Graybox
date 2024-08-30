
using SkiaSharp;
using System;

namespace Graybox.Interface
{
	public static class SKColorExtensions
	{
		public static SKColor Darken( this SKColor color, float percentage )
		{
			return ChangeBrightness( color, -percentage );
		}

		public static SKColor Lighten( this SKColor color, float percentage )
		{
			return ChangeBrightness( color, percentage );
		}

		private static SKColor ChangeBrightness( SKColor color, float percentage )
		{
			float factor = 1 + percentage;
			byte r = (byte)Math.Max( Math.Min( color.Red * factor, 255 ), 0 );
			byte g = (byte)Math.Max( Math.Min( color.Green * factor, 255 ), 0 );
			byte b = (byte)Math.Max( Math.Min( color.Blue * factor, 255 ), 0 );
			return new SKColor( r, g, b, color.Alpha );
		}

		public static SKColor ToGrayscale( this SKColor color )
		{
			int avg = (color.Red + color.Green + color.Blue) / 3;
			return new SKColor( (byte)avg, (byte)avg, (byte)avg, color.Alpha );
		}

		public static SKColor AdjustOpacity( this SKColor color, float opacityFactor )
		{
			return new SKColor( color.Red, color.Green, color.Blue, (byte)(color.Alpha * opacityFactor) );
		}
	}
}
