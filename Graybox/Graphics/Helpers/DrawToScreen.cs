
namespace Graybox.Graphics.Helpers
{
	public struct DrawToScreen : IDisposable
	{

		int width;
		int height;

		public DrawToScreen( int width, int height )
		{
			this.width = width;
			this.height = height;
			Begin2D();
		}

		public void Dispose()
		{
			End2D();
		}

		public void DrawTexturedQuad( int textureId, int width, int height )
		{
			GL.ActiveTexture( TextureUnit.Texture0 );
			GL.BindTexture( TextureTarget.Texture2D, textureId );
			GL.Begin( PrimitiveType.Quads );
			GL.TexCoord2( 0, 1 );
			GL.Vertex2( 0, height );
			GL.TexCoord2( 1, 1 );
			GL.Vertex2( width, height );
			GL.TexCoord2( 1, 0 );
			GL.Vertex2( width, 0 );
			GL.TexCoord2( 0, 0 );
			GL.Vertex2( 0, 0 );
			GL.End();
			GL.BindTexture( TextureTarget.Texture2D, 0 );
		}

		void Begin2D()
		{
			GL.MatrixMode( MatrixMode.Projection );
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho( 0, width, 0, height, 0, 1 );
			GL.MatrixMode( MatrixMode.Modelview );
			GL.PushMatrix();
			GL.LoadIdentity();

			GL.Enable( EnableCap.Blend );
			GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
			GL.Disable( EnableCap.CullFace );
			GL.Disable( EnableCap.DepthTest );
			GL.Enable( EnableCap.Texture2D );
			GL.Color3( 1f, 1f, 1f );
		}

		void End2D()
		{
			GL.BindTexture( TextureTarget.Texture2D, 0 );
			GL.Disable( EnableCap.Texture2D );
			GL.Enable( EnableCap.CullFace );
			GL.Enable( EnableCap.DepthTest );

			GL.MatrixMode( MatrixMode.Modelview );
			GL.PopMatrix();
			GL.MatrixMode( MatrixMode.Projection );
			GL.PopMatrix();
		}

	}
}
