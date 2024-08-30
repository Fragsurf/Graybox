
using Graybox.Fuck.RectPacker;

namespace Graybox;

public struct Rect
{

	public float X { get; private set; }
	public float Y { get; private set; }
	public float Width { get; private set; }
	public float Height { get; private set; }

	public Rect( float x, float y, float width, float height )
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public float Left => X;
	public float Right => X + Width;
	public float Top => Y;
	public float Bottom => Y + Height;
	public float Area => Width * Height;

	public bool Contains( float x, float y )
	{
		return (x >= Left && x <= Right && y >= Top && y <= Bottom);
	}

	public bool Intersects( Rect other )
	{
		return (Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top);
	}

	public void Move( float deltaX, float deltaY )
	{
		X += deltaX;
		Y += deltaY;
	}

	public void Resize( float newWidth, float newHeight )
	{
		Width = newWidth;
		Height = newHeight;
	}

	public void Expand( float amount )
	{
		// Expand width and height by the amount (can be negative for shrinking)
		Width += 2 * amount;
		Height += 2 * amount;

		// Move the top-left corner to keep the expansion centered
		X -= amount;
		Y -= amount;
	}

	public override string ToString()
	{
		return $"Rect({X}, {Y}, {Width}, {Height})";
	}

}

public static class RectPacker
{
	public static (List<Rect> packedRects, Vector2 maxSize) Pack( Rect container, IEnumerable<Rect> items, float margin = 0f )
	{
		// First pack without margins to get the initial arrangement
		var initialRectangles = items.Select( ( item, index ) =>
			new PackingRectangle(
				(uint)MathF.Round( item.X ),
				(uint)MathF.Round( item.Y ),
				(uint)MathF.Round( item.Width ),
				(uint)MathF.Round( item.Height ),
				index ) )
			.ToArray();
		RectanglePacker.Pack( initialRectangles, out var initialBounds );

		// Grow rectangles by adding margin
		var grownRectangles = initialRectangles.Select( r =>
			new PackingRectangle(
				r.X,
				r.Y,
				r.Width + 2 * (uint)MathF.Round( margin ),
				r.Height + 2 * (uint)MathF.Round( margin ),
				r.Id ) )
			.ToArray();

		// Repack the grown rectangles
		RectanglePacker.Pack( grownRectangles, out var grownBounds );

		// Calculate scale factor based on the container size and grown bounds
		float scale = Math.Min( Math.Min( container.Width / grownBounds.Width, container.Height / grownBounds.Height ), 1 );

		// Calculate the maximum needed size
		Vector2 maxSize = new Vector2(
			MathF.Ceiling( grownBounds.Width * scale ),
			MathF.Ceiling( grownBounds.Height * scale )
		);

		// Create scaled rectangles and subtract margins
		var packedRects = grownRectangles
			.OrderBy( r => r.Id )
			.Select( r => new Rect(
				MathF.Round( r.X * scale + container.X + margin / 2 ),
				MathF.Round( r.Y * scale + container.Y + margin / 2 ),
				MathF.Round( (r.Width - 2 * (uint)MathF.Round( margin )) * scale ),
				MathF.Round( (r.Height - 2 * (uint)MathF.Round( margin )) * scale ) ) )
			.ToList();

		return (packedRects, maxSize);
	}
}
