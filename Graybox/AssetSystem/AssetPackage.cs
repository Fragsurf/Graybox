
using System.Collections.Generic;
using System.IO;

namespace Graybox;

public class AssetPackage
{

	public readonly DirectoryInfo Directory;
	public readonly AssetSystem AssetSystem;

	List<Asset> assets = new List<Asset>();
	public IReadOnlyList<Asset> Assets => assets;

	public string Name => Directory.Name;

	public AssetPackage( AssetSystem system, DirectoryInfo directory )
	{
		AssetSystem = system;
		Directory = directory;
	}
	
	public void ScanForAssets()
	{
		assets.Clear();

		var files = Directory.GetFiles( "*.*", SearchOption.AllDirectories );

		foreach ( var file in files )
		{
			var asset = Asset.CreateAsset( AssetSystem, this, file );
			if ( asset == null ) continue;

			assets.Add( asset );
		}
	}

}
