
using SkiaSharp;

namespace Graybox.Interface
{
	public static class Theme
	{

		// Global

		public static SKColor Background { get; set; } = new SKColor( 20, 20, 20 );
		public static SKColor Foreground { get; set; } = new SKColor( 225, 225, 225 );

		// Forms

		public static SKColor InputBackground { get; set; } = new SKColor( 20, 20, 20 );
		public static SKColor InputBackgroundHover { get; set; } = new SKColor( 35, 35, 35 );
		public static SKColor InputForeground { get; set; } = new SKColor( 215, 215, 215 );
		public static SKColor InputBorderColor { get; set; } = new SKColor( 62, 62, 62 );
		public static float InputBorderWidth { get; set; } = 1.0f;
		public static float InputBorderRadius { get; set; } = 4;

		// Containers

		public static SKColor ContainerBackground { get; set; } = new SKColor( 36, 36, 36 );
		public static SKColor ContainerForeground { get; set; } = new SKColor( 225, 225, 225 );

		// Buttons

		public static SKColor ButtonBackground { get; set; } = new SKColor( 74, 118, 188 );
		public static SKColor ButtonBackgroundHover { get; set; } = new SKColor( 96, 135, 196 );
		public static SKColor DimButtonBackground { get; set; } = new SKColor( 64, 64, 64 );
		public static SKColor DimButtonBackgroundHover { get; set; } = new SKColor( 87, 87, 87 );
		public static SKColor DimButtonForeground { get; set; } = new SKColor( 255, 255, 255 );
		public static SKColor ButtonForeground { get; set; } = new SKColor( 255, 255, 255 );
		public static float ButtonBorderRadius { get; set; } = 6f;

		// Popups

		public static SKColor PopupBackgroundColor { get; set; } = new SKColor( 36, 36, 36 );
		public static SKColor PopupBorderColor { get; set; } = new SKColor( 62, 62, 62 );
		public static float PopupBorderWidth { get; set; } = 1.0f;
		public static float PopupBorderRadius { get; set; } = 4.0f;

	}
}
