
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions.MapObjects.Operations
{
    public class Reparent : IAction
    {
        private class ReparentReference
        {
            public long ID { get; set; }
            public long OriginalParentID { get; set; }
        }

        public bool SkipInStack { get { return false; } }
        public bool ModifiesState { get { return true; } }

        private readonly long _parentId;
        private List<ReparentReference> _objects;

        public Reparent(long parentId, IEnumerable<MapObject> objects)
        {
            _parentId = parentId;
            _objects = objects.Select(x => new ReparentReference
            {
                ID = x.ID,
                OriginalParentID = x.Parent.ID
            }).ToList();
        }

        public void Dispose()
        {
            _objects = null;
        }

        public void Reverse(Document document)
        {
            Dictionary<long, MapObject> parents = _objects.Select(x => x.OriginalParentID)
                .Distinct()
                .ToDictionary(x => x, x => document.Map.WorldSpawn.FindByID(x));
            foreach (ReparentReference o in _objects)
            {
                MapObject obj = document.Map.WorldSpawn.FindByID(o.ID);
                if (obj == null) continue;

                obj.SetParent(parents[o.OriginalParentID]);
            }

            EventSystem.Publish(EditorEvents.DocumentTreeStructureChanged);
            EventSystem.Publish(EditorEvents.VisgroupsChanged);
        }

        public void Perform(Document document)
        {
            MapObject parent = document.Map.WorldSpawn.FindByID(_parentId);
            foreach (ReparentReference o in _objects)
            {
                MapObject obj = document.Map.WorldSpawn.FindByID(o.ID);
                if (obj == null) continue;
                obj.SetParent(parent);
            }

            EventSystem.Publish(EditorEvents.DocumentTreeStructureChanged);
            EventSystem.Publish(EditorEvents.VisgroupsChanged);
        }
    }
}
