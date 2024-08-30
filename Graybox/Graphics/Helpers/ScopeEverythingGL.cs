
using System;

namespace Graybox.Graphics.Helpers
{
	internal struct ScopeEverythingGL : IDisposable
	{
		int prevProgram;
		int prevTextureBinding;
		int prevFramebuffer;
		int prevVertexArray;
		int prevElementBuffer;
		Vector4 prevViewport;
		bool prevDepthTest;
		bool prevBlend;
		int activeTextureUnit;
		bool prevScissorTest;
		int[] prevScissorBox = new int[4];
		int[] viewport = new int[4];
		bool prevStencilTest;
		int prevStencilMask;
		Vector4 prevColorMask;
		float prevLineWidth;

		public ScopeEverythingGL()
		{
			GL.PushAttrib( AttribMask.AllAttribBits );
			GL.PushClientAttrib( ClientAttribMask.ClientAllAttribBits );

			// Program and Bindings
			GL.GetInteger( GetPName.CurrentProgram, out prevProgram );
			GL.GetInteger( GetPName.TextureBinding2D, out prevTextureBinding );
			GL.GetInteger( GetPName.FramebufferBinding, out prevFramebuffer );
			GL.GetInteger( GetPName.VertexArrayBinding, out prevVertexArray );
			GL.GetInteger( GetPName.ElementArrayBufferBinding, out prevElementBuffer );

			// Viewport
			GL.GetInteger( GetPName.Viewport, viewport );
			prevViewport = new Vector4( viewport[0], viewport[1], viewport[2], viewport[3] );

			// Textures
			GL.GetInteger( GetPName.ActiveTexture, out activeTextureUnit );

			// Capabilities
			prevDepthTest = GL.IsEnabled( EnableCap.DepthTest );
			prevBlend = GL.IsEnabled( EnableCap.Blend );
			prevScissorTest = GL.IsEnabled( EnableCap.ScissorTest );
			prevStencilTest = GL.IsEnabled( EnableCap.StencilTest );

			// Scissor Box
			GL.GetInteger( GetPName.ScissorBox, prevScissorBox );

			// Stencil
			GL.GetInteger( GetPName.StencilWritemask, out prevStencilMask );

			// Color Mask
			int[] colorMask = new int[4];
			GL.GetInteger( GetPName.ColorWritemask, colorMask );
			prevColorMask = new Vector4( colorMask[0], colorMask[1], colorMask[2], colorMask[3] );

			// Line Width
			GL.GetFloat( GetPName.LineWidth, out prevLineWidth );
		}

		public void Dispose()
		{
			GL.ActiveTexture( (TextureUnit)activeTextureUnit );
			GL.BindTexture( TextureTarget.Texture2D, prevTextureBinding );
			GL.UseProgram( prevProgram );
			GL.BindFramebuffer( FramebufferTarget.Framebuffer, prevFramebuffer );
			GL.BindVertexArray( prevVertexArray );
			GL.BindBuffer( BufferTarget.ElementArrayBuffer, prevElementBuffer );
			GL.Viewport( (int)prevViewport.X, (int)prevViewport.Y, (int)prevViewport.Z, (int)prevViewport.W );
			GL.Scissor( prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3] );
			SetState( EnableCap.DepthTest, prevDepthTest );
			SetState( EnableCap.Blend, prevBlend );
			SetState( EnableCap.ScissorTest, prevScissorTest );
			SetState( EnableCap.StencilTest, prevStencilTest );
			GL.StencilMask( prevStencilMask );
			GL.ColorMask( prevColorMask.X == 1, prevColorMask.Y == 1, prevColorMask.Z == 1, prevColorMask.W == 1 );
			GL.LineWidth( prevLineWidth );
			GL.PopClientAttrib();
			GL.PopAttrib();
		}

		private void SetState( EnableCap cap, bool enabled )
		{
			if ( enabled )
				GL.Enable( cap );
			else
				GL.Disable( cap );
		}
	}


}
