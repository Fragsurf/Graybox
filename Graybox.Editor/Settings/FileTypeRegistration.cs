
namespace Graybox.Editor.Settings;

public static class FileTypeRegistration
{
	public static FileType[] GetSupportedExtensions()
	{
		return new[]
		{
			new FileType(".graybox", "Graybox File", true, true),
			new FileType(".rmap", "Fragsurf Map File", true, true),
			//new FileType(".cbr", "Containment Breach Room", false, true),
			new FileType(".vmf", "Valve Map File", false, true),
			//new FileType(".3dw", "Leadwerks 3D World Studio File", false, true),
			//new FileType(".msl", "Mapscape 2 Level", false, true),
		};
	}
}
