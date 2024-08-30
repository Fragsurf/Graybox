
using Graybox.Scenes;
using Graybox.Scenes.Shaders;

namespace Graybox.Graphics;

internal class GridSettings
{

	public int Size { get; set; }
	public int Spacing { get; set; }
	public Vector3 Forward { get; set; }
	public Vector3 Origin { get; set; }
	public Color4 Color { get; set; } = new( 75, 75, 75, 255 );
	public Color4 ZeroColor { get; set; } = new( 0, 100, 100, 255 );
	public Color4 HighlightColor { get; set; } = new( 115, 115, 115, 255 );
	public Color4 BoundaryColor { get; set; } = new( 255, 0, 0, 255 );

}

internal class GridRenderer : VBO2<GridSettings, MapObjectVertex>
{

	protected override PrimitiveType Mode => PrimitiveType.Lines;

	ShaderProgram GridLinesShader;
	Scene Scene;

	public GridRenderer( Scene scene )
	{
		Scene = scene;
		GridLinesShader = new( ShaderSource.VertGridLinesShader, ShaderSource.FragGridLinesShader );
	}

	protected override void PreRender()
	{
		base.PreRender();

		var viewMatrix = Scene.Camera.GetViewMatrix();
		var projMatrix = Scene.Camera.GetProjectionMatrix();

		GridLinesShader.Bind();
		GridLinesShader.SetUniform( ShaderConstants.ModelMatrix, Matrix4.Identity );
		GridLinesShader.SetUniform( ShaderConstants.CameraProjection, projMatrix );
		GridLinesShader.SetUniform( ShaderConstants.CameraView, viewMatrix );
		GridLinesShader.SetUniform( "_ScreenSize", new Vector2( Scene.Camera.OrthographicWidth, Scene.Camera.OrthographicHeight ) );
		GridLinesShader.SetUniform( "_ZoomLevel", Scene.Camera.OrthographicZoom );
	}

	protected override void PostRender()
	{
		base.PostRender();

		GridLinesShader.Unbind();
	}

	protected override IEnumerable<VBO2SubsetPart<GridSettings, MapObjectVertex>> Convert( GridSettings item )
	{
		var result = new VBO2SubsetPart<GridSettings, MapObjectVertex>();
		var vertices = new List<MapObjectVertex>();

		var boundary = item.Size / 2;
		var rotation = GetQuaternionFromForward( item.Forward );

		for ( float i = -boundary; i <= boundary; i += item.Spacing )
		{
			Color4 c = item.Color;
			if ( i == 0 ) c = item.ZeroColor;
			else if ( i % (item.Spacing * 8) == 0 ) c = item.HighlightColor;

			AddLine( vertices, c, new Vector3( -boundary, i, 0 ), new Vector3( boundary, i, 0 ), rotation, item.Origin );
			AddLine( vertices, c, new Vector3( i, -boundary, 0 ), new Vector3( i, boundary, 0 ), rotation, item.Origin );
		}

		Color4 boundaryColor = item.BoundaryColor;
		AddLine( vertices, boundaryColor, new Vector3( -boundary, boundary, 0 ), new Vector3( boundary, boundary, 0 ), rotation, item.Origin );
		AddLine( vertices, boundaryColor, new Vector3( -boundary, -boundary, 0 ), new Vector3( boundary, -boundary, 0 ), rotation, item.Origin );
		AddLine( vertices, boundaryColor, new Vector3( -boundary, -boundary, 0 ), new Vector3( -boundary, boundary, 0 ), rotation, item.Origin );
		AddLine( vertices, boundaryColor, new Vector3( boundary, -boundary, 0 ), new Vector3( boundary, boundary, 0 ), rotation, item.Origin );

		result.Subset = null;
		result.Data = vertices.ToArray();
		yield return result;
	}

	private void AddLine( List<MapObjectVertex> vertices, Color4 color, Vector3 start, Vector3 end, Quaternion rotation, Vector3 origin )
	{
		vertices.Add( new MapObjectVertex
		{
			Position = Vector3.Transform( start, rotation ) + origin,
			Color = color,
			Normal = Vector3.UnitY
		} );
		vertices.Add( new MapObjectVertex
		{
			Position = Vector3.Transform( end, rotation ) + origin,
			Color = color,
			Normal = Vector3.UnitY
		} );
	}

	private Quaternion GetQuaternionFromForward( Vector3 forward )
	{
		forward = forward.Normalized();

		var up = Vector3.UnitZ;

		if ( Math.Abs( Vector3.Dot( forward, up ) ) > 0.9999f )
		{
			return Quaternion.FromAxisAngle( Vector3.UnitZ, MathHelper.PiOver2 );
		}
		else
		{
			var right = Vector3.Cross( up, forward ).Normalized();
			up = Vector3.Cross( forward, right );

			var rotationMatrix = new Matrix4(
				right.X, right.Y, right.Z, 0,
				up.X, up.Y, up.Z, 0,
				forward.X, forward.Y, forward.Z, 0,
				0, 0, 0, 1
			);

			return rotationMatrix.ExtractRotation();
		}
	}

}
