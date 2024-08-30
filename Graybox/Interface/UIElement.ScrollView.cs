
using SkiaSharp;
using System;

namespace Graybox.Interface
{
	public partial class UIElement
	{

		SKPoint scrollVelocity;
		SKPoint scrollPosition;
		public SKPoint ScrollPosition
		{
			get => scrollPosition;
			set
			{
				var minY = -CalculateChildrenHeight() + PaddedRect.Height;
				if ( value.Y < minY ) value.Y = minY;
				if ( value.Y > 0 ) value.Y = 0;
				scrollPosition = value;
			}
		}

		public bool SmoothScroll { get; set; }

		void AddScrollVelocity( float scrollAmount )
		{
			if ( SmoothScroll )
			{
				var velocity = scrollVelocity;
				velocity.Y += scrollAmount * .1f;
				scrollVelocity = velocity;
			}
			else
			{
				var scrollPos = ScrollPosition;
				scrollPos.Y += scrollAmount;
				ScrollPosition = scrollPos;
				Refresh();
			}
		}

		void UpdateScrollVelocity()
		{
			if ( !SmoothScroll ) return;

			if ( scrollVelocity.Y != 0 )
			{
				var dampFactor = 0.75f; 
				var scrollPos = ScrollPosition;
				scrollPos.Y += scrollVelocity.Y;
				ScrollPosition = scrollPos;
				scrollVelocity.Y *= dampFactor;

				if ( Math.Abs( scrollVelocity.Y ) < 0.1f )
					scrollVelocity.Y = 0;

				Refresh();
			}
		}

	}
}
