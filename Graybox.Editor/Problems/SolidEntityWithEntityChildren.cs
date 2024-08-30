using Graybox.DataStructures.GameData;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Problems
{
	public class SolidEntityWithEntityChildren : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			foreach ( Entity entity in map.WorldSpawn
				.Find( x => x is Entity && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
				.OfType<Entity>()
				.Where( x => x.GameData != null )
				.Where( x => x.GameData.ClassType == ClassType.Solid )
				.Where( x => x.GetChildren().SelectMany( y => y.FindAll() ).Any( y => !(y is Group) && !(y is Solid) ) ) )
			{
				yield return new Problem( GetType(), map, new[] { entity }, Fix, "Brush entity has child entities", "A brush entity with child entities was found. A brush entity must only have solid contents. Fixing the problem will move the child entities outside of the entity's group." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new Reparent( problem.Objects[0].Parent.ID, problem.Objects[0].GetChildren().SelectMany( x => x.Find( y => y is Entity ) ) );
		}
	}
}
