
namespace Graybox.Providers.Map;

public abstract class MapProvider
{
	private static readonly List<MapProvider> RegisteredProviders;

	public static string warnings;

	static MapProvider()
	{
		RegisteredProviders = new List<MapProvider>();
	}

	public static void Register( MapProvider provider )
	{
		RegisteredProviders.Add( provider );
	}

	public static void Deregister( MapProvider provider )
	{
		RegisteredProviders.Remove( provider );
	}

	public static void DeregisterAll()
	{
		RegisteredProviders.Clear();
	}

	public static DataStructures.MapObjects.Map GetMapFromFile( string fileName )
	{
		if ( !File.Exists( fileName ) ) throw new ProviderException( "The supplied file doesn't exist." );
		MapProvider provider = RegisteredProviders.FirstOrDefault( p => p.IsValidForFileName( fileName ) );
		if ( provider != null )
		{
			warnings = "";
			return provider.GetFromFile( fileName );
		}
		throw new ProviderNotFoundException( "No map provider was found for this file." );
	}

	public static void SaveMapToFile( string filename, DataStructures.MapObjects.Map map, AssetSystem assetSystem = null )
	{
		MapProvider provider = RegisteredProviders.FirstOrDefault( p => p.IsValidForFileName( filename ) );
		if ( provider != null )
		{
			provider.SaveToFile( filename, map, assetSystem );
			return;
		}
		throw new ProviderNotFoundException( "No map provider was found for this file format." );
	}

	protected virtual DataStructures.MapObjects.Map GetFromFile( string filename )
	{
		using ( FileStream strm = new FileStream( filename, FileMode.Open, FileAccess.Read ) )
		{
			return GetFromStream( strm );
		}
	}

	protected virtual void SaveToStream( Stream stream, DataStructures.MapObjects.Map map, AssetSystem assetSystem ) { }
	protected virtual void SaveToFile( string filename, DataStructures.MapObjects.Map map, AssetSystem assetSystem )
	{
		using ( FileStream strm = new FileStream( filename, FileMode.Create, FileAccess.Write ) )
		{
			SaveToStream( strm, map, assetSystem );
		}
	}

	protected virtual DataStructures.MapObjects.Map GetFromStream( Stream stream ) => throw new NotImplementedException();
	protected virtual bool IsValidForFileName( string filename ) => false;

}
