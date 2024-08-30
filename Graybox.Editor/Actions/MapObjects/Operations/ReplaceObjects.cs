
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions.MapObjects.Operations
{
    public class ReplaceObjects : IAction
    {
        public bool SkipInStack { get { return false; } }
        public bool ModifiesState { get { return true; } }

        private readonly Dictionary<long, MapObject> _perform;
        private readonly Dictionary<long, MapObject> _reverse;

        public ReplaceObjects(IEnumerable<MapObject> before, IEnumerable<MapObject> after)
        {
            _perform = before.ToDictionary(x => x.ID, x => after.FirstOrDefault(y => y.ID == x.ID));
            _reverse = new Dictionary<long, MapObject>();
        }

        public void Dispose()
        {
            _perform.Clear();
            _reverse.Clear();
        }

        public void Reverse(Document document)
        {
            World root = document.Map.WorldSpawn;
            foreach (KeyValuePair<long, MapObject> kv in _reverse)
            {
                MapObject obj = root.FindByID(kv.Key);
                if (obj == null) return;

                // Unclone will reset children, need to reselect them if needed
                List<MapObject> deselect = obj.FindAll().Where(x => x.IsSelected).ToList();
                document.Selection.Deselect(deselect);

                obj.Unclone(kv.Value);

                IEnumerable<MapObject> select = obj.FindAll().Where(x => deselect.Any(y => x.ID == y.ID));
                document.Selection.Select(select);
            }

            EventSystem.Publish(EditorEvents.DocumentTreeStructureChanged, _reverse.Select(x => document.Map.WorldSpawn.FindByID(x.Key)));
            EventSystem.Publish(EditorEvents.SelectionChanged);
            EventSystem.Publish(EditorEvents.VisgroupsChanged);

            _reverse.Clear();
        }

        public void Perform(Document document)
        {
            World root = document.Map.WorldSpawn;
            _reverse.Clear();
            foreach (KeyValuePair<long, MapObject> kv in _perform)
            {
                MapObject obj = root.FindByID(kv.Key);
                if (obj == null) return;

                _reverse.Add(kv.Key, obj.Clone());

                // Unclone will reset children, need to reselect them if needed
                List<MapObject> deselect = obj.FindAll().Where(x => x.IsSelected).ToList();
                document.Selection.Deselect(deselect);

                obj.Unclone(kv.Value);

                IEnumerable<MapObject> select = obj.FindAll().Where(x => deselect.Any(y => x.ID == y.ID));
                document.Selection.Select(select);
            }

            EventSystem.Publish(EditorEvents.DocumentTreeStructureChanged, _perform.Select(x => document.Map.WorldSpawn.FindByID(x.Key)));
            EventSystem.Publish(EditorEvents.SelectionChanged);
            EventSystem.Publish(EditorEvents.VisgroupsChanged);
        }
    }
}
