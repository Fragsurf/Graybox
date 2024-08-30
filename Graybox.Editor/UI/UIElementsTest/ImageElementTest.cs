
using Graybox.Interface;
using SkiaSharp;

namespace Graybox.Editor.UI.TestElements
{
	internal class ImageElementTest : UIElement
	{

		public ImageElementTest()
		{
			Padding = 20;
			Overflow = OverflowTypes.Hidden;
			Grow = 0;
			Shrink = 1;
			MaxHeight = Length.Percent( 100 );

			Add( new TextElement()
			{
				Text = "Images",
				FontSize = 20,
				MarginBottom = 10
			} );

			var container = new UIElement()
			{
				Direction = FlexDirection.Row,
				Wrap = WrapModes.Wrap,
				Overflow = OverflowTypes.Scroll,
				Height = Length.Percent( 100 ),
			};

			for ( int i = 0; i < 15; i++ )
			{
				container.Add( new ImageElement()
				{
					ImagePath = "Assets/Images/fragsurf_test.png",
					MinWidth = 150,
					MinHeight = 150,
					MaxWidth = 150,
					MaxHeight = 150,
					BackgroundColor =	Graybox.Interface.Theme.ContainerBackground,
					BackgroundHoverColor = Graybox.Interface.Theme.ButtonBackground,
					Padding = 20,
					BorderRadius = 8,
					Margin = 4,
					BoxShadow = true
				} );
			}

			Add( container );
		}

	}
}
