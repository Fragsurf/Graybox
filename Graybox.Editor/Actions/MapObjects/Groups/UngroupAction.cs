
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions.MapObjects.Groups
{
    public class UngroupAction : IAction
    {
        private Dictionary<long, long> _groupsAndParents;
        private Dictionary<long, long> _childrenAndParents;

        public bool SkipInStack { get { return false; } }
        public bool ModifiesState { get { return true; } }

        public UngroupAction(IEnumerable<MapObject> objects)
        {
            List<Group> objs = objects.Where(x => x != null && x.Parent != null).OfType<Group>().ToList();
            _groupsAndParents = objs.ToDictionary(x => x.ID, x => x.Parent.ID);
            _childrenAndParents = objs.SelectMany(x => x.GetChildren()).ToDictionary(x => x.ID, x => x.Parent.ID);
        }

        public void Perform(Document document)
        {
            foreach (MapObject child in _childrenAndParents.Keys.Select(x => document.Map.WorldSpawn.FindByID(x)))
            {
                child.SetParent(document.Map.WorldSpawn);
                child.UpdateBoundingBox();
                child.Colour = ColorUtility.GetRandomBrushColour();
            }

            foreach (long groupId in _groupsAndParents.Keys)
            {
                MapObject group = document.Map.WorldSpawn.FindByID(groupId);
                if (group == null) continue;

                if (group.IsSelected)
                {
                    document.Selection.Deselect(group);
                }

                group.SetParent(null);
            }

            EventSystem.Publish(EditorEvents.SelectionChanged);
            EventSystem.Publish(EditorEvents.DocumentTreeStructureChanged);
        }

        public void Reverse(Document document)
        {
            foreach (KeyValuePair<long, long> gp in _groupsAndParents)
            {
                Group group = new Group(gp.Key) { Colour = ColorUtility.GetRandomGroupColour() };
                MapObject parent = document.Map.WorldSpawn.FindByID(gp.Value);
                group.SetParent(parent);
            }
            foreach (KeyValuePair<long, long> cp in _childrenAndParents)
            {
                MapObject child = document.Map.WorldSpawn.FindByID(cp.Key);
                MapObject parent = document.Map.WorldSpawn.FindByID(cp.Value);
                child.SetParent(parent);
                child.UpdateBoundingBox();
                child.Colour = parent.Colour.Vary();
            }
            foreach (KeyValuePair<long, long> gp in _groupsAndParents)
            {
                MapObject group = document.Map.WorldSpawn.FindByID(gp.Key);
                if (group.GetChildren().All(x => x.IsSelected)) document.Selection.Select(group);
            }

            EventSystem.Publish(EditorEvents.SelectionChanged);
            EventSystem.Publish(EditorEvents.DocumentTreeStructureChanged);
        }

        public void Dispose()
        {
            _groupsAndParents = null;
            _childrenAndParents = null;
        }
    }
}
