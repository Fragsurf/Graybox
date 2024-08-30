
namespace Graybox;

public class EventAttribute : Attribute
{
	public string Message { get; }

	public EventAttribute( string message )
	{
		Message = message;
	}
}
