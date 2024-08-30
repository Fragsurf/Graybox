
using OpenTK.Graphics.OpenGL;
using System;

namespace Graybox.Graphics.Helpers
{
	internal struct ScopeFramebuffer : IDisposable
	{

		int oldFramebuffer;

		public ScopeFramebuffer( int newBuffer )
		{
			GL.GetInteger( GetPName.FramebufferBinding, out oldFramebuffer );
			GL.BindFramebuffer( FramebufferTarget.Framebuffer, newBuffer );
		}

		public void Dispose()
		{
			GL.BindFramebuffer( FramebufferTarget.Framebuffer, oldFramebuffer );
		}

	}
}
