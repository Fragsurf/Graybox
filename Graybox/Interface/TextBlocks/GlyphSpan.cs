﻿
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Graybox.Interface.TextBlocks
{

	/// <summary>
	/// Contains all the information necessary to quickly measure and draw parts (or all) of a string.
	/// </summary>
	public class GlyphSpan : IDisposable
	{

		public readonly SKPaint Paint;
		public readonly SKFont[] Fonts;

		/// <summary>
		/// The direction the span should be read in.
		/// Note that for RLT strings, the first word in the string will be printed on the right.
		/// IOW, Words are 0 is first word, but Glyphs in the span are ReadDirection dependant.
		/// This is important for correct line breaking for RTL strings.
		/// </summary>
		public readonly FlowDirection ReadDirection;

		// RTL: 
		//                     word2abc  word0
		//                               ^   ^
		//                               |   first glyph:[0]
		//                               last glyph: [4] 
		// 
		// glyph               14            0
		// StartPoints.Index   0             14
		// StartPoints.x:    -100            0
		// codepoints.Index    0             14
		//                              
		private readonly ushort[] Codepoints; // due to the way HarfBuzz works, these are always LTR. 2 bytes per glyph
		private readonly byte[] FontIDs;
		private readonly SKPoint[] StartPoints;
		private readonly SKPoint[] PaintPoints; // buffer for transposed locations

		/// <summary>
		/// All the words in the set.
		/// </summary>
		public readonly (int firstglyph, int lastglyph, WordType type)[] Words;

		/// <summary>
		/// The number of glyphs in the whole set.
		/// Note that this may be different from the number of characters in the original string.
		/// </summary>
		public readonly int GlyphCount;

		/// <summary>
		/// The number of words.
		/// </summary>
		public readonly int WordCount;

		public GlyphSpan( SKPaint paint, SKFont[] fonts )
		{
			if ( paint == null ) throw new ArgumentOutOfRangeException( nameof( paint ) );
			if ( fonts == null || fonts.Length < 1 ) throw new ArgumentOutOfRangeException( nameof( fonts ) );
			Paint = paint;
			Fonts = fonts;
			Codepoints = new ushort[0];
			StartPoints = new SKPoint[0];
			PaintPoints = new SKPoint[0];
			Words = new (int, int, WordType)[0];
		}

		public void Dispose()
		{
			Paint.Dispose();
			for ( int i = 0; i < Fonts.Length; i++ )
				Fonts[i].Dispose();
		}

		public GlyphSpan( SKPaint paint, SKFont[] fonts, FlowDirection readDirection, byte[] fontids, ushort[] codepoints, SKPoint[] startpoints, int glyphcount, List<(int firstglyph, int lastglyph, WordType type)> words )
		{

			if ( paint == null ) throw new ArgumentOutOfRangeException( nameof( paint ) );
			if ( fonts == null || fonts.Length < 1 ) throw new ArgumentOutOfRangeException( nameof( fonts ) );

			Paint = paint;
			Fonts = fonts;

			ReadDirection = readDirection == FlowDirection.Unknown ? FlowDirection.LeftToRight : readDirection;
			FontIDs = fontids;
			Codepoints = codepoints;
			StartPoints = startpoints;
			PaintPoints = new SKPoint[StartPoints.Length];
			GlyphCount = glyphcount; // note that the startpoints array in some scenario's isn't fully filled out, and glyphcount may be different from StartPoints.Length
			Words = words.ToArray();
			WordCount = Words.Length;

		}


		/// <summary>
		/// Calculate the full extent of the span, optionally removing trailing white space.
		/// Note that linebreak words are always trimmed from the end. 
		/// The caller is responsible for making sure no linebreak words are embedded in the requested span.
		/// </summary>
		/// <param name="wordstart">the index of the first word to measure</param>
		/// <param name="wordend"></param>
		/// <param name="trimtrailingwhitespace"></param>
		/// <returns></returns>
		public MeasuredSpan MeasureWordSpan( int wordstart, int wordend, bool trimtrailingwhitespace = false )
		{

			var lastglyph = Words[wordend].lastglyph; // last measured glyph (ie, including whitespace, and line breaks)
			var start = Words[wordstart].firstglyph; // first printed glyph

			if ( wordend < wordstart )
				return new MeasuredSpan( start, -1, lastglyph, 0 );

			if ( trimtrailingwhitespace )
				while ( wordend > -1 && Words[wordend].type != WordType.Word )
					wordend--; // trim trailing whitespace and line breaks
			else
				while ( wordend > -1 && Words[wordend].type == WordType.Linebreak )
					wordend--; // trim trailing line breaks only


			if ( wordend < wordstart )
				return new MeasuredSpan( start, -1, lastglyph, 0 );

			var end = Words[wordend].lastglyph; // last printed glyph (ie, excluding whitespace, and line breaks)

			return Measure( start, end, lastglyph );

		}

		/// <summary>
		/// Calculate the full extent of the span, optionally removing trailing white space.
		/// Note that linebreak words are always trimmed from the end. 
		/// The caller is responsible for making sure no linebreak words are embedded in the requested span.
		/// </summary>
		/// <param name="wordstart">the index of the first word to measure</param>
		/// <param name="wordend"></param>
		/// <param name="trimtrailingwhitespace"></param>
		/// <returns></returns>
		public MeasuredSpan MeasureGlyphToWordSpan( int start, int wordend, bool trimtrailingwhitespace = false )
		{

			var lastglyph = Words[wordend].lastglyph; // last measured glyph (ie, including whitespace, and line breaks)

			if ( trimtrailingwhitespace )
				while ( wordend > -1 && Words[wordend].type != WordType.Word )
					wordend--; // trim trailing whitespace and line breaks
			else
				while ( wordend > -1 && Words[wordend].type == WordType.Linebreak )
					wordend--; // trim trailing line breaks only

			var end = Words[wordend].lastglyph; // last printed glyph (ie, excluding whitespace, and line breaks)

			return Measure( start, end, lastglyph );

		}

		/// <summary>
		/// Calculate the measure of a (zero based) span of glyphs.
		/// </summary>
		public MeasuredSpan Measure( int firstglyph, int lastglyph )
		{
			return Measure( firstglyph, lastglyph, lastglyph );
		}

		private MeasuredSpan Measure( int firstglyph, int lastglyph, int lastmeasuredglyph )
		{
			if ( ReadDirection == FlowDirection.LeftToRight )
			{
				var xstart = StartPoints[firstglyph].X;
				var xend = StartPoints[lastglyph + 1].X;
				return new MeasuredSpan( firstglyph, lastglyph, lastmeasuredglyph, xend - xstart );
			}
			else
			{
				var pstart = StartPoints.Length - lastglyph - 2;
				var pend = StartPoints.Length - firstglyph - 1;
				var xstart = StartPoints[pstart].X;
				var xend = StartPoints[pend].X;
				return new MeasuredSpan( firstglyph, lastglyph, lastmeasuredglyph, xend - xstart );
			}
		}

		public void PaintBlocks( SKCanvas canvas, int firstglyph, int lastglyph, float x, float y, SKColor color, GlyphAnimation animation )
		{

			var pointstart = (ReadDirection == FlowDirection.LeftToRight) ? firstglyph : StartPoints.Length - lastglyph - 2;
			var deltax = x - StartPoints[pointstart].X;
			deltax = (float)Math.Round( deltax );

			var points = new SKPoint[1];
			for ( var g = firstglyph; g <= lastglyph; g++ )
			{

				var paint = Paint;
				var font = Fonts[FontIDs[g]];

				var idx = (ReadDirection == FlowDirection.LeftToRight) ? g : StartPoints.Length - g - 2;
				var sp = StartPoints[idx];
				points[0] = animation.Transpose( g, GlyphCount, new SKPoint( deltax + sp.X, sp.Y + y ) );
				paint.Color = animation.GetColor == null ? color : animation.GetColor( g, GlyphCount );
				if ( animation.UpdatePaint != null ) animation.UpdatePaint( g, GlyphCount, paint, false );

				var builder = new SKTextBlobBuilder();
				builder.AddPositionedRun( Codepoints.AsSpan( g, 1 ), font, points.AsSpan() );
				using ( var textBlob = builder.Build() )
					canvas.DrawText( textBlob, 0, 0, paint );

				if ( animation.UpdatePaint != null ) animation.UpdatePaint( g, GlyphCount, paint, true );

			}

		}

		public void PaintBlocks( SKCanvas canvas, int firstglyph, int lastglyph, float x, float y, SKColor color, Action<SKPaint, bool> updatePaint = null )
		{

			var pointstart = (ReadDirection == FlowDirection.LeftToRight) ? firstglyph : StartPoints.Length - lastglyph - 2;
			var deltax = x - StartPoints[pointstart].X;
			deltax = (float)Math.Round( deltax );

			// draw each paint

			var codepointstart = new ReadOnlySpan<ushort>( Codepoints );
			//fixed (byte* codepointstart = Codepoints)
			{
				for ( int f = 0; f < Fonts.Length; f++ )
				{

					for ( int s = firstglyph; s <= lastglyph; s++ )
						if ( FontIDs[s] == f )
						{

							var e = s;
							while ( e < lastglyph && FontIDs[e + 1] == f )
								e++;

							paintspan( f, s, e, codepointstart );

							s = e;

						}

				}

			}

			void paintspan( int fontid, int s, int e, ReadOnlySpan<ushort> codepointstart2 )
			{


				var idx = (ReadDirection == FlowDirection.LeftToRight) ? s : StartPoints.Length - e - 2;

				var len = e - s + 1;

				// calculate paint locations
				for ( var i = 0; i < len; i++ )
				{
					var sp = StartPoints[idx + i];
					PaintPoints[i] = new SKPoint( deltax + sp.X, sp.Y + y );
				}

				ReadOnlySpan<ushort> pos = codepointstart2.Slice( idx, len );// + idx * 2;

				var paint = Paint;
				var font = Fonts[fontid];
				paint.Color = color;

				updatePaint?.Invoke( paint, false );

				using ( var builder = new SKTextBlobBuilder() )
				{
					builder.AddPositionedRun( pos, font, PaintPoints.AsSpan( 0, len ) );

					using ( var textBlob = builder.Build() )
					{
						canvas.DrawText( textBlob, 0, 0, paint );
					}
				}

				updatePaint?.Invoke( paint, true );
			}
		}

	}

}
