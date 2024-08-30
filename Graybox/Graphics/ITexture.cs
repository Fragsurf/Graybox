
namespace Graybox.Graphics;

public interface ITexture : IDisposable
{
	TextureFlags Flags { get; }
	string AssetPath { get; }
	int Width { get; }
	int Height { get; }
	int GraphicsID { get; }
	void Bind();
	void Unbind();
	bool HasTransparency => Flags.HasFlag( TextureFlags.Transparent );
}
