
using Graybox.Editor.Documents;

namespace Graybox.Editor.Tools;

public static class ToolManager
{

	public static IReadOnlyList<BaseTool> Tools { get; } = new List<BaseTool>()
	{
		new SelectTool2(),
		new BlockTool(),
		new ClipTool(),
		new TextureTool(),
		new MeshEditorTool(),
		new EntityTool(),
		new EnvironmentTool(),
	};

	public static BaseTool ActiveTool { get; private set; }

	public static void Activate<T>() where T : BaseTool
	{
		var tool = Tools.OfType<T>().FirstOrDefault();
		if ( tool == null ) return;
		if ( ActiveTool == tool )
		{
			ActiveTool?.ActivatedAgain();
			return;
		}

		Activate( tool );
	}

	public static void Activate( BaseTool tool, bool preventHistory = false )
	{
		if ( tool == ActiveTool ) return;
		if ( DocumentManager.CurrentDocument == null ) return;
		ActiveTool?.ToolDeselected( preventHistory );
		ActiveTool = tool;
		ActiveTool?.ToolSelected( preventHistory );
		EventSystem.Publish( EditorEvents.ToolSelected );
	}

}
