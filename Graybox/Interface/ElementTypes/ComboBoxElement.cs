
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Graybox.Interface
{
	public class ComboBoxElement : TextElement
	{

		public Action<string> OnValueChanged;
		public List<string> Options { get; set; } = new List<string>();
		public bool UsePopup { get; set; } = true;

		UIElement ComboDropdown;

		public ComboBoxElement()
		{
			BackgroundColor = Theme.InputBackground;
			BackgroundHoverColor = Theme.InputBackgroundHover;
			Color = Theme.InputForeground;
			BorderRadius = Theme.InputBorderRadius;
			BorderWidth = Theme.InputBorderWidth;
			BorderColor = Theme.InputBorderColor;
			PointerEvents = PointerEvents.All;
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			using ( var paint = new SKPaint() )
			{
				var color = SKColors.White.WithAlpha( 55 );

				if ( ComboDropdown != null || IsHovered )
					color = SKColors.White.WithAlpha( 85 );

				paint.Style = SKPaintStyle.Fill;
				paint.Color = color;
				paint.IsAntialias = true;

				var arrowWidth = 10;
				var arrowHeight = 7;
				var arrowOffsetX = 5;
				var verticalCenter = BoxRect.Top + (BoxRect.Height / 2);

				var point1 = new SKPoint( BoxRect.Right - arrowOffsetX, verticalCenter - (arrowHeight / 2) );
				var point2 = new SKPoint( BoxRect.Right - arrowOffsetX - (arrowWidth / 2), verticalCenter + (arrowHeight / 2) );
				var point3 = new SKPoint( BoxRect.Right - arrowOffsetX - arrowWidth, verticalCenter - (arrowHeight / 2) );

				using ( var path = new SKPath() )
				{
					path.MoveTo( point1 );
					path.LineTo( point2 );
					path.LineTo( point3 );
					path.Close();

					canvas.DrawPath( path, paint );
				}
			}
		}

		protected override void MouseDown( ref InputEvent e )
		{
			base.MouseDown( ref e );

			ToggleComboBox();

			e.Handled = true;
		}

		void CloseComboBox()
		{
			if ( ComboDropdown == null ) return;

			ComboDropdown.Window?.Close();
			ComboDropdown.Remove();
			ComboDropdown = null;
		}

		void ToggleComboBox()
		{
			if ( ComboDropdown != null )
			{
				CloseComboBox();
				return;
			}

			ComboDropdown = new UIElement();
			ComboDropdown.Width = Length.Percent( 100 );
			ComboDropdown.Height = Length.Percent( 100 );
			ComboDropdown.MinWidth = 32;
			ComboDropdown.MinHeight = 32;
			ComboDropdown.BackgroundColor = Theme.PopupBackgroundColor;
			ComboDropdown.BorderWidth = Theme.PopupBorderWidth;
			ComboDropdown.BorderColor = Theme.PopupBorderColor;
			ComboDropdown.BorderRadius = Theme.PopupBorderRadius;
			ComboDropdown.Name = "Combo Dropdown";
			ComboDropdown.Direction = FlexDirection.Column;
			ComboDropdown.Overflow = OverflowTypes.Scroll;

			var optionHeight = 24;

			foreach ( var option in Options )
			{
				var optionElement = new TextElement()
				{
					Text = option,
					Padding = 4,
					Shrink = 0,
					MinHeight = optionHeight,
					MaxHeight = optionHeight,
					BackgroundHoverColor = Theme.PopupBackgroundColor.Lighten( .25f ),
					BorderBottomWidth = 1.0f,
					BorderColor = Theme.PopupBorderColor,
					Color = Theme.ButtonForeground,
					Cursor = CursorTypes.Pointer,
					FontSize = this.FontSize
				};

				if ( option == Text )
				{
					optionElement.BackgroundColor = Theme.ButtonBackground.Darken( .5f );
					optionElement.BackgroundHoverColor = Theme.ButtonBackground.Darken( .4f );
				}

				optionElement.OnMouseDown = ( e ) =>
				{
					SetValue( option );
					CloseComboBox();
					e.Handled = true;
				};
				ComboDropdown.Add( optionElement );
			}

			ComboDropdown.CalculateLayout();

			if ( UsePopup )
			{
				var windowHeight = (int)ComboDropdown.MarginRect.Height;
				windowHeight = Math.Min( windowHeight, 450 );
				var screenRect = ToScreen( BoxRect );

				FindRoot().Window.CreateWindow( new WindowOptions()
				{
					IsPopup = true,
					Width = (int)BoxRect.Width,
					Height = (int)windowHeight,
					Position = new SKPoint( screenRect.Left, screenRect.Top + screenRect.Height + 1 ),
					Resizable = false,
					TransparentBackground = true
				}, ComboDropdown );
			}
			else
			{
				ComboDropdown.Position = PositionType.Absolute;
				ComboDropdown.Top = BoxRect.Top + BoxRect.Height + 1;
				ComboDropdown.Width = BoxRect.Width;
				ComboDropdown.Left = BoxRect.Left;
				ComboDropdown.MinWidth = 30;
				ComboDropdown.MinHeight = 30;
				ComboDropdown.Height = Length.Undefined;
				ComboDropdown.OnMouseDown = ( e ) =>
				{
					e.Handled = true;
					CloseComboBox();
				};
				FindRoot().Add( ComboDropdown );
				ComboDropdown.CaptureMouse();
			}
		}

		public void SetValue( string value, bool notify = true )
		{
			if ( Text == value ) return;

			Text = value;

			if ( notify )
			{
				OnValueChanged?.Invoke( value );
			}
		}

	}
}
