﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using SkiaSharp;

namespace Graybox.Interface.TextBlocks
{

	public interface RichTextSpan
	{

		(float FontHeight, float MarginY) GetMeasures( TextShaper textShaper );

		void GetLines( List<MeasuredSpan> lines, float maximumwidth, float firstlinestart, bool trimtrailingwhitespace );

		void DrawMeasuredSpan( SKCanvas canvas, float x, float y, float fontheight, float marginy, MeasuredSpan measuredSpan, bool isrtl );

	}

	/// <summary>
	/// A part of the text in a rich text block
	/// </summary>
	public class TextBlockSpan : RichTextSpan
	{

		//private static ListCache<MeasuredSpan> ListCache = new ListCache<MeasuredSpan>();

		/// <summary>
		/// The text to print
		/// </summary>
		public TextBlock TextBlock;

		/// <summary>
		/// X, and Y translation to apply before printing text in this span
		/// </summary>
		public SKPoint Translate;

		public TextBlockSpan( TextBlock textBlock )
		{
			TextBlock = textBlock ?? throw new ArgumentNullException( nameof( textBlock ) );
		}

		public TextBlockSpan( TextBlock textBlock, SKPoint translate )
		{
			TextBlock = textBlock ?? throw new ArgumentNullException( nameof( textBlock ) );
			Translate = translate;
		}

		public (float FontHeight, float MarginY) GetMeasures( TextShaper textShaper )
		{
			if ( TextBlock == null ) return (0, 0);
			TextBlock.LoadMeasures( textShaper );
			return (TextBlock.FontHeight, TextBlock.MarginY);
		}

		public void GetLines( List<MeasuredSpan> lines, float maximumwidth, float firstlinestart, bool trimtrailingwhitespace )
		{
			TextBlock.GetLines( lines, maximumwidth, firstlinestart, trimtrailingwhitespace );
		}

		public void DrawMeasuredSpan( SKCanvas canvas, float x, float y, float fontheight, float marginy, MeasuredSpan measuredSpan, bool isrtl )
		{
			if ( isrtl )
				canvas.DrawGlyphSpan( TextBlock.GlyphSpan, x - Translate.X, y + Translate.Y, TextBlock.Color, measuredSpan );
			else
				canvas.DrawGlyphSpan( TextBlock.GlyphSpan, x + Translate.X, y + Translate.Y, TextBlock.Color, measuredSpan );
		}

	}

	public abstract class OwnerDrawnRichTextSpan : RichTextSpan
	{

		public float Width;
		public float Height;

		public abstract SKSize GetSize( TextShaper textShaper );

		public (float FontHeight, float MarginY) GetMeasures( TextShaper textShaper )
		{
			var size = GetSize( textShaper );
			Width = size.Width;
			Height = size.Height;
			return (0, 0);
		}

		public void GetLines( List<MeasuredSpan> lines, float maximumwidth, float firstlinestart, bool trimtrailingwhitespace )
		{
			lines.Add( new MeasuredSpan( 0, 0, 0, Width ) );
		}

		public abstract void DrawMeasuredSpan( SKCanvas canvas, float x, float y, float fontheight, float marginy, MeasuredSpan measuredSpan, bool isrtl );

	}

}
