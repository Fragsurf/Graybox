﻿using Graybox.Graphics;
using Graybox.DataStructures.Geometric;
using Graybox.DataStructures.MapObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Compiling.Lightmap
{
	public class LMFace
	{
		public Plane Plane { get; set; }
		public OpenTK.Mathematics.Vector3 Normal;
		public OpenTK.Mathematics.Vector3 Tangent;
		public OpenTK.Mathematics.Vector3 Bitangent;

		public OpenTK.Mathematics.Vector3 LightBasis0;
		public OpenTK.Mathematics.Vector3 LightBasis1;
		public OpenTK.Mathematics.Vector3 LightBasis2;

		public bool CastsShadows;

		public int LmIndex;

		public class Vertex
		{
			public Vertex( DataStructures.MapObjects.Vertex original )
			{
				OriginalVertex = original;
				Location = new OpenTK.Mathematics.Vector3( original.Position );
				DiffU = original.TextureU; DiffV = original.TextureV;
				LMU = original.LightmapU; LMV = original.LightmapV;
			}
			public OpenTK.Mathematics.Vector3 Location;
			public float DiffU; public float DiffV;
			public float LMU; public float LMV;
			public DataStructures.MapObjects.Vertex OriginalVertex;
		};

		public List<Vertex> Vertices { get; set; }

		public Box BoundingBox { get; set; }

		public string Texture;

		public Face OriginalFace;

		public LMFace( Face face, Solid solid )
		{
			Plane = face.Plane;
			Normal = Plane.Normal;
			Vertices = face.Vertices.Select( x => new Vertex( x ) ).ToList();
			CastsShadows = !(solid?.Parent?.GetEntityData()?.Name.Equals( "noshadow", StringComparison.OrdinalIgnoreCase ) ?? false);

			int i1 = 0;
			int i2 = 1;
			int i3 = 2;

			OpenTK.Mathematics.Vector3 v1 = Vertices[i1].Location;
			OpenTK.Mathematics.Vector3 v2 = Vertices[i2].Location;
			OpenTK.Mathematics.Vector3 v3 = Vertices[i3].Location;

			float w1x = Vertices[i1].DiffU; float w1y = Vertices[i1].DiffV;
			float w2x = Vertices[i2].DiffU; float w2y = Vertices[i2].DiffV;
			float w3x = Vertices[i3].DiffU; float w3y = Vertices[i3].DiffV;

			float x1 = v2.X - v1.X;
			float x2 = v3.X - v1.X;
			float y1 = v2.Y - v1.Y;
			float y2 = v3.Y - v1.Y;
			float z1 = v2.Z - v1.Z;
			float z2 = v3.Z - v1.Z;

			float s1 = w2x - w1x;
			float s2 = w3x - w1x;
			float t1 = w2y - w1y;
			float t2 = w3y - w1y;

			float r = 1.0f / (s1 * t2 - s2 * t1);
			OpenTK.Mathematics.Vector3 sdir = new OpenTK.Mathematics.Vector3( (t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r );
			OpenTK.Mathematics.Vector3 tdir = new OpenTK.Mathematics.Vector3( (s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r );

			Tangent = (sdir - Normal * Normal.Dot( sdir )).Normalized();
			Bitangent = (tdir - Normal * Normal.Dot( tdir )).Normalized();

			LightBasis0 = Tangent * (-1.0f / (float)Math.Sqrt( 6.0 )) + Bitangent * (-1.0f / (float)Math.Sqrt( 2.0 )) + Normal * (1.0f / (float)Math.Sqrt( 3.0 ));
			LightBasis1 = Tangent * (-1.0f / (float)Math.Sqrt( 6.0 )) + Bitangent * (1.0f / (float)Math.Sqrt( 2.0 )) + Normal * (1.0f / (float)Math.Sqrt( 3.0 ));
			LightBasis2 = Tangent * ((float)Math.Sqrt( 2.0 / 3.0 )) + Normal * (1.0f / (float)Math.Sqrt( 3.0 ));

			Texture = face.TextureRef.AssetPath;

			OriginalFace = face;

			UpdateBoundingBox();
		}

		public virtual IEnumerable<Line> GetLines()
		{
			return GetEdges();
		}

		public virtual IEnumerable<Line> GetEdges()
		{
			for ( int i = 0; i < Vertices.Count; i++ )
			{
				yield return new Line( Vertices[i].Location, Vertices[(i + 1) % Vertices.Count].Location );
			}
		}

		public virtual IEnumerable<Vertex> GetIndexedVertices()
		{
			return Vertices;
		}

		public virtual IEnumerable<uint> GetTriangleIndices()
		{
			for ( uint i = 1; i < Vertices.Count - 1; i++ )
			{
				yield return 0;
				yield return i;
				yield return i + 1;
			}
		}

		public virtual IEnumerable<Vertex[]> GetTriangles()
		{
			for ( int i = 1; i < Vertices.Count - 1; i++ )
			{
				yield return new[]
				{
					Vertices[0],
					Vertices[i],
					Vertices[i + 1]
				};
			}
		}

		public virtual void UpdateBoundingBox()
		{
			BoundingBox = new Box( Vertices.Select( x => x.Location ) );
		}

		/// <summary>
		/// Returns the point that this line intersects with this face.
		/// </summary>
		/// <param name="line">The intersection line</param>
		/// <returns>The point of intersection between the face and the line.
		/// Returns null if the line does not intersect this face.</returns>
		public virtual Vector3 GetIntersectionPoint( Line line )
		{
			return GetIntersectionPoint( this, line );
		}

		/// <summary>
		/// Test all the edges of this face against a bounding box to see if they intersect.
		/// </summary>
		/// <param name="box">The box to intersect</param>
		/// <returns>True if one of the face's edges intersects with the box.</returns>
		public bool IntersectsWithLine( Box box )
		{
			// Shortcut through the bounding box to avoid the line computations if they aren't needed
			return BoundingBox.IntersectsWith( box ) && GetLines().Any( box.IntersectsWith );
		}

		/// <summary>
		/// Test this face to see if the given bounding box intersects with it
		/// </summary>
		/// <param name="box">The box to test against</param>
		/// <returns>True if the box intersects</returns>
		public bool IntersectsWithBox( Box box )
		{
			List<Vertex> verts = Vertices.ToList();
			return box.GetBoxLines().Any( x => GetIntersectionPoint( this, x, true ) != null );
		}

		protected static Vector3 GetIntersectionPoint( LMFace face, Line line, bool ignoreDirection = false )
		{
			Plane plane = face.Plane;
			var intersect = plane.GetIntersectionPoint( line, ignoreDirection );
			if ( intersect == default ) return default;

			List<Vector3> coordinates = face.Vertices.Select( x => x.Location ).ToList();
			Box bbox = new Box( face.BoundingBox.Start - new Vector3( 0.5f, 0.5f, 0.5f ), face.BoundingBox.End + new OpenTK.Mathematics.Vector3( 0.5f, 0.5f, 0.5f ) );
			if ( !bbox.CoordinateIsInside( intersect ) ) return default;

			var centerPoint = face.BoundingBox.Center;
			for ( int i = 0; i < coordinates.Count; i++ )
			{
				int i1 = i;
				int i2 = (i + 1) % coordinates.Count;

				var lineMiddle = (coordinates[i1] + coordinates[i2]) / 2f;
				var middleToCenter = centerPoint - lineMiddle;
				var v = coordinates[i1] - coordinates[i2];
				var lineNormal = face.Plane.Normal.Cross( v );

				if ( (middleToCenter - lineNormal).LengthSquared > (middleToCenter + lineNormal).LengthSquared )
				{
					lineNormal = -lineNormal;
				}

				if ( lineNormal.Dot( intersect - lineMiddle ) < 0 ) return default;
			}
			return intersect;
		}

		public static void FindFacesAndGroups( Map map, out List<LMFace> faces, out List<LightmapGroup> lmGroups )
		{
			faces = new List<LMFace>();
			lmGroups = new List<LightmapGroup>();
			foreach ( Solid solid in map.WorldSpawn.Find( x => x is Solid ).OfType<Solid>() )
			{
				foreach ( Face tface in solid.Faces )
				{
					tface.Vertices.ForEach( v => { v.LightmapU = -500.0f; v.LightmapV = -500.0f; } );
					tface.UpdateBoundingBox();
					if ( tface.TextureRef?.Texture == null ) continue;
					if ( tface.TextureRef.IsToolTexture ) continue;
					if ( tface.TextureRef.Texture.HasTransparency ) continue;
					LMFace face = new LMFace( tface, solid );
					LightmapGroup group = LightmapGroup.FindCoplanar( lmGroups, face );
					Box faceBox = new Box( face.BoundingBox.Start - new OpenTK.Mathematics.Vector3( 3.0f, 3.0f, 3.0f ), face.BoundingBox.End + new OpenTK.Mathematics.Vector3( 3.0f, 3.0f, 3.0f ) );
					if ( group == null )
					{
						group = new LightmapGroup();
						group.BoundingBox = faceBox;
						group.Faces = new List<LMFace>();
						group.Plane = new Plane( face.Plane.Normal, face.Vertices[0].Location );
						lmGroups.Add( group );
					}
					group.Faces.Add( face );
					group.Plane = new Plane( group.Plane.Normal, (face.Vertices[0].Location + group.Plane.PointOnPlane) / 2 );
					group.BoundingBox = new Box( new Box[] { group.BoundingBox, faceBox } );
				}
			}
		}
	}
}
