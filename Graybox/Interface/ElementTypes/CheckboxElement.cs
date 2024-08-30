
using SkiaSharp;
using System;
using static Aardvark.Base.MultimethodTest;

namespace Graybox.Interface
{
	public class CheckboxElement : UIElement
	{

		public Action<bool> OnValueChanged;

		public SKColor CheckedColor { get; set; } = Theme.ButtonBackground;
		public bool Checked { get; set; }

		public CheckboxElement()
		{
			BackgroundColor = Theme.InputBackground;
			BackgroundHoverColor = Theme.InputBackgroundHover;
			CheckedColor = Theme.ButtonBackground;
			Cursor = CursorTypes.Pointer;
			Padding = 2;
			MaxWidth = 32;
			MaxHeight = 32;
			Width = 32;
			Height = 32;
			BorderRadius = Theme.InputBorderRadius;
			BorderWidth = Theme.InputBorderWidth;
			BorderColor = Theme.InputBorderColor;
		}

		protected override void MouseDown( ref InputEvent e )
		{
			base.MouseDown( ref e );

			Checked = !Checked;
			OnValueChanged?.Invoke( Checked );
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			if ( !Checked )
			{
				return;
			}

			var rect = BoxRect;
			rect.Left += 2;
			rect.Right -= 2;
			rect.Top += 2;
			rect.Bottom -= 2;

			using ( var paint = new SKPaint() )
			{
				paint.Color = CheckedColor;
				canvas.DrawRoundRect( rect, new SKSize( Theme.InputBorderRadius, Theme.InputBorderRadius ), paint );
			}
		}

		protected override int GetPaintState()
		{
			return HashCode.Combine ( base.GetPaintState(), Checked );
		}

	}
}
