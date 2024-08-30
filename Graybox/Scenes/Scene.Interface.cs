
using SkiaSharp;
using System;

namespace Graybox.Scenes
{
	public partial class Scene
	{

		GRContext interfaceGrContext;
		SKSurface interfaceSurface;
		GRBackendRenderTarget interfaceSkRenderTarget;
		int interfaceFbo;
		int interfaceTexture;
		int interfaceLastWidth;
		int interfaceLastHeight;

		void RenderInterface()
		{
			EnsureInterface();

			if ( interfaceSkRenderTarget == null ) return;

			Interface.Width = Width;
			Interface.Height = Height;

			Interface.MarkLayoutDirty();
			Interface.HandleUpdate();
			interfaceSurface.Canvas.Clear( SKColors.Transparent );
			Interface.PaintCanvas( interfaceSurface.Canvas );

			interfaceGrContext.ResetContext( GRGlBackendState.All );
			interfaceSurface.Canvas.Flush();
		}

		void EnsureInterface()
		{
			if ( Interface == null )
			{
				DestroyInterface();
				return;
			}

			if ( interfaceLastWidth != Width || interfaceLastHeight != Height )
			{
				DestroyInterface();

				interfaceLastWidth = Width;
				interfaceLastHeight = Height;

				var surfaceWidth = Width;
				var surfaceHeight = Height;
				var prevFramebuffer = GL.GetInteger( GetPName.FramebufferBinding );
				var prevTexture = GL.GetInteger( GetPName.TextureBinding2D );

				GL.CreateFramebuffers( 1, out interfaceFbo );
				GL.CreateTextures( TextureTarget.Texture2D, 1, out interfaceTexture );

				GL.BindFramebuffer( FramebufferTarget.Framebuffer, interfaceFbo );
				GL.BindTexture( TextureTarget.Texture2D, interfaceTexture );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
				GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, surfaceWidth, surfaceHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero );
				GL.FramebufferTexture2D( FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, interfaceTexture, 0 );

				if ( interfaceGrContext == null )
				{
					GRGlInterface backendContext = GRGlInterface.Create();
					interfaceGrContext = GRContext.CreateGl( backendContext );
				}

				var info = new GRGlFramebufferInfo( (uint)interfaceFbo, SKColorType.Rgba8888.ToGlSizedFormat() );
				interfaceSkRenderTarget = new GRBackendRenderTarget( surfaceWidth, surfaceHeight, 0, 8, info );
				interfaceSurface = SKSurface.Create( interfaceGrContext, interfaceSkRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888 );

				GL.BindFramebuffer( FramebufferTarget.Framebuffer, prevFramebuffer );
				GL.BindTexture( TextureTarget.Texture2D, prevTexture );
			}
		}

		void DestroyInterface()
		{
			interfaceSurface?.Dispose();
			interfaceSurface = null;
			//interfaceGrContext?.Dispose();
			//interfaceGrContext = null;
			interfaceSkRenderTarget?.Dispose();
			interfaceSkRenderTarget = null;
			interfaceLastWidth = 0;
			interfaceLastHeight = 0;

			if ( interfaceFbo != 0 )
			{
				GL.DeleteFramebuffer( interfaceFbo );
				interfaceFbo = 0;
			}

			if ( interfaceTexture != 0 )
			{
				GL.DeleteTexture( interfaceTexture );
				interfaceTexture = 0;
			}
		}

	}
}
