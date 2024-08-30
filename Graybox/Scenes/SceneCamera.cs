using System;

namespace Graybox.Scenes
{
	public class SceneCamera
	{
		public Vector3 Position { get; set; }

		private Quaternion worldRotation = LookRotation( Vector3.UnitX, Vector3.UnitZ );
		private Vector3 localEulerRadians;

		public Quaternion Rotation
		{
			get => FinalRotation();
		}

		public Vector3 Forward
		{
			get => (FinalRotation() * Vector3.UnitX).Normalized();
			set => localEulerRadians = LookRotation( value, Vector3.UnitZ ).ToEulerAngles();
		}

		public Vector3 Backward => -Forward;
		public Vector3 Right => (FinalRotation() * -Vector3.UnitY).Normalized();
		public Vector3 Up => (FinalRotation() * Vector3.UnitZ).Normalized();

		public Vector3 LocalEulerAngles
		{
			get => new Vector3( MathHelper.RadiansToDegrees( localEulerRadians.X ), MathHelper.RadiansToDegrees( localEulerRadians.Y ), MathHelper.RadiansToDegrees( localEulerRadians.Z ) );
			set => localEulerRadians = new Vector3( MathHelper.DegreesToRadians( value.X ), MathHelper.DegreesToRadians( value.Y ), MathHelper.DegreesToRadians( value.Z ) );
		}

		public bool Orthographic { get; set; } = false;
		public float OrthographicZoom { get; set; } = 2.5f;
		public float OrthographicWidth { get; set; } = 250;
		public float OrthographicHeight { get; set; } = 250;
		public float AspectRatio { get; set; } = 1.0f;
		public float FieldOfView { get; set; } = 60;
		public float NearClip { get; set; } = 1f;
		public float FarClip { get; set; } = 8000f;

		private Quaternion FinalRotation()
		{
			var yawRotation = Quaternion.FromAxisAngle( Vector3.UnitZ, localEulerRadians.Z );
			var pitchRotation = Quaternion.FromAxisAngle( Vector3.UnitY, localEulerRadians.Y );
			return yawRotation * pitchRotation * worldRotation;
		}

		public Matrix4 GetProjectionMatrix()
		{
			var ratio = AspectRatio;
			if ( ratio <= 0 ) ratio = 1;

			var near = MathHelper.Clamp( NearClip, 0.1f, 10f );
			var far = MathHelper.Clamp( FarClip, near + 1f, 100000 );
			var zoom = MathHelper.Clamp( OrthographicZoom, 0.001f, 100f );
			var fov = MathHelper.Clamp( FieldOfView, 5, 180 );

			if ( Orthographic )
			{
				float width = OrthographicWidth * zoom * ratio;
				float height = OrthographicHeight * zoom;

				return Matrix4.CreateOrthographic( width, height, near, far );
			}

			return Matrix4.CreatePerspectiveFieldOfView( MathHelper.DegreesToRadians( fov ), ratio, near, far );
		}

		public Matrix4 GetViewMatrix()
		{
			Vector3 up = Vector3.UnitZ;

			if ( Math.Abs( Vector3.Dot( Forward, up ) ) > 0.99f )
				up = Vector3.UnitX;

			return Matrix4.LookAt( Position, Position + Forward, up );
		}

		private static Quaternion LookRotation( Vector3 forward, Vector3 up )
		{
			forward.Normalize();

			if ( Vector3.Cross( forward, up ).LengthSquared < 0.0001f )
			{
				var arbitrary = MathF.Abs( Vector3.Dot( forward, new Vector3( 0, 0, 1 ) ) ) < 0.9999f ? new Vector3( 0, 0, 1 ) : new Vector3( 0, 1, 0 );
				up = Vector3.Cross( forward, arbitrary ).Normalized();
			}

			var right = Vector3.Cross( up, forward ).Normalized();
			up = Vector3.Cross( forward, right ).Normalized();

			var rotationMatrix = new Matrix3(
				forward.X, right.X, up.X,
				forward.Y, right.Y, up.Y,
				forward.Z, right.Z, up.Z
			);

			return Quaternion.FromMatrix( rotationMatrix );
		}
	}
}
