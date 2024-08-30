using System.Drawing;
using System.Drawing.Imaging;
using SkiaSharp;

namespace Graybox.Utility;

internal static class SkiaExtensions
{
	// Extension method to convert System.Drawing.Bitmap to SKImage
	public static SKImage ToSKImage( this Bitmap bitmap )
	{
		using ( var memoryStream = new System.IO.MemoryStream() )
		{
			// Save bitmap to memory stream
			bitmap.Save( memoryStream, ImageFormat.Png );
			memoryStream.Seek( 0, System.IO.SeekOrigin.Begin );

			// Decode memory stream to SKImage
			using ( var skiaStream = new SKManagedStream( memoryStream ) )
			{
				return SKImage.FromEncodedData( skiaStream );
			}
		}
	}

	// Extension method to convert System.Drawing.Color to SKColor
	public static SKColor ToSKColor( this Color color )
	{
		return new SKColor( color.R, color.G, color.B, color.A );
	}
}
