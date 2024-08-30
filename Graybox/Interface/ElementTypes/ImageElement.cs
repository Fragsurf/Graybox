
using SkiaSharp;
using System.Collections.Generic;

namespace Graybox.Interface
{
	public class ImageElement : UIElement
	{

		string imagePath;
		public string ImagePath
		{
			get => imagePath;
			set
			{
				if ( imagePath == value ) return;
				imagePath = value;
				LoadImage();
			}
		}

		SKImage Image;

		public ImageElement( string imagePath )
		{
			ImagePath = imagePath;
		}

		public ImageElement()
		{

		}

		private void LoadImage()
		{
			if( ImageCache.ContainsKey( ImagePath ) )
			{
				Image = ImageCache[ ImagePath ];
				return;
			}

			try
			{
				using ( var stream = new SKFileStream( ImagePath ) )
				{
					using ( var bmp = SKBitmap.Decode( stream ) )
					{
						var result = SKImage.FromBitmap( bmp );
						ImageCache[ImagePath] = result;

						Image = result;
					}
				}
			}
			catch
			{
				Image = null; 
			}
		}

		static Dictionary<string, SKImage> ImageCache = new Dictionary<string, SKImage>();

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			if ( Image == null ) return;

			using ( var paint = new SKPaint() )
			{
				canvas.DrawImage( image: Image, source: Image.Info.Rect, dest: PaddedRect, new SKSamplingOptions( SKFilterMode.Linear, SKMipmapMode.Nearest ), paint );
			}
		}
	}
}
