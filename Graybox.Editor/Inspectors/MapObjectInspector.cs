
using Graybox.DataStructures.GameData;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions.MapObjects.Entities;
using Graybox.Editor.Documents;
using Graybox.Utility;
using ImGuiNET;

namespace Graybox.Editor.Inspectors
{
	internal class MapObjectInspector : BaseInspector<MapObject>
	{

		public override void DrawInspector()
		{
			ImGui.Text( "Map object edit" );
		}

	}
}
