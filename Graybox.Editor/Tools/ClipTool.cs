
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Graphics.Immediate;
using Graybox.Scenes;
using ImGuiNET;
using System.Drawing;

namespace Graybox.Editor.Tools;

public class ClipTool : BaseTool
{

	public enum ClipState
	{
		None,
		Drawing,
		Drawn,
	}

	public enum ClipSide
	{
		Both,
		Front,
		Back
	}

	public override string Name => "Clip Tool";
	public override string EditorIcon => "assets/icons/tool_cut.png";

	private Vector3 _clipPlanePoint1;
	private Vector3 _clipPlanePoint2;
	private ClipState _state = ClipState.None;
	private ClipSide _side = ClipSide.Back;
	private Plane _clipPlane;
	private Plane _hitPlane;

	public override void UpdateWidget()
	{
		base.UpdateWidget();

		base.UpdateWidget();
		ImGuiEx.Header( "Clip Tool" );

		// Clip Actions
		int clipActionIndex = (int)_side;

		ImGui.Spacing();
		ImGui.PushStyleVar( ImGuiStyleVar.ItemSpacing, new SVector2( 5, 5 ) );
		for ( int i = 0; i < 3; i++ )
		{
			string clipSideString = ((ClipSide)i).ToString();

			if ( i < 3 ) ImGui.SameLine();
			if ( ImGui.RadioButton( clipSideString, ref clipActionIndex, i ) )
			{
				_side = (ClipSide)i;
			}
		}
		ImGui.PopStyleVar();

		// Reset button
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		if ( ImGui.Button( "Reset Clip Plane" ) )
		{
			_clipPlane = default;
			_clipPlanePoint1 = default;
			_clipPlanePoint2 = default;
			_state = ClipState.None;
		}

		// Hotkey information
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.BeginDisabled();
		ImGui.TextWrapped( "Select brushes with the select tool then use this to cut them" );
		ImGui.Separator();
		ImGui.TextWrapped( "Hotkeys:" );
		ImGui.TextWrapped( "Click+Drag: Set clip plane" );
		ImGui.TextWrapped( "Enter: Perform clip" );
		ImGui.TextWrapped( "Escape: Reset clip plane" );
		ImGui.TextWrapped( "Tab: Toggle clip operation" );
		ImGui.EndDisabled();
	}

	public override void MouseDown( Scene scene, ref InputEvent e )
	{
		base.MouseDown( scene, ref e );

		if ( e.Button == MouseButton.Left )
		{
			_state = ClipState.Drawing;
			_clipPlanePoint1 = GetWorldPosition( scene, e.LocalMousePosition, out _hitPlane );
		}
	}

	public override void MouseUp( Scene scene, ref InputEvent e )
	{
		if ( e.Button == MouseButton.Left && _state == ClipState.Drawing )
		{
			SetPoint2( scene, e );
			_state = ClipState.Drawn;
		}
	}

	void SetPoint2( Scene scene, InputEvent e )
	{
		_clipPlanePoint2 = GetWorldPosition( scene, e.LocalMousePosition, out _ );
		var point3 = _clipPlanePoint2;
		Vector3 lineDirection = (_clipPlanePoint2 - _clipPlanePoint1).Normalized();
		Vector3 planeNormal;

		if ( scene.Camera.Orthographic )
		{
			planeNormal = Vector3.Cross( lineDirection, -scene.Camera.Forward ).Normalized();
			point3 += scene.Camera.Forward * 256;
		}
		else
		{
			planeNormal = Vector3.Cross( lineDirection, _hitPlane.Normal ).Normalized();
			point3 -= _hitPlane.Normal * 256;
		}

		_clipPlane = new Plane( planeNormal, _clipPlanePoint1 );
	}

	public override void MouseMove( Scene scene, ref InputEvent e )
	{
		if ( e.Button == MouseButton.Left && _state == ClipState.Drawing )
		{
			SetPoint2( scene, e );
		}
	}

	public override void KeyDown( Scene scene, ref InputEvent e )
	{
		base.KeyDown( scene, ref e );

		if ( e.Key == Key.Tab )
		{
			CycleClipSide();
			e.Handled = true;
			return;
		}

		if ( e.Key == Key.Enter && _state == ClipState.Drawn )
		{
			PerformClip();
			e.Handled = true;
			return;
		}

		if ( e.Key == Key.Escape )
		{
			_state = ClipState.None;
			_clipPlanePoint1 = default;
			_clipPlanePoint2 = default;
			e.Handled = true;
			return;
		}
	}

	private void PerformClip()
	{
		if ( _state != ClipState.Drawn ) return;

		var solids = CollectSolids();
		Document.PerformAction( "Perform Clip", new Clip( solids, _clipPlane, _side != ClipSide.Back, _side != ClipSide.Front ) );
		_state = ClipState.None;
	}

	IEnumerable<Solid> CollectSolids()
	{
		var result = new List<Solid>();
		var objects = Document.Selection.GetSelectedObjects();
		foreach ( var obj in objects )
		{
			if ( obj is Solid s )
			{
				result.Add( s );
			}
			result.AddRange( obj.GetChildren().OfType<Solid>() );
		}
		return result.Distinct();
	}

	public override void Render( Scene scene )
	{
		var solids = CollectSolids();
		foreach ( var obj in solids )
		{
			GL.Color4( obj.Colour );
			MapObjectRenderer.DrawWireframe( obj.Faces, true, true );
		}

		if ( _state == ClipState.None ) return;

		var selectionBox = Document?.Selection?.GetSelectionBoundingBox();
		if ( selectionBox == null ) return;

		var p1 = _clipPlanePoint1;
		var p2 = _clipPlanePoint2;

		GL.Color3( Color.White );
		GL.PointSize( 16 );
		GL.Begin( PrimitiveType.Points );
		GL.Vertex3( p1.X, p1.Y, p1.Z );
		GL.Vertex3( p2.X, p2.Y, p2.Z );
		GL.End();
		GL.PointSize( 1 );

		// Draw clipped solids
		var backFaces = new List<Face>();
		var frontFaces = new List<Face>();

		foreach ( Solid solid in solids )
		{
			solid.Split( _clipPlane, new(), out var frontSolid, out var backSolid );

			if ( frontSolid != null ) frontFaces.AddRange( frontSolid.Faces );
			if ( backSolid != null ) backFaces.AddRange( backSolid.Faces );
		}

		DrawClipVisualization( _side, _clipPlane, selectionBox, frontFaces, backFaces );
	}

	public override void ActivatedAgain()
	{
		base.ActivatedAgain();

		CycleClipSide();
	}

	private void CycleClipSide()
	{
		int side = (int)_side;
		side = (side + 1) % (Enum.GetValues( typeof( ClipSide ) ).Length);
		_side = (ClipSide)side;
	}

	public static void DrawPolygon( Polygon polygon, Color4 color )
	{
		if ( polygon.Vertices.Count < 3 )
			return;

		Vector3 normal = polygon.Plane.Normal;

		// Enable depth testing
		GL.Enable( EnableCap.DepthTest );

		// Draw polygon outline
		GL.LineWidth( 2.0f );
		GL.Begin( PrimitiveType.LineLoop );
		GL.Color3( 0.0f, 1.0f, 0.0f ); // Green outline
		foreach ( var vertex in polygon.Vertices )
		{
			GL.Vertex3( vertex );
		}
		GL.End();

		// Draw filled polygon
		GL.Enable( EnableCap.Blend );
		GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
		GL.Begin( PrimitiveType.Polygon );
		GL.Color4( color );
		foreach ( var vertex in polygon.Vertices )
		{
			GL.Vertex3( vertex );
		}
		GL.End();
		GL.Disable( EnableCap.Blend );

		// Draw normal vector
		Vector3 center = polygon.GetCenter();
		float normalLength = 1.0f; // Adjust this value to change the length of the normal vector
		GL.Begin( PrimitiveType.Lines );
		GL.Color3( 1.0f, 0.0f, 0.0f ); // Red for normal vector
		GL.Vertex3( center );
		GL.Vertex3( center + normal * normalLength );
		GL.End();

		GL.LineWidth( 1.0f ); // Reset line width
	}

	public void DrawClipVisualization( ClipSide side, Plane plane, Box selectionBox, List<Face> frontFaces, List<Face> backFaces )
	{
		GL.Enable( EnableCap.DepthTest );
		GL.Enable( EnableCap.Blend );
		GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );

		// Draw all faces with a neutral color
		GL.LineWidth( 2.0f );
		GL.Color4( 0.7f, 0.7f, 0.7f, 0.5f ); // Semi-transparent gray
		MapObjectRenderer.DrawWireframe( frontFaces, true, false );
		MapObjectRenderer.DrawWireframe( backFaces, true, false );

		// Draw the double-sided cutting plane
		//DrawDoubleSidedCuttingPlane( plane, selectionBox.Dimensions.Length );

		// Highlight the part that will be kept
		GL.LineWidth( 3.0f );
		GL.Color4( 0.0f, 1.0f, 0.0f, 0.7f ); // Green for kept part
		if ( side == ClipSide.Front || side == ClipSide.Both )
		{
			MapObjectRenderer.DrawWireframe( frontFaces, true, false );
		}
		if ( side == ClipSide.Back || side == ClipSide.Both )
		{
			MapObjectRenderer.DrawWireframe( backFaces, true, false );
		}

		// Highlight the part that will be removed
		GL.LineWidth( 2.0f );
		GL.Color4( 1.0f, 0.0f, 0.0f, 0.5f ); // Semi-transparent red for removed part
		if ( side != ClipSide.Front && side != ClipSide.Both )
		{
			MapObjectRenderer.DrawWireframe( frontFaces, true, false );
			MapObjectRenderer.DrawFilledNoFucks( frontFaces, Color.FromArgb( 75, 255, 0, 0 ), false, true, 0.15f );
		}
		if ( side != ClipSide.Back && side != ClipSide.Both )
		{
			MapObjectRenderer.DrawWireframe( backFaces, true, false );
			MapObjectRenderer.DrawFilledNoFucks( backFaces, Color.FromArgb( 75, 255, 0, 0 ), false, true, 0.15f );
		}

		// Draw arrows indicating which side is being kept/removed
		DrawClipSideArrows( plane, selectionBox, side );

		GL.Disable( EnableCap.Blend );
		GL.LineWidth( 1.0f ); // Reset line width
	}

	public static void DrawDoubleSidedCuttingPlane( Plane plane, float size = 10.0f )
	{
		Vector3 u, v;
		if ( Math.Abs( plane.Normal.Y ) < Math.Abs( plane.Normal.X ) )
			u = Vector3.Cross( plane.Normal, Vector3.UnitY );
		else
			u = Vector3.Cross( plane.Normal, Vector3.UnitX );
		u = Vector3.Normalize( u );
		v = Vector3.Cross( plane.Normal, u );
		Vector3 center = plane.PointOnPlane;

		GL.Enable( EnableCap.DepthTest );
		GL.Enable( EnableCap.Blend );
		GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );

		// Draw plane as a solid, semi-transparent surface (visible from both sides)
		GL.Disable( EnableCap.CullFace );
		GL.Begin( PrimitiveType.Quads );
		GL.Color4( 1.0f, 1.0f, 0.0f, 0.3f ); // Semi-transparent yellow
		GL.Vertex3( center - size * u - size * v );
		GL.Vertex3( center + size * u - size * v );
		GL.Vertex3( center + size * u + size * v );
		GL.Vertex3( center - size * u + size * v );
		// Draw the back side
		GL.Vertex3( center - size * u + size * v );
		GL.Vertex3( center + size * u + size * v );
		GL.Vertex3( center + size * u - size * v );
		GL.Vertex3( center - size * u - size * v );
		GL.End();
		GL.Enable( EnableCap.CullFace );

		// Draw plane outline
		GL.LineWidth( 3.0f );
		GL.Begin( PrimitiveType.LineLoop );
		GL.Color4( 1.0f, 1.0f, 0.0f, 1.0f ); // Solid yellow
		GL.Vertex3( center - size * u - size * v );
		GL.Vertex3( center + size * u - size * v );
		GL.Vertex3( center + size * u + size * v );
		GL.Vertex3( center - size * u + size * v );
		GL.End();

		GL.LineWidth( 1.0f ); // Reset line width
	}

	private void DrawClipSideArrows( Plane plane, Box selectionBox, ClipSide side )
	{
		Vector3 center = selectionBox.Center;
		Vector3 normal = plane.Normal;
		float arrowLength = selectionBox.Dimensions.Length * 0.2f;
		Vector3 arrowStart = center - normal * arrowLength;
		Vector3 arrowEnd = center + normal * arrowLength;

		GL.LineWidth( 4.0f );
		GL.Begin( PrimitiveType.Lines );

		if ( side == ClipSide.Front || side == ClipSide.Both )
		{
			// Green arrow pointing to the front (kept) side
			GL.Color4( 0.0f, 1.0f, 0.0f, 1.0f ); // Solid green
			GL.Vertex3( center );
			GL.Vertex3( arrowEnd );
		}
		else
		{
			// Red arrow pointing to the front (removed) side
			GL.Color4( 1.0f, 0.0f, 0.0f, 1.0f ); // Solid red
			GL.Vertex3( center );
			GL.Vertex3( arrowEnd );
		}

		if ( side == ClipSide.Back || side == ClipSide.Both )
		{
			// Green arrow pointing to the back (kept) side
			GL.Color4( 0.0f, 1.0f, 0.0f, 1.0f ); // Solid green
			GL.Vertex3( center );
			GL.Vertex3( arrowStart );
		}
		else
		{
			// Red arrow pointing to the back (removed) side
			GL.Color4( 1.0f, 0.0f, 0.0f, 1.0f ); // Solid red
			GL.Vertex3( center );
			GL.Vertex3( arrowStart );
		}

		GL.End();
		GL.LineWidth( 1.0f ); // Reset line width
	}

}
