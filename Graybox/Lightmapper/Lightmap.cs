
namespace Graybox.Lightmapper;

public class Lightmap : IDisposable
{

	public int Width;
	public int Height;
	public float[] ImageData;
	public Vector2 DirectionalSize;
	public float[] DirectionalData;
	public Vector2 ShadowMaskSize;
	public float[] ShadowMaskData;

	int graphicsId;

	public void Dispose()
	{
		if ( graphicsId != 0 )
		{
			GL.DeleteTexture( graphicsId );
			graphicsId = 0;
		}
	}

	public int GetGraphicsId()
	{
		if ( graphicsId != 0 )
			return graphicsId;

		graphicsId = GL.GenTexture();

		GL.BindTexture( TextureTarget.Texture2D, graphicsId );
		GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, Width, Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgb, PixelType.Float, ImageData );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
		GL.BindTexture( TextureTarget.Texture2D, 0 );

		return graphicsId;
	}

}
