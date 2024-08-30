
using Graybox.Interface;
using Graybox.Scenes;
using Graybox.Editor.Documents;
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics.Immediate;
using System.Drawing;
using Graybox.Scenes.Drawing;
using System;
using Graybox.Editor.Actions.MapObjects.Selection;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Graybox.Editor.Tools;

public abstract class BaseTool
{

	public virtual string Name => string.Empty;
	public virtual string HelpDescription => string.Empty;
	public virtual string EditorIcon => string.Empty;
	public Document Document => DocumentManager.CurrentDocument;
	public Scene Scene { get; set; }

	public void Setup( Document document, Scene scene )
	{
		Scene = scene;
	}

	public virtual void UpdateWidget()
	{

	}

	internal virtual void LoadWidgetData( string data )
	{

	}

	internal virtual string SaveWidgetData()
	{
		return string.Empty;
	}

	protected Vector3 SnapIfNeeded( Vector3 c )
	{
		var snapEnabled = EditorPrefs.GridSnapEnabled;
		var snapSize = EditorPrefs.GridSize;

		if ( Input.AltModifier ) snapEnabled = !snapEnabled;
		if ( !snapEnabled ) return c;
		if ( snapSize <= 0 ) return c;

		return c.Snap( snapSize );
	}

	protected Vector3 SnapIfNeeded( Vector3 c, Vector3 normal )
	{
		var snapEnabled = EditorPrefs.GridSnapEnabled;
		var snapSize = EditorPrefs.GridSize;
		if ( Input.AltModifier ) snapEnabled = !snapEnabled;
		if ( !snapEnabled ) return c;
		if ( snapSize <= 0 ) return c;

		normal = normal.Normalized();
		float projectedDistance = Vector3.Dot( c, normal );

		float snappedDistance = MathF.Round( projectedDistance / snapSize ) * snapSize;
		Vector3 snappedPosition = normal * snappedDistance;

		Vector3 perpendicularComponent = c - (normal * projectedDistance);

		return snappedPosition + perpendicularComponent;
	}

	public virtual void ToolSelected( bool preventHistory ) { }
	public virtual void ToolDeselected( bool preventHistory ) { }
	public virtual void MouseEnter( Scene scene, ref InputEvent e ) { }
	public virtual void MouseLeave( Scene scene, ref InputEvent e ) { }
	public virtual void MouseDown( Scene scene, ref InputEvent e ) { }
	public virtual void MouseClick( Scene scene, ref InputEvent e ) { }
	public virtual void MouseDoubleClick( Scene scene, ref InputEvent e ) { }
	public virtual void MouseUp( Scene scene, ref InputEvent e ) { }
	public virtual void MouseWheel( Scene scene, ref InputEvent e ) { }
	public virtual void MouseMove( Scene scene, ref InputEvent e ) { }
	public virtual void KeyPress( Scene scene, ref InputEvent e ) { }
	public virtual void KeyDown( Scene scene, ref InputEvent e ) { }
	public virtual void KeyUp( Scene scene, ref InputEvent e ) { }
	public virtual void UpdateFrame( Scene scene, FrameInfo frame ) { }
	public virtual void Render( Scene scene ) { }
	public virtual void Paint( Scene scene, UIElementPaintEvent e ) { }
	public virtual void BuildOverlay( Scene scene, Graybox.Interface.UIElement container ) { }
	public virtual void PreRender( Scene scene ) { }
	public virtual bool IsCapturingMouseWheel() => false;
	public virtual void ActivatedAgain() { }

	protected Vector3 GetWorldPosition( Scene scene, Vector2 screenPoint, out Plane plane )
	{
		plane = new Plane( Vector3.UnitY, Vector3.Zero );

		if ( scene.Camera.Orthographic )
		{
			var ray = scene.ScreenToRay( (int)screenPoint.X, (int)screenPoint.Y );
			var startPos = RemoveAxis( ray.Origin, scene.Camera.Forward, (float)Document.Map.GridSpacing );
			plane = new Plane( -scene.Camera.Forward, Vector3.Zero );
			return SnapIfNeeded( startPos );
		}

		if ( !scene.Camera.Orthographic )
		{
			var ray = scene.ScreenToRay( (int)screenPoint.X, (int)screenPoint.Y );
			var trace = scene.Physics.Trace( ray );

			if ( trace.Hit )
			{
				plane = new Plane( trace.Normal, trace.Position );

				return SnapIfNeeded( trace.Position/*, trace.Normal*/ );
			}

			var dir = ray.Direction.Normalized();
			var end = ray.Origin + dir * 1024;

			plane = new Plane( Vector3.UnitZ, end );

			return SnapIfNeeded( end );
		}

		return Vector3.Zero;
	}

	public static Vector3 RemoveAxis( Vector3 vector, Vector3 axis, float newValue = 0 )
	{
		return new Vector3(
			axis.X.IsNearlyZero() ? vector.X : newValue,
			axis.Y.IsNearlyZero() ? vector.Y : newValue,
			axis.Z.IsNearlyZero() ? vector.Z : newValue
		);
	}

	public void RenderSelection( Scene scene )
	{
		if ( Document?.Selection?.IsEmpty() ?? true ) return;

		var box = Document.Selection.GetSelectionBoundingBox();
		if ( box == null ) return;

		scene.Draw.DrawBox( box, Color.Yellow, 1, false, !scene.Camera.Orthographic );

		GL.LineWidth( 1.0f );

		foreach ( var obj in Document.Selection.GetSelectedObjects() )
		{
			var faces = obj.CollectFaces();
			GL.Color4( scene.Camera.Orthographic ? Color.White : obj.Colour );
			MapObjectRenderer.DrawWireframe( faces, true, false );
		}
	}

	protected virtual void OnTranslate( SceneGizmos.TranslateEvent e )
	{

	}

	protected void RenderTranslateGizmo( Vector3 origin, Quaternion rotation, Scene scene )
	{
		var translateScale = scene.CalculateScaleForSomething( origin );
		scene.Gizmos.TranslateWidget( origin, rotation, 80 * translateScale, OnTranslate );
	}

	protected void Select( IEnumerable<MapObject> objects, KeyModifiers modifiers )
	{
		if ( Document == null ) return;

		var currentSelection = Document.Selection.GetSelectedObjects().ToList();
		var newSelection = new List<MapObject>();
		var deselected = new List<MapObject>();

		if ( !objects?.Any() ?? true )
		{
			if ( modifiers == KeyModifiers.Control )
				return;

			deselected = currentSelection;
		}
		else
		{
			if ( modifiers == KeyModifiers.Control )
			{
				foreach ( var obj in objects )
				{
					if ( currentSelection.Contains( obj ) )
					{
						deselected.Add( obj );
					}
					else
					{
						newSelection.Add( obj );
					}
				}
			}
			else
			{
				deselected = currentSelection.Except( objects ).ToList();
				newSelection = objects.ToList();
			}
		}

		for ( int i = newSelection.Count - 1; i >= 0; i-- )
		{
			var obj = newSelection[i];
			if ( obj.Parent is Entity e && !newSelection.Contains( e ) )
			{
				newSelection.Remove( obj );
				newSelection.Add( e );
			}
		}

		Document.PerformAction( "Selection changed", new ChangeSelection( newSelection, deselected ) );
	}

}

public enum HotkeyInterceptResult
{
	Continue,
	Abort,
	SwitchToSelectTool
}
