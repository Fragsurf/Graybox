
using System.Drawing.Imaging;
using System.Drawing;

namespace Graybox.Editor;

internal static class EditorResource
{

	private static Dictionary<string, int> textureCache = new Dictionary<string, int>();
	private static int errorTextureId;
	static EditorResource()
	{
		InitializeErrorTexture();
	}

	public static int Image( string path )
	{
		if ( textureCache.ContainsKey( path ) )
		{
			return textureCache[path];
		}

		int textureId = LoadTexture( path );
		if ( textureId == 0 )
		{
			textureId = errorTextureId;
		}

		textureCache[path] = textureId;
		return textureId;
	}

	private static int LoadTexture( string path )
	{
		if ( string.IsNullOrEmpty( path ) || !System.IO.File.Exists( path ) )
		{
			return 0;
		}

		try
		{
			using ( var image = new Bitmap( path ) )
			{
				int texId = GL.GenTexture();
				GL.BindTexture( TextureTarget.Texture2D, texId );

				var data = image.LockBits(
					new Rectangle( 0, 0, image.Width, image.Height ),
					ImageLockMode.ReadOnly,
					System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
					image.Width, image.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0 );

				image.UnlockBits( data );

				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
				GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );

				return texId;
			}
		}
		catch
		{
			return 0; 
		}
	}

	private static void InitializeErrorTexture()
	{
		using ( var bitmap = new Bitmap( 1, 1 ) )
		{
			bitmap.SetPixel( 0, 0, Color.Red );

			errorTextureId = GL.GenTexture();
			GL.BindTexture( TextureTarget.Texture2D, errorTextureId );

			var data = bitmap.LockBits(
				new Rectangle( 0, 0, 1, 1 ),
				ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb );

			GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0 );

			bitmap.UnlockBits( data );

			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
		}
	}

}
