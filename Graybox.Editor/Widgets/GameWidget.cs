
using ImGuiNET;

namespace Graybox.Editor.Widgets;

internal class GameWidget : BaseWidget
{

	public override string Title => "Game";

	PlaytestGame.PlaytestGame Game;

	public GameWidget()
	{
		Game = new();
		Game.Initialize();
	}

	protected override void OnMouseDown( ref InputEvent e )
	{
		base.OnMouseDown( ref e );

		this.CursorGrabbed = true;
	}

	protected override void OnKeyDown( ref InputEvent e )
	{
		base.OnKeyDown( ref e );

		if ( e.Key == Key.Escape )
		{
			this.CursorGrabbed = false;
		}
	}

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );

		var sz = ImGui.GetContentRegionAvail();

		Game.ActiveScene.Configure( (int)sz.X, (int)sz.Y, 2 );
		Game.ActiveScene.Render();
		Game.ActiveScene.ShowFPS = true;
		Game.DeltaTime = frameInfo.DeltaTime;
		Game.IsFocused = CursorGrabbed;
		Game.Update();

		ImGui.Image( Game.ActiveScene.TextureId, ImGui.GetContentRegionAvail(), new SVector2( 0, 1 ), new SVector2( 1, 0 ) );

		EditorWindow.Instance.UpdateACoupleFrames( 30 );
	}

}
