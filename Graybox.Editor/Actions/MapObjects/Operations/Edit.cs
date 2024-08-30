using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions.MapObjects.Operations.EditOperations;
using System.Collections.Generic;

namespace Graybox.Editor.Actions.MapObjects.Operations
{
    /// <summary>
    /// Perform: Changes the given objects (by ID) to the "after" state.
    /// Reverse: Changes the given objects (by ID) to the "before" state.
    /// </summary>
    public class Edit : CreateEditDelete
    {
        public Edit(IEnumerable<MapObject> before, IEnumerable<MapObject> after)
        {
            Edit(before, after);
        }

        public Edit(IEnumerable<MapObject> objects, IEditOperation editOperation)
        {
            Edit(objects, editOperation);
        }
    }
}
