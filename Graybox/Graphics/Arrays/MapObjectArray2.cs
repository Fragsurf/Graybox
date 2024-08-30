
using Graybox.DataStructures.MapObjects;
using Graybox.Scenes.Shaders;

namespace Graybox.Graphics;

public class MapObjectArray2 : VBO<MapObject, MapObjectVertex>
{

	const int Opaque = 0;

	public MapObjectArray2( IEnumerable<MapObject> data ) : base( data )
	{
	}

	protected override void CreateArray( IEnumerable<MapObject> data )
	{
		foreach ( var obj in data )
		{
			if ( obj is Solid s )
			{
				var submeshes = s.Faces.GroupBy( x => x.TextureRef.Texture );
				foreach ( var m in submeshes )
				{
					if ( m.Key == null )
						continue;

					StartSubset( Opaque );

					var indices = new List<uint>();
					var verts = new List<MapObjectVertex>();
					int vertexOffset = 0;
					foreach ( var face in m )
					{
						var faceVerts = Convert( face ).ToList();
						verts.AddRange( faceVerts );
						indices.AddRange( face.GetTriangleIndices().Select( index => (uint)(index + vertexOffset) ) );
						vertexOffset += faceVerts.Count;
					}
					var idx = PushData( verts );

					var subsetKey = new SolidSubsetData()
					{
						Object = obj,
						Texture = m.Key,
						Bounds = new Bounds( obj.BoundingBox.Start, obj.BoundingBox.End )
					};

					PushIndex( Opaque, idx, indices );
					PushSubset( Opaque, subsetKey );
				}
			}
		}
	}

	public void RenderTextured( ShaderProgram program, Frustum cullFrustum )
	{
		Begin();
		int drawcall = 0;
		foreach ( Subset subset in GetSubsets<SolidSubsetData>( Opaque ) )
		{
			if ( subset.Instance is not SolidSubsetData submesh ) continue;
			if ( !cullFrustum.Contains( submesh.Bounds ) ) continue;

			drawcall++;
			program.ResetTexturePosition();
			program.SetTexture( ShaderConstants.MainTexture, submesh.Texture.GraphicsID );
			program.SetUniform( ShaderConstants.ModelMatrix, Matrix4.Identity );
			Render( PrimitiveType.Triangles, subset );
		}
		End();
	}

	public void UpdatePartial( IEnumerable<MapObject> objects )
	{
	}

	public void UpdatePartial( IEnumerable<Face> faces )
	{
	}

	static IEnumerable<MapObjectVertex> Convert( Face face )
	{
		var normal = -new Vector3( face.Plane.Normal.X, face.Plane.Normal.Y, face.Plane.Normal.Z );
		var color = new Color4( face.Colour.R / 255f, face.Colour.G / 255f, face.Colour.B / 255f, face.Opacity );
		var selected = face.IsSelected || (face.Parent != null && face.Parent.IsSelected) ? 1 : 0;

		foreach ( var vert in face.Vertices )
		{
			yield return new MapObjectVertex
			{
				Position = new Vector3( vert.Position.X, vert.Position.Y, vert.Position.Z ),
				Normal = normal,
				TexCoords = new Vector2( vert.TextureU, vert.TextureV ),
				TexCoordsLM = new Vector2( vert.LightmapU, vert.LightmapV ),
				Color = color,
				IsSelected = selected
			};
		}
	}

}
