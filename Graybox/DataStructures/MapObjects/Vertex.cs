
namespace Graybox.DataStructures.MapObjects;

public class Vertex 
{

	public Vector3 Position { get; set; }
	public float TextureU { get; set; }
	public float TextureV { get; set; }
	public float LightmapU { get; set; } = -1000.0f;
	public float LightmapV { get; set; } = -1000.0f;
	public Face Parent { get; set; }

	public Vertex( Vector3 position, Face parent )
	{
		Position = position;
		Parent = parent;
		TextureV = TextureU = 0;
		LightmapU = LightmapV = -1000f;
	}

	public Vertex Clone()
	{
		return new Vertex( Position, Parent )
		{
			LightmapU = LightmapU,
			LightmapV = LightmapV,
			TextureU = TextureU,
			TextureV = TextureV
		};
	}
}
