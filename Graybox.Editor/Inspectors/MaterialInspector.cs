
using ImGuiNET;

namespace Graybox.Editor.Inspectors;

internal class MaterialInspector : BaseInspector<MaterialAsset>
{

	public override void DrawInspector()
	{
		ImGui.Text( "This is a material!" );
	}

}
