
using Graybox.DataStructures.MapObjects;

namespace Graybox.Graphics;

public class DecalArray : VBO<MapObject, MapObjectVertex>
{
	private const int Transparent = 0;
	private const int Wireframe = 1;

	public DecalArray( IEnumerable<MapObject> data )
		: base( data )
	{
	}

	public void RenderTransparent( Vector3 cameraLocation )
	{
		Begin();
		IEnumerable<Subset> sorted =
			from subset in GetSubsets<Face>( Transparent )
			let face = subset.Instance as Face
			where face != null
			orderby (cameraLocation - face.BoundingBox.Center).LengthSquared descending
			select subset;
		foreach ( Subset subset in sorted )
		{
			TextureReference tex = ((Face)subset.Instance).TextureRef;
			tex.Texture.Bind();
			Render( PrimitiveType.Triangles, subset );
		}
		End();
	}

	public void RenderWireframe( IGraphicsContext context )
	{
		Begin();
		foreach ( Subset subset in GetSubsets( Wireframe ) )
		{
			Render( PrimitiveType.Lines, subset );
		}
		End();
	}

	protected override void CreateArray( IEnumerable<MapObject> objects )
	{
		//List<Entity> entities = objects.OfType<Entity>().Where( x => x.HasDecal() ).ToList();

		//StartSubset( Wireframe );

		//List<Tuple<Entity, Face>> decals = new List<Tuple<Entity, Face>>();
		//foreach ( Entity entity in entities.Where( x => x.HasDecal() ) )
		//{
		//	decals.AddRange( entity.GetDecalGeometry().Select( x => Tuple.Create( entity, x ) ) );
		//}

		//// Render decals
		//foreach ( Entity entity in entities )
		//{
		//	foreach ( Face face in entity.GetDecalGeometry() )
		//	{
		//		StartSubset( Transparent );
		//		face.IsSelected = entity.IsSelected;
		//		uint index = PushData( Convert( face ) );
		//		if ( !entity.IsRenderHidden3D ) PushIndex( Transparent, index, Triangulate( face.Vertices.Count ) );
		//		if ( !entity.IsRenderHidden2D ) PushIndex( Wireframe, index, Linearise( face.Vertices.Count ) );

		//		PushSubset( Transparent, face );
		//	}
		//}

		//PushSubset( Wireframe, (object)null );
	}

	protected IEnumerable<MapObjectVertex> Convert( Face face )
	{
		float nx = face.Plane.Normal.X,
		  ny = face.Plane.Normal.Y,
		  nz = face.Plane.Normal.Z;
		float r = face.Colour.R / 255f,
			  g = face.Colour.G / 255f,
			  b = face.Colour.B / 255f,
			  a = face.Opacity;
		return face.Vertices.Select( vert => new MapObjectVertex
		{
			Position = new OpenTK.Mathematics.Vector3( vert.Position.X, vert.Position.Y, vert.Position.Z ),
			Normal = new OpenTK.Mathematics.Vector3( nx, ny, nz ),
			TexCoords = new Vector2( vert.TextureU, vert.TextureV ),
			TexCoordsLM = new Vector2( vert.LightmapU, vert.LightmapV ),
			Color = new Color4( r, g, b, a ),
			IsSelected = face.IsSelected || (face.Parent != null && face.Parent.IsSelected) ? 1 : 0
		} );
	}
}
