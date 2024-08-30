
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics.Immediate;
using Graybox.Interface;
using SkiaSharp;
using System.Drawing;
using Graybox.Editor.Brushes;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Editor.Actions;
using Graybox.Scenes;
using Graybox.Editor.Settings;
using ImGuiNET;

namespace Graybox.Editor.Tools;

internal class BlockTool : BaseTool
{

	public override string EditorIcon => "assets/icons/tool_block.png";
	public override string Name => "Block Tool";

	Vector3 MouseWorldPosition;
	Plane MousePlane;
	Box Bounds;
	Plane BrushPlane;
	BrushSteps CurrentStep;
	MapObject Preview;
	bool CanExtrude;
	ColorPulse FillColorPulse;
	BrushSettings CurrentBrushSettings;

	enum BrushSteps
	{
		ChoosingFirstCorner,
		ChoosingSecondCorner,
		Extruding
	}

	public override void UpdateWidget()
	{
		base.UpdateWidget();

		if ( Document == null ) return;

		ImGuiEx.Header( "Brushes" );

		float windowVisibleX2 = ImGui.GetContentRegionAvail().X;
		float buttonSize = 32.0f;
		float buttonSpacing = ImGui.GetStyle().ItemSpacing.X;
		float maxX = ImGui.GetCursorScreenPos().X + windowVisibleX2 - buttonSpacing;

		foreach ( var brush in BrushManager.Brushes )
		{
			if ( string.IsNullOrEmpty( brush.EditorIcon ) ) continue;

			var img = EditorResource.Image( brush.EditorIcon );
			if ( img == 0 ) continue;

			var isSelected = BrushManager.CurrentBrush == brush;

			if ( isSelected ) ImGuiEx.PushButtonPrimary();

			if ( ImGui.ImageButton( brush.Name, img, new System.Numerics.Vector2( buttonSize, buttonSize ) ) )
			{
				BrushManager.UpdateSelectedBrush( brush );
				CurrentBrushSettings = brush.GetSettingsInstance();
			}

			float xpos = ImGui.GetItemRectMax().X;

			if ( ImGui.IsItemHovered() ) ImGui.SetTooltip( brush.Name );

			if ( isSelected ) ImGuiEx.PopButtonPrimary();

			if ( brush != BrushManager.Brushes[BrushManager.Brushes.Count - 1] && xpos < maxX - buttonSpacing * 2 - buttonSize )
			{
				ImGui.SameLine();
			}
		}

		var brushName = BrushManager.CurrentBrush?.Name ?? "Brush";

		if ( CurrentBrushSettings != null )
		{
			ImGuiEx.Header( $"{brushName} Settings", true );
			ImObjectEditor.EditObject( CurrentBrushSettings );
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.BeginDisabled();
		ImGui.TextWrapped( "Click & drag to draw a shape.  Release mouse to extrude, then click again to build the brush." );
		ImGui.EndDisabled();
	}

	public override void MouseMove( Scene scene, ref InputEvent e )
	{
		switch ( CurrentStep )
		{
			case BrushSteps.ChoosingFirstCorner:
				MouseWorldPosition = GetWorldPosition( scene, e.LocalMousePosition, out MousePlane );
				CanExtrude = !scene.Camera.Orthographic;
				break;
			case BrushSteps.ChoosingSecondCorner:
				MouseWorldPosition = GetPlanePosition( scene, new Vector3( e.LocalMousePosition.X, e.LocalMousePosition.Y, 0 ), BrushPlane );
				Bounds = new Box( Bounds.Start, MouseWorldPosition );
				UpdatePreview();
				break;
			case BrushSteps.Extruding:
				var extrudePlane = GetExtrudePlane( scene );
				MouseWorldPosition = GetPlanePosition( scene, new Vector3( e.LocalMousePosition.X, e.LocalMousePosition.Y, 0 ), extrudePlane );
				var fixedPoint = Bounds.Start;
				var movingPoint = MouseWorldPosition;

				var direction = BrushPlane.Normal;

				movingPoint = new Vector3(
					!direction.X.IsNearlyZero() ? movingPoint.X : Bounds.End.X,
					!direction.Y.IsNearlyZero() ? movingPoint.Y : Bounds.End.Y,
					!direction.Z.IsNearlyZero() ? movingPoint.Z : Bounds.End.Z
				);

				Bounds = new Box( fixedPoint, movingPoint );
				UpdatePreview();
				break;
		}
	}

	Plane GetExtrudePlane( Scene scene )
	{
		if ( !scene.Camera.Orthographic )
		{
			var crossVector = Vector3.Cross( BrushPlane.Normal, Vector3.UnitX );
			if ( crossVector.LengthSquared < 0.0001f )
			{
				crossVector = Vector3.Cross( BrushPlane.Normal, Vector3.UnitY );
				if ( crossVector.LengthSquared < 0.0001f )
				{
					crossVector = Vector3.Cross( BrushPlane.Normal, Vector3.UnitZ );
				}
			}
			crossVector = crossVector.Normalized();
			return new Plane( crossVector, Bounds.End );
		}

		return new Plane( -scene.Camera.Forward, Vector3.Zero );
	}

	Vector3 GetPlanePosition( Scene scene, Vector3 screenPoint, Plane plane )
	{
		var ray = scene.ScreenToRay( (int)screenPoint.X, (int)screenPoint.Y );

		if ( scene.Camera.Orthographic )
		{
			var startPos = RemoveAxis( ray.Origin, scene.Camera.Forward, (float)Document.Map.GridSpacing );
			plane = new Plane( scene.Camera.Forward, Vector3.Zero );

			return SnapIfNeeded( startPos );
		}

		var gplane = new Plane( plane.Normal, plane.PointOnPlane );
		if ( gplane.Intersect( ray, out var intersection ) )
			return SnapIfNeeded( intersection/*, plane.Normal*/ );

		return Vector3.Zero;
	}

	void UpdatePreview()
	{
		Preview = null;

		if ( Bounds == null ) return;

		Preview = GetBrush( Bounds, new IDGenerator() );
		if ( Preview == null )
		{
			return;
		}
		Preview.Colour = Color.White;

		//var solids = new List<Solid>();
		//if ( Preview is Solid s ) solids.Add( s );

		//solids.AddRange( Preview.GetAllDescendants<Solid>() );

		//var normal = BrushPlane.Normal.Normalized() * 1.1M;

		//foreach ( var solid in solids )
		//{
		//	foreach ( var face in solid.Faces )
		//	{
		//		foreach ( var v in face.Vertices )
		//		{
		//			v.Location += normal;
		//		}
		//	}
		//}
	}

	public override void MouseDown( Scene scene, ref InputEvent e )
	{
		if ( e.Button != MouseButton.Left ) return;

		e.Handled = true;

		switch ( CurrentStep )
		{
			case BrushSteps.ChoosingFirstCorner:
				Bounds = new Box( MouseWorldPosition, MouseWorldPosition );
				BrushPlane = MousePlane;
				CurrentStep = BrushSteps.ChoosingSecondCorner;
				FillColorPulse = new ColorPulse( GetRenderFillColour() );
				break;
			case BrushSteps.ChoosingSecondCorner:
				Bounds = new Box( Bounds.Start, MouseWorldPosition );
				if ( CanExtrude ) CurrentStep = BrushSteps.Extruding;
				else Complete( scene );
				break;
			case BrushSteps.Extruding:
				Complete( scene );
				CurrentStep = BrushSteps.ChoosingFirstCorner;
				break;
		}
	}

	void Cancel()
	{
		Preview = null;
		Bounds = null;
		CurrentStep = BrushSteps.ChoosingFirstCorner;
	}

	Vector3 GetUnusedCoordinate( Scene scene, Vector3 t )
	{
		var f = scene.Camera.Forward.Absolute();
		if ( f.X != 0 ) return new( t.X, 0, 0 );
		if ( f.Y != 0 ) return new( 0, t.Y, 0 );
		if ( f.Z != 0 ) return new( 0, 0, t.Z );
		return t;
	}

	void Complete( Scene scene )
	{
		Preview = null;
		CurrentStep = BrushSteps.ChoosingFirstCorner;

		if ( scene.Camera.Orthographic )
		{
			var end = Bounds.End;
			end += GetUnusedCoordinate( scene, OpenTK.Mathematics.Vector3.One ) * 128;
			Bounds = new Box( Bounds.Start, end );
		}

		var brush = GetBrush( Bounds, Document.Map.IDGenerator );
		if ( brush == null ) return;

		brush.IsSelected = Graybox.Editor.Settings.Select.SelectCreatedBrush;
		IAction action = new Create( Document.Map.WorldSpawn.ID, brush );
		if ( Graybox.Editor.Settings.Select.SelectCreatedBrush && Graybox.Editor.Settings.Select.DeselectOthersWhenSelectingCreation )
		{
			action = new ActionCollection( new ChangeSelection( new MapObject[0], Document.Selection.GetSelectedObjects() ), action );
		}

		Document.PerformAction( "Create " + BrushManager.CurrentBrush.Name.ToLower(), action );
	}

	public override void Render( Scene scene )
	{
		base.Render( scene );

		if ( CurrentStep == BrushSteps.ChoosingFirstCorner ) return;
		if ( Preview == null ) return;

		var preview = CollectFaces( Preview );
		GL.Disable( EnableCap.CullFace );
		GL.BindTexture( TextureTarget.Texture2D, 0 );
		MapObjectRenderer.DrawFilled( preview, FillColorPulse, false );
		GL.Color4( GetRenderBoxColour() );
		MapObjectRenderer.DrawWireframe( preview, true, true );
		GL.Enable( EnableCap.CullFace );

#if DEBUG && false
		if ( viewport is Viewport3D threed )
		{
			if ( CurrentStep == BrushSteps.Extruding )
			{
				var plane = GetExtrudePlane( viewport );
				var poly = new Polygon( plane );

				GL.Disable( EnableCap.CullFace );
				GL.Begin( PrimitiveType.Polygon );
				GL.Color4( Color.FromArgb( 100, Color.White ) );

				foreach ( Coordinate c in poly.Vertices )
					GL.Vertex3( c.DX + (Bounds.Start.DX - plane.Normal.DX * Bounds.Start.DX),
							   c.DY + (Bounds.Start.DY - plane.Normal.DY * Bounds.Start.DY),
							   c.DZ + (Bounds.Start.DZ - plane.Normal.DZ * Bounds.Start.DZ) );

				GL.End();
				GL.Enable( EnableCap.CullFace );
			}
		}
#endif
	}

	public override void MouseUp( Scene scene, ref InputEvent e )
	{
		if ( e.Button != MouseButton.Left ) return;
		if ( CurrentStep != BrushSteps.ChoosingSecondCorner ) return;
		if ( (MouseWorldPosition - Bounds.Start).LengthSquared < 4 ) return;

		Bounds = new Box( Bounds.Start, MouseWorldPosition );

		if ( CanExtrude )
		{
			CurrentStep = BrushSteps.Extruding;
		}
		else
		{
			Complete( scene );
		}
	}

	public override void KeyDown( Scene scene, ref InputEvent e )
	{
		if ( e.Key == Key.Escape )
		{
			Cancel();
			EventSystem.Publish( HotkeysMediator.SelectionClear );
		}
	}

	public override void Paint( Scene scene, UIElementPaintEvent e )
	{
		base.Paint( scene, e );

		if ( MouseWorldPosition != default && CurrentStep != BrushSteps.Extruding )
		{
			var uiPos = scene.WorldToScreen( MouseWorldPosition );
			var rectSize = new SKSize( 10, 10 );

			if ( !scene.Camera.Orthographic )
			{
				var campos = new Vector3( (int)scene.Camera.Position.X, (int)scene.Camera.Position.Y, (int)scene.Camera.Position.Z );
				var distance = (campos - MouseWorldPosition).LengthSquared;
				var scale = 1.0f / (1.0f + (float)distance * .000001f);
				rectSize = new SKSize( rectSize.Width * scale, rectSize.Height * scale );
			}

			using ( var paint = new SKPaint() )
			{
				paint.IsAntialias = true;
				paint.Color = SKColors.Yellow;

				var x = uiPos.X - rectSize.Width * .5f;
				var y = uiPos.Y - rectSize.Height * .5f;
				var rect = new SKRect( x, y, x + rectSize.Width, y + rectSize.Height );
				e.Canvas.DrawRoundRect( rect, 4, 4, paint );

				paint.Color = SKColors.Black;
				paint.IsStroke = true;

				e.Canvas.DrawRoundRect( rect, 4, 4, paint );
			}
		}

		if ( CurrentStep >= BrushSteps.ChoosingSecondCorner )
		{
			scene.Draw.PaintBoxSize( Bounds, Color.Yellow, e );
		}
	}

	private MapObject GetBrush( Box bounds, IDGenerator idg )
	{
		var _bounds = new Box( bounds.Start, bounds.End );
		if ( (_bounds.Start - _bounds.End).VectorMagnitude() > 1000000f )
		{
			_bounds = new Box( bounds.Start, ((bounds.End - bounds.Start).Normalized() * 1000000f) + bounds.Start );
		}

		_bounds = new Box(
			new Vector3( Math.Min( _bounds.Start.X, _bounds.End.X ), Math.Min( _bounds.Start.Y, _bounds.End.Y ), Math.Min( _bounds.Start.Z, _bounds.End.Z ) ),
			new Vector3( Math.Max( _bounds.Start.X, _bounds.End.X ), Math.Max( _bounds.Start.Y, _bounds.End.Y ), Math.Max( _bounds.Start.Z, _bounds.End.Z ) )
		);

		var brush = BrushManager.CurrentBrush;
		var texture = Document.SelectedTexture;
		var created = brush.Create( CurrentBrushSettings, idg, _bounds, texture, BrushManager.RoundCreatedVertices ? 0 : 2 ).ToList();

		if ( created.Count > 1 )
		{
			Group g = new Group( idg.GetNextObjectID() );
			created.ForEach( x => x.SetParent( g ) );
			g.UpdateBoundingBox();
			return g;
		}

		return created.FirstOrDefault();
	}

	private List<Face> CollectFaces( MapObject brush )
	{
		var result = new List<Face>();
		var solids = brush.GetAllDescendants<Solid>().ToList();

		if ( brush is Solid s )
			solids.Add( s );

		return solids.SelectMany( x => x.Faces ).ToList();
	}

	void Coord( double x, double y, double z ) => GL.Vertex3( x, y, z );
	Color GetRenderFillColour() => Color.FromArgb( 100, Color.DodgerBlue );
	Color GetRenderFillColour2d() => Color.FromArgb( 85, Color.White );
	Color GetRenderBoxColour() => Color.FromArgb( 255, Color.White );

}
