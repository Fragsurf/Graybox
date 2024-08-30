
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics.Helpers;
using Graybox.Scenes.Shaders;

namespace Graybox.Graphics;

public class MapObjectArray : VBO<MapObject, MapObjectVertex>
{

	private const int Textured = 0;
	private const int Transparent = 1;
	private const int BrushWireframe = 2;
	private const int EntityWireframe = 3;

	public bool IsWireframe { get; set; }

	public MapObjectArray( IEnumerable<MapObject> data )
		: base( data )
	{
	}

	public void RenderTextured( ShaderProgram program, Frustum? cullFrustum = null )
	{
		Begin();
		int drawcall = 0;
		foreach ( Subset subset in GetSubsets<SolidSubsetData>( Textured ) )
		{
			if ( subset.Instance is not SolidSubsetData submesh ) continue;
			if ( !cullFrustum?.Contains( submesh.Bounds ) ?? false ) continue;

			drawcall++;
			program.ResetTexturePosition();
			program.SetTexture( ShaderConstants.MainTexture, submesh.Texture.GraphicsID );
			program.SetUniform( ShaderConstants.ModelMatrix, Matrix4.Identity );

			if ( submesh.Material != null )
			{
				submesh.Material.Apply( program );
			}

			Render( PrimitiveType.Triangles, subset );
		}
		End();
	}

	public void RenderUntextured( OpenTK.Mathematics.Vector3 location )
	{
		Begin();
		GL.ActiveTexture( TextureUnit.Texture0 );
		GL.BindTexture( TextureTarget.Texture2D, 0 );
		GL.ActiveTexture( TextureUnit.Texture1 );
		GL.BindTexture( TextureTarget.Texture2D, 0 );
		GL.ActiveTexture( TextureUnit.Texture2 );
		GL.BindTexture( TextureTarget.Texture2D, 0 );
		GL.ActiveTexture( TextureUnit.Texture0 );
		foreach ( Subset subset in GetSubsets<ITexture>( Textured ).Where( x => x.Instance == null ) )
		{
			Render( PrimitiveType.Triangles, subset );
		}
		foreach ( Subset subset in GetSubsets<Entity>( Textured ) )
		{
			Entity e = (Entity)subset.Instance;
			//if ( !Graybox.Settings.View.DisableModelRendering && e.HasModel() && e.HideDistance() > (location - e.Origin).VectorMagnitude() ) continue;
			Render( PrimitiveType.Triangles, subset );
		}
		End();
	}

	private float LookAtOrder( Face face, OpenTK.Mathematics.Vector3 cameraLocation, OpenTK.Mathematics.Vector3 lookAt )
	{
		return -(face.BoundingBox.Center - cameraLocation).LengthSquared;
	}

	public void RenderTransparent( Action<TextureReference> textureCallback, OpenTK.Mathematics.Vector3 cameraLocation, OpenTK.Mathematics.Vector3 lookAt )
	{
		Begin();
		IEnumerable<Subset> sorted =
			from subset in GetSubsets<Face>( Transparent )
			let face = subset.Instance as Face
			where face != null
			orderby LookAtOrder( face, cameraLocation, lookAt ) ascending
			select subset;

		foreach ( Subset subset in sorted )
		{
			TextureReference tex = ((Face)subset.Instance).TextureRef;
			if ( tex.Texture != null )
			{
				tex.Texture.Bind();
			}
			else
			{
				GL.BindTexture( TextureTarget.Texture2D, 0 );

			}
			textureCallback( tex );
			Render( PrimitiveType.Triangles, subset );
		}
		End();
	}

	public void RenderWireframe()
	{
		Begin();
		foreach ( Subset subset in GetSubsets( BrushWireframe ) )
		{
			Render( PrimitiveType.Lines, subset );
		}
		foreach ( Subset subset in GetSubsets( EntityWireframe ) )
		{
			Render( PrimitiveType.Lines, subset );
		}
		End();
	}

	public void RenderVertices( int pointSize )
	{
		Begin();
		GL.PointSize( pointSize );
		foreach ( Subset subset in GetSubsets( BrushWireframe ) )
		{
			Render( PrimitiveType.Points, subset );
		}
		End();
	}

	public void UpdatePartial( IEnumerable<MapObject> objects )
	{
		UpdatePartial( objects.OfType<Solid>().SelectMany( x => x.Faces ) );
		UpdatePartial( objects.OfType<Entity>().Where( x => !x.HasChildren ) );
	}

	public void UpdatePartial( IEnumerable<Face> faces )
	{
		foreach ( Face face in faces )
		{
			int offset = GetOffset( face );
			if ( offset < 0 ) continue;
			IEnumerable<MapObjectVertex> conversion = Convert( face );
			Update( offset, conversion );
		}
	}

	public void UpdatePartial( IEnumerable<Entity> entities )
	{
		foreach ( Entity entity in entities )
		{
			int offset = GetOffset( entity );
			if ( offset < 0 ) continue;

			var conversion = entity.GetBoxFaces().SelectMany( Convert );

			Update( offset, conversion );
		}
	}

	protected override void CreateArray( IEnumerable<MapObject> objects )
	{
		var obj = objects.Where( x => !x.IsVisgroupHidden && !x.IsCodeHidden ).ToList();
		var faces = obj.OfType<Solid>().SelectMany( x => x.Faces ).ToList();
		var entities = obj.OfType<Entity>().Where( x => !x.HasChildren ).ToList();

		if ( IsWireframe )
		{
			StartSubset( BrushWireframe );
			foreach ( var face in faces )
			{
				PushOffset( face );
				var uidx = PushData( Convert( face ) );
				PushIndex( BrushWireframe, uidx, face.GetLineIndices() );
			}
			PushSubset( BrushWireframe, (object)null );
		}
		else
		{
			var textureGroups = faces.GroupBy( x => x.TextureRef.Texture );
			foreach ( var texGroup in textureGroups )
			{
				var tex = texGroup.Key;
				if ( tex == null ) continue;

				Bounds? bounds = null;

				StartSubset( Textured );
				foreach ( var face in texGroup )
				{
					bounds ??= new Bounds( face.Vertices[0].Position, 1f );

					var vertData = Convert( face );
					foreach ( var vert in vertData )
						bounds = bounds.Value.Encapsulate( vert.Position );

					PushOffset( face );
					var uidx = PushData( vertData );
					PushIndex( Textured, uidx, face.GetTriangleIndices() );
				}

				var data = new SolidSubsetData()
				{
					Bounds = bounds ?? default,
					Object = null,
					Texture = tex
				};

				PushSubset( Textured, data );
			}
		}
	}

	protected IEnumerable<MapObjectVertex> Convert( Face face )
	{
		var normal = new Vector3( face.Plane.Normal.X, face.Plane.Normal.Y, face.Plane.Normal.Z );
		var color = new Color4( face.Colour.R / 255f, face.Colour.G / 255f, face.Colour.B / 255f, face.Opacity );
		var selected = face.IsSelected || (face.Parent != null && face.Parent.IsSelected) ? 1 : 0;

		if ( face.Vertices.Count < 3 )
			yield break;

		var positions = face.Vertices.Select( vert => new Vector3( vert.Position.X, vert.Position.Y, vert.Position.Z ) ).ToArray();

		// Calculate tangents
		for ( int i = 0; i < positions.Length; i++ )
		{
			int nextIndex = (i + 1) % positions.Length;
			var position = positions[i];
			var nextPosition = positions[nextIndex];

			var tangent = Vector3.Normalize( nextPosition - position );

			yield return new MapObjectVertex
			{
				Position = position,
				Normal = normal,
				Tangent = tangent,
				TexCoords = new Vector2( (float)face.Vertices[i].TextureU, (float)face.Vertices[i].TextureV ),
				TexCoordsLM = new Vector2( face.Vertices[i].LightmapU, face.Vertices[i].LightmapV ),
				Color = color,
				IsSelected = selected
			};
		}
	}

}
