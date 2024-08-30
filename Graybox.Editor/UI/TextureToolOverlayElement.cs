
using Graybox.Editor.Documents;
using Graybox.Editor.Tools;
using Graybox.Interface;
using SkiaSharp;

namespace Graybox.Editor.UI
{
	internal class AssetThumbnailElement : UIElement
	{

		SKImage Image;

		public AssetThumbnailElement( Asset asset )
		{
			Image = asset?.GetSKThumbnail();
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			if ( Image == null ) return;

			using ( var paint = new SKPaint() )
			{
				var rect = PaddedRect;
				canvas.DrawImage( Image, rect, paint );
			}
		}
	}

	internal class TextureToolOverlayElement : UIElement
	{

		class TexturePaletteBox : UIElement
		{

			TextureAsset Texture;
			TextElement AssetLabel;
			UIElement AssetThumbnail;
			ButtonElement FindButton;

			public TexturePaletteBox( int slot )
			{
				Direction = FlexDirection.Row;
				JustifyContent = FlexJustify.Center;
				AlignItems = FlexAlign.Center;

				FindButton = Add( new ButtonElement.Clear()
				{
					Icon = MaterialIcons.ChevronLeft,
					Width = 20,
					Height = 20,
					FontSize = 20,
					HoverColor = Interface.Theme.ButtonBackgroundHover,
					OnMouseDown = ( e ) =>
					{
						if ( Texture is TextureAsset texture )
						{
							EventSystem.Publish( EditorEvents.TextureSelected, texture );
						}
					}
				} );

				AssetThumbnail = Add( new UIElement()
				{
					Width = 20,
					Height = 20,
					Shrink = 0,
					Padding = 1,
					MarginRight = 4,
					Cursor = CursorTypes.Pointer,
					BackgroundColor = Interface.Theme.ContainerBackground,
					BackgroundHoverColor = Interface.Theme.Foreground,
					OnPaint = ( e ) =>
					{
						var img = Texture?.GetSKThumbnail();
						if ( img == null ) return;

						e.Canvas.DrawImage( img, e.Element.PaddedRect );
					},
					OnMouseDown = ( e ) =>
					{
						throw new System.NotImplementedException();
						//using ( var ab = new AssetBrowserDialog() )
						//{
						//	ab.Browser.TypeFilter = AssetTypes.Texture;
						//	ab.OnAssetPicked = x =>
						//	{
						//		if ( x is TextureAsset texture )
						//		{
						//			SetAsset( texture );
						//		}
						//	};
						//	ab.ShowDialog();
						//}
					}
				} );
				AssetLabel = Add( new TextElement()
				{
					Text = "None Selected",
					Grow = 1,
					Shrink = 0
				} );
			}

			protected override void MouseDown( ref InputEvent e )
			{
				base.MouseDown( ref e );

			}

			void SetAsset( TextureAsset asset )
			{
				Texture = asset;
				if ( Texture == null ) return;

				AssetLabel.Text = asset.Name;
			}

		}

		TextureTool Tool;

		public TextureToolOverlayElement( TextureTool tool )
		{
			Tool = tool;
			Width = Length.Percent( 100 );
			Height = Length.Percent( 100 );
			Direction = FlexDirection.Row;

			var selectedImage = Add( new UIElement()
			{
				OnPaint = PaintSelectedTexture,
				Width = 82,
				Height = 82,
				Padding = 2,
				Cursor = CursorTypes.Pointer,
				BackgroundColor = Graybox.Interface.Theme.Background,
				BackgroundHoverColor = Graybox.Interface.Theme.Foreground,
				OnMouseDown = x =>
				{
					OpenTexturePicker();
					x.Handled = true;
				}
			} );
			selectedImage.Shrink = 0;

			var middleContainer2 = Add( new UIElement() );
			middleContainer2.Width = 70;
			middleContainer2.MarginLeft = 10;
			middleContainer2.Shrink = 0;
			middleContainer2.JustifyContent = FlexJustify.Center;
			var applyBtn = middleContainer2.Add( new ButtonElement.Dim( "Apply" ) { MarginBottom = 2, BorderRadius = 2, Shrink = 0 } );
			middleContainer2.Add( new ButtonElement.Dim( "Replace" ) { MarginBottom = 2, BorderRadius = 2, Shrink = 0 } );
			middleContainer2.Add( new ButtonElement.Dim( "Select" ) { BorderRadius = 2, Shrink = 0 } );

			applyBtn.OnMouseDown = x => Tool.ApplySelection();

			var leftContainer = Add( new UIElement() );
			leftContainer.Width = 200;
			leftContainer.MinWidth = 200;
			leftContainer.Grow = 0;
			leftContainer.Shrink = 0;
			leftContainer.MarginLeft = 10;
			leftContainer.Direction = FlexDirection.Column;
			leftContainer.JustifyContent = FlexJustify.Center;
			{
				var row1 = leftContainer.Add( new UIElement() { Direction = FlexDirection.Row, MarginBottom = 4 } );
				{
					row1.Add( new TextElement( "Scale" ) { MinWidth = 40 } );
					row1.AddGrow();
					row1.Add( new VectorEntryElement( true ) );
				}
				var row2 = leftContainer.Add( new UIElement() { Direction = FlexDirection.Row, MarginBottom = 4 } );
				{
					row2.Add( new TextElement( "Shift" ) { MinWidth = 40 } );
					row2.AddGrow();
					row2.Add( new VectorEntryElement( true ) );
				}
				var row3 = leftContainer.Add( new UIElement() { Direction = FlexDirection.Row, MarginBottom = 4 } );
				{
					row3.Add( new TextElement( "Rotation" ) { MinWidth = 40 } );
					row3.AddGrow();
					row3.Shrink = 1;
					row3.Grow = 0;
					row3.Add( new SliderElement() { Grow = 5, MinValue = 0, MaxValue = 360, Value = 0 } );
				}
			}

			var middleContainer = Add( new UIElement() );
			middleContainer.Width = 90;
			middleContainer.MarginLeft = 10;
			middleContainer.Shrink = 0;
			middleContainer.Add( new JustifyButtonBox() );

			var rightContainer = Add( new UIElement() );
			rightContainer.Grow = 1;
			rightContainer.Shrink = 0;
			rightContainer.MarginLeft = 10;
			rightContainer.Padding = 4;
			rightContainer.Direction = FlexDirection.Column;
			rightContainer.Wrap = WrapModes.Wrap;
			rightContainer.JustifyContent = FlexJustify.Center;
			{
				rightContainer.Add( new TexturePaletteBox( 1 ) { Height = 20, Grow = 1, Shrink = 0, MarginBottom = 1 } );
				rightContainer.Add( new TexturePaletteBox( 2 ) { Height = 20, Grow = 1, Shrink = 0, MarginBottom = 1 } );
				rightContainer.Add( new TexturePaletteBox( 3 ) { Height = 20, Grow = 1, Shrink = 0, MarginBottom = 1 } );
			}
		}

		protected override void MouseMove( ref InputEvent e )
		{
			base.MouseMove( ref e );

			e.Handled = true;
		}

		void OpenTexturePicker()
		{
			throw new System.NotImplementedException();
			//using ( var ab = new AssetBrowserDialog() )
			//{
			//	ab.Browser.TypeFilter = AssetTypes.Texture;
			//	ab.OnAssetPicked = x =>
			//	{
			//		if ( x is TextureAsset texture )
			//		{
			//			Mediator.Publish( EditorMediator.TextureSelected, texture );
			//		}
			//	};
			//	ab.ShowDialog();
			//}
		}

		void PaintSelectedTexture( UIElementPaintEvent e )
		{
			var image = DocumentManager.CurrentDocument?.SelectedTexture?.GetSKThumbnail();
			if ( image == null ) return;

			using ( var paint = new SKPaint() )
			{
				var rect = e.Element.PaddedRect;
				e.Canvas.DrawImage( image, rect, paint );
			}
		}

		class JustifyButtonBox : UIElement
		{

			RectHelper TopRect = new RectHelper();
			RectHelper LeftRect = new RectHelper();
			RectHelper RightRect = new RectHelper();
			RectHelper BottomRect = new RectHelper();
			RectHelper CenterRect = new RectHelper();
			RectHelper FitRect = new RectHelper();
			RectHelper FitXRect = new RectHelper();
			RectHelper FitYRect = new RectHelper();

			class RectHelper
			{
				public bool IsHovered;
				public bool IsPressed;
				public SKRect Rect;
				public Action MouseDown;
			}

			public JustifyButtonBox()
			{
				Width = Length.Percent( 100 );
				Height = Length.Percent( 100 );
				TopRect.MouseDown = () => Justify( TextureTool.JustifyMode.Top );
				LeftRect.MouseDown = () => Justify( TextureTool.JustifyMode.Left );
				RightRect.MouseDown = () => Justify( TextureTool.JustifyMode.Right );
				BottomRect.MouseDown = () => Justify( TextureTool.JustifyMode.Bottom );
				CenterRect.MouseDown = () => Justify( TextureTool.JustifyMode.Center );
				FitRect.MouseDown = () => Justify( TextureTool.JustifyMode.Fit );
				//FitXRect.MouseDown = () => Justify( TextureTool.JustifyMode.Fit );
				//FitYRect.MouseDown = () => Justify( TextureTool.JustifyMode.Fit );
			}

			void Justify( TextureTool.JustifyMode mode )
			{
				var tool = ToolManager.ActiveTool as TextureTool;
				if ( tool == null ) return;

				//tool.JustifySelection( mode, true );
			}

			protected override void MouseMove( ref InputEvent e )
			{
				base.MouseMove( ref e );

				TopRect.IsHovered = TopRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
				BottomRect.IsHovered = BottomRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
				LeftRect.IsHovered = LeftRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
				RightRect.IsHovered = RightRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
				CenterRect.IsHovered = CenterRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
				FitRect.IsHovered = FitRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
				FitXRect.IsHovered = FitXRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
				FitYRect.IsHovered = FitYRect.Rect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
			}

			protected override void MouseDown( ref InputEvent e )
			{
				base.MouseDown( ref e );

				if ( TopRect.IsHovered ) TopRect?.MouseDown?.Invoke();
				if ( BottomRect.IsHovered ) BottomRect?.MouseDown?.Invoke();
				if ( LeftRect.IsHovered ) LeftRect?.MouseDown?.Invoke();
				if ( RightRect.IsHovered ) RightRect?.MouseDown?.Invoke();
				if ( CenterRect.IsHovered ) CenterRect?.MouseDown?.Invoke();
				if ( FitRect.IsHovered ) FitRect?.MouseDown?.Invoke();
				if ( FitXRect.IsHovered ) FitXRect?.MouseDown?.Invoke();
				if ( FitYRect.IsHovered ) FitYRect?.MouseDown?.Invoke();
			}

			protected override void Paint( SKCanvas canvas )
			{
				base.Paint( canvas );

				float buttonSize = 22f;
				float centerX = BoxRect.MidX;
				float centerY = BoxRect.MidY;
				float top = BoxRect.Top;
				float bottom = BoxRect.Bottom;
				float right = BoxRect.Right;
				float left = BoxRect.Left;

				var buttonBackground = Graybox.Interface.Theme.DimButtonBackground;
				var buttonForeground = Graybox.Interface.Theme.DimButtonForeground;
				var buttonHovered = Graybox.Interface.Theme.DimButtonBackgroundHover;
				var round = new SKSize( 4, 4 );

				using ( var paint = new SKPaint { IsAntialias = true, TextSize = 12, TextAlign = SKTextAlign.Center, Color = SKColors.Black } )
				{
					float spacing = buttonSize - 5;

					TopRect.Rect = new SKRect( centerX - buttonSize / 2, centerY - spacing * 2, centerX + buttonSize / 2, centerY - spacing );
					paint.Color = TopRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( TopRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "T", TopRect.Rect.MidX, TopRect.Rect.MidY + paint.TextSize / 3, paint );

					BottomRect.Rect = new SKRect( centerX - buttonSize / 2, centerY + spacing, centerX + buttonSize / 2, centerY + spacing * 2 );
					paint.Color = BottomRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( BottomRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "B", BottomRect.Rect.MidX, BottomRect.Rect.MidY + paint.TextSize / 3, paint );

					LeftRect.Rect = new SKRect( centerX - spacing * 2, centerY - buttonSize / 2, centerX - spacing, centerY + buttonSize / 2 );
					paint.Color = LeftRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( LeftRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "L", LeftRect.Rect.MidX, LeftRect.Rect.MidY + paint.TextSize / 3, paint );

					RightRect.Rect = new SKRect( centerX + spacing, centerY - buttonSize / 2, centerX + spacing * 2, centerY + buttonSize / 2 );
					paint.Color = RightRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( RightRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "R", RightRect.Rect.MidX, RightRect.Rect.MidY + paint.TextSize / 3, paint );

					CenterRect.Rect = new SKRect( centerX - buttonSize / 2, centerY - buttonSize / 2, centerX + buttonSize / 2, centerY + buttonSize / 2 );
					paint.Color = CenterRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( CenterRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "C", CenterRect.Rect.MidX, CenterRect.Rect.MidY + paint.TextSize / 3, paint );

					FitRect.Rect = new SKRect( left + 2, top + 2, left + 28, top + 2 + buttonSize );
					paint.Color = FitRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( FitRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "FIT", FitRect.Rect.MidX, FitRect.Rect.MidY + paint.TextSize / 3, paint );

					float fitRectBottom = bottom - 1;
					float fitRectTop = fitRectBottom - buttonSize;

					FitXRect.Rect = new SKRect( left + 2, fitRectTop, left + buttonSize + 2, fitRectBottom );
					paint.Color = FitXRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( FitXRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "X", FitXRect.Rect.MidX, FitXRect.Rect.MidY + paint.TextSize / 3, paint );

					// New FitYRect button at the bottom right
					FitYRect.Rect = new SKRect( right - buttonSize - 2, fitRectTop, right - 2, fitRectBottom );
					paint.Color = FitYRect.IsHovered ? buttonHovered : buttonBackground;
					canvas.DrawRoundRect( FitYRect.Rect, round, paint );
					paint.Color = buttonForeground;
					canvas.DrawText( "Y", FitYRect.Rect.MidX, FitYRect.Rect.MidY + paint.TextSize / 3, paint );
				}
			}

		}

	}
}
