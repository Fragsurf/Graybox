using OpenTK.Graphics.OpenGL;
using SkiaSharp;
using System;
using System.Drawing;
using System.IO;

namespace Graybox.Graphics.Helpers
{
	public class GraphicsHelper
	{

		[System.Diagnostics.Conditional( "DEBUG" )]
		public static void SpamGLErrors( string context = "Unknown", int msDelay = 100 )
		{
			var err = GL.GetError();
			while ( err != ErrorCode.NoError )
			{
				System.Diagnostics.Debug.WriteLine( context + ": " + err );
				err = GL.GetError();
				System.Threading.Thread.Sleep( msDelay );
			}
		}

		/// <summary>
		/// Captures the current OpenGL framebuffer content and saves it as a PNG image.
		/// </summary>
		/// <param name="width">Width of the framebuffer.</param>
		/// <param name="height">Height of the framebuffer.</param>
		/// <param name="filePath">File path to save the PNG image.</param>
		public static void SaveFramebufferToPng( int width, int height, string filePath )
		{
			// Read pixels from the OpenGL framebuffer
			byte[] pixelData = new byte[width * height * 4];  // Assuming 4 bytes per pixel (RGBA)
			GL.ReadPixels( 0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData );

			// Use SkiaSharp to create a bitmap and save it
			using ( var bitmap = new SKBitmap( width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul ) )
			{
				unsafe
				{
					fixed ( byte* p = pixelData )
					{
						// Copy the data into the SKBitmap
						IntPtr ptr = new IntPtr( p );
						SKImageInfo info = new SKImageInfo( width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul );
						bitmap.InstallPixels( info, ptr, width * 4 );

						// Flip the bitmap vertically
						using ( var canvas = new SKCanvas( bitmap ) )
						{
							using ( var paint = new SKPaint() )
							{
								paint.BlendMode = SKBlendMode.Src;
								canvas.Scale( 1, -1, 0, height / 2f );  // Set the scale to flip vertically
								canvas.DrawBitmap( bitmap.Copy(), 0, 0, paint );
							}
						}
					}
				}

				// Save the bitmap to file
				using ( var image = SKImage.FromBitmap( bitmap ) )
				using ( var data = image.Encode( SKEncodedImageFormat.Png, 100 ) )
				{
					// Ensure directory exists
					string directory = Path.GetDirectoryName( filePath );
					if ( !Directory.Exists( directory ) )
					{
						Directory.CreateDirectory( directory );
					}

					using ( var stream = new FileStream( filePath, FileMode.Create, FileAccess.Write ) )
					{
						data.SaveTo( stream );
					}
				}
			}
		}

		public static void InitGL3D()
		{
			GL.ClearColor( Color.Black );
			GL.Hint( HintTarget.PerspectiveCorrectionHint, HintMode.Nicest );
			GL.Enable( EnableCap.DepthTest );
			GL.DepthFunc( DepthFunction.Lequal );
			GL.Enable( EnableCap.CullFace );
			GL.CullFace( CullFaceMode.Front );
			GL.PolygonMode( MaterialFace.FrontAndBack, PolygonMode.Fill );
			GL.Enable( EnableCap.Texture2D );
			GL.Enable( EnableCap.Blend );
			GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
			GL.PixelStore( PixelStoreParameter.UnpackAlignment, 1 );
		}

		public static void DrawTexturedQuad( int textureId, Vector3 position, Vector3 size, Vector3 forward, Color4 color = default )
		{
			if ( color == default )
			{
				color = Color4.White;
			}

			// Save current GL state
			bool depthTestWasEnabled = GL.IsEnabled( EnableCap.DepthTest );
			bool blendWasEnabled = GL.IsEnabled( EnableCap.Blend );

			// Enable blending
			GL.Enable( EnableCap.Blend );
			GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );

			// Disable depth writing, but keep depth testing
			GL.DepthMask( false );
			GL.Enable( EnableCap.DepthTest );

			var crossvec = Vector3.UnitZ;
			var right = Vector3.Cross( forward, crossvec ).Normalized();
			var up = Vector3.Cross( forward, right );
			Vector3 halfWidth = right * size.Y * 0.5f;
			Vector3 halfHeight = up * size.Z * 0.5f;
			Vector3 topLeft = position - halfWidth + halfHeight;
			Vector3 topRight = position + halfWidth + halfHeight;
			Vector3 bottomRight = position + halfWidth - halfHeight;
			Vector3 bottomLeft = position - halfWidth - halfHeight;

			GL.Enable( EnableCap.Texture2D );
			GL.ActiveTexture( TextureUnit.Texture0 );
			GL.BindTexture( TextureTarget.Texture2D, textureId );
			GL.Color4( color );

			GL.Begin( PrimitiveType.Quads );
			GL.TexCoord2( 0, 1 );
			GL.Vertex3( topLeft );
			GL.TexCoord2( 1, 1 );
			GL.Vertex3( topRight );
			GL.TexCoord2( 1, 0 );
			GL.Vertex3( bottomRight );
			GL.TexCoord2( 0, 0 );
			GL.Vertex3( bottomLeft );
			GL.End();

			GL.BindTexture( TextureTarget.Texture2D, 0 );

			// Restore previous GL state
			GL.DepthMask( true );
			if ( !depthTestWasEnabled ) GL.Disable( EnableCap.DepthTest );
			if ( !blendWasEnabled ) GL.Disable( EnableCap.Blend );
		}

	}
}
