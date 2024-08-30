using ImGuiNET;

namespace Graybox.Editor;

internal class BusyPopup : Popup
{
	public string Title { get; set; }
	public string Label { get; set; }
	public float Progress { get; set; }
	public bool IsCancelable { get; set; }
	public Action OnCancel { get; set; }

	public BusyPopup( string title, string label, bool isCancelable = false, Action onCancel = null )
	{
		Title = title;
		Label = label;
		Progress = 0f;
		IsCancelable = isCancelable;
		OnCancel = onCancel;
	}

	public override void Update()
	{
		ImGui.OpenPopup( $"{Title}##BusyPopup{id}" );
		ImGui.SetNextWindowSize( new SVector2( 400, 150 ) );
		if ( ImGui.BeginPopupModal( $"{Title}##BusyPopup{id}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings ) )
		{
			ImGui.Spacing();
			ImGui.TextWrapped( Label );
			ImGui.Spacing();
			ImGui.ProgressBar( Progress, new SVector2( -1, 0 ), $"{(int)(Progress * 100)}%" );
			ImGui.Spacing();

			if ( IsCancelable )
			{
				var availableSize = ImGui.GetContentRegionAvail();
				if ( ImGui.Button( "Cancel", new SVector2( availableSize.X, 0 ) ) )
				{
					OnCancel?.Invoke();
					Close();
				}
			}

			ImGui.EndPopup();
		}
	}

	public void SetProgress( float progress )
	{
		Progress = System.Math.Clamp( progress, 0f, 1f );
	}
}
