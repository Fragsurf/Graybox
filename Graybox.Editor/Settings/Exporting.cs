
namespace Graybox.Editor.Settings;

public class Exporting
{
	public static bool ShowModelBakingWarning { get; set; }

	static Exporting()
	{
		ShowModelBakingWarning = true;
	}
}
