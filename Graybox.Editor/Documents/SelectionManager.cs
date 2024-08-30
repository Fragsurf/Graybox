using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Documents
{
	public class SelectionManager
	{

		private Document Document { get; set; }
		private HashSet<MapObject> SelectedObjects { get; set; }
		private HashSet<Face> SelectedFaces { get; set; }
		public bool InFaceSelection { get; private set; }

		public SelectionManager( Document doc )
		{
			Document = doc;
			SelectedObjects = new HashSet<MapObject>();
			SelectedFaces = new HashSet<Face>();
			InFaceSelection = false;
		}

		public Box GetSelectionBoundingBox()
		{
			if ( IsEmpty() ) return null;
			return InFaceSelection
				? new Box( GetSelectedFaces().Select( x => x.BoundingBox ) )
				: new Box( GetSelectedObjects().Select( x => x.BoundingBox ) );
		}

		public IEnumerable<MapObject> GetSelectedObjects() => SelectedObjects;
		public IEnumerable<Face> GetSelectedFaces() => SelectedFaces;

		public int GetSelectionHash()
		{
			var result = 0;

			foreach ( var obj in SelectedObjects )
			{
				result = HashCode.Combine( result, obj.ID, obj.UpdateCounter );
			}

			return result;
		}

		public void SwitchToFaceSelection()
		{
			InFaceSelection = true;
			var objectsToSelect = SelectedObjects.ToList();

			Clear();

			foreach ( var mapobj in objectsToSelect )
			{
				var allFaces = mapobj.GetAllDescendants<Solid>().SelectMany( x => x.Faces ).ToList();
				if ( mapobj is Solid s ) allFaces.AddRange( s.Faces );

				foreach ( var face in allFaces.Distinct() )
				{
					face.IsSelected = true;
					SelectedFaces.Add( face );
				}
			}

			EventSystem.Publish( EditorEvents.SelectionTypeChanged, Document );
			EventSystem.Publish( EditorEvents.SelectionChanged, Document );
		}

		public void SwitchToObjectSelection()
		{
			var newSelection = new List<MapObject>();
			foreach ( var face in SelectedFaces )
			{
				face.IsSelected = false;
				face.Parent.IsSelected = true;

				if ( !newSelection.Contains( face.Parent ) )
				{
					newSelection.Add( face.Parent );
				}
			}

			InFaceSelection = false;
			Clear();

			Select( newSelection );

			EventSystem.Publish( EditorEvents.SelectionTypeChanged, Document );
			EventSystem.Publish( EditorEvents.SelectionChanged, Document );
		}

		public void Clear()
		{
			foreach ( MapObject obj in SelectedObjects )
				obj.IsSelected = false;

			SelectedObjects.Clear();

			foreach ( Face face in SelectedFaces )
				face.IsSelected = false;

			SelectedFaces.Clear();
		}

		public void Select( MapObject obj )
		{
			Select( new[] { obj } );
		}

		public void Select( IEnumerable<MapObject> objs )
		{
			List<MapObject> list = objs.ToList();
			foreach ( MapObject o in list ) o.IsSelected = true;
			SelectedObjects.UnionWith( list );

			SyncShitWad();
		}

		public void Select( Face face )
		{
			if ( SelectedFaces.Contains( face ) ) return;
			face.IsSelected = true;
			SelectedFaces.Add( face );
		}

		public void Select( IEnumerable<Face> faces )
		{
			var list = faces.ToList();
			foreach ( Face face in list ) face.IsSelected = true;
			SelectedFaces.UnionWith( list );
		}

		public void Deselect( MapObject obj )
		{
			Deselect( new[] { obj } );
		}

		public void Deselect( IEnumerable<MapObject> objs )
		{
			var list = objs.ToList();

			SelectedObjects.ExceptWith( list );

			foreach ( MapObject o in list )
				o.IsSelected = false;

			SyncShitWad();
		}

		public void Deselect( Face face )
		{
			SelectedFaces.Remove( face );
			face.IsSelected = false;
		}

		public void Deselect( IEnumerable<Face> faces )
		{
			List<Face> list = faces.ToList();
			SelectedFaces.ExceptWith( list );
			foreach ( Face o in list ) o.IsSelected = false;
		}

		public bool IsEmpty()
		{
			return InFaceSelection ? SelectedFaces.Count == 0 : SelectedObjects.Count == 0;
		}

		public IEnumerable<MapObject> GetSelectedParents()
		{
			List<MapObject> sel = GetSelectedObjects().ToList();
			sel.SelectMany( x => x.GetChildren() ).ToList().ForEach( x => sel.Remove( x ) );
			return sel;
		}

		void SyncShitWad()
		{
			Selection.Clear();
			Selection.TrySelect( SelectedObjects );
		}

	}
}
