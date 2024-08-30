using Graybox.GameSystem;

namespace Graybox.Bunnyhop
{
	internal class Program
	{
		static void Main( string[] args )
		{
			using ( var window = new GrayboxGameWindow( new BunnyhopGame() ) )
			{
				window.Run();
			}
		}
	}
}
