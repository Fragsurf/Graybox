using Graybox.DataStructures.MapObjects;
using System.Collections.Generic;

namespace Graybox.Editor.Actions.MapObjects.Selection
{
    public class DeselectFace : ChangeFaceSelection
    {
        public DeselectFace(IEnumerable<Face> objects) : base(new Face[0], objects)
        {
        }

        public DeselectFace(params Face[] objects) : base(new Face[0], objects)
        {
        }
    }
}