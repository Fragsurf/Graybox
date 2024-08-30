
namespace Graybox.Editor;

internal class Popup
{

	public bool Shown { get; private set; }

	protected readonly int id;
	static int idaccumulator;

	public Popup()
	{
		id = ++idaccumulator;
		PopupManager.Add( this );
	}

	public void Close()
	{
		PopupManager.Remove( this );
	}

	public void Show()
	{
		Shown = true;
	}

	public virtual void Update()
	{
	}

}
