using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Problems
{
	public class SolidWithChildren : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			foreach ( Solid solid in map.WorldSpawn
				.Find( x => x is Solid && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
				.OfType<Solid>()
				.Where( x => x.HasChildren ) )
			{
				yield return new Problem( GetType(), map, new[] { solid }, Fix, "Solid has children", "A solid with children was found. A solid cannot have any contents. Fixing the issue will move the children outside of the solid's group." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new Reparent( problem.Objects[0].Parent.ID, problem.Objects[0].GetChildren() );
		}
	}
}
