
using System.Text.Json;

namespace Graybox.Editor;

internal static class EditorPrefs
{

	public static string Folder => Path.Combine( Environment.CurrentDirectory, "prefs" );
	public static string DefaultLayoutFile => Path.Combine( Folder, "default.layout" );

	public static bool GridSnapEnabled
	{
		get => EditorCookie.Get( "editor.gridsnap", true );
		set => EditorCookie.Set( "editor.gridsnap", value );
	}

	public static int GridSize
	{
		get => EditorCookie.Get( "editor.gridsize", 16 );
		set => EditorCookie.Set( "editor.gridsize", value );
	}

	public static int AngleSnap
	{
		get => EditorCookie.Get( "editor.anglesnap", 5 );
		set => EditorCookie.Set( "editor.anglesnap", value );
	}

	static EditorPrefs()
	{
		if ( !Directory.Exists( Folder ) )
		{
			Directory.CreateDirectory( Folder );
		}
	}

	public static void Write( string fileName, object obj )
	{
		try
		{
			var contents = JsonSerializer.Serialize( obj );
			Write( fileName, contents );
		}
		catch
		{

		}
	}

	public static void Write( string fileName, string content )
	{
		File.WriteAllText( Path.Combine( Folder, fileName ), content );
	}

	public static string Read( string fileName )
	{
		if ( File.Exists( Path.Combine( Folder, fileName ) ) )
		{
			return File.ReadAllText( Path.Combine( Folder, fileName ) );
		}
		return string.Empty;
	}

	public static T Read<T>( string fileName, T defaultIfNull )
	{
		var json = Read( fileName );
		if ( string.IsNullOrEmpty( json ) )
		{
			return defaultIfNull;
		}

		try
		{
			return JsonSerializer.Deserialize<T>( json );
		}
		catch
		{
			return defaultIfNull;
		}
	}

	public static void DecreaseGridSize()
	{
		var sz = GridSize.NearestPowerOfTwo() / 2;
		sz = Math.Clamp( sz, 1, 512 );

		GridSize = sz;
	}

	public static void IncreaseGridSize()
	{
		var sz = GridSize.NearestPowerOfTwo() * 2;
		sz = Math.Clamp( sz, 1, 512 );

		GridSize = sz;
	}

}
