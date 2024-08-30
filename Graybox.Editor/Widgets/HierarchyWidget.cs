using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;
using ImGuiNET;

namespace Graybox.Editor.Widgets;

internal class HierarchyWidget : BaseWidget
{
	public override string Title => "Hierarchy";
	private string searchFilter = string.Empty;

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );
		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;

		ImGui.BeginChild( "HierarchyChild", new SVector2( 0, 48 ) );
		ImGui.PushItemWidth( -1 );
		ImGui.InputText( "##HierarchyFilter", ref searchFilter, 100 );
		ImGui.PopItemWidth();
		ImGui.EndChild();

		ImGui.BeginChild( "HierarchyTree", new SVector2( 0, 0 ) );

		CreateCategoryTree( "Brush Entities", doc.Map.WorldSpawn, IsBrushEntity );
		CreateCategoryTree( "Point Entities", doc.Map.WorldSpawn, IsPointEntity );
		CreateCategoryTree( "Solids", doc.Map.WorldSpawn, IsSolid );
		CreateCategoryTree( "Groups", doc.Map.WorldSpawn, IsGroup );
		CreateCategoryTree( "Lights", doc.Map.WorldSpawn, IsLight );

		ImGui.EndChild();
		//TryToSelect( null );
	}

	private void CreateCategoryTree( string categoryName, MapObject root, Func<MapObject, bool> filter )
	{
		if ( ImGui.TreeNodeEx( categoryName, ImGuiTreeNodeFlags.CollapsingHeader ) )
		{
			ImGui.PushStyleColor( ImGuiCol.Header, Graybox.Editor.EditorTheme.accentColor );
			ImGui.PushStyleColor( ImGuiCol.HeaderHovered, Graybox.Editor.EditorTheme.accentHoverColor );
			ImGui.PushStyleColor( ImGuiCol.HeaderActive, Graybox.Editor.EditorTheme.accentHoverColor );
			CreateFilteredTree( root, filter );
			ImGui.PopStyleColor( 3 );
			//ImGui.TreePop();
		}
	}

	private void CreateFilteredTree( MapObject node, Func<MapObject, bool> filter )
	{
		if ( filter( node ) )
		{
			ImGui.Indent( 20 );
			ImGui.PushStyleVar( ImGuiStyleVar.ItemSpacing, new SVector2( 0, 2 ) );
			ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 0, 4 ) );
			CreateTreeNode( node );
			ImGui.PopStyleVar( 2 );
			ImGui.Indent( -20 );
		}

		foreach ( var child in node.Children.ToList() )
		{
			CreateFilteredTree( child, filter );
		}
	}

	private void CreateTreeNode( MapObject node )
	{
		var flags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.OpenOnArrow;
		var name = string.IsNullOrEmpty( node.Name ) ? node.GetType().Name : node.Name;
		var matchesFilter = string.IsNullOrEmpty( searchFilter ) || name.Contains( searchFilter, StringComparison.InvariantCultureIgnoreCase );

		if ( matchesFilter )
		{
			if ( !node.HasChildren )
				flags |= ImGuiTreeNodeFlags.Leaf;
			if ( node.IsSelected )
				flags |= ImGuiTreeNodeFlags.Selected;

			var id = node.ID.ToString();
			var nodeName = $"{name} ({id})";
			var nodeOpen = ImGui.TreeNodeEx( nodeName + $"##{id}", flags );

			if ( !ImGui.IsItemToggledOpen() )
			{
				TryToSelect( node );
			}

			// Right-click context menu
			if ( ImGui.BeginPopupContextItem( $"ObjectContext##{node.ID}" ) )
			{
				if ( !node.IsSelected )
				{
					DocumentManager.CurrentDocument?.Selection?.Clear();
					DocumentManager.CurrentDocument?.Selection?.Select( node );
				}

				var selectedObjs = DocumentManager.CurrentDocument.Selection.GetSelectedObjects();
				MapObjectUtil.ContextMenu( selectedObjs );

				ImGui.EndPopup();
			}

			if ( nodeOpen )
			{
				foreach ( var child in node.Children.ToList() )
				{
					CreateTreeNode( child );
				}
				ImGui.TreePop();
			}
		}
	}

	private void TryToSelect( MapObject obj )
	{
		if ( !ImGui.IsItemClicked( ImGuiMouseButton.Left ) ) return;
		if ( obj is World ) return;
		var selection = DocumentManager.CurrentDocument?.Selection;
		if ( selection == null ) return;

		if ( !Input.ControlModifier )
		{
			selection.Clear();
		}
		if ( obj == null ) return;
		if ( obj.IsSelected )
		{
			selection.Deselect( obj );
		}
		else
		{
			selection.Select( obj );
		}
	}

	private bool IsPointEntity( MapObject obj )
	{
		return obj is Entity e && e.GameData != null && e.GameData.ClassType == DataStructures.GameData.ClassType.Point;
	}

	private bool IsBrushEntity( MapObject obj )
	{
		return obj is Entity e && e.GameData != null && e.GameData.ClassType == DataStructures.GameData.ClassType.Solid;
	}

	private bool IsSolid( MapObject obj )
	{
		return obj is Solid && obj.Parent is not Entity && obj.Parent is not Group g;
	}

	private bool IsGroup( MapObject obj )
	{
		return obj is Group;
	}

	private bool IsLight( MapObject obj )
	{
		return obj is Light;
	}

}
