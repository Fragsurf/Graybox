
namespace Graybox.Graphics;

public static class TextureCache
{

	private static int missingTextureId = 0;
	public static int MissingTexture
	{
		get
		{
			if ( missingTextureId == 0 )
			{
				missingTextureId = CreateMissingTexture();
			}
			return missingTextureId;
		}
	}

	private static int CreateMissingTexture()
	{
		int textureId;
		GL.GenTextures( 1, out textureId );
		GL.BindTexture( TextureTarget.Texture2D, textureId );

		const int size = 512;
		byte[] data = new byte[size * size * 4];

		for ( int y = 0; y < size; y++ )
		{
			for ( int x = 0; x < size; x++ )
			{
				int index = (x + y * size) * 4;
				bool isPink = ((x / 32) % 2 == (y / 32) % 2);

				data[index] = isPink ? (byte)255 : (byte)0;
				data[index + 1] = 0;
				data[index + 2] = isPink ? (byte)255 : (byte)0;
				data[index + 3] = 255;
			}
		}

		GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size, size, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
		GL.BindTexture( TextureTarget.Texture2D, 0 );

		return textureId;
	}

}
