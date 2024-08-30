
using Graybox.DataStructures.MapObjects;

namespace Graybox.Lightmapper;

public class LightmapFace
{

	public Solid Solid;
	public Face Face;
	public Rect UVBounds;
	public List<LightmapSample> Samples;

	public IEnumerable<LightmapSample> GenerateSamples()
	{
		var face = Face;
		var offset = new Vector2( UVBounds.X, UVBounds.Y );
		var size = new Vector2( UVBounds.Width, UVBounds.Height );
		var normal = face.Plane.Normal;

		var center = Vector3.Zero;
		foreach ( var vert in face.Vertices )
		{
			center += vert.Position;
		}
		center /= face.Vertices.Count;

		var direction = face.Plane.GetClosestAxisToNormal();
		var tempV = direction == Vector3.UnitZ ? Vector3.UnitY : Vector3.UnitZ;
		var uAxis = face.Plane.Normal.Cross( tempV ).Normalized();
		var vAxis = uAxis.Cross( face.Plane.Normal ).Normalized();

		float minU = float.MaxValue, minV = float.MaxValue;
		float maxU = float.MinValue, maxV = float.MinValue;

		foreach ( var vertex in face.Vertices )
		{
			var u = Vector3.Dot( vertex.Position - center, uAxis );
			var v = Vector3.Dot( vertex.Position - center, vAxis );
			minU = Math.Min( minU, u );
			minV = Math.Min( minV, v );
			maxU = Math.Max( maxU, u );
			maxV = Math.Max( maxV, v );
		}

		var origin = center;
		var uRange = maxU - minU;
		var vRange = maxV - minV;

		float uStep = uRange / size.X;
		float vStep = vRange / size.Y;

		for ( int x = 0; x < size.X; x++ )
		{
			for ( int y = 0; y < size.Y; y++ )
			{
				float u = minU + uStep * (x + 0.5f);
				float v = minV + vStep * (y + 0.5f);

				var sampleOrigin = origin + (u * uAxis) + (v * vAxis);

				var texCoords = new Vector2(
					MathF.Floor( offset.X + x ),
					MathF.Floor( offset.Y + y )
				);

				yield return new LightmapSample
				{
					Origin = sampleOrigin,
					Normal = normal,
					TexCoords = texCoords,
					FaceCenter = center,
					ObjectID = this.Solid.ID
				};
			}
		}
	}

}
