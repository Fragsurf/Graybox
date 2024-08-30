
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Groups;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Editor.Documents;
using ImGuiNET;

namespace Graybox.Editor;

internal static class MapObjectUtil
{

	internal static void GroupObjects( IEnumerable<MapObject> objs )
	{
		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;
		if ( objs == null || !objs.Any() ) return;

		var all = objs.Where( x => x is not Group && x.Parent is not Group );
		if ( !all.Any() ) return;

		var action = new GroupAction( all );
		doc.PerformAction( $"Group {all.Count()} objects", action );
	}

	internal static void UngroupObjects( IEnumerable<MapObject> objs )
	{
		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;
		if ( objs == null || !objs.Any() ) return;

		var ungroup = new UngroupAction( objs );
		doc.PerformAction( "Ungroup", ungroup );
	}

	internal static void Duplicate( IEnumerable<MapObject> objs )
	{
		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;
		if ( objs == null || !objs.Any() ) return;

		var parent = objs.First().Parent.ID;
		var newObjs = new List<MapObject>();

		foreach ( var o in objs )
		{
			newObjs.Add( o.Copy( doc.Map.IDGenerator ) );
		}

		var create = new Create( parent, newObjs );
		var actions = new ActionCollection( create );

		var selectAction = new ChangeSelection( newObjs, doc.Selection.GetSelectedObjects() );
		actions.Add( selectAction );

		doc.PerformAction( $"Duplicate {newObjs.Count} objects", actions );
	}

	internal static void Delete( IEnumerable<MapObject> objs )
	{
		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;
		if ( objs == null || !objs.Any() ) return;

		var count = objs.Count();
		var delete = new Delete( objs.Select( x => x.ID ) );
		doc.PerformAction( $"Delete {count} objects", delete );
	}

	internal static void ContextMenu( IEnumerable<MapObject> objs )
	{
		if ( !objs.Any() ) return;

		if ( objs.Count() == 1 )
		{
			ContextMenu( objs.First() );
			return;
		}

		ImGui.TextDisabled( "Multiple Objects" );

		if ( ImGui.MenuItem( "Delete" ) )
		{
			Delete( objs );
		}

		if ( ImGui.MenuItem( "Duplicate" ) )
		{
			Duplicate( objs );
		}

		if ( ImGui.MenuItem( "Group" ) )
		{
			GroupObjects( objs );
		}
	}

	internal static void ContextMenu( MapObject obj )
	{
		var name = obj.Name;
		if ( string.IsNullOrEmpty( name ) )
		{
			name = $"{obj.GetType().Name} ({obj.ID})";
		}

		ImGui.TextDisabled( name );
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		if ( ImGui.MenuItem( "Delete" ) )
		{
			Delete( new[] { obj } );
		}

		if ( ImGui.MenuItem( "Duplicate" ) )
		{
			Duplicate( new[] { obj } );
		}

		if ( obj is Group )
		{
			if ( ImGui.MenuItem( "Ungroup" ) )
			{
				UngroupObjects( new[] { obj } );
			}
		}
	}

}
