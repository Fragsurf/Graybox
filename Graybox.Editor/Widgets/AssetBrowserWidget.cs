using Graybox.Editor.Documents;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace Graybox.Editor.Widgets;

internal class AssetBrowserWidget : BaseWidget
{

	[EditorLayout.Data]
	public float ThumbnailSize { get; set; } = 120f;

	public override string Title => "Asset Browser";
	private string _searchText = "";
	private AssetTypes _selectedAssetType = AssetTypes.None;
	private const float MIN_THUMBNAIL_SIZE = 60f;
	private const float MAX_THUMBNAIL_SIZE = 200f;
	private string _selectedPackage = "All Packages";

	bool IsListView => ThumbnailSize <= MIN_THUMBNAIL_SIZE;

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );

		var assetSystem = DocumentManager.CurrentDocument?.AssetSystem;
		if ( assetSystem == null )
		{
			ImGui.End();
			return;
		}

		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 4, 4 ) );
		{
			// Asset type dropdown
			ImGui.SetNextItemWidth( 150 );

			var assetTypeName = _selectedAssetType == AssetTypes.None ? "All" : _selectedAssetType.ToString();

			if ( ImGui.BeginCombo( "##AssetTypeCombo", assetTypeName ) )
			{
				if ( ImGui.Selectable( "All", _selectedAssetType == AssetTypes.None ) )
					_selectedAssetType = AssetTypes.None;
				foreach ( AssetTypes type in Enum.GetValues( typeof( AssetTypes ) ) )
				{
					if ( type == AssetTypes.None ) continue;
					if ( ImGui.Selectable( type.ToString(), _selectedAssetType == type ) )
						_selectedAssetType = type;
				}
				ImGui.EndCombo();
			}

			ImGui.SameLine();

			// Package selection combo
			ImGui.SetNextItemWidth( 150 );
			if ( ImGui.BeginCombo( "##PackageCombo", _selectedPackage ) )
			{
				if ( ImGui.Selectable( "All Packages", _selectedPackage == "All Packages" ) )
					_selectedPackage = "All Packages";
				foreach ( var package in assetSystem.Packages )
				{
					if ( ImGui.Selectable( package.Name, _selectedPackage == package.Name ) )
						_selectedPackage = package.Name;
				}
				ImGui.EndCombo();
			}

			ImGui.SameLine();

			// Search input
			float remainingWidth = ImGui.GetContentRegionAvail().X - 170 - 24; // Reserve space for the slider and toggle button
			ImGui.SetNextItemWidth( remainingWidth );
			ImGui.InputTextWithHint( "##AssetSearch", "Search...", ref _searchText, 265 );

			ImGui.SameLine();

			ImGui.SameLine();
			ImGui.SetNextItemWidth( -1 );
			var thumbSize = ThumbnailSize;
			ImGui.SliderFloat( "##ThumbnailSize", ref thumbSize, MIN_THUMBNAIL_SIZE, MAX_THUMBNAIL_SIZE, "Size: %.0f" );
			ThumbnailSize = thumbSize;
		}
		ImGui.PopStyleVar();

		ImGui.Spacing();

		if ( ImGui.BeginChild( "Assets", new SVector2( 0, 0 ) ) )
		{
			ImGui.Spacing();

			var pos = ImGui.GetWindowPos();
			var sz = ImGui.GetWindowSize();
			var wnd = ImGui.GetWindowDrawList();
			var color = ImGui.ColorConvertFloat4ToU32( new SVector4( 0, 0, 0, .25f ) );
			wnd.AddRectFilled( pos, pos + sz, color );

			if ( IsListView )
			{
				DisplayAssetsListView( assetSystem );
			}
			else
			{
				DisplayAssetsGridView( assetSystem );
			}

			ImGui.Spacing();
			ImGui.EndChild();
		}
	}

	private void DisplayAssetsGridView( AssetSystem assetSystem )
	{
		var framePadding = ImGui.GetStyle().FramePadding;
		var thumbnailWidth = ThumbnailSize - framePadding.X * 2;
		var thumbnailHeight = ThumbnailSize - framePadding.X * 2;

		float windowWidth = ImGui.GetContentRegionAvail().X;
		int numColumns = Math.Max( 1, (int)(windowWidth / (ThumbnailSize + 10)) ); // +10 for spacing

		ImGui.Columns( numColumns, "Assets", false );

		foreach ( var package in assetSystem.Packages )
		{
			foreach ( var asset in package.Assets )
			{
				if ( _selectedAssetType != AssetTypes.None && asset.AssetType != _selectedAssetType )
					continue;
				if ( !string.IsNullOrEmpty( _searchText ) && !asset.Name.Contains( _searchText, StringComparison.OrdinalIgnoreCase ) )
					continue;

				ImGui.BeginGroup();
				ImGui.PushStyleColor( ImGuiCol.Button, new SVector4( 0, 0, 0, 0 ) );

				var thumbnailTexture = asset.GetGLThumbnail();
				if ( ImGui.ImageButton( asset.RelativePath, thumbnailTexture, new SVector2( thumbnailWidth, thumbnailHeight ) ) )
				{
					Debug.LogError( asset.RelativePath + ":" + thumbnailTexture );
					Selection.TrySelect( asset );
				}
				ImGui.PopStyleColor();

				// Adjust text size based on thumbnail size
				float textScale = Math.Max( 0.7f, Math.Min( 1.0f, ThumbnailSize / 120f ) );
				ImGui.SetWindowFontScale( textScale );

				// Truncate and center-align asset name
				string displayName = asset.Name;

				ImGuiEx.TextTruncate( displayName, ImGui.GetContentRegionAvail().X );
				ImGui.SetWindowFontScale( 1.0f );

				if ( ImGui.IsItemHovered() )
				{
					ImGui.BeginTooltip();
					ImGui.Text( asset.RelativePath );
					ImGui.EndTooltip();
				}
				ImGui.EndGroup();

				if ( ImGui.BeginPopupContextItem( $"ContextMenu_{asset.RelativePath}" ) )
				{
					DisplayContextMenu( asset );
					ImGui.EndPopup();
				}

				if ( ImGui.BeginDragDropSource( ImGuiDragDropFlags.None ) )
				{
					string assetPath = asset.RelativePath;
					int byteCount = System.Text.Encoding.UTF8.GetByteCount( assetPath );
					IntPtr unmanagedPointer = Marshal.AllocHGlobal( byteCount );
					try
					{
						Marshal.Copy( System.Text.Encoding.UTF8.GetBytes( assetPath ), 0, unmanagedPointer, byteCount );
						ImGui.SetDragDropPayload( "ASSET", unmanagedPointer, (uint)byteCount );
						ImGui.Text( asset.RelativePath );
					}
					finally
					{
						ImGui.EndDragDropSource();
						Marshal.FreeHGlobal( unmanagedPointer ); // Free the unmanaged memory
					}
				}

				ImGui.NextColumn();
			}
		}

		ImGui.Columns( 1 );
	}

	private void DisplayAssetsListView( AssetSystem assetSystem )
	{
		ImGui.Columns( 2, "AssetListView", false );
		ImGui.SetColumnWidth( 0, 32 ); // Width for the icon column

		foreach ( var package in assetSystem.Packages )
		{
			foreach ( var asset in package.Assets )
			{
				if ( !ShouldDisplayAsset( asset ) ) continue;

				var thumbnailTexture = asset.GetGLThumbnail();
				ImGui.Image( thumbnailTexture, new SVector2( 16, 16 ) );
				ImGui.NextColumn();

				bool isSelected = ImGui.Selectable( asset.Name, false, ImGuiSelectableFlags.SpanAllColumns );
				if ( isSelected )
				{
					Selection.TrySelect( asset );
				}

				if ( ImGui.IsItemHovered() )
				{
					ImGui.BeginTooltip();

					// Display a larger thumbnail
					ImGui.Image( thumbnailTexture, new SVector2( 128, 128 ) );

					ImGui.SameLine();
					ImGui.BeginGroup();
					ImGui.Text( "Name: " + asset.Name );
					ImGui.Text( "Type: " + asset.AssetType.ToString() );
					ImGui.Text( "Path: " + asset.RelativePath );
					ImGui.EndGroup();

					ImGui.EndTooltip();
				}

				if ( ImGui.BeginDragDropSource( ImGuiDragDropFlags.None ) )
				{
					string assetPath = asset.RelativePath;
					int byteCount = System.Text.Encoding.UTF8.GetByteCount( assetPath );
					IntPtr unmanagedPointer = Marshal.AllocHGlobal( byteCount );
					try
					{
						Marshal.Copy( System.Text.Encoding.UTF8.GetBytes( assetPath ), 0, unmanagedPointer, byteCount );
						ImGui.SetDragDropPayload( "ASSET", unmanagedPointer, (uint)byteCount );
						ImGui.Text( asset.RelativePath );
					}
					finally
					{
						ImGui.EndDragDropSource();
						Marshal.FreeHGlobal( unmanagedPointer ); // Free the unmanaged memory
					}
				}

				ImGui.NextColumn();
			}
		}

		ImGui.Columns( 1 );
	}

	private void DisplayContextMenu( Asset asset )
	{
		if ( ImGui.MenuItem( "Open in Explorer" ) )
		{
			var absPath = asset.AbsolutePath;
			if ( File.Exists( absPath ) )
			{
				System.Diagnostics.Process.Start( "explorer.exe", $"/select,\"{absPath}\"" );
			}
			else
			{
				Debug.LogError( "File not found: " + absPath );
			}
		}
		if ( ImGui.MenuItem( "Copy Path" ) )
		{
			ImGui.SetClipboardText( asset.RelativePath );
			Debug.Log( $"Copied asset path: {asset.RelativePath}" );
		}
	}

	private bool ShouldDisplayAsset( Asset asset )
	{
		if ( _selectedAssetType != AssetTypes.None && asset.AssetType != _selectedAssetType )
			return false;
		if ( !string.IsNullOrEmpty( _searchText ) && !asset.Name.Contains( _searchText, StringComparison.OrdinalIgnoreCase ) )
			return false;
		if ( _selectedPackage != "All Packages" && asset.Package.Name != _selectedPackage )
			return false;
		return true;
	}

}
