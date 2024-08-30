
namespace Graybox.Editor.Settings;

public class General
{
	public static bool CheckUpdatesOnStartup { get; set; }
	public static bool EnableDiscordPresence { get; set; }

	static General()
	{
		CheckUpdatesOnStartup = true;
		EnableDiscordPresence = true;
	}
}
