
using Graybox.Editor.Documents;
using ImGuiNET;

namespace Graybox.Editor.Widgets;

internal class BaseWidget
{

	private bool wasHovered;

	static Dictionary<Type, int> widgetIdAccumulator = new();
	static int GetNextWidgetId( BaseWidget widget )
	{
		if ( !widgetIdAccumulator.ContainsKey( widget.GetType() ) )
		{
			widgetIdAccumulator[widget.GetType()] = 0;
		}
		widgetIdAccumulator[widget.GetType()]++;
		return widgetIdAccumulator[widget.GetType()];
	}

	public virtual string Title => "Base Widget";
	public SVector2 Size { get; private set; }
	public SVector2 Position { get; private set; }
	public bool IsHovered { get; private set; }
	public bool IsFocused { get; private set; }
	public bool CursorGrabbed { get; set; }
	public virtual bool HideTabBar { get; }
	public virtual SVector4? WindowBackground { get; }
	public virtual SVector2? WindowPadding { get; }

	public string LayoutID;

	SVector2 lastSize;

	public BaseWidget()
	{
		LayoutID = GetType().Name + "#" + GetNextWidgetId( this );
	}

	protected virtual void OnUpdate( FrameInfo frameInfo )
	{
	}

	protected virtual void OnResized()
	{
	}

	protected virtual void OnDestroyed()
	{
	}

	public void Destroy() 
	{ 
		OnDestroyed();
	}

	public void Update( FrameInfo frameInfo )
	{
		var result = ImGui.BeginChild( GetType().Name );

		if ( !result ) return;

		var eventProcessed = false;
		var io = ImGui.GetIO();
		var widgetScreenPos = ImGui.GetWindowPos();
		var widgetSize = ImGui.GetWindowSize();
		var isMouseOverWidget = ImGui.IsMouseHoveringRect( widgetScreenPos, widgetScreenPos + widgetSize );

		Size = widgetSize;
		Position = widgetScreenPos;
		IsHovered = isMouseOverWidget;
		IsFocused = ImGui.IsWindowFocused() && ImGui.IsWindowHovered();

		if ( lastSize != widgetSize )
		{
			lastSize = widgetSize;
			OnResized();
		}

		if ( isMouseOverWidget && !wasHovered )
		{
			OnMouseEnter();
			eventProcessed = true;
		}
		else if ( !isMouseOverWidget && wasHovered )
		{
			OnMouseLeave();
			eventProcessed = true;
		}

		OnUpdate( frameInfo );
		ImGui.EndChild();

		wasHovered = isMouseOverWidget;

		if ( eventProcessed )
		{
			EditorWindow.Instance.UpdateACoupleFrames( 10 );
		}
	}

	public virtual void OnDocumentActivated( Document document ) { }
	public virtual void OnDocumentDeactivated( Document document ) { }

	public void PushWindowStyles()
	{
		if ( WindowBackground != null )
		{
			ImGui.PushStyleColor( ImGuiCol.WindowBg, WindowBackground.Value );
		}

		if ( WindowPadding != null )
		{
			ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, WindowPadding.Value );
		}
	}

	public void PopWindowStyles()
	{
		if ( WindowBackground != null )
		{
			ImGui.PopStyleColor( 1 );
		}

		if ( WindowPadding != null )
		{
			ImGui.PopStyleVar( 1 );
		}
	}

	public Vector2 ScreenToLocal( Vector2 screenPos )
	{
		return new( screenPos.X - Position.X, screenPos.Y - Position.Y );
	}

	protected virtual void OnMouseEnter() { }
	protected virtual void OnMouseLeave() { }
	protected virtual void OnKeyDown( ref InputEvent e ) { }
	protected virtual void OnKeyUp( ref InputEvent e ) { }
	protected virtual void OnMouseMove( ref InputEvent e ) { }
	protected virtual void OnMouseDown( ref InputEvent e ) { }
	protected virtual void OnMouseDoubleClick( ref InputEvent e ) { }
	protected virtual void OnMouseUp( ref InputEvent e ) { }

	internal virtual void OnDataGet() { }
	internal virtual void OnDataSet() { }

	DateTime lastMouseDownTime;
	MouseButton lastMouseDownButton;
	Vector2 lastMouseDownPosition;
	public void HandleMouseDown( ref InputEvent e )
	{
		if ( !IsFocused || !IsHovered ) return;

		var dist = ( e.MousePosition - lastMouseDownPosition ).Length;
		if ( dist < 5 && e.Button == MouseButton.Left && DateTime.UtcNow - lastMouseDownTime < TimeSpan.FromMilliseconds( 250 ) && e.Button == lastMouseDownButton )
		{
			OnMouseDoubleClick( ref e );
		}

		lastMouseDownTime = DateTime.UtcNow;
		lastMouseDownButton = e.Button;
		lastMouseDownPosition = e.MousePosition;

		if ( e.Handled ) return;

		e.LocalMousePosition = ScreenToLocal( e.MousePosition );
		OnMouseDown( ref e );
	}

	public void HandleMouseUp( ref InputEvent e )
	{
		if ( !IsFocused ) return;

		e.LocalMousePosition = ScreenToLocal( e.MousePosition );
		OnMouseUp( ref e );
	}

	public void HandleMouseMove( ref InputEvent e )
	{
		if ( !IsFocused ) return;

		e.LocalMousePosition = ScreenToLocal( e.MousePosition );
		OnMouseMove( ref e );
	}

	public void HandleKeyDown( ref InputEvent e )
	{
		if ( !IsFocused ) return;

		e.LocalMousePosition = ScreenToLocal( e.MousePosition );
		OnKeyDown( ref e );
	}

	public void HandleKeyUp( ref InputEvent e )
	{
		if ( !IsFocused ) return;

		e.LocalMousePosition = ScreenToLocal( e.MousePosition );
		OnKeyUp( ref e );
	}

}
