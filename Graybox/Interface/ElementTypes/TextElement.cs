
using Facebook.Yoga;
using SkiaSharp;
using System.Collections.Generic;
using Graybox.Interface.TextBlocks;
using System;

namespace Graybox.Interface
{
	public partial class TextElement : UIElement
	{

		string text;
		public string Text
		{
			get => text ?? string.Empty;
			set
			{
				text = value;
				EnsureCaret();
			}
		}

		public int FontSize { get; set; } = 12;
		public SKColor Color { get; set; } = Theme.Foreground;
		public SKColor? HoverColor { get; set; }
		public SKRect TextMargin { get; set; }
		public bool Centered { get; set; }
		public bool WordWrap { get; set; } = true;

		private readonly string FontFamily = "Arial";

		int oldhash;
		TextBlock textBlock;

		public TextElement( string text ) : this()
		{
			Text = text;
			PointerEvents = PointerEvents.None;
		}

		public TextElement() : base()
		{
			BackgroundColor = SKColors.Transparent;
			Color = Theme.Foreground;
			Node.SetMeasureFunction( MeasureFunction );
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			EnsureTextBlock();
			PaintSelection( canvas );

			var color = IsHovered ? (HoverColor ?? Color) : Color;
			textBlock.Color = color;

			var rect = PaddedRect;
			var margin = FinalTextMargin();
			rect.Left += margin.Left;
			rect.Right -= margin.Right;
			rect.Top += margin.Top;
			rect.Bottom -= margin.Bottom;

			if ( Centered )
			{
				float textHeight = textBlock.LineHeight;
				float verticalOffset = (rect.Height - textHeight) / 2;
				rect.Top += verticalOffset;
				rect.Bottom = rect.Top + textHeight;
			}

			canvas.DrawTextBlock( textBlock, rect );
		}

		private YogaSize MeasureFunction( YogaNode node, float width, YogaMeasureMode widthMode, float height, YogaMeasureMode heightMode )
		{
			if ( string.IsNullOrEmpty( Text ) )
				return new YogaSize();

			EnsureTextBlock();

			var measuredSize = textBlock.Measure( width );
			var measuredWidth = (widthMode == YogaMeasureMode.Undefined || measuredSize.Width < width) ? measuredSize.Width : width;
			var measuredHeight = (heightMode == YogaMeasureMode.Undefined || measuredSize.Height < height) ? measuredSize.Height : height;

			return new YogaSize
			{
				width = measuredWidth,
				height = measuredHeight
			};
		}

		protected SKSize MeasureText( string text, float maxWidth = 9999 )
		{
			if ( string.IsNullOrEmpty( text ) )
				return new SKSize();

			var block = new TextBlock( GetFont( FontSize, FontFamily ), SKColors.White, text );
			return block.Measure( maxWidth );
		}

		void EnsureTextBlock()
		{
			var hash = HashCode.Combine( Text, FontSize, Centered );
			var font = GetFont( FontSize, FontFamily );
			var lbrmode = Centered ? LineBreakMode.Center : (WordWrap ? LineBreakMode.WordWrap : LineBreakMode.MiddleTruncation);

			if ( hash != oldhash )
			{
				oldhash = hash;
				textBlock = new TextBlock( font, Color, Text, lbrmode );
			}

			if ( textBlock == null )
			{
				textBlock = new TextBlock( font, SKColors.White, ".", lbrmode );
			}

			textBlock.Color = Color;
		}

		protected override int GetPaintState()
		{
			return HashCode.Combine( base.GetPaintState(), Text, SelectionStart, CaretIndex, CaretVisible );
		}

		static Dictionary<string, Font> FontCache = new Dictionary<string, Font>();
		static Font GetFont( int size, string family )
		{
			var key = $"{family.ToLower()}/{size}";

			if ( !FontCache.TryGetValue( key, out var result ) )
			{
				result = new Font( family, size );
				FontCache[key] = result;
			}

			return result;
		}

		protected virtual SKRect FinalTextMargin()
		{
			return TextMargin;
		}

	}

	public enum FlowDirection
	{
		Unknown,
		LeftToRight,
		RightToLeft
	}

	public enum LineAlignment
	{
		Near,
		Center,
		Far
	}

	public enum LineBreakMode
	{
		WordWrap,
		Center,
		MiddleTruncation
	}

}
