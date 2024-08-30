
using Newtonsoft.Json;

namespace Graybox.Editor;

internal static class EditorCookie
{

	private static readonly string FilePath = Path.Combine( EditorPrefs.Folder, "editorCookies.json" );

	static EditorCookie()
	{
		if ( !Directory.Exists( EditorPrefs.Folder ) )
		{
			Directory.CreateDirectory( EditorPrefs.Folder );
		}
	}

	public static bool Exists( string key )
	{
		var data = ReadData();
		return data.ContainsKey( key );
	}

	public static T Get<T>( string key, T defaultValue )
	{
		var data = ReadData();
		if ( data.TryGetValue( key, out object value ) )
		{
			return JsonConvert.DeserializeObject<T>( value.ToString() );
		}
		return defaultValue;
	}

	public static T Get<T>( string key )
	{
		var data = ReadData();
		if ( data.TryGetValue( key, out object value ) )
		{
			return JsonConvert.DeserializeObject<T>( value.ToString() );
		}
		return default;
	}

	public static void Set<T>( string key, T value )
	{
		var data = ReadData();
		string jsonValue = JsonConvert.SerializeObject( value );
		data[key] = jsonValue;
		WriteData( data );
	}

	private static Dictionary<string, object> ReadData()
	{
		if ( !File.Exists( FilePath ) )
		{
			return new Dictionary<string, object>();
		}

		string json = File.ReadAllText( FilePath );
		return JsonConvert.DeserializeObject<Dictionary<string, object>>( json ) ?? new Dictionary<string, object>();
	}

	private static void WriteData( Dictionary<string, object> data )
	{
		string json = JsonConvert.SerializeObject( data, Formatting.Indented );
		File.WriteAllText( FilePath, json );
	}
}
