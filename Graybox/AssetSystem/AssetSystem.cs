
using System.Collections.Generic;
using System.IO;

namespace Graybox;

public class AssetSystem
{

	List<AssetPackage> packages = new List<AssetPackage>();
	public IReadOnlyList<AssetPackage> Packages => packages;

	public void AddDirectory( string path )
	{
		try
		{
			var di = new DirectoryInfo( path );
			if ( !di.Exists ) return;

			if ( Contains( di ) )
				return;
		}
		catch
		{
			// logging needed
			return;
		}

		var package = new AssetPackage( this, new DirectoryInfo( path ) );
		package.ScanForAssets();
		packages.Add( package );
	}

	public IEnumerable<T> FindAssetsOfType<T>() where T : Asset
	{
		foreach ( var package in packages )
		{
			foreach ( var asset in package.Assets )
			{
				if ( asset is T t )
					yield return t;
			}
		}
	}

	public T FindAsset<T>( string relativePath ) where T : Asset
	{
		if ( string.IsNullOrEmpty( relativePath ) )
			return null;

		relativePath = NormalizePath( relativePath );
		foreach ( var package in packages )
		{
			foreach ( var asset in package.Assets )
			{
				if ( asset is T t )
				{
					if ( asset.RelativePath == relativePath )
						return t;
				}
			}
		}
		return null;
	}

	public Asset FindAsset( string relativePath )
	{
		if ( string.IsNullOrEmpty( relativePath ) )
			return null;

		relativePath = NormalizePath( relativePath );
		foreach ( var package in packages )
		{
			foreach ( var asset in package.Assets )
			{
				if ( asset.RelativePath == relativePath )
					return asset;
			}
		}
		return null;
	}

	bool Contains( DirectoryInfo di )
	{
		foreach ( var pkg in packages )
		{
			if ( pkg.Directory.FullName.Equals( di.FullName, System.StringComparison.OrdinalIgnoreCase ) )
				return true;
		}
		return false;
	}

	public static string NormalizePath( string input )
	{
		return input.Replace( "\\", "/" ).TrimStart( '/' ).ToLower();
	}

}
