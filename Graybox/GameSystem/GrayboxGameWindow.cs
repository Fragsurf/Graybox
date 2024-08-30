
using System.ComponentModel;
using System.Runtime;

namespace Graybox.GameSystem
{
	public class GrayboxGameWindow : GameWindow
	{

		static GameWindowSettings GameWindowSettings = new GameWindowSettings()
		{
			UpdateFrequency = 300
		};

		static NativeWindowSettings NativeWindowSettings = new NativeWindowSettings()
		{
			APIVersion = new( 3, 3 ),
			Flags = ContextFlags.Default,
			Profile = ContextProfile.Compatability,
			StartFocused = true,
			StartVisible = true,
			Title = "Graybox Game",
			WindowState = WindowState.Normal,
			ClientSize = new( 1920, 1080 ),
		};

		readonly GrayboxGame GameSystem;

		public GrayboxGameWindow( GrayboxGame gameSystem ) : base( GameWindowSettings, NativeWindowSettings )
		{
			GameSystem = gameSystem;
			RawMouseInput = true;
			GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
			Environment.SetEnvironmentVariable( "COMPlus_gcConcurrent", "1" );
		}

		protected override void OnLoad()
		{
			base.OnLoad();

			Graybox.Graphics.Helpers.GraphicsHelper.InitGL3D();

			GameSystem.Initialize();
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			base.OnClosing( e );

			GameSystem.Dispose();
		}

		protected override void OnRenderFrame( FrameEventArgs args )
		{
			base.OnRenderFrame( args );

			GL.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
			GL.ClearColor( System.Drawing.Color.DeepSkyBlue );
			GL.Clear( ClearBufferMask.ColorBufferBit );
			GL.Viewport( 0, 0, Size.X, Size.Y );

			var scene = GameSystem.ActiveScene;

			if ( scene != null )
			{
				scene.Render();
				GL.BindFramebuffer( FramebufferTarget.ReadFramebuffer, scene.FramebufferId );
				GL.BindFramebuffer( FramebufferTarget.DrawFramebuffer, 0 );
				GL.BlitFramebuffer( 0, 0, scene.Width, scene.Height, 0, 0, ClientSize.X, ClientSize.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear );
			}

			SwapBuffers();
		}

		protected override void OnUpdateFrame( FrameEventArgs args )
		{
			base.OnUpdateFrame( args );

			Input.KeyboardState = KeyboardState;
			Input.MouseState = MouseState;

			CursorState = CursorState.Grabbed;

			var scene = GameSystem.ActiveScene;
			scene?.Configure( Size.X, Size.Y, 2 );

			GameSystem.DeltaTime = (float)args.Time;
			GameSystem.Update();
		}

	}
}
