
using ImGuiNET;

namespace Graybox.Editor.Inspectors;

internal class TextureInspector : BaseInspector<TextureAsset>
{

	public override void DrawInspector()
	{
		ImGui.Text( "This is a texture" );
	}

}
