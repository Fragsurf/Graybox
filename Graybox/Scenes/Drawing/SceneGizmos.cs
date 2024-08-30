
namespace Graybox.Scenes.Drawing;

public class SceneGizmos
{

	public bool Enabled { get; set; }
	public bool DebugHitboxes { get; set; }
	public bool IsHovered { get; private set; }
	public bool IsCaptured { get; private set; }

	Scene Scene;
	List<GizmoCommand> Commands = new List<GizmoCommand>();
	string HoveredGizmo;
	string CapturedGizmo;

	Action<GizmoEvent> CurrentGizmoCallback;
	List<(Action<GizmoEvent>, GizmoEvent)> PendingCallbacks = new();
	string CurrentGizmoName;
	GizmoEvent CurrentEvent;

	public SceneGizmos( Scene scene )
	{
		Scene = scene;
	}

	public void CancelGizmos()
	{
		IsHovered = false;
		IsCaptured = false;
		CurrentGizmoName = string.Empty;
		CurrentGizmoCallback = null;
		CurrentEvent = default;
		HoveredGizmo = string.Empty;
		CapturedGizmo = string.Empty;
		translateOffset = null;
	}

	public void OnMouseDown( ref InputEvent e )
	{
		if ( e.Button == MouseButton.Left )
		{
			var gizmo = Pick( e.LocalMousePosition );
			if ( gizmo.cmd != null )
			{
				IsCaptured = true;
				CapturedGizmo = gizmo.hitbox.Name;
				CurrentEvent = new();
				CurrentEvent.Matrix = gizmo.hitbox.Matrix;
				CurrentEvent.MouseStart = e.LocalMousePosition;
				CurrentEvent.Completed = false;
			}
		}

		e.Handled = IsCaptured || IsHovered;
	}

	public void OnMouseUp( ref InputEvent e )
	{
		if ( !IsCaptured ) return;
		if ( e.Button != MouseButton.Left ) return;

		CurrentEvent.MousePrev = CurrentEvent.MouseCurrent;
		CurrentEvent.MouseCurrent = e.LocalMousePosition;
		CurrentEvent.Completed = true;

		var hb = FindCapturedHitbox();
		if ( hb?.Callback != null )
		{
			CurrentEvent.Matrix = hb.Matrix;
			PendingCallbacks.Add( (hb.Callback, CurrentEvent) );
		}

		IsCaptured = false;
		CapturedGizmo = string.Empty;

		e.Handled = IsCaptured || IsHovered;
	}

	public void OnMouseMove( ref InputEvent e )
	{
		if ( !IsCaptured )
		{
			var gizmo = Pick( e.LocalMousePosition );
			HoveredGizmo = gizmo.hitbox?.Name ?? string.Empty;
		}
		else
		{
			CurrentEvent.MousePrev = CurrentEvent.MouseCurrent;
			CurrentEvent.MouseCurrent = e.LocalMousePosition;

			var hb = FindCapturedHitbox();
			if ( hb?.Callback != null )
			{
				PendingCallbacks.Add( (hb.Callback, CurrentEvent) );
			}
		}

		IsHovered = !string.IsNullOrEmpty( HoveredGizmo );

		e.Handled = IsCaptured || IsHovered;
	}

	GizmoHitbox FindCapturedHitbox()
	{
		if ( !IsCaptured ) return null;

		foreach ( var cmd in Commands )
		{
			foreach ( var hb in cmd.Hitboxes )
			{
				if ( hb.Name == CapturedGizmo )
				{
					return hb;
				}
			}
		}

		return null;
	}

	(GizmoCommand cmd, GizmoHitbox hitbox) Pick( Vector2 localMousePosition )
	{
		var ray = Scene.ScreenToRay( localMousePosition );
		var origin = ray.Origin;
		var dir = ray.Direction.Normalized();
		var bestDist = float.MaxValue;
		var pickedCmd = (GizmoCommand)null;
		var pickedHitbox = (GizmoHitbox)null;

		foreach ( var command in Commands )
		{
			foreach ( var hb in command.Hitboxes )
			{
				if ( RayIntersectsOBB( origin, dir, hb, out float dist ) && dist < bestDist )
				{
					bestDist = dist;
					pickedCmd = command;
					pickedHitbox = hb;
				}
			}
		}

		return (pickedCmd, pickedHitbox);
	}

	public void Render()
	{
		if ( DebugHitboxes )
		{
			foreach ( var cmd in this.Commands )
			{
				foreach ( var hb in cmd.Hitboxes )
				{
					DrawHitbox( hb );
				}
			}
		}

		lock ( DrawCommands )
		{
			for ( int i = DrawCommands.Count - 1; i >= 0; i-- )
			{
				if ( DrawCommands[i].Execute() )
					DrawCommands.RemoveAt( i );
			}
		}

		foreach ( var cb in PendingCallbacks )
		{
			cb.Item1?.Invoke( cb.Item2 );
		}
		PendingCallbacks.Clear();

		Commands.Clear();
	}

	void TranslateCallback( GizmoEvent e, Vector3 axis, Vector3 center, Action<TranslateEvent> onTranslate = null, int handleIndex = -1 )
	{
		translateOffset = null;

		var matrix = e.Matrix;
		var translation = ScreenToWorldTranslation( e, axis, center ) ?? default;
		translation.HandleIndex = handleIndex;

		if ( e.Completed )
		{
			onTranslate?.Invoke( new TranslateEvent()
			{
				Completed = true,
				StartPosition = translation.StartPosition,
				NewPosition = translation.NewPosition,
				HandleIndex = handleIndex,
				TranslateMatrix = e.Matrix,
				TranslateAxis = axis
			} );
			return;
		}

		translateOffset = translation.TranslateDelta;
		onTranslate?.Invoke( translation );
	}

	public void RotationWidget( Vector3 center, Quaternion currentRotation, float scale, float snapAngle, Action<RotateEvent> onRotate = null )
	{
		if ( !Enabled ) return;
		snapAngle = MathHelper.DegreesToRadians( snapAngle );
		var command = new GizmoCommand();
		Commands.Add( command );
		Matrix4 translationMatrix = Matrix4.CreateTranslation( center );
		Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion( currentRotation );
		Matrix4 scaleMatrix = Matrix4.CreateScale( scale );
		Matrix4 transformMatrix = rotationMatrix * scaleMatrix * translationMatrix;
		GL.Disable( EnableCap.DepthTest );
		GL.PushMatrix();
		GL.MultMatrix( ref transformMatrix );
		GL.LineWidth( 2.0f );
		float radius = 1.0f;
		int segments = 32;

		// Get current rotation angles
		float startAngleX = 0, startAngleY = 0, startAngleZ = 0;
		float currentAngleX = 0, currentAngleY = 0, currentAngleZ = 0;
		if ( CapturedGizmo == "rotate_x" ) { startAngleX = CurrentEvent.StartAngle; currentAngleX = CurrentEvent.CurrentAngle; }
		if ( CapturedGizmo == "rotate_y" ) { startAngleY = CurrentEvent.StartAngle; currentAngleY = CurrentEvent.CurrentAngle; }
		if ( CapturedGizmo == "rotate_z" ) { startAngleZ = CurrentEvent.StartAngle; currentAngleZ = CurrentEvent.CurrentAngle; }

		if ( string.IsNullOrEmpty( CapturedGizmo ) || CapturedGizmo == "rotate_x" )
			DrawRotationRing( Vector3.UnitX, new Vector4( 1, 0, 0, 1 ), radius, segments, "rotate_x",
				e => RotateCallback( e, Vector3.UnitX, center, currentRotation, snapAngle, onRotate ), startAngleX, currentAngleX, snapAngle );
		if ( string.IsNullOrEmpty( CapturedGizmo ) || CapturedGizmo == "rotate_y" )
			DrawRotationRing( Vector3.UnitY, new Vector4( 0, 1, 0, 1 ), radius, segments, "rotate_y",
				e => RotateCallback( e, Vector3.UnitY, center, currentRotation, snapAngle, onRotate ), startAngleY, currentAngleY, snapAngle );
		if ( string.IsNullOrEmpty( CapturedGizmo ) || CapturedGizmo == "rotate_z" )
			DrawRotationRing( Vector3.UnitZ, new Vector4( 0, 0, 1, 1 ), radius, segments, "rotate_z",
				e => RotateCallback( e, Vector3.UnitZ, center, currentRotation, snapAngle, onRotate ), startAngleZ, currentAngleZ, snapAngle );

		foreach ( var hb in command.Hitboxes )
		{
			hb.Matrix = transformMatrix;
		}
		GL.PopMatrix();
		GL.Enable( EnableCap.DepthTest );
		GL.LineWidth( 1.0f );
	}

	private void DrawRotationRing( Vector3 axis, Vector4 color, float radius, int segments, string gizmoName, Action<GizmoEvent> callback, float startAngle, float currentAngle, float snapAngle )
	{
		CurrentGizmoName = gizmoName;
		CurrentGizmoCallback = callback;

		// Draw the main ring
		SetColor( color.X, color.Y, color.Z, color.W );
		GL.Begin( PrimitiveType.LineLoop );
		for ( int i = 0; i < segments; i++ )
		{
			float angle = (float)i / segments * MathF.PI * 2;
			Vector3 point = GetPointOnCircle( axis, angle, radius );
			GL.Vertex3( point );
		}
		GL.End();

		if ( IsCaptured && snapAngle > 0 )
		{
			GL.Begin( PrimitiveType.Lines );
			GL.Color4( 1.0f, 1.0f, 0f, 0.95f );
			for ( float angle = 0; angle < MathF.PI * 2; angle += snapAngle )
			{
				Vector3 point = GetPointOnCircle( axis, angle, radius );
				GL.Vertex3( point );
				GL.Vertex3( point * 1.1f ); // Slightly longer than the main ring
			}
			GL.End();
		}

		// Draw rotation visualization if gizmo is active
		if ( CapturedGizmo == gizmoName )
		{
			Vector3 startPoint = GetPointOnCircle( axis, startAngle, radius );
			Vector3 endPoint = GetPointOnCircle( axis, startAngle + currentAngle, radius );

			GL.Begin( PrimitiveType.Lines );
			GL.Color4( 1f, 1f, 1f, 1f );
			GL.Vertex3( Vector3.Zero );
			GL.Vertex3( startPoint );
			GL.End();

			GL.Begin( PrimitiveType.Lines );
			GL.Color4( 1f, 1f, 0f, 1f ); // Solid white
			GL.Vertex3( Vector3.Zero );
			GL.Vertex3( endPoint );
			GL.End();

			GL.Begin( PrimitiveType.LineStrip );
			GL.Color4( color );
			//SetColor( 1f, 1f, 0f, 1f ); // Yellow
			int arcSegments = Math.Max( 2, (int)(segments * Math.Abs( currentAngle ) / (2 * MathF.PI)) );
			for ( int i = 0; i <= arcSegments; i++ )
			{
				float angle = startAngle + (float)i / arcSegments * currentAngle;
				Vector3 point = GetPointOnCircle( axis, angle, radius );
				GL.Vertex3( point );
			}
			GL.End();
			DrawHandle( startPoint, 0.05f * radius );
			DrawHandle( endPoint, 0.05f * radius );
		}

		// Create hitbox (unchanged)
		float hitboxThickness = 0.05f * radius;
		List<Vector3> innerPoints = new List<Vector3>();
		List<Vector3> outerPoints = new List<Vector3>();
		for ( int i = 0; i < segments; i++ )
		{
			float angle = (float)i / segments * MathF.PI * 2;
			Vector3 point = GetPointOnCircle( axis, angle, radius );
			Vector3 toCenter = -point.Normalized();
			Vector3 inner = point + toCenter * hitboxThickness;
			Vector3 outer = point - toCenter * hitboxThickness;
			innerPoints.Add( inner );
			outerPoints.Add( outer );
		}

		Vector3 min = new Vector3( float.MaxValue );
		Vector3 max = new Vector3( float.MinValue );
		foreach ( var point in innerPoints.Concat( outerPoints ) )
		{
			min = Vector3.ComponentMin( min, point );
			max = Vector3.ComponentMax( max, point );
		}

		Vector3 offset = axis * 0.001f;
		min += offset;
		max += offset;

		var hitbox = new GizmoHitbox( CurrentGizmoName, CurrentGizmoCallback, min, max );
		Commands[Commands.Count - 1].Hitboxes.Add( hitbox );
	}

	private Vector3 GetPointOnCircle( Vector3 axis, float angle, float radius )
	{
		Vector3 u = Vector3.Cross( axis, Vector3.UnitY );
		if ( u.LengthSquared < 0.001f )
			u = Vector3.Cross( axis, Vector3.UnitZ );
		u = Vector3.Normalize( u );
		Vector3 v = Vector3.Cross( axis, u );

		return u * radius * MathF.Cos( angle ) + v * radius * MathF.Sin( angle );
	}

	private void RotateCallback( GizmoEvent e, Vector3 axis, Vector3 origin, Quaternion startRotation, float snapAngle, Action<RotateEvent> onRotate = null )
	{
		Vector3 startPoint = ProjectMouseOntoSphere( e.MouseStart, origin, axis );
		Vector3 currentPoint = ProjectMouseOntoSphere( e.MouseCurrent, origin, axis );

		Vector3 startVector = (startPoint - origin).Normalized();
		Vector3 currentVector = (currentPoint - origin).Normalized();

		if ( e.StartAngle == 0 )
		{
			e.StartAngle = CalculateAngleOnPlane( startVector, axis );

			if ( snapAngle > 0 )
			{
				e.StartAngle = (float)Math.Round( e.StartAngle / snapAngle ) * snapAngle;
			}
		}

		float currentAngle = CalculateAngleOnPlane( currentVector, axis );
		float deltaAngle = currentAngle - e.StartAngle;

		if ( deltaAngle > Math.PI ) deltaAngle -= 2 * (float)Math.PI;
		if ( deltaAngle < -Math.PI ) deltaAngle += 2 * (float)Math.PI;

		// Apply angle snapping if snapAngle is greater than zero
		if ( snapAngle > 0 )
		{
			deltaAngle = (float)Math.Round( deltaAngle / snapAngle ) * snapAngle;
		}

		CurrentEvent.StartAngle = e.StartAngle;
		CurrentEvent.CurrentAngle = deltaAngle;

		Matrix4 rotationMatrix = Matrix4.CreateFromAxisAngle( axis, deltaAngle );
		Matrix4 transformMatrix = Matrix4.CreateTranslation( -origin ) * rotationMatrix * Matrix4.CreateTranslation( origin );

		onRotate?.Invoke( new RotateEvent
		{
			Axis = axis,
			Angle = deltaAngle,
			StartAngle = e.StartAngle,
			Origin = origin,
			StartRotation = startRotation,
			RotationMatrix = rotationMatrix,
			TransformMatrix = transformMatrix,
			Completed = e.Completed
		} );
	}

	private float CalculateAngleOnPlane( Vector3 vector, Vector3 axis )
	{
		Vector3 projectedVector = vector.ProjectOntoPlane( axis ).Normalized();
		Vector3 referenceVector = Vector3.Cross( axis, Vector3.UnitY );
		if ( referenceVector.LengthSquared < 0.001f )
			referenceVector = Vector3.Cross( axis, Vector3.UnitZ );
		referenceVector = Vector3.Normalize( referenceVector );

		float angle = (float)Math.Atan2(
			Vector3.Dot( Vector3.Cross( referenceVector, projectedVector ), axis ),
			Vector3.Dot( referenceVector, projectedVector )
		);

		return angle;
	}

	private Vector3 ProjectMouseOntoSphere( Vector2 mousePosition, Vector3 sphereCenter, Vector3 axis )
	{
		Ray ray = Scene.ScreenToRay( mousePosition );
		Vector3 rayOrigin = ray.Origin;
		Vector3 rayDirection = ray.Direction.Normalized();

		// Project ray onto the plane perpendicular to the rotation axis
		Vector3 planeNormal = axis;
		float t = Vector3.Dot( sphereCenter - rayOrigin, planeNormal ) / Vector3.Dot( rayDirection, planeNormal );
		Vector3 pointOnPlane = rayOrigin + rayDirection * t;

		// Project point onto sphere
		Vector3 toPointOnPlane = pointOnPlane - sphereCenter;
		float distanceToPlane = toPointOnPlane.Length;
		float sphereRadius = 1f; // Assuming unit sphere, adjust if necessary

		if ( distanceToPlane > sphereRadius )
		{
			// If the point is outside the sphere, project it onto the sphere's edge
			return sphereCenter + toPointOnPlane.Normalized() * sphereRadius;
		}
		else
		{
			// If the point is inside the sphere, project it onto the sphere's surface
			float h = (float)Math.Sqrt( sphereRadius * sphereRadius - distanceToPlane * distanceToPlane );
			return pointOnPlane - rayDirection * h;
		}
	}

	public struct RotateEvent
	{
		public Vector3 Axis { get; set; }
		public Vector3 Origin { get; set; }
		public Quaternion StartRotation { get; set; }
		public Matrix4 RotationMatrix { get; set; }
		public Matrix4 TransformMatrix { get; set; }
		public bool Completed { get; set; }
		public float Angle { get; set; }
		public float StartAngle { get; set; }
		public float CurrentAngle { get; set; }
	}

	public void TranslateWidget2D( Vector3 mins, Vector3 maxs, Vector3 axis, Action<TranslateEvent> onTranslate = null )
	{
		if ( !Enabled ) return;

		var command = new GizmoCommand();
		Commands.Add( command );

		var pos = mins;
		var size = maxs - mins;

		if ( translateOffset.HasValue )
		{
			pos += translateOffset.Value;
		}

		if ( axis.AlmostEqual( Vector3.UnitZ ) )
		{
			CurrentGizmoName = "translate_xy";
			CurrentGizmoCallback = x => TranslateCallback( x, new Vector3( 1.0f, 1.0f, 0.0f ), mins, onTranslate );
		}
		else if ( axis.AlmostEqual( Vector3.UnitX ) )
		{
			CurrentGizmoName = "translate_yz";
			CurrentGizmoCallback = x => TranslateCallback( x, new Vector3( 0.0f, 1.0f, 1.0f ), mins, onTranslate );
			pos.Z += size.Z;
		}
		else if ( axis.AlmostEqual( Vector3.UnitY ) )
		{
			CurrentGizmoName = "translate_xz";
			CurrentGizmoCallback = x => TranslateCallback( x, new Vector3( 1.0f, 0.0f, 1.0f ), mins, onTranslate );
			pos.Z += size.Z;
		}
		else
		{
			return;
		}

		Matrix4 rotationMatrix = Matrix4.Identity;
		if ( axis != Vector3.UnitZ )
		{
			Vector3 rotAxis = Vector3.Cross( Vector3.UnitZ, axis );
			float dot = Vector3.Dot( Vector3.UnitZ, axis );
			float angle = (float)Math.Acos( dot );
			Quaternion rotation = Quaternion.FromAxisAngle( rotAxis.Normalized(), angle );
			rotationMatrix = Matrix4.CreateFromQuaternion( rotation );
		}
		else
		{
			rotationMatrix = Matrix4.CreateFromQuaternion( Quaternion.Identity );  // No rotation needed
		}

		Matrix4 translationMatrix = Matrix4.CreateTranslation( pos );
		Matrix4 scaleMatrix = Matrix4.CreateScale( size );
		Matrix4 transformMatrix = rotationMatrix * scaleMatrix * translationMatrix;

		GL.Disable( EnableCap.DepthTest );
		GL.Disable( EnableCap.CullFace );
		GL.PushMatrix();
		GL.MultMatrix( ref transformMatrix );

		SetColor2( 1.0f, 1.0f, 1.0f, 0.1f );  // Semi-transparent
		GL.Begin( PrimitiveType.Quads );
		GL.Vertex2( 0.0f, 1.0f );
		GL.Vertex2( 1.0f, 1.0f );
		GL.Vertex2( 1.0f, 0.0f );
		GL.Vertex2( 0.0f, 0.0f );
		GL.End();

		command.Hitboxes.Add( RectangleHitbox( new( 0, 0, 0 ), new( 1, 1, 1 ), 0.05f ) );

		foreach ( var hb in command.Hitboxes )
		{
			hb.Matrix = transformMatrix;
		}

		GL.PopMatrix();
		GL.Enable( EnableCap.DepthTest );
		GL.Enable( EnableCap.CullFace );
	}

	Vector3? translateOffset;
	public void TranslateWidget( Vector3 center, OpenTK.Mathematics.Quaternion rotation, float scale, Action<TranslateEvent> onTranslate = null )
	{
		if ( !Enabled ) return;

		var command = new GizmoCommand();
		Commands.Add( command );

		var pos = center;
		if ( translateOffset.HasValue )
			pos += translateOffset.Value;

		Matrix4 translationMatrix = Matrix4.CreateTranslation( pos );
		Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion( rotation );
		Matrix4 scaleMatrix = Matrix4.CreateScale( scale );
		Matrix4 transformMatrix = rotationMatrix * scaleMatrix * translationMatrix;

		// Apply transformation
		GL.Disable( EnableCap.DepthTest );
		GL.PushMatrix();
		GL.MultMatrix( ref transformMatrix );

		GL.LineWidth( 2.0f );

		scale = 1.0f;

		// Draw and compute hitboxes for each axis

		// X Axis (Red)
		CurrentGizmoName = "translate_x";
		CurrentGizmoCallback = x => TranslateCallback( x, Vector3.UnitX, center, onTranslate );
		SetColor( 1.0f, 0.0f, 0f );
		GL.Begin( PrimitiveType.Lines );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.Vertex3( scale, 0.0f, 0.0f );
		GL.End();
		command.Hitboxes.Add( LineHitbox( new Vector3( 0.25f * scale, 0.0f, 0.0f ), new Vector3( scale, 0.0f, 0.0f ), 0.05f * scale ) );
		command.Hitboxes.Add( DrawCone( new Vector3( scale, 0.0f, 0.0f ), new Vector3( 1.0f, 0.0f, 0.0f ), 0.1f * scale, 0.2f * scale ) );

		// Y Axis (Green)
		CurrentGizmoName = "translate_y";
		CurrentGizmoCallback = x => TranslateCallback( x, Vector3.UnitY, center, onTranslate );
		SetColor( 0f, 1.0f, 0f );
		GL.Begin( PrimitiveType.Lines );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.Vertex3( 0.0f, scale, 0.0f );
		GL.End();
		command.Hitboxes.Add( LineHitbox( new Vector3( 0.0f, 0.25f * scale, 0.0f ), new Vector3( 0.0f, scale, 0.0f ), 0.05f * scale ) );
		command.Hitboxes.Add( DrawCone( new Vector3( 0.0f, scale, 0.0f ), new Vector3( 0.0f, 1.0f, 0.0f ), 0.1f * scale, 0.2f * scale ) );

		// Z Axis (Blue)
		CurrentGizmoName = "translate_z";
		CurrentGizmoCallback = x => TranslateCallback( x, Vector3.UnitZ, center, onTranslate );
		SetColor( 0f, 0f, 1.0f );
		GL.Begin( PrimitiveType.Lines );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.Vertex3( 0.0f, 0.0f, scale );
		GL.End();
		command.Hitboxes.Add( LineHitbox( new Vector3( 0.0f, 0.0f, 0.25f * scale ), new Vector3( 0.0f, 0.0f, scale ), 0.05f * scale ) );
		command.Hitboxes.Add( DrawCone( new Vector3( 0.0f, 0.0f, scale ), new Vector3( 0.0f, 0.0f, 1.0f ), 0.1f * scale, 0.2f * scale ) );

		CurrentGizmoName = "translate_xy";
		CurrentGizmoCallback = x => TranslateCallback( x, new Vector3( 1.0f, 1.0f, 0.0f ), center, onTranslate );
		var doubleAxisPlaneScale = 0.2f;
		var xyColor = new Vector4( 0f, 0f, 1f, 1.0f );

		GL.LineWidth( 1.0f );
		GL.Disable( EnableCap.CullFace );

		SetColor( xyColor.X, xyColor.Y, xyColor.Z, 0.25f );
		GL.Begin( PrimitiveType.Quads );
		GL.Vertex3( doubleAxisPlaneScale * scale, 0.0f, 0.0f );
		GL.Vertex3( doubleAxisPlaneScale * scale, doubleAxisPlaneScale * scale, 0.0f );
		GL.Vertex3( 0.0f, doubleAxisPlaneScale * scale, 0.0f );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.End();

		// Opaque outline
		SetColor( xyColor.X, xyColor.Y, xyColor.Z, 1 );
		GL.Begin( PrimitiveType.LineLoop );
		GL.Vertex3( 0.0f, doubleAxisPlaneScale * scale, 0.0f );
		GL.Vertex3( doubleAxisPlaneScale * scale, doubleAxisPlaneScale * scale, 0.0f );
		GL.Vertex3( doubleAxisPlaneScale * scale, 0.0f, 0.0f );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.End();

		command.Hitboxes.Add( RectangleHitbox( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( doubleAxisPlaneScale * scale, doubleAxisPlaneScale * scale, 0.0f ), 0.05f * scale ) );

		// Translate YZ
		CurrentGizmoName = "translate_yz";
		CurrentGizmoCallback = x => TranslateCallback( x, new Vector3( 0.0f, 1.0f, 1.0f ), center, onTranslate );
		var yzColor = new Vector4( 1f, 0f, 0f, 1.0f );

		SetColor( yzColor.X, yzColor.Y, yzColor.Z, 0.25f );
		GL.Begin( PrimitiveType.Quads );
		GL.Vertex3( 0.0f, doubleAxisPlaneScale * scale, 0.0f );
		GL.Vertex3( 0.0f, doubleAxisPlaneScale * scale, doubleAxisPlaneScale * scale );
		GL.Vertex3( 0.0f, 0.0f, doubleAxisPlaneScale * scale );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.End();
		SetColor( yzColor.X, yzColor.Y, yzColor.Z, 1 );
		GL.Begin( PrimitiveType.LineLoop );
		GL.Vertex3( 0.0f, doubleAxisPlaneScale * scale, 0.0f );
		GL.Vertex3( 0.0f, doubleAxisPlaneScale * scale, doubleAxisPlaneScale * scale );
		GL.Vertex3( 0.0f, 0.0f, doubleAxisPlaneScale * scale );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.End();

		command.Hitboxes.Add( RectangleHitbox( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( 0.0f, doubleAxisPlaneScale * scale, doubleAxisPlaneScale * scale ), 0.05f * scale ) );

		// Translate XZ
		CurrentGizmoName = "translate_xz";
		CurrentGizmoCallback = x => TranslateCallback( x, new Vector3( 1.0f, 0.0f, 1.0f ), center, onTranslate );
		var xzColor = new Vector4( 0, 1.0f, 0, 1f );

		SetColor( xzColor.X, xzColor.Y, xzColor.Z, 0.25f );
		GL.Begin( PrimitiveType.Quads );
		GL.Vertex3( 0.0f, 0.0f, doubleAxisPlaneScale * scale );
		GL.Vertex3( doubleAxisPlaneScale * scale, 0.0f, doubleAxisPlaneScale * scale );
		GL.Vertex3( doubleAxisPlaneScale * scale, 0.0f, 0.0f );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.End();
		SetColor( xzColor.X, xzColor.Y, xzColor.Z, 1 );
		GL.Begin( PrimitiveType.LineLoop );
		GL.Vertex3( 0.0f, 0.0f, doubleAxisPlaneScale * scale );
		GL.Vertex3( doubleAxisPlaneScale * scale, 0.0f, doubleAxisPlaneScale * scale );
		GL.Vertex3( doubleAxisPlaneScale * scale, 0.0f, 0.0f );
		GL.Vertex3( 0.0f, 0.0f, 0.0f );
		GL.End();

		command.Hitboxes.Add( RectangleHitbox( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( doubleAxisPlaneScale * scale, 0.0f, doubleAxisPlaneScale * scale ), 0.05f * scale ) );

		foreach ( var hb in command.Hitboxes )
		{
			hb.Matrix = transformMatrix;
		}

		GL.PopMatrix();
		GL.Enable( EnableCap.DepthTest );
		GL.Enable( EnableCap.CullFace );
		GL.LineWidth( 1.0f );
	}

	public void BoundsWidget( Bounds box, Vector3 center, Quaternion rotation, float handleSize, Vector3 skipAxis, Action<TranslateEvent> onHandleDrag = null )
	{
		if ( !Enabled ) return;

		var command = new GizmoCommand();
		Commands.Add( command );

		Matrix4 translationMatrix = Matrix4.CreateTranslation( center );
		Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion( rotation );
		Matrix4 scaleMatrix = Matrix4.CreateScale( 1.0f );
		Matrix4 transformMatrix = rotationMatrix * scaleMatrix * translationMatrix;

		// Apply transformation
		GL.Disable( EnableCap.DepthTest );
		GL.PushMatrix();
		GL.MultMatrix( ref transformMatrix );

		GL.Disable( EnableCap.DepthTest );
		GL.Enable( EnableCap.Blend );

		var boundsColors = new GizmoColorOptions()
		{
			Normal = new Color4( 1.0f, 1.0f, 1.0f, 0.05f ),
			Hovered = new Color4( 1.0f, 1.0f, 1.0f, 0.35f ),
			Pressed = new Color4( 0.25f, 0.5f, 0.25f, 0f )
		};

		var idx = 0;
		foreach ( var (Center, Normal, Size) in box.GetFaceCentersNormalsAndSizes() )
		{
			var offset = Normal * (handleSize * 0.5f);
			var faceCenter = Center + offset;
			var faceNormal = Normal;
			var handleIndex = idx++;

			Scene.Gizmos.Line( center + faceCenter, center + faceCenter + faceNormal * 32f, Normal, 2 );

			var plane = new Plane( faceNormal, center + faceCenter );
			var ray = Scene.ScreenToRay( CurrentEvent.MouseStart );
			var translateOrigin = center;

			if ( Scene.Camera.Orthographic )
			{
				plane = new Plane( Scene.Camera.Forward, center );
			}

			if ( plane.Intersect( ray, out var intersection ) )
			{
				translateOrigin = intersection;
				//Scene.Gizmos.Line( intersection, intersection + faceNormal * 256, new( 1, 0, 0 ), 2 );
			}

			if ( skipAxis != default && faceNormal.IsParallel( skipAxis ) )
			{
				continue;
			}

			var newCenter = Center;
			var faceSize = Size;

			if ( translateOffset.HasValue && this.CapturedGizmo == CurrentGizmoName )
			{
				newCenter += translateOffset.Value;
			}

			CurrentGizmoCallback = x => TranslateCallback( x, faceNormal, translateOrigin, onHandleDrag, handleIndex );
			CurrentGizmoName = $"bounds_{handleIndex}";

			if ( translateOffset.HasValue && this.CapturedGizmo == CurrentGizmoName )
			{
				faceCenter += translateOffset.Value;
			}

			Vector3 scaledHandleSize;
			if ( Math.Abs( faceNormal.X ) > 0.5 )
				scaledHandleSize = new Vector3( handleSize, Size.X, Size.Y );
			else if ( Math.Abs( faceNormal.Y ) > 0.5 )
				scaledHandleSize = new Vector3( Size.X, handleSize, Size.Y );
			else
				scaledHandleSize = new Vector3( Size.X, Size.Y, handleSize );

			var start = faceCenter - scaledHandleSize * .5f;
			var end = faceCenter + scaledHandleSize * .5f;
			SetColor( boundsColors );
			DrawHandle( faceCenter, scaledHandleSize );
			command.Hitboxes.Add( RectangleHitbox( start, end, 0.05f ) );
		}

		foreach ( var hb in command.Hitboxes )
		{
			hb.Matrix = transformMatrix; // No transformation
		}

		GL.Enable( EnableCap.DepthTest );
		GL.PopMatrix();
	}

	void SetColor( GizmoColorOptions color )
	{
		if ( CapturedGizmo == CurrentGizmoName )
		{
			GL.Color4( color.Pressed.R, color.Pressed.G, color.Pressed.B, color.Pressed.A );
			return;
		}

		if ( HoveredGizmo == CurrentGizmoName )
		{
			GL.Color4( color.Hovered.R, color.Hovered.G, color.Hovered.B, color.Hovered.A );
			return;
		}

		GL.Color4( color.Normal.R, color.Normal.G, color.Normal.B, color.Normal.A );
	}

	private void DrawHandle( Vector3 position, float size ) => DrawHandle( position, new Vector3( size ) );
	private void DrawHandle( Vector3 position, Vector3 size )
	{
		Vector3 halfSize = size * 0.5f;

		GL.Begin( PrimitiveType.Quads );

		// +Z face
		GL.Vertex3( position.X - halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z );

		// -Z face
		GL.Vertex3( position.X - halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z );

		// -X face
		GL.Vertex3( position.X - halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z );

		// +X face
		GL.Vertex3( position.X + halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z );

		// +Y face
		GL.Vertex3( position.X + halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z );

		// -Y face
		GL.Vertex3( position.X - halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z );
		GL.Vertex3( position.X - halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z );
		GL.Vertex3( position.X + halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z );

		GL.End();
	}

	private GizmoHitbox SphereHitbox( Vector3 center, float radius )
	{
		return new GizmoHitbox( CurrentGizmoName, CurrentGizmoCallback, center - Vector3.One * radius, center + Vector3.One * radius );
	}

	void SetColor( float r, float g, float b, float a = 1.0f )
	{
		if ( CapturedGizmo == CurrentGizmoName )
		{
			GL.Color3( System.Drawing.Color.White );
			return;
		}

		if ( HoveredGizmo == CurrentGizmoName )
		{
			GL.Color3( System.Drawing.Color.Yellow );
			return;
		}

		GL.Color4( r, g, b, a );
	}

	void SetColor2( float r, float g, float b, float a = 1.0f )
	{
		if ( CapturedGizmo == CurrentGizmoName )
		{
			var col = System.Drawing.Color.White;
			GL.Color4( col.R, col.G, col.B, (byte)15 );
			return;
		}

		if ( HoveredGizmo == CurrentGizmoName )
		{
			var col = System.Drawing.Color.White;
			GL.Color4( col.R, col.G, col.B, (byte)50 );
			return;
		}

		GL.Color4( r, g, b, a );
	}

	public GizmoHitbox DrawCone( Vector3 baseCenter, Vector3 direction, float baseRadius, float height )
	{
		int numSegments = 16;
		Vector3[] circleVertices = new Vector3[numSegments];

		// Compute the circle vertices
		for ( int i = 0; i < numSegments; i++ )
		{
			float angle = (float)(i * 2 * Math.PI / numSegments);
			circleVertices[i] = new Vector3(
				baseRadius * (float)Math.Cos( angle ),
				baseRadius * (float)Math.Sin( angle ),
				0.0f );
		}

		// Apply rotation to align with the direction vector
		var q = OpenTK.Mathematics.Quaternion.FromAxisAngle( Vector3.Cross( Vector3.UnitZ, direction ), (float)Math.Acos( Vector3.Dot( Vector3.UnitZ, direction ) ) );
		for ( int i = 0; i < numSegments; i++ )
		{
			circleVertices[i] = Vector3.Transform( circleVertices[i], q );
		}

		// Determine the apex of the cone
		Vector3 apex = baseCenter + direction * height;

		// Initialize min and max points with the apex and baseCenter
		Vector3 min = Vector3.ComponentMin( baseCenter, apex );
		Vector3 max = Vector3.ComponentMax( baseCenter, apex );

		// Update min and max with the circle vertices
		for ( int i = 0; i < numSegments; i++ )
		{
			Vector3 vertex = baseCenter + circleVertices[i];
			min = Vector3.ComponentMin( min, vertex );
			max = Vector3.ComponentMax( max, vertex );
		}

		// Draw the cone
		GL.Begin( PrimitiveType.TriangleFan );
		GL.Vertex3( apex ); // The apex of the cone
		for ( int i = 0; i <= numSegments; i++ )
		{
			GL.Vertex3( baseCenter + circleVertices[i % numSegments] );
		}
		GL.End();

		// Draw the bottom cap
		GL.Begin( PrimitiveType.TriangleFan );
		GL.Vertex3( baseCenter );
		for ( int i = numSegments; i >= 0; i-- )
		{
			GL.Vertex3( baseCenter + circleVertices[i % numSegments] );
		}
		GL.End();

		return new GizmoHitbox( CurrentGizmoName, CurrentGizmoCallback, min, max );
	}

	GizmoHitbox RectangleHitbox( Vector3 start, Vector3 end, float thickness )
	{
		// Implementation for rectangle hitbox
		return new GizmoHitbox( CurrentGizmoName, CurrentGizmoCallback, start, end );
	}

	GizmoHitbox LineHitbox( Vector3 start, Vector3 end, float padding )
	{
		Vector3 min = Vector3.ComponentMin( start, end ) - new Vector3( padding );
		Vector3 max = Vector3.ComponentMax( start, end ) + new Vector3( padding );
		return new GizmoHitbox( CurrentGizmoName, CurrentGizmoCallback, min, max );
	}

	void DrawHitbox( GizmoHitbox hitbox )
	{
		var transformMatrix = hitbox.Matrix;

		GL.Disable( EnableCap.DepthTest );
		GL.PushMatrix();
		GL.MultMatrix( ref transformMatrix );

		Vector3 min = hitbox.Min;
		Vector3 max = hitbox.Max;

		GL.LineWidth( 1.0f );
		if ( hitbox.IsHovered ) GL.Color3( 0f, 1.0f, 0f );
		else GL.Color3( 1.0f, 1.0f, 1.0f );

		GL.Begin( PrimitiveType.LineLoop );

		// Bottom face
		GL.Vertex3( min.X, min.Y, min.Z );
		GL.Vertex3( max.X, min.Y, min.Z );
		GL.Vertex3( max.X, min.Y, max.Z );
		GL.Vertex3( min.X, min.Y, max.Z );

		GL.End();

		GL.Begin( PrimitiveType.LineLoop );

		// Top face
		GL.Vertex3( min.X, max.Y, min.Z );
		GL.Vertex3( max.X, max.Y, min.Z );
		GL.Vertex3( max.X, max.Y, max.Z );
		GL.Vertex3( min.X, max.Y, max.Z );

		GL.End();

		GL.Begin( PrimitiveType.Lines );

		// Vertical lines
		GL.Vertex3( min.X, min.Y, min.Z ); GL.Vertex3( min.X, max.Y, min.Z );
		GL.Vertex3( max.X, min.Y, min.Z ); GL.Vertex3( max.X, max.Y, min.Z );
		GL.Vertex3( max.X, min.Y, max.Z ); GL.Vertex3( max.X, max.Y, max.Z );
		GL.Vertex3( min.X, min.Y, max.Z ); GL.Vertex3( min.X, max.Y, max.Z );

		GL.End();
		GL.PopMatrix();

		GL.Enable( EnableCap.DepthTest );
	}

	public bool RayIntersectsOBB( Vector3 rayOrigin, Vector3 rayDir, GizmoHitbox hitbox, out float distance )
	{
		distance = float.MaxValue;

		// Transform the ray to the object's local space
		var obbMatrix = hitbox.Matrix;
		var inverseObbMatrix = !hitbox.Matrix.Determinant.IsNearlyZero()
			? Matrix4.Invert( obbMatrix )
			: hitbox.Matrix;

		Vector3 localRayOrigin = Vector3.TransformPosition( rayOrigin, inverseObbMatrix );
		Vector3 localRayDir = Vector3.TransformNormalInverse( rayDir, hitbox.Matrix );
		localRayDir.Normalize();

		// Calculate the min and max extents of the OBB
		Vector3 minExtents = hitbox.Min;
		Vector3 maxExtents = hitbox.Max;

		// Array of OBB axes in local space
		Vector3[] axes = { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };

		// Initialize tMin and tMax for the intersection intervals
		float tMin = float.MinValue;
		float tMax = float.MaxValue;

		// Iterate over each axis
		for ( int i = 0; i < 3; i++ )
		{
			Vector3 axis = axes[i];
			float e = Vector3.Dot( axis, localRayOrigin );
			float f = Vector3.Dot( localRayDir, axis );

			if ( Math.Abs( f ) > 0.001f )
			{
				float t1 = (minExtents[i] - e) / f;
				float t2 = (maxExtents[i] - e) / f;

				if ( t1 > t2 )
				{
					float temp = t1;
					t1 = t2;
					t2 = temp;
				}

				tMin = Math.Max( tMin, t1 );
				tMax = Math.Min( tMax, t2 );

				if ( tMax < tMin )
					return false;
			}
			else
			{
				if ( minExtents[i] > e || maxExtents[i] < e )
					return false;
			}
		}

		distance = tMax;

		return tMax > 0;
	}


	public class GizmoHitbox
	{
		public Vector3 Min { get; set; }
		public Vector3 Max { get; set; }
		public Matrix4 Matrix { get; set; }
		public bool IsHovered { get; set; }
		public string Name { get; set; }
		public Action<GizmoEvent> Callback { get; set; }

		public GizmoHitbox( string name, Action<GizmoEvent> callback, Vector3 min, Vector3 max )
		{
			Callback = callback;
			Name = name;
			Min = min;
			Max = max;
		}
	}

	private TranslateEvent? ScreenToWorldTranslation( GizmoEvent e, Vector3 axisConstraint, Vector3? translateOrigin = null )
	{
		var matrix = e.Matrix;
		var gizmoCenter = translateOrigin ?? matrix.ExtractTranslation();
		var gizmoRotation = matrix.ExtractRotation();
		var transformedAxisConstraint = Vector3.Transform( axisConstraint, gizmoRotation ).Normalized();

		var cameraPosition = Scene.Camera.Position;
		var cameraForward = Scene.Camera.Forward;

		Vector3 planeNormal;
		bool isTwoPlaneTranslation = axisConstraint.CountNonZeroComponents() == 2;

		if ( Scene.Camera.Orthographic )
		{
			planeNormal = -cameraForward;
		}
		else if ( isTwoPlaneTranslation )
		{
			// For two-plane translation, use the normal of the plane defined by the two axes
			planeNormal = Vector3.Cross(
				Vector3.Transform( Vector3.UnitX * axisConstraint.X + Vector3.UnitY * axisConstraint.Y, gizmoRotation ),
				Vector3.Transform( Vector3.UnitY * axisConstraint.Y + Vector3.UnitZ * axisConstraint.Z, gizmoRotation )
			).Normalized();
		}
		else
		{
			// For single axis, use a plane that contains both the axis and the camera direction
			planeNormal = Vector3.Cross( transformedAxisConstraint, Vector3.Cross( cameraForward, transformedAxisConstraint ) ).Normalized();
		}

		var plane = new Plane( planeNormal, gizmoCenter );
		var startRay = Scene.ScreenToRay( e.MouseStart );
		var currentRay = Scene.ScreenToRay( e.MouseCurrent );

		if ( !plane.Intersect( startRay, out Vector3 startPosition ) || !plane.Intersect( currentRay, out Vector3 currentPosition ) )
		{
			return null; // No valid intersection
		}

		Vector3 movement = currentPosition - startPosition;

		if ( !isTwoPlaneTranslation )
		{
			// Project movement onto the constrained axis for single-axis translation
			movement = Vector3.Dot( movement, transformedAxisConstraint ) * transformedAxisConstraint;
		}
		else
		{
			movement -= Vector3.Dot( movement, planeNormal ) * planeNormal;
		}

		Vector3 newPosition = gizmoCenter + movement;

		return new TranslateEvent()
		{
			StartPosition = gizmoCenter,
			NewPosition = newPosition,
			TranslateDelta = movement,
			TranslateAxis = axisConstraint,
			TranslateMatrix = e.Matrix
		};
	}

	private static Vector3 CalculatePlaneNormal( Vector3 axisConstraint )
	{
		Vector3 orthogonalVector;

		// Determine an orthogonal vector based on the axis constraint components
		if ( Math.Abs( axisConstraint.X ) > 0.9f && Math.Abs( axisConstraint.Y ) > 0.9f )
			orthogonalVector = Vector3.UnitX;
		else if ( Math.Abs( axisConstraint.X ) > 0.9f && Math.Abs( axisConstraint.Z ) > 0.9f )
			orthogonalVector = Vector3.UnitZ;
		else if ( Math.Abs( axisConstraint.Y ) > 0.9f && Math.Abs( axisConstraint.Z ) > 0.9f )
			orthogonalVector = Vector3.UnitY;
		else
			orthogonalVector = Math.Abs( axisConstraint.Z ) > 0.9f ? Vector3.UnitX : Vector3.UnitZ;

		Vector3 planeNormal = Vector3.Cross( axisConstraint, orthogonalVector );

		// If planeNormal is zero (unlikely but good to check), use another orthogonal vector
		if ( planeNormal.LengthSquared < 1e-6 )
		{
			orthogonalVector = orthogonalVector == Vector3.UnitX ? Vector3.UnitY : Vector3.UnitX;
			planeNormal = Vector3.Cross( axisConstraint, orthogonalVector );
		}

		return Vector3.Normalize( planeNormal );
	}



	static Vector3 ProjectOntoAxis( Vector3 point, Vector3 axis, Vector3 origin )
	{
		var toPoint = point - origin;
		var projection = Vector3.Dot( toPoint, axis ) * axis;
		return origin + projection;
	}

	private static Vector3 CalculatePlaneNormal( Vector3 axisConstraint, SceneCamera camera )
	{
		// Ensure axisConstraint is normalized
		axisConstraint = Vector3.Normalize( axisConstraint );

		// Define variables for camera directions
		var camFwd = camera.Forward;
		var camRight = camera.Right;
		var camUp = camera.Up;

		// Initialize the plane normal
		Vector3 planeNormal;

		// Calculate the plane normal based on camera type
		if ( camera.Orthographic )
		{
			// In orthographic mode, set the plane normal to face the camera directly
			planeNormal = -camera.Forward;
		}
		else
		{
			// Calculate the plane normal to face the camera in perspective mode
			planeNormal = Vector3.Cross( axisConstraint, camFwd );

			// If planeNormal is zero or too small, choose another orthogonal vector
			if ( planeNormal == Vector3.Zero || planeNormal.LengthSquared < 1e-6 )
			{
				planeNormal = Vector3.Cross( axisConstraint, camRight );

				if ( planeNormal == Vector3.Zero || planeNormal.LengthSquared < 1e-6 )
				{
					planeNormal = Vector3.Cross( axisConstraint, camUp );

					if ( planeNormal == Vector3.Zero || planeNormal.LengthSquared < 1e-6 )
					{
						planeNormal = Vector3.Cross( axisConstraint, Vector3.UnitY );
					}
				}
			}

			// Normalize the plane normal
			planeNormal = Vector3.Normalize( planeNormal );
		}

		return planeNormal;
	}

	//static Vector3? GetIntersectionPoint( Vector3 point1, Vector3 direction1, Vector3 point2, Vector3 direction2 )
	//{
	//	Vector3 crossDir = Vector3.Cross( direction1, direction2 );
	//	float denominator = crossDir.LengthSquared;

	//	if ( denominator == 0 )
	//	{
	//		return null;
	//	}

	//	Vector3 diff = point2 - point1;
	//	float t = Vector3.Dot( Vector3.Cross( diff, direction2 ), crossDir ) / denominator;
	//	return point1 + t * direction1;
	//}

	List<DrawCommand> DrawCommands = new List<DrawCommand>();
	public void Line( Vector3 start, Vector3 end, Vector3 color, float width = 1.0f, float duration = 0.0f )
	{
		var cmd = new LineDrawCommand()
		{
			Color = color,
			Duration = duration,
			Start = start,
			End = end,
			TimeCreated = DateTime.Now,
			Width = width
		};
		lock ( DrawCommands ) DrawCommands.Add( cmd );
	}

	public void WireBox( Vector3 mins, Vector3 maxs, Vector4 color, float duration )
	{
		var cmd = new WireBoxDrawCommand()
		{
			Color = color,
			Duration = duration,
			Mins = mins,
			Maxs = maxs,
			TimeCreated = DateTime.Now
		};
		lock ( DrawCommands ) DrawCommands.Add( cmd );
	}

	public void Box( Vector3 mins, Vector3 maxs, Vector4 color, float duration = 0f )
	{
		var cmd = new BoxDrawCommand()
		{
			Color = color,
			Duration = duration,
			Mins = mins,
			Maxs = maxs,
			TimeCreated = DateTime.Now
		};
		lock ( DrawCommands ) DrawCommands.Add( cmd );
	}

	public void Sphere( Vector3 center, float radius, Vector3 color, int slices = 16, int stacks = 16, float duration = 0.0f )
	{
		var cmd = new SphereDrawCommand()
		{
			Center = center,
			Color = color,
			Duration = duration,
			Radius = radius,
			Slices = slices,
			Stacks = stacks,
			TimeCreated = DateTime.Now
		};
		lock ( DrawCommands ) DrawCommands.Add( cmd );
	}

	public void GridPlane( Vector3 normal, Vector3 center, Vector4 color, float size = 256.0f, float duration = 0.0f )
	{
		var command = new GridPlaneDrawCommand()
		{
			Center = center,
			Color = color,
			Normal = normal,
			Size = size,
			Duration = duration,
			TimeCreated = DateTime.Now
		};

		lock ( DrawCommands ) DrawCommands.Add( command );
	}


	public void SolidPlane( Vector3 normal, Vector3 center, Vector4 color, float size = 256.0f, float duration = 0.0f )
	{
		var command = new PlaneDrawCommand()
		{
			Center = center,
			Color = color,
			Normal = normal,
			Size = size,
			Duration = duration,
			TimeCreated = DateTime.Now
		};

		lock ( DrawCommands ) DrawCommands.Add( command );
	}

	public struct TranslateEvent
	{
		public Vector3 StartPosition { get; set; }
		public Vector3 NewPosition { get; set; }
		public Vector3 TranslateDelta { get; set; }
		public Vector3 TranslateAxis { get; set; }
		public Matrix4 TranslateMatrix { get; set; }
		public bool Completed { get; set; }
		public int HandleIndex { get; set; }
	}

	public struct GizmoEvent
	{
		public Vector2 MouseStart { get; set; }
		public Vector2 MousePrev { get; set; }
		public Vector2 MouseCurrent { get; set; }
		public Matrix4 Matrix { get; set; }
		public bool Completed { get; set; }
		public int HandleIndex { get; set; }
		public float StartAngle { get; set; }
		public float CurrentAngle { get; set; }
	}

	class GizmoCommand
	{
		public List<GizmoHitbox> Hitboxes = new List<GizmoHitbox>();
		public bool InputHandled;
	}

	class BoxDrawCommand : DrawCommand
	{

		public Vector3 Mins;
		public Vector3 Maxs;
		public Vector4 Color;

		protected override void OnExecute()
		{
			GL.Color4( Color );

			GL.Begin( PrimitiveType.Quads );
			// Bottom face
			GL.Vertex3( Mins.X, Mins.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Mins.X, Maxs.Y, Mins.Z );

			// Top face
			GL.Vertex3( Mins.X, Maxs.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Maxs.Z );
			GL.Vertex3( Mins.X, Mins.Y, Maxs.Z );

			// Front face
			GL.Vertex3( Mins.X, Mins.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Mins.Z );
			GL.Vertex3( Mins.X, Mins.Y, Mins.Z );

			// Back face
			GL.Vertex3( Mins.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Maxs.Z );
			GL.Vertex3( Mins.X, Maxs.Y, Maxs.Z );

			// Left face
			GL.Vertex3( Mins.X, Mins.Y, Mins.Z );
			GL.Vertex3( Mins.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Mins.X, Maxs.Y, Maxs.Z );
			GL.Vertex3( Mins.X, Mins.Y, Maxs.Z );

			// Right face
			GL.Vertex3( Maxs.X, Mins.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Mins.Z );

			GL.End();
		}

	}

	class WireBoxDrawCommand : DrawCommand
	{

		public Vector3 Mins;
		public Vector3 Maxs;
		public Vector4 Color;

		protected override void OnExecute()
		{
			GL.Color4( Color );
			GL.Begin( PrimitiveType.LineLoop );
			// Bottom face
			GL.Vertex3( Mins.X, Mins.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Mins.X, Maxs.Y, Mins.Z );
			GL.End();

			GL.Begin( PrimitiveType.LineLoop );
			// Top face
			GL.Vertex3( Mins.X, Mins.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Maxs.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Maxs.Z );
			GL.Vertex3( Mins.X, Maxs.Y, Maxs.Z );
			GL.End();

			GL.Begin( PrimitiveType.Lines );
			// Connecting edges
			GL.Vertex3( Mins.X, Mins.Y, Mins.Z );
			GL.Vertex3( Mins.X, Mins.Y, Maxs.Z );

			GL.Vertex3( Maxs.X, Mins.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Mins.Y, Maxs.Z );

			GL.Vertex3( Maxs.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Maxs.X, Maxs.Y, Maxs.Z );

			GL.Vertex3( Mins.X, Maxs.Y, Mins.Z );
			GL.Vertex3( Mins.X, Maxs.Y, Maxs.Z );
			GL.End();
		}

	}

	class PlaneDrawCommand : DrawCommand
	{
		public Vector3 Normal;
		public Vector3 Center;
		public Vector4 Color;
		public float Size;

		protected override void OnExecute()
		{
			GL.Disable( EnableCap.DepthTest );
			GL.Disable( EnableCap.CullFace );
			GL.Color4( Color );

			// Calculate tangent vectors
			Vector3 tangent1 = Vector3.Cross( Normal, Vector3.UnitY );
			if ( tangent1.Length < 0.001f ) // If Normal is parallel to UnitY
				tangent1 = Vector3.Cross( Normal, Vector3.UnitZ );
			tangent1 = Vector3.Normalize( tangent1 );
			Vector3 tangent2 = Vector3.Cross( Normal, tangent1 );

			GL.Begin( PrimitiveType.Quads );
			GL.Vertex3( Center - tangent1 * Size + tangent2 * Size );
			GL.Vertex3( Center + tangent1 * Size + tangent2 * Size );
			GL.Vertex3( Center + tangent1 * Size - tangent2 * Size );
			GL.Vertex3( Center - tangent1 * Size - tangent2 * Size );
			GL.End();
			GL.Enable( EnableCap.CullFace );
			GL.Enable( EnableCap.DepthTest );
		}
	}

	class GridPlaneDrawCommand : DrawCommand
	{
		public Vector3 Normal;
		public Vector3 Center;
		public Vector4 Color;
		public float Size;

		protected override void OnExecute()
		{
			GL.Disable( EnableCap.DepthTest );
			GL.Color4( Color );

			// Calculate tangent vectors
			Vector3 tangent1 = Vector3.Cross( Normal, Vector3.UnitY );
			if ( tangent1.Length < 0.001f ) // If Normal is parallel to UnitY
				tangent1 = Vector3.Cross( Normal, Vector3.UnitZ );
			tangent1 = Vector3.Normalize( tangent1 );
			Vector3 tangent2 = Vector3.Cross( Normal, tangent1 );

			GL.Begin( PrimitiveType.Lines );
			for ( int i = -10; i <= 10; ++i )
			{
				Vector3 offset1 = tangent1 * i * Size / 10;
				Vector3 offset2 = tangent2 * i * Size / 10;
				// Lines along tangent1
				GL.Vertex3( Center + offset1 - tangent2 * Size );
				GL.Vertex3( Center + offset1 + tangent2 * Size );
				// Lines along tangent2
				GL.Vertex3( Center - tangent1 * Size + offset2 );
				GL.Vertex3( Center + tangent1 * Size + offset2 );
			}
			GL.End();

			GL.Enable( EnableCap.DepthTest );
		}
	}

	class SphereDrawCommand : DrawCommand
	{
		public Vector3 Center;
		public float Radius;
		public Vector3 Color;
		public int Slices;
		public int Stacks;

		protected override void OnExecute()
		{
			GL.Color3( Color );
			GL.PushMatrix();
			GL.Translate( Center );
			GL.Scale( Radius, Radius, Radius );
			GL.Begin( PrimitiveType.QuadStrip );
			for ( int i = 0; i <= Stacks; ++i )
			{
				double lat0 = Math.PI * (-0.5 + (double)(i - 1) / Stacks);
				double z0 = Math.Sin( lat0 );
				double zr0 = Math.Cos( lat0 );

				double lat1 = Math.PI * (-0.5 + (double)i / Stacks);
				double z1 = Math.Sin( lat1 );
				double zr1 = Math.Cos( lat1 );

				for ( int j = 0; j <= Slices; ++j )
				{
					double lng = 2 * Math.PI * (double)(j - 1) / Slices;
					double x = Math.Cos( lng );
					double y = Math.Sin( lng );

					GL.Vertex3( x * zr0, y * zr0, z0 );
					GL.Vertex3( x * zr1, y * zr1, z1 );
				}
			}
			GL.End();
			GL.PopMatrix();
		}
	}

	class LineDrawCommand : DrawCommand
	{
		public Vector3 Start;
		public Vector3 End;
		public Vector3 Color;
		public float Width;

		protected override void OnExecute()
		{
			GL.LineWidth( Width );
			GL.Color3( Color );
			GL.Begin( PrimitiveType.Lines );
			GL.Vertex3( Start );
			GL.Vertex3( End );
			GL.End();
		}
	}

	class DrawCommand
	{

		public float Duration;
		public DateTime TimeCreated;

		protected virtual void OnExecute() { }
		public bool Execute()
		{
			OnExecute();
			float timeElapsed = (float)(DateTime.Now - TimeCreated).TotalSeconds;
			return timeElapsed > Duration;
		}

	}

	struct GizmoColorOptions
	{

		public GizmoColorOptions()
		{

		}

		public GizmoColorOptions( Color4 normal, Color4 hovered, Color4 pressed )
		{
			Normal = normal;
			Hovered = hovered;
			Pressed = pressed;
		}

		public Color4 Normal { get; set; } = Color4.White;
		public Color4 Hovered { get; set; } = Color4.White;
		public Color4 Pressed { get; set; } = Color4.White;

	}

}
