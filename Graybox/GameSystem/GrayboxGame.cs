
using Graybox.Scenes;
using System;

namespace Graybox.GameSystem
{
	public class GrayboxGame : IDisposable
	{


		int tickRate = 100;
		public int TickRate
		{
			get => tickRate;
			set
			{
				tickRate = MathHelper.Clamp( value, 10, 256 );
				FixedDeltaTime = 1.0f / TickRate;
			}
		}
		public float FixedDeltaTime { get; private set; } = 0.01f;
		public int TargetFrameRate { get; set; } = 300;
		public float TickAlpha { get; private set; }

		public Scene ActiveScene;
		public float DeltaTime;
		public float ElapsedTime;
		public AssetSystem AssetSystem { get; private set; }

		private float _accumulatedTime;

		public void Initialize()
		{
			AssetSystem = new();
			AssetSystem.AddDirectory( "Assets/" );

			OnInitialized();
		}

		protected virtual void OnInitialized()
		{
		}

		public virtual void Update()
		{
			if ( ActiveScene == null ) return;

			ActiveScene.HandleUpdate( new( DeltaTime, ElapsedTime ) );

			_accumulatedTime += DeltaTime;

			while ( _accumulatedTime >= FixedDeltaTime )
			{
				Tick();
				_accumulatedTime -= FixedDeltaTime;
			}

			TickAlpha = _accumulatedTime / FixedDeltaTime;
			//InterpolateState( TickAlpha );
		}

		public virtual void Tick()
		{

		}

		public void Dispose()
		{
			ActiveScene?.Dispose();
			ActiveScene = null;
		}

	}
}
