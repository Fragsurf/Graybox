
using ImGuiNET;

namespace Graybox.Editor;

internal class ExceptionPopup : Popup
{

	public string Title { get; set; }
	public string ExceptionMessage { get; set; }
	public string StackTrace { get; set; }
	public string OkText { get; set; }
	public string ReportText { get; set; }
	public Action OnOk { get; set; }
	public Action OnReport { get; set; }

	public ExceptionPopup( string title, Exception exception, string okText, string reportText, Action onOk, Action onReport )
	{
		Title = title;
		ExceptionMessage = exception.Message;
		StackTrace = exception.StackTrace;
		OkText = okText;
		ReportText = reportText;
		OnOk = onOk;
		OnReport = onReport;
	}

	public override void Update()
	{
		ImGui.OpenPopup( $"{Title}##ExceptionPopup{id}" );

		ImGui.SetNextWindowSize( new SVector2( 750, 550 ) );
		if ( ImGui.BeginPopupModal( $"{Title}##ExceptionPopup{id}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings ) )
		{
			ImGui.Spacing();

			var width = ImGui.GetContentRegionAvail().X;

			ImGui.PushTextWrapPos( width );
			ImGui.TextColored( new SVector4( 0.8f, 0.2f, 0.2f, 1.0f ), ExceptionMessage );
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();
			ImGui.PopTextWrapPos();

			if ( !string.IsNullOrEmpty( StackTrace ) )
			{
				ImGui.BeginChild( "StackTrace", new SVector2( 0, -55 ) );
				ImGui.TextWrapped( StackTrace );
				ImGui.EndChild();
			}

			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginGroup();

			var availableSize = ImGui.GetContentRegionAvail();
			var space = 8f;
			var btnWidth = (availableSize.X - space) / 2;

			ImGuiEx.PushButtonPrimary();
			if ( ImGui.Button( ReportText, new SVector2( btnWidth, 0 ) ) )
			{
				OnReport?.Invoke();
				Close();
			}
			ImGuiEx.PopButtonPrimary();

			ImGui.SameLine();

			if ( ImGui.Button( OkText, new SVector2( btnWidth, 0 ) ) )
			{
				OnOk?.Invoke();
				Close();
			}

			ImGui.EndGroup();
			ImGui.EndPopup();
		}
	}

}
