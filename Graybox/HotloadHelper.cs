
[assembly: System.Reflection.Metadata.MetadataUpdateHandler( typeof( HotloadHelper ) )]

public static class HotloadHelper
{

	public static event Action OnHotload;

	public static void ClearCache( Type[] updatedTypes )
	{
	}

	public static void UpdateApplication( Type[] updatedTypes )
	{
		OnHotload?.Invoke();
	}

}
