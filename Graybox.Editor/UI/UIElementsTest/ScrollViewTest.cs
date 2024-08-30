
using Graybox.Interface;
using SkiaSharp;

namespace Graybox.Editor.UI.TestElements
{
	internal class ScrollViewTest : UIElement
	{

		public ScrollViewTest()
		{
			Direction = FlexDirection.Column;

			Add( new TextElement()
			{
				Text = "Scroll view",
				Margin = 10,
				FontSize = 20
			} );

			var container = Add( new UIElement() { Direction = FlexDirection.Row } );

			var scrollBox = container.Add ( new UIElement()
			{
				BackgroundColor = Graybox.Interface.Theme.ContainerBackground,
				MaxHeight = 300,
				MinHeight = 300,
				Grow = 1,
				Shrink = 0,
				Margin = 10,
				Overflow = OverflowTypes.Scroll
			} );

			for ( int i = 0; i < 30; i++ )
			{
				var label = new TextElement()
				{
					Text = $"Label {i}",
					Padding = 4,
					BorderColor = Graybox.Interface.Theme.PopupBorderColor,
					BorderWidth = 1,
					FontSize = 15,
					Shrink = 0,
					Margin = 10,
					BorderRadius = 4,
					BoxShadow = true,
					BackgroundColor = SKColors.Black,
					BackgroundHoverColor = Graybox.Interface.Theme.PopupBorderColor,
					Cursor = CursorTypes.Pointer
				};
				scrollBox.Add( label );
			}

			scrollBox.Add( new TextElement()
			{
				Padding = 4,
				BackgroundColor = SKColors.Black,
				BorderColor = SKColors.Red,
				BorderWidth = 1,
				Editable = true,
				Selectable = true,
				Shrink = 0,
				FontSize = 20,
				Text = "Hello bottom of scroll",
				Margin = 10,
				Cursor = CursorTypes.Text
			} );

			var smoothScrollBox = container.Add( new UIElement()
			{
				BackgroundColor = Graybox.Interface.Theme.ContainerBackground,
				MaxHeight = 300,
				MinHeight = 300,
				Grow = 1,
				Shrink = 0,
				Margin = 10,
				Overflow = OverflowTypes.Scroll,
				SmoothScroll = true,
				BorderRadius = 50,
				BoxShadow = true,
			} );

			for ( int i = 0; i < 30; i++ )
			{
				var label = new TextElement()
				{
					Text = $"Label {i}",
					Padding = 4,
					BorderColor = Graybox.Interface.Theme.PopupBorderColor,
					BorderWidth = 1,
					FontSize = 15,
					Shrink = 0,
					Margin = 10,
					BorderRadius = 4,
					BoxShadow = true,
					BackgroundColor = SKColors.Black,
					BackgroundHoverColor = Graybox.Interface.Theme.PopupBorderColor,
					Cursor = CursorTypes.Pointer
				};
				smoothScrollBox.Add( label );
			}

			smoothScrollBox.Add( new TextElement()
			{
				Padding = 4,
				BackgroundColor = SKColors.Black,
				BorderColor = SKColors.Red,
				BorderWidth = 1,
				Editable = true,
				Selectable = true,
				Shrink = 0,
				FontSize = 20,
				Text = "Hello bottom of scroll",
				Margin = 10,
				Cursor = CursorTypes.Text
			} );
		}

	}
}
