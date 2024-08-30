
using Graybox.Editor.Documents;
using Graybox.Scenes;

namespace Graybox.Editor.Widgets;

internal class EditorCameraController
{

	SceneWidget SceneWidget;
	Scene Scene => SceneWidget.Scene;

	float moveSpeed = 512;
	float MoveSpeed
	{
		get => moveSpeed;
		set => moveSpeed = MathHelper.Clamp( value, 32f, 4096f );
	}

	float mouseSensitivity = .1f;
	float MouseSensitivity
	{
		get => mouseSensitivity;
		set => mouseSensitivity = MathHelper.Clamp( value, 0.05f, 5f );
	}

	public bool FreeLooking { get; private set; }

	float pitch = 0f;
	float yaw = 0f;
	bool freeLookToggle;

	public EditorCameraController( SceneWidget sceneWidget )
	{
		SceneWidget = sceneWidget;
	}

	public void OnKeyDown( ref InputEvent e )
	{
		if ( FreeLooking )
		{
			e.Handled = true;
		}

		if ( e.Key == Key.Z )
		{
			freeLookToggle = !freeLookToggle;
			e.Handled = true;
		}

		if ( e.Key == Key.F )
		{
			FocusOnSelection();
			e.Handled = true;
		}

		if ( e.Key == Key.Escape )
		{
			freeLookToggle = false;
		}
	}

	public void DisableFreeLook()
	{
		freeLookToggle = false;
	}

	public void Update( FrameInfo frameInfo )
	{
		FreeLooking = false;

		var screenMousePos = Input.MousePosition;
		var localMousePos = SceneWidget.ScreenToLocal( screenMousePos );

		if ( !SceneWidget.IsFocused ) return;

		if ( Input.MouseScroll.Y != 0 )
		{
			if ( Scene.Camera.Orthographic )
			{
				var before = Scene.ScreenToWorld( (int)localMousePos.X, (int)localMousePos.Y );
				var zoomMultiplier = 1.2f; // Graybox.Settings.View.ScrollWheelZoomMultiplier
				Scene.Camera.OrthographicZoom *= MathF.Pow( zoomMultiplier, (Input.MouseScroll.Y < 0 ? 1 : -1) );
				var after = Scene.ScreenToWorld( (int)localMousePos.X, (int)localMousePos.Y );
				var shift = before - after;
				Scene.Camera.Position += shift;
			}
			else
			{
				Scene.Camera.Position += Scene.Camera.Forward * Input.MouseScroll.Y * 250;
			}
		}

		FreeLooking = Input.IsDown( MouseButton.Right ) || freeLookToggle;

		if ( FreeLooking )
		{
			var left = Input.IsDown( Key.A ) ? 1 : 0;
			var right = Input.IsDown( Key.D ) ? 1 : 0;
			var forward = Input.IsDown( Key.W ) ? 1 : 0;
			var backward = Input.IsDown( Key.S ) ? 1 : 0;

			var moveVector = new Vector3( forward - backward, left - right, 0 ).Normalized();
			if ( moveVector.Length > 0 )
			{
				var moveDir = (Scene.Camera.Rotation * moveVector).Normalized();
				var speed = MoveSpeed;

				if ( Input.ShiftModifier )
					speed *= 2f;
				if ( Input.ControlModifier )
					speed /= 2f;

				Scene.Camera.Position += moveDir * speed * frameInfo.DeltaTime;
			}

			if ( !Scene.Camera.Orthographic )
			{
				var mouseLook = Input.MouseDelta;
				if ( mouseLook.X != 0 )
				{
					var mouseDelta = Input.MouseDelta;
					yaw -= mouseDelta.X * MouseSensitivity;
					pitch += mouseDelta.Y * MouseSensitivity;
					pitch = Math.Clamp( pitch, -80, 80 );
					Scene.Camera.LocalEulerAngles = new Vector3( 0, pitch, yaw );
				}
			}

			EditorWindow.Instance.UpdateACoupleFrames( 30 );
		}

		if ( Input.IsDown( MouseButton.Middle ) || (Scene.Camera.Orthographic && (Input.IsDown( MouseButton.Right ) || FreeLooking)) )
		{
			var cameraForward = Scene.Camera.Forward;
			var mouseDelta = Input.MouseDelta;
			var previousPosition = localMousePos - new Vector2( mouseDelta.X, mouseDelta.Y );

			if ( Scene.Camera.Orthographic )
			{
				var prevWorldPosition = Scene.ScreenToWorld( (int)previousPosition.X, (int)previousPosition.Y );
				var newWorldPosition = Scene.ScreenToWorld( (int)localMousePos.X, (int)localMousePos.Y );
				Scene.Camera.Position -= newWorldPosition - prevWorldPosition;
			}
			else
			{
				var depth = 1024.0f;
				var prevRay = Scene.ScreenToRay( (int)previousPosition.X, (int)previousPosition.Y );
				var currRay = Scene.ScreenToRay( (int)localMousePos.X, (int)localMousePos.Y );

				var trace = Scene.Physics.Trace( currRay );
				if ( trace.Hit )
				{
					depth = Vector3.Distance( trace.Position, currRay.Origin );
					Scene.Gizmos.Sphere( trace.Position, 10f, Vector3.UnitZ );
				}

				var prevWorldPosition = prevRay.Origin + prevRay.Direction * depth;
				var newWorldPosition = currRay.Origin + currRay.Direction * depth;
				var movementVector = prevWorldPosition - newWorldPosition;
				var forwardComponent = Vector3.Dot( movementVector, cameraForward ) * cameraForward;
				var planarMovementVector = movementVector - forwardComponent;

				Scene.Camera.Position += planarMovementVector;
			}
		}
	}

	public void FocusOnSelection()
	{
		var bbox = DocumentManager.CurrentDocument?.Selection?.GetSelectionBoundingBox();
		if ( bbox == null || bbox.Width <= 0 || bbox.Height <= 0 ) return;

		var center = bbox.Center;

		if ( Scene.Camera.Orthographic )
		{
			var scale = Math.Max( bbox.Width / Scene.Camera.OrthographicWidth, bbox.Height / Scene.Camera.OrthographicHeight );
			Scene.Camera.OrthographicZoom = scale * 2f;

			var cameraForward = Scene.Camera.Forward;
			Scene.Camera.Position = bbox.Center - 50000 * Scene.Camera.Forward;
		}
		else
		{
			var cameraDistance = 2.0f;
			var objectSizes = bbox.End - bbox.Start;
			var objectSize = MathF.Max( objectSizes.X, MathF.Max( objectSizes.Y, objectSizes.Z ) );
			var cameraView = 2.0f * MathF.Tan( 0.5f * MathHelper.DegreesToRadians( Scene.Camera.FieldOfView ) );
			var distance = cameraDistance * objectSize / cameraView;
			distance += 0.5f * objectSize;
			Scene.Camera.Position = bbox.Center - distance * Scene.Camera.Forward;
		}
	}

}
