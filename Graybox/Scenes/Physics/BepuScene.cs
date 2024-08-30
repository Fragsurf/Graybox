
using Graybox.DataStructures.MapObjects;
using System;
using System.Collections.Generic;
using BEPUphysics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionShapes;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Settings;
using System.Linq;
using Assimp;

namespace Graybox.Scenes.Physics
{
	internal class BepuScene : IPhysicsScene
	{

		Space Physics = new();
		Scene Scene;

		public BepuScene( Scene scene )
		{
			Scene = scene;
		}

		public BEPUutilities.Vector3 ToBepu( Vector3 v ) => new BEPUutilities.Vector3( v.X, v.Y, v.Z );
		public Vector3 FromBepu( BEPUutilities.Vector3 v ) => new Vector3( v.X, v.Y, v.Z );

		public TraceResult Trace<T>( Vector3 start, Vector3 direction, float maxDistance = 50000, bool withTriggers = true ) where T : MapObject
		{
			var origin = ToBepu( start );
			var dir = ToBepu( direction.Normalized() );
			var ray = new BEPUutilities.Ray( origin, dir );

			var result = new TraceResult();

			if ( Physics.RayCast( ray, out var trace ) )
			{
				result.Hit = true;
				result.Object = trace.HitObject?.Tag as MapObject;
				result.Normal = FromBepu( trace.HitData.Normal );
				result.Position = FromBepu( trace.HitData.Location );

				Scene.Gizmos.Line( result.Position, result.Position + result.Normal * 100f, new( 1, 0, 0 ), 2f );
			}

			return result;
		}

		public List<PenetrationResult> OverlapBox( Vector3 position, Vector3 mins, Vector3 maxs )
		{
			throw new System.NotImplementedException();
		}

		public TraceResult TraceBox( Vector3 mins, Vector3 maxs, Vector3 start, Vector3 end )
		{
			throw new NotImplementedException();
		}

		public void Remove( MapObject obj )
		{

		}

		public void Add( MapObject obj )
		{

		}

		EntityCollidable GetShape( MapObject obj, out Vector3 position )
		{
			position = default;

			if ( obj is Solid s )
			{
				var center = (Vector3)s.BoundingBox.Center;
				var points = new List<BEPUutilities.Vector3>();
				position = center;
				foreach ( var face in s.Faces )
				{
					var vv = face.GetLineIndices();
					foreach ( var v in vv )
					{
						points.Add( ToBepu( (Vector3)face.Vertices[(int)v].Position - center ) );
					}
				}
				BEPUutilities.ConvexHullHelper.RemoveRedundantPoints( points, 10 );
				return new ConvexHullShape( points ).GetCollidableInstance();
			}

			return null;
		}

		public TraceResult EdgeTraceBox( Vector3 mins, Vector3 maxs, Vector3 start, Vector3 end )
		{
			throw new NotImplementedException();
		}
	}

}
