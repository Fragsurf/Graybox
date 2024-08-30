
using System.Diagnostics;

namespace Graybox;

public static class EditorUtils
{

	public static void OpenWebsite( string url )
	{
		try
		{
			if ( !url.StartsWith( "http://" ) && !url.StartsWith( "https://" ) )
			{
				url = "https://" + url;  
			}

			var psi = new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			};

			Process.Start( psi );
		}
		catch ( Exception ex )
		{
			Debug.LogWarning( $"Error opening URL: {ex.Message}" );
		}
	}

}
