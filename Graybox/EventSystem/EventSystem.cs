
using System.Reflection;

namespace Graybox;

public static class EventSystem
{

	private static Dictionary<string, List<SubscriberInfo>> eventHandlers = new Dictionary<string, List<SubscriberInfo>>();

	public static void Publish( Enum enu, params object[] parameters )
	{
		Publish( enu.ToString(), parameters );
	}

	public static void Publish( string message, params object[] parameters )
	{
		if ( eventHandlers.ContainsKey( message ) )
		{
			foreach ( var subscriber in eventHandlers[message] )
			{
				var methodParams = subscriber.MethodInfo.GetParameters();
				var args = new object[methodParams.Length];
				for ( int i = 0; i < methodParams.Length; i++ )
				{
					if ( i < parameters.Length && parameters[i] != null && methodParams[i].ParameterType.IsAssignableFrom( parameters[i].GetType() ) )
					{
						args[i] = parameters[i];
					}
					else
					{
						args[i] = GetDefault( methodParams[i].ParameterType );
						Console.WriteLine( $"Warning: Parameter mismatch for event '{message}', using default for parameter {i}." );
					}
				}

				try
				{
					subscriber.MethodInfo.Invoke( subscriber.Listener, args );
				}
				catch ( Exception ex )
				{
					Console.WriteLine( $"Exception when invoking event handler: {ex.Message}" );
				}
			}
		}
	}

	public static void Subscribe( object listener )
	{
		var methods = listener.GetType().GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
		foreach ( var method in methods )
		{
			var attrs = method.GetCustomAttributes( typeof( EventAttribute ), false );
			foreach ( EventAttribute attr in attrs )
			{
				if ( !eventHandlers.ContainsKey( attr.Message ) )
				{
					eventHandlers[attr.Message] = new List<SubscriberInfo>();
				}
				eventHandlers[attr.Message].Add( new SubscriberInfo { Listener = listener, MethodInfo = method } );
			}
		}
	}

	public static void Unsubscribe( object listener )
	{
		foreach ( var handlerList in eventHandlers.Values )
		{
			handlerList.RemoveAll( subscriber => subscriber.Listener == listener );
		}
	}

	private static object GetDefault( Type type )
	{
		return type.IsValueType ? Activator.CreateInstance( type ) : null;
	}

	private class SubscriberInfo
	{
		public object Listener { get; set; }
		public MethodInfo MethodInfo { get; set; }
	}

}
