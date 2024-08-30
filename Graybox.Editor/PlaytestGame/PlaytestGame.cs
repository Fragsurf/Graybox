
using Graybox.Editor.Documents;
using Graybox.GameSystem;

namespace Graybox.Editor.PlaytestGame;

internal class PlaytestGame : GrayboxGame
{

	public bool IsFocused;

	private Graybox.Bunnyhop.BhopMovement movement;

	protected override void OnInitialized()
	{
		base.OnInitialized();

		ActiveScene = new();
		movement = new( this );
	}

	public override void Update()
	{
		base.Update();

		var document = DocumentManager.CurrentDocument;
		var map = document?.Map;
		if ( map == null ) return;

		ActiveScene.AssetSystem = document.AssetSystem;
		ActiveScene.Camera.FarClip = 20000;
		ActiveScene.Lightmaps = document.Map.LightmapData;
		ActiveScene.Environment = document.Map.EnvironmentData;
		ActiveScene.SkyboxEnabled = true;
		ActiveScene.UpdateObjects( map.WorldSpawn );

		if ( IsFocused )
		{
			movement.Update();
			EditorWindow.Instance.UpdateFrequency = 300;
		}
	}

	public override void Tick()
	{
		base.Tick();

		movement.Tick();
	}

}
