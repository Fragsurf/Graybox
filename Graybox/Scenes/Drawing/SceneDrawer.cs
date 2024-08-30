
using Graybox.Interface;
using SkiaSharp;
using System.Drawing;
using static NetVips.Enums;

namespace Graybox.Scenes.Drawing
{
	public class SceneDrawer
	{

		Scene Scene;

		public SceneDrawer( Scene scene )
		{
			Scene = scene;
		}

		static bool stippleFlip = false;
		public void DrawBox( Box box, Color color, float width = 1.0f, bool dashed = false, bool depthTest = true )
		{
			var prevLineWidth = GL.GetInteger( GetPName.LineWidth );
			GL.LineWidth( width );
			GL.ActiveTexture( TextureUnit.Texture0 );
			GL.BindTexture( TextureTarget.Texture2D, 0 );

			if ( dashed )
			{
				stippleFlip = !stippleFlip;
				GL.Enable( EnableCap.LineStipple );

				if ( stippleFlip )
				{
					GL.LineStipple( 4, 0xAAAA );
				}
				else
				{
					GL.LineStipple( 4, 0x5555 );
				}
			}

			var prevDepthTest = GL.GetBoolean( GetPName.DepthTest );
			if ( !depthTest ) GL.Disable( EnableCap.DepthTest );
			else GL.Enable( EnableCap.DepthTest );

			GL.Color3( color );
			GL.Begin( PrimitiveType.Lines );

			// Bottom face
			Coord( box.Start.X, box.Start.Y, box.Start.Z ); // Bottom-left front
			Coord( box.End.X, box.Start.Y, box.Start.Z );   // Bottom-right front

			Coord( box.End.X, box.Start.Y, box.Start.Z );   // Bottom-right front
			Coord( box.End.X, box.Start.Y, box.End.Z );     // Bottom-right back

			Coord( box.End.X, box.Start.Y, box.End.Z );     // Bottom-right back
			Coord( box.Start.X, box.Start.Y, box.End.Z );   // Bottom-left back

			Coord( box.Start.X, box.Start.Y, box.End.Z );   // Bottom-left back
			Coord( box.Start.X, box.Start.Y, box.Start.Z ); // Bottom-left front

			// Top face
			Coord( box.Start.X, box.End.Y, box.Start.Z );   // Top-left front
			Coord( box.End.X, box.End.Y, box.Start.Z );     // Top-right front

			Coord( box.End.X, box.End.Y, box.Start.Z );     // Top-right front
			Coord( box.End.X, box.End.Y, box.End.Z );       // Top-right back

			Coord( box.End.X, box.End.Y, box.End.Z );       // Top-right back
			Coord( box.Start.X, box.End.Y, box.End.Z );     // Top-left back

			Coord( box.Start.X, box.End.Y, box.End.Z );     // Top-left back
			Coord( box.Start.X, box.End.Y, box.Start.Z );   // Top-left front

			// Vertical lines
			Coord( box.Start.X, box.Start.Y, box.Start.Z ); // Bottom-left front
			Coord( box.Start.X, box.End.Y, box.Start.Z );   // Top-left front

			Coord( box.End.X, box.Start.Y, box.Start.Z );   // Bottom-right front
			Coord( box.End.X, box.End.Y, box.Start.Z );     // Top-right front

			Coord( box.End.X, box.Start.Y, box.End.Z );     // Bottom-right back
			Coord( box.End.X, box.End.Y, box.End.Z );       // Top-right back

			Coord( box.Start.X, box.Start.Y, box.End.Z );   // Bottom-left back
			Coord( box.Start.X, box.End.Y, box.End.Z );

			GL.End();
			GL.Disable( EnableCap.LineStipple );

			if ( prevDepthTest ) GL.Enable( EnableCap.DepthTest );
			else GL.Disable( EnableCap.DepthTest );

			GL.LineWidth( prevLineWidth );
		}

		public void DrawTranslateGizmo( Vector3 position, OpenTK.Mathematics.Quaternion rotation, float scale )
		{
			GL.PushMatrix();

			// Apply translation and rotation
			GL.Disable( EnableCap.DepthTest );
			GL.Translate( position );
			rotation.ToAxisAngle( out Vector3 axis, out float angle );
			GL.Rotate( MathHelper.RadiansToDegrees( angle ), axis.X, axis.Y, axis.Z );

			GL.LineWidth( 2.0f );

			// X Axis (Red)
			GL.Color3( 1.0f, 0.0f, 0.0f );
			GL.Begin( PrimitiveType.Lines );
			GL.Vertex3( 0.0f, 0.0f, 0.0f );
			GL.Vertex3( scale, 0.0f, 0.0f );
			GL.End();

			DrawCone( new Vector3( scale, 0.0f, 0.0f ), new Vector3( 1.0f, 0.0f, 0.0f ), 0.1f * scale, 0.2f * scale );

			// Y Axis (Green)
			GL.Color3( 0.0f, 1.0f, 0.0f );
			GL.Begin( PrimitiveType.Lines );
			GL.Vertex3( 0.0f, 0.0f, 0.0f );
			GL.Vertex3( 0.0f, scale, 0.0f );
			GL.End();

			DrawCone( new Vector3( 0.0f, scale, 0.0f ), new Vector3( 0.0f, 1.0f, 0.0f ), 0.1f * scale, 0.2f * scale );

			// Z Axis (Blue)
			GL.Color3( 0.0f, 0.0f, 1.0f );
			GL.Begin( PrimitiveType.Lines );
			GL.Vertex3( 0.0f, 0.0f, 0.0f );
			GL.Vertex3( 0.0f, 0.0f, scale );
			GL.End();

			DrawCone( new Vector3( 0.0f, 0.0f, scale ), new Vector3( 0.0f, 0.0f, 1.0f ), 0.1f * scale, 0.2f * scale );

			GL.PopMatrix();
			GL.Enable( EnableCap.DepthTest );
		}

		public void DrawCone( Vector3 baseCenter, Vector3 direction, float baseRadius, float height )
		{
			int numSegments = 16;
			Vector3[] circleVertices = new Vector3[numSegments];

			// Compute the circle vertices
			for ( int i = 0; i < numSegments; i++ )
			{
				float angle = (float)(i * 2 * Math.PI / numSegments);
				circleVertices[i] = new Vector3(
					baseRadius * (float)Math.Cos( angle ),
					baseRadius * (float)Math.Sin( angle ),
					0.0f );
			}

			// Apply rotation to align with the direction vector
			var q = OpenTK.Mathematics.Quaternion.FromAxisAngle( Vector3.Cross( Vector3.UnitZ, direction ), (float)Math.Acos( Vector3.Dot( Vector3.UnitZ, direction ) ) );
			for ( int i = 0; i < numSegments; i++ )
			{
				circleVertices[i] = Vector3.Transform( circleVertices[i], q );
			}

			// Draw the cone
			GL.Begin( PrimitiveType.TriangleFan );
			GL.Vertex3( baseCenter + direction * height ); // The apex of the cone
			for ( int i = 0; i <= numSegments; i++ )
			{
				GL.Vertex3( baseCenter + circleVertices[i % numSegments] );
			}
			GL.End();

			// Draw the bottom cap
			GL.Begin( PrimitiveType.TriangleFan );
			GL.Vertex3( baseCenter );
			for ( int i = numSegments; i >= 0; i-- )
			{
				GL.Vertex3( baseCenter + circleVertices[i % numSegments] );
			}
			GL.End();
		}

		public void PaintBoxSize( Box box, Color color, UIElementPaintEvent e, bool discardEmpty = false )
		{
			var xSize = Math.Round( Math.Abs( box.End.X - box.Start.X ) );
			var ySize = Math.Round( Math.Abs( box.End.Y - box.Start.Y ) );
			var zSize = Math.Round( Math.Abs( box.End.Z - box.Start.Z ) );

			var mins = box.Start;
			var maxs = box.End;
			var center = box.Center;
			var camPos = Scene.Camera.Position;

			var distance = Vector3.Distance( center, camPos );
			var scaleFactor = 800f / distance; // Adjust this factor based on your requirements

			if ( Scene.Camera.Orthographic )
				scaleFactor = 1.0f;

			scaleFactor = MathHelper.Clamp( scaleFactor, 0.45f, 1.35f );

			using ( var rectPaint = new SKPaint() )
			using ( var paint = new SKPaint() )
			{
				paint.IsAntialias = false;
				paint.TextSize = 16;
				paint.Color = new SKColor( color.R, color.G, color.B, color.A );

				rectPaint.IsAntialias = true;
				rectPaint.Color = new SKColor( 0, 0, 0, 155 );
				rectPaint.Style = SKPaintStyle.Fill;

				if ( Scene.Camera.Orthographic )
				{
					var min = Scene.WorldToScreen( mins );
					var max = Scene.WorldToScreen( maxs );

					var minX = Math.Min( min.X, max.X );
					var maxX = Math.Max( min.X, max.X );
					var minY = Math.Min( min.Y, max.Y );
					var maxY = Math.Max( min.Y, max.Y );

					var xpos = new SKPoint( (minX + maxX) * 0.5f, maxY + 24 );
					var ypos = new SKPoint( minX - 32, (minY + maxY) * 0.5f );

					void DrawTextWithBackground( SKCanvas canvas, string text, SKPoint pos )
					{
						canvas.Save();
						canvas.Translate( pos.X, pos.Y );
						canvas.Scale( scaleFactor );
						canvas.Translate( -pos.X, -pos.Y );

						var textBounds = new SKRect();
						paint.MeasureText( text, ref textBounds );
						var rect = new SKRect( pos.X + textBounds.Left - 4, pos.Y + textBounds.Top - 4,
											  pos.X + textBounds.Right + 4, pos.Y + textBounds.Bottom + 4 );
						canvas.DrawRoundRect( rect, 4, 4, rectPaint );
						canvas.DrawText( text, pos, paint );

						canvas.Restore();
					}

					if ( Scene.Camera.Forward.AlmostEqual( -Vector3.UnitX ) )
					{
						if ( (!discardEmpty || ySize != 0) ) DrawTextWithBackground( e.Canvas, $"{ySize}", xpos );
						if ( (!discardEmpty || zSize != 0) ) DrawTextWithBackground( e.Canvas, $"{zSize}", ypos );
					}
					else if ( Scene.Camera.Forward.AlmostEqual( -Vector3.UnitY ) )
					{
						if ( (!discardEmpty || xSize != 0) ) DrawTextWithBackground( e.Canvas, $"{xSize}", xpos );
						if ( (!discardEmpty || ySize != 0) ) DrawTextWithBackground( e.Canvas, $"{zSize}", ypos );
					}
					else
					{
						if ( (!discardEmpty || xSize != 0) ) DrawTextWithBackground( e.Canvas, $"{xSize}", ypos );
						if ( (!discardEmpty || ySize != 0) ) DrawTextWithBackground( e.Canvas, $"{ySize}", xpos );
					}
				}
				else
				{
					var xCenter = center;
					xCenter.Y = mins.Y;
					xCenter.Z = mins.Z;

					var yCenter = center;
					yCenter.X = mins.X;
					yCenter.Z = mins.Z;

					var zCenter = maxs;
					zCenter.Z -= (float)box.Dimensions.Z * 0.5f;

					var xpos = Scene.WorldToScreen( xCenter );
					var ypos = Scene.WorldToScreen( yCenter );
					var zpos = Scene.WorldToScreen( zCenter );

					void DrawTextWithBackground( SKCanvas canvas, string text, SKPoint pos )
					{
						canvas.Save();
						canvas.Translate( pos.X, pos.Y );
						canvas.Scale( scaleFactor );
						canvas.Translate( -pos.X, -pos.Y );

						var textBounds = new SKRect();
						paint.MeasureText( text, ref textBounds );
						var rect = new SKRect( pos.X + textBounds.Left - 4, pos.Y + textBounds.Top - 4,
											  pos.X + textBounds.Right + 4, pos.Y + textBounds.Bottom + 4 );
						canvas.DrawRoundRect( rect, 4, 4, rectPaint );
						canvas.DrawText( text, pos, paint );

						canvas.Restore();
					}

					if ( (!discardEmpty || xSize != 0) && xpos.Z > 0 )
					{
						DrawTextWithBackground( e.Canvas, $"{xSize}", new SKPoint( xpos.X, xpos.Y ) );
					}

					if ( (!discardEmpty || ySize != 0) && ypos.Z > 0 )
					{
						DrawTextWithBackground( e.Canvas, $"{ySize}", new SKPoint( ypos.X, ypos.Y ) );
					}

					if ( (!discardEmpty || zSize != 0) && zpos.Z > 0 )
					{
						DrawTextWithBackground( e.Canvas, $"{zSize}", new SKPoint( zpos.X, zpos.Y ) );
					}
				}
			}
		}

		public void DrawTextWithBackground( SKCanvas canvas, string text, Vector2 position, Color textColor, Color backgroundColor )
		{
			using var paint = new SKPaint();
			paint.Color = new SKColor( textColor.R, textColor.G, textColor.B, textColor.A );
			paint.TextSize = 16;

			using var rectPaint = new SKPaint();
			rectPaint.Color = new SKColor( backgroundColor.R, backgroundColor.G, backgroundColor.B, backgroundColor.A );

			//canvas.Save();
			//canvas.Translate( position.X, position.Y );
			//canvas.Translate( -position.X, -position.Y );

			var textBounds = new SKRect();
			paint.MeasureText( text, ref textBounds );
			var rect = new SKRect( position.X + textBounds.Left - 4, position.Y + textBounds.Top - 4,
								  position.X + textBounds.Right + 4, position.Y + textBounds.Bottom + 4 );
			canvas.DrawRoundRect( rect, 4, 4, rectPaint );
			canvas.DrawText( text, position.X, position.Y, paint );

			//canvas.Restore();
		}

		void Coord( float x, float y, float z ) => GL.Vertex3( x, y, z );

	}
}
