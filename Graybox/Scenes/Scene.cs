
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics;
using Graybox.Graphics.Helpers;
using Graybox.Interface;
using Graybox.Lightmapper;
using Graybox.Scenes.Drawing;
using Graybox.Scenes.Physics;
using Graybox.Scenes.Shaders;
using SkiaSharp;
using System.Drawing;

namespace Graybox.Scenes
{
	public partial class Scene : IDisposable
	{

		public string Name { get; set; }
		public SceneCamera Camera { get; set; } = new SceneCamera();
		public int Width { get; private set; } = 1280;
		public int Height { get; private set; } = 720;
		public int MSAA { get; private set; } = 0;
		public IReadOnlyList<MapObject> Objects => _objects;
		public bool Wireframe { get; set; }
		public bool GridEnabled { get; set; }
		public bool GridSnap { get; set; }
		public AssetSystem AssetSystem { get; set; }
		public LightmapData Lightmaps { get; set; }
		public EnvironmentData Environment { get; set; } = new();
		public LightmapBaker LightBaker => Lightmaps?.Baker;

		float gridSize = 32;
		public float GridSize
		{
			get => gridSize;
			set => gridSize = MathHelper.Clamp( value, 1, 1024 );
		}
		public bool ShowGizmos
		{
			get => Gizmos.Enabled;
			set => Gizmos.Enabled = value;
		}

		public bool ShowFPS { get; set; } = false;
		public bool DebugVisualizeTexels { get; set; }

		public SceneDrawer Draw { get; }
		public SceneGizmos Gizmos { get; }
		public IPhysicsScene Physics { get; }
		public UIElement Interface { get; }

		public Action BeforeRender;
		public Action AfterRender;

		int _framebuffer = -1;
		int _msFramebuffer = -1;
		int _sceneTexture = -1;
		int _depthBuffer = -1;
		int _msColorbuffer = -1;
		bool _initialized;
		int _renderHash;

		bool _rebuildShaders;
		List<MapObject> _objects = new List<MapObject>();
		ShaderProgram _gridShader;
		ShaderProgram _texelShader;
		//ImGuiController _imgui;


		SolidRenderer _solidRenderer;
		GridRenderer _gridRenderer;
		SolidWireframeRenderer _solidWireframeRenderer;

		public int TextureId => _sceneTexture;
		public int FramebufferId => _framebuffer;

		const int MapSize = 16384 * 2;

		public Scene()
		{
			InitializeGL();
			Configure( 256, 256 );
			RebuildShaders();
			Physics = new Jitter2Scene( this );
			Draw = new SceneDrawer( this );
			Gizmos = new SceneGizmos( this );
			_solidRenderer = new( this );
			_solidWireframeRenderer = new( this );
			_gridRenderer = new( this );
			_gridRenderer.Add( new GridSettings()
			{
				Forward = Vector3.UnitZ,
				Size = 16384,
				Origin = Vector3.Zero,
				Spacing = 32
			} );

			Interface = new()
			{
				IsRoot = true,
				Top = 0,
				Left = 0,
				BackgroundColor = SkiaSharp.SKColors.Transparent
			};
			Interface.AfterPaint += PaintFPS;

			//_imgui = new( 1280, 720 );

			HotloadHelper.OnHotload += HotloadManager_OnHotload;
			EventSystem.Subscribe( this );
		}

		private void HotloadManager_OnHotload()
		{
			_rebuildShaders = true;
		}

		void PaintFPS( UIElementPaintEvent e )
		{
			if ( !ShowFPS ) return;
			if ( frameTimes.Count <= 0 ) return;

			using var paint = new SKPaint();
			paint.TextSize = 20;
			paint.Color = SKColors.White;

			using var backgroundPaint = new SKPaint();
			backgroundPaint.Color = SKColors.Black;

			var averageFrameTime = 0f;
			foreach ( var frameTime in frameTimes )
				averageFrameTime += frameTime;

			averageFrameTime /= frameTimes.Count;
			var averageFPS = 1.0f / averageFrameTime;

			var fpsText = $"FPS {Name}: {(int)averageFPS}";
			var textWidth = paint.MeasureText( fpsText );
			var textBounds = new SKRect();
			paint.MeasureText( fpsText, ref textBounds );

			var padding = 4;
			var x = 5;
			var y = 52;

			e.Canvas.DrawRect( new SKRect( x - padding, y - textBounds.Height - padding, x + textWidth + padding, y + padding ), backgroundPaint );
			e.Canvas.DrawText( fpsText, x, y, paint );
		}

		private List<float> frameTimes = new List<float>( MaxFrames );
		private const int MaxFrames = 33;
		int lastGridHash;
		public void HandleUpdate( FrameInfo frameinfo )
		{
			int gridHash = HashCode.Combine( GridEnabled, GridSize, Camera.Orthographic ? Camera.Forward : default );

			if ( gridHash != lastGridHash )
			{
				lastGridHash = gridHash;
				UpdateGrid();
			}

			if ( ShowFPS )
			{
				frameTimes.Add( frameinfo.DeltaTime );
				if ( frameTimes.Count > MaxFrames )
					frameTimes.RemoveAt( 0 );
			}
		}

		public void Clear()
		{
			if ( !_initialized ) return;

			var cc = Environment?.SkyColor ?? new( 0.2f, 0.2f, 0.2f, 1.0f );
			cc.A = 1.0f;

			GL.ClearColor( cc );
			GL.BindFramebuffer( FramebufferTarget.Framebuffer, _msFramebuffer );
			GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );
			GL.BindFramebuffer( FramebufferTarget.Framebuffer, _framebuffer );
			GL.Clear( ClearBufferMask.ColorBufferBit );
			GL.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
		}

		void RebuildShaders()
		{
			_gridShader?.Dispose();
			_texelShader?.Dispose();
			_gridShader = new ShaderProgram( ShaderSource.VertexGrid, ShaderSource.FragmentGrid );
			_texelShader = new ShaderProgram( ShaderSource.VertexTexelDisplay, ShaderSource.FragTexelDisplay );
		}

		public void Render()
		{
			if ( !_initialized ) return;

			if ( _rebuildShaders )
			{
				_rebuildShaders = false;
				RebuildShaders();
			}

			Clear();

			if ( Camera.Orthographic )
				Camera.AspectRatio = 1.0f;
			else
				Camera.AspectRatio = Width / (float)Height;

			var viewMatrix = Camera.GetViewMatrix();
			var projectionMatrix = Camera.GetProjectionMatrix();
			var modelView = Matrix4.Identity;

			GL.Viewport( 0, 0, Width, Height );
			GL.Scissor( 0, 0, Width, Height );

			GL.MatrixMode( MatrixMode.Projection );
			GL.PushMatrix();
			GL.LoadMatrix( ref projectionMatrix );

			GL.MatrixMode( MatrixMode.Modelview );
			GL.PushMatrix();
			GL.LoadMatrix( ref viewMatrix );

			GL.LineWidth( 1.0f );
			GL.Disable( EnableCap.LineStipple );

			BeforeRender?.Invoke();

			using ( var _ = new InterfaceScope() )
			{
				RenderInterface();
			}

			// shadowmap has its own fbo
			RenderShadowmap();

			GL.BindFramebuffer( FramebufferTarget.Framebuffer, _msFramebuffer );

			RenderSkybox();

			if ( GridEnabled && Camera.Orthographic )
			{
				GL.Disable( EnableCap.DepthTest );
				_gridRenderer.Render();
				GL.Enable( EnableCap.DepthTest );
			}

			if ( Wireframe )
			{
				_solidWireframeRenderer.Render();
			}
			else
			{
				_solidRenderer.Render();
			}

			if ( GridEnabled && !Camera.Orthographic )
			{
				_gridShader.Bind();
				_gridShader.SetUniform( ShaderConstants.CameraProjection, projectionMatrix );
				_gridShader.SetUniform( ShaderConstants.CameraView, viewMatrix );
				_gridShader.SetUniform( ShaderConstants.CameraPosition, Camera.Position );
				_gridShader.SetUniform( ShaderConstants.ModelMatrix, Matrix4.Identity );
				_gridShader.SetUniform( ShaderConstants.GridSize, 1f / GridSize );

				GL.Disable( EnableCap.CullFace );
				GL.DepthMask( false );
				GL.Begin( PrimitiveType.Quads );

				var mapsize = MapSize;
				GL.Vertex3( -mapsize / 2, mapsize / 2, 0 );
				GL.Vertex3( mapsize / 2, mapsize / 2, 0 );
				GL.Vertex3( mapsize / 2, -mapsize / 2, 0 );
				GL.Vertex3( -mapsize / 2, -mapsize / 2, 0 );

				GL.End();
				GL.DepthMask( true );
				GL.Enable( EnableCap.CullFace );
				_gridShader.Unbind();
			}

			Gizmos.Render();
			RenderUtility();

			AfterRender?.Invoke();

			GL.BindFramebuffer( FramebufferTarget.ReadFramebuffer, _msFramebuffer );
			GL.BindFramebuffer( FramebufferTarget.DrawFramebuffer, _framebuffer );
			GL.BlitFramebuffer( 0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest );
			GL.BlitFramebuffer( 0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest );
			GL.BindFramebuffer( FramebufferTarget.ReadFramebuffer, 0 );
			GL.BindFramebuffer( FramebufferTarget.DrawFramebuffer, 0 );

			if ( interfaceFbo != 0 )
			{
				using var _ = new ScopeFramebuffer( _framebuffer );
				using var draw = new DrawToScreen( Width, Height );
				draw.DrawTexturedQuad( interfaceTexture, Width, Height );
			}

			GL.MatrixMode( MatrixMode.Modelview );
			GL.PopMatrix();
			GL.MatrixMode( MatrixMode.Projection );
			GL.PopMatrix();

			GL.ActiveTexture( TextureUnit.Texture0 );
			GL.BindTexture( TextureTarget.Texture2D, 0 );
		}

		public Bitmap Screenshot()
		{
			if ( !_initialized )
				return null;

			int prevBuffer = GL.GetInteger( GetPName.FramebufferBinding );

			GL.BindFramebuffer( FramebufferTarget.Framebuffer, _framebuffer );

			var bmp = new Bitmap( Width, Height );
			var rect = new Rectangle( 0, 0, Width, Height );
			var data = bmp.LockBits( rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb );
			GL.ReadPixels( 0, 0, Width, Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0 );
			bmp.UnlockBits( data );
			bmp.RotateFlip( RotateFlipType.RotateNoneFlipY );

			GL.BindFramebuffer( FramebufferTarget.Framebuffer, prevBuffer );

			return bmp;
		}

		void InitializeGL()
		{
			var width = Width;
			var height = Height;
			var msaa = MSAA;

			GL.GetInteger( GetPName.MaxSamples, out int maxSamples );
			msaa = Math.Min( maxSamples, msaa );

			if ( _framebuffer == -1 )
			{
				GL.GenFramebuffers( 1, out _framebuffer );
				GL.BindFramebuffer( FramebufferTarget.Framebuffer, _framebuffer );
			}

			if ( _sceneTexture == -1 )
			{
				GL.GenTextures( 1, out _sceneTexture );
				GL.BindTexture( TextureTarget.Texture2D, _sceneTexture );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
				GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero );

				GL.BindFramebuffer( FramebufferTarget.Framebuffer, _framebuffer );
				GL.FramebufferTexture2D( FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _sceneTexture, 0 );

				GL.BindTexture( TextureTarget.Texture2D, 0 );

				if ( GL.CheckFramebufferStatus( FramebufferTarget.Framebuffer ) != FramebufferErrorCode.FramebufferComplete )
				{
					throw new Exception( "Error: Framebuffer is not complete!" );
				}

				GL.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
			}

			if ( _msFramebuffer == -1 )
			{
				GL.GenFramebuffers( 1, out _msFramebuffer );
				GL.BindFramebuffer( FramebufferTarget.Framebuffer, _msFramebuffer );

				GL.GenRenderbuffers( 1, out _depthBuffer );
				GL.BindRenderbuffer( RenderbufferTarget.Renderbuffer, _depthBuffer );
				GL.RenderbufferStorageMultisample( RenderbufferTarget.Renderbuffer, msaa, RenderbufferStorage.DepthComponent24, width, height );
				GL.FramebufferRenderbuffer( FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthBuffer );

				GL.GenRenderbuffers( 1, out _msColorbuffer );
				GL.BindRenderbuffer( RenderbufferTarget.Renderbuffer, _msColorbuffer );
				GL.RenderbufferStorageMultisample( RenderbufferTarget.Renderbuffer, msaa, RenderbufferStorage.Rgba8, width, height );
				GL.FramebufferRenderbuffer( FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _msColorbuffer );

				if ( GL.CheckFramebufferStatus( FramebufferTarget.Framebuffer ) != FramebufferErrorCode.FramebufferComplete )
				{
					Debug.LogError( "Error: Multisampled Framebuffer is not complete!" );
				}

				GL.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
			}

			_initialized = true;
		}

		public void Configure( int width, int height, int msaa = 0 )
		{
			if ( !_initialized ) return;

			width = MathHelper.Clamp( width, 16, 4096 );
			height = MathHelper.Clamp( height, 16, 4096 );
			GL.GetInteger( GetPName.MaxSamples, out int maxSamples );
			msaa = Math.Min( maxSamples, msaa );

			if ( Width == width && Height == height && MSAA == msaa ) return;

			Width = width;
			Height = height;
			MSAA = msaa;

			DestroyBase();
			DestroyInterface();
			InitializeGL();
		}

		public void Dispose()
		{
			HotloadHelper.OnHotload -= HotloadManager_OnHotload;
			_objects.Clear();
			_objects = null;
			_solidRenderer?.Dispose();
			_solidRenderer = null;
			_solidWireframeRenderer?.Dispose();
			_solidWireframeRenderer = null;
			//_imgui?.Dispose();
			//_imgui = null;
			DestroyBase();
			DestroyShadowmap();
			DestroyInterface();
		}

		void DestroyBase()
		{
			if ( _framebuffer != -1 )
			{
				GL.DeleteFramebuffer( _framebuffer );
				_framebuffer = -1;
			}

			if ( _msFramebuffer != -1 )
			{
				GL.DeleteFramebuffer( _msFramebuffer );
				_msFramebuffer = -1;
			}

			if ( _depthBuffer != -1 )
			{
				GL.DeleteRenderbuffer( _depthBuffer );
				_depthBuffer = -1;
			}

			if ( _msColorbuffer != -1 )
			{
				GL.DeleteRenderbuffer( _msColorbuffer );
				_msColorbuffer = -1;
			}

			if ( _sceneTexture != -1 )
			{
				GL.DeleteTexture( _sceneTexture );
				_sceneTexture = -1;
			}
		}

		void UpdateGrid()
		{
			_gridRenderer.Clear();
			_gridRenderer.Add( new List<GridSettings>()
			{
				new()
				{
					Forward = Camera.Forward,
					Origin = Vector3.Zero,
					Spacing = (int)GridSize,
					Size = MapSize,
				}
			} );
		}

		struct InterfaceScope : IDisposable
		{

			public InterfaceScope()
			{
				GL.PushClientAttrib( ClientAttribMask.ClientVertexArrayBit );
			}

			public void Dispose()
			{
				GL.PopClientAttrib();
				GL.Enable( EnableCap.DepthTest );
				GL.DepthMask( true );
				GL.UseProgram( 0 );
				GL.Enable( EnableCap.Dither );
				GL.Enable( EnableCap.Blend );
				GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
				GL.PixelStore( PixelStoreParameter.UnpackAlignment, 1 );
			}

		}

	}
}
