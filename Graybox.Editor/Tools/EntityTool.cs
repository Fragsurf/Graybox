
using Graybox.DataStructures.GameData;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Editor.Brushes;
using Graybox.Graphics.Helpers;
using Graybox.Interface;
using Graybox.Scenes;
using ImGuiNET;
using SkiaSharp;

namespace Graybox.Editor.Tools;

public class EntityTool : BaseTool
{

	public override string Name => "Entity Tool";
	public override string EditorIcon => "assets/icons/tool_entity.png";

	GameDataObject selectedEntity = null;
	Vector3 worldPos = default;
	Vector3 worldNormal = default;

	public override void Render( Scene scene )
	{
		base.Render( scene );

		RenderSelection( scene );

		if ( selectedEntity != null )
		{
			var lightIcon = scene.AssetSystem?.FindAsset<TextureAsset>( "icons/icon_entity" );
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

		worldPos = GetWorldPosition( scene, e.LocalMousePosition, out var plane );
		worldNormal = plane.Normal;
		worldPos += worldNormal * 16;
	}

	public override void MouseDown( Scene scene, ref InputEvent e )
	{
		base.MouseDown( scene, ref e );

		if ( selectedEntity == null ) return;
		if ( Document?.Map?.IDGenerator == null ) return;

		CreateEntity( worldPos, selectedEntity );
	}

	public override void Paint( Scene scene, UIElementPaintEvent e )
	{
		base.Paint( scene, e );

		var screenPos = scene.WorldToScreen( worldPos + Vector3.UnitZ * 24 );
		using var paint = new SKPaint();
		paint.TextSize = 15;

		if ( selectedEntity != null )
		{
			var msg = selectedEntity?.Name ?? string.Empty;
			var width = paint.MeasureText( msg );
			var posx = screenPos.X - width / 2;

			paint.FakeBoldText = true;
			paint.Color = SKColors.Black;
			e.Canvas.DrawText( msg, posx + 1, screenPos.Y + 1, paint );
			paint.Color = SKColors.Yellow;
			e.Canvas.DrawText( msg, posx, screenPos.Y, paint );
		}
		else
		{
			paint.Color = SKColors.Black;
			e.Canvas.DrawText( $"No entity selected", screenPos.X + 1, screenPos.Y + 1, paint );
			paint.Color = SKColors.Yellow;
			e.Canvas.DrawText( $"No entity selected", screenPos.X, screenPos.Y, paint );
		}
	}

	public override void UpdateWidget()
	{
		base.UpdateWidget();

		ImGuiEx.Header( "Entity Tool" );

		if ( Document == null ) return;

		var entGroups = Document.GameData.Classes.GroupBy( x => x.ClassType ).OrderBy( x => x.Key );
		foreach ( var group in entGroups )
		{
			if ( ImGui.CollapsingHeader( group.Key.ToString() ) )
			{
				ImGui.PushStyleColor( ImGuiCol.Header, Graybox.Editor.EditorTheme.accentColor );
				ImGui.PushStyleColor( ImGuiCol.HeaderHovered, Graybox.Editor.EditorTheme.accentHoverColor );
				ImGui.Indent( 20 );
				foreach ( var entClass in group )
				{
					if ( ImGui.Selectable( entClass.Name, selectedEntity == entClass ) )
					{
						selectedEntity = entClass;
					}
				}
				ImGui.Indent( -20 );
				ImGui.PopStyleColor( 2 );
			}
			ImGui.Spacing();
		}
	}

	private void CreateEntity( Vector3 origin, GameDataObject gd )
	{
		var idgen = Document.Map.IDGenerator;
		var newShits = new List<MapObject>();

		var entity = new Entity( idgen.GetNextObjectID() )
		{
			EntityData = new EntityData( gd ),
			GameData = gd,
			ClassName = gd.Name,
			Colour = ColorUtility.GetDefaultEntityColour(),
			Origin = origin
		};
		newShits.Add( entity );

		if ( gd.ClassType == ClassType.Solid )
		{
			var block = new BlockBrush();
			var sz = new Vector3( 16, 16, 16 );
			var brush = block.Create( null, idgen, new Box( worldPos - sz, worldPos + sz ), Document.SelectedTexture, 4 ).First();
			brush.SetParent( entity, true );
			brush.Colour = ColorUtility.GetRandomBrushColour();
			if ( brush is Solid s )
			{
				s.Faces.ForEach( x => x.CalculateTextureCoordinates( true ) );
			}
		}

		var create = new Create( Document.Map.WorldSpawn.ID, newShits );
		var select = new ChangeSelection( newShits, Document.Selection.GetSelectedObjects() );
		var action = new ActionCollection( create, select );
		Document.PerformAction( "Create entity: " + gd.Name, action );
	}

}
