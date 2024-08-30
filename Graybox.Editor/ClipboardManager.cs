
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;
using Graybox.Format;
using Graybox.Providers.Map;
using TextCopy;

namespace Graybox.Editor;

public static class ClipboardManager
{

	public static void Push( IEnumerable<MapObject> copiedObjects )
	{
		string item = CreateCopyStream( copiedObjects );
		ClipboardService.SetText( item );
		EventSystem.Publish( EditorEvents.ClipboardChanged );
	}

	public static IEnumerable<MapObject> GetPastedContent( Document document )
	{
		var txt = ClipboardService.GetText();

		if ( string.IsNullOrEmpty( txt ) )
			return null;

		return ExtractCopyStream( document, txt );
	}

	public static bool CanPaste()
	{
		var txt = ClipboardService.GetText();

		if ( string.IsNullOrEmpty( txt ) )
			return false;

		return txt.StartsWith( "clipboard" );
	}

	public static IEnumerable<MapObject> CloneFlatHeirarchy( Document document, IEnumerable<MapObject> objects )
	{
		return ExtractCopyStream( document, CreateCopyStream( objects ) );
	}

	private static string CreateCopyStream( IEnumerable<MapObject> copiedObjects )
	{
		var objs = copiedObjects.Select( x => GBMapProvider.ConvertToGBObject( x ) );
		return System.Text.Json.JsonSerializer.Serialize( objs );
	}

	private static IEnumerable<MapObject> ExtractCopyStream( Document document, string str )
	{
		var objs = System.Text.Json.JsonSerializer.Deserialize<List<GBObject>>( str );
		var result = new List<MapObject>();

		foreach ( var obj in objs )
		{
			result.Add( GBMapProvider.ConvertToMapObject( obj ) );
		}

		foreach ( var obj in result )
		{
			obj.ID = document.Map.IDGenerator.GetNextObjectID();
			obj.UpdateBoundingBox();

			foreach ( var child in obj.GetAllDescendants<MapObject>() )
			{
				child.ID = document.Map.IDGenerator.GetNextObjectID();
				child.UpdateBoundingBox();
			}

			if ( obj is Solid s )
			{
				foreach ( var face in s.Faces )
				{
					face.ID = document.Map.IDGenerator.GetNextFaceID();
					face.UpdateBoundingBox();
				}
			}
		}

		return result;
	}

}
