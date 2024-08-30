
using Graybox.DataStructures.GameData;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions.MapObjects.Entities
{
	public class EditEntityData : IAction
	{
		private class EntityReference
		{
			public long ID { get; set; }
			public EntityData Before { get; set; }
			public EntityData After { get; set; }
		}

		public bool SkipInStack { get { return false; } }
		public bool ModifiesState { get { return true; } }

		private List<EntityReference> _objects = new();

		public void AddEntity( MapObject obj, EntityData newData )
		{
			_objects.Add( new EntityReference { ID = obj.ID, Before = obj.GetEntityData().Clone(), After = newData } );
		}

		public bool IsEmpty()
		{
			return _objects.Count == 0;
		}

		public void Dispose()
		{
			_objects = null;
		}

		public void Reverse( Document document )
		{
			List<MapObject> changed = new List<MapObject>();
			foreach ( EntityReference r in _objects )
			{
				MapObject obj = document.Map.WorldSpawn.FindByID( r.ID );
				changed.Add( obj );
				if ( obj is Entity ) SetEntityData( (Entity)obj, r.Before, document.GameData );
				else if ( obj is World ) SetEntityData( (World)obj, r.Before );
			}
			EventSystem.Publish( EditorEvents.EntityDataChanged, changed );
			EventSystem.Publish( EditorEvents.DocumentTreeObjectsChanged, changed );
			EventSystem.Publish( EditorEvents.VisgroupsChanged );
		}

		public void Perform( Document document )
		{
			List<MapObject> changed = new List<MapObject>();
			foreach ( EntityReference r in _objects )
			{
				MapObject obj = document.Map.WorldSpawn.FindByID( r.ID );
				changed.Add( obj );
				if ( obj is Entity ) SetEntityData( (Entity)obj, r.After, document.GameData );
				else if ( obj is World ) SetEntityData( (World)obj, r.After );

				if ( obj != null ) obj.UpdateBoundingBox();
			}
			EventSystem.Publish( EditorEvents.EntityDataChanged, changed );
			EventSystem.Publish( EditorEvents.DocumentTreeObjectsChanged, changed );
			EventSystem.Publish( EditorEvents.VisgroupsChanged );
		}

		private void SetEntityData( Entity ent, EntityData data, GameData gameData )
		{
			ent.EntityData = data;
			ent.GameData = gameData.Classes.FirstOrDefault( x => String.Equals( x.Name, data.Name, StringComparison.CurrentCultureIgnoreCase ) && x.ClassType != ClassType.Base );
		}

		private void SetEntityData( World world, EntityData data )
		{
			world.EntityData = data;
		}
	}
}
