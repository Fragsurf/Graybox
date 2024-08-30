using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Problems
{
	public class DuplicateObjectIDs : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			IEnumerable<IGrouping<long, MapObject>> dupes = from o in map.WorldSpawn.Find( x => (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
															group o by o.ID
						into g
															where g.Count() > 1
															select g;
			foreach ( IGrouping<long, MapObject> dupe in dupes )
			{
				yield return new Problem( GetType(), map, dupe, Fix, "Multiple objects have the same ID", "More than one object has the same ID. Each object ID should be unique. Fixing the problem will assign the duplicates new IDs." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new ReplaceObjects( problem.Objects, problem.Objects.Select( x =>
			{
				MapObject c = x.Clone();
				c.ID = problem.Map.IDGenerator.GetNextObjectID();
				return c;
			} ) );
		}
	}
}
