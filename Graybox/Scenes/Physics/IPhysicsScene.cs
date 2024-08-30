
using Graybox.DataStructures.MapObjects;

namespace Graybox.Scenes.Physics
{
	public interface IPhysicsScene
	{
		public void Remove( MapObject obj );
		public void Add( MapObject obj );
		public TraceResult EdgeTraceBox( Vector3 mins, Vector3 maxs, Vector3 start, Vector3 end );
		public TraceResult TraceBox( Vector3 mins, Vector3 maxs, Vector3 start, Vector3 end );
		public TraceResult Trace<T>( Vector3 start, Vector3 direction, float maxDistance = 50000, bool withTriggers = true ) where T : MapObject;
		public TraceResult Trace( Ray ray ) => Trace<MapObject>( ray.Origin, ray.Direction.Normalized() );
		public TraceResult Trace<T>( Ray ray ) where T : MapObject => Trace<T>( ray.Origin, ray.Direction.Normalized() );
		public TraceResult Trace( Vector3 start, Vector3 direction ) => Trace<MapObject>( start, direction );
		public List<PenetrationResult> OverlapBox( Vector3 position, Vector3 mins, Vector3 maxs );
	}
}
