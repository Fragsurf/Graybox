
namespace Graybox.Lightmapper;

public enum LightTypes
{
	Point,
	Directional
}

public struct LightInfo
{

	public Vector3 Position { get; set; }
	public Vector3 Direction { get; set; }
	public float Range { get; set; }
	public float ShadowStrength { get; set; } = 1.0f;
	public float Intensity { get; set; }
	public Color4 Color { get; set; }
	public LightTypes Type { get; set; }

	public LightInfo() { }

}
