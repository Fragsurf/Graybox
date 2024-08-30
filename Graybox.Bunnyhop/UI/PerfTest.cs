using Graybox.Interface;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Graybox.Bunnyhop.UI;

internal class PerfTest : UIElement
{
	private Random rand = new Random();
	private SKPaint paint = new SKPaint();
	private List<Shape> shapes = new List<Shape>();

	public PerfTest()
	{
		Width = Length.Percent( 100 );
		Height = Length.Percent( 100 );
		Position = PositionType.Absolute;
		Top = 0;
		Left = 0;

		paint.IsAntialias = true;
		paint.Style = SKPaintStyle.Fill;

		InitializeShapes( 2000 );
	}

	private void InitializeShapes( int count )
	{
		for ( int i = 0; i < count; i++ )
		{
			// Ensure the velocity is never zero and varies more randomly
			int vx = 0;
			int vy = 0;
			while ( vx == 0 && vy == 0 ) // Avoid stationary shapes
			{
				vx = rand.Next( -3, 4 ); // Expanded range and can be negative, zero, or positive
				vy = rand.Next( -3, 4 );
			}

			shapes.Add( new Shape
			{
				IsRectangle = rand.Next( 2 ) == 0,
				X = rand.Next( (int)BoxRect.Width ),
				Y = rand.Next( (int)BoxRect.Height ),
				Size = rand.Next( 20, 100 ),
				Color = new SKColor( (byte)rand.Next( 256 ), (byte)rand.Next( 256 ), (byte)rand.Next( 256 ), 255 ),
				VelocityX = vx,
				VelocityY = vy
			} );
		}
	}

	protected override void Update()
	{
		base.Update();

		foreach ( var shape in shapes )
		{
			// Update position based on velocity
			shape.X += shape.VelocityX;
			shape.Y += shape.VelocityY;

			// Reverse direction if hitting bounds
			if ( shape.X <= 0 || shape.X + shape.Size >= BoxRect.Width )
			{
				shape.VelocityX = -shape.VelocityX;
				shape.X = Math.Max( 0, Math.Min( shape.X, (int)BoxRect.Width - shape.Size ) );
			}
			if ( shape.Y <= 0 || shape.Y + shape.Size >= BoxRect.Height )
			{
				shape.VelocityY = -shape.VelocityY;
				shape.Y = Math.Max( 0, Math.Min( shape.Y, (int)BoxRect.Height - shape.Size ) );
			}
		}
	}

	protected override void Paint( SKCanvas canvas )
	{
		base.Paint( canvas );

		foreach ( var shape in shapes )
		{
			paint.Color = shape.Color;

			// Draw shape
			if ( shape.IsRectangle )
			{
				canvas.DrawRect( shape.X, shape.Y, shape.Size, shape.Size, paint );
			}
			else
			{
				canvas.DrawCircle( shape.X + shape.Size / 2, shape.Y + shape.Size / 2, shape.Size / 2, paint );
			}
		}
	}

	private class Shape
	{
		public bool IsRectangle { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int Size { get; set; }
		public SKColor Color { get; set; }
		public int VelocityX { get; set; }
		public int VelocityY { get; set; }
	}
}
