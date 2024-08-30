using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Problems
{
	public class TargetWithoutMatchingName : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			List<Entity> entities = map.WorldSpawn
				.Find( x => x is Entity && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
				.OfType<Entity>()
				.Where( x => x.GameData != null ).ToList();
			foreach ( Entity entity in entities.Where( x => !String.IsNullOrWhiteSpace( x.EntityData.GetPropertyValue( "target" ) ) ) )
			{
				string target = entity.EntityData.GetPropertyValue( "target" );
				Entity tname = entities.FirstOrDefault( x => x.EntityData.GetPropertyValue( "targetname" ) == target );
				if ( tname == null ) yield return new Problem( GetType(), map, new[] { entity }, Fix, "Entity target has no matching named entity", "This entity's target value doesn't have an matching named entity. Each target should have a matching target name. Fixing the problem will reset the target's value to a blank string." );
			}
		}

		public IAction Fix( Problem problem )
		{
			EditEntityData edit = new EditEntityData();
			foreach ( MapObject mo in problem.Objects )
			{
				EntityData ed = mo.GetEntityData().Clone();
				Property prop = ed.Properties.FirstOrDefault( x => x.Key.ToLowerInvariant() == "target" );
				if ( prop != null )
				{
					ed.Properties.Remove( prop );
					edit.AddEntity( mo, ed );
				}
			}
			return edit;
		}
	}
}
