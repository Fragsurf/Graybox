
namespace Graybox.Scenes;

public partial class Scene
{

	public bool SkyboxEnabled { get; set; } = false;
	public TextureAsset Skybox { get; set; }

	TextureAsset FindSkybox()
	{
		if ( string.IsNullOrEmpty( Environment.Skybox ) )
			return null;

		return AssetSystem?.FindAsset<TextureAsset>( Environment.Skybox );
	}

	void RenderSkybox()
	{
		var skybox = Skybox ?? FindSkybox();

		if ( skybox == null ) return;
		if ( !SkyboxEnabled ) return;
		if ( Camera.Orthographic ) return;

		skybox.IsCubemap = true;
		var texId = skybox.GraphicsID;
		skybox.IsCubemap = false;
		if ( texId <= 0 ) return;

		var size = 4f;

		GL.Enable( EnableCap.TextureCubeMap );
		GL.ActiveTexture( TextureUnit.Texture0 );
		GL.BindTexture( TextureTarget.TextureCubeMap, texId );
		GL.Color3( 1f, 1, 1 );

		GL.MatrixMode( MatrixMode.Modelview );
		GL.PushMatrix();
		GL.Translate( Camera.Position );
		GL.Rotate( -280.0f, 0f, 0f, 1.0f );
		GL.Rotate( 90.0f, 1.0f, 0.0f, 0.0f );

		GL.DepthMask( false );
		GL.Begin( PrimitiveType.Quads );

		// Front face (+Z)
		GL.TexCoord3( -1.0f, -1.0f, 1.0f ); GL.Vertex3( size, -size, size );
		GL.TexCoord3( -1.0f, 1.0f, 1.0f ); GL.Vertex3( size, size, size );
		GL.TexCoord3( 1.0f, 1.0f, 1.0f ); GL.Vertex3( -size, size, size );
		GL.TexCoord3( 1.0f, -1.0f, 1.0f ); GL.Vertex3( -size, -size, size );

		// Back face (-Z)
		GL.TexCoord3( 1.0f, -1.0f, -1.0f ); GL.Vertex3( -size, -size, -size );
		GL.TexCoord3( 1.0f, 1.0f, -1.0f ); GL.Vertex3( -size, size, -size );
		GL.TexCoord3( -1.0f, 1.0f, -1.0f ); GL.Vertex3( size, size, -size );
		GL.TexCoord3( -1.0f, -1.0f, -1.0f ); GL.Vertex3( size, -size, -size );

		// Right face (+X)
		GL.TexCoord3( -1.0f, -1.0f, -1.0f ); GL.Vertex3( size, -size, -size );
		GL.TexCoord3( -1.0f, 1.0f, -1.0f ); GL.Vertex3( size, size, -size );
		GL.TexCoord3( -1.0f, 1.0f, 1.0f ); GL.Vertex3( size, size, size );
		GL.TexCoord3( -1.0f, -1.0f, 1.0f ); GL.Vertex3( size, -size, size );

		// Left face (-X)
		GL.TexCoord3( 1.0f, -1.0f, 1.0f ); GL.Vertex3( -size, -size, size );
		GL.TexCoord3( 1.0f, 1.0f, 1.0f ); GL.Vertex3( -size, size, size );
		GL.TexCoord3( 1.0f, 1.0f, -1.0f ); GL.Vertex3( -size, size, -size );
		GL.TexCoord3( 1.0f, -1.0f, -1.0f ); GL.Vertex3( -size, -size, -size );

		// Top face (+Y)
		GL.TexCoord3( 1.0f, 1.0f, 1.0f ); GL.Vertex3( -size, size, size );
		GL.TexCoord3( -1.0f, 1.0f, 1.0f ); GL.Vertex3( size, size, size );
		GL.TexCoord3( -1.0f, 1.0f, -1.0f ); GL.Vertex3( size, size, -size );
		GL.TexCoord3( 1.0f, 1.0f, -1.0f ); GL.Vertex3( -size, size, -size );

		// Bottom face (-Y)
		GL.TexCoord3( 1.0f, -1.0f, -1.0f ); GL.Vertex3( -size, -size, -size );
		GL.TexCoord3( -1.0f, -1.0f, -1.0f ); GL.Vertex3( size, -size, -size );
		GL.TexCoord3( -1.0f, -1.0f, 1.0f ); GL.Vertex3( size, -size, size );
		GL.TexCoord3( 1.0f, -1.0f, 1.0f ); GL.Vertex3( -size, -size, size );

		GL.End();

		GL.BindTexture( TextureTarget.TextureCubeMap, 0 );
		GL.Disable( EnableCap.TextureCubeMap );
		GL.DepthMask( true );

		GL.MatrixMode( MatrixMode.Modelview );
		GL.PopMatrix();
	}

}
