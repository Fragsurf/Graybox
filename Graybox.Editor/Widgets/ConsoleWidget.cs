using ImGuiNET;

namespace Graybox.Editor.Widgets;

internal class ConsoleWidget : BaseWidget
{

	public static ConsoleWidget Instance;

	private string _filterText = string.Empty;

	public override string Title => "Console";

	public ConsoleWidget()
	{
		Instance = this;
	}

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 8, 4 ) );

		ImGui.PushItemWidth( 300 ); // Set width for the search box
		ImGui.InputTextWithHint( "##filter", "Search...", ref _filterText, 256 );
		ImGui.PopItemWidth();

		// Align the Clear button to the right
		int btnWidth = 80;
		ImGui.SameLine( ImGui.GetWindowWidth() - btnWidth ); // Adjust the position of the Clear button
		ImGui.PushStyleColor( ImGuiCol.Button, new SVector4( 0.5f, 0.5f, 0.5f, 1.0f ) ); // Muted color
		ImGui.PushStyleColor( ImGuiCol.ButtonHovered, new SVector4( 0.6f, 0.6f, 0.6f, 1.0f ) ); // Slightly brighter on hover
		ImGui.PushStyleColor( ImGuiCol.ButtonActive, new SVector4( 0.7f, 0.7f, 0.7f, 1.0f ) ); // Slightly brighter when active

		if ( ImGui.Button( "Clear", new SVector2( btnWidth, 0 ) ) )
		{
			Debug.Clear();
		}

		ImGui.PopStyleColor( 3 );
		ImGui.Separator();

		ImGui.PushStyleColor( ImGuiCol.ChildBg, new SVector4( 0.1f, 0.1f, 0.1f, 1.0f ) );
		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 0, 0 ) );
		if ( ImGui.BeginChild( "scrolling", new SVector2( 0, 0 ) ) )
		{
			ImGui.Indent( 10 );
			ImGui.Spacing();

			for ( int i = Debug.LogMessages.Count - 1; i >= 0; i-- )
			{
				var logEntry = Debug.LogMessages[i];
				if ( string.IsNullOrEmpty( _filterText ) || logEntry.Message.Contains( _filterText, StringComparison.OrdinalIgnoreCase ) )
				{
					ImGui.AlignTextToFramePadding();
					ImGui.PushStyleColor( ImGuiCol.Text, logEntry.GetColor() );
					ImGui.PushTextWrapPos( ImGui.GetContentRegionAvail().X );
					string displayMessage = logEntry.Count > 1 ? $"[{logEntry.Count}] {logEntry.Message}" : logEntry.Message;
					if ( logEntry.OnClicked != null )
					{
						if ( ImGui.Selectable( displayMessage, false, ImGuiSelectableFlags.None ) )
						{
							logEntry.OnClicked?.Invoke();
						}
					}
					else
					{
						ImGui.TextUnformatted( displayMessage );
					}
					ImGui.PopStyleColor();
					ImGui.PopTextWrapPos();
				}
			}

			ImGui.Spacing();
			ImGui.EndChild();
		}
		ImGui.PopStyleColor();
		ImGui.PopStyleVar( 2 );
	}
}
