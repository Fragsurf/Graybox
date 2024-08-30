
namespace Graybox.Editor;

internal static class Selection
{

	static List<object> selection = new( 256 );
	public static IReadOnlyList<object> SelectedObjects => selection;

	public static void Clear()
	{
		selection.Clear();
	}

	public static void TrySelect( object obj )
	{
		if ( Input.ControlModifier )
		{
			if ( selection.Contains( obj ) )
			{
				selection.Remove( obj );
			}
			else
			{
				selection.Add( obj );
			}
		}
		else
		{
			if ( selection.Count == 1 && selection[0] == obj )
				return;

			selection.Clear();
			selection.Add( obj );
		}
	}

	public static void TrySelect( IEnumerable<object> objects )
	{
		if ( Input.ControlModifier )
		{
			foreach ( var obj in objects )
			{
				if ( selection.Contains( obj ) )
				{
					selection.Remove( obj );
				}
				else
				{
					selection.Add( obj );
				}
			}
		}
		else
		{
			selection.Clear();
			selection.AddRange( objects );
		}
	}

}
