using System.Collections.Generic;

namespace Graybox.Editor.Settings;

public class FavouriteTextureFolder
{
	public string Name { get; set; }
	public List<FavouriteTextureFolder> Children { get; set; }
	public List<string> Items { get; set; }

	public FavouriteTextureFolder()
	{
		Name = "";
		Children = new List<FavouriteTextureFolder>();
		Items = new List<string>();
	}
}
