
using System.Drawing;

namespace Graybox;

internal class ModelAsset : TextureAsset
{

	public override AssetTypes AssetType => AssetTypes.Model;

	public override Image GenerateThumbnail()
	{
		return GeneratePlainThumb();
	}

	Image GeneratePlainThumb()
	{
		var thumbnail = new Bitmap( 128, 128 ); // adjust the size as needed
		using ( var graphics = System.Drawing.Graphics.FromImage( thumbnail ) )
		{
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
			graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
			graphics.FillRectangle( System.Drawing.Brushes.DarkGray, 0, 0, thumbnail.Width, thumbnail.Height );

			using ( var font = new Font( "Arial", 12, FontStyle.Regular ) )
			using ( var brush = new SolidBrush( Color.Black ) )
			{
				var txt = "MODEL";
				var x = (thumbnail.Width - (int)graphics.MeasureString( txt, font ).Width) / 2;
				var y = (thumbnail.Height - (int)graphics.MeasureString( txt, font ).Height) / 2;

				graphics.DrawString( txt, font, brush, x, y );
			}
		}

		return thumbnail;
	}

}
