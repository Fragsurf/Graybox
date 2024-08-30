
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions.MapObjects.Groups
{
	public class GroupAction : IAction
	{
		private readonly List<long> _groupedObjects;
		private long _groupId;
		private Dictionary<long, long> _originalChildParents;

		public bool SkipInStack { get { return false; } }
		public bool ModifiesState { get { return true; } }

		public GroupAction( IEnumerable<MapObject> groupedObjects )
		{
			_groupedObjects = groupedObjects.Where( x => x is not Group && x.Parent is not Group ).Select( x => x.ID ).ToList();
		}

		public void Perform( Document document )
		{
			List<MapObject> objects = _groupedObjects
				.Select( x => document.Map.WorldSpawn.FindByID( x ) )
				.Where( x => x != null && x.Parent != null )
				.ToList();

			_originalChildParents = objects.ToDictionary( x => x.ID, x => x.Parent.ID );

			if ( _groupId == 0 ) _groupId = document.Map.IDGenerator.GetNextObjectID();
			Group group = new Group( _groupId ) { Colour = ColorUtility.GetRandomGroupColour() };

			objects.ForEach( x => x.SetParent( group ) );
			objects.ForEach( x => x.Colour = group.Colour.Vary() );
			group.SetParent( document.Map.WorldSpawn );
			group.UpdateBoundingBox();

			if ( group.GetChildren().All( x => x.IsSelected ) )
			{
				document.Selection.Select( group );
				EventSystem.Publish( EditorEvents.SelectionChanged );
			}

			EventSystem.Publish( EditorEvents.DocumentTreeStructureChanged );
		}

		public void Reverse( Document document )
		{
			MapObject group = document.Map.WorldSpawn.FindByID( _groupId );
			List<MapObject> children = group.GetChildren().ToList();
			children.ForEach( x => x.SetParent( document.Map.WorldSpawn.FindByID( _originalChildParents[x.ID] ) ) );
			children.ForEach( x => x.Colour = ColorUtility.GetRandomBrushColour() );
			children.ForEach( x => x.UpdateBoundingBox() );
			group.SetParent( null );

			if ( group.IsSelected )
			{
				document.Selection.Deselect( group );
				EventSystem.Publish( EditorEvents.SelectionChanged );
			}

			_originalChildParents.Clear();

			EventSystem.Publish( EditorEvents.DocumentTreeStructureChanged );
		}

		public void Dispose()
		{
			_originalChildParents = null;
		}
	}
}
