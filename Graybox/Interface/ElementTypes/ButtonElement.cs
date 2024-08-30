
using SkiaSharp;
using System.Collections.Generic;

namespace Graybox.Interface
{
	public class ButtonElement : TextElement
	{

		static Dictionary<string, SKFont> LocalFontCache = new Dictionary<string, SKFont>();
		static SKFont GetFont( string family, int size )
		{
			var key = $"{family}-{size}";

			if ( !LocalFontCache.ContainsKey( key ) )
			{
				SKFont result = null;

				try
				{
					var tf = SKTypeface.FromFile( family );
					result = new SKFont( tf, size );
				}
				catch
				{
					result = new SKFont( SKTypeface.FromFamilyName( "Arial" ), size );
				}

				result.Edging = SKFontEdging.Antialias;
				result.Hinting = SKFontHinting.Normal;
				LocalFontCache[key] = result;
			}

			return LocalFontCache[key];
		}

		string icon;
		public string Icon
		{
			get => icon;
			set
			{
				icon = value;
				if ( string.IsNullOrEmpty( icon ) )
				{
					TextMargin = default;
				}
				else
				{
					TextMargin = new SKRect( 7 + FontSize, 0, 0, 0 );
				}
			}
		}

		public ButtonElement( string text ) : base( text )
		{
			SetDefaultTheme();
		}

		public ButtonElement()
		{
			SetDefaultTheme();
		}

		protected override void MouseDown( ref InputEvent e )
		{
			base.MouseDown( ref e );

			e.Handled = true;
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			if ( string.IsNullOrEmpty( Icon ) ) return;

			var materialFont = GetFont( "Assets/Fonts/MaterialIcons-Regular.ttf", FontSize );
			var color = IsHovered && HoverColor != null ? HoverColor.Value : Color;
			var centered = string.IsNullOrEmpty( Text );

			canvas.Save();
			canvas.Translate( BoxRect.Left, BoxRect.Top );

			using ( var paint = new SKPaint() )
			{
				paint.Color = color;
				paint.IsAntialias = false; // Enable anti-aliasing for smoother edges
				paint.SubpixelText = true; // Enable subpixel rendering for sharper text
				paint.HintingLevel = SKPaintHinting.NoHinting;

				// Calculate the position to draw the text
				float x = centered ? (BoxRect.Width - FontSize) / 2 : 5;
				float y = BoxRect.Height * 0.5f + FontSize * 0.5f - 1;
				var pos = new SKPoint( x, y );

				// Draw the text
				canvas.DrawText( Icon, pos, materialFont, paint );
			}

			canvas.Restore();
		}

		protected virtual void SetDefaultTheme()
		{
			BackgroundColor = Theme.ButtonBackground;
			BackgroundHoverColor = Theme.ButtonBackgroundHover;
			Color = Theme.ButtonForeground;
			BorderRadius = Theme.ButtonBorderRadius;
			Padding = 4;
			Cursor = CursorTypes.Pointer;
			Centered = true;
			PointerEvents = PointerEvents.All;
		}

		public class Clear : ButtonElement
		{

			public Clear( string text ) : base( text )
			{
			}

			public Clear() : base()
			{
			}

			protected override void SetDefaultTheme()
			{
				base.SetDefaultTheme();

				BackgroundColor = SKColors.Transparent;
				BackgroundHoverColor = SKColors.Transparent;
				Color = Theme.DimButtonForeground;
				HoverColor = Theme.DimButtonForeground.Lighten( .15f );
			}


		}

		public class Dim : ButtonElement
		{

			public Dim( string text ) : base( text )
			{
			}

			public Dim() : base()
			{
			}

			protected override void SetDefaultTheme()
			{
				base.SetDefaultTheme();

				BackgroundColor = Theme.DimButtonBackground;
				BackgroundHoverColor = Theme.DimButtonBackgroundHover;
				Color = Theme.DimButtonForeground;
			}

		}

	}
}
