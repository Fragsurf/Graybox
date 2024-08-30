
using Graybox.Interface;
using SkiaSharp;

namespace Graybox.Editor.UI.TestElements
{
	internal class DialogElementsTest : UIElement
	{

		TextElement confirmResult;
		UIElement popupButton;

		public DialogElementsTest()
		{
			Add( new TextElement()
			{
				Margin = 10,
				FontSize = 20,
				Text = "Dialogs"
			} );

			Add( new FormRow( "Confirmation Dialog", new ButtonElement()
			{
				Text = "Open",
				Icon = MaterialIcons.OpenInNew,
				OnMouseDown = OpenConfirmationDialog
			} )
			{ Padding = 4 } );

			confirmResult = new TextElement( "Pending" );
			Add( new FormRow( "Confirmation Result", confirmResult ) { Padding = 4 } );

			popupButton = new ButtonElement()
			{
				Text = "Popup",
				Icon = MaterialIcons.OpenInNew,
				OnMouseDown = OpenPopupDialog
			};

			Add( new FormRow( "Popup Dialog", popupButton ) { Padding = 4 } );
		}

		void OpenConfirmationDialog( InputEvent e )
		{
			Window?.CreateWindow( new WindowOptions()
			{
				Title = "Confirmation",
				Width = 450,
				Height = 250,
				Resizable = false,
				IsDialog = true,
				CenterOnScreen = true
				//HideTitleBar = true
			}, new ConfirmationDialog()
			{
				OnConfirm = () =>
				{
					confirmResult.Text = "Confirmed";
					confirmResult.Color = SKColors.LightGreen;
				},
				OnCancel = () =>
				{
					confirmResult.Text = "Cancelled";
					confirmResult.Color = SKColors.IndianRed;
				}
			} );
		}

		void OpenPopupDialog( InputEvent e )
		{
			var popupContents = new UIElement()
			{
				Width = Length.Percent( 100 ),
				Height = Length.Percent( 100 ),
				AlignItems = FlexAlign.Center,
				JustifyContent = FlexJustify.Center
			};

			var btn = popupContents.Add( new ButtonElement( "Close Popup" ) );
			btn.OnMouseDown = ( ee ) => popupContents.FindRoot()?.Window?.Close();

			var btnScreenRect = ToScreen( popupButton.BoxRect );

			Window?.CreateWindow( new WindowOptions()
			{
				IsPopup = true,
				HideTitleBar = true,
				Width = 250,
				Height = 250,
				Position = new SKPoint( btnScreenRect.Left, btnScreenRect.Bottom )
			}, popupContents );
		}

	}
}
