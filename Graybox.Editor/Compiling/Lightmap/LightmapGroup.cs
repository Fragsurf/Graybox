using Graybox.DataStructures.Geometric;
using Graybox.Editor.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Compiling.Lightmap
{
	public class LightmapGroup
	{
		public Plane Plane;
		public Box BoundingBox;
		public List<LMFace> Faces;

		public Vector3? uAxis;
		public Vector3? vAxis;
		public float? minTotalX;
		public float? minTotalY;
		public float? maxTotalX;
		public float? maxTotalY;
		public int writeX;
		public int writeY;

		private void CalculateInitialUV()
		{
			if ( uAxis == null || vAxis == null )
			{
				var direction = Plane.GetClosestAxisToNormal();
				var tempV = direction == Vector3.UnitZ ? -Vector3.UnitY : -Vector3.UnitZ;
				uAxis = Plane.Normal.Cross( tempV ).Normalized();
				vAxis = uAxis.Value.Cross( Plane.Normal ).Normalized();

				if ( Plane.OnPlane( Plane.PointOnPlane + uAxis.Value * 1000f ) != 0 )
				{
					throw new Exception( "uAxis is misaligned" );
				}
				if ( Plane.OnPlane( Plane.PointOnPlane + vAxis.Value * 1000f ) != 0 )
				{
					throw new Exception( "vAxis is misaligned" );
				}
			}

			if ( minTotalX == null || minTotalY == null || maxTotalX == null || maxTotalY == null )
			{
				foreach ( LMFace face in Faces )
				{
					foreach ( var coord in face.Vertices.Select( x => x.Location ) )
					{
						float x = coord.Dot( uAxis.Value );
						float y = coord.Dot( vAxis.Value );

						if ( minTotalX == null || x < minTotalX ) minTotalX = x;
						if ( minTotalY == null || y < minTotalY ) minTotalY = y;
						if ( maxTotalX == null || x > maxTotalX ) maxTotalX = x;
						if ( maxTotalY == null || y > maxTotalY ) maxTotalY = y;
					}
				}

				minTotalX -= LightmapConfig.DownscaleFactor; minTotalY -= LightmapConfig.DownscaleFactor;
				maxTotalX += LightmapConfig.DownscaleFactor; maxTotalY += LightmapConfig.DownscaleFactor;

				minTotalX /= LightmapConfig.DownscaleFactor; minTotalX = (float)Math.Ceiling( minTotalX.Value ); minTotalX *= LightmapConfig.DownscaleFactor;
				minTotalY /= LightmapConfig.DownscaleFactor; minTotalY = (float)Math.Ceiling( minTotalY.Value ); minTotalY *= LightmapConfig.DownscaleFactor;
				maxTotalX /= LightmapConfig.DownscaleFactor; maxTotalX = (float)Math.Ceiling( maxTotalX.Value ); maxTotalX *= LightmapConfig.DownscaleFactor;
				maxTotalY /= LightmapConfig.DownscaleFactor; maxTotalY = (float)Math.Ceiling( maxTotalY.Value ); maxTotalY *= LightmapConfig.DownscaleFactor;

				if ( (maxTotalX - minTotalX) < (maxTotalY - minTotalY) )
				{
					SwapUV();
				}
			}
		}

		public float Width
		{
			get
			{
				CalculateInitialUV();
				return (maxTotalX - minTotalX).Value;
			}
		}

		public float Height
		{
			get
			{
				CalculateInitialUV();
				return (maxTotalY - minTotalY).Value;
			}
		}

		public void SwapUV()
		{
			float maxSwap = maxTotalX.Value; float minSwap = minTotalX.Value;
			maxTotalX = maxTotalY; minTotalX = minTotalY;
			maxTotalY = maxSwap; minTotalY = minSwap;

			var swapAxis = uAxis.Value;
			uAxis = vAxis;
			vAxis = swapAxis;
		}

		public static LightmapGroup FindCoplanar( List<LightmapGroup> lmGroups, LMFace otherFace )
		{
			foreach ( LightmapGroup group in lmGroups )
			{
				if ( (group.Plane.Normal - otherFace.Plane.Normal).LengthSquared < float.Epsilon )
				{
					Plane plane2 = new Plane( otherFace.Plane.Normal, otherFace.Vertices[0].Location );
					if ( Math.Abs( plane2.EvalAtPoint( (group.Plane.PointOnPlane) ) ) > 4.0f ) continue;
					Box faceBox = new Box( otherFace.BoundingBox.Start - new OpenTK.Mathematics.Vector3( 3.0f, 3.0f, 3.0f ), otherFace.BoundingBox.End + new OpenTK.Mathematics.Vector3( 3.0f, 3.0f, 3.0f ) );
					if ( faceBox.IntersectsWith( group.BoundingBox ) ) return group;
				}
			}
			return null;
		}
	}
}
