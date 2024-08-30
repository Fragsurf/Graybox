
namespace Graybox.Editor.Settings;

public class FileType
{

	public string Extension { get; set; }
	public string Description { get; set; }
	public bool CanSave { get; set; }
	public bool CanLoad { get; set; }

	public FileType( string extension, string description, bool canSave, bool canLoad )
	{
		Extension = extension;
		Description = description;
		CanSave = canSave;
		CanLoad = canLoad;
	}

}
