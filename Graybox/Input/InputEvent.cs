
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Graybox;

public struct InputEvent
{

	public InputEvent( MouseMoveEventArgs e )
	{
		MousePosition = new Vector2( e.X, e.Y );
		MouseDelta = new Vector2( e.DeltaX, e.DeltaY );
		MouseScroll = Input.MouseScroll;
		Modifiers = Input.KeyModifiers;
	}

	public InputEvent( MouseButtonEventArgs e )
	{
		Button = e.Button;
		Action = e.Action;
		Modifiers = e.Modifiers;
		MousePosition = Input.MousePosition;
		MouseDelta = Input.MouseDelta;
		MouseScroll = Input.MouseScroll;
	}

	public InputEvent( KeyboardKeyEventArgs e )
	{
		Key = e.Key;
		Modifiers = e.Modifiers;
		Handled = false;
		MousePosition = Input.MousePosition;
		MouseDelta = Input.MouseDelta;
		MouseScroll = Input.MouseScroll;
	}

	public Key Key;
	public MouseButton Button;
	public InputAction Action;
	public KeyModifier Modifiers;
	public Vector2 LocalMousePosition;
	public Vector2 MousePosition;
	public Vector2 MouseDelta;
	public Vector2 MouseScroll;
	public char KeyChar;
	public bool Handled;

	public readonly bool Control => Modifiers.HasFlag( KeyModifier.Control );
	public readonly bool Alt => Modifiers.HasFlag( KeyModifier.Alt );
	public readonly bool Shift => Modifiers.HasFlag( KeyModifier.Shift );

}
