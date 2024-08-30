
using Graybox.Interface;
using SkiaSharp;

namespace Graybox.Editor.UI.TestElements
{
	internal class MainPageTest : UIElement
	{

		public MainPageTest()
		{
			Position = PositionType.Relative;
			Direction = FlexDirection.Column;
			Grow = 0;
			Shrink = 1;

			Add( new TextElement()
			{
				Text = "Home",
				Margin = 10,
				FontSize = 20
			} );

			var paragraph = new TextElement();
			paragraph.Text = "Roquefort pecorino feta. Mascarpone caerphilly goat camembert de normandie everyone loves boursin the big cheese squirty cheese. Cheddar mozzarella cheese slices emmental st. agur blue cheese caerphilly the big cheese cheese triangles. Jarlsberg emmental parmesan.\r\n\r\nCheesecake camembert de normandie roquefort. Parmesan paneer stinking bishop red leicester mozzarella the big cheese cheese strings cream cheese. Pepper jack cottage cheese mascarpone cheese strings bavarian bergkase smelly cheese gouda bocconcini. Bocconcini jarlsberg manchego squirty cheese camembert de normandie monterey jack halloumi caerphilly. Paneer fromage frais cheese triangles.";
			paragraph.Padding = 10;
			paragraph.BackgroundColor = Graybox.Interface.Theme.PopupBackgroundColor;
			paragraph.BorderColor = Graybox.Interface.Theme.PopupBorderColor;
			paragraph.BorderWidth = 1;
			paragraph.Margin = 10;
			paragraph.Shrink = 0;
			paragraph.MaxWidth = Length.Percent( 100 );

			Add( paragraph );

			Add( new TextElement()
			{
				Text = "😀",
				FontSize = 256,
				AlignSelf = FlexAlign.Center
			} );
		}

	}
}
