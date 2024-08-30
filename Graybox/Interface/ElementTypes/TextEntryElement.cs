
using SkiaSharp;
using System.IO;

namespace Graybox.Interface
{
	public enum TextEntryModes
	{
		Everything,
		AlphaNumeric,
		Integer,
		Float
	}

	public class TextEntryElement : TextElement
	{

		public TextEntryModes Mode { get; set; }
		public bool ShowArrows { get; set; }
		public float StepSize { get; set; } = 1;

		public override CursorTypes Cursor
		{
			get => (MouseOverDownArrow || MouseOverUpArrow) ? CursorTypes.Default : base.Cursor;
			set => base.Cursor = value;
		}

		SKRect UpArrowRect;
		SKRect DownArrowRect;
		bool MouseOverUpArrow;
		bool MouseOverDownArrow;
		const float ArrowBoxWidth = 18;

		protected override SKRect FinalTextMargin()
		{
			var result = base.FinalTextMargin();

			if ( ShowArrows )
				result.Right += ArrowBoxWidth;

			return base.FinalTextMargin();
		}

		protected override void MouseMove( ref InputEvent e )
		{
			base.MouseMove( ref e );

			MouseOverUpArrow = ShowArrows && UpArrowRect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
			MouseOverDownArrow = ShowArrows && DownArrowRect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
			Refresh();
		}

		protected override void MouseDown( ref InputEvent e )
		{
			if ( MouseOverUpArrow )
			{
				Increment( StepSize );
				e.Handled = true;
				return;
			}

			if ( MouseOverDownArrow )
			{
				Increment( -StepSize );
				e.Handled = true;
				return;
			}

			base.MouseDown( ref e );
		}

		void Increment( float amount )
		{
			if ( Mode == TextEntryModes.Integer )
			{
				var intVal = int.Parse( Text );
				intVal += (int)amount;
				Text = intVal.ToString();
				SetCaretIndex( 9999, false );
				SkipCaret();
				EnsureNumber();
			}

			if ( Mode == TextEntryModes.Float )
			{
				var floatVal = float.Parse( Text );
				floatVal += amount;
				Text = floatVal.ToString();
				SetCaretIndex( 9999, false );
				SkipCaret();
				EnsureNumber();
			}
		}

		protected override void MouseExit( ref InputEvent e )
		{
			base.MouseExit( ref e );

			MouseOverUpArrow = false;
			MouseOverDownArrow = false;
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			if ( !ShowArrows ) return;

			var rect = BoxRect;
			var arrowHeight = rect.Height / 2;
			UpArrowRect = new SKRect( rect.Right - ArrowBoxWidth, rect.Top, rect.Right, rect.Top + arrowHeight );
			DownArrowRect = new SKRect( rect.Right - ArrowBoxWidth, rect.Top + arrowHeight, rect.Right, rect.Bottom );

			var hoveringUp = MouseOverUpArrow && IsHovered;
			var hoveringDown = MouseOverDownArrow && IsHovered;

			DrawChevron( canvas, UpArrowRect, true, hoveringUp ? Theme.ButtonBackgroundHover : Theme.DimButtonBackground );
			DrawChevron( canvas, DownArrowRect, false, hoveringDown ? Theme.ButtonBackgroundHover : Theme.DimButtonBackground );
		}

		private void DrawChevron( SKCanvas canvas, SKRect rect, bool up, SKColor color )
		{
			const int arrowWidth = 10;
			const int arrowHeight = 7;
			const int arrowOffsetX = 4;

			float verticalCenter = rect.Top + (rect.Height / 2);

			SKPoint point1, point2, point3;
			if ( up )
			{
				point1 = new SKPoint( rect.Right - arrowOffsetX, verticalCenter + (arrowHeight / 2) );
				point2 = new SKPoint( rect.Right - arrowOffsetX - arrowWidth / 2, verticalCenter - (arrowHeight / 2) );
				point3 = new SKPoint( rect.Right - arrowOffsetX - arrowWidth, verticalCenter + (arrowHeight / 2) );
			}
			else
			{
				point1 = new SKPoint( rect.Right - arrowOffsetX, verticalCenter - (arrowHeight / 2) );
				point2 = new SKPoint( rect.Right - arrowOffsetX - arrowWidth / 2, verticalCenter + (arrowHeight / 2) );
				point3 = new SKPoint( rect.Right - arrowOffsetX - arrowWidth, verticalCenter - (arrowHeight / 2) );
			}

			using ( var path = new SKPath() )
			{
				path.MoveTo( point1 );
				path.LineTo( point2 );
				path.LineTo( point3 );
				path.Close();

				using ( var paint = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true } )
				{
					canvas.DrawPath( path, paint );
				}
			}
		}

		protected override void Unfocused()
		{
			base.Unfocused();

			if ( Mode == TextEntryModes.Integer || Mode == TextEntryModes.Float )
			{
				EnsureNumber();
			}
		}

		protected override void KeyDown( ref InputEvent e )
		{
			if ( !IsFocused ) return;
			if ( !Editable ) return;

			//if ( e.ControlKey != ControlKeys.None )
			//{
			//	base.KeyDown( ref e );
			//	EnsureNumber();
			//	return;
			//}

			e.Handled = true;

			switch ( Mode )
			{
				case TextEntryModes.Everything:
					base.KeyDown( ref e );
					break;
				case TextEntryModes.AlphaNumeric:
					if ( char.IsLetterOrDigit( e.KeyChar ) )
						base.KeyDown( ref e );
					break;
				case TextEntryModes.Integer:
					if ( char.IsDigit( e.KeyChar ) || (e.KeyChar == '-' && CaretIndex == 0) )
					{
						base.KeyDown( ref e );
						EnsureNumber();
					}
					break;
				case TextEntryModes.Float:
					if ( char.IsDigit( e.KeyChar ) || e.KeyChar == '.' || (e.KeyChar == '-' && CaretIndex == 0) )
					{
						base.KeyDown( ref e );
						EnsureNumber();
					}
					break;
			}
		}

		protected override void OnPaste()
		{
			base.OnPaste();

			EnsureNumber();
		}

		void EnsureNumber()
		{
			if ( Mode == TextEntryModes.Integer || Mode == TextEntryModes.Float )
			{
				try
				{
					Text = Text.Trim();
					var val = float.Parse( Text );

					if ( Mode == TextEntryModes.Float )
					{
						Text = val.ToString( "0.00" );
					}
				}
				catch
				{
					Text = Mode == TextEntryModes.Integer ? "0" : "0.00";
				}
			}
		}

		public TextEntryElement( string text ) : base( text )
		{
			ApplyTheme();
		}

		public TextEntryElement()
		{
			ApplyTheme();
		}

		void ApplyTheme()
		{
			Editable = true;
			Selectable = true;
			BackgroundColor = Theme.InputBackground;
			BackgroundHoverColor = Theme.InputBackgroundHover;
			Color = Theme.InputForeground;
			BorderRadius = Theme.InputBorderRadius;
			BorderWidth = Theme.InputBorderWidth;
			BorderColor = Theme.InputBorderColor;
			PointerEvents = PointerEvents.All;
			Cursor = CursorTypes.Text;
			WordWrap = false;
		}

	}
}
