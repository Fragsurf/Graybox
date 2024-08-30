using Graybox.DataStructures.MapObjects;
using System.Collections.Generic;

namespace Graybox.Editor.Actions.MapObjects.Selection
{
    public class SelectFace : ChangeFaceSelection
    {
        public SelectFace(IEnumerable<Face> objects) : base(objects, new Face[0])
        {
        }

        public SelectFace(params Face[] objects) : base(objects, new Face[0])
        {
        }
    }
}