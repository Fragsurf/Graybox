
using Graybox.DataStructures.MapObjects;
using Graybox.Scenes;
using Graybox.Scenes.Shaders;

namespace Graybox.Graphics;

internal class SolidRenderer : VBO2<Solid, MapObjectVertex>
{

	ShaderProgram LitShader;
	Scene Scene;

	public SolidRenderer( Scene scene )
	{
		Scene = scene;
		LitShader = new( ShaderSource.VertexSimpleLit, ShaderSource.FragmentSimpleLit );
	}

	public override void Dispose()
	{
		base.Dispose();

		LitShader?.Dispose();
	}

	protected override void PreRender()
	{
		base.PreRender();

		var viewMatrix = Scene.Camera.GetViewMatrix();
		var projMatrix = Scene.Camera.GetProjectionMatrix();
		var lm = Scene.Lightmaps?.Lightmaps?.FirstOrDefault();
		var lmTextureId = lm?.GetGraphicsId() ?? 0;

		LitShader.Bind();

		LitShader.SetUniform( ShaderConstants.ModelMatrix, Matrix4.Identity );
		LitShader.SetUniform( ShaderConstants.Shininess, 32f );
		//_litShader.SetUniform( ShaderConstants.GridSize, GridEnabled ? GridSize : 0 );
		LitShader.SetUniform( ShaderConstants.CameraProjection, projMatrix );
		LitShader.SetUniform( ShaderConstants.CameraView, viewMatrix );
		LitShader.SetUniform( ShaderConstants.SunMatrix, Scene.SunLightSpaceMatrix );
		LitShader.SetUniform( ShaderConstants.SunColor, Scene.SunColor );
		LitShader.SetUniform( ShaderConstants.SunEnabled, Scene.SunDepthTexture != 0 && Scene.SunEnabled );
		LitShader.SetUniform( ShaderConstants.SunDirection, Scene.SunDirection.Normalized() );
		LitShader.SetUniform( ShaderConstants.AmbientColor, new Vector3( Scene.Environment.AmbientColor.R, Scene.Environment.AmbientColor.G, Scene.Environment.AmbientColor.B ) );
		LitShader.SetUniform( ShaderConstants.CameraPosition, Scene.Camera.Position );
		LitShader.SetUniform( ShaderConstants.FogEnabled, Scene.Environment.FogEnabled );
		LitShader.SetUniform( ShaderConstants.FogDensity, Scene.Environment.FogDensity );
		LitShader.SetUniform( ShaderConstants.FogColor, new Vector3 ( Scene.Environment.FogColor.R, Scene.Environment.FogColor.G, Scene.Environment.FogColor.B ) );
		LitShader.SetUniform( ShaderConstants.LightmapIndex, lmTextureId != 0 ? 0f : -1f );

		LitShader.SetTextureOffset( 0 );
		LitShader.ResetTexturePosition();
		LitShader.SetTexture( ShaderConstants.SunShadowMap, Scene.SunDepthTexture );
		LitShader.SetTexture( ShaderConstants.LightmapTexture0, lmTextureId );
		LitShader.SetTextureOffset( 2 );

		GL.Enable( EnableCap.CullFace );
		GL.CullFace( CullFaceMode.Back );
		GL.FrontFace( FrontFaceDirection.Cw );
	}

	protected override void PostRender()
	{
		base.PostRender();

		LitShader.Unbind();
	}

	protected override void PreRenderSubset( object subset )
	{
		base.PreRenderSubset( subset );

		if ( subset is ITexture tex && tex.GraphicsID != 0 )
		{
			LitShader.ResetTexturePosition();
			LitShader.SetTexture( ShaderConstants.MainTexture, tex.GraphicsID );
		}
		else
		{
			LitShader.ResetTexturePosition();
			LitShader.SetTexture( ShaderConstants.MainTexture, TextureCache.MissingTexture );
		}
	}

	protected override IEnumerable<VBO2SubsetPart<Solid, MapObjectVertex>> Convert( Solid item )
	{
		foreach ( var group in item.Faces.GroupBy( x => x.TextureRef.AssetPath ) )
		{
			var convertedFaces = group.SelectMany( Convert );
			var tex = Scene.AssetSystem.FindAsset<TextureAsset>( group.Key );

			var subset = (object)tex;
			var data = convertedFaces.ToArray();

			yield return new VBO2SubsetPart<Solid, MapObjectVertex>
			{
				Subset = subset,
				Data = data
			};
		}
	}

	MapObjectVertex[] Convert( Face face )
	{
		var normal = face.Plane.Normal;
		var color = face.Colour;
		var verticesCount = (face.Vertices.Count - 2) * 3; 
		var result = new MapObjectVertex[verticesCount];
		var lightmapDisabled = face.Parent is Solid s && s.Parent is Entity e && e.ClassName.StartsWith( "trigger_" );

		int resultIndex = 0;

		for ( int i = 1; i < face.Vertices.Count - 1; i++ )
		{
			var v0 = face.Vertices[0];
			var v1 = face.Vertices[i];
			var v2 = face.Vertices[i + 1];

			var edge1 = v1.Position - v0.Position;
			var edge2 = v2.Position - v0.Position;
			var deltaUV1 = new Vector2( v1.TextureU - v0.TextureU, v1.TextureV - v0.TextureV );
			var deltaUV2 = new Vector2( v2.TextureU - v0.TextureU, v2.TextureV - v0.TextureV );

			var f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

			var tangent = new Vector3(
				f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X),
				f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y),
				f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z)
			);
			tangent.Normalize();

			var vert = new MapObjectVertex()
			{
				Normal = normal,
				Tangent = tangent,
				Color = color
			};

			vert.Position = v0.Position;
			vert.TexCoords = new Vector2( v0.TextureU, v0.TextureV );
			vert.TexCoordsLM = lightmapDisabled ? new() : new Vector2( v0.LightmapU, v0.LightmapV );
			result[resultIndex++] = vert;

			vert.Position = v1.Position;
			vert.TexCoords = new Vector2( v1.TextureU, v1.TextureV );
			vert.TexCoordsLM = lightmapDisabled ? new() : new Vector2( v1.LightmapU, v1.LightmapV );
			result[resultIndex++] = vert;

			vert.Position = v2.Position;
			vert.TexCoords = new Vector2( v2.TextureU, v2.TextureV );
			vert.TexCoordsLM = lightmapDisabled ? new() : new Vector2( v2.LightmapU, v2.LightmapV );
			result[resultIndex++] = vert;
		}

		return result;
	}


}
