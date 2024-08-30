
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics;

namespace Graybox.Editor.Brushes
{
	public class OverlayInfo
	{
		public string Icon;
	}
	public interface IBrush
	{
		string Name { get; }
		string EditorIcon => string.Empty;
		BrushSettings GetSettingsInstance() => new BrushSettings();
		IEnumerable<MapObject> Create( BrushSettings brushSettings, IDGenerator generator, Box box, ITexture texture, int roundDecimals );
		OverlayInfo GetOverlayInfo() => null;
	}
}
