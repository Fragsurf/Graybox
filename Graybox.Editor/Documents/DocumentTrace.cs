
using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Documents
{
	public class DocumentTrace
	{

		Document Document;

		public DocumentTrace( Document document )
		{
			Document = document;
		}

		public Face FirstFace( Graybox.Scenes.Scene scene, Vector2 screenPos, bool ignoreTriggers = true )
		{
			if ( !scene.Camera.Orthographic )
			{
				var ray = scene.ScreenToRay( screenPos );
				var line = new Line( ray.Origin, ray.Origin + ray.Direction * 50000 );
				var isect = Document.Map.WorldSpawn.GetAllNodesIntersectingWith( line )
					.OfType<Solid>()
					.Where( x => !ignoreTriggers || ( !(x.Parent is Entity ee) || !ee.ClassName.StartsWith( "trigger_", System.StringComparison.InvariantCultureIgnoreCase ) ) )
					.SelectMany( x => x.Faces )
					.Select( x => new { Item = x, Intersection = x.GetIntersectionPoint( line ) } )
					.Where( x => x.Intersection != default )
					.OrderBy( x => (x.Intersection - line.Start).VectorMagnitude() )
					.FirstOrDefault();

				return isect?.Item;
			}

			return null;
		}

	}
}
