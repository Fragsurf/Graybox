using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Problems
{
	public class GroupWithoutChildren : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			foreach ( Group group in map.WorldSpawn
				.Find( x => x is Group && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
				.OfType<Group>()
				.Where( x => !x.GetChildren().Any() ) )
			{
				yield return new Problem( GetType(), map, new[] { @group }, Fix, "Group has no children", "This group is empty. A group must have contents. Fixing the problem will delete the group." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new Delete( problem.Objects.Select( x => x.ID ) );
		}
	}
}
