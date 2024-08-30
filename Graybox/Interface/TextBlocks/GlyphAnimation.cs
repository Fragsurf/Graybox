
using System;
using SkiaSharp;

namespace Graybox.Interface.TextBlocks
{
	public class GlyphAnimation
	{

		// glyph, glyphcount, original, transposed
		public Func<int, int, SKPoint, SKPoint> Transpose;
		public Func<int, int, SKColor> GetColor;
		public Action<int, int, SKPaint, bool> UpdatePaint;

	}
}
