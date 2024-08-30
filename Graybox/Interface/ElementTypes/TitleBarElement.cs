
using SkiaSharp;

namespace Graybox.Interface
{
	public class TitleBarElement : UIElement
	{

		public string Title
		{
			get => TitleElement.Text;
			set => TitleElement.Text = value;
		}

		TextElement TitleElement;

		public TitleBarElement()
		{
			MaxHeight = 24;
			MinHeight = 24;
			Width = Length.Percent( 100 );
			Shrink = 1;
			BackgroundColor = Theme.Background.Darken( .05f );
			//BackgroundHoverColor = Theme.Background.Darken( .15f );
			BorderBottomWidth = 1;
			BorderColor = SKColors.Black;
			Direction = FlexDirection.Row;
			AlignItems = FlexAlign.Center;
			PaddingRight = 4;
			PaddingLeft = 4;

			TitleElement = Add( new TextElement( "Some Window" ) );
			AddGrow();

			var minimizeBtn = Add( new ButtonElement.Clear() { Icon = MaterialIcons.HorizontalRule, MinWidth = 20, MinHeight = 20 } );
			var maximizeBtn = Add( new ButtonElement.Clear() { Icon = MaterialIcons.CropSquare, MinWidth = 20, MinHeight = 20 } );
			var closeBtn = Add( new ButtonElement.Clear() { Icon = MaterialIcons.Close, MinWidth = 20, MinHeight = 20 } );

			minimizeBtn.OnMouseDown = ( e ) => Window.Minimize();
			maximizeBtn.OnMouseDown = ( e ) => Window.Maximize();
			closeBtn.OnMouseDown = ( e ) => Window.Close();

			foreach ( var el in Children )
			{
				if ( !(el is ButtonElement btn) ) continue;

				btn.Color = Theme.DimButtonForeground;
				btn.HoverColor = Theme.ButtonBackgroundHover;
			}
		}

		protected override void DragBegin( ref InputEvent e )
		{
			base.DragBegin( ref e );

			lastDragPoint = ToScreen( new SKPoint( e.LocalMousePosition.X, e.LocalMousePosition.Y ) );
		}

		SKPoint lastDragPoint;
		protected override void DragMove( ref InputEvent e )
		{
			base.DragMove( ref e );

			var curDragPoint = ToScreen( new SKPoint( e.LocalMousePosition.X, e.LocalMousePosition.Y ) );
			var delta = curDragPoint - lastDragPoint;
			lastDragPoint = curDragPoint;

			Window?.Translate( delta.X, delta.Y );
		}

	}
}
