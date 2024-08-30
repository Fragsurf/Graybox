
using Graybox.Interface;
using SkiaSharp;

namespace Graybox.Editor.UI.TestElements
{
	internal class DirtyPageTest : UIElement
	{

		public DirtyPageTest()
		{
			Padding = 10;

			Add( new TextElement()
			{
				Text = "Dirty Render",
				FontSize = 20
			} );

			AddSpace( 10 );

			var container = Add( new UIElement()
			{
				Padding = 20,
				Wrap = WrapModes.Wrap,
				Direction = FlexDirection.Row
			} );

			for ( int i = 0; i < 10; i++ )
			{
				container.Add( new DirtyPageTestElement()
				{
					Width = 128,
					Height = 128,
					Margin = 4,
					BackgroundColor = SKColors.Black,
					BackgroundHoverColor = SKColors.OrangeRed,
				} );
			}
		}

	}

	internal class DirtyPageTestElement : UIElement
	{

		int DrawCount;

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			DrawCount++;

			using ( var paint = new SKPaint() )
			{
				paint.Color = SKColors.White;
				paint.TextAlign = SKTextAlign.Right;
				paint.TextSize = 12;

				var text = $"{DrawCount}";
				var metrics = paint.FontMetrics;
				var textWidth = paint.MeasureText( text );

				var lx = BoxRect.Right;
				var ly = BoxRect.Top - metrics.Ascent;

				canvas.DrawText( text, lx, ly, paint );
			}
		}

	}
}
