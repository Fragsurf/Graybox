
using Graybox.DataStructures.MapObjects;
using Graybox.Scenes;

namespace Graybox.Lightmapper;

public class LightmapConfig
{

	public int Width = 2048;
	public int Height = 2048;
	public Color4 AmbientColor = new( 0.35f, 0.35f, 0.35f, 1.0f );
	public Scene Scene;
	public float BlurStrength = 1.0f;
	public IEnumerable<Solid> Solids;
	public IEnumerable<LightInfo> Lights;

}
