
using Graybox.GameSystem;
using Graybox.Scenes;
using Graybox.Scenes.Physics;

namespace Graybox.Bunnyhop
{
	public class BhopMovement
	{

		public float MoveSpeed = 320f;
		public float MaxVelocity = 3500f;
		public float Gravity = 800;
		public float JumpPower = 320f;
		public float Acceleration = 10f;
		public float AirAcceleration = 1500f;
		public float StopSpeed = 80f;
		public float Friction = 4f;
		public float AirCap = 30f;
		public float MouseSensitivity = 0.5f;
		public float EyeHeight = 72;

		public bool Noclip = true;

		float pitch = 0.0f;
		float yaw = 0f;
		GrayboxGame GameSystem;
		SceneCamera Camera;
		Vector3 Position;
		Vector3 LastPosition;

		public bool Grounded { get; private set; }
		public Vector3 GroundNormal { get; private set; }
		public Vector3 Velocity { get; private set; }

		public BhopMovement( GrayboxGame gameSystem )
		{
			GameSystem = gameSystem;
			Camera = gameSystem.ActiveScene.Camera;
			Position = new Vector3( 0, 0, 200 );
		}

		public void Update()
		{
			if ( Input.JustPressed( Key.R ) )
			{
				Noclip = !Noclip;
			}

			Camera.Position = Vector3.Lerp( LastPosition, Position, GameSystem.TickAlpha );
			Camera.Position = Camera.Position.WithZ( Camera.Position.Z + EyeHeight );

			var mouseDelta = Input.MouseDelta;

			yaw -= mouseDelta.X * MouseSensitivity * .022f;
			pitch += mouseDelta.Y * MouseSensitivity * .022f;
			pitch = Math.Clamp( pitch, -80, 80 );
			Camera.LocalEulerAngles = new Vector3( 0, pitch, yaw );
		}

		public void Tick()
		{
			LastPosition = Position;

			CheckForGround();
			CheckJump();

			var inputVector = GetInputVector();
			var wishDir = inputVector.Normalized();
			var wishSpeed = inputVector.Length;

			if ( float.IsNaN( wishDir.X ) || float.IsNaN( wishDir.Y ) || float.IsNaN( wishDir.Z ) )
			{
				wishDir = Vector3.Zero;
			}

			if ( Noclip )
			{
				Velocity = wishDir * 1500f;
			}
			else
			{
				if ( Grounded )
				{
					if ( wishSpeed > 0 ) ApplyGroundAcceleration( wishDir, wishSpeed, Acceleration, GameSystem.FixedDeltaTime, 1f );
					ClampVelocity( MoveSpeed );
					ApplyFriction( StopSpeed, Friction, GameSystem.FixedDeltaTime );
					Velocity = Velocity.WithZ( 0 );
				}
				else
				{
					if ( wishSpeed > 0 ) ApplyAirAcceleration( wishDir, wishSpeed, AirAcceleration, AirCap, GameSystem.FixedDeltaTime );
					Velocity += 800 * -Vector3.UnitZ * GameSystem.FixedDeltaTime;
				}
			}

			Position += Velocity * GameSystem.FixedDeltaTime;

			ResolveCollisions();
		}

		Vector3 GetInputVector()
		{
			var result = new Vector3();

			if ( Input.IsDown( Key.A ) )
				result -= Camera.Right;
			if ( Input.IsDown( Key.D ) )
				result += Camera.Right;
			if ( Input.IsDown( Key.W ) )
				result += Camera.Forward;
			if ( Input.IsDown( Key.S ) )
				result -= Camera.Forward;

			if ( result.Length == 0 )
				return Vector3.Zero;

			return result.Normalized() * MoveSpeed;
		}

		void ResolveCollisions()
		{
			var boxMins = new Vector3( -16, -16, 0 );
			var boxMaxs = new Vector3( 16, 16, 72 );

			foreach ( var penetration in GameSystem.ActiveScene.Physics.OverlapBox( Position, boxMins, boxMaxs ) )
			{
				Velocity = ClipVelocity( Velocity, penetration.SeparationVector, 1.0f );
				Position += penetration.SeparationVector.Normalized() * penetration.Distance;
			}
		}

		void CheckForGround()
		{
			Grounded = false;

			var result = TraceBBox( Position + Vector3.UnitZ, Position - Vector3.UnitZ * 2 );
			Grounded = result.Hit && result.Normal.Z >= 0.71f;

			if ( Grounded )
			{
				GroundNormal = result.Normal;
				Position = Position.WithZ( result.Position.Z + 1 );
			}
			else
			{
				GroundNormal = Vector3.Zero;
			}
		}

		TraceResult TraceBBox( Vector3 start, Vector3 end )
		{
			var boxMins = new Vector3( -16, -16, 0 );
			var boxMaxs = new Vector3( 16, 16, 72 );

			return GameSystem.ActiveScene.Physics.TraceBox( boxMins, boxMaxs, start, end );
		}

		private void ApplyGroundAcceleration( Vector3 wishDir, float wishSpeed, float accel, float deltaTime, float surfaceFriction )
		{
			var currentSpeed = Vector3.Dot( Velocity, wishDir );
			var addSpeed = wishSpeed - currentSpeed;

			if ( addSpeed <= 0 )
			{
				return;
			}

			var accelspeed = Math.Min( accel * deltaTime * wishSpeed * surfaceFriction, addSpeed );
			Velocity += ClipVelocity( accelspeed * wishDir, -GroundNormal, 1.0f );
		}

		private void ApplyAirAcceleration( Vector3 wishDir, float wishSpeed, float accel, float airCap, float deltaTime )
		{
			var wishSpd = Math.Min( wishSpeed, airCap );
			var currentSpeed = Vector3.Dot( Velocity, wishDir );
			var addSpeed = wishSpd - currentSpeed;

			if ( addSpeed <= 0 )
			{
				return;
			}

			var accelspeed = Math.Min( addSpeed, accel * wishSpeed * deltaTime );
			Velocity += accelspeed * wishDir;
		}

		private void ApplyFriction( float stopSpeed, float friction, float deltaTime )
		{
			var speed = Velocity.Length;

			if ( speed < 0.0001905f )
			{
				return;
			}

			var drop = 0f;
			var control = (speed < stopSpeed) ? stopSpeed : speed;
			drop += control * friction * deltaTime;
			var newspeed = Math.Max( speed - drop, 0 );

			if ( newspeed != speed )
			{
				newspeed /= speed;
				Velocity *= newspeed;
			}
		}

		private void ClampVelocity( float range )
		{
			Velocity = Vector3.Clamp( Velocity, -new Vector3( range ), new Vector3( range ) );
		}

		private void CheckJump()
		{
			if ( Grounded && Input.IsDown( Key.Space ) )
			{
				Velocity = Velocity.WithZ( JumpPower );
				Grounded = false;
			}
		}

		public static Vector3 ClipVelocity( Vector3 input, Vector3 normal, float overbounce )
		{
			var backoff = Vector3.Dot( input, normal ) * overbounce;

			for ( int i = 0; i < 3; i++ )
			{
				var change = normal[i] * backoff;
				input[i] = input[i] - change;
			}

			var adjust = Vector3.Dot( input, normal );
			if ( adjust < 0.0f )
			{
				input -= (normal * adjust);
			}

			return input;
		}

	}
}
