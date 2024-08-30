
namespace Graybox.Lightmapper;

public class LightmapData : IDisposable
{

	List<Lightmap> lightmaps = new();
	public IReadOnlyList<Lightmap> Lightmaps => lightmaps;
	public LightmapBaker Baker { get; } = new();

	public void Clear()
	{
		foreach ( var lm in Lightmaps )
		{
			lm.Dispose();
		}
		lightmaps.Clear();
	}

	public void Set( IEnumerable<Lightmap> lightmaps )
	{
		Clear();
		if ( lightmaps != null )
			this.lightmaps.AddRange( lightmaps );
	}

	public void Dispose()
	{
		Clear();
	}

}
