
namespace Graybox;

public interface IInputListener
{

	void OnKeyUp( ref InputEvent e );
	void OnKeyDown( ref InputEvent e );
	void OnMouseDown( ref InputEvent e );
	void OnMouseUp( ref InputEvent e );
	void OnMouseMove( ref InputEvent e );

}
