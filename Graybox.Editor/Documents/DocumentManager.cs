
namespace Graybox.Editor.Documents;

public static class DocumentManager
{

	public static List<Document> Documents { get; private set; } = new();
	public static Document CurrentDocument { get; private set; }

	private static int _untitledCount = 1;

	public static string GetUntitledDocumentName()
	{
		return "Untitled " + _untitledCount++;
	}

	public static void Add( Document doc )
	{
		Documents.Add( doc );
		EventSystem.Publish( EditorEvents.DocumentOpened, doc );
	}

	public static void Remove( Document doc )
	{
		bool current = doc == CurrentDocument;
		int index = Documents.IndexOf( doc );

		if ( current && Documents.Count > 1 )
		{
			int ni = index + 1;
			if ( ni >= Documents.Count ) ni = index - 1;
			SwitchTo( Documents[ni] );
		}

		doc.Close();
		Documents.Remove( doc );
		EventSystem.Publish( EditorEvents.DocumentClosed, doc );

		if ( Documents.Count == 0 )
		{
			SwitchTo( null );
			EventSystem.Publish( EditorEvents.DocumentAllClosed );
		}

	}

	public static void SwitchTo( Document doc )
	{
		var prev = CurrentDocument;

		if ( CurrentDocument != null )
		{
			CurrentDocument.SetInactive();
			EventSystem.Publish( EditorEvents.DocumentDeactivated, CurrentDocument );
		}

		CurrentDocument = doc;

		if ( CurrentDocument != null )
		{
			CurrentDocument.SetActive();
			EventSystem.Publish( EditorEvents.DocumentActivated, CurrentDocument );
		}
	}

	public static void AddAndSwitch( Document doc )
	{
		Add( doc );
		SwitchTo( doc );
	}

}
