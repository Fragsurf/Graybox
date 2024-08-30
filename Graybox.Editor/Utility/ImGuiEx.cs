
using ImGuiNET;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Graybox.Editor;

internal static class ImGuiEx
{

	const int EditLabelWidth = 75;

	public static unsafe void Header( string label, bool spacing = false )
	{
		if ( spacing )
		{
			ImGui.Spacing();
			ImGui.Spacing();
		}

		var padding = new SVector2( 10, 5 );
		var bgColor = *ImGui.GetStyleColorVec4( ImGuiCol.Header );
		var borderColor = bgColor * .25f;
		borderColor.W = 1.0f;

		// Calculate the size of the header with padding
		var textSize = ImGui.CalcTextSize( label );
		var headerSize = textSize + padding * 2;
		headerSize.X = ImGui.GetContentRegionAvail().X;

		// Get the current cursor position
		var cursorPos = ImGui.GetCursorScreenPos();

		// Draw the background rectangle
		var borderPos = cursorPos + new SVector2( 0, 2 );
		ImGui.GetWindowDrawList().AddRectFilled( borderPos, borderPos + headerSize, ImGui.ColorConvertFloat4ToU32( borderColor ) );
		ImGui.GetWindowDrawList().AddRectFilled( cursorPos, cursorPos + headerSize, ImGui.ColorConvertFloat4ToU32( bgColor ) );

		ImGui.Dummy( new SVector2( 0, padding.Y ) );
		ImGui.SetCursorScreenPos( cursorPos + padding );
		ImGui.Text( label );
		ImGui.Dummy( new SVector2( 0, padding.Y ) );
	}

	public static void EditAsset( string label, AssetSystem assetSystem, ref string assetPath )
	{
		var asset = assetSystem?.FindAsset( assetPath );
		int assetThumb = 0;
		if ( asset != null )
		{
			assetThumb = asset.GetGLThumbnail();
		}

		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 3, 3 ) );
		if ( ImGui.ImageButton( $"##{label}AssetThumb", assetThumb, new SVector2( 50, 50 ) ) ) // Placeholder thumbnail ID and button size
		{
			Debug.LogError( "Open asset browser" );
		}
		ImGui.PopStyleVar( 1 );

		unsafe
		{
			if ( ImGui.BeginDragDropTarget() )
			{
				ImGuiPayload* payload = ImGui.AcceptDragDropPayload( "ASSET" );
				if ( payload != null && payload->Data != null )
				{
					unsafe
					{
						var b = (byte*)payload->Data;
						var droppedData = System.Text.Encoding.UTF8.GetString( b, payload->DataSize );
						assetPath = droppedData;
					}
				}
				ImGui.EndDragDropTarget();
			}
		}

		ImGui.SameLine();
		ImGui.AlignTextToFramePadding();
		ImGui.TextDisabled( assetPath );
	}

	public static unsafe void EditAssetGrid( string label, AssetSystem assetSystem, string[] assetPaths, ref int selectedIndex, float slotSize = 50.0f )
	{
		// Calculate the size for each thumbnail
		var thumbnailSize = new SVector2( slotSize, slotSize );
		var framePadding = new SVector2( 3, 3 );
		var highlightColor = new SVector4( 1f, 1f, 0f, 1.0f );
		var itemSpacing = ImGui.GetStyle().ItemSpacing;

		// Begin the grid
		ImGui.BeginGroup();

		// Calculate max slots per row based on window width and slot size
		float windowWidth = ImGui.GetContentRegionAvail().X;
		int maxSlotsPerRow = (int)(windowWidth / (thumbnailSize.X + framePadding.X + itemSpacing.X));

		// Ensure we don't exceed the specified slots
		maxSlotsPerRow = Math.Max( 1, Math.Min( assetPaths.Length, maxSlotsPerRow ) );

		for ( int i = 0; i < assetPaths.Length; i++ )
		{
			int assetThumb = 0;
			if ( i < assetPaths.Length )
			{
				var asset = assetSystem?.FindAsset( assetPaths[i] );
				if ( asset != null )
				{
					assetThumb = asset.GetGLThumbnail();
				}
			}

			// Apply frame padding
			ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, framePadding );

			var highlightPos = ImGui.GetCursorScreenPos();

			var btnColor = selectedIndex == i ? new System.Numerics.Vector4( 1.0f, 1.0f, 0, 1.0f ) : *ImGui.GetStyleColorVec4( ImGuiCol.Button );
			ImGui.PushStyleColor( ImGuiCol.Button, btnColor );
			// ImageButton for the asset thumbnail
			if ( ImGui.ImageButton( $"##{label}AssetThumb{i}", (IntPtr)assetThumb, thumbnailSize ) )
			{
				selectedIndex = i; // Set the selection index
			}
			ImGui.PopStyleColor();

			// Highlight the selected asset
			if ( selectedIndex == i )
			{
				var drawList = ImGui.GetWindowDrawList();
				drawList.AddRect( highlightPos, highlightPos + thumbnailSize + framePadding * 2, ImGui.ColorConvertFloat4ToU32( highlightColor ) );
			}

			// Check for double-click to open asset browser
			if ( ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked( 0 ) )
			{
				Debug.LogError( "Open asset browser" );
			}

			// Pop frame padding style
			ImGui.PopStyleVar( 1 );

			// Handle drag-and-drop
			unsafe
			{
				if ( ImGui.BeginDragDropTarget() )
				{
					ImGuiPayload* payload = ImGui.AcceptDragDropPayload( "ASSET" );
					if ( payload != null && payload->Data != null )
					{
						var b = (byte*)payload->Data;
						var droppedData = System.Text.Encoding.UTF8.GetString( b, payload->DataSize );
						assetPaths[i] = droppedData;
					}
					ImGui.EndDragDropTarget();
				}
			}

			// Align assets in a horizontal grid
			if ( (i + 1) % maxSlotsPerRow != 0 )
			{
				ImGui.SameLine();
			}
		}

		// End the grid
		ImGui.EndGroup();
	}

	public static void TextTruncate( string text, float maxWidth )
	{
		var textSize = ImGui.CalcTextSize( text );
		if ( textSize.X <= maxWidth )
		{
			ImGui.Text( text );
			return;
		}

		var truncated = text;
		var endIndex = text.Length - 1;

		while ( endIndex > 0 && ImGui.CalcTextSize( string.Concat( truncated.AsSpan( 0, endIndex ), "..." ) ).X > maxWidth )
		{
			endIndex--;
		}

		truncated = string.Concat( truncated.AsSpan( 0, endIndex ), "..." );
		ImGui.Text( truncated );
	}

	public static void EditFloatSlider( string label, ref float value, float min, float max )
	{
		ImGui.BeginGroup();

		// Calculate the necessary width for the element
		var sliderWidth = ImGui.GetContentRegionAvail().X;
		sliderWidth -= EditLabelWidth;

		// Display the main label
		ImGui.AlignTextToFramePadding();
		ImGui.TextDisabled( label );
		ImGui.SameLine();
		ImGui.SetCursorPosX( EditLabelWidth );

		// Display the slider float
		ImGui.PushItemWidth( sliderWidth );
		ImGui.SliderFloat( $"##{label}", ref value, min, max, $"{value} degrees", ImGuiSliderFlags.None );
		ImGui.PopItemWidth();

		ImGui.EndGroup();
	}

	public static void EditFloat( string label, ref float value, float speed, float min, float max )
	{
		ImGui.BeginGroup();

		var dragFloatWidth = ImGui.GetContentRegionAvail().X;
		dragFloatWidth -= EditLabelWidth;

		// Display the main label
		ImGui.AlignTextToFramePadding();
		ImGui.TextDisabled( label );
		ImGui.SameLine();
		ImGui.SetCursorPosX( EditLabelWidth );

		// Display the drag float
		ImGui.PushItemWidth( dragFloatWidth );
		ImGui.DragFloat( $"##{label}", ref value, speed, min, max );
		ImGui.PopItemWidth();

		ImGui.EndGroup();
	}

	public static void EditVector2( string label, ref Vector2 value, float speed, float min, float max )
	{
		ImGui.BeginGroup();

		var posX = ImGui.GetCursorPosX();
		var labelWidth = string.IsNullOrEmpty( label ) ? 0 : EditLabelWidth;
		var spacingCount = labelWidth > 0 ? 3 : 2;

		// Calculate the necessary width for each element
		var xLabelSize = ImGui.CalcTextSize( "X" );
		var yLabelSize = ImGui.CalcTextSize( "Y" );
		var dragFloatWidth = ImGui.GetContentRegionAvail().X - xLabelSize.X - yLabelSize.X - ImGui.GetStyle().ItemSpacing.X * spacingCount;
		dragFloatWidth -= labelWidth;

		// Display the main label
		ImGui.AlignTextToFramePadding();
		ImGui.TextDisabled( label );
		ImGui.SameLine();
		ImGui.SetCursorPosX( posX + labelWidth );

		// Display X label and drag float
		ImGui.AlignTextToFramePadding();
		ImGui.Text( "X" );
		ImGui.SameLine();
		//ImGui.PushItemWidth( dragFloatWidth * 0.5f );
		ImGui.SetNextItemWidth( dragFloatWidth * 0.5f );
		ImGui.DragFloat( $"##{label}X", ref value.X, speed, min, max );
		ImGui.SameLine();

		// Display Y label and drag float
		ImGui.AlignTextToFramePadding();
		ImGui.Text( "Y" );
		ImGui.SameLine();
		//ImGui.PushItemWidth( dragFloatWidth * 0.5f );
		ImGui.SetNextItemWidth( dragFloatWidth * 0.5f );
		ImGui.DragFloat( $"##{label}Y", ref value.Y, speed, min, max );
		ImGui.EndGroup();
	}

	public static void EditVector2Int( string label, ref Vector2 value, float speed, int min, int max )
	{
		ImGui.BeginGroup();

		var posX = ImGui.GetCursorPosX();
		var labelWidth = string.IsNullOrEmpty( label ) ? 0 : EditLabelWidth;
		var spacingCount = labelWidth > 0 ? 3 : 2;

		// Calculate the necessary width for each element
		var xLabelSize = ImGui.CalcTextSize( "X" );
		var yLabelSize = ImGui.CalcTextSize( "Y" );
		var dragIntWidth = ImGui.GetContentRegionAvail().X - xLabelSize.X - yLabelSize.X - ImGui.GetStyle().ItemSpacing.X * spacingCount;
		dragIntWidth -= labelWidth;

		// Display the main label
		ImGui.AlignTextToFramePadding();
		ImGui.TextDisabled( label );
		ImGui.SameLine();
		ImGui.SetCursorPosX( posX + labelWidth );

		// Display X label and drag int
		ImGui.AlignTextToFramePadding();
		ImGui.Text( "X" );
		ImGui.SameLine();
		ImGui.SetNextItemWidth( dragIntWidth * 0.5f );

		int xValue = (int)value.X;
		if ( ImGui.DragInt( $"##{label}X", ref xValue, speed, min, max ) )
		{
			value.X = xValue;
		}
		ImGui.SameLine();

		// Display Y label and drag int
		ImGui.AlignTextToFramePadding();
		ImGui.Text( "Y" );
		ImGui.SameLine();
		ImGui.SetNextItemWidth( dragIntWidth * 0.5f );

		int yValue = (int)value.Y;
		if ( ImGui.DragInt( $"##{label}Y", ref yValue, speed, min, max ) )
		{
			value.Y = yValue;
		}

		ImGui.EndGroup();
	}


	public static void EditVector3( string label, ref Vector3 value, float speed, float min, float max )
	{
		ImGui.BeginGroup();

		// Calculate the necessary width for each element
		var xLabelSize = ImGui.CalcTextSize( "X" );
		var yLabelSize = ImGui.CalcTextSize( "Y" );
		var zLabelSize = ImGui.CalcTextSize( "Z" );
		var dragFloatWidth = ImGui.GetContentRegionAvail().X - xLabelSize.X - yLabelSize.X - zLabelSize.X - ImGui.GetStyle().ItemSpacing.X * 5;
		dragFloatWidth -= EditLabelWidth;

		// Display the main label
		ImGui.AlignTextToFramePadding();
		ImGui.TextDisabled( label );
		ImGui.SameLine();
		ImGui.SetCursorPosX( EditLabelWidth );

		// Display X label and drag float
		ImGui.AlignTextToFramePadding();
		ImGui.Text( "X" );
		ImGui.SameLine();
		ImGui.PushItemWidth( dragFloatWidth / 3 );
		ImGui.DragFloat( $"##{label}X", ref value.X, speed, min, max );
		ImGui.PopItemWidth();
		ImGui.SameLine();

		// Display Y label and drag float
		ImGui.AlignTextToFramePadding();
		ImGui.Text( "Y" );
		ImGui.SameLine();
		ImGui.PushItemWidth( dragFloatWidth / 3 );
		ImGui.DragFloat( $"##{label}Y", ref value.Y, speed, min, max );
		ImGui.PopItemWidth();
		ImGui.SameLine();

		// Display Z label and drag float
		ImGui.AlignTextToFramePadding();
		ImGui.Text( "Z" );
		ImGui.SameLine();
		ImGui.PushItemWidth( dragFloatWidth / 3 );
		ImGui.DragFloat( $"##{label}Z", ref value.Z, speed, min, max );
		ImGui.PopItemWidth();

		ImGui.EndGroup();
	}


	public static unsafe bool IsFontValid( ImFontPtr font )
	{
		return (nint)font.NativePtr != IntPtr.Zero;
	}

	public static bool IconButton( string text, nint image, SVector2 buttonSize = default, float iconSize = default )
	{
		var noText = string.IsNullOrEmpty( text ) || text.StartsWith( "##" );
		var btnSize = ImGui.GetFontSize();
		var textSize = noText ? default : ImGui.CalcTextSize( text );
		var imgSize = iconSize > 0 ? new SVector2( iconSize, iconSize ) : new SVector2( btnSize, btnSize ); ;
		var padding = ImGui.GetStyle().FramePadding;
		var spacing = noText ? 0 : 6.0f;
		var minButtonSize = new SVector2( textSize.X + imgSize.X + padding.X * 2 + spacing, Math.Max( textSize.Y, imgSize.Y ) + padding.Y * 2 );

		buttonSize.X = MathF.Max( buttonSize.X, minButtonSize.X );
		buttonSize.Y = MathF.Max( buttonSize.Y, minButtonSize.Y );

		var pos = ImGui.GetCursorScreenPos();
		var result = ImGui.Button( $"##{text}", buttonSize );

		var drawList = ImGui.GetWindowDrawList();
		var imagePos = pos + new SVector2( (buttonSize.X - imgSize.X - textSize.X - spacing) / 2, (buttonSize.Y - imgSize.Y) / 2 );
		drawList.AddImage( image, imagePos, imagePos + imgSize );

		if ( !noText )
		{
			var textPos = imagePos + new SVector2( imgSize.X + spacing, 0 );
			drawList.AddText( textPos, ImGui.ColorConvertFloat4ToU32( ImGui.GetStyle().Colors[(int)ImGuiCol.Text] ), text );
		}

		return result;
	}

	public static bool IconButtonPrimary( string text, nint image, SVector2 buttonSize = default )
	{
		PushButtonPrimary();
		var result = IconButton( text, image, buttonSize );
		PopButtonPrimary();

		return result;
	}

	public static bool IconButtonDim( string text, nint image, SVector2 buttonSize = default )
	{
		PushButtonDim();
		var result = IconButton( text, image, buttonSize );
		PopButtonDim();

		return result;
	}

	public static uint GetColorMixin( SVector4 color, SVector4 mixin )
	{
		var r = color.X * mixin.X;
		var g = color.Y * mixin.Y;
		var b = color.Z * mixin.Z;
		var a = color.W * mixin.W;

		return ImGui.ColorConvertFloat4ToU32( new SVector4( r, g, b, a ) );
	}

	public static void PushButtonPrimary()
	{
		var colors = ImGui.GetStyle().Colors;
		var activeColor = colors[(int)ImGuiCol.ButtonActive];
		var hoverColor = GetColorMixin( activeColor, new ( 1.1f, 1.1f, 1.1f, 1.0f ) );

		ImGui.PushStyleColor( ImGuiCol.Button, activeColor );
		ImGui.PushStyleColor( ImGuiCol.ButtonHovered, hoverColor );
	}

	public static void PopButtonPrimary()
	{
		ImGui.PopStyleColor( 2 );
	}

	public static void PushButtonActive( bool isActive )
	{
		if ( isActive )
		{
			PushButtonPrimary();
		}
		else
		{
			PushButtonDim();
		}
	}

	public static void PopButtonActive( bool isActive )
	{
		if ( isActive )
		{
			PopButtonPrimary();
		}
		else
		{
			PopButtonDim();
		}
	}

	public static void PushButtonDim()
	{
		var colors = ImGui.GetStyle().Colors;
		var activeColor = colors[(int)ImGuiCol.Button];
		var hoverColor = GetColorMixin( activeColor, new ( 0.9f, 0.9f, 0.9f, 1.0f ) );

		ImGui.PushStyleColor( ImGuiCol.Button, activeColor );
		ImGui.PushStyleColor( ImGuiCol.ButtonHovered, hoverColor );
	}

	public static void PopButtonDim()
	{
		ImGui.PopStyleColor( 2 );
	}

	public static void PushButtonOutline()
	{
		var colors = ImGui.GetStyle().Colors;
		var activeColor = colors[(int)ImGuiCol.ButtonActive];
		var hoverColor = GetColorMixin( activeColor, new ( 1.1f, 1.1f, 1.1f, 1.0f ) );

		ImGui.PushStyleColor( ImGuiCol.Button, new SVector4( 0, 0, 0, 0 ) );
		ImGui.PushStyleColor( ImGuiCol.ButtonHovered, new SVector4( 1f, 1f, 1f, 0.12f ) );
	}

	public static void PopButtonOutline()
	{
		ImGui.PopStyleColor( 2 );
	}

	public static void DrawBorder( float thickness, ImGuiDir edge )
	{
		var drawList = ImGui.GetWindowDrawList();
		var pos = ImGui.GetWindowPos();
		var size = ImGui.GetWindowSize();

		switch ( edge )
		{
			case ImGuiDir.Up:
				drawList.AddRectFilled( pos, new SVector2( pos.X + size.X, pos.Y + thickness ), ImGui.GetColorU32( ImGuiCol.Border ) );
				break;
			case ImGuiDir.Down:
				drawList.AddRectFilled( new SVector2( pos.X, pos.Y + size.Y - thickness ), new SVector2( pos.X + size.X, pos.Y + size.Y ), ImGui.GetColorU32( ImGuiCol.Border ) );
				break;
			case ImGuiDir.Left:
				drawList.AddRectFilled( pos, new SVector2( pos.X + thickness, pos.Y + size.Y ), ImGui.GetColorU32( ImGuiCol.Border ) );
				break;
			case ImGuiDir.Right:
				drawList.AddRectFilled( new SVector2( pos.X + size.X - thickness, pos.Y ), new SVector2( pos.X + size.X, pos.Y + size.Y ), ImGui.GetColorU32( ImGuiCol.Border ) );
				break;
			default:
				throw new ArgumentException( "Invalid edge direction specified." );
		}
	}

	public static bool TextButtonGroup<T>(
		List<(string Label, T Value)> options,
		Func<T> getter,
		Action<T> setter,
		float height = 30,
		float spacing = 2 )
	{
		bool valueChanged = false;
		T currentValue = getter();
		var buttonSize = new SVector2( (ImGui.GetContentRegionAvail().X - (spacing * (options.Count - 1))) / options.Count, height );
		float fullWidth = buttonSize.X * options.Count + spacing * (options.Count - 1);
		var cursorPos = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();

		float rounding = 2f;

		// Draw background
		drawList.AddRectFilled(
			cursorPos,
			cursorPos + new SVector2( fullWidth, height ),
			ImGui.ColorConvertFloat4ToU32( new SVector4( 0.2f, 0.2f, 0.2f, 1.0f ) ),
			rounding );

		for ( int i = 0; i < options.Count; i++ )
		{
			var (label, value) = options[i];
			bool isSelected = EqualityComparer<T>.Default.Equals( currentValue, value );
			var buttonPos = cursorPos + new SVector2( (buttonSize.X + spacing) * i, 0 );

			ImGui.SetCursorScreenPos( buttonPos );

			ImGui.PushStyleColor( ImGuiCol.Button, new SVector4( 0.2f, 0.2f, 0.2f, 0.0f ) );
			ImGui.PushStyleColor( ImGuiCol.ButtonHovered, new SVector4( 0.3f, 0.3f, 0.3f, 1.0f ) );
			ImGui.PushStyleColor( ImGuiCol.ButtonActive, new SVector4( 0.4f, 0.4f, 0.4f, 1.0f ) );
			ImGui.PushStyleColor( ImGuiCol.Text, new SVector4( 1.0f, 1.0f, 1.0f, 1.0f ) );

			if ( ImGui.Button( $"##{label}", buttonSize ) )
			{
				setter( value );
				valueChanged = true;
			}

			ImGui.PopStyleColor( 4 );

			if ( isSelected )
			{
				// Draw selection indicator with matched rounding
				drawList.AddRectFilled(
					buttonPos,
					buttonPos + buttonSize,
					ImGui.ColorConvertFloat4ToU32( new SVector4( 0.4f, 0.4f, 0.4f, 1.0f ) ),
					rounding,
					ImDrawFlags.RoundCornersAll );
			}

			// Always draw text at the same position, regardless of selection state
			var textSize = ImGui.CalcTextSize( label );
			var textPos = buttonPos + (buttonSize - textSize) * 0.5f;
			drawList.AddText( textPos, ImGui.ColorConvertFloat4ToU32( new SVector4( 1.0f, 1.0f, 1.0f, 1.0f ) ), label );
		}

		ImGui.SetCursorScreenPos( cursorPos + new SVector2( 0, height + spacing ) );
		return valueChanged;
	}

	public static bool ImageButtonGroup<T>(
	List<(string Tooltip, IntPtr TextureId, T Value)> options,
	Func<T> getter,
	Action<T> setter,
	float height = 30,
	float spacing = 2 )
	{
		bool valueChanged = false;
		T currentValue = getter();
		var buttonSize = new SVector2( (ImGui.GetContentRegionAvail().X - (spacing * (options.Count - 1))) / options.Count, height );
		float fullWidth = buttonSize.X * options.Count + spacing * (options.Count - 1);
		var cursorPos = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		float rounding = 2f;

		// Draw background
		drawList.AddRectFilled(
			cursorPos,
			cursorPos + new SVector2( fullWidth, height ),
			ImGui.ColorConvertFloat4ToU32( new SVector4( 0.2f, 0.2f, 0.2f, 1.0f ) ),
			rounding );

		for ( int i = 0; i < options.Count; i++ )
		{
			var (tooltip, textureId, value) = options[i];
			bool isSelected = EqualityComparer<T>.Default.Equals( currentValue, value );
			var buttonPos = cursorPos + new SVector2( (buttonSize.X + spacing) * i, 0 );

			ImGui.SetCursorScreenPos( buttonPos );

			// Calculate image size to maintain aspect ratio
			SVector2 imageSize = CalculateAspectRatioFit( 32, 32, buttonSize.X - 4, buttonSize.Y - 4 );
			SVector2 imagePos = buttonPos + (buttonSize - imageSize) * 0.5f;

			// Draw selection indicator if selected
			if ( isSelected )
			{
				drawList.AddRectFilled(
					buttonPos,
					buttonPos + buttonSize,
					ImGui.ColorConvertFloat4ToU32( new SVector4( 0.4f, 0.4f, 0.4f, 1.0f ) ),
					rounding,
					ImDrawFlags.RoundCornersAll );
			}

			// Draw the image
			drawList.AddImage(
				textureId,
				imagePos,
				imagePos + imageSize );

			// Invisible button for interaction
			ImGui.PushStyleColor( ImGuiCol.Button, new SVector4( 0, 0, 0, 0 ) );
			ImGui.PushStyleColor( ImGuiCol.ButtonHovered, new SVector4( 0.3f, 0.3f, 0.3f, 0.5f ) );
			ImGui.PushStyleColor( ImGuiCol.ButtonActive, new SVector4( 0.4f, 0.4f, 0.4f, 0.5f ) );

			if ( ImGui.Button( $"##{tooltip}", buttonSize ) )
			{
				setter( value );
				valueChanged = true;
			}

			ImGui.PopStyleColor( 3 );

			if ( ImGui.IsItemHovered() )
			{
				ImGui.SetTooltip( tooltip );
			}
		}

		ImGui.SetCursorScreenPos( cursorPos + new SVector2( 0, height + spacing ) );
		return valueChanged;
	}

	private static SVector2 CalculateAspectRatioFit( float srcWidth, float srcHeight, float maxWidth, float maxHeight )
	{
		float ratio = Math.Min( maxWidth / srcWidth, maxHeight / srcHeight );
		return new SVector2( srcWidth * ratio, srcHeight * ratio );
	}

}
