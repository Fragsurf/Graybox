
namespace Graybox.Editor.Documents;

public partial class Document
{

	public void SetCookie<T>( string name, T obj )
	{
		Debug.LogError( "Implement cookie get for: " + name );

		//throw new System.NotImplementedException();
	}

	public T GetCookie<T>( string name, T def = default( T ) )
	{
		Debug.LogError( "Implement cookie set for: " + name );
		return default;
		//throw new System.NotImplementedException();
	}

}
