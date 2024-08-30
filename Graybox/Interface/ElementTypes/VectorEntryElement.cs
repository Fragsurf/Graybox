
using SkiaSharp;

namespace Graybox.Interface
{
	public class VectorEntryElement : UIElement
	{

		public SKPoint3 Value { get; set; }
		public bool IncludeZ { get; set; } = false;
		public bool ShowArrows { get; set; } = false;

		public VectorEntryElement( bool showArrows = false )
		{
			ShowArrows = showArrows;
			Grow = 1;
			Shrink = 0;
			Direction = FlexDirection.Row;

			float inputWidth = 55;

			var xLabel = Add( new TextElement( "X" ) { MinWidth = 15, MarginLeft = 0, MarginRight = 5, Centered = true, Shrink = 0 } );
			var xInput = Add( new TextEntryElement( "0.00" ) { Mode = TextEntryModes.Float, BorderRadius = 0, Grow = 1, Centered = !ShowArrows, ShowArrows = ShowArrows } );
			var yLabel = Add( new TextElement( "Y" ) { MinWidth = 15, MarginLeft = 5, MarginRight = 5, Centered = true, Shrink = 0 } );
			var yInput = Add( new TextEntryElement( "0.00" ) { Mode = TextEntryModes.Float, BorderRadius = 0, Grow = 1, Centered = !ShowArrows, ShowArrows = ShowArrows } );

			xInput.MinWidth = inputWidth;
			xInput.MaxWidth = inputWidth;
			xInput.PaddingLeft = 5;
			yInput.MinWidth = inputWidth;
			yInput.MaxWidth = inputWidth;
			yInput.PaddingLeft = 5;

			if ( IncludeZ )
			{
				var zLabel = Add( new TextElement( "Z" ) { MinWidth = 15, MarginLeft = 5, MarginRight = 5, Centered = true, Shrink = 0 } );
				var zInput = Add( new TextEntryElement( "0.00" ) { Mode = TextEntryModes.Float, BorderRadius = 0, Grow = 1, Centered = !ShowArrows, ShowArrows = ShowArrows } );

				zInput.MinWidth = inputWidth;
				zInput.MaxWidth = inputWidth;
				zInput.PaddingLeft = 5;
			}
		}

	}
}
