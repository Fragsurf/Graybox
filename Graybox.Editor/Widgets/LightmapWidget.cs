
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;
using Graybox.Lightmapper;
using ImGuiNET;
using System.Collections.Concurrent;

namespace Graybox.Editor.Widgets;

internal class LightmapWidget : BaseWidget
{

	public override string Title => "Lightmap";

	SceneWidget lastActiveScene;
	LightmapConfig lightmapConfig = new();
	ConcurrentQueue<LightmapResult> completedBakes = new();

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );

		if ( completedBakes.TryDequeue( out var result ) )
		{
			if ( result.Success && result.Context is Document doc )
			{
				doc.Map.LightmapData.Set( result.Lightmaps );
				result.Scene?.RefreshAllObjects();
			}
		}

		lastActiveScene = SceneWidget.All.FirstOrDefault( x => x.IsFocused ) ?? lastActiveScene;

		var baker = lastActiveScene?.Scene?.LightBaker;
		if ( baker == null )
		{
			ImGui.Text( "Nothing is open" );
			return;
		}

		if ( ImGui.BeginChild( "Lightmap Settings" ) )
		{
			bool useAo = false;

			ImGui.SeparatorText( "Lightmap Settings" );

			int displayWidth = lightmapConfig.Width;

			if ( ImGui.Button( "<<" ) )
			{
				lightmapConfig.Width = Math.Max( 512, lightmapConfig.Width / 2 );
				lightmapConfig.Height = lightmapConfig.Width;
			}

			ImGui.SameLine();
			ImGui.InputInt( "##width", ref displayWidth, 0, 0, ImGuiInputTextFlags.ReadOnly );
			ImGui.SameLine();

			if ( ImGui.Button( ">>" ) )
			{
				lightmapConfig.Width = Math.Min( 4096, lightmapConfig.Width * 2 );
				lightmapConfig.Height = lightmapConfig.Width;
			}

			ImGui.SameLine();
			ImGui.Text( "Resolution" );

			// Ensure width stays within bounds
			lightmapConfig.Width = Math.Clamp( lightmapConfig.Width, 512, 4096 );

			ImGui.Checkbox( "Enable AO", ref useAo );
			float blurStrength = lightmapConfig.BlurStrength;
			if ( ImGui.SliderFloat( "Blur Strength", ref blurStrength, 0.0f, 5.0f ) )
			{
				lightmapConfig.BlurStrength = MathHelper.Clamp( blurStrength, 0, 5f );
			}

			ImGui.SeparatorText( string.Empty );

			if ( baker.Progress == 1.0f )
				ImGui.PushStyleColor( ImGuiCol.PlotHistogram, new SVector4( 0.0f, 1.0f, 0.0f, 1.0f ) ); // RGB + Alpha
			else if ( baker.Progress == -1.0f )
				ImGui.PushStyleColor( ImGuiCol.PlotHistogram, new SVector4( 1.0f, 0.0f, 0.0f, 1.0f ) );
			else
				ImGui.PushStyleColor( ImGuiCol.PlotHistogram, new SVector4( 1.0f, 0.85f, 0.0f, 1.0f ) ); // Orange color

			ImGui.ProgressBar( baker.Progress, new( ImGui.GetContentRegionAvail().X, 24 ), $"{(int)(baker.Progress * 100)}%" );

			ImGui.PopStyleColor();

			if ( baker.Status == LightmapBaker.BakeStatus.Baking )
			{
				if ( ImGui.Button( "Cancel" ) )
				{
					baker.Cancel();
				}
			}
			else
			{
				if ( ImGui.Button( "Bake" ) )
				{
					BeginBake();
				}

				ImGui.SameLine();

				if ( ImGui.Button( "Clear" ) )
				{
					lastActiveScene.Scene.Lightmaps.Clear();
				}

				ImGui.SameLine();

				if ( ImGui.Button( "Calculate Texels" ) )
				{
					RecalculateFaceTexels();
				}
			}

			DisplayLightmap();

			ImGui.EndChild();
		}
	}

	async void BeginBake()
	{

		var scene = lastActiveScene?.Scene;
		if ( scene == null ) return;

		var baker = scene.LightBaker;
		if ( baker == null ) return;

		var document = DocumentManager.CurrentDocument;
		if ( document == null ) return;

		var lights = scene.Objects.OfType<Light>().Select( x => x.LightInfo );
		var targetObjects = scene.Objects.OfType<Solid>();

		lightmapConfig.Scene = scene;
		lightmapConfig.Solids = targetObjects;
		lightmapConfig.Lights = lights;

		var lightmapResult = await baker.BakeAsync( lightmapConfig );

		lightmapResult.Context = document;

		completedBakes.Enqueue( lightmapResult );
	}

	private void DisplayLightmap()
	{
		var lm = lastActiveScene?.Scene?.Lightmaps?.Lightmaps?.FirstOrDefault();
		if ( lm == null ) return;

		var lmWidth = lm.Width;
		var lmHeight = lm.Height;

		ImGui.SeparatorText( $"Size: {lmWidth}x{lmHeight}" );

		var sz = ImGui.GetContentRegionAvail();
		sz.X = MathF.Min( sz.X, lmWidth );
		sz.Y = MathF.Min( sz.Y, lmHeight );
		var pos = ImGui.GetCursorScreenPos();
		var maxWidth = sz.X;
		var maxHeight = sz.Y;
		var scaleRatio = Math.Min( maxWidth / lmWidth, maxHeight / lmHeight );
		var scaledWidth = lmWidth * scaleRatio;
		var scaledHeight = lmHeight * scaleRatio;

		ImGui.Image( lm.GetGraphicsId(), new SVector2( scaledWidth, scaledHeight ) );

		if ( DocumentManager.CurrentDocument?.Selection != null )
		{
			var solidSelection = DocumentManager.CurrentDocument.Selection.GetSelectedObjects().OfType<Solid>();
			foreach ( var solid in solidSelection )
			{
				foreach ( var face in solid.Faces )
				{
					DrawYellowRectangle( face, sz, pos );
				}
			}
		}
	}

	private void DrawYellowRectangle( Face face, SVector2 sz, SVector2 pos )
	{
		var lm = lastActiveScene?.Scene?.Lightmaps?.Lightmaps?.FirstOrDefault();
		if ( lm == null ) return;

		var lmWidth = lm.Width;
		var lmHeight = lm.Height;

		var maxWidth = sz.X;
		var maxHeight = sz.Y;
		var scaleRatio = Math.Min( maxWidth / lmWidth, maxHeight / lmHeight );
		var scaledWidth = lmWidth * scaleRatio;
		var scaledHeight = lmHeight * scaleRatio;

		var imagePosX = pos.X;
		var imagePosY = pos.Y;

		float minX = face.Vertices.Min( v => v.LightmapU );
		float maxX = face.Vertices.Max( v => v.LightmapU );
		float minY = face.Vertices.Min( v => v.LightmapV );
		float maxY = face.Vertices.Max( v => v.LightmapV );

		float screenMinX = imagePosX + minX * scaledWidth;
		float screenMaxX = imagePosX + maxX * scaledWidth;
		float screenMinY = imagePosY + minY * scaledHeight;
		float screenMaxY = imagePosY + maxY * scaledHeight;

		var color1 = new SVector4( 1, 1, 0, 1 );
		var color2 = new SVector4( 0, 0, 0, 1 );
		var thickness = 1.0f;

		ImGui.GetWindowDrawList().AddRect( new SVector2( screenMinX - 1, screenMinY - 1 ), new SVector2( screenMaxX + 1, screenMaxY + 1 ), ImGui.ColorConvertFloat4ToU32( color1 ), 0.0f, ImDrawFlags.None, thickness );
		ImGui.GetWindowDrawList().AddRect( new SVector2( screenMinX - 2, screenMinY - 2 ), new SVector2( screenMaxX + 2, screenMaxY + 2 ), ImGui.ColorConvertFloat4ToU32( color2 ), 0.0f, ImDrawFlags.None, thickness );
	}

	private void RecalculateFaceTexels()
	{
		if ( lastActiveScene?.Scene == null ) return;

		foreach ( var obj in lastActiveScene.Scene.Objects )
		{
			if ( obj is not Solid s ) continue;
			foreach ( Face face in s.Faces )
			{
				face.RecalculateTexelSize();
			}
		}
	}

}
