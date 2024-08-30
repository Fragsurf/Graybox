
namespace Graybox.DataStructures.MapObjects;

public class EnvironmentData
{
	public bool FogEnabled { get; set; }
	public Color4 FogColor { get; set; }
	public float FogDensity { get; set; }
	public Color4 AmbientColor { get; set; }
	public Color4 SkyColor { get; set; } = new( 0.2f, 0.2f, 0.2f, 1.0f );
	[EditAsAsset]
	public string Skybox { get; set; }
}
