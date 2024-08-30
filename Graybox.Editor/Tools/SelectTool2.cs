
using Graybox.DataStructures.MapObjects;
using Graybox.Graphics.Immediate;
using System.Drawing;
using Graybox.Editor.Actions.MapObjects.Selection;
using Graybox.Interface;
using Graybox.Graphics.Helpers;
using Graybox.DataStructures.Transformations;
using Graybox.Editor.Actions.MapObjects.Operations.EditOperations;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Actions;
using Graybox.Scenes.Physics;
using Graybox.Scenes.Drawing;
using Graybox.Scenes;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

namespace Graybox.Editor.Tools;

internal class SelectTool2 : BaseTool
{

	public enum WidgetTools
	{
		Translate,
		Resize,
		Rotate,
	}

	public override string Name => "Select Tool";
	public override string EditorIcon => "assets/icons/tool_select.png";

	MapObject hoveredObject;
	Box boxSelect;
	bool dragging;
	bool dragging3d;
	bool mouseIsDown;
	List<MapObject> selectionPreview = new List<MapObject>();
	WidgetTools ActiveWidget = WidgetTools.Translate;
	(Vector3 origin, SceneGizmos.RotateEvent rotation)? RotateData;

	Vector3 transformTranslate = default;
	Vector3 transformRotate = default;
	Vector3 transformScale = default;

	public override void UpdateWidget()
	{
		base.UpdateWidget();

		ImGuiEx.Header( "3D Widget" );

		var boundsIcon = EditorResource.Image( "assets/icons/widget_bounds.png" );
		var rotateIcon = EditorResource.Image( "assets/icons/widget_rotate.png" );
		var translateIcon = EditorResource.Image( "assets/icons/widget_translate.png" );
		var createIcon = EditorResource.Image( "assets/icons/widget_create.png" );

		var options = new List<(string Tooltip, int Icon, WidgetTools Tool)>()
		{
			new ( "Translate (Q)", translateIcon, WidgetTools.Translate ),
			new ( "Resize (W)", boundsIcon, WidgetTools.Resize ),
			new ( "Rotate (E)", rotateIcon, WidgetTools.Rotate ),
		};

		foreach ( var opt in options )
		{
			var primary = ActiveWidget == opt.Tool;
			if ( primary )
				ImGuiEx.PushButtonPrimary();

			if ( ImGui.ImageButton( "##" + opt.Tooltip, opt.Icon, new System.Numerics.Vector2( 32, 32 ) ) )
				ActiveWidget = opt.Tool;

			if ( primary )
				ImGuiEx.PopButtonPrimary();

			if ( ImGui.IsItemHovered() ) ImGui.SetTooltip( opt.Tooltip );
			if ( opt != options.Last() ) ImGui.SameLine();
		}

		ImGuiEx.Header( "Transformation", true );

		ImGuiEx.EditVector3( "Move", ref this.transformTranslate, 0.1f, -1024, 1024 );
		ImGuiEx.EditVector3( "Rotate", ref this.transformRotate, 1f, -360, 360 );
		ImGuiEx.EditVector3( "Scale", ref this.transformScale, 0.1f, -10, 10 );

		var disabled = (transformRotate == default && transformTranslate == default &&
				(transformScale == Vector3.One || transformScale == Vector3.Zero));

		if ( ImGui.Button( "Apply" ) && !disabled )
		{
			var selectionBounds = Document?.Selection?.GetSelectionBoundingBox();
			if ( selectionBounds != null )
			{
				var rot = new Vector3(
					MathHelper.DegreesToRadians( this.transformRotate.X ),
					MathHelper.DegreesToRadians( this.transformRotate.Y ),
					MathHelper.DegreesToRadians( this.transformRotate.Z )
				);

				var toOrigin = Matrix4.CreateTranslation( -selectionBounds.Center );
				var fromOrigin = Matrix4.CreateTranslation( selectionBounds.Center );
				var rotation = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( rot ) );
				var translation = Matrix4.CreateTranslation( this.transformTranslate );

				// Create scale matrix, excluding components that are 0 or 1
				var scale = Matrix4.CreateScale(
					Math.Abs( transformScale.X ) < float.Epsilon || Math.Abs( transformScale.X - 1 ) < float.Epsilon ? 1 : transformScale.X,
					Math.Abs( transformScale.Y ) < float.Epsilon || Math.Abs( transformScale.Y - 1 ) < float.Epsilon ? 1 : transformScale.Y,
					Math.Abs( transformScale.Z ) < float.Epsilon || Math.Abs( transformScale.Z - 1 ) < float.Epsilon ? 1 : transformScale.Z
				);

				var transformation = new UnitMatrixMult(
					toOrigin *    // 1. Move to origin
					scale *       // 2. Apply scaling
					rotation *    // 3. Apply rotation
					fromOrigin *  // 4. Move back to original center
					translation   // 5. Apply translation
				);

				ExecuteTransform( "Transform", transformation, false );
			}
		}
		ImGui.SameLine();
		if ( ImGui.Button( "Reset" ) )
		{
			this.transformRotate = default;
			this.transformTranslate = default;
			this.transformScale = default;
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.TextDisabled( "Hotkeys:" );
		ImGui.TextDisabled( "Q: Translate widget" );
		ImGui.TextDisabled( "W: Resize widget" );
		ImGui.TextDisabled( "E: Rotate widget" );
		ImGui.TextDisabled( "Hold Alt: Grid snap on/off" );
	}

	public override void KeyDown( Scene scene, ref InputEvent e )
	{
		if ( e.Key == Key.Escape )
		{
			if ( scene.Gizmos.IsCaptured )
			{
				scene.Gizmos.CancelGizmos();
				widgetBoundsBox = null;
				RotateData = null;
				translateBox = null;
				dragging = false;
				boxSelect = null;
				dragging3d = false;
				mouseIsDown = false;
				e.Handled = true;
				return;
			}
			else
			{
				selectionPreview.Clear();
				e.Handled = true;
				Document?.ClearSelection();
			}
		}

		if ( e.Key == Key.Q )
		{
			ActiveWidget = WidgetTools.Translate;
			e.Handled = true;
			return;
		}

		if ( e.Key == Key.W )
		{
			ActiveWidget = WidgetTools.Resize;
			e.Handled = true;
			return;
		}

		if ( e.Key == Key.E )
		{
			ActiveWidget = WidgetTools.Rotate;
			e.Handled = true;
			return;
		}

		if ( e.Key == Key.Delete )
		{
			Document?.DeleteSelection();
		}
	}

	public override void MouseMove( Scene scene, ref InputEvent e )
	{
		var ray = scene.ScreenToRay( e.LocalMousePosition );

		TraceResult trace;
		if ( scene.Camera.Orthographic )
		{
			var size = new OpenTK.Mathematics.Vector3( 8 * scene.Camera.OrthographicZoom );
			trace = scene.Physics.EdgeTraceBox( -size, size, ray.Origin, ray.Origin + ray.Direction.Normalized() * 100000 );
		}
		else
		{
			trace = scene.Physics.Trace( ray );
		}

		if ( trace.Object?.Parent is Group g )
		{
			hoveredObject = g;
		}
		else
		{
			hoveredObject = trace.Object;
		}

		if ( hoveredObject != null )
		{
			//this.Scene.Gizmos.Line( trace.Position, trace.Position + trace.Normal * 32, Vector3.UnitX );
			//Debug.LogWarning( hoveredObject.ID );
		}

		if ( mouseIsDown && boxSelect != null )
		{
			var worldPos = GetWorldPosition( scene, e );
			if ( !dragging )
			{
				var dist = (worldPos - boxSelect.Start).VectorMagnitude();
				if ( dist > 1 )
				{
					dragging = true;
					dragging3d = !scene.Camera.Orthographic;
					e.Handled = true;
				}
			}
			else
			{
				boxSelect = new Box( boxSelect.Start, worldPos );
				selectionPreview.Clear();
				selectionPreview.AddRange( BoxSelect( scene, boxSelect ) );
				e.Handled = true;
			}
		}
	}

	public override void MouseDown( Scene scene, ref InputEvent e )
	{
		if ( e.Button != MouseButton.Left ) return;

		var startPos = GetWorldPosition( scene, e );
		boxSelect = new Box( startPos, startPos );
		mouseIsDown = true;
	}

	public override void MouseUp( Scene scene, ref InputEvent e )
	{
		if ( e.Button != MouseButton.Left ) return;
		if ( !mouseIsDown ) return;

		if ( dragging )
		{
			var selection = BoxSelect( scene, boxSelect ).ToList();
			this.Select( selection, e.Modifiers );
		}
		else
		{
			var arr = hoveredObject != null
				? new MapObject[] { hoveredObject }
				: Enumerable.Empty<MapObject>();

			Select( arr, e.Modifiers );
		}

		mouseIsDown = false;
		dragging = false;
		selectionPreview.Clear();
	}

	public override void MouseLeave( Scene scene, ref InputEvent e )
	{
		mouseIsDown = false;
		dragging = false;
	}

	Box translateBox;
	Box widgetBoundsBox;
	public override void Render( Scene scene )
	{
		RenderSelection( scene );

		if ( !Document.Selection.IsEmpty() )
		{
			var box = Document.Selection.GetSelectionBoundingBox();
			if ( box == null ) return;

			//float angle = MathHelper.DegreesToRadians( 45 );
			//var upRotation = OpenTK.Quaternion.FromAxisAngle( Vector3.UnitX, angle );
			//var rightRotation = OpenTK.Quaternion.FromAxisAngle( Vector3.UnitZ, angle );
			//var combinedRotation = upRotation * rightRotation;

			if ( !scene.Camera.Orthographic )
			{
				if ( ActiveWidget == WidgetTools.Translate )
				{
					var translateScale = scene.CalculateScaleForSomething( box.Center );
					scene.Gizmos.TranslateWidget( box.Center, Quaternion.Identity, 80 * translateScale, x =>
					{
						var startPosition = x.StartPosition;
						var currentPosition = x.NewPosition;
						var translationVector = SnapIfNeeded( currentPosition - startPosition );
						var matrix = Matrix4.CreateTranslation( translationVector );
						var clone = Input.ShiftModifier;
						var boxColor = clone ? Color.OliveDrab : Color.DodgerBlue;

						if ( !x.Completed )
						{
							var faces = Document.Selection.GetSelectedObjects().OfType<Solid>().SelectMany( y => y.Faces );
							GL.PushMatrix();
							GL.MultMatrix( ref matrix );
							MapObjectRenderer.DrawFilled( faces, new ColorPulse( Color.FromArgb( 125, boxColor ) ), false, false );
							GL.Color3( 1f, 1, 0 );
							MapObjectRenderer.DrawWireframe( faces, true, false );
							scene.Draw.DrawBox( box, clone ? Color.OliveDrab : Color.Yellow, 1 );
							GL.PopMatrix();

							translateBox = new Box( box.Center, box.Center + translationVector );

							GL.Disable( EnableCap.DepthTest );
							scene.Draw.DrawBox( translateBox, Color.White, 2 );
							GL.Enable( EnableCap.DepthTest );

							return;
						}
						translateBox = null;
						ExecuteTransform( "Translate", new UnitMatrixMult( matrix ), clone );
					} );
				}

				if ( ActiveWidget == WidgetTools.Resize )
				{
					var bounds = new Bounds( box.Start, box.End );
					var localBounds = new Bounds( bounds.Mins - bounds.Center, bounds.Maxs - bounds.Center );
					var handleScale = scene.CalculateScaleForSomething( bounds.Center );
					scene.Gizmos.BoundsWidget( localBounds, bounds.Center, OpenTK.Mathematics.Quaternion.Identity, 8f, default, (Action<SceneGizmos.TranslateEvent>)(( x ) =>
					{
						var snappedPosition = base.SnapIfNeeded( x.NewPosition );
						var newBounds = bounds;
						newBounds.ExpandFaceCenter( x.HandleIndex, snappedPosition );

						scene.Gizmos.Sphere( x.NewPosition, 10f, new( 1, 0, 0 ) );

						if ( x.Completed )
						{
							var originalSize = bounds.Size;
							var newSize = newBounds.Size;
							var scale = new OpenTK.Mathematics.Vector3( newSize.X / originalSize.X, newSize.Y / originalSize.Y, newSize.Z / originalSize.Z );
							var origin = bounds.Center;

							switch ( x.HandleIndex )
							{
								case 0: // Bottom face
									origin = new OpenTK.Mathematics.Vector3( bounds.Center.X, bounds.Center.Y, bounds.Maxs.Z );
									break;
								case 1: // Top face
									origin = new OpenTK.Mathematics.Vector3( bounds.Center.X, bounds.Center.Y, bounds.Mins.Z );
									break;
								case 2: // Front face
									origin = new OpenTK.Mathematics.Vector3( bounds.Center.X, bounds.Maxs.Y, bounds.Center.Z );
									break;
								case 3: // Back face
									origin = new OpenTK.Mathematics.Vector3( bounds.Center.X, bounds.Mins.Y, bounds.Center.Z );
									break;
								case 4: // Left face
									origin = new OpenTK.Mathematics.Vector3( bounds.Maxs.X, bounds.Center.Y, bounds.Center.Z );
									break;
								case 5: // Right face
									origin = new OpenTK.Mathematics.Vector3( bounds.Mins.X, bounds.Center.Y, bounds.Center.Z );
									break;
							}

							ExecuteTransform( "Resize", new UnitScale( scale, origin ), false );
							widgetBoundsBox = null;
							return;
						}
						widgetBoundsBox = new Box( newBounds.Mins, newBounds.Maxs );
						scene.Draw.DrawBox( widgetBoundsBox, Color.Yellow, 1 );
					}) );
				}
			}

			if ( ActiveWidget == WidgetTools.Rotate )
			{
				var rotateScale = scene.CalculateScaleForSomething( box.Center );
				var angleSnap = EditorPrefs.AngleSnap;
				angleSnap = Math.Clamp( angleSnap, 0, 90 );
				Scene.Gizmos.RotationWidget( box.Center, Quaternion.Identity, 80 * rotateScale, angleSnap, x =>
				{
					var origin = box.Center;
					var translation1 = Matrix4.CreateTranslation( -origin );
					var translation2 = Matrix4.CreateTranslation( origin );
					var matrix = translation1 * Matrix4.CreateFromAxisAngle( x.Axis, x.Angle ) * translation2;

					if ( x.Completed )
					{
						ExecuteTransform( "Rotate", new UnitMatrixMult( matrix ), false );
						RotateData = null;
					}
					else
					{
						var faces = Document.Selection.GetSelectedObjects().OfType<Solid>().SelectMany( y => y.Faces );
						GL.PushMatrix();
						GL.MultMatrix( ref matrix );
						MapObjectRenderer.DrawFilled( faces, new ColorPulse( Color.FromArgb( 125, Color.Orange ) ), false, false );
						GL.Color3( 1f, 1, 0 );
						MapObjectRenderer.DrawWireframe( faces, true, false );
						//scene.Draw.DrawBox( box, Color.Yellow, 1 );
						GL.PopMatrix();
						RotateData = new( box.Center, x );
					}
				} );
			}

			if ( scene.Camera.Orthographic && ActiveWidget != WidgetTools.Rotate )
			{
				scene.Gizmos.TranslateWidget2D( box.Start, box.End, scene.Camera.Backward, x =>
				{
					var startPosition = x.StartPosition;
					var currentPosition = x.NewPosition;
					var translationVector = SnapIfNeeded( currentPosition - startPosition );
					var matrix = Matrix4.CreateTranslation( translationVector );
					var clone = Input.ShiftModifier;
					var boxColor = clone ? Color.OliveDrab : Color.DodgerBlue;

					if ( !x.Completed )
					{
						var faces = Document.Selection.GetSelectedObjects().OfType<Solid>().SelectMany( y => y.Faces );
						GL.PushMatrix();
						GL.MultMatrix( ref matrix );
						MapObjectRenderer.DrawFilled( faces, new ColorPulse( Color.FromArgb( 125, boxColor ) ), false, false );
						GL.Color3( 1f, 1, 1 );
						MapObjectRenderer.DrawWireframe( faces, true, false );
						scene.Draw.DrawBox( box, new ColorPulse( boxColor ), 1, true, false );
						GL.PopMatrix();

						GL.Disable( EnableCap.DepthTest );
						translateBox = new Box( box.Center, box.Center + translationVector );
						scene.Draw.DrawBox( translateBox, Color.Gray, 1.0f, false );
						GL.Enable( EnableCap.DepthTest );

						return;
					}
					translateBox = null;
					ExecuteTransform( "Translate", new UnitMatrixMult( matrix ), clone );
				} );

				var bounds = new Bounds( box.Start, box.End );
				var localBounds = new Bounds( bounds.Mins - bounds.Center, bounds.Maxs - bounds.Center );
				var handleScale = scene.CalculateScaleForSomething( bounds.Center );
				scene.Gizmos.BoundsWidget( localBounds, bounds.Center, OpenTK.Mathematics.Quaternion.Identity, 16f * handleScale, scene.Camera.Forward, (Action<SceneGizmos.TranslateEvent>)(( x ) =>
				{
					var snappedPosition = base.SnapIfNeeded( x.NewPosition );
					var newBounds = bounds;
					newBounds.ExpandFaceCenter( x.HandleIndex, snappedPosition );

					if ( x.Completed )
					{
						var originalSize = bounds.Size;
						var newSize = newBounds.Size;
						var scale = new OpenTK.Mathematics.Vector3( newSize.X / originalSize.X, newSize.Y / originalSize.Y, newSize.Z / originalSize.Z );
						var origin = bounds.Center;

						switch ( x.HandleIndex )
						{
							case 0: // Bottom face
								origin = new Vector3( bounds.Center.X, bounds.Center.Y, bounds.Maxs.Z );
								break;
							case 1: // Top face
								origin = new Vector3( bounds.Center.X, bounds.Center.Y, bounds.Mins.Z );
								break;
							case 2: // Front face
								origin = new Vector3( bounds.Center.X, bounds.Maxs.Y, bounds.Center.Z );
								break;
							case 3: // Back face
								origin = new Vector3( bounds.Center.X, bounds.Mins.Y, bounds.Center.Z );
								break;
							case 4: // Left face
								origin = new Vector3( bounds.Maxs.X, bounds.Center.Y, bounds.Center.Z );
								break;
							case 5: // Right face
								origin = new Vector3( bounds.Mins.X, bounds.Center.Y, bounds.Center.Z );
								break;
						}

						ExecuteTransform( "Resize", new UnitScale( scale, origin ), false );
						widgetBoundsBox = null;
						return;
					}
					widgetBoundsBox = new Box( newBounds.Mins, newBounds.Maxs );
					scene.Draw.DrawBox( widgetBoundsBox, Color.Yellow, 2, false );
				}) );
			}
		}

		if ( dragging )
		{
			if ( !scene.Camera.Orthographic && dragging3d )
			{
				using ( var scr = new DrawToScreen( scene.Width, scene.Height ) )
				{
					var scrMin = scene.WorldToScreen( boxSelect.Start );
					var scrMax = scene.WorldToScreen( boxSelect.End );

					scrMin.Y = scene.Height - scrMin.Y;
					scrMax.Y = scene.Height - scrMax.Y;

					GL.Color4( Color.FromArgb( 128, Color.DodgerBlue ) ); // Set color with transparency
					GL.Begin( PrimitiveType.Quads ); // Draw filled rectangle
					GL.Vertex2( scrMin.X, scrMin.Y );
					GL.Vertex2( scrMax.X, scrMin.Y );
					GL.Vertex2( scrMax.X, scrMax.Y );
					GL.Vertex2( scrMin.X, scrMax.Y );
					GL.End();

					GL.Color4( Color.DodgerBlue ); // Set color for outline
					GL.Begin( PrimitiveType.LineLoop ); // Draw outline
					GL.Vertex2( scrMin.X, scrMin.Y );
					GL.Vertex2( scrMax.X, scrMin.Y );
					GL.Vertex2( scrMax.X, scrMax.Y );
					GL.Vertex2( scrMin.X, scrMax.Y );
					GL.End();
				}
			}
			else if ( scene.Camera.Orthographic && !dragging3d )
			{
				scene.Draw.DrawBox( boxSelect, Color.Yellow, 1.5f, true, false );
			}

			if ( selectionPreview.Any() )
			{
				foreach ( var obj in selectionPreview )
				{
					if ( obj is Solid previewSolid )
					{
						MapObjectRenderer.DrawFilledNoFucks( previewSolid.Faces, Color.FromArgb( 125, Color.Green ), true );
					}
				}
			}

			return;
		}

		var gizmoHasFocus = scene.Gizmos.IsHovered || scene.Gizmos.IsCaptured;
		var hoveredFaces = hoveredObject?.CollectFaces() ?? Enumerable.Empty<Face>();

		if ( !gizmoHasFocus && !dragging && hoveredFaces.Any() )
		{
			if ( scene.Camera.Orthographic )
			{
				GL.PushMatrix();
				GL.Translate( scene.Camera.Backward * 35000 );
			}
			GL.Color3( 1f, 1f, 1f );
			MapObjectRenderer.DrawWireframe( hoveredFaces, true, false );
			if ( scene.Camera.Orthographic )
			{
				GL.PopMatrix();
			}
		}
	}

	public override void Paint( Scene scene, UIElementPaintEvent e )
	{
		base.Paint( scene, e );

		if ( Document == null )
		{
			Debug.LogError( "Can't paint tool without document set." );
			return;
		}

		if ( RotateData != null )
		{
			var origin = RotateData.Value.origin;
			var rotation = RotateData.Value.rotation;
			var screenpos = scene.WorldToScreen( origin );
			var angle = MathF.Round( MathHelper.RadiansToDegrees( rotation.Angle ) );
			angle = MathF.Abs( angle );

			scene.Draw.DrawTextWithBackground( e.Canvas, $"{angle}deg", new Vector2( screenpos.X, screenpos.Y ), Color.Yellow, Color.Black );
		}

		if ( translateBox != null )
		{
			scene.Draw.PaintBoxSize( translateBox, Color.Yellow, e, true );
			return;
		}

		if ( widgetBoundsBox != null )
		{
			scene.Draw.PaintBoxSize( widgetBoundsBox, Color.Yellow, e, true );
			return;
		}

		if ( !Document.Selection.IsEmpty() )
		{
			var box = Document.Selection.GetSelectionBoundingBox();
			if ( box == null ) return;
			scene.Draw.PaintBoxSize( box, Color.Yellow, e );
		}
	}

	Vector3 GetWorldPosition( Scene scene, InputEvent e )
	{
		var ray = scene.ScreenToRay( e.LocalMousePosition );
		var startPos = new Vector3( ray.Origin.X, ray.Origin.Y, ray.Origin.Z );

		if ( scene.Camera.Orthographic )
			return startPos;

		var floatingPos = ray.Origin + ray.Direction.Normalized() * 32;
		startPos.X = floatingPos.X;
		startPos.Y = floatingPos.Y;
		startPos.Z = floatingPos.Z;

		return startPos;
	}

	void ExecuteTransform( string transformationName, IUnitTransformation transform, bool clone )
	{
		if ( clone ) transformationName += "-clone";

		var objects = Document.Selection.GetSelectedParents().ToList();
		var name = string.Format( $"{transformationName} {objects.Count} object{(objects.Count == 1 ? "" : "s")}" );

		var cad = new CreateEditDelete();
		var action = new ActionCollection( cad );

		if ( clone )
		{
			// Copy the selection, transform it, and reselect
			var copies = ClipboardManager.CloneFlatHeirarchy( Document, Document.Selection.GetSelectedObjects() ).ToList();
			foreach ( var mo in copies )
			{
				if ( mo is Entity ent )
				{
					ent.GameData = Document.GameData.Classes.FirstOrDefault( x => x.Name == ent.ClassName );
				}
				mo.Transform( transform, Document.Map.GetTransformFlags() );
			}
			cad.Create( Document.Map.WorldSpawn.ID, copies );
			var sel = new ChangeSelection( copies.SelectMany( x => x.FindAll() ), Document.Selection.GetSelectedObjects() );
			action.Add( sel );
		}
		else
		{
			cad.Edit( objects, new TransformEditOperation( transform, Document.Map.GetTransformFlags() ) );
		}

		Document.PerformAction( name, action );
	}

	IEnumerable<MapObject> BoxSelect( Scene scene, Box box )
	{
		var selectMin = scene.WorldToScreen( box.Start );
		var selectMax = scene.WorldToScreen( box.End );
		var everything = Document.Map.WorldSpawn.GetAllDescendants<MapObject>();

		foreach ( var eachthing in everything )
		{
			var bounds = eachthing.BoundingBox;
			var boundMin = scene.WorldToScreen( bounds.Start );
			var boundMax = scene.WorldToScreen( bounds.End );

			if ( boundMin.Z <= 0 || boundMax.Z <= 0 )
				continue;

			var boundCenter = new OpenTK.Mathematics.Vector3(
				(boundMin.X + boundMax.X) / 2,
				(boundMin.Y + boundMax.Y) / 2,
				(boundMin.Z + boundMax.Z) / 2
			);

			if ( IsWithinSelection( boundCenter, selectMin, selectMax ) )
			{
				yield return eachthing;
			}
		}

		bool IsWithinSelection( OpenTK.Mathematics.Vector3 center, OpenTK.Mathematics.Vector3 objMin, OpenTK.Mathematics.Vector3 objMax )
		{
			var normalizedSelectMin = new OpenTK.Mathematics.Vector3( Math.Min( objMin.X, objMax.X ), Math.Min( objMin.Y, objMax.Y ), Math.Min( objMin.Z, objMax.Z ) );
			var normalizedSelectMax = new OpenTK.Mathematics.Vector3( Math.Max( objMin.X, objMax.X ), Math.Max( objMin.Y, objMax.Y ), Math.Max( objMin.Z, objMax.Z ) );

			return (center.X >= normalizedSelectMin.X && center.X <= normalizedSelectMax.X) &&
				   (center.Y >= normalizedSelectMin.Y && center.Y <= normalizedSelectMax.Y);
		}
	}

}
