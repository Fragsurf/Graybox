
using Graybox.Editor.Documents;
using Graybox.Editor.Tools;
using ImGuiNET;

namespace Graybox.Editor.Widgets;

internal class ToolPropertiesWidget : BaseWidget
{

	[EditorLayout.Data]
	public Dictionary<string, string> ToolProperties { get; set; } = new();

	public override string Title => "Tool Properties";

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );

		if ( ToolManager.ActiveTool == null )
		{
			ImGui.Text( "No tool selected" );
			return;
		}

		if ( DocumentManager.CurrentDocument?.AssetSystem == null )
		{
			ImGui.Text( "No document opened" );
			return;
		}

		ToolManager.ActiveTool.UpdateWidget();
	}

	internal override void OnDataGet()
	{
		base.OnDataGet();

		ToolProperties.Clear();

		foreach ( var tool in ToolManager.Tools )
		{
			ToolProperties[tool.GetType().Name] = tool.SaveWidgetData();
		}
	}

	internal override void OnDataSet()
	{
		base.OnDataSet();

		foreach ( var tool in ToolManager.Tools )
		{
			if ( ToolProperties.ContainsKey( tool.GetType().Name ) )
			{
				tool.LoadWidgetData( ToolProperties[tool.GetType().Name] );
			}
		}
	}

}
