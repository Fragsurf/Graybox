
using SkiaSharp;
using System;

namespace Graybox.Interface
{
	public partial class UIElement
	{

		public Action<InputEvent> OnMouseEnter;
		public Action<InputEvent> OnMouseExit;
		public Action<InputEvent> OnMouseDown;
		public Action<InputEvent> OnMouseUp;

		public bool IsPressed { get; private set; }
		public bool IsHovered { get; private set; }
		public bool IsFocused { get; private set; }
		public bool HasMouseCapture { get; private set; }
		public CursorTypes DesiredCursor { get; private set; }
		public UIElement FocusedElement { get; private set; }

		protected virtual void MouseEnter( ref InputEvent e ) { }
		protected virtual void MouseExit( ref InputEvent e ) { }
		protected virtual void MouseDown( ref InputEvent e ) { }
		protected virtual void MouseUp( ref InputEvent e ) { }
		protected virtual void MouseMove( ref InputEvent e ) { }
		protected virtual void DragBegin( ref InputEvent e ) { }
		protected virtual void DragMove( ref InputEvent e ) { }
		protected virtual void DragEnd( ref InputEvent e ) { }
		protected virtual void Focused() { }
		protected virtual void Unfocused() { }
		protected virtual void KeyDown( ref InputEvent e ) { }

		DateTime tooltipDecay;
		bool hasTooltip;

		void TransformToLocal( ref InputEvent e )
		{
			var localPos = ToLocal( new SKPoint( e.MousePosition.X, e.MousePosition.Y ) );
			e.LocalMousePosition = new ( localPos.X, localPos.Y );
		}

		internal void HandleMouseExit( ref InputEvent e )
		{
			TransformToLocal( ref e );
			if ( hasTooltip ) Window?.HideTooltip();
			hasTooltip = false;
			IsHovered = false;
			IsPressed = false;
			OnMouseExit?.Invoke( e );
			MouseEnter( ref e );

			if ( e.Handled ) return;

			Parent?.HandleMouseExit( ref e );
		}

		internal void HandleMouseEnter( ref InputEvent e )
		{
			TransformToLocal( ref e );
			IsHovered = true;
			OnMouseEnter?.Invoke( e );
			MouseExit( ref e );
			tooltipDecay = DateTime.Now;

			if ( e.Handled ) return;

			Parent?.HandleMouseEnter( ref e );
		}

		internal void HandleFocused()
		{
			IsFocused = true;
			Focused();
		}

		internal void HandleUnfocused()
		{
			IsFocused = false;
			hasTooltip = false;
			Unfocused();
		}

		internal void SetFocus( UIElement element )
		{
			if ( FocusedElement == element ) return;
			FocusedElement?.HandleUnfocused();
			FocusedElement = element;
			FocusedElement?.HandleFocused();
		}

		internal void HandleMouseDown( ref InputEvent e )
		{
			TransformToLocal( ref e );
			IsPressed = true;
			OnMouseDown?.Invoke( e );
			MouseDown( ref e );
			FindRoot()?.SetFocus( this );
			tooltipDecay = DateTime.Now;

			if ( e.Handled ) return;

			Parent?.HandleMouseDown( ref e );
		}

		internal void HandleMouseUp( ref InputEvent e )
		{
			TransformToLocal( ref e );
			IsPressed = false;
			OnMouseUp?.Invoke( e );
			MouseUp( ref e );
			tooltipDecay = DateTime.Now;

			if ( e.Handled ) return;

			Parent?.HandleMouseUp( ref e );
		}

		internal void HandleMouseMove( ref InputEvent e )
		{
			TransformToLocal( ref e );
			MouseMove( ref e );

			if ( e.Handled ) return;

			Parent?.HandleMouseMove( ref e );
		}

		internal void HandleScrollWheel( float scrollAmount )
		{
			AddScrollVelocity( scrollAmount );
		}

		internal void HandleKeyDown( ref InputEvent e )
		{
			KeyDown( ref e );
		}

		void HandleDragBegin( ref InputEvent e )
		{
			TransformToLocal( ref e );
			DragBegin( ref e );

			if ( e.Handled ) return;

			Parent?.HandleDragBegin( ref e );
		}

		void HandleDragMove( ref InputEvent e )
		{
			TransformToLocal( ref e );
			DragMove( ref e );

			if ( e.Handled ) return;

			Parent?.HandleDragMove( ref e );
		}

		void HandleDragEnd( ref InputEvent e )
		{
			TransformToLocal( ref e );
			DragEnd( ref e );

			if ( e.Handled ) return;

			Parent?.HandleDragEnd( ref e );
		}

		private UIElement lastHoveredElement;
		private UIElement FindTopMostHoveredElement( UIElement element, SKPoint screenPosition )
		{
			for ( int i = element.Children.Count - 1; i >= 0; i-- )
			{
				var child = element.Children[i];
				var adjustedPosition = screenPosition - child.scrollPosition;

				var worldRect = CalculateWorldRect( child, ScrollPosition );
				if ( worldRect.Contains( adjustedPosition ) )
				{
					var hoveredChild = FindTopMostHoveredElement( child, adjustedPosition );
					if ( hoveredChild != null && hoveredChild.PointerEvents == PointerEvents.All )
					{
						return hoveredChild;
					}
				}
			}

			if ( element.BoxRect.Contains( screenPosition ) && element.PointerEvents == PointerEvents.All )
			{
				return element;
			}

			return null;
		}

		public SKRect CalculateWorldRect( UIElement element, SKPoint parentScrollPosition )
		{
			return new SKRect(
				element.BoxRect.Left + parentScrollPosition.X - element.scrollPosition.X,
				element.BoxRect.Top + parentScrollPosition.Y - element.scrollPosition.Y,
				element.BoxRect.Right + parentScrollPosition.X - element.scrollPosition.X,
				element.BoxRect.Bottom + parentScrollPosition.Y - element.scrollPosition.Y
			);
		}

		public void ProcessKeyPressed( ref InputEvent e )
		{
			FocusedElement?.HandleKeyDown( ref e );
		}

		UIElement pressedElement;
		SKPoint pressedStartPoint;
		bool dragStarted;

		SKPoint ToLocal( SKPoint point )
		{
			return new SKPoint( point.X - BoxRect.Left, point.Y - BoxRect.Top ) - ScrollPosition;
		}

		bool IsAncestorOf( UIElement other )
		{
			if ( other == null ) return false;

			var parent = other;
			while ( parent != null )
			{
				if ( parent == this )
					return true;
				parent = parent.Parent;
			}
			return false;
		}

		UIElement FindCaptureElement( UIElement hoveredElement )
		{
			if ( MouseCaptureElement == null ) return hoveredElement;
			if ( MouseCaptureElement.IsAncestorOf( hoveredElement ) ) return hoveredElement;

			return MouseCaptureElement;
		}

		protected virtual int DragThreshold => 5;
		public void ProcessMouseInput( ref InputEvent e, bool isMousePressed = false )
		{
			var mpos = new SKPoint( e.MousePosition.X, e.MousePosition.Y );
			var hoveredElement = FindTopMostHoveredElement( this, mpos );

			if ( hoveredElement != null )
			{
				var localPos = hoveredElement.ToLocal( mpos );
				e.LocalMousePosition = new( localPos.X, localPos.Y );

				if ( e.MouseScroll.Y != 0 )
				{
					FindScrollableElement( this, mpos )?.HandleScrollWheel( e.MouseScroll.Y );
				}
			}

			if ( lastHoveredElement != hoveredElement )
			{
				lastHoveredElement?.HandleMouseExit( ref e );
				lastHoveredElement = hoveredElement;
				hoveredElement?.HandleMouseEnter( ref e );
			}

			var captureElement = FindCaptureElement( lastHoveredElement );
			captureElement?.HandleMouseMove( ref e );

			DesiredCursor = captureElement?.Cursor ?? CursorTypes.Default;

			if ( isMousePressed && e.Button == MouseButton.Left && captureElement != null )
			{
				captureElement.HandleMouseDown( ref e );
				pressedElement = captureElement;
				pressedStartPoint = mpos;
				dragStarted = false;
			}

			if ( isMousePressed && e.Button != MouseButton.Left && pressedElement != null )
			{
				if ( dragStarted )
				{
					var localPos = pressedElement.ToLocal( mpos );
					e.LocalMousePosition = new( localPos.X, localPos.Y );
					pressedElement.HandleDragEnd( ref e );
				}

				pressedElement.HandleMouseUp( ref e );
				pressedElement = null;
			}

			if ( pressedElement != null && e.Button == MouseButton.Left && !isMousePressed )
			{
				if ( !dragStarted )
				{
					var dist = (pressedStartPoint - mpos).Length;
					if ( dist > DragThreshold )
					{
						dragStarted = true;
						var localPos = pressedElement.ToLocal( mpos );
						e.LocalMousePosition = new( localPos.X, localPos.Y );
						pressedElement.HandleDragBegin( ref e );
					}
				}
				else
				{
					var localPos = pressedElement.ToLocal( mpos );
					e.LocalMousePosition = new( localPos.X, localPos.Y );
					pressedElement.HandleDragMove( ref e );
				}
			}
		}

		private bool IsScrollable()
		{
			if ( Overflow != OverflowTypes.Scroll ) return false;
			if ( CalculateChildrenHeight() < PaddedRect.Height ) return false;

			return true;
		}

		private static UIElement FindScrollableElement( UIElement element, SKPoint screenPosition )
		{
			var adjustedPosition = new SKPoint( screenPosition.X - element.scrollPosition.X, screenPosition.Y - element.scrollPosition.Y );

			foreach ( var child in element.Children )
			{
				var scrollableChild = FindScrollableElement( child, adjustedPosition );
				if ( scrollableChild != null && scrollableChild.IsScrollable() )
				{
					return scrollableChild;
				}
			}

			if ( element.IsScrollable() && element.BoxRect.Contains( screenPosition ) )
			{
				return element;
			}

			return null;
		}

		int lastPaintState;
		void CheckForChanges()
		{
			var newPaintState = GetPaintState();
			if ( newPaintState == lastPaintState ) return;

			lastPaintState = newPaintState;
			Refresh();
		}

		protected virtual int GetPaintState()
		{
			var result = 0;

			if ( BackgroundHoverColor != null )
				result = HashCode.Combine( result, IsHovered, BackgroundHoverColor );

			if ( BorderHoverColor != null )
				result = HashCode.Combine( result, IsHovered, BorderHoverColor );

			var scrollOffset = CalculateScrollOffset();
			result = HashCode.Combine( result, scrollOffset.X, scrollOffset.Y, ScrollPosition.X, ScrollPosition.Y );

			return result;
		}

	}
}
