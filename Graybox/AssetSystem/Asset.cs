
using SkiaSharp;
using System.Drawing;
using Graybox.Utility;
using System.Drawing.Imaging;
using System.Security.Cryptography.Xml;

namespace Graybox;

public abstract class Asset
{

	public abstract AssetTypes AssetType { get; }
	public string AbsolutePath { get; private set; }
	public string RelativePath { get; private set; }
	public string Name { get; private set; }
	public AssetPackage Package { get; private set; }
	public AssetSystem AssetSystem { get; private set; }

	static Dictionary<string, Image> thumbnailCache = new Dictionary<string, Image>();
	public static Image GetThumbnail( Asset asset )
	{
		if ( !thumbnailCache.ContainsKey( asset.RelativePath ) )
		{
			thumbnailCache[asset.RelativePath] = asset.GenerateThumbnail();
		}

		return thumbnailCache[asset.RelativePath];
	}

	static Dictionary<string, SKImage> skThumbnailCache = new Dictionary<string, SKImage>();
	public static SKImage GetSKThumbnail( Asset asset )
	{
		if ( !skThumbnailCache.ContainsKey( asset.RelativePath ) )
		{
			skThumbnailCache[asset.RelativePath] = new Bitmap( asset.GenerateThumbnail() ).ToSKImage();
		}

		return skThumbnailCache[asset.RelativePath];
	}

	static Dictionary<string, int> glThumbnailCache = new Dictionary<string, int>();
	public static int GetGLThumbnail( Asset asset )
	{
		if ( glThumbnailCache.ContainsKey( asset.RelativePath ) )
			return glThumbnailCache[asset.RelativePath];

		var bitmap = new Bitmap( GetThumbnail( asset ) );
		var bitmapData = bitmap.LockBits( new System.Drawing.Rectangle( 0, 0, bitmap.Width, bitmap.Height ), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
		int reference = GL.GenTexture();

		GL.BindTexture( TextureTarget.Texture2D, reference );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp );
		GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0 );

		GL.BindTexture( TextureTarget.Texture2D, 0 );

		bitmap.Dispose();

		glThumbnailCache[asset.RelativePath] = reference;
		return reference;
	}

	public Image GetThumbnail() => Asset.GetThumbnail( this );
	public SKImage GetSKThumbnail() => Asset.GetSKThumbnail( this );
	public int GetGLThumbnail() => Asset.GetGLThumbnail( this );

	public virtual Image GenerateThumbnail()
	{
		var thumbnail = new Bitmap( 128, 128 ); // adjust the size as needed
		using ( var graphics = System.Drawing.Graphics.FromImage( thumbnail ) )
		{
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
			graphics.FillRectangle( System.Drawing.Brushes.DarkGray, 0, 0, thumbnail.Width, thumbnail.Height );

			var className = this.GetType().Name;

			using ( var font = new Font( "Arial", 12, FontStyle.Bold ) )
			using ( var brush = new SolidBrush( Color.Black ) )
			{
				var x = (thumbnail.Width - (int)graphics.MeasureString( className, font ).Width) / 2;
				var y = (thumbnail.Height - (int)graphics.MeasureString( className, font ).Height) / 2;

				graphics.DrawString( className, font, brush, x, y );
			}
		}

		return thumbnail;
	}

	protected virtual void Load() { }

	public static Asset CreateAsset( AssetSystem system, AssetPackage package, FileInfo fileInfo )
	{
		Asset result = null;

		switch ( fileInfo.Extension.ToLower() )
		{
			case ".png":
			case ".jpg":
			case ".jpeg":
				result = new TextureAsset();
				break;
			case ".fbx":
			case ".gltf":
			case ".glb":
				result = new ModelAsset();
				break;
			case ".gbmat":
				result = new MaterialAsset();
				break;
			default:
				return null;
		}

		var packageDir = package.Directory.FullName;
		var relativePath = fileInfo.FullName.Substring( packageDir.Length ).Trim( '\\', '/' );
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension( relativePath );
		var directoryPath = Path.GetDirectoryName( relativePath );
		relativePath = Path.Combine( directoryPath, fileNameWithoutExtension );

		result.Package = package;
		result.AbsolutePath = fileInfo.FullName;
		result.RelativePath = AssetSystem.NormalizePath( relativePath );
		result.Name = Path.GetFileNameWithoutExtension( fileInfo.Name ).ToLower();
		result.AssetSystem = system;
		result.Load();

		return result;
	}

}
