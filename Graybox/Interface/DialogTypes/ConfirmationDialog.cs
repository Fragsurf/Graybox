
using SkiaSharp;
using System;

namespace Graybox.Interface
{
	public class ConfirmationDialog : UIElement
	{

		public string Message
		{
			get => ConfirmationText.Text;
			set => ConfirmationText.Text = value;
		}

		public Action OnConfirm;
		public Action OnCancel;

		TextElement ConfirmationText;

		public ConfirmationDialog()
		{
			BackgroundColor = Theme.PopupBackgroundColor;
			BorderColor = Theme.PopupBorderColor;
			BorderWidth = Theme.PopupBorderWidth;
			Width = Length.Percent( 100 );
			Height = Length.Percent( 100 );
			Shrink = 0;
			Padding = 20;

			ConfirmationText = new TextElement( "Are you sure you want to do that?" );
			ConfirmationText.Shrink = 0;

			Add( ConfirmationText );
			AddGrow();

			var buttonRow = new UIElement();
			buttonRow.Direction = FlexDirection.Row;
			buttonRow.Width = Length.Percent( 100 );
			buttonRow.Shrink = 0;

			var yesBtn = buttonRow.Add( new ButtonElement( "Yes" ) { Icon = MaterialIcons.Check, Grow = 1, Shrink = 0 } );
			buttonRow.AddSpace( 5 );
			var noBtn = buttonRow.Add( new ButtonElement.Dim( "No" ) { Icon = MaterialIcons.Cancel, Grow = 1, Shrink = 0 } );

			yesBtn.OnMouseDown = ( e ) =>
			{
				OnConfirm?.Invoke();
				FindRoot()?.Window?.Close();
				e.Handled = true;
			};

			noBtn.OnMouseDown = ( e ) =>
			{
				OnCancel?.Invoke();
				FindRoot()?.Window?.Close();

				e.Handled = true;
			};

			Add( buttonRow );
		}

	}
}
