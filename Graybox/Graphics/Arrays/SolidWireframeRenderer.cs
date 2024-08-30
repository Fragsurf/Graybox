
using Graybox.DataStructures.MapObjects;
using Graybox.Scenes;
using Graybox.Scenes.Shaders;

namespace Graybox.Graphics;

internal class SolidWireframeRenderer : VBO2<Solid, MapObjectVertex>
{

	ShaderProgram VertexColorShader;
	Scene Scene;

	protected override int Passes => 2;
	protected override PrimitiveType Mode => Pass == 0 ? PrimitiveType.Lines : PrimitiveType.Points;

	public SolidWireframeRenderer( Scene scene )
	{
		Scene = scene;
		VertexColorShader = new( ShaderSource.VertVertexColorShader, ShaderSource.FragVertexColorShader );
	}

	public override void Dispose()
	{
		base.Dispose();

		VertexColorShader?.Dispose();
	}

	protected override void PreRender()
	{
		base.PreRender();

		if ( Scene.Camera.Orthographic )
		{
			var basePointSize = 8f;
			var exponent = -0.5f;
			GL.PointSize( basePointSize * MathF.Pow( Scene.Camera.OrthographicZoom, exponent ) );
		}
		else
		{
			GL.PointSize( 8f );
		}

		var viewMatrix = Scene.Camera.GetViewMatrix();
		var projMatrix = Scene.Camera.GetProjectionMatrix();

		VertexColorShader.Bind();

		VertexColorShader.SetUniform( ShaderConstants.ModelMatrix, Matrix4.Identity );
		VertexColorShader.SetUniform( ShaderConstants.CameraProjection, projMatrix );
		VertexColorShader.SetUniform( ShaderConstants.CameraView, viewMatrix );
		VertexColorShader.SetUniform( ShaderConstants.CameraPosition, Scene.Camera.Position );
	}

	protected override void PostRender()
	{
		base.PostRender();

		VertexColorShader.Unbind();
	}

	protected override void PreRenderSubset( object subset )
	{
		base.PreRenderSubset( subset );

	}

	protected override IEnumerable<VBO2SubsetPart<Solid, MapObjectVertex>> Convert( Solid item )
	{
		foreach ( var group in item.Faces.GroupBy( x => x.TextureRef.AssetPath ) )
		{
			var convertedFaces = group.SelectMany( Convert );
			var tex = Scene.AssetSystem.FindAsset<TextureAsset>( group.Key );

			yield return new VBO2SubsetPart<Solid, MapObjectVertex>
			{
				Subset = tex,
				Data = convertedFaces.ToArray()
			};
		}
	}

	MapObjectVertex[] Convert( Face face )
	{
		var normal = face.Plane.Normal;
		var color = face.Colour;
		var result = new List<MapObjectVertex>();

		foreach ( var edge in face.GetEdges() )
		{
			result.Add ( new MapObjectVertex()
			{
				Normal = normal,
				Color = color,
				Position = edge.Start,
			} );

			result.Add ( new MapObjectVertex()
			{
				Normal = normal,
				Color = color,
				Position = edge.End,
			} );
		}

		return result.ToArray();

		//var verticesCount = (face.Vertices.Count - 2) * 3;
		//var result = new MapObjectVertex[verticesCount];

		//int resultIndex = 0;

		//for ( int i = 1; i < face.Vertices.Count - 1; i++ )
		//{
		//	var v0 = face.Vertices[0];
		//	var v1 = face.Vertices[i];
		//	var v2 = face.Vertices[i + 1];

		//	var edge1 = v1.Position - v0.Position;
		//	var edge2 = v2.Position - v0.Position;
		//	var deltaUV1 = new Vector2( v1.TextureU - v0.TextureU, v1.TextureV - v0.TextureV );
		//	var deltaUV2 = new Vector2( v2.TextureU - v0.TextureU, v2.TextureV - v0.TextureV );

		//	var f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

		//	var tangent = new Vector3(
		//		f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X),
		//		f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y),
		//		f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z)
		//	);
		//	tangent.Normalize();

		//	var vert = new MapObjectVertex()
		//	{
		//		Normal = normal,
		//		Tangent = tangent,
		//		Color = color
		//	};

		//	vert.Position = v0.Position;
		//	vert.TexCoords = new Vector2( v0.TextureU, v0.TextureV );
		//	vert.TexCoordsLM = new Vector2( v0.LightmapU, v0.LightmapV );
		//	result[resultIndex++] = vert;

		//	vert.Position = v1.Position;
		//	vert.TexCoords = new Vector2( v1.TextureU, v1.TextureV );
		//	vert.TexCoordsLM = new Vector2( v1.LightmapU, v1.LightmapV );
		//	result[resultIndex++] = vert;

		//	vert.Position = v2.Position;
		//	vert.TexCoords = new Vector2( v2.TextureU, v2.TextureV );
		//	vert.TexCoordsLM = new Vector2( v2.LightmapU, v2.LightmapV );
		//	result[resultIndex++] = vert;
		//}

		//return result;
	}


}
