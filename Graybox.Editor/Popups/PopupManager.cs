
namespace Graybox.Editor;

internal static class PopupManager
{

	static List<Popup> popups = new();

	public static bool PopupOpen => popups.Any( x => x.Shown );

	public static void Add( Popup popup )
	{
		popups.Add( popup );
	}

	public static void Remove( Popup popup )
	{
		popups.Remove( popup );
	}

	public static void Update()
	{
		foreach ( var popup in popups )
		{
			if ( !popup.Shown )
				continue;

			popup.Update();
			break;
		}
	}

}
