
using SkiaSharp;
using System;

namespace Graybox.Interface
{
	public class SliderElement : UIElement
	{

		public Action<float> OnValueChanged;

		public float Value { get; set; } = 10f;
		public float MinValue { get; set; } = 0f;
		public float MaxValue { get; set; } = 100f;

		SKRect sliderTrackRect;
		SKRect sliderHandleRect;
		bool draggingSlider;
		bool draggingTrack;
		bool hoveringSlider;
		bool hoveringTrack;
		TextElement inputBox;

		public SliderElement()
		{
			MinHeight = 24;
			AlignItems = FlexAlign.FlexEnd;
			JustifyContent = FlexJustify.Center;

			inputBox = Add( new TextElement()
			{
				//Editable = true,
				//Selectable = true,
				BackgroundColor = Theme.InputBackground,
				BackgroundHoverColor = Theme.InputBackgroundHover,
				Color = Theme.InputForeground,
				MinWidth = 40,
				MaxWidth = 40,
				Centered = true,
				Text = Value.ToString( "0.00" )
			} );
		}

		protected override void Update()
		{
			base.Update();

			Cursor = (hoveringSlider || hoveringTrack || HasMouseCapture) ? CursorTypes.Pointer : CursorTypes.Default;
		}

		protected override void Paint( SKCanvas canvas )
		{
			base.Paint( canvas );

			var marginRight = inputBox.MarginRect.Width + 4;
			var trackColor = Theme.InputBackgroundHover;

			if ( ( hoveringTrack && !hoveringSlider ) || draggingTrack )
			{
				trackColor = trackColor.Lighten( .15f );
			}

			sliderTrackRect = new SKRect( BoxRect.Left + 10, BoxRect.MidY - 4, BoxRect.Right - 10 - marginRight, BoxRect.MidY + 4 );
			using ( var paint = new SKPaint { Color = trackColor } )
			{
				canvas.DrawRect( sliderTrackRect, paint );
			}

			var handleColor = Theme.ButtonBackground;
			var handleX = sliderTrackRect.Left + (Value - MinValue) / (MaxValue - MinValue) * sliderTrackRect.Width;
			sliderHandleRect = new SKRect( handleX - 8, BoxRect.MidY - 8, handleX + 8, BoxRect.MidY + 8 );

			if ( draggingSlider )
			{
				sliderHandleRect.Inflate( 2, 2 );
				handleColor = Theme.ButtonBackgroundHover;
			}
			else if ( hoveringSlider )
			{
				handleColor = Theme.ButtonBackgroundHover;
			}

			using ( var paint = new SKPaint { Color = handleColor } )
			{
				canvas.DrawRoundRect( sliderHandleRect, new SKSize( 2, 2 ), paint );
			}
		}

		protected override void MouseExit( ref InputEvent e )
		{
			base.MouseExit( ref e );

			hoveringSlider = false;
		}

		protected override void MouseMove( ref InputEvent e )
		{
			base.MouseMove( ref e );

			hoveringSlider = sliderHandleRect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
			hoveringTrack = sliderTrackRect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
		}

		protected override void MouseDown( ref InputEvent e )
		{
			base.MouseDown( ref e );

			draggingTrack = sliderTrackRect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
			draggingSlider = draggingTrack || sliderHandleRect.Contains( e.LocalMousePosition.X, e.LocalMousePosition.Y );
			if ( draggingSlider )
			{
				UpdateSliderValue( e.LocalMousePosition.X );
				CaptureMouse();
			}
		}

		protected override void MouseUp( ref InputEvent e )
		{
			base.MouseUp( ref e );

			draggingSlider = false;
			draggingTrack = false;
			ReleaseMouse();
		}

		protected override void DragMove( ref InputEvent e )
		{
			base.DragMove( ref e );

			if ( draggingSlider )
			{
				UpdateSliderValue( e.LocalMousePosition.X );
			}
		}

		private void UpdateSliderValue( float posX )
		{
			if ( posX < sliderTrackRect.Left ) posX = sliderTrackRect.Left;
			if ( posX > sliderTrackRect.Right ) posX = sliderTrackRect.Right;

			Value = MinValue + (posX - sliderTrackRect.Left) / sliderTrackRect.Width * (MaxValue - MinValue);

			inputBox.Text = Value.ToString( "0.00" );
			OnValueChanged?.Invoke( Value );
		}

		protected override int GetPaintState()
		{
			return HashCode.Combine( base.GetPaintState(), Value, draggingSlider, hoveringSlider, hoveringTrack, draggingTrack );
		}

	}
}
