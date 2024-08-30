
using Graybox.Editor.Documents;
using Graybox.Editor.Tools;

namespace Graybox.Editor;

internal enum EditorHotkeyNames
{
	DecreaseGrid,
	IncreaseGrid,
}

internal class Hotkey2
{

	private bool requireCtrl;
	private bool requireShift;
	private bool requireAlt;
	private Key requiredKey;
	private Action onPressed;
	private string name;

	public string Name => name;

	public Hotkey2( string keys, Action onPressed, string name = "" )
	{
		this.name = name;
		this.onPressed = onPressed;
		ParseHotkeyString( keys );
	}

	public Hotkey2( string keys )
	{
		ParseHotkeyString( keys );
	}

	private void ParseHotkeyString( string name )
	{
		var parts = name.Split( '+' );
		foreach ( var part in parts )
		{
			var trimmedPart = part.Trim().ToLower();
			switch ( trimmedPart )
			{
				case "ctrl":
					requireCtrl = true;
					break;
				case "shift":
					requireShift = true;
					break;
				case "alt":
					requireAlt = true;
					break;
				default:
					requiredKey = ParseKey( trimmedPart );
					break;
			}
		}
	}

	private Key ParseKey( string keyString )
	{
		if ( keyString.Length == 1 && char.IsLetterOrDigit( keyString[0] ) )
		{
			return (Key)Enum.Parse( typeof( Key ), keyString.ToUpper() );
		}
		else if ( keyString.StartsWith( "f" ) && int.TryParse( keyString.Substring( 1 ), out int fKeyNumber ) && fKeyNumber >= 1 && fKeyNumber <= 12 )
		{
			return (Key)Enum.Parse( typeof( Key ), keyString.ToUpper() );
		}
		else
		{
			switch ( keyString )
			{
				case "[":
					return Key.LeftBracket;
				case "]":
					return Key.RightBracket;
			}
		}

		Debug.LogError( "Invalid key: " + keyString );

		return Key.Unknown;
	}

	public bool IsPressed( InputEvent e )
	{
		return e.Control == requireCtrl &&
			   e.Shift == requireShift &&
			   e.Alt == requireAlt &&
			   e.Key == requiredKey;
	}

	public void Execute()
	{
		onPressed?.Invoke();
	}

	public override string ToString()
	{
		var parts = new List<string>();
		if ( requireCtrl ) parts.Add( "Ctrl" );
		if ( requireShift ) parts.Add( "Shift" );
		if ( requireAlt ) parts.Add( "Alt" );

		var reqKey = string.Empty;

		switch ( requiredKey )
		{
			case Key.LeftBracket:
				reqKey = "[";
				break;
			case Key.RightBracket:
				reqKey = "]";
				break;
			default:
				reqKey = requiredKey.ToString();
				break;
		}

		parts.Add( reqKey );
		return string.Join( "+", parts );
	}

}

internal static class EditorHotkeys
{

	internal static List<Hotkey2> Hotkeys = new();

	static EditorHotkeys()
	{
		Hotkeys.Add( new( "Shift+S", ToolManager.Activate<SelectTool2>, nameof( SelectTool2 ) ) );
		Hotkeys.Add( new( "Shift+B", ToolManager.Activate<BlockTool>, nameof( BlockTool ) ) );
		Hotkeys.Add( new( "Shift+A", ToolManager.Activate<TextureTool>, nameof( TextureTool ) ) );
		Hotkeys.Add( new( "Shift+X", ToolManager.Activate<ClipTool>, nameof( ClipTool ) ) );
		Hotkeys.Add( new( "Shift+E", ToolManager.Activate<EntityTool>, nameof( EntityTool ) ) );
		Hotkeys.Add( new( "Shift+L", ToolManager.Activate<EnvironmentTool>, nameof( EnvironmentTool ) ) );
		Hotkeys.Add( new( "Shift+V", ToolManager.Activate<MeshEditorTool>, nameof( MeshEditorTool ) ) );
		Hotkeys.Add( new( "[", EditorPrefs.DecreaseGridSize, nameof( EditorHotkeyNames.DecreaseGrid ) ) );
		Hotkeys.Add( new( "]", EditorPrefs.IncreaseGridSize, nameof( EditorHotkeyNames.IncreaseGrid ) ) );
	}

	public static string GetHotkeyString( string name )
	{
		var hotkey = Hotkeys.FirstOrDefault( x => x.Name == name );
		return hotkey?.ToString() ?? string.Empty;
	}

	public static void TryExecute( ref InputEvent e )
	{
		foreach ( var hotkey in Hotkeys )
		{
			if ( hotkey.IsPressed( e ) )
			{
				hotkey.Execute();
				e.Handled = true;
				return;
			}
		}
	}

	public static bool TryExecute( KeyboardKeyEventArgs e )
	{
		if ( e.Control && e.Key == Key.O )
		{
			EditorWindow.Instance?.BrowseForMap();
			return true;
		}

		if ( e.Control && e.Key == Key.S )
		{
			EditorWindow.Instance?.SaveToFile( e.Shift );
			return true;
		}

		if ( e.Control && e.Key == Key.N )
		{
			EditorWindow.Instance?.CreateNewMap();
			return true;
		}

		if ( e.Control && e.Key == Key.Z )
		{
			DocumentManager.CurrentDocument?.History?.Undo();
			return true;
		}

		if ( e.Control && e.Key == Key.Y )
		{
			DocumentManager.CurrentDocument?.History?.Redo();
			return true;
		}

		if ( e.Control && e.Key == Key.X )
		{
			DocumentManager.CurrentDocument?.Cut();
			return true;
		}

		if ( e.Control && e.Key == Key.C )
		{
			DocumentManager.CurrentDocument?.Copy();
			return true;
		}

		if ( e.Control && e.Key == Key.V )
		{
			DocumentManager.CurrentDocument?.Paste();
			return true;
		}

		return false;
	}

}
