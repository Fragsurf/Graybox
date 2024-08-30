
using Jitter2.Collision;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.Collision.Shapes;
using MapObject = Graybox.DataStructures.MapObjects.MapObject;
using Solid = Graybox.DataStructures.MapObjects.Solid;
using Entity = Graybox.DataStructures.MapObjects.Entity;
using Face = Graybox.DataStructures.MapObjects.Face;
using Group = Graybox.DataStructures.MapObjects.Group;
using Graybox.DataStructures.MapObjects;

namespace Graybox.Scenes.Physics
{
	public class Jitter2Scene : IPhysicsScene
	{

		Jitter2.World PhysicsWorld;
		Scene Scene;
		Dictionary<MapObject, RigidBody> BodyMap;
		List<Shape> CandidateList = new();

		public Jitter2Scene( Scene scene )
		{
			Scene = scene;
			PhysicsWorld = new();
			BodyMap = new();
		}

		public List<PenetrationResult> OverlapBox( Vector3 position, Vector3 mins, Vector3 maxs )
		{
			if ( PhysicsWorld.Shapes.Count == 0 )
				return new List<PenetrationResult>();

			Vector3 boxCenter = (maxs + mins) / 2;
			Vector3 boxSize = maxs - mins;
			Vector3 worldCenter = position + boxCenter;

			var shape = new BoxShape( ToJitter( boxSize ) );
			var origin = ToJitter( worldCenter );
			var box = new JBBox( origin - ToJitter( boxSize * .5f ), origin + ToJitter( boxSize * .5f ) );

			CandidateList.Clear();
			PhysicsWorld.DynamicTree.Query( CandidateList, box );
			var result = new List<PenetrationResult>();

			foreach ( var worldShape in CandidateList )
			{
				if ( NarrowPhase.GJKEPA( worldShape, shape, JQuaternion.Identity, JQuaternion.Identity, worldShape.RigidBody.Position, origin, out var pointA, out var pointB, out var normal, out var penetration ) )
				{
					if ( penetration <= 0 ) continue;
					var pen = new PenetrationResult()
					{
						Distance = penetration,
						SeparationVector = ToOpenTK( normal )
					};
					result.Add( pen );
				}
			}

			return result;
		}


		public TraceResult TraceBox( Vector3 mins, Vector3 maxs, Vector3 start, Vector3 end )
		{
			if ( PhysicsWorld.Shapes.Count == 0 )
				return default;

			Vector3 boxCenter = (maxs + mins) / 2;
			Vector3 boxSize = maxs - mins;
			Vector3 worldCenterStart = start + boxCenter;
			Vector3 worldCenterEnd = end + boxCenter;

			var shape = new BoxShape( ToJitter( boxSize ) );
			var direction = ToJitter( worldCenterEnd - worldCenterStart );
			var distance = direction.Length();

			var startBounds = new Bounds( worldCenterStart - boxSize * .5f, worldCenterStart + boxSize * .5f );
			var endBounds = new Bounds( worldCenterEnd - boxSize * .5f, worldCenterEnd + boxSize * .5f );

			var sampleBounds = startBounds.Encapsulate( endBounds );
			var jSampleBounds = new JBBox( ToJitter( sampleBounds.Mins ), ToJitter( sampleBounds.Maxs ) );

			CandidateList.Clear();
			PhysicsWorld.DynamicTree.Query( CandidateList, jSampleBounds );

			var result = new TraceResult();
			var sweepStart = ToJitter( worldCenterStart );

			foreach ( var worldShape in CandidateList )
			{
				var sweep = NarrowPhase.SweepTest( worldShape, shape, JQuaternion.Identity, JQuaternion.Identity, worldShape.RigidBody.Position, sweepStart, JVector.Zero, direction, out var pointA, out var pointB, out var sweepNormal, out var sweepFraction );
				var dist = sweepFraction * distance;
				if ( sweep && dist <= distance )
				{
					var dirNormal = direction;
					dirNormal.Normalize();
					result.Hit = true;
					result.Normal = ToOpenTK( sweepNormal );
					result.Object = shape.RigidBody?.Tag as MapObject;
					result.Position = start + ToOpenTK( dirNormal * distance * sweepFraction );
					result.Tag = shape?.Tag;
					break;
				}
			}

			return result;
		}

		public TraceResult EdgeTraceBox( Vector3 mins, Vector3 maxs, Vector3 start, Vector3 end )
		{
			if ( PhysicsWorld.Shapes.Count == 0 )
				return default;

			var box = new Bounds( start + mins, start + maxs ).Encapsulate( end );
			var jbbox = new JBBox( ToJitter( box.Mins ), ToJitter( box.Maxs ) );

			CandidateList.Clear();
			PhysicsWorld.DynamicTree.Query( CandidateList, jbbox );

			var result = new TraceResult();

			foreach ( var worldShape in CandidateList )
			{
				if ( worldShape?.RigidBody?.Tag is MapObject obj )
				{
					var faces = obj.CollectFaces();
					foreach ( var face in faces )
					{
						foreach ( var edge in face.GetEdges() )
						{
							var a = edge.Start;
							var b = edge.End;
							if ( box.LineIntersects( a, b ) )
							{
								result.Hit = true;
								result.Object = obj;
								result.Tag = worldShape?.Tag;
								break;
							}
						}
					}
				}
			}

			return result;
		}

		public TraceResult Trace<T>( Vector3 start, Vector3 direction, float maxDistance = 50000, bool withTriggers = true ) where T : MapObject
		{
			if ( PhysicsWorld.Shapes.Count == 0 )
				return default;

			var origin = ToJitter( start );
			var dir = ToJitter( direction );
			dir.Normalize();
			dir *= maxDistance;

			var result = new TraceResult();

			if ( PhysicsWorld.RayCast( origin, dir, x => TypeCheck( x, typeof( T ), withTriggers ), x => EdgeDetectionCheck( origin, dir, x ), out var shape, out var normal, out var fraction ) )
			{
				normal.Normalize();
				result.Hit = true;
				result.Normal = ToOpenTK( normal );
				result.Object = shape.RigidBody.Tag as MapObject;
				result.Position = start + ToOpenTK( dir * fraction );
				result.Tag = shape?.Tag;
			}

			return result;
		}

		bool EdgeDetectionCheck( JVector origin, JVector dir, Jitter2.World.RayCastResult result )
		{
			return true;
			//if ( result.Entity?.RigidBody?.Tag is Solid s )
			//{
			//	var hitPoint = origin + dir * result.Fraction;
			//	var box = new Bounds( ToOpenTK( hitPoint ), 32f );
			//	foreach ( var face in s.Faces )
			//	{
			//		foreach ( var edge in face.GetEdges() )
			//		{
			//			var a = (Vector3)edge.Start;
			//			var b = (Vector3)edge.End;
			//			if ( box.LineIntersects( a, b ) )
			//			{
			//				return true;
			//			}
			//		}
			//	}

			//	return false;
			//}

			//return true;
		}

		bool TypeCheck( Shape shape, Type type, bool withTriggers )
		{
			var rb = shape.RigidBody;
			var obj = rb?.Tag as MapObject;

			if ( obj == null ) return false;

			var objType = obj.GetType();
			if ( objType == type || type.IsAssignableFrom( objType ) )
			{
				if ( obj is Solid s && !withTriggers && s.IsTrigger() )
					return false;
				return true;
			}

			return false;
		}

		public void Remove( MapObject obj )
		{
			if ( !BodyMap.TryGetValue( obj, out RigidBody value ) )
				return;

			PhysicsWorld.Remove( value );
			BodyMap.Remove( obj );
		}

		public void Add( MapObject obj )
		{
			Remove( obj );

			var rb = GetRigidBody( obj );
			if ( rb == null ) return;

			BodyMap[obj] = rb;
		}

		RigidBody GetRigidBody( MapObject obj )
		{
			if ( obj.BoundingBox == null )
			{
				Debug.LogError( "NULL BOUNDING BOX" );
				return null;
			}
			var center = ToJitter( obj.BoundingBox.Center );
			var rb = PhysicsWorld.CreateRigidBody();
			rb.Position = center;
			rb.Tag = obj;
			rb.IsStatic = true;

			if ( obj is Solid s )
			{
				var shapes = GetTriangleShapes( new[] { s }, center );
				if ( shapes != null )
				{
					rb.AddShape( shapes );
				}
			}

			if ( obj is Entity e && !e.HasChildren )
			{
				rb.AddShape( new BoxShape( new JVector( 32 ) ) );
			}

			if ( obj is Light l )
			{
				rb.AddShape( new BoxShape( new JVector( 32 ) ) );
			}

			return rb;
		}

		List<Shape> GetTriangleShapes( IEnumerable<Solid> solids, JVector centerOffset )
		{
			var result = new List<Shape>();
			var tries = new List<JTriangle>();
			var faceMapping = new List<Face>();

			foreach ( var s in solids )
			{
				foreach ( var face in s.Faces )
				{
					foreach ( var tri in face.GetTriangles() )
					{
						var pos1 = ToJitter( tri[0].Position ) - centerOffset;
						var pos2 = ToJitter( tri[1].Position ) - centerOffset;
						var pos3 = ToJitter( tri[2].Position ) - centerOffset;
						tries.Add( new JTriangle( pos1, pos2, pos3 ) );
						faceMapping.Add( face );
					}
				}
			}

			try
			{
				var triMesh = new TriangleMesh( tries );
				for ( int i = 0; i < tries.Count; i++ )
				{
					var shape = new TriangleShape( triMesh, i );
					shape.Tag = faceMapping[i];
					result.Add( shape );
				}

				return result;
			}
			catch ( Exception e )
			{
				Debug.LogError( "Failed to create triangle mesh: " + e.Message );
				return null;
			}
		}

		static Vector3 ToOpenTK( JVector vec ) => new Vector3( vec.X, vec.Y, vec.Z );
		static JVector ToJitter( Vector3 vec ) => new JVector( vec.X, vec.Y, vec.Z );

	}

	public struct PenetrationResult
	{
		public float Distance;
		public Vector3 SeparationVector;
	}

	public struct TraceResult
	{
		public bool Hit;
		public MapObject Object;
		public Face Face;
		public Vector3 Normal;
		public Vector3 Position;
		public object Tag;
	}

}
