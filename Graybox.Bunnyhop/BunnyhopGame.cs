
using Graybox.Bunnyhop.UI;
using Graybox.DataStructures.MapObjects;
using Graybox.GameSystem;
using Graybox.Lightmapper;
using Graybox.Providers.Map;
using Graybox.Scenes;
using System.Collections.Concurrent;

namespace Graybox.Bunnyhop
{
	public class BunnyhopGame : GrayboxGame
	{

		private BhopMovement movement;
		private Map map;

		ConcurrentQueue<LightmapResult> completedBakes = new();

		protected override void OnInitialized()
		{
			base.OnInitialized();

			//map = LoadMap( "C:\\Users\\jacob\\Documents\\heavyski.rmap" );
			map = LoadMap( $"C:\\git\\fragsurf-good\\Maps\\Official\\hns_wowzOr.rmap" );
			//map = LoadMap( "C:\\Users\\jacob\\Desktop\\fucknut.rmap" );

			ActiveScene = new Scene();
			ActiveScene.Camera.Position = new Vector3( 1000, 1000, 2500 );
			ActiveScene.Camera.FarClip = 20000;
			ActiveScene.SkyboxEnabled = true;
			ActiveScene.SunDirection = -new Vector3( -1000, 1000, 1500 ).Normalized();
			ActiveScene.SunEnabled = false;
			ActiveScene.ShadowDistance = 5000;
			ActiveScene.ShowFPS = true;
			ActiveScene.ShowTriggers = false;
			ActiveScene.ShowGizmos = false;
			ActiveScene.ShowEntities = false;
			ActiveScene.Interface.Add( new Crosshair() );
			ActiveScene.AssetSystem = AssetSystem;
			ActiveScene.UpdateObjects( map.WorldSpawn );

			Bake();

			movement = new( this );
		}

		async void Bake()
		{
			ActiveScene.Lightmaps = new();

			var solids = map.WorldSpawn.GetAllDescendants<Solid>();

			foreach ( var s in solids )
			{
				foreach ( var face in s.Faces )
				{
					face.TexelSize = 2;
				}
			}

			var r = await ActiveScene.LightBaker.BakeAsync( new()
			{
				AmbientColor = new( 55, 85, 55, 255 ),
				BlurStrength = 0,
				Width = 2048,
				Height = 2048,
				Scene = ActiveScene,
				Solids = solids,
				Lights = new List<LightInfo>()
				{
					//new LightInfo()
					//{
					//	Type = LightTypes.Directional,
					//	Color = new( 75, 75, 75, 255 ),
					//	ShadowStrength = 0.35f,
					//	Intensity = 1.0f,
					//	Direction = -new Vector3( -1000, 1000, 1500 ).Normalized(),
					//},
					new LightInfo()
					{
						Type = LightTypes.Point,
						Color = new( 200, 200, 215, 255 ),
						Position = new( 855, 855, 600 )
					},
					new LightInfo()
					{
						Type = LightTypes.Point,
						Color = new( 55, 15, 175, 255 ),
						Position = new( -955, -855, 600 )
					},
				}
			} );

			completedBakes.Enqueue( r );
		}

		public override void Update()
		{
			base.Update();

			if ( completedBakes.TryDequeue( out var r ) )
			{
				if ( r.Success )
				{
					ActiveScene.Lightmaps.Set( r.Lightmaps );
					ActiveScene.RefreshAllObjects();
					ActiveScene.UpdateObjects( map.WorldSpawn );
				}
			}

			movement.Update();
		}

		public override void Tick()
		{
			base.Tick();

			movement.Tick();
		}

		Map LoadMap( string absPath )
		{
			MapProvider.Register( new RMapProvider() );
			return MapProvider.GetMapFromFile( absPath );
		}

	}
}
