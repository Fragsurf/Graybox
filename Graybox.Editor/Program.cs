
using System.Globalization;
using System.Runtime.Versioning;

namespace Graybox.Editor;

[SupportedOSPlatform( "windows7.0" )]
static class Program
{
	[STAThread]
	static void Main()
	{
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		AppDomain.CurrentDomain.UnhandledException += UnhandledException;

		using ( var win = new EditorWindow() )
		{
			win.Run();
		}
	}

	private static void UnhandledException( object sender, UnhandledExceptionEventArgs args )
	{
		LogException( (Exception)args.ExceptionObject );
	}

	private static void ThreadException( object sender, ThreadExceptionEventArgs args )
	{
		LogException( args.Exception );
	}

	private static void LogException( Exception ex )
	{
		//Debug.LogException(ex);
	}
}
