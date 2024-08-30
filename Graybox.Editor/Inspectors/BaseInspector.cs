
namespace Graybox.Editor.Inspectors;

public abstract class BaseInspector
{
	public virtual object Target { get; set; }
	public abstract void DrawInspector();
}

public abstract class BaseInspector<T> : BaseInspector
{
	public override abstract void DrawInspector();
}
