
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Documents;
using Graybox.Editor.Tools;
using Graybox.Editor.UI;
using Graybox.Interface;
using Graybox.Scenes;
using ImGuiNET;

namespace Graybox.Editor.Widgets;

internal class SceneWidgetConfig
{

	public enum OrthoView
	{
		Front,
		Side,
		Top
	}

	public bool Orthographic { get; set; } = false;
	public OrthoView View { get; set; } = OrthoView.Top;
	public bool Wireframe { get; set; } = false;
	public bool GridEnabled { get; set; } = true;
	public float GridSize { get; set; } = 32;
}

internal class SceneWidget : BaseWidget
{

	[EditorLayout.Data]
	public SceneWidgetConfig Config { get; set; } = new();

	public override SVector4? WindowBackground => new SVector4( .1f, .1f, .1f, 1 );
	public override SVector2? WindowPadding => new SVector2( 2, 2 );
	public override string Title => "Scene";

	public Scene Scene { get; private set; }

	public static List<SceneWidget> All = new();

	Document ActiveDocument;
	BaseTool ActiveTool => ToolManager.ActiveTool;
	EditorCameraController CameraController;

	public bool FreeLooking => CameraController?.FreeLooking ?? false;

	bool focused;
	bool forceUpdate;
	int lightmapCounter;

	public SceneWidget()
	{
		All.Add( this );
		EventSystem.Subscribe( this );
		CreateScene();
	}

	void AfterSceneRendered()
	{
		if ( Scene == null || ActiveDocument == null || ActiveTool?.Document == null ) return;

		//if ( IsFocused )
		{
			ActiveTool?.Setup( ActiveDocument, Scene );
			ActiveTool?.Render( Scene );
		}

		RenderAxis();
	}

	void RenderAxis()
	{
		if ( Scene.Camera.Orthographic ) return;

		int margin = 10;
		int axisLength = 100;
		var worldPos = Scene.ScreenToWorld( 512, 512 );
		worldPos += Scene.Camera.Forward * 250;

		var translate = Matrix4.CreateTranslation( worldPos );

		GL.PushMatrix();
		GL.MultMatrix( ref translate );

		GL.Viewport( (int)Size.X - axisLength - margin, (int)Size.Y - axisLength, axisLength, axisLength );
		GL.Disable( EnableCap.DepthTest );

		GL.LineWidth( 2.0f );
		GL.Begin( PrimitiveType.Lines );
		GL.Color3( Vector3.UnitX );
		GL.Vertex3( Vector3.Zero );
		GL.Vertex3( Vector3.UnitX * axisLength );
		GL.Color3( Vector3.UnitZ );
		GL.Vertex3( Vector3.Zero );
		GL.Vertex3( Vector3.Zero + Vector3.UnitZ * axisLength );
		GL.Color3( Vector3.UnitY );
		GL.Vertex3( Vector3.Zero );
		GL.Vertex3( Vector3.Zero + Vector3.UnitY * axisLength );
		GL.End();

		Vector3 forward = Scene.Camera.Forward;
		float dotX = Vector3.Dot( forward, Vector3.UnitX );
		float dotY = Vector3.Dot( forward, Vector3.UnitY );
		float dotZ = Vector3.Dot( forward, Vector3.UnitZ );

		Vector3 targetColor;
		if ( Math.Abs( dotX ) > Math.Abs( dotY ) && Math.Abs( dotX ) > Math.Abs( dotZ ) )
			targetColor = Vector3.UnitX;
		else if ( Math.Abs( dotY ) > Math.Abs( dotZ ) )
			targetColor = Vector3.UnitY;
		else
			targetColor = Vector3.UnitZ;

		currentAxisColor = Vector3.Lerp( currentAxisColor, targetColor, 0.05f );  // Lerp by 10%

		GL.PointSize( 10 );
		GL.Begin( PrimitiveType.Points );
		GL.Color3( currentAxisColor );
		GL.Vertex3( 0, 0, 0 );
		GL.End();

		GL.PopMatrix();
		GL.Viewport( 0, 0, Scene.Width, Scene.Height );
		GL.Enable( EnableCap.DepthTest );
	}
	Vector3 currentAxisColor;

	protected override void OnResized()
	{
		base.OnResized();

		ConfigureScene();
	}

	protected override void OnDestroyed()
	{
		base.OnDestroyed();

		All.Remove( this );
	}

	public override void OnDocumentActivated( Document document )
	{
		base.OnDocumentActivated( document );

		SetActiveDocument( document );
	}

	protected override void OnMouseDoubleClick( ref InputEvent e )
	{
		base.OnMouseDoubleClick( ref e );

		if ( Scene == null || ActiveDocument == null || ActiveTool?.Document == null ) return;
		if ( !IsFocused ) return;

		ActiveTool?.MouseDoubleClick( Scene, ref e );
	}

	protected override void OnMouseDown( ref InputEvent e )
	{
		base.OnMouseDown( ref e );

		if ( Scene == null || ActiveDocument == null || ActiveTool?.Document == null ) return;
		if ( !IsFocused ) return;

		var interfaceEvent = e;
		interfaceEvent.MousePosition = e.LocalMousePosition;
		Scene.Interface?.ProcessMouseInput( ref interfaceEvent, true );
		if ( interfaceEvent.Handled ) return;

		if ( e.Button == MouseButton.Left )
		{
			if ( Scene.ShowGizmos )
			{
				Scene.Gizmos.OnMouseDown( ref e );

				if ( e.Handled ) return;
			}

			ActiveTool?.MouseDown( Scene, ref e );
		}
	}

	protected override void OnMouseUp( ref InputEvent e )
	{
		base.OnMouseUp( ref e );

		if ( Scene == null || ActiveDocument == null || ActiveTool?.Document == null ) return;
		if ( !IsFocused ) return;

		if ( e.Button == MouseButton.Left )
		{
			if ( Scene.ShowGizmos )
			{
				Scene.Gizmos.OnMouseUp( ref e );

				if ( e.Handled ) return;
			}

			ActiveTool?.MouseUp( Scene, ref e );
		}
	}

	protected override void OnMouseMove( ref InputEvent e )
	{
		base.OnMouseMove( ref e );

		if ( Scene == null || ActiveDocument == null || ActiveTool?.Document == null ) return;
		if ( !IsFocused ) return;

		var interfaceEvent = e;
		interfaceEvent.MousePosition = e.LocalMousePosition;
		Scene.Interface?.ProcessMouseInput( ref interfaceEvent, false );

		if ( e.Handled ) return;

		if ( Scene.ShowGizmos && !Scene.Gizmos.IsCaptured )
		{
			Scene.Gizmos.OnMouseMove( ref e );

			if ( e.Handled ) return;
		}

		ActiveTool?.MouseMove( Scene, ref e );
	}

	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();

		focused = true;
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();

		focused = false;
	}

	protected override void OnKeyDown( ref InputEvent e )
	{
		base.OnKeyDown( ref e );

		CameraController?.OnKeyDown( ref e );

		if ( e.Handled )
		{
			return;
		}

		if ( Scene != null && ActiveTool != null && ActiveTool?.Document != null )
		{
			ActiveTool.KeyDown( Scene, ref e );
		}
	}

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );

		if ( Input.JustPressed( Key.Escape ) )
		{
			CameraController?.DisableFreeLook();
		}

		CursorGrabbed = CameraController?.FreeLooking ?? false;

		if ( ActiveDocument == null && DocumentManager.CurrentDocument != null )
		{
			SetActiveDocument( DocumentManager.CurrentDocument );
		}

		if ( ActiveDocument != null && DocumentManager.CurrentDocument == null )
		{
			SetActiveDocument( null );
		}

		if ( ActiveDocument == null || Scene == null )
		{
			var windowSize = ImGui.GetWindowSize();
			var windowCenter = new SVector2( windowSize.X / 2, windowSize.Y / 2 );
			var message = "Nothing open";
			var textSize = ImGui.CalcTextSize( message );
			var textPos = new SVector2( windowCenter.X - textSize.X / 2, windowCenter.Y - textSize.Y / 2 );

			ImGui.SetCursorPos( textPos + new SVector2( 2, 2 ) );
			ImGui.PushStyleColor( ImGuiCol.Text, new SVector4( 0.0f, 0, 0, 1 ) );
			ImGui.TextUnformatted( message );
			ImGui.PopStyleColor();

			ImGui.SetCursorPos( textPos );
			ImGui.PushStyleColor( ImGuiCol.Text, new SVector4( 1.0f, 0.1f, 0.1f, 0.5f ) );
			ImGui.TextUnformatted( message );
			ImGui.PopStyleColor();

			return;
		}

		ApplyConfig( false );
		ConfigureScene();

		if ( focused || forceUpdate )
		{
			forceUpdate = false;

			CameraController.Update( frameInfo );

			if ( Scene.Gizmos.IsCaptured )
			{
				var ge = new InputEvent()
				{
					LocalMousePosition = ScreenToLocal( Input.MousePosition )
				};
				Scene.Gizmos.OnMouseMove( ref ge );
			}

			if ( ImGui.IsWindowHovered() && !ImGui.IsAnyItemFocused() && !ImGui.IsAnyItemActive() && !ImGui.IsAnyItemActive() && !ImGui.IsAnyItemFocused() && !PopupManager.PopupOpen )
			{
				ImGui.SetWindowFocus();
			}
		}

		Scene.GridSize = EditorPrefs.GridSize;
		Scene.HandleUpdate( frameInfo );
		Scene.UpdateObjects( ActiveDocument.Map.WorldSpawn );
		Scene.Render();

		ImGui.Image( Scene.TextureId, ImGui.GetContentRegionAvail(), new SVector2( 0, 1 ), new SVector2( 1, 0 ) );

		UpdateViewportOverlay();

		if ( CameraController.FreeLooking )
		{
			var windowPos = ImGui.GetWindowPos();
			var windowSize = ImGui.GetWindowSize();
			var drawlist = ImGui.GetWindowDrawList();

			drawlist.AddRect( windowPos, windowPos + windowSize, ImGui.ColorConvertFloat4ToU32( new SVector4( 1.0f, 0.5f, 0, 0.5f ) ), 0.0f, ImDrawFlags.None, 2.0f );
		}

		UpdateDragDrop();

		ActiveTool?.UpdateFrame( Scene, frameInfo );
	}

	bool hasMouseInputs = false;
	float utilityBarWidth = 500f;
	void UpdateViewportOverlay()
	{
		var isOrthographic = Scene.Camera.Orthographic;
		var orthoView = Config.View;
		var skyboxEnabled = Scene.SkyboxEnabled;
		var sunEnabled = Scene.SunEnabled;
		var texelOverlay = Scene.DebugVisualizeTexels;
		var wireframe = Config.Wireframe;

		var viewportSize = ImGui.GetWindowSize();
		var overlaySize = new SVector2( utilityBarWidth, 40 );
		var overlayPosition = ImGui.GetWindowPos();

		ImGui.SetNextWindowPos( overlayPosition - new SVector2( 1, 1 ) );
		ImGui.SetNextWindowSize( overlaySize );
		ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, new SVector2( 4, 4 ) );
		ImGui.PushStyleVar( ImGuiStyleVar.ItemSpacing, new SVector2( 4, 4 ) );
		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 4, 4 ) );

		var toolbarFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBackground;
		if ( ImGui.Begin( $"1-{LayoutID}", toolbarFlags ) )
		{
			ImGui.BeginGroup();
			ImGui.BeginGroup();
			{
				// Perspective/Orthographic dropdown
				string currentView = isOrthographic ? orthoView.ToString() : "3D";
				ImGui.SetNextItemWidth( 80 );
				if ( ImGui.BeginCombo( "##ViewMode", currentView, ImGuiComboFlags.HeightSmall | ImGuiComboFlags.NoArrowButton ) )
				{
					if ( ImGui.Selectable( "3D", !isOrthographic ) ) isOrthographic = false;
					if ( ImGui.Selectable( "Front", isOrthographic && orthoView == SceneWidgetConfig.OrthoView.Front ) )
					{
						isOrthographic = true;
						orthoView = SceneWidgetConfig.OrthoView.Front;
					}
					if ( ImGui.Selectable( "Side", isOrthographic && orthoView == SceneWidgetConfig.OrthoView.Side ) )
					{
						isOrthographic = true;
						orthoView = SceneWidgetConfig.OrthoView.Side;
					}
					if ( ImGui.Selectable( "Top", isOrthographic && orthoView == SceneWidgetConfig.OrthoView.Top ) )
					{
						isOrthographic = true;
						orthoView = SceneWidgetConfig.OrthoView.Top;
					}

					if ( isOrthographic != Config.Orthographic || orthoView != Config.View )
					{
						Config.Orthographic = isOrthographic;
						Config.View = orthoView;
						ApplyConfig( false );
						ResetCamera();
					}

					ImGui.EndCombo();
				}
			}
			ImGui.EndGroup();

			// Right side controls
			ImGui.SameLine();
			ImGui.BeginGroup();
			{
				var gridIcon = EditorResource.Image( "assets/icons/icon_grid.png" );
				var entityIcon = EditorResource.Image( "assets/icons/icon_entity.png" );
				ImGuiEx.PushButtonActive( Scene.GridEnabled );
				if ( ImGuiEx.IconButton( "##SceneGridToggle", gridIcon ) )
				{
					Config.GridEnabled = !Config.GridEnabled;
				}
				ImGuiEx.PopButtonActive( Scene.GridEnabled );
				ImGui.SameLine();
				ImGuiEx.PushButtonActive( Scene.ShowEntities );
				if ( ImGuiEx.IconButton( "##SceneEntityToggle", entityIcon ) )
				{
					Scene.ShowEntities = !Scene.ShowEntities;
				}
				ImGuiEx.PopButtonActive( Scene.ShowEntities );
				ImGui.SameLine();
				var settingsIcon = EditorResource.Image( "assets/icons/icon_settings.png" );
				if ( ImGuiEx.IconButton( "##OptionsDropdown", settingsIcon ) )
				{
					ImGui.OpenPopup( "ViewOptionsPopup" );
				}

				if ( ImGui.BeginPopup( "ViewOptionsPopup" ) )
				{
					if ( Scene.Camera.Orthographic )
					{
						ImGui.BeginDisabled();
					}
					if ( ImGui.Checkbox( "Skybox", ref skyboxEnabled ) )
					{
						Scene.SkyboxEnabled = skyboxEnabled;
					}
					if ( ImGui.Checkbox( "Sun", ref sunEnabled ) )
					{
						Scene.SunEnabled = sunEnabled;
					}
					if ( ImGui.Checkbox( "Texel Overlay", ref texelOverlay ) )
					{
						Scene.DebugVisualizeTexels = texelOverlay;
					}
					if ( Scene.Camera.Orthographic )
					{
						ImGui.EndDisabled();
					}
					if ( ImGui.Checkbox( "Wireframe", ref wireframe ) )
					{
						Config.Wireframe = wireframe;
					}
					ImGui.EndPopup();
				}
			}
			ImGui.EndGroup();
			ImGui.EndGroup();
			utilityBarWidth = ImGui.GetItemRectSize().X + 8;
			ImGui.End();
		}
		hasMouseInputs = ImGui.IsAnyItemHovered();

		ImGui.PopStyleVar( 3 );
	}

	unsafe void UpdateDragDrop()
	{
		if ( Scene?.AssetSystem == null ) return;

		if ( ImGui.BeginDragDropTarget() )
		{
			ImGuiPayload* payload = ImGui.AcceptDragDropPayload( "ASSET" );
			if ( payload != null && payload->Data != null )
			{
				unsafe
				{
					var b = (byte*)payload->Data;
					var droppedData = System.Text.Encoding.UTF8.GetString( b, payload->DataSize );
					var asset = Scene.AssetSystem.FindAsset( droppedData );
					if ( asset is TextureAsset tex )
					{
						var mpos = Input.MousePosition;
						var localmpos = ScreenToLocal( mpos );
						var ray = Scene.ScreenToRay( localmpos );
						var trace = Scene.Physics.Trace( ray );
						if ( trace.Hit && trace.Object is Solid s )
						{
							var action = new EditFace( s.Faces, ( x, y ) => y.TextureRef.Texture = tex );
							ActiveDocument.PerformAction( "Change Texture", action );
						}
					}
				}
			}
			ImGui.EndDragDropTarget();
		}
	}

	void ConfigureScene()
	{
		if ( Scene == null ) return;

		var msaa = Scene.Camera.Orthographic ? 0 : 2;
		Scene.Configure( (int)Size.X, (int)Size.Y, msaa );
		Scene.Camera.OrthographicWidth = Scene.Width;
		Scene.Camera.OrthographicHeight = Scene.Height;
	}

	void SetActiveDocument( Document doc )
	{
		ActiveDocument = doc;
		if ( doc == null ) return;

		Scene.AssetSystem = doc.AssetSystem;
		Scene.Lightmaps = doc.Map.LightmapData;
		Scene.Environment = doc.Map.EnvironmentData;
		ActiveTool?.Setup( doc, Scene );
	}

	void CreateScene()
	{
		Scene = new Scene();
		CameraController = new( this );
		Scene.Wireframe = false;
		Scene.Camera.FarClip = 20000;
		Scene.ShadowDistance = 5000;
		Scene.SunShadowIntensity = 0.65f;
		Scene.SunDirection = -new Vector3( -1000, 1000, 1500 ).Normalized();
		Scene.SunColor = new Vector3( 255 / 255f, 245 / 255f, 245 / 255f );
		Scene.SkyboxEnabled = true;
		Scene.Camera.Position = new( 1500, 800, 1500 );
		Scene.Camera.Forward = Vector3.UnitX;
		Scene.AfterRender = AfterSceneRendered;
		Scene.Interface.AfterPaint = PaintOverlay;
		//Scene.Interface.Add( new ViewportOverlay( this ) );
		Scene.ShowGizmos = true;
		ApplyConfig( true );
		ConfigureScene();
	}

	void PaintOverlay( UIElementPaintEvent e )
	{
		if ( Scene == null || ActiveDocument == null || ActiveTool?.Document == null ) return;

		ActiveTool?.Paint( Scene, e );
	}

	internal override void OnDataSet()
	{
		base.OnDataSet();

		ApplyConfig( true );
	}

	void ResetCamera()
	{
		if ( Config.Orthographic )
		{
			switch ( Config.View )
			{
				case SceneWidgetConfig.OrthoView.Top:
					Scene.Camera.Position = new( 0, 0, 50000 );
					Scene.Camera.Forward = -Vector3.UnitZ;
					break;
				case SceneWidgetConfig.OrthoView.Front:
					Scene.Camera.Position = new( 50000, 0, 0 );
					Scene.Camera.Forward = -Vector3.UnitX;
					break;
				case SceneWidgetConfig.OrthoView.Side:
					Scene.Camera.Position = new( 0, 50000, 0 );
					Scene.Camera.Forward = -Vector3.UnitY;
					break;
			}
			Config.Wireframe = true;
			Config.GridEnabled = true;
			Scene.Wireframe = true;
		}
		else
		{
			Scene.Camera.Position = new( 450, 450, 450 );
			Config.GridEnabled = false;
			Scene.Camera.Forward = -Scene.Camera.Position.Normalized();
			Config.Wireframe = false;
			Scene.Wireframe = false;
		}
	}

	void ApplyConfig( bool initial )
	{
		if ( Config == null ) return;
		if ( Scene?.Camera == null ) return;

		Scene.Camera.Orthographic = Config.Orthographic;
		Scene.GridEnabled = Config.GridEnabled;
		Scene.GridSize = Config.GridSize;
		Scene.Wireframe = Config.Wireframe;
		Scene.Camera.FarClip = Config.Orthographic ? 100000 : 20000;

		if ( Config.Orthographic )
		{
			switch ( Config.View )
			{
				case SceneWidgetConfig.OrthoView.Top:
					if ( initial ) Scene.Camera.Position = new( 0, 0, 50000 );
					Scene.Camera.Forward = -Vector3.UnitZ;
					break;
				case SceneWidgetConfig.OrthoView.Front:
					if ( initial ) Scene.Camera.Position = new( 50000, 0, 0 );
					Scene.Camera.Forward = -Vector3.UnitX;
					break;
				case SceneWidgetConfig.OrthoView.Side:
					if ( initial ) Scene.Camera.Position = new( 0, 50000, 0 );
					Scene.Camera.Forward = -Vector3.UnitY;
					break;
			}
			Config.Wireframe = true;
			Scene.Wireframe = true;
		}

		if ( initial )
		{
			forceUpdate = true;
		}
	}

}
