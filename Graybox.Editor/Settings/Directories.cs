
namespace Graybox.Editor.Settings;

public class Directories
{

	public static List<string> TextureDirs { get; set; }

	static Directories()
	{
		TextureDirs = new List<string>();
	}

	public static readonly string[] ModelExtensions = { "fbx", "x", "b3d", "glb", "gltf" };

	public static string GetModelPath( string filename )
	{
		foreach ( string dir in TextureDirs )
		{
			string dirSlash = dir;
			if ( dir.Last() != '/' && dir.Last() != '\\' )
			{
				dirSlash += "/";
			}
			foreach ( string ext in ModelExtensions )
			{
				string fullFilename = dirSlash + filename + "." + ext;
				if ( File.Exists( fullFilename ) )
				{
					return fullFilename;
				}
			}
		}
		return null;
	}

}
