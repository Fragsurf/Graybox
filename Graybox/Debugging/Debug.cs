
namespace Graybox;

public static class Debug
{

	public static Action<Profiler> Profile;
	public static Action<Exception> ShowException;
	public static readonly List<DebugLogEntry> LogMessages = new();
	private const int MaxLogMessages = 250;

	static int lastLogHash;

	public static void Clear()
	{
		lock ( LogMessages )
		{
			LogMessages.Clear();
		}
		lastLogHash = -123;
	}

	public static void Assert( bool condition, string message = "Assertion failed" )
	{
		if ( !condition )
		{
			throw new Exception( message );
		}
	}

	public static void Log( object message, Action onClick = null )
	{
		AddLogEntry( DebugLogTypes.Info, message?.ToString() ?? "NULL", onClick );
	}

	public static void LogRandom()
	{
		Log( new System.Random().Next() );
	}

	public static void LogWarning( object message, Action onClick = null )
	{
		AddLogEntry( DebugLogTypes.Warning, message?.ToString() ?? "NULL", onClick );
	}

	public static void LogException( System.Exception e )
	{
		//StackTrace st = new StackTrace();
		//StackFrame[] frames = st.GetFrames() ?? new StackFrame[0];
		//string msg = "Action exception: " + name + " (" + action + ")";
		//foreach ( StackFrame frame in frames )
		//{
		//	System.Reflection.MethodBase method = frame.GetMethod();
		//	msg += "\r\n    " + method.ReflectedType.FullName + "." + method.Name;
		//}
		AddLogEntry( DebugLogTypes.Error, e.Message, () =>
		{
			ShowException?.Invoke( e );
		} );
	}

	public static void LogError( object message, Action onClick = null )
	{
		AddLogEntry( DebugLogTypes.Error, message?.ToString() ?? "NULL", onClick );
	}

	public static void LogSuccess( object message, Action onClick = null )
	{
		AddLogEntry( DebugLogTypes.Success, message?.ToString() ?? "NULL", onClick );
	}

	public static void LogExciting( object message, Action onClick = null )
	{
		AddLogEntry( DebugLogTypes.Exciting, message?.ToString() ?? "NULL", onClick );
	}

	private static void AddLogEntry( DebugLogTypes type, string message, Action onClick = null )
	{
		lock ( LogMessages )
		{
			var hash = HashCode.Combine( message, type );
			if ( hash == lastLogHash )
			{
				LogMessages[^1].Count++;
			}
			else
			{
				LogMessages.Add( new DebugLogEntry
				{
					Type = type,
					Message = message,
					Time = DateTime.Now,
					OnClicked = onClick,
					Count = 1
				} );

				if ( LogMessages.Count > MaxLogMessages )
				{
					LogMessages.RemoveAt( 0 );
				}
			}
			lastLogHash = hash;
		}
	}
}

public enum DebugLogTypes
{
	Info,
	Warning,
	Error,
	Success,
	Exciting
}

public class DebugLogEntry
{

	public DebugLogTypes Type { get; set; }
	public string Message { get; set; }
	public int Count { get; set; }
	public DateTime Time { get; set; }
	public Action OnClicked { get; set; }

	public SVector4 GetColor()
	{
		return Type switch
		{
			DebugLogTypes.Info => new SVector4( 1.0f, 1.0f, 1.0f, 1.0f ), // White
			DebugLogTypes.Warning => new SVector4( 1.0f, 1.0f, 0.0f, 1.0f ), // Yellow
			DebugLogTypes.Error => new SVector4( 1.0f, 0.0f, 0.0f, 1.0f ), // Red
			DebugLogTypes.Success => new SVector4( 0.0f, 1.0f, 0.0f, 1.0f ), // Green
			DebugLogTypes.Exciting => new SVector4( 0.2f, 1.0f, 0.8f, 1.0f ), // Purplish
			_ => new SVector4( 1.0f, 1.0f, 1.0f, 1.0f ), // Default to white
		};
	}
}
