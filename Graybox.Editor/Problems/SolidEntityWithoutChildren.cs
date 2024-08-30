using Graybox.DataStructures.GameData;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Problems
{
	public class SolidEntityWithoutChildren : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			foreach ( Entity entity in map.WorldSpawn
				.Find( x => x is Entity && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
				.OfType<Entity>()
				.Where( x => x.GameData != null )
				.Where( x => x.GameData.ClassType == ClassType.Solid )
				.Where( x => !x.GetChildren().SelectMany( y => y.FindAll() ).Any( y => y is Solid ) ) )
			{
				yield return new Problem( GetType(), map, new[] { entity }, Fix, "Brush entity has no solid children", "A brush entity with no solid children was found. A brush entity must have solid contents. Fixing the problem will delete the entity." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new Delete( problem.Objects.Select( x => x.ID ) );
		}
	}
}
