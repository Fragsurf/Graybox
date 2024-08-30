using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graybox.Editor;

internal class ConfirmationPopup : Popup
{

	public string Title { get; set; }
	public string Message { get; set; }
	public string ConfirmText { get; set; }
	public string CancelText { get; set; }
	public Action OnConfirm { get; set; }
	public Action OnCancel { get; set; }

	public ConfirmationPopup( string title, string message, string confirmText, string cancelText, Action onConfirm, Action onCancel )
	{
		Title = title;
		Message = message;
		ConfirmText = confirmText;
		CancelText = cancelText;
		OnConfirm = onConfirm;
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
			var btnWidth = (availableSize.X - space) / 2;

			if ( ImGuiEx.IconButtonPrimary( ConfirmText, EditorResource.Image( "assets/icons/check.png" ), new SVector2( btnWidth, 0 ) ) )
			{
				OnConfirm?.Invoke();
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
