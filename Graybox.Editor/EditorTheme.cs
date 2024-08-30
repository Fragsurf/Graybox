﻿
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Graybox.Editor;

internal static class EditorTheme
{

	public static Vector4 textColor = new Vector4( 1.0f, 1.0f, 1.0f, 1.0f );
	public static Vector4 disabledTextColor = new Vector4( 0.7f, 0.7f, 0.7f, 1.0f );
	public static Vector4 backgroundColor = new Vector4( 0.15f, 0.15f, 0.15f, 1.0f );
	public static Vector4 elementBackground = new Vector4( 0.2f, 0.2f, 0.2f, 1.0f );
	public static Vector4 hoverBackgroundColor = new Vector4( 0.25f, 0.25f, 0.25f, 1.0f );
	public static Vector4 activeBackgroundColor = new Vector4( 0.30f, 0.30f, 0.30f, 1.0f );
	public static Vector4 accentColor = new Vector4( 0.20f, 0.40f, 0.80f, 1.0f ); // Muted blue
	public static Vector4 accentHoverColor = new Vector4( 0.30f, 0.50f, 0.85f, 1.0f ); // Lighter muted blue
	public static Vector4 highlightColor = new Vector4( 1.0f, 1.0f, 1.0f, 0.9f );
	public static Vector4 subtleColor = new Vector4( 0.1f, 0.1f, 0.1f, 0.2f );
	public static Vector4 mediumDarkBackground = new Vector4( 0.12f, 0.12f, 0.12f, 1f );
	public static Vector4 darkBackground = new Vector4( 0.05f, 0.05f, 0.05f, 1f );

	public static void SetupImGuiStyle()
	{
		var style = ImGuiNET.ImGui.GetStyle();

		style.Alpha = 1.0f;
		style.DisabledAlpha = 0.5f;
		style.WindowPadding = new Vector2( 8.0f, 8.0f );
		style.WindowRounding = 0.0f;
		style.WindowBorderSize = 1.0f;
		style.WindowMinSize = new Vector2( 32.0f, 32.0f );
		style.WindowTitleAlign = new Vector2( 0.0f, 0.5f );
		style.WindowMenuButtonPosition = ImGuiDir.Right;
		style.ChildRounding = 0.0f;
		style.ChildBorderSize = 1.0f;
		style.PopupRounding = 0.0f;
		style.PopupBorderSize = 1.0f;
		style.FramePadding = new Vector2( 8.4f, 6.0f );
		style.FrameRounding = 2.0f;
		style.FrameBorderSize = 0.0f;
		style.ItemSpacing = new Vector2( 8.0f, 4.0f );
		style.ItemInnerSpacing = new Vector2( 4.0f, 4.0f );
		style.CellPadding = new Vector2( 4.2f, 2.0f );
		style.IndentSpacing = 20.0f;
		style.ColumnsMinSpacing = 6.0f;
		style.ScrollbarSize = 20.0f;
		style.ScrollbarRounding = 2.3f;
		style.GrabMinSize = 10.0f;
		style.GrabRounding = 0.4f;
		style.TabRounding = 1.5f;
		style.TabBorderSize = 0.0f;
		style.TabMinWidthForCloseButton = 0.0f;
		style.ColorButtonPosition = ImGuiDir.Right;
		style.ButtonTextAlign = new Vector2( 0.5f, 0.5f );
		style.SelectableTextAlign = new Vector2( 0.0f, 0.0f );

		// Apply colors
		style.Colors[(int)ImGuiCol.Text] = textColor;
		style.Colors[(int)ImGuiCol.TextDisabled] = disabledTextColor;
		style.Colors[(int)ImGuiCol.WindowBg] = backgroundColor;
		style.Colors[(int)ImGuiCol.ChildBg] = new Vector4( 0.0f, 0.0f, 0.0f, 0.0f ); // Fully transparent
		style.Colors[(int)ImGuiCol.PopupBg] = darkBackground;
		style.Colors[(int)ImGuiCol.Border] = new Vector4( 0.25f, 0.25f, 0.25f, 0.5f );
		style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4( 0.0f, 0.0f, 0.0f, 0.0f ); // Fully transparent
		style.Colors[(int)ImGuiCol.FrameBg] = elementBackground;
		style.Colors[(int)ImGuiCol.FrameBgHovered] = hoverBackgroundColor;
		style.Colors[(int)ImGuiCol.FrameBgActive] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.TitleBg] = mediumDarkBackground;
		style.Colors[(int)ImGuiCol.TitleBgActive] = mediumDarkBackground;
		style.Colors[(int)ImGuiCol.TitleBgCollapsed] = mediumDarkBackground;
		style.Colors[(int)ImGuiCol.MenuBarBg] = backgroundColor;
		style.Colors[(int)ImGuiCol.ScrollbarBg] = backgroundColor;
		style.Colors[(int)ImGuiCol.ScrollbarGrab] = hoverBackgroundColor;
		style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = accentColor;
		style.Colors[(int)ImGuiCol.CheckMark] = accentColor;
		style.Colors[(int)ImGuiCol.SliderGrab] = accentHoverColor;
		style.Colors[(int)ImGuiCol.SliderGrabActive] = accentColor;
		style.Colors[(int)ImGuiCol.Button] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.ButtonHovered] = accentHoverColor;
		style.Colors[(int)ImGuiCol.ButtonActive] = accentHoverColor;
		style.Colors[(int)ImGuiCol.Header] = elementBackground;
		style.Colors[(int)ImGuiCol.HeaderHovered] = hoverBackgroundColor;
		style.Colors[(int)ImGuiCol.HeaderActive] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.Separator] = new Vector4( 0.35f, 0.35f, 0.35f, 0.5f );
		style.Colors[(int)ImGuiCol.SeparatorHovered] = accentHoverColor;
		style.Colors[(int)ImGuiCol.SeparatorActive] = accentColor;
		style.Colors[(int)ImGuiCol.ResizeGrip] = hoverBackgroundColor;
		style.Colors[(int)ImGuiCol.ResizeGripHovered] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.ResizeGripActive] = accentHoverColor;
		style.Colors[(int)ImGuiCol.Tab] = backgroundColor;
		style.Colors[(int)ImGuiCol.TabHovered] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.TabActive] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.TabUnfocused] = backgroundColor;
		style.Colors[(int)ImGuiCol.TabUnfocusedActive] = activeBackgroundColor;
		style.Colors[(int)ImGuiCol.PlotLines] = new Vector4( 0.61f, 0.61f, 0.61f, 1.0f );
		style.Colors[(int)ImGuiCol.PlotLinesHovered] = highlightColor;
		style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4( 0.9f, 0.7f, 0.0f, 1.0f );
		style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4( 1.0f, 0.85f, 0.35f, 1.0f );
		style.Colors[(int)ImGuiCol.TableHeaderBg] = elementBackground;
		style.Colors[(int)ImGuiCol.TableBorderStrong] = hoverBackgroundColor;
		style.Colors[(int)ImGuiCol.TableBorderLight] = subtleColor;
		style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4( 0.0f, 0.0f, 0.0f, 0.0f ); // Fully transparent
		style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4( 1.0f, 1.0f, 1.0f, 0.05f );
		style.Colors[(int)ImGuiCol.TextSelectedBg] = accentColor;
		style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4( 1.0f, 1.0f, 0.0f, 0.9f );
		style.Colors[(int)ImGuiCol.NavHighlight] = accentColor;
		style.Colors[(int)ImGuiCol.NavWindowingHighlight] = highlightColor;
		style.Colors[(int)ImGuiCol.NavWindowingDimBg] = subtleColor;
		style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4( 0.8f, 0.8f, 0.8f, 0.35f );
	}


	public static void ClassicSteamStyle()
	{
		// Classic Steam stylemetasprite from ImThemes
		var style = ImGuiNET.ImGui.GetStyle();

		style.Alpha = 1.0f;
		style.DisabledAlpha = 0.6000000238418579f;
		style.WindowPadding = new Vector2( 8.0f, 8.0f );
		style.WindowRounding = 0.0f;
		style.WindowBorderSize = 1.0f;
		style.WindowMinSize = new Vector2( 32.0f, 32.0f );
		style.WindowTitleAlign = new Vector2( 0.0f, 0.5f );
		style.WindowMenuButtonPosition = ImGuiDir.Left;
		style.ChildRounding = 0.0f;
		style.ChildBorderSize = 1.0f;
		style.PopupRounding = 0.0f;
		style.PopupBorderSize = 1.0f;
		style.FramePadding = new Vector2( 4.0f, 3.0f );
		style.FrameRounding = 0.0f;
		style.FrameBorderSize = 1.0f;
		style.ItemSpacing = new Vector2( 8.0f, 4.0f );
		style.ItemInnerSpacing = new Vector2( 4.0f, 4.0f );
		style.CellPadding = new Vector2( 4.0f, 2.0f );
		style.IndentSpacing = 21.0f;
		style.ColumnsMinSpacing = 6.0f;
		style.ScrollbarSize = 14.0f;
		style.ScrollbarRounding = 0.0f;
		style.GrabMinSize = 10.0f;
		style.GrabRounding = 0.0f;
		style.TabRounding = 0.0f;
		style.TabBorderSize = 0.0f;
		style.TabMinWidthForCloseButton = 0.0f;
		style.ColorButtonPosition = ImGuiDir.Right;
		style.ButtonTextAlign = new Vector2( 0.5f, 0.5f );
		style.SelectableTextAlign = new Vector2( 0.0f, 0.0f );

		style.Colors[(int)ImGuiCol.Text] = new Vector4( 1.0f, 1.0f, 1.0f, 1.0f );
		style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4( 0.4980392158031464f, 0.4980392158031464f, 0.4980392158031464f, 1.0f );
		style.Colors[(int)ImGuiCol.WindowBg] = new Vector4( 0.2862745225429535f, 0.3372549116611481f, 0.2588235437870026f, 1.0f );
		style.Colors[(int)ImGuiCol.ChildBg] = new Vector4( 0.2862745225429535f, 0.3372549116611481f, 0.2588235437870026f, 1.0f );
		style.Colors[(int)ImGuiCol.PopupBg] = new Vector4( 0.239215686917305f, 0.2666666805744171f, 0.2000000029802322f, 1.0f );
		style.Colors[(int)ImGuiCol.Border] = new Vector4( 0.5372549295425415f, 0.5686274766921997f, 0.5098039507865906f, 0.5f );
		style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4( 0.1372549086809158f, 0.1568627506494522f, 0.1098039224743843f, 0.5199999809265137f );
		style.Colors[(int)ImGuiCol.FrameBg] = new Vector4( 0.239215686917305f, 0.2666666805744171f, 0.2000000029802322f, 1.0f );
		style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4( 0.2666666805744171f, 0.2980392277240753f, 0.2274509817361832f, 1.0f );
		style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4( 0.2980392277240753f, 0.3372549116611481f, 0.2588235437870026f, 1.0f );
		style.Colors[(int)ImGuiCol.TitleBg] = new Vector4( 0.239215686917305f, 0.2666666805744171f, 0.2000000029802322f, 1.0f );
		style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4( 0.2862745225429535f, 0.3372549116611481f, 0.2588235437870026f, 1.0f );
		style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4( 0.0f, 0.0f, 0.0f, 0.5099999904632568f );
		style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4( 0.239215686917305f, 0.2666666805744171f, 0.2000000029802322f, 1.0f );
		style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4( 0.3490196168422699f, 0.4196078479290009f, 0.3098039329051971f, 1.0f );
		style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4( 0.2784313857555389f, 0.3176470696926117f, 0.239215686917305f, 1.0f );
		style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4( 0.2470588237047195f, 0.2980392277240753f, 0.2196078449487686f, 1.0f );
		style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4( 0.2274509817361832f, 0.2666666805744171f, 0.2078431397676468f, 1.0f );
		style.Colors[(int)ImGuiCol.CheckMark] = new Vector4( 0.5882353186607361f, 0.5372549295425415f, 0.1764705926179886f, 1.0f );
		style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4( 0.3490196168422699f, 0.4196078479290009f, 0.3098039329051971f, 1.0f );
		style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4( 0.5372549295425415f, 0.5686274766921997f, 0.5098039507865906f, 0.5f );
		style.Colors[(int)ImGuiCol.Button] = new Vector4( 0.2862745225429535f, 0.3372549116611481f, 0.2588235437870026f, 0.4000000059604645f );
		style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4( 0.3490196168422699f, 0.4196078479290009f, 0.3098039329051971f, 1.0f );
		style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4( 0.5372549295425415f, 0.5686274766921997f, 0.5098039507865906f, 0.5f );
		style.Colors[(int)ImGuiCol.Header] = new Vector4( 0.3490196168422699f, 0.4196078479290009f, 0.3098039329051971f, 1.0f );
		style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4( 0.3490196168422699f, 0.4196078479290009f, 0.3098039329051971f, 0.6000000238418579f );
		style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4( 0.5372549295425415f, 0.5686274766921997f, 0.5098039507865906f, 0.5f );
		style.Colors[(int)ImGuiCol.Separator] = new Vector4( 0.1372549086809158f, 0.1568627506494522f, 0.1098039224743843f, 1.0f );
		style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4( 0.5372549295425415f, 0.5686274766921997f, 0.5098039507865906f, 1.0f );
		style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4( 0.5882353186607361f, 0.5372549295425415f, 0.1764705926179886f, 1.0f );
		style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4( 0.1882352977991104f, 0.2274509817361832f, 0.1764705926179886f, 0.0f );
		style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4( 0.5372549295425415f, 0.5686274766921997f, 0.5098039507865906f, 1.0f );
		style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4( 0.5882353186607361f, 0.5372549295425415f, 0.1764705926179886f, 1.0f );
		style.Colors[(int)ImGuiCol.Tab] = new Vector4( 0.3490196168422699f, 0.4196078479290009f, 0.3098039329051971f, 1.0f );
		style.Colors[(int)ImGuiCol.TabHovered] = new Vector4( 0.5372549295425415f, 0.5686274766921997f, 0.5098039507865906f, 0.7799999713897705f );
		style.Colors[(int)ImGuiCol.TabActive] = new Vector4( 0.5882353186607361f, 0.5372549295425415f, 0.1764705926179886f, 1.0f );
		style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4( 0.239215686917305f, 0.2666666805744171f, 0.2000000029802322f, 1.0f );
		style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4( 0.3490196168422699f, 0.4196078479290009f, 0.3098039329051971f, 1.0f );
		style.Colors[(int)ImGuiCol.PlotLines] = new Vector4( 0.6078431606292725f, 0.6078431606292725f, 0.6078431606292725f, 1.0f );
		style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4( 0.5882353186607361f, 0.5372549295425415f, 0.1764705926179886f, 1.0f );
		style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4( 1.0f, 0.7764706015586853f, 0.2784313857555389f, 1.0f );
		style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4( 1.0f, 0.6000000238418579f, 0.0f, 1.0f );
		style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4( 0.1882352977991104f, 0.1882352977991104f, 0.2000000029802322f, 1.0f );
		style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4( 0.3098039329051971f, 0.3098039329051971f, 0.3490196168422699f, 1.0f );
		style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4( 0.2274509817361832f, 0.2274509817361832f, 0.2470588237047195f, 1.0f );
		style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4( 0.0f, 0.0f, 0.0f, 0.0f );
		style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4( 1.0f, 1.0f, 1.0f, 0.05999999865889549f );
		style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4( 0.5882353186607361f, 0.5372549295425415f, 0.1764705926179886f, 1.0f );
		style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4( 0.729411780834198f, 0.6666666865348816f, 0.239215686917305f, 1.0f );
		style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4( 0.5882353186607361f, 0.5372549295425415f, 0.1764705926179886f, 1.0f );
		style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4( 1.0f, 1.0f, 1.0f, 0.699999988079071f );
		style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4( 0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.2000000029802322f );
		style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4( 0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.3499999940395355f );
	}

}
