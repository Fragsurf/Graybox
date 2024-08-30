using Graybox.DataStructures.MapObjects;
using System.Collections.Generic;

namespace Graybox.Editor.Actions.MapObjects.Selection
{
    public class Deselect : ChangeSelection
    {
        public Deselect(IEnumerable<MapObject> objects) : base(new MapObject[0], objects)
        {
        }
    }
}