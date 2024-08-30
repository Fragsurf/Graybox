
using Graybox.Editor.Tools;
using ImGuiNET;

namespace Graybox.Editor.Widgets;

//internal class ToolListWidget : BaseWidget
//{

//	public override string Title => "Tools";

//	protected unsafe override void OnUpdate( FrameInfo frameInfo )
//	{
//		base.OnUpdate( frameInfo );

//		var windowSize = ImGui.GetWindowSize();
//		bool horizontalLayout = windowSize.X > windowSize.Y;

//		if ( horizontalLayout )
//			ImGui.Columns( ToolManager.Tools.Count, null, false );

//		foreach ( var tool in ToolManager.Tools )
//		{
//			bool isActiveTool = tool == ToolManager.ActiveTool;

//			if ( isActiveTool )
//			{
//				var buttonColor = ImGui.GetStyleColorVec4( ImGuiCol.ButtonActive );
//				var buttonHoveredColor = ImGui.GetStyleColorVec4( ImGuiCol.ButtonHovered );
//				var buttonActiveColor = ImGui.GetStyleColorVec4( ImGuiCol.ButtonActive );

//				ImGui.PushStyleColor( ImGuiCol.Button, *buttonColor );
//				ImGui.PushStyleColor( ImGuiCol.ButtonHovered, *buttonHoveredColor );
//				ImGui.PushStyleColor( ImGuiCol.ButtonActive, *buttonActiveColor );
//			}

//			var img = EditorResource.Image( tool.EditorIcon );
//			var btnSize = ImGui.GetContentRegionAvail().X;
//			var size = new SVector2( btnSize, btnSize );
//			//new SVector2( horizontalLayout ? ImGui.GetColumnWidth() : ImGui.GetContentRegionAvail().X, 0 )

//			var toolnow = tool;
//			if ( ImGui.ImageButton( $"tool_" + tool.Name, img, size ) )
//			{
//				ToolManager.Activate( toolnow );
//			}

//			if ( ImGui.IsItemHovered() )
//			{
//				ImGui.BeginTooltip();
//				ImGui.Text( tool.Name );
//				ImGui.EndTooltip();
//			}

//			if ( isActiveTool )
//				ImGui.PopStyleColor( 3 );

//			if ( horizontalLayout )
//				ImGui.NextColumn();
//		}

//		if ( horizontalLayout )
//			ImGui.Columns( 1 );
//	}

//}
