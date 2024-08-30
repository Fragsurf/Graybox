
using SkiaSharp;
using System;
using System.Text.RegularExpressions;

namespace Graybox.Interface
{
	public partial class TextElement
	{

		protected override int DragThreshold => Editable ? 0 : base.DragThreshold;

		public bool Selectable { get; set; }
		public bool Editable { get; set; }

		int selectionStart;
		protected int SelectionStart
		{
			get => selectionStart;
			set
			{
				selectionStart = value;
				EnsureCaret();
			}
		}

		int caretIndex;
		protected int CaretIndex
		{
			get => caretIndex;
			set
			{
				caretIndex = value;
				EnsureCaret();
				RefreshCaret();
			}
		}

		protected void EnsureCaret()
		{
			if ( caretIndex < 0 ) caretIndex = 0;
			if ( caretIndex >= Text?.Length ) caretIndex = Text.Length;
			if ( selectionStart < 0 ) selectionStart = 0;
			if ( selectionStart >= Text?.Length ) selectionStart = Text.Length;
		}

		bool CaretVisible = true;
		DateTime LastBlinkTime = DateTime.Now;
		const int BlinkRate = 500;

		protected override void DragMove( ref InputEvent e )
		{
			base.DragMove( ref e );

			if ( Selectable )
			{
				SetCaretIndex( HitTest( e.LocalMousePosition.X, e.LocalMousePosition.Y ), true );
				e.Handled = true;
			}
		}

		protected override void MouseDown( ref InputEvent e )
		{
			base.MouseDown( ref e );

			if ( e.Button == MouseButton.Left && Selectable )
			{
				ClearSelection();
				SetCaretIndex( HitTest( e.LocalMousePosition.X, e.LocalMousePosition.Y ), e.Shift );
				e.Handled = true;
			}
		}

		protected override void KeyDown( ref InputEvent e )
		{
			base.KeyDown( ref e );

			if ( !Editable ) return;
			if ( !IsFocused ) return;

			e.Handled = true;

			switch ( e.Key )
			{
				case Key.Left:
					SetCaretIndex( MoveCaretLeft( e.Control ), e.Shift );
					return;
				case Key.Right:
					SetCaretIndex( MoveCaretRight( e.Control ), e.Shift );
					return;
				case Key.Backspace:
					if ( SelectionStart != CaretIndex )
					{
						DeleteSelection();
						return;
					}
					if ( CaretIndex == 0 ) return;
					var jumpPoint = MoveCaretLeft( e.Control );
					Text = Text.Remove( jumpPoint, CaretIndex - jumpPoint );
					SetCaretIndex( jumpPoint, false );
					return;
				case Key.Delete:
					if ( SelectionStart != CaretIndex )
					{
						DeleteSelection();
						return;
					}
					if ( CaretIndex == 0 ) return;
					var delJumpPoint = MoveCaretRight( e.Control );
					Text = Text.Remove( CaretIndex, delJumpPoint - CaretIndex );
					return;
			}

			if ( e.Modifiers == KeyModifier.Control )
			{
				switch ( e.KeyChar )
				{
					case 'A':
						SelectionStart = Text.Length;
						CaretIndex = 0;
						return;
					case 'X':
						Window.SetClipboard( GetSelectedText() );
						DeleteSelection();
						OnCut();
						return;
					case 'C':
						Window.SetClipboard( GetSelectedText() );
						OnCopy();
						return;
					case 'V':
						var clipbaord = Window.GetClipboard();
						if ( !string.IsNullOrEmpty( clipbaord ) )
						{
							InsertText( clipbaord );
							OnPaste();
						}
						return;
				}
			}

			if ( !char.IsControl( e.KeyChar ) && !char.IsSurrogate( e.KeyChar ) )
			{
				DeleteSelection();
				Text = Text.Insert( CaretIndex, e.KeyChar.ToString() );
				SetCaretIndex( CaretIndex + 1, false );
			}
		}

		protected virtual void OnCut() { }
		protected virtual void OnCopy() { }
		protected virtual void OnPaste() { }

		protected override void Unfocused()
		{
			base.Unfocused();

			ClearSelection();
		}

		protected override void Update()
		{
			base.Update();

			if ( !IsFocused ) return;
			if ( !Editable ) return;

			if ( (DateTime.Now - LastBlinkTime).TotalMilliseconds > BlinkRate )
			{
				CaretVisible = !CaretVisible;
				LastBlinkTime = DateTime.Now;
				Refresh();
			}
		}

		public void ClearSelection()
		{
			CaretIndex = 0;
			SelectionStart = 0;
		}

		void PaintSelection( SKCanvas canvas )
		{
			if ( TryGetSelectionRect( out var selectionRect ) )
			{
				using ( var paint = new SKPaint() )
				{
					paint.Color = SKColors.DodgerBlue;
					canvas.DrawRect( selectionRect, paint );
				}
			}

			if ( IsFocused && CaretVisible && Editable )
			{
				var textUpToCaret = Text.Substring( 0, CaretIndex );
				var textSize = MeasureText( textUpToCaret );
				var textFullSize = MeasureText( Text );

				float posX;
				if ( Centered )
				{
					// Adjust caret position when text is centered
					var startOfText = (PaddedRect.Width - textFullSize.Width) / 2;
					posX = PaddedRect.Left + startOfText + textSize.Width;
				}
				else
				{
					posX = PaddedRect.Left + textSize.Width;
				}

				using ( var paint = new SKPaint() )
				{
					paint.Color = SKColors.White;
					paint.IsStroke = true;
					paint.StrokeWidth = 2;

					var posY = PaddedRect.Top;
					canvas.DrawLine( posX, posY, posX, PaddedRect.Bottom, paint );
				}
			}
		}

		protected bool TryGetSelectionRect( out SKRect rect )
		{
			rect = default;

			if ( !Selectable || SelectionStart == CaretIndex || string.IsNullOrEmpty( Text ) )
				return false;

			var start = Math.Min( SelectionStart, CaretIndex );
			var end = Math.Max( SelectionStart, CaretIndex );

			var startPos = MeasureText( Text.Substring( 0, start ) ).Width;
			var endPos = MeasureText( Text.Substring( 0, end ) ).Width;

			if ( Centered )
			{
				// Adjust selection rectangle when text is centered
				var fullTextWidth = MeasureText( Text ).Width;
				var startOfText = (PaddedRect.Width - fullTextWidth) / 2;
				startPos += startOfText;
				endPos += startOfText;
			}

			var margin = FinalTextMargin();
			var paddedRect = PaddedRect;

			rect = new SKRect(
				paddedRect.Left + margin.Left + startPos,  // Add the left margin to the starting position
				paddedRect.Top + margin.Top,               // Optionally add the top margin if needed for vertical alignment
				paddedRect.Left + margin.Right + endPos,   // Add the right margin to the ending position
				paddedRect.Bottom - margin.Bottom          // Optionally subtract the bottom margin if needed for vertical alignment
			);

			// Ensure that the rectangle does not extend outside the padded area
			rect.Left = Math.Max( rect.Left, paddedRect.Left );
			rect.Right = Math.Min( rect.Right, paddedRect.Right );
			rect.Top = Math.Max( rect.Top, paddedRect.Top );
			rect.Bottom = Math.Min( rect.Bottom, paddedRect.Bottom );

			return true;
		}


		protected string GetSelectedText()
		{
			if ( SelectionStart == CaretIndex ) return string.Empty;

			int start = Math.Min( SelectionStart, CaretIndex );
			int end = Math.Max( SelectionStart, CaretIndex );
			int length = end - start;

			return Text.Substring( start, length );
		}

		protected void InsertText( string text )
		{
			if ( string.IsNullOrEmpty( text ) ) return;
			if ( !Editable ) return;

			DeleteSelection();
			Text = Text.Insert( CaretIndex, text );
			SetCaretIndex( CaretIndex + text.Length, false );
		}

		protected int MoveCaretLeft( bool jump )
		{
			var currentIndex = CaretIndex;
			if ( currentIndex <= 1 )
				return 0;

			if ( jump )
			{
				var textToLeft = Text.Substring( 0, currentIndex );
				var match = Regex.Match( textToLeft, @"\b\w+\b(?=[^\w]*$)" );
				if ( match.Success )
					return match.Index;
				else
					return 0;
			}

			return currentIndex - 1;
		}

		protected int MoveCaretRight( bool jump )
		{
			var currentIndex = CaretIndex;
			if ( currentIndex >= Text.Length - 1 )
				return Text.Length;

			if ( jump )
			{
				var textToRight = Text.Substring( currentIndex );
				var match = Regex.Match( textToRight, @"\b\w+\b" );
				if ( match.Success )
					return currentIndex + match.Index + match.Length;
				else
					return Text.Length;
			}

			return currentIndex + 1;
		}

		protected void DeleteSelection()
		{
			if ( !Editable ) return;
			if ( SelectionStart == CaretIndex ) return;

			int start = Math.Min( SelectionStart, CaretIndex );
			int end = Math.Max( SelectionStart, CaretIndex );
			int length = end - start;

			Text = Text.Remove( start, length );

			CaretIndex = start;
			SelectionStart = CaretIndex;

			RefreshCaret();
		}

		protected void SetCaretIndex( int index, bool maintainSelection )
		{
			CaretIndex = index;

			if ( !maintainSelection )
			{
				SelectionStart = CaretIndex;
			}
		}


		protected void RefreshCaret()
		{
			CaretVisible = true;
			LastBlinkTime = DateTime.Now;
		}

		protected void SkipCaret()
		{
			CaretVisible = false;
			LastBlinkTime = DateTime.Now + TimeSpan.FromMilliseconds( BlinkRate );
		}

		protected int HitTest( float x, float y )
		{
			var rect = new SKRect();
			rect.Left = 0;
			rect.Top = 0;
			rect.Right = PaddedRect.Width;
			rect.Bottom = PaddedRect.Height;

			rect.Left += TextMargin.Left;
			rect.Right -= TextMargin.Right;
			rect.Top += TextMargin.Top;
			rect.Bottom -= TextMargin.Bottom;

			return textBlock.HitTest( x, y, rect );
		}

	}
}
