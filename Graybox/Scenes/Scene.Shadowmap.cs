
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics.Helpers;
using Graybox.Scenes.Shaders;
using Graybox.Utility;

namespace Graybox.Scenes
{
	public partial class Scene
	{

		public bool SunEnabled { get; set; } = false;
		public int ShadowDistance { get; set; } = 7000;
		public int ShadowResolution { get; set; } = 4096;
		public Vector3 SunColor { get; set; } = new Vector3( 1, 1, 1 );
		public Vector3 SunDirection { get; set; } = new Vector3( -1000, -1000, -2500 ).Normalized();
		public float SunShadowIntensity { get; set; } = 0.35f;
		public Matrix4 SunLightSpaceMatrix { get; private set; }

		ShaderProgram _depthShader;
		int sunDepthTexture = 0;
		int sunFramebuffer = 0;

		public int SunDepthTexture => sunDepthTexture;

		void LoadFromEntity()
		{
			foreach ( var obj in Objects )
			{
				if ( obj is not Light l ) continue;
				if ( l.LightType != Lightmapper.LightTypes.Directional ) continue;

				SunColor = new( l.LightInfo.Color.R, l.LightInfo.Color.G, l.LightInfo.Color.B );
				SunDirection = l.Direction.EulerToForward().Normalized();
				SunShadowIntensity = l.LightInfo.ShadowStrength;
				break;
			}
		}

		void RenderShadowmap()
		{
			LoadFromEntity();

			if ( !SunEnabled )
			{
				DestroyShadowmap();
				return;
			}

			if ( sunDepthTexture == 0 || sunFramebuffer == 0 || _depthShader == null )
			{
				DestroyShadowmap();
				InitializeShadowmap();
			}

			if ( sunFramebuffer == 0 )
				return;

			SunDirection = SunDirection.Normalized();

			GL.BindFramebuffer( FramebufferTarget.Framebuffer, sunFramebuffer );
			GL.Clear( ClearBufferMask.DepthBufferBit );
			GL.Viewport( 0, 0, ShadowResolution, ShadowResolution );

			var lightPos = Camera.Position - SunDirection * (ShadowDistance * .5f);
			var upVector = Math.Abs( Vector3.Dot( SunDirection, Vector3.UnitZ ) ) < 0.999f ? Vector3.UnitZ : Vector3.UnitX;
			var view = Matrix4.LookAt( lightPos, lightPos + SunDirection, upVector );
			var left = -ShadowDistance;
			var right = ShadowDistance;
			var bottom = -ShadowDistance;
			var top = ShadowDistance;
			var near = 0.1f;
			var far = ShadowDistance * 2;
			var projection = Matrix4.CreateOrthographicOffCenter( left, right, bottom, top, near, far );

			SunLightSpaceMatrix = view * projection;

			GL.Disable( EnableCap.CullFace );

			_depthShader.Bind();
			_depthShader.SetUniform( ShaderConstants.CameraProjection, projection );
			_depthShader.SetUniform( ShaderConstants.CameraView, view );
			_solidRenderer.DrawArrays();
			_depthShader.Unbind();

			GL.Enable( EnableCap.CullFace );
			GL.Viewport( 0, 0, Width, Height );
		}

		void InitializeShadowmap()
		{
			if ( sunFramebuffer != 0 || sunDepthTexture != 0 || _depthShader != null )
			{
				throw new Exception( "Shadowmap buffer already initialized" );
			}

			sunFramebuffer = GL.GenFramebuffer();

			using ( var _ = new ScopeFramebuffer( sunFramebuffer ) )
			{
				sunDepthTexture = GL.GenTexture();
				GL.BindTexture( TextureTarget.Texture2D, sunDepthTexture );
				GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24,
							  ShadowResolution, ShadowResolution, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero );

				// Set texture parameters
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f } );

				// Attach texture to the framebuffer
				GL.FramebufferTexture( FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, sunDepthTexture, 0 );

				// Check framebuffer status
				if ( GL.CheckFramebufferStatus( FramebufferTarget.Framebuffer ) != FramebufferErrorCode.FramebufferComplete )
				{
					throw new Exception( "Shadowmap framebuffer not complete!" );
				}

				_depthShader = new ShaderProgram( ShaderSource.VertDepth, ShaderSource.FragDepth );
			}
		}

		void DestroyShadowmap()
		{
			if ( sunDepthTexture != 0 )
			{
				GL.DeleteTexture( sunDepthTexture );
				sunDepthTexture = 0;
			}

			if ( sunFramebuffer != 0 )
			{
				GL.DeleteFramebuffer( sunFramebuffer );
				sunFramebuffer = 0;
			}

			_depthShader?.Dispose();
			_depthShader = null;
		}

	}
}
