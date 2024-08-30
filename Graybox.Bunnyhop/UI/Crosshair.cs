
using Graybox.Interface;
using SkiaSharp;

namespace Graybox.Bunnyhop.UI;

internal class Crosshair : UIElement
{

	public Crosshair()
	{
		Width = Length.Percent( 100 );
		Height = Length.Percent( 100 );
	}

	protected override void Paint( SKCanvas canvas )
	{
		base.Paint( canvas );

		float centerX = (float)Math.Floor( canvas.LocalClipBounds.MidX );
		float centerY = (float)Math.Floor( canvas.LocalClipBounds.MidY );

		float crosshairSize = 12;
		float crosshairThickness = 2;
		float crosshairGap = 4;
		SKColor crosshairColor = SKColors.White;
		SKColor crosshairOutlineColor = SKColors.Black; // Default outline color
		float crosshairOutlineThickness = 1; // Default outline thickness
		float outlineExtension = crosshairOutlineThickness; // Length to extend the outline beyond the main crosshair lines

		using ( var outlinePaint = new SKPaint() )
		{
			outlinePaint.Style = SKPaintStyle.Stroke;
			outlinePaint.Color = crosshairOutlineColor;
			outlinePaint.StrokeWidth = crosshairThickness + 2 * crosshairOutlineThickness; // Ensure the outline is visible around the main lines

			canvas.DrawLine( centerX - crosshairSize - outlineExtension, centerY, centerX - crosshairGap + outlineExtension, centerY, outlinePaint );
			canvas.DrawLine( centerX + crosshairGap - outlineExtension, centerY, centerX + crosshairSize + outlineExtension, centerY, outlinePaint );
			canvas.DrawLine( centerX, centerY - crosshairSize - outlineExtension, centerX, centerY - crosshairGap + outlineExtension, outlinePaint );
			canvas.DrawLine( centerX, centerY + crosshairGap - outlineExtension, centerX, centerY + crosshairSize + outlineExtension, outlinePaint );
		}

		using ( var paint = new SKPaint() )
		{
			paint.Style = SKPaintStyle.Stroke;
			paint.Color = crosshairColor;
			paint.StrokeWidth = crosshairThickness;
			
			canvas.DrawLine( centerX - crosshairSize, centerY, centerX - crosshairGap, centerY, paint );
			canvas.DrawLine( centerX + crosshairGap, centerY, centerX + crosshairSize, centerY, paint );
			canvas.DrawLine( centerX, centerY - crosshairSize, centerX, centerY - crosshairGap, paint );
			canvas.DrawLine( centerX, centerY + crosshairGap, centerX, centerY + crosshairSize, paint );
		}
	}

}
