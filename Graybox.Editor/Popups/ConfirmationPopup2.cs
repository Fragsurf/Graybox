using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graybox.Editor;

internal class ConfirmationPopup2 : Popup
{
	public string Title { get; set; }
	public string Message { get; set; }
	public string ConfirmText { get; set; }
	public string DenyText { get; set; }
	public string CancelText { get; set; }
	public Action OnConfirm { get; set; }
	public Action OnDeny { get; set; }
	public Action OnCancel { get; set; }

	public ConfirmationPopup2( string title, string message, string confirmText, string denyText, string cancelText, Action onConfirm, Action onDeny, Action onCancel )
	{
		Title = title;
		Message = message;
		ConfirmText = confirmText;
		DenyText = denyText;
		CancelText = cancelText;
		OnConfirm = onConfirm;
		OnDeny = onDeny;
		OnCancel = onCancel;
	}

	public override void Update()
	{
		ImGui.OpenPopup( $"{Title}##ConfirmationPopup{id}" );

		ImGui.SetNextWindowSize( new SVector2( 500, 0 ) );
		if ( ImGui.BeginPopupModal( $"{Title}##ConfirmationPopup{id}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings ) )
		{
			ImGui.Spacing();

			ImGui.Text( Message );
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginGroup();

			var availableSize = ImGui.GetContentRegionAvail();
			var space = 8f;
			var btnWidth = (availableSize.X - space * 3) / 3;

			if ( ImGuiEx.IconButtonPrimary( ConfirmText, EditorResource.Image( "assets/icons/check.png" ), new SVector2( btnWidth, 0 ) ) )
			{
				OnConfirm?.Invoke();
				Close();
			}

			ImGui.SameLine();

			if ( ImGuiEx.IconButtonDim( DenyText, EditorResource.Image( "assets/icons/exclamation.png" ), new SVector2( btnWidth, 0 ) ) )
			{
				OnDeny?.Invoke();
				Close();
			}

			ImGui.SameLine();

			if ( ImGuiEx.IconButtonDim( CancelText, EditorResource.Image( "assets/icons/cancel.png" ), new SVector2( btnWidth, 0 ) ) )
			{
				OnCancel?.Invoke();
				Close();
			}

			ImGui.EndGroup();
			ImGui.EndPopup();
		}
	}
}
