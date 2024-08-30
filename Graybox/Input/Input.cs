
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Graybox;

public static class Input
{

	public static KeyboardState KeyboardState;
	public static MouseState MouseState;

	public static bool IsDown( Keys key ) => KeyboardState?.IsKeyDown( key ) ?? false;
	public static bool JustPressed( Keys key ) => KeyboardState?.IsKeyPressed( key ) ?? false;
	public static bool JustReleased( Keys key ) => KeyboardState?.IsKeyReleased( key ) ?? false;

	public static bool IsDown( MouseButton mouseButton ) => MouseState?.IsButtonDown( mouseButton ) ?? false;
	public static bool JustPressed( MouseButton mouseButton ) => MouseState?.IsButtonPressed( mouseButton ) ?? false;
	public static bool JustReleased( MouseButton mouseButton ) => MouseState?.IsButtonReleased( mouseButton ) ?? false;

	public static Vector2 MouseDelta => MouseState?.Delta ?? default;
	public static Vector2 MouseScroll => MouseState?.ScrollDelta ?? default;
	public static Vector2 MousePosition => MouseState?.Position ?? default;

	public static KeyModifier KeyModifiers
	{
		get
		{
			var result = 0;
			if ( AltModifier ) result |= (int)KeyModifier.Alt;
			if ( ShiftModifier ) result |= (int)KeyModifier.Shift;
			if ( ControlModifier ) result |= (int)KeyModifier.Control;

			return (KeyModifier)result;
		}
	}
	public static bool ControlModifier => IsDown( Keys.LeftControl ) || IsDown( Keys.RightControl );
	public static bool ShiftModifier => IsDown( Keys.LeftShift ) || IsDown( Keys.RightShift );
	public static bool AltModifier => IsDown( Keys.LeftAlt ) || IsDown( Keys.RightAlt );

	static List<IInputListener> InputListeners = new();

	public static void Register( IInputListener listener )
	{
		if ( InputListeners.Contains( listener ) )
			return;

		InputListeners.Add( listener );
	}

	public static InputEvent ProcessKeyDown( KeyboardKeyEventArgs e )
	{
		var inputEvent = new InputEvent( e );

		foreach ( var listener in InputListeners )
		{
			listener.OnKeyDown( ref inputEvent );
			if ( inputEvent.Handled ) break;
		}

		return inputEvent;
	}

	public static InputEvent ProcessKeyUp( KeyboardKeyEventArgs e )
	{
		var inputEvent = new InputEvent( e );

		foreach ( var listener in InputListeners )
		{
			listener.OnKeyUp( ref inputEvent );
			if ( inputEvent.Handled ) break;
		}

		return inputEvent;
	}

	public static InputEvent ProcessMouseDown( MouseButtonEventArgs e )
	{
		var inputEvent = new InputEvent( e );

		foreach ( var listener in InputListeners )
		{
			listener.OnMouseDown( ref inputEvent );
			if ( inputEvent.Handled ) break;
		}

		return inputEvent;
	}

	public static InputEvent ProcessMouseUp( MouseButtonEventArgs e )
	{
		var inputEvent = new InputEvent( e );

		foreach ( var listener in InputListeners )
		{
			listener.OnMouseUp( ref inputEvent );
			if ( inputEvent.Handled ) break;
		}

		return inputEvent;
	}

	public static InputEvent ProcessMouseMove( MouseMoveEventArgs e )
	{
		var inputEvent = new InputEvent( e );

		foreach ( var listener in InputListeners )
		{
			listener.OnMouseMove( ref inputEvent );
			if ( inputEvent.Handled ) break;
		}

		return inputEvent;
	}

}
