
namespace Graybox.Editor;

public class ExceptionWrapper
{

	public static void Execute<T>( Action<T> action, T param )
	{
		try
		{
			action.Invoke( param );
		}
		catch ( Exception ex )
		{
			Debug.LogException( ex );
			ShowExceptionPopup( ex );
		}
	}

	public static void Execute( Action action )
	{
		try
		{
			action.Invoke();
		}
		catch ( Exception ex )
		{
			Debug.LogException( ex );
			ShowExceptionPopup( ex );
		}
	}

	public static void ShowExceptionPopup( Exception exception )
	{
		var exceptionPopup = new ExceptionPopup( "ERROR!", exception, "Close", "Report", () => { }, () => { } );
		exceptionPopup.Show();
	}
}
