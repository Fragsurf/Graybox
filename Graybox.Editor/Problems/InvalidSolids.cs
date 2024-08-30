using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Problems
{
	public class InvalidSolids : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			foreach ( MapObject invalid in map.WorldSpawn.Find( x => x is Solid && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) && !((Solid)x).IsValid() ) )
			{
				yield return new Problem( GetType(), map, new[] { invalid }, Fix, "Invalid solid", "This solid is invalid. It is either not convex, has coplanar faces, or has off-plane vertices. Fixing the issue will delete the solid." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new Delete( problem.Objects.Select( x => x.ID ) );
		}
	}
}
