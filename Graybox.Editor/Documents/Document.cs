
using Graybox.DataStructures.GameData;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Editor.History;
using Graybox.Editor.Settings;
using Graybox.Providers.Map;
using System.Windows.Forms;
using Path = System.IO.Path;

namespace Graybox.Editor.Documents;

public partial class Document
{

	public string MapFile { get; set; }
	public string MapFileName { get; set; }
	public Map Map { get; set; }
	public GameData GameData { get; set; }
	public SelectionManager Selection { get; private set; }
	public HistoryManager History { get; private set; }
	public AssetSystem AssetSystem { get; private set; }
	public TextureAsset SelectedTexture { get; set; }
	public DocumentTrace Trace { get; }

	public Document( string mapFile, Map map )
	{
		MapFile = mapFile;
		Map = map;
		MapFileName = mapFile == null ? DocumentManager.GetUntitledDocumentName() : Path.GetFileName( mapFile );
		Trace = new DocumentTrace( this );
		Selection = new SelectionManager( this );
		History = new HistoryManager( this );
		GameData = new GameData();

		AssetSystem = new AssetSystem();
		AssetSystem.AddDirectory( "Assets/" );
		foreach ( var package in Directories.TextureDirs )
		{
			AssetSystem.AddDirectory( package );
		}
		SelectedTexture = AssetSystem.FindAsset<TextureAsset>( "textures/prototype/prototype_gray" );

		Debug.LogError( "Start autosave scheduler" );
	}

	public bool SaveToFile( string path = null, bool forceOverride = false, bool switchPath = true )
	{
		path = forceOverride ? path : path ?? MapFile;

		if ( path != null )
		{
			var noSaveExtensions = FileTypeRegistration.GetSupportedExtensions().Where( x => !x.CanSave ).Select( x => x.Extension );
			foreach ( string ext in noSaveExtensions )
			{
				if ( path.EndsWith( ext, StringComparison.OrdinalIgnoreCase ) )
				{
					path = null;
					break;
				}
			}
		}

		if ( path == null )
		{
			using ( SaveFileDialog sfd = new SaveFileDialog() )
			{
				string filter = String.Join( "|", FileTypeRegistration.GetSupportedExtensions()
					.Where( x => x.CanSave ).Select( x => x.Description + " (*" + x.Extension + ")|*" + x.Extension ) );
				string[] all = FileTypeRegistration.GetSupportedExtensions().Where( x => x.CanSave ).Select( x => "*" + x.Extension ).ToArray();
				sfd.Filter = "All supported formats (" + String.Join( ", ", all ) + ")|" + String.Join( ";", all ) + "|" + filter;
				if ( sfd.ShowDialog() == DialogResult.OK )
				{
					path = sfd.FileName;
				}
			}
		}

		if ( string.IsNullOrEmpty( path ) )
			return false;

		MapProvider.SaveMapToFile( path, Map, AssetSystem );

		if ( switchPath )
		{
			MapFile = path;
			MapFileName = Path.GetFileName( MapFile );
			History.TotalActionsSinceLastSave = 0;
			EventSystem.Publish( EditorEvents.DocumentSaved, this );
		}

		return true;
	}

	public void SetActive()
	{
	}

	public void SetInactive()
	{
	}

	public void Close()
	{
	}

	public void PerformAction( string name, IAction action )
	{
		try
		{
			action.Perform( this );
		}
		catch ( Exception ex )
		{
			Debug.LogException( ex );
		}

		HistoryAction history = new HistoryAction( name, action );
		History.AddHistoryItem( history );
	}

	public void ClearSelection()
	{
		if ( Selection.IsEmpty() ) return;

		PerformAction( "Deselect All", new Deselect( Selection.GetSelectedObjects() ) );
	}

	public void DeleteSelection()
	{
		if ( !Selection.IsEmpty() && !Selection.InFaceSelection )
		{
			var sel = Selection.GetSelectedObjects().Select( x => x.ID ).ToList();
			var name = "Removed " + sel.Count + " item" + (sel.Count == 1 ? "" : "s");
			PerformAction( name, new Delete( sel ) );
		}
	}

	public void Cut()
	{
		Copy();
		DeleteSelection();
	}

	public void Copy()
	{
		if ( !Selection.IsEmpty() && !Selection.InFaceSelection )
		{
			ClipboardManager.Push( Selection.GetSelectedObjects() );
		}
	}

	public void Paste()
	{
		var content = ClipboardManager.GetPastedContent( this );
		if ( content == null ) return;

		var list = content.ToList();
		if ( list.Count == 0 ) return;

		list.SelectMany( x => x.FindAll() ).ToList().ForEach( x => x.IsSelected = true );

		var name = "Pasted " + list.Count + " item" + (list.Count == 1 ? "" : "s");
		var selected = Selection.GetSelectedObjects().ToList();

		PerformAction( name, new ActionCollection(
										  new Deselect( selected ),
										  new Create( Map.WorldSpawn.ID, list ) ) );
	}

}
