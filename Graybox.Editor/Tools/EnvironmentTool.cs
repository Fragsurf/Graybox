
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Editor.Documents;
using Graybox.Editor.Widgets;
using Graybox.Graphics.Helpers;
using Graybox.Graphics.Immediate;
using Graybox.Interface;
using Graybox.Lightmapper;
using Graybox.Scenes;
using ImGuiNET;
using SkiaSharp;
using static Graybox.Interface.ButtonElement;

namespace Graybox.Editor.Tools;

public class EnvironmentTool : BaseTool
{

	public override string Name => "Environment Tool";
	public override string EditorIcon => "assets/icons/tool_lighting.png";

	Vector3 worldPos = default;
	Vector3 worldNormal = default;
	bool editTexels = false;
	Face hoveredFace;
	Light hoveredLight;

	public override void Render( Scene scene )
	{
		base.Render( scene );

		RenderSelection( scene );

		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;

		if ( editTexels )
		{
			if ( hoveredFace != null )
			{
				List<Face> facesToDraw = new();
				if ( !Input.ShiftModifier && !Input.ControlModifier )
				{
					facesToDraw.AddRange( hoveredFace.Parent?.Faces );
				}
				else
				{
					facesToDraw.Add( hoveredFace );
				}

				if ( Input.AltModifier )
				{
					GL.Color3( 0.5f, 1.0f, 1.0f );
				}
				else if ( Input.ControlModifier )
				{
					GL.Color3( 1.0f, 0.0f, 0.0f );
				}
				else
				{
					GL.Color3( 1.0f, 1.0f, 0.0f );
				}
				MapObjectRenderer.DrawWireframe( facesToDraw, true, false );
			}
			return;
		}

		foreach ( var obj in doc.Selection.GetSelectedObjects() )
		{
			if ( obj is not Light l )
				continue;

			MapObjectRenderer.DrawWireframe( l.CollectFaces(), false, false );
		}

		if ( hoveredLight != null )
		{
			GL.Color3( 1.0f, 1.0f, 1.0f );
			MapObjectRenderer.DrawWireframe( hoveredLight.CollectFaces(), true, false );
		}
		else
		{
			var lightIcon = scene.AssetSystem?.FindAsset<TextureAsset>( "icons/icon_pointlight" );
			if ( lightIcon != null )
			{
				var dir = (scene.Camera.Position - worldPos).Normalized();
				if ( scene.Camera.Orthographic )
				{
					dir = scene.Camera.Forward;
				}
				GraphicsHelper.DrawTexturedQuad( lightIcon.GraphicsID, worldPos, new( 32, 32, 32 ), dir, Color4.White );
			}
		}
	}

	public override void MouseMove( Scene scene, ref InputEvent e )
	{
		base.MouseMove( scene, ref e );

		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;

		worldPos = GetWorldPosition( scene, e.LocalMousePosition, out var plane );
		worldNormal = plane.Normal;
		worldPos += worldNormal * 16f;
		hoveredFace = null;
		hoveredLight = null;

		if ( editTexels )
		{
			hoveredFace = doc.Trace.FirstFace( scene, e.LocalMousePosition );
		}
		else
		{
			var tr = scene.Physics.Trace<Light>( scene.ScreenToRay( e.LocalMousePosition ) );
			if ( tr.Hit )
			{
				hoveredLight = tr.Object as Light;
			}
		}
	}

	public override void MouseDown( Scene scene, ref InputEvent e )
	{
		base.MouseDown( scene, ref e );

		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;

		if ( editTexels )
		{
			if ( hoveredFace != null )
			{
				if ( e.Button == MouseButton.Left )
				{
					if ( e.Control )
					{
						hoveredFace.DisableInLightmap = !hoveredFace.DisableInLightmap;
					}
					else if ( e.Alt )
					{
						hoveredFace.DecreaseTexelSize();
					}
					else
					{
						hoveredFace.IncreaseTexelSize();
					}

					if ( !e.Shift && !e.Control )
					{
						foreach ( var face in hoveredFace?.Parent?.Faces ?? Enumerable.Empty<Face>() )
						{
							face.TexelSize = hoveredFace.TexelSize;
						}
					}

					e.Handled = true;
				}
			}
			return;
		}
		else
		{
			if ( hoveredLight != null )
			{
				doc.Selection.Clear();
				doc.Selection.Select( hoveredLight );
			}
			else
			{
				CreateLight( worldPos );
			}
		}
	}

	public override void Paint( Scene scene, UIElementPaintEvent e )
	{
		base.Paint( scene, e );

		if ( editTexels )
		{
			if ( hoveredFace != null )
			{
				var screenPos = scene.WorldToScreen( worldPos );
				using var paint = new SKPaint();
				paint.TextSize = 15;

				var msg = $"Texel Size: {hoveredFace.TexelSize}";

				paint.Color = SKColors.Black;
				e.Canvas.DrawText( msg, screenPos.X + 1, screenPos.Y + 1, paint );
				paint.Color = SKColors.Yellow;
				e.Canvas.DrawText( msg, screenPos.X, screenPos.Y, paint );
			}

			return;
		}
		else
		{
			if ( hoveredLight == null )
			{
				var screenPos = scene.WorldToScreen( worldPos );
				using var paint = new SKPaint();
				paint.TextSize = 15;

				paint.Color = SKColors.Black;
				e.Canvas.DrawText( $"Create Light", screenPos.X + 1, screenPos.Y + 1, paint );
				paint.Color = SKColors.Yellow;
				e.Canvas.DrawText( $"Create Light", screenPos.X, screenPos.Y, paint );
			}
		}
	}

	void CreateLight( Vector3 position )
	{
		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;
		var lmData = doc.Map.LightmapData;
		if ( lmData == null ) return;

		var lightInfo = new LightInfo()
		{
			Color = new Color4( 1.0f, 1.0f, 1.0f, 1.0f ),
			Direction = Vector3.UnitX,
			Intensity = 1.0f,
			Position = position,
			Range = 3250f,
			ShadowStrength = 1.0f,
			Type = LightTypes.Point
		};

		var lightObj = new Light( doc.Map.IDGenerator.GetNextObjectID() );
		lightObj.LightInfo = lightInfo;
		lightObj.UpdateBoundingBox();

		var create = new Create( doc.Map.WorldSpawn.ID, lightObj );
		var select = new ChangeSelection( new[] { lightObj }, doc.Selection.GetSelectedObjects() );
		var action = new ActionCollection( create, select );
		Document.PerformAction( "Create Light: " + lightObj.Name, action );
	}

	public override void UpdateWidget()
	{
		base.UpdateWidget();

		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;
		var lmData = doc.Map.LightmapData;
		if ( lmData == null ) return;
		var baker = lmData?.Baker;
		if ( baker == null ) return;

		if ( baker.Progress < 1.0f && baker.Progress > 0f )
		{
			EditorWindow.Instance.SetBusyState( "Lightmapper Busy", "Baking Lights...", baker.Progress, baker.Cancel );
			return;
		}

		var scene = SceneWidget.All?.FirstOrDefault( x => x.Config?.Orthographic == false )?.Scene;
		if ( scene == null )
		{
			ImGui.TextWrapped( "No 3D viewport found.  Switch one of your scenes to 3D view to use this tool." );
			return;
		}

		ImGuiEx.Header( "Environment Settings" );

		var env = doc.Map.EnvironmentData;
		ImObjectEditor.EditObject( ref env );

		ImGuiEx.Header( "Lightmap" );

		if ( ImGui.Button( "Bake Lightmap" ) )
		{
			Bake( doc.Map, scene );
		}

		ImGui.SameLine( 0, 8 );

		if ( ImGui.Button( "Clear Baked Data" ) )
		{
			lmData.Clear();
		}

		ImGui.Spacing();

		ImGui.SeparatorText( "Texels" );

		ImGui.TextWrapped( "Texels are sample density per face.  Smaller texels means better quality but slower bake times.  Surfaces that are large or unimportant can have larger texels to reduce sampling and improve bake times." );
		ImGui.Spacing();

		var visTexels = scene.DebugVisualizeTexels;

		if ( ImGui.Checkbox( "Edit Texels", ref editTexels ) )
		{
		}

		scene.DebugVisualizeTexels = editTexels;

		ImGui.SameLine( 0, 8 );

		if ( ImGui.Button( "Auto Texel" ) )
		{
			RecalculateFaceTexels();
		}

		ImGui.Spacing();

		if ( editTexels )
		{
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();
			ImGui.TextDisabled( "Click on an object to adjust texel density.\nHold SHIFT to adjust only the hovered face.\nHold ALT to adjust smaller.\nHold CTRL to enable or disable in baking" );
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		if ( ImGui.CollapsingHeader( "View Lightmap" ) )
		{
			DisplayLightmap( scene.Lightmaps );
		}
	}

	async void Bake( Map map, Scene scene )
	{
		var config = new LightmapConfig();
		config.Scene = scene;
		config.Width = 4096;
		config.Height = 4096;
		config.BlurStrength = 1.0f;
		config.AmbientColor = scene.Environment.AmbientColor;
		config.Solids = scene.Objects.OfType<Solid>().Where( x => x != null && !x.IsTrigger() );
		config.Lights = scene.Objects.OfType<Light>().Select( x => x.LightInfo );

		var result = await map.LightmapData.Baker.BakeAsync( config );

		if ( result.Success )
		{
			map.LightmapData.Set( result.Lightmaps );

			foreach ( var s in config.Solids )
			{
				s.IncrementUpdateCounter();
			}
		}
	}

	void RecalculateFaceTexels()
	{
		var doc = DocumentManager.CurrentDocument;
		if ( doc == null ) return;
		var map = doc.Map;
		if ( map == null ) return;

		foreach ( var solid in map.WorldSpawn.GetAllDescendants<Solid>() )
		{
			solid.Faces.ForEach( x => x.RecalculateTexelSize() );
		}
	}

	private void DisplayLightmap( LightmapData lmData )
	{
		var lm = lmData?.Lightmaps?.FirstOrDefault();
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
					DrawYellowRectangle( lm, face, sz, pos );
				}
			}
		}
	}

	private void DrawYellowRectangle( Lightmap lm, Face face, SVector2 sz, SVector2 pos )
	{
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

}
