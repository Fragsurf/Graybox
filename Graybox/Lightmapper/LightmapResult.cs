
using Graybox.Scenes;

namespace Graybox.Lightmapper;

public class LightmapResult
{

	public Scene Scene;
	public object Context;
	public bool Success;
	public string ErrorMessage;
	public List<Lightmap> Lightmaps;

}
