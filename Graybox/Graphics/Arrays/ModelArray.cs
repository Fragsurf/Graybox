
using Graybox.DataStructures.Models;

namespace Graybox.Graphics;

public class ModelArray : VBO<Model, MapObjectVertex>
{
	private const int Textured = 0;

	public ModelArray( Model model )
		: base( new[] { model } )
	{
	}

	public void RenderTextured()
	{
		Begin();
		foreach ( Subset subset in GetSubsets<ITexture>( Textured ) )
		{
			((ITexture)subset.Instance).Bind();
			Render( PrimitiveType.Triangles, subset );
		}
		End();
	}

	protected override void CreateArray( IEnumerable<Model> objects )
	{
		foreach ( Model model in objects )
		{
			PushOffset( model );

			var transforms = model.GetTransforms();

			foreach ( var g in model.GetActiveMeshes().GroupBy( x => x.SkinRef ) )
			{
				StartSubset( Textured );
				var tex = model.Textures[g.Key];

				foreach ( var mesh in g )
				{
					foreach ( var vertex in mesh.Vertices )
					{
						var transform = transforms[vertex.BoneWeightings.First().Bone.BoneIndex];
						//var c = vertex.Location * transform;
						//var n = vertex.Normal * transform;
						var c = new Vector3();
						var n = new Vector3();
						var index = PushData( new[]
						{
							new MapObjectVertex
							{
								Position = new Vector3(c.X, c.Y, c.Z),
								Normal = new Vector3(n.X, n.Y, n.Z),
								Color = new Color4( vertex.Color.X, vertex.Color.Y, vertex.Color.Z, vertex.Color.W ),
								TexCoords = new Vector2(vertex.TextureU, vertex.TextureV),
								TexCoordsLM = new Vector2(-500.0f, -500.0f),
								IsSelected = 0
							}
						} );
						PushIndex( Textured, index, new[] { (uint)0 } );
					}
				}
				PushSubset( Textured, tex.TextureObject );
			}
		}
	}
}
