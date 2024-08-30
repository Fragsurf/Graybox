
using Graybox.Graphics;

namespace Graybox.DataStructures.MapObjects;

public class TextureReference
{

	ITexture _texture;
	public ITexture Texture
	{
		get => _texture;
		set
		{
			_texture = value;
			AssetPath = _texture?.AssetPath ?? string.Empty;
		}
	}

	public bool IsToolTexture => AssetPath.Contains( "tool_" );

	public string AssetPath { get; set; }
	public Vector3 UAxis { get; set; }
	public Vector3 VAxis { get; set; }
	public float XShift { get; set; }
	public float XScale { get; set; }
	public float YShift { get; set; }
	public float YScale { get; set; }
	public float Rotation { get; set; }

	public TextureReference()
	{
		AssetPath = "";
		Texture = null;
		Rotation = 0;
		UAxis = -Vector3.UnitZ;
		VAxis = Vector3.UnitX;
		XShift = YShift = 0;
		XScale = YScale = 0.25f;
	}

	public TextureReference Clone()
	{
		return new TextureReference
		{
			AssetPath = AssetPath,
			Texture = Texture,
			Rotation = Rotation,
			UAxis = UAxis,
			VAxis = VAxis,
			XShift = XShift,
			XScale = XScale,
			YShift = YShift,
			YScale = YScale
		};
	}

	public Vector3 GetNormal() => Vector3.Cross( UAxis, VAxis ).Normalized();

}
