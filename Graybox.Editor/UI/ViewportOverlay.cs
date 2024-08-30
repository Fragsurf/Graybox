
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Editor.Documents;
using Graybox.Interface;
using SkiaSharp;
using Graybox.Scenes;
using Graybox.Editor.Tools;
using Graybox.Editor.Widgets;

namespace Graybox.Editor.UI
{
	internal class ViewportOverlay : UIElement
	{

		public UIElement ToolOverlay { get; private set; }

		UIElement toolbar;
		SKPoint mousePosition;
		bool showEntityTags;

		bool Is3D => !Scene.Camera.Orthographic;
		MapObject HoveredEntity;
		SceneWidget SceneWidget;
		Scene Scene => SceneWidget?.Scene;

		public ViewportOverlay( SceneWidget sceneWidget )
		{
			SceneWidget = sceneWidget;
			Width = Length.Percent( 100 );
			Height = Length.Percent( 100 );
			Position = PositionType.Absolute;
			Top = 0;
			Left = 0;

			Direction = FlexDirection.Column;

			toolbar = Add( CreateToolbar() );

			ToolOverlay = Add( new UIElement()
			{
				Width = Length.Percent( 100 ),
				Height = Length.Percent( 100 ),
				Padding = 8,
				OnPaint = PaintToolOverlay
			} );
		}

		void PaintToolOverlay( UIElementPaintEvent e )
		{
			if ( Scene == null || DocumentManager.CurrentDocument == null ) return;

			e.PaintDefault = false;
			ToolManager.ActiveTool?.Paint( Scene, e );
		}

		protected override void MouseDown( ref InputEvent e )
		{
			base.MouseDown( ref e );

			if ( HoveredEntity != null )
			{
				Select( HoveredEntity );
				e.Handled = true;
			}

			if ( toolbar.BoxRect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y ) )
			{
				e.Handled = true;
			}
		}

		BaseTool lastTool;
		protected override void Update()
		{
			base.Update();

			if ( Scene == null ) return;

			if ( ToolOverlay != null )
			{
				var currentTool = ToolManager.ActiveTool;
				if ( lastTool != currentTool )
				{
					lastTool = currentTool;
					ToolOverlay.Clear();
					ToolOverlay.Padding = 4;

					currentTool?.BuildOverlay( Scene, ToolOverlay );
				}
			}
		}

		void Select( MapObject obj )
		{
			var doc = DocumentManager.CurrentDocument;
			if ( doc == null ) return;

			obj.IsSelected = true;
			var action = new ChangeSelection( new MapObject[] { obj }, doc.Selection.GetSelectedObjects() );
			doc.PerformAction( "Select entity: " + obj.ClassName, action );
		}

		UIElement CreateToolbar()
		{
			var toolbar = new UIElement();
			toolbar.Direction = FlexDirection.Row;
			toolbar.Padding = 2f;
			toolbar.BackgroundColor = Graybox.Interface.Theme.Background;
			toolbar.Shrink = 0;

			//toolbar.Add( CreateViewportTypeLabel() );
			toolbar.Add( CreateViewportTypeCombo() );

			// Grid
			{
				var gridSizeLabel = toolbar.Add( new TextElement() );
				gridSizeLabel.Centered = true;
				gridSizeLabel.MinWidth = 32;
				gridSizeLabel.FontSize = 12;
				gridSizeLabel.Color = Graybox.Interface.Theme.DimButtonForeground.Darken( .45f );
				gridSizeLabel.BackgroundColor = Graybox.Interface.Theme.DimButtonBackground.Darken( .45f );
				gridSizeLabel.MarginLeft = 2;
				gridSizeLabel.Tooltip = "Grid Size";
				gridSizeLabel.OnUpdate = () => gridSizeLabel.Text = SceneWidget.Config.GridSize.ToString() ?? "0";

				var gridBtnTooltip = Is3D ? "Toggle 3D Grid" : "Toggle 2D Grid";
				var gridBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.Grid3x3, Tooltip = gridBtnTooltip } );
				BindToggleButton( gridBtn, () => SceneWidget.Config.GridEnabled, x => SceneWidget.Config.GridEnabled = !SceneWidget.Config.GridEnabled );

				var gridDecreaseBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.Remove, Tooltip = "Decrease Grid Size" } );
				var gridIncreaseBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.Add, Tooltip = "Increase Grid Size" } );

				gridDecreaseBtn.OnMouseDown = x => SceneWidget.Config.GridSize /= 2;
				gridIncreaseBtn.OnMouseDown = x => SceneWidget.Config.GridSize *= 2;
			}

			toolbar.AddGrow();

			if ( Is3D )
			{
				var skyBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.Sunny, Tooltip = "Toggle Skybox" } );
				var lightsBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.Lightbulb, Tooltip = "Toggle Lighting" } );
				var texelsBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.LightbulbOutline, Tooltip = "Visualize Texels" } );
				BindToggleButton( skyBtn, () => Scene.SkyboxEnabled, x => Scene.SkyboxEnabled = !Scene.SkyboxEnabled );
				BindToggleButton( lightsBtn, () => Scene.SunEnabled, x => Scene.SunEnabled = !Scene.SunEnabled );
				BindToggleButton( texelsBtn, () => Scene.DebugVisualizeTexels, x => Scene.DebugVisualizeTexels = !Scene.DebugVisualizeTexels );
			}

			var wireframeBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.ShapeLine, Tooltip = "Toggle Wireframe" } );
			var entBtn = toolbar.Add( new ButtonElement.Dim() { Icon = MaterialIcons.Person, Tooltip = "Toggle Entity Overlay" } );

			BindToggleButton( wireframeBtn, () => SceneWidget.Config.Wireframe, x => SceneWidget.Config.Wireframe = !SceneWidget.Config.Wireframe );
			BindToggleButton( entBtn, () => showEntityTags, x => showEntityTags = x );

			foreach ( var btn in toolbar.Children.OfType<ButtonElement>() )
			{
				btn.Width = 32;
				btn.Height = 32;
				btn.Shrink = 0;
				btn.BorderRadius = 1;
				btn.MarginLeft = 2;
				btn.FontSize = 15;
			}

			void BindToggleButton( UIElement element, Func<bool> getState, Action<bool> setState )
			{
				element.OnMouseDown = x =>
				{
					x.Handled = true;
					setState?.Invoke( !getState() );
				};
				element.OnUpdate = () =>
				{
					var state = getState?.Invoke() ?? false;
					element.BackgroundColor = state ? Graybox.Interface.Theme.ButtonBackground : Graybox.Interface.Theme.DimButtonBackground;
					element.BackgroundHoverColor = state ? Graybox.Interface.Theme.ButtonBackgroundHover : Graybox.Interface.Theme.DimButtonBackgroundHover;
				};
			}

			return toolbar;
		}

		ComboBoxElement CreateViewportTypeCombo()
		{
			var combo = new ComboBoxElement();
			combo.UsePopup = false;
			combo.BorderRadius = 0;
			combo.Padding = 3;
			combo.PaddingRight = 20;
			combo.FontSize = 12;
			combo.Width = 120;
			combo.Shrink = 0;
			combo.BorderWidth = 0;

			combo.Options.Add( "3D" );

			var viewDirections = Enum.GetValues( typeof( SceneWidgetConfig.OrthoView ) );
			foreach ( var viewDir in viewDirections )
				combo.Options.Add( "2D " + viewDir.ToString() );

			if ( Is3D )
				combo.SetValue( "3D", false );
			else
				combo.SetValue( "2D " + SceneWidget.Config.View, false );

			combo.OnValueChanged = SetViewport;

			return combo;
		}

		protected override void MouseMove( ref InputEvent e )
		{
			base.MouseMove( ref e );

			mousePosition = new SKPoint( e.LocalMousePosition.X, e.LocalMousePosition.Y );
		}

		void SetViewport( string comboValue )
		{
			if ( comboValue == "3D" )
			{
				if ( SceneWidget.Config.Orthographic )
				{
					Scene.Camera.Position = Vector3.UnitZ * 500 - Vector3.UnitX * 500;
					Scene.Camera.Forward = -Scene.Camera.Position.Normalized();
				}
				SceneWidget.Config.Orthographic = false;
				SceneWidget.Config.Wireframe = false;
			}
			else
			{
				var dir = comboValue[3..];

				if ( Enum.TryParse( typeof( SceneWidgetConfig.OrthoView ), dir, out var viewDir ) )
					SceneWidget.Config.View = (SceneWidgetConfig.OrthoView)viewDir;

				SceneWidget.Config.Orthographic = true;
				SceneWidget.Config.Wireframe = true;

				if ( !Scene.Camera.Orthographic )
				{
					Scene.Camera.OrthographicZoom = 2.5f;
				}

				switch ( SceneWidget.Config.View )
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
			}
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			HoveredEntity = null;

			if ( showEntityTags )
			{
				PaintEntities( canvas );
			}
		}

		void PaintEntities( SKCanvas canvas )
		{
			var doc = DocumentManager.CurrentDocument;
			if ( doc == null ) return;

			using ( var paint = new SKPaint() )
			{
				var ents = doc.Map.WorldSpawn.GetAllDescendants<Entity>();
				foreach ( var e in ents )
				{
					paint.Color = SKColors.Black.WithAlpha( 125 );

					var screenPos = Scene.WorldToScreen( e.BoundingBox.Center );
					if ( screenPos.Z <= 0 ) continue;

					var size = paint.MeasureText( e.ClassName );
					var padding = 5;
					var rect = new SKRect( screenPos.X - padding, screenPos.Y - 8 - padding, screenPos.X + size + padding, screenPos.Y + padding );

					if ( (HoveredEntity == null || HoveredEntity == e) && rect.Contains( mousePosition ) )
					{
						HoveredEntity = e;
						paint.Color = SKColors.IndianRed;
					}

					if ( e.IsSelected )
					{
						paint.Color = SKColors.DodgerBlue;
					}

					canvas.DrawRoundRect( rect, 4, 4, paint );

					paint.Color = SKColors.White;
					canvas.DrawText( e.ClassName, screenPos.X, screenPos.Y, paint );
				}
			}
		}

	}
}
