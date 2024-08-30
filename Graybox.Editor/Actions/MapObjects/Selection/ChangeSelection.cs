
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions.MapObjects.Selection
{
    public class ChangeSelection : IAction
    {
        public bool SkipInStack { get { return Graybox.Editor.Settings.Select.SkipSelectionInUndoStack; } }
        public bool ModifiesState { get { return false; } }

        private List<long> _selected;
        private List<long> _deselected;

        public ChangeSelection(IEnumerable<MapObject> selected, IEnumerable<MapObject> deselected)
        {
            _selected = selected.Select(x => x.ID).ToList();
            _deselected = deselected.Select(x => x.ID).ToList();
        }

        public ChangeSelection(IEnumerable<long> selected, IEnumerable<long> deselected)
        {
            _selected = selected.ToList();
            _deselected = deselected.ToList();
        }

        public void Dispose()
        {
            _selected = _deselected = null;
        }

        public void Reverse(Document document)
        {
            List<MapObject> sel = _selected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null).ToList();
            List<MapObject> desel = _deselected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null && x.BoundingBox != null).ToList();

            document.Selection.Select(desel);
            document.Selection.Deselect(sel);

            EventSystem.Publish(EditorEvents.DocumentTreeSelectedObjectsChanged, sel.Union(desel));
            EventSystem.Publish(EditorEvents.SelectionChanged);
        }

        public void Perform(Document document)
        {
            List<MapObject> desel = _deselected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null).ToList();
            List<MapObject> sel = _selected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null && x.BoundingBox != null).ToList();

            document.Selection.Deselect(desel);
            document.Selection.Select(sel);

            EventSystem.Publish(EditorEvents.DocumentTreeSelectedObjectsChanged, sel.Union(desel));
            EventSystem.Publish(EditorEvents.SelectionChanged);
        }
    }
}
