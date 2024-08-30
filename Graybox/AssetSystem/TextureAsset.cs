
using Graybox.Graphics;
using NetVips.Extensions;

namespace Graybox;

public class TextureAsset : Asset, ITexture
{

	public override AssetTypes AssetType => AssetTypes.Texture;
	public int Width { get; private set; }
	public int Height { get; private set; }
	public bool IsCubemap { get; set; }

	string ITexture.AssetPath => RelativePath;
	bool loadedAsCubemap;

	protected override void Load()
	{
		base.Load();

		if ( !File.Exists( AbsolutePath ) )
		{
			Debug.LogWarning( "Missing texture: " + AbsolutePath );
			Flags = TextureFlags.Missing;
			return;
		}

		var absPath = AbsolutePath;
		using var image = NetVips.Image.NewFromFile( absPath, access: NetVips.Enums.Access.Sequential );
		Width = image.Width;
		Height = image.Height;
		Flags = TextureFlags.None;

		if ( image.HasAlpha() )
		{
			var alphaChannel = image[image.Bands - 1];
			var hasTransparency = alphaChannel.Min() < 255;
			if ( hasTransparency )
			{
				Flags |= TextureFlags.Transparent;
			}
		}
	}

	public override System.Drawing.Image GenerateThumbnail()
	{
		var nvimg = NetVips.Image.Thumbnail( AbsolutePath, 128 );
		return nvimg.ToBitmap();
	}

	public void Bind()
	{

	}

	public void Unbind()
	{

	}

	public void Dispose()
	{

	}

	public TextureFlags Flags { get; private set; }

	int reference;
	public int GraphicsID
	{
		get
		{
			if ( loadedAsCubemap != IsCubemap && reference != 0 )
			{
				GL.DeleteTexture( reference );
				reference = 0;
			}

			if ( reference == 0 )
			{
				CreateGLTexture();
			}

			return reference;
		}
	}

	void CreateGLTexture()
	{
		if ( reference != 0 )
		{
			GL.DeleteTexture( reference );
			reference = 0;
		}

		if ( !File.Exists( AbsolutePath ) )
		{
			Debug.LogWarning( "Missing texture: " + AbsolutePath );
			return;
		}

		if ( IsCubemap )
		{
			CreateCubemapTexture();
		}
		else
		{
			CreateStandardTexture();
		}
	}

	void CreateCubemapTexture()
	{
		loadedAsCubemap = true;
		reference = GL.GenTexture();
		GL.BindTexture( TextureTarget.TextureCubeMap, reference );

		using ( var bmp = NetVips.Image.NewFromFile( AbsolutePath ) )
		{
			int faceWidth = bmp.Width / 4;
			int faceHeight = bmp.Height / 3;
			faceWidth = faceHeight = Math.Min( faceWidth, faceHeight );

			Vector2[] facePositions = {
				new (faceWidth * 2, faceHeight), // Positive X
				new (0, faceHeight), // Negative X
				new (faceWidth, 0), // Positive Y
				new (faceWidth, faceHeight * 2), // Negative Y
				new (faceWidth, faceHeight), // Positive Z
				new (faceWidth * 3, faceHeight) // Negative Z
			};

			for ( int i = 0; i < 6; i++ )
			{
				var rect = new Rect( facePositions[i].X, facePositions[i].Y, faceWidth, faceHeight );
				using ( var faceImage = bmp.ExtractArea( (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height ) )
				{
					var formattedFace = faceImage.Format != NetVips.Enums.BandFormat.Uchar ? faceImage.Cast( NetVips.Enums.BandFormat.Uchar ) : faceImage;
					var data = formattedFace.WriteToMemory( out var sz );

					var internalFormat = (formattedFace.Bands == 3) ? PixelInternalFormat.Rgb : PixelInternalFormat.Rgba;
					var pixelFormat = (formattedFace.Bands == 3) ? PixelFormat.Rgb : PixelFormat.Rgba;

					GL.TexImage2D( TextureTarget.TextureCubeMapPositiveX + i, 0, internalFormat, faceWidth, faceHeight, 0, pixelFormat, PixelType.UnsignedByte, data );
				}
			}
		}

		GL.TexParameter( TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
		GL.TexParameter( TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
		GL.TexParameter( TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge );

		GL.BindTexture( TextureTarget.TextureCubeMap, 0 );
	}

	void CreateStandardTexture()
	{
		loadedAsCubemap = false;

		using ( var image = NetVips.Image.NewFromFile( AbsolutePath ) )
		{
			var formattedImage = image.Format != NetVips.Enums.BandFormat.Uchar ? image.Cast( NetVips.Enums.BandFormat.Uchar ) : image;
			//formattedImage = formattedImage.Flip( NetVips.Enums.Direction.Vertical );

			var data = formattedImage.WriteToMemory( out var sz );
			int width = formattedImage.Width;
			int height = formattedImage.Height;

			reference = GL.GenTexture();
			GL.BindTexture( TextureTarget.Texture2D, reference );
			GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1 );
			GL.GenerateMipmap( GenerateMipmapTarget.Texture2D );

			var internalFormat = (formattedImage.Bands == 3) ? PixelInternalFormat.Rgb : PixelInternalFormat.Rgba;
			var pixelFormat = (formattedImage.Bands == 3) ? PixelFormat.Rgb : PixelFormat.Rgba;

			GL.TexImage2D( TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, PixelType.UnsignedByte, data );

			GL.BindTexture( TextureTarget.Texture2D, 0 );
		}
	}

}
