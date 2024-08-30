
namespace Graybox.Editor.Brushes;

public static class BrushManager
{

	public static BrushSettings CurrentBrushSettings { get; private set; } = new();

	static IBrush currentBrush;
	public static IBrush CurrentBrush
	{
		get => currentBrush ?? _brushes.FirstOrDefault();
		set => currentBrush = value;
	}
	public static IReadOnlyList<IBrush> Brushes => _brushes;

	private static bool _roundCreatedVertices = true;
	private static readonly List<IBrush> _brushes = new()
	{
		new BlockBrush(),
		new TetrahedronBrush(),
		new PyramidBrush(),
		new WedgeBrush(),
		new CylinderBrush(),
		new ConeBrush(),
		new PipeBrush(),
		//new ArchBrush(),
		new SphereBrush(),
		new StairsBrush(),
		//new TorusBrush()
	};

	public static bool RoundCreatedVertices
	{
		get { return _roundCreatedVertices; }
		set => _roundCreatedVertices = value;
	}

	public static void UpdateSelectedBrush( IBrush brush )
	{
		CurrentBrush = brush;
		CurrentBrushSettings = brush.GetSettingsInstance();
	}

}
