using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using System.Collections.Generic;

namespace Graybox.Editor.Problems
{
	public interface IProblemCheck
	{
		IEnumerable<Problem> Check( Map map, bool visibleOnly );
		IAction Fix( Problem problem );
	}
}
