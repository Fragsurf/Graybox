
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;
using Graybox.Editor.Widgets;
using Graybox.Graphics.Helpers;
using Graybox.Providers.Map;
using Graybox.Providers.Model;
using Graybox.Editor.Settings.Models;
using ImGuiNET;
using System.Windows.Forms;
using OpenTK.Windowing.Common.Input;
using System.ComponentModel;
using Graybox.Editor.Tools;
using System.Runtime;
using System.Text.Json;

namespace Graybox.Editor;

internal partial class EditorWindow : GameWindow, IInputListener
{

	ImGuiController _imgui;
	float elapsedTime;
	List<BaseWidget> dockWidgets = new();

	public IReadOnlyList<BaseWidget> Widgets => dockWidgets;

	public static EditorWindow Instance;

	public EditorWindow() : base( new GameWindowSettings()
	{
		UpdateFrequency = 120
	}, new NativeWindowSettings()
	{
		API = ContextAPI.OpenGL,
		Flags = ContextFlags.Default,
		Profile = ContextProfile.Compatability,
		IsEventDriven = true,
		StartVisible = true,
		StartFocused = true,
		Title = "Graybox",
		WindowState = WindowState.Maximized,
		Icon = new WindowIcon( new Image( 128, 128, ImageToByteArray( "assets/icons/graybox_logo.png" ) ) )
	} )
	{
		_imgui = new( ClientSize.X, ClientSize.Y );
		Instance = this;
	}

	protected override void OnLoad()
	{
		base.OnLoad();

		UpdateFrame += x => ExceptionWrapper.Execute( DoUpdate, x );
		RenderFrame += x => ExceptionWrapper.Execute( DoRender, x );
		TextInput += x => ExceptionWrapper.Execute( DoTextInput, x );
		MouseWheel += x => ExceptionWrapper.Execute( DoMouseWheel, x );
		KeyDown += x => ExceptionWrapper.Execute( DoKeyDown, x );
		MouseMove += x => ExceptionWrapper.Execute( DoMouseMove, x );
		KeyUp += x => ExceptionWrapper.Execute( DoKeyUp, x );
		MouseUp += x => ExceptionWrapper.Execute( DoMouseUp, x );
		MouseDown += x => ExceptionWrapper.Execute( DoMouseDown, x );

		Debug.ShowException = ExceptionWrapper.ShowExceptionPopup;

		ExceptionWrapper.Execute( () =>
		{
			SetImGuiStyles();
			InitializeEditor();
			LoadDefaultLayout();
			LoadUserLayout();
		} );
	}

	private void DoUpdate( FrameEventArgs args )
	{
		Input.MouseState = MouseState;
		Input.KeyboardState = KeyboardState;

		// Update ImGui
		_imgui.Update( this, (float)args.Time );
		{
			bool restoreCursor = true;
			elapsedTime += (float)args.Time;

			List<BaseWidget> widgetsToClose = null;

			var io = ImGui.GetIO();
			io.WantCaptureMouse = false;

			if ( ImGuiEx.IsFontValid( _font ) ) ImGui.PushFont( _font );
			ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, new SVector2( 0, 0 ) );
			ImGui.SetNextWindowSize( new SVector2( ClientSize.X, ClientSize.Y ) );
			ImGui.SetNextWindowPos( new( 0, 0 ) );
			if ( ImGui.Begin( "Graybox Window", ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize ) )
			{
				ImGui.PopStyleVar( 1 );
				var menuHeight = DoMenuBar();
				menuHeight += DoMapTabs( menuHeight );

				PopupManager.Update();
				UpdateBusyState( args );
				ImGui.SetNextWindowPos( new( 0, menuHeight ) );
				ImGui.SetNextWindowContentSize( new SVector2( ClientSize.X, ClientSize.Y - menuHeight ) );

				var toolbarWidth = 80;

				if ( ImGui.BeginChild( "Dock Area" ) )
				{
					if ( ImGui.BeginChild( "Tools Bar", new SVector2( toolbarWidth, 0 ) ) )
					{
						DoToolsList();
						ImGui.EndChild();
					}
					ImGui.SameLine( toolbarWidth );

					ImGui.DockSpace( ImGui.GetID( "Dock Area" ), new SVector2(), ImGuiDockNodeFlags.AutoHideTabBar );
					var frameInfo = new FrameInfo( (float)args.Time, elapsedTime );
					foreach ( var widget in dockWidgets )
					{
						bool open = true;

						widget.PushWindowStyles();
						var displayed = ImGui.Begin( widget.Title + "##" + widget.LayoutID, ref open );
						widget.PopWindowStyles();

						if ( displayed && open )
						{
							if ( widget.CursorGrabbed )
							{
								restoreCursor = false;
								CursorState = CursorState.Grabbed;
							}
							widget.Update( frameInfo );
							ImGui.End();
						}

						if ( !open )
						{
							widgetsToClose ??= new();
							widgetsToClose.Add( widget );
						}
					}
					ImGui.EndChild();
				}
				ImGui.End();
			}
			if ( ImGuiEx.IsFontValid( _font ) ) ImGui.PopFont();

			switch ( ImGui.GetMouseCursor() )
			{
				case ImGuiMouseCursor.Arrow:
					Cursor = MouseCursor.Default;
					break;
				case ImGuiMouseCursor.TextInput:
					Cursor = MouseCursor.IBeam;
					break;
				case ImGuiMouseCursor.Hand:
					Cursor = MouseCursor.Hand;
					break;
				case ImGuiMouseCursor.ResizeEW:
					Cursor = MouseCursor.HResize;
					break;
				case ImGuiMouseCursor.ResizeNS:
					Cursor = MouseCursor.VResize;
					break;
			}

			if ( widgetsToClose != null )
			{
				foreach ( var widget in widgetsToClose )
				{
					widget.Destroy();
					dockWidgets.Remove( widget );
				}
			}

			if ( restoreCursor )
			{
				CursorState = CursorState.Normal;
			}
		}

		if ( updateQueue > 0 )
		{
			updateQueue--;

			if ( updateQueue <= 0 )
			{
				IsEventDriven = true;
			}
		}
	}

	private void DoRender( FrameEventArgs args )
	{
		GL.Viewport( 0, 0, ClientSize.X, ClientSize.Y );
		GL.Scissor( 0, 0, ClientSize.X, ClientSize.Y );
		GL.ClearColor( new Color4( 0, 32, 48, 255 ) );
		GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit );

		_imgui.Render();

		ImGuiController.CheckGLError( "End of frame" );

		SwapBuffers();
	}

	private void DoMouseDown( MouseButtonEventArgs e )
	{
		UpdateACoupleFrames();

		var inputEvent = Input.ProcessMouseDown( e );
		if ( inputEvent.Handled ) return;
	}

	private void DoMouseUp( MouseButtonEventArgs e )
	{
		UpdateACoupleFrames();

		var inputEvent = Input.ProcessMouseUp( e );
		if ( inputEvent.Handled ) return;
	}

	private void DoKeyUp( KeyboardKeyEventArgs e )
	{
		var inputEvent = Input.ProcessKeyUp( e );
		if ( inputEvent.Handled ) return;

		UpdateACoupleFrames();
		_imgui.ProcessKeyUp( e );
	}

	private void DoMouseMove( MouseMoveEventArgs e )
	{
		var inputEvent = Input.ProcessMouseMove( e );
		if ( inputEvent.Handled ) return;
	}

	private void DoKeyDown( KeyboardKeyEventArgs e )
	{
		if ( !ImGui.IsAnyItemActive() && EditorHotkeys.TryExecute( e ) )
			return;

		var inputEvent = Input.ProcessKeyDown( e );
		if ( inputEvent.Handled ) return;

		_imgui.ProcessKeyDown( e );
	}

	private void DoMouseWheel( MouseWheelEventArgs e )
	{
		_imgui.MouseScroll( e.Offset );
		UpdateACoupleFrames();
	}

	private void DoTextInput( TextInputEventArgs e )
	{
		_imgui.PressChar( (char)e.Unicode );
		UpdateACoupleFrames();
	}

	void LoadUserLayout()
	{
		var layout = EditorPrefs.Read<EditorLayout>( "user.layout", null );
		if ( layout != null )
		{
			LoadLayout( layout );
		}
	}

	void WriteUserLayout()
	{
		EditorPrefs.Write( "user.layout", SaveLayout() );
	}

	void LoadDefaultLayout()
	{
		LoadLayout( JsonSerializer.Deserialize<EditorLayout>( DefaultLayout ) );
	}

	void InitializeEditor()
	{
		GCSettings.LatencyMode = GCLatencyMode.Batch;

		MapProvider.Register( new VmfProvider() );
		MapProvider.Register( new RMapProvider() );
		MapProvider.Register( new GBMapProvider() );
		ModelProvider.Register( new AssimpProvider() );
		Input.Register( this );
		RawMouseInput = true;
		EventSystem.Subscribe( this );
		GraphicsHelper.InitGL3D();
		Debug.LogExciting( "Welcome to Graybox.  Join https://discord.gg/fragsurf for community, feedback, and help.", () => OpenWebsite( "https://discord.gg/fragsurf" ) );
	}

	int updateQueue = 0;
	public void UpdateACoupleFrames( int count = 120 )
	{
		updateQueue = Math.Max( count, 1 );
		IsEventDriven = false;
	}

	bool disregardUnsavedChanges;
	protected override void OnClosing( CancelEventArgs e )
	{
		var unsavedDocuments = DocumentManager.Documents.Where( x => x.History.TotalActionsSinceLastSave > 0 );

		if ( disregardUnsavedChanges || !unsavedDocuments.Any() )
		{
			base.OnClosing( e );

			WriteUserLayout();

			return;
		}

		e.Cancel = true;

		var unsavedMaps = string.Empty;
		foreach ( var doc in unsavedDocuments )
			unsavedMaps += "\n" + doc.MapFileName;

		new ConfirmationPopup2( "Unsaved Changes", $"Some files haven't been saved:\n{unsavedMaps}", "Save All", "Close All", "Cancel", () =>
		{
			foreach ( var doc in unsavedDocuments )
			{
				doc.SaveToFile();
			}
			Close();
		}, () =>
		{
			disregardUnsavedChanges = true;
			Close();
		}, () =>
		{
		} ).Show();
	}

	ImFontPtr _font;

	void SetImGuiStyles()
	{
		var dpiScale = 1.0f;
		if ( TryGetCurrentMonitorDpi( out var h, out var _ ) )
			dpiScale = h / 96.0f;

		var io = ImGui.GetIO();
		var robotoPath = "Assets/Fonts/Roboto-Regular.ttf";
		if ( File.Exists( robotoPath ) )
		{
			var fontSize = 14 * dpiScale;
			_font = io.Fonts.AddFontFromFileTTF( robotoPath, fontSize );
			_imgui.RecreateFontDeviceTexture();
		}

		//ImGui.StyleColorsDark();
		//EditorTheme.ClassicSteamStyle();
		EditorTheme.SetupImGuiStyle();

		ImGuiStylePtr style = ImGui.GetStyle();
		//style.TabBarBorderSize = 1;
		style.WindowMenuButtonPosition = ImGuiDir.Left;
		style.ScaleAllSizes( dpiScale );
	}

	protected override void OnResize( ResizeEventArgs e )
	{
		base.OnResize( e );

		_imgui.WindowResized( this.ClientSize.X, this.ClientSize.Y );
	}

	unsafe void DoToolsList()
	{
		ImGui.SetCursorPosY( ImGui.GetCursorPosY() + 4 );

		foreach ( var tool in ToolManager.Tools )
		{
			bool isActiveTool = tool == ToolManager.ActiveTool;

			if ( isActiveTool )
			{
				ImGuiEx.PushButtonPrimary();
			}
			else
			{
				ImGuiEx.PushButtonDim();
			}

			var img = EditorResource.Image( tool.EditorIcon );
			var btnSize = ImGui.GetContentRegionAvail().X - 24;
			var size = new SVector2( btnSize, btnSize );

			var toolnow = tool;

			ImGui.SetCursorPosX( ImGui.GetCursorPosX() + 6 );

			if ( ImGuiEx.IconButton( $"##tool_{tool.Name}", img, size, btnSize * .75f ) )
			{
				ToolManager.Activate( toolnow );
			}

			if ( ImGui.IsItemHovered() )
			{
				ImGui.BeginTooltip();
				var hotkey = EditorHotkeys.GetHotkeyString( tool.GetType().Name );
				ImGui.Text( $"{tool.Name} ({hotkey})" );
				ImGui.EndTooltip();
			}

			if ( isActiveTool )
			{
				ImGuiEx.PopButtonPrimary();
			}
			else
			{
				ImGuiEx.PopButtonDim();
			}
		}

		ImGuiEx.DrawBorder( 2f, ImGuiDir.Right );
	}

	float DoMenuBar()
	{
		var height = 0f;

		ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, new SVector2( 8, 8 ) );
		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 8, 8 ) );

		if ( ImGui.BeginMainMenuBar() )
		{
			ImGui.SetWindowFontScale( 1f );
			if ( ImGui.BeginMenu( "File" ) )
			{
				if ( ImGui.MenuItem( "New", "Ctrl+N" ) )
				{
					CreateNewMap();
				}

				if ( ImGui.MenuItem( "Open...", "Ctrl+O" ) )
				{
					BrowseForMap();
				}

				if ( ImGui.MenuItem( "Save", "Ctrl+S" ) )
				{
					SaveToFile( false );
				}

				if ( ImGui.MenuItem( "Save As...", "Ctrl+Shift+S" ) )
				{
					SaveToFile( true );
				}

				var recentFiles = GetRecentFiles();
				if ( ImGui.BeginMenu( "Recent", recentFiles.Count > 0 ) )
				{
					foreach ( var file in recentFiles )
					{
						var fileName = System.IO.Path.GetFileName( file.Location );
						if ( ImGui.MenuItem( fileName ) )
						{
							OpenMap( file.Location );
						}
					}
					ImGui.EndMenu();
				}

				if ( ImGui.MenuItem( "Exit", "Alt+F4" ) ) { Close(); }
				ImGui.EndMenu();
			}

			if ( ImGui.BeginMenu( "Edit" ) )
			{
				bool canUndo = DocumentManager.CurrentDocument?.History?.CanUndo() ?? false;
				bool canRedo = DocumentManager.CurrentDocument?.History?.CanRedo() ?? false;
				if ( ImGui.MenuItem( "Undo", "Ctrl+Z", false, canUndo ) ) DocumentManager.CurrentDocument?.History?.Undo();
				if ( ImGui.MenuItem( "Redo", "Ctrl+Y", false, canRedo ) ) DocumentManager.CurrentDocument?.History?.Redo();
				ImGui.Separator();
				if ( ImGui.MenuItem( "Cut", "Ctrl+X" ) ) DocumentManager.CurrentDocument?.Cut();
				if ( ImGui.MenuItem( "Copy", "Ctrl+C" ) ) DocumentManager.CurrentDocument?.Copy();
				if ( ImGui.MenuItem( "Paste", "Ctrl+V" ) ) DocumentManager.CurrentDocument?.Paste();
				ImGui.EndMenu();
			}

			if ( ImGui.BeginMenu( "View" ) )
			{
				if ( ImGui.BeginMenu( "Layout" ) )
				{
					if ( ImGui.MenuItem( "Save Layout (Not done yet)", false ) )
					{
						//layout = SaveLayout();
					}
					if ( ImGui.MenuItem( "Load Default" ) )
					{
						LoadDefaultLayout();
					}
					ImGui.EndMenu();
				}
				if ( ImGui.MenuItem( "Scene" ) )
				{
					dockWidgets.Add( new SceneWidget() );
				}
				if ( ImGui.MenuItem( "Game" ) )
				{
					dockWidgets.Add( new GameWidget() );
				}
				if ( ImGui.MenuItem( "Lightmaps" ) )
				{
					dockWidgets.Add( new LightmapWidget() );
				}
				if ( ImGui.MenuItem( "Hierarchy" ) )
				{
					dockWidgets.Add( new HierarchyWidget() );
				}
				if ( ImGui.MenuItem( "Tool Properties" ) )
				{
					dockWidgets.Add( new ToolPropertiesWidget() );
				}
				if ( ImGui.MenuItem( "Asset Browser" ) )
				{
					dockWidgets.Add( new AssetBrowserWidget() );
				}
				if ( ImGui.MenuItem( "Console" ) )
				{
					dockWidgets.Add( new ConsoleWidget() );
				}
				if ( ImGui.MenuItem( "Profiler" ) )
				{
					dockWidgets.Add( new ProfilerWidget() );
				}
				ImGui.EndMenu();
			}

			if ( ImGui.BeginMenu( "Debug & Dev" ) )
			{
				if ( ImGui.MenuItem( "Copy Editor Layout" ) )
				{
					var layout = SaveLayout();
					var layoutJson = JsonSerializer.Serialize( layout );
					TextCopy.ClipboardService.SetText( layoutJson );
				}
				if ( ImGui.MenuItem( "Throw Exception" ) )
				{
					throw new NotImplementedException( "This thing isn't implemented" );
				}
				ImGui.EndMenu();
			}

			height = ImGui.GetWindowSize().Y;
			ImGui.EndMainMenuBar();
		}

		ImGui.PopStyleVar( 2 );

		return height;
	}

	List<RecentFile> GetRecentFiles()
	{
		return EditorCookie.Get<List<RecentFile>>( "RecentFiles" ) ?? new List<RecentFile>();
	}

	public void CreateNewMap()
	{
		DocumentManager.AddAndSwitch( new Document( null, new Map() ) );
	}

	public void SaveToFile( bool saveAs )
	{
		if ( DocumentManager.CurrentDocument?.SaveToFile( null, saveAs ) ?? false )
		{
			if ( saveAs )
			{
				UpdateRecentFiles( DocumentManager.CurrentDocument.MapFile.NormalizePath() );
			}
		}
	}

	public void BrowseForMap()
	{
		var openFileDialog = new OpenFileDialog
		{
			Filter = "RMAP Files (*.rmap)|*.rmap|All Files (*.*)|*.*",
			Title = "Open RMAP File"
		};

		if ( openFileDialog.ShowDialog() == DialogResult.OK )
		{
			string selectedFile = openFileDialog.FileName;
			OpenMap( selectedFile );
		}
	}

	void OpenMap( string filePath )
	{
		if ( string.IsNullOrEmpty( filePath ) )
		{
			Debug.LogError( $"Invalid file path." );
			return;
		}

		filePath = filePath.NormalizePath();

		if ( DocumentManager.Documents.Any( x => x.MapFile == filePath ) )
		{
			Debug.LogWarning( $"{filePath} is already open." );
			return;
		}

		if ( !File.Exists( filePath ) )
		{
			Debug.LogWarning( $"{filePath} doesn't exist at that location." );
			return;
		}

		try
		{
			var map = MapProvider.GetMapFromFile( filePath );
			var doc = new Document( filePath, map );
			map.PostLoadProcess( doc.GameData );
			DocumentManager.AddAndSwitch( doc );
			UpdateRecentFiles( filePath );
			Debug.LogSuccess( "Map opened: " + filePath );

			if ( ToolManager.ActiveTool == null )
			{
				ToolManager.Activate<SelectTool2>();
			}
		}
		catch ( Exception e )
		{
			Debug.LogError( "Failed to open map with exception: " + e.Message );
		}
	}

	void UpdateRecentFiles( string filePath )
	{
		var recentFiles = EditorCookie.Get<List<RecentFile>>( "RecentFiles" ) ?? new List<RecentFile>();
		var existingFile = recentFiles.FirstOrDefault( rf => rf.Location == filePath );

		if ( existingFile != null )
			recentFiles.Remove( existingFile );

		recentFiles.Insert( 0, new RecentFile { Location = filePath } );

		int maxRecentFiles = 10;
		if ( recentFiles.Count > maxRecentFiles )
			recentFiles = recentFiles.Take( maxRecentFiles ).ToList();

		EditorCookie.Set( "RecentFiles", recentFiles );
	}

	float DoMapTabs( float verticaloffset )
	{
		var height = 0f;

		ImGui.PushStyleVar( ImGuiStyleVar.WindowMinSize, new SVector2( 16, 16 ) );
		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 8, 10 ) );
		ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, new SVector2( 4, 4 ) );
		ImGui.PushStyleVar( ImGuiStyleVar.TabBarBorderSize, 0 );
		ImGui.PushStyleColor( ImGuiCol.WindowBg, new SVector4( 0, 0, 0, .4f ) );
		ImGui.PushStyleColor( ImGuiCol.Tab, new SVector4( 0, 0, 0, 0 ) );
		ImGui.PushStyleColor( ImGuiCol.TabActive, EditorTheme.hoverBackgroundColor );
		ImGui.PushStyleColor( ImGuiCol.TabHovered, EditorTheme.activeBackgroundColor );

		ImGui.SetNextWindowPos( new SVector2( 0, verticaloffset ) );
		ImGui.SetNextWindowSize( new SVector2( ImGui.GetIO().DisplaySize.X, -1 ) );

		if ( ImGui.Begin( "TabMenu", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize ) )
		{
			if ( ImGui.BeginTabBar( "TabBar", ImGuiTabBarFlags.AutoSelectNewTabs ) )
			{
				Document wantstoClose = null;

				foreach ( var doc in DocumentManager.Documents )
				{
					var isOpen = true;
					var name = doc.MapFileName;
					var flags = ImGuiTabItemFlags.None;

					if ( doc.History.TotalActionsSinceLastSave > 0 )
						flags |= ImGuiTabItemFlags.UnsavedDocument;

					if ( ImGui.BeginTabItem( name, ref isOpen, flags ) )
					{
						ImGui.EndTabItem();
					}

					if ( ImGui.IsItemActivated() )
					{
						SetActiveDocument( doc );
					}

					if ( !isOpen )
					{
						wantstoClose = doc;
					}
				}

				if ( wantstoClose != null )
				{
					TryCloseDocument( wantstoClose );
				}

				ImGui.EndTabBar();
			}

			DoUtilityBar();

			height = ImGui.GetWindowSize().Y;

			ImGui.End();
		}

		ImGui.PopStyleVar( 4 );
		ImGui.PopStyleColor( 4 );

		return height;
	}

	float utilityBarOffset = 0f;
	float utilityBarOffset2 = 0f;
	public void DoUtilityBar()
	{
		//ImGui.SetCursorPosX( ImGui.GetWindowSize().X * .5f - utilityBarOffset );
		//ImGui.SetCursorPosY( 4 );
		//ImGui.BeginGroup();
		//{
		//	var playIcon = EditorResource.Image( "assets/icons/icon_play.png" );
		//	ImGuiEx.PushButtonOutline();
		//	if ( ImGuiEx.IconButton( "##PlayButton", playIcon ) )
		//	{

		//	}
		//	ImGuiEx.PopButtonOutline();
		//}
		//ImGui.EndGroup();

		//utilityBarOffset = ImGui.GetItemRectSize().X * .5f;

		ImGui.SameLine();
		ImGui.SetCursorPosX( ImGui.GetWindowSize().X - utilityBarOffset2 - 4 );
		ImGui.BeginGroup();
		{
			var gridsnapIcon = EditorResource.Image( "assets/icons/icon_gridsnap.png" );
			ImGuiEx.PushButtonActive( EditorPrefs.GridSnapEnabled );
			if ( ImGuiEx.IconButton( "Grid Snap", gridsnapIcon ) )
			{
				EditorPrefs.GridSnapEnabled = !EditorPrefs.GridSnapEnabled;
			}
			ImGuiEx.PopButtonActive( EditorPrefs.GridSnapEnabled );

			ImGui.SameLine( 0, 4 );

			int gridSize = EditorPrefs.GridSize;
			var oldSize = gridSize;

			bool valueChanged = false;
			ImGui.SetNextItemWidth( 64 );
			ImGui.DragInt( "##GridSize", ref gridSize, 0, 0 );

			ImGuiEx.PushButtonOutline();
			ImGui.SameLine( 0, 4 );
			if ( ImGui.Button( "-", new SVector2( 32, ImGui.GetFrameHeight() ) ) )
			{
				gridSize--;
				valueChanged = true;
			}
			if ( ImGui.IsItemHovered() )
			{
				var decreaseHotkey = EditorHotkeys.GetHotkeyString( nameof( EditorHotkeyNames.DecreaseGrid ) ) + " Decrease Grid Size";
				ImGui.SetTooltip( decreaseHotkey );
			}

			// Manual + button
			ImGui.SameLine( 0, 4 );
			if ( ImGui.Button( "+", new SVector2( 32, ImGui.GetFrameHeight() ) ) )
			{
				gridSize++;
				valueChanged = true;
			}
			if ( ImGui.IsItemHovered() )
			{
				var increaseHotkey = EditorHotkeys.GetHotkeyString( nameof( EditorHotkeyNames.IncreaseGrid ) ) + " Increase Grid Size";
				ImGui.SetTooltip( increaseHotkey );
			}

			if ( valueChanged )
			{
				if ( gridSize < oldSize )
					oldSize /= 2;
				else
					oldSize *= 2;
				oldSize = oldSize.NearestPowerOfTwo();
				oldSize = Math.Clamp( oldSize, 1, 512 );
				EditorPrefs.GridSize = oldSize;
			}

			ImGuiEx.PopButtonOutline();
			ImGui.SameLine();
			ImGuiEx.PushButtonOutline();
			float angleSnap = EditorPrefs.AngleSnap;
			ImGui.TextDisabled( "Angle Snap" );
			ImGui.SameLine();
			ImGui.SetNextItemWidth( 64 );
			if ( ImGui.DragFloat( "##AngleSnap", ref angleSnap, 1.0f, 0, 180, "%.1f°" ) )
			{
				EditorPrefs.AngleSnap = (int)Math.Clamp( angleSnap, 0f, 90f );
			}
			ImGuiEx.PopButtonOutline();
		}
		ImGui.EndGroup();

		utilityBarOffset2 = ImGui.GetItemRectSize().X;
	}

	public void TryCloseDocument( Document doc )
	{
		if ( doc.History.TotalActionsSinceLastSave > 0 )
		{
			new ConfirmationPopup2( "Unsaved Changes", $"{doc.MapFileName} has unsaved changes, save now?", "Save", "Close", "Cancel", () =>
			{
				doc.SaveToFile();
				DocumentManager.Remove( doc );
			}, () =>
			{
				DocumentManager.Remove( doc );
			}, () =>
			{

			} ).Show();
		}
		else
		{
			DocumentManager.Remove( doc );
		}
	}

	void SetActiveDocument( Document document )
	{
		if ( DocumentManager.CurrentDocument == document )
			return;

		DocumentManager.SwitchTo( document );
	}

	public void OpenWebsite( string url )
	{
		new ConfirmationPopup( "Open Website", $"Are you sure you want to open this website?\n{url}", "Yes", "No", () => EditorUtils.OpenWebsite( url ), () => { } ).Show();
	}

	public void OnKeyUp( ref InputEvent e )
	{
		foreach ( var widget in dockWidgets )
		{
			widget.HandleKeyUp( ref e );
			if ( e.Handled ) break;
		}
	}

	public void OnKeyDown( ref InputEvent e )
	{
		var freelooking = SceneWidget.All?.Any( x => x.FreeLooking ) ?? false;

		if ( !freelooking )
		{
			EditorHotkeys.TryExecute( ref e );
		}

		if ( e.Handled ) return;

		foreach ( var widget in dockWidgets )
		{
			widget.HandleKeyDown( ref e );
			if ( e.Handled ) break;
		}
	}

	public void OnMouseDown( ref InputEvent e )
	{
		foreach ( var widget in dockWidgets )
		{
			widget.HandleMouseDown( ref e );
			if ( e.Handled ) break;
		}
	}

	public void OnMouseUp( ref InputEvent e )
	{
		foreach ( var widget in dockWidgets )
		{
			widget.HandleMouseUp( ref e );
			if ( e.Handled ) break;
		}
	}

	public void OnMouseMove( ref InputEvent e )
	{
		foreach ( var widget in dockWidgets )
		{
			widget.HandleMouseMove( ref e );
			if ( e.Handled ) break;
		}
	}

	[Event( nameof( EditorEvents.DocumentActivated ) )]
	public void OnDocumentActivated( Document document )
	{
		foreach ( var widget in dockWidgets )
		{
			widget.OnDocumentActivated( document );
		}
	}

	[Event( nameof( EditorEvents.DocumentDeactivated ) )]
	public void OnDocumentDeactivated( Document document )
	{
		foreach ( var widget in dockWidgets )
		{
			widget.OnDocumentDeactivated( document );
		}
	}

	private bool _isBusy = false;
	private float _busyTimeout = 0.2f;
	private const float BusyTimeout = 0.2f;

	private string _busyTitle = "";
	private string _busyMessage = "";
	private float _busyProgress = 0f;
	private Action _busyCancelAction = null;

	public void SetBusyState( string title = "Busy", string message = "Processing...", float progress = 0f, Action cancelAction = null )
	{
		if ( !_isBusy )
		{
			_isBusy = true;
		}

		_busyTimeout = BusyTimeout;
		_busyTitle = title;
		_busyMessage = message;
		_busyProgress = progress;
		_busyCancelAction = cancelAction;
	}

	private void UpdateBusyState( FrameEventArgs e )
	{
		if ( _isBusy )
		{
			_busyTimeout -= (float)e.Time;
			if ( _busyTimeout <= 0 )
			{
				_busyTitle = "";
				_busyMessage = "";
				_busyProgress = 0f;
				_busyCancelAction = null;
			}
			else
			{
				ShowBusyPopup();
			}
		}
	}

	private void ShowBusyPopup()
	{
		var id = $"{_busyTitle}##0123";
		ImGui.OpenPopup( id );
		ImGui.SetNextWindowSize( new SVector2( 450, 0 ) );
		if ( ImGui.BeginPopupModal( id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings ) )
		{
			ImGui.Text( _busyMessage );
			ImGui.Spacing();
			ImGui.ProgressBar( _busyProgress, new SVector2( -1, 0 ), $"{(_busyProgress * 100):F0}%" );
			ImGui.Spacing();

			if ( _busyCancelAction != null )
			{
				if ( ImGui.Button( "Cancel", new SVector2( -1, 0 ) ) )
				{
					_busyCancelAction.Invoke();
					_isBusy = false;
				}
			}

			ImGui.EndPopup();
		}
	}

	public static byte[] ImageToByteArray( string iconPath )
	{
		try
		{
			using ( var img = NetVips.Image.NewFromFile( iconPath ) )
			{
				var rgbaImage = img.Colourspace( NetVips.Enums.Interpretation.Srgb );
				if ( rgbaImage.Width != 128 || rgbaImage.Height != 128 )
				{
					rgbaImage = rgbaImage.Resize( 128.0 / rgbaImage.Width );
				}
				byte[] pixels = rgbaImage.WriteToMemory();
				return pixels;
			}
		}
		catch ( Exception )
		{
			return CreateBlackImage();
		}

		static byte[] CreateBlackImage()
		{
			const int size = 128 * 128 * 4;
			return new byte[size];
		}
	}

}
