
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics.Helpers;
using System.Drawing;

namespace Graybox.Graphics.Immediate;

public static class MapObjectRenderer
{

	public static Color Tint( Vector3 sun, Vector3 normal, Color c )
	{
		double tintvar = (double)sun.Normalized().Dot( normal.Normalized() );
		// tint variation = 128
		int diff = (int)(64 * (tintvar + 1));

		return Color.FromArgb( c.A, Math.Max( 0, c.R - diff ), Math.Max( 0, c.G - diff ), Math.Max( 0, c.B - diff ) );
	}

	private static void GLCoordinateTriangle( Vertex v1, Vertex v2, Vertex v3 )
	{
		var p1 = v1.Position;
		var p2 = v2.Position;
		var p3 = v3.Position;
		var normal = (p3 - p1).Cross( p2 - p1 ).Normalized();
		GLCoordinate( v1, normal );
		GLCoordinate( v2, normal );
		GLCoordinate( v3, normal );
	}

	private static void GLCoordinate( Vertex v, Vector3 normal, float offset = 0f )
	{
		GL.TexCoord2( v.TextureU, v.TextureV );
		GL.Normal3( normal.X, normal.Y, normal.Z );
		GLCoordinate( v.Position + normal.Normalized() * offset );
	}

	private static void GLCoordinate( Vector3 c )
	{
		GL.Vertex3( c.X, c.Y, c.Z );
	}

	private static float GetOpacity( ITexture texture, Face face )
	{
		return face.Opacity;
	}

	public static void DrawFilledNoFucks( IEnumerable<Face> faces, Color color, bool textured, bool blend = true, float offset = 0f )
	{
		if ( color.IsEmpty ) color = Color.White;

		GL.Begin( PrimitiveType.Triangles );
		GL.Color4( color );

		var texgroups = from f in faces
						group f by new
						{
							f.TextureRef.Texture,
							Opacity = GetOpacity( f.TextureRef.Texture, f ),
							Transparent = GetOpacity( f.TextureRef.Texture, f ) < 0.9 || (f.TextureRef.Texture != null && f.TextureRef.Texture.HasTransparency)
						}
							into g
						select g;
		foreach ( var g in texgroups.OrderBy( x => x.Key.Transparent ? 1 : 0 ) )
		{
			bool texture = false;
			float alpha = g.Key.Opacity * 255;
			byte blendAlpha = (byte)((color.A) / 255f * (alpha / 255f) * 255);
			GL.End();
			if ( g.Key.Texture != null && textured )
			{
				texture = true;
				GL.Color4( Color.FromArgb( blendAlpha, color ) );
				g.Key.Texture.Bind();
			}
			else
			{
				GL.BindTexture( TextureTarget.Texture2D, 0 );
			}
			GL.Begin( PrimitiveType.Triangles );
			foreach ( Face f in g )
			{
				bool disp = f is Displacement;
				var finalColor = blend ? f.Colour.Blend( color ) : color;
				finalColor.A = blendAlpha / 255f;
				GL.Color4( finalColor );
				foreach ( Vertex[] tri in f.GetTriangles() )
				{
					if ( disp )
					{
						GLCoordinateTriangle( tri[0], tri[1], tri[2] );
					}
					else
					{
						GLCoordinate( tri[0], f.Plane.Normal, offset );
						GLCoordinate( tri[1], f.Plane.Normal, offset );
						GLCoordinate( tri[2], f.Plane.Normal, offset );
					}
				}
				GL.Color3( Color.White );
			}
		}

		GL.End();
		GL.Color4( Color.White );
	}

	public static void DrawFilled( IEnumerable<Face> faces, Color color, bool textured, bool blend = true )
	{
		if ( color.IsEmpty ) color = Color.White;
		faces = faces.Where( x => x.Parent == null || !(x.Parent.IsCodeHidden || x.Parent.IsVisgroupHidden || x.Parent.IsRenderHidden3D) );

		GL.Begin( PrimitiveType.Triangles );
		GL.Color4( color );

		var texgroups = from f in faces
						group f by new
						{
							f.TextureRef.Texture,
							Opacity = GetOpacity( f.TextureRef.Texture, f ),
							Transparent = GetOpacity( f.TextureRef.Texture, f ) < 0.9 || (f.TextureRef.Texture != null && f.TextureRef.Texture.HasTransparency)
						}
							into g
						select g;
		foreach ( var g in texgroups.OrderBy( x => x.Key.Transparent ? 1 : 0 ) )
		{
			bool texture = false;
			float alpha = g.Key.Opacity * 255;
			byte blendAlpha = (byte)((color.A) / 255f * (alpha / 255f) * 255);
			GL.End();
			if ( g.Key.Texture != null && textured )
			{
				texture = true;
				GL.Color4( Color.FromArgb( blendAlpha, color ) );
				g.Key.Texture.Bind();
			}
			else
			{
				GL.BindTexture( TextureTarget.Texture2D, 0 );
			}
			GL.Begin( PrimitiveType.Triangles );
			foreach ( Face f in g )
			{
				bool disp = f is Displacement;
				var finalColor = texture ? f.Colour.Blend( color ) : color;
				finalColor.A = blendAlpha / 255f;
				GL.Color4( finalColor );
				foreach ( Vertex[] tri in f.GetTriangles() )
				{
					if ( disp )
					{
						GLCoordinateTriangle( tri[0], tri[1], tri[2] );
					}
					else
					{
						GLCoordinate( tri[0], f.Plane.Normal );
						GLCoordinate( tri[1], f.Plane.Normal );
						GLCoordinate( tri[2], f.Plane.Normal );
					}
				}
				GL.Color3( Color.White );
			}
		}

		GL.End();
		GL.Color4( Color.White );
	}

	public static void DrawWireframe( IEnumerable<Face> faces, bool overrideColor, bool drawVertices )
	{
		GL.BindTexture( TextureTarget.Texture2D, 0 );
		GL.Begin( PrimitiveType.Lines );

		foreach ( Face f in faces )
		{
			if ( !overrideColor ) GL.Color4( f.Colour );
			foreach ( Line line in f.GetLines() )
			{
				GLCoordinate( line.Start );
				GLCoordinate( line.End );
			}
		}

		GL.End();

		if ( !drawVertices ) return;

		GL.PointSize( 3 );
		GL.Begin( PrimitiveType.Points );
		GL.Color4( 1f, 1, 1, 1 );
		foreach ( Face f in faces )
		{
			foreach ( Line line in f.GetLines() )
			{
				GLCoordinate( line.Start );
				GLCoordinate( line.End );
			}
		}
		GL.End();
	}

}
