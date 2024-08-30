
using SkiaSharp;

namespace Graybox.Interface
{
	public interface IWindow
	{

		void Close();
		void CreateWindow( WindowOptions options, UIElement contents );
		SKPoint ToScreen( SKPoint local );
		SKRect ToScreen( SKRect local );
		void Translate( float x, float y );
		void Minimize();
		void Maximize();
		void Invalidate( UIElement element );
		void ShowTooltip( string text );
		void HideTooltip();
		void SetClipboard( string text );
		string GetClipboard();

	}
}
