
using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.Transformations;
using System.Drawing;

namespace Graybox.Editor.Compiling.Lightmap
{
	class Light
	{
		public OpenTK.Mathematics.Vector3 Color;
		public float Intensity;
		public bool HasSprite;
		public OpenTK.Mathematics.Vector3 Origin;
		public float Range;

		public OpenTK.Mathematics.Vector3 Direction;
		public float? innerCos;
		public float? outerCos;

		public static void FindLights( Map map, out List<Light> lightEntities )
		{
			Predicate<string> parseBooleanProperty = ( prop ) =>
			{
				return prop.Equals( "yes", StringComparison.OrdinalIgnoreCase ) || prop.Equals( "true", StringComparison.OrdinalIgnoreCase );
			};

			lightEntities = new List<Light>();
			lightEntities.AddRange( map.WorldSpawn.Find( q => q.ClassName == "light" ).OfType<Entity>()
				.Select( x =>
				{
					float range;
					if ( !float.TryParse( x.EntityData.GetPropertyValue( "range" ), out range ) )
					{
						range = 100.0f;
					}
					float intensity;
					if ( !float.TryParse( x.EntityData.GetPropertyValue( "intensity" ), out intensity ) )
					{
						intensity = 1.0f;
					}
					bool hasSprite = parseBooleanProperty( x.EntityData.GetPropertyValue( "hassprite" ) ?? "true" );

					// TODO: RGB\A color
					var c = x.EntityData.GetPropertyColor( "color", System.Drawing.Color.Black );

					return new Light()
					{
						Origin = new OpenTK.Mathematics.Vector3( x.Origin ),
						Range = range,
						Color = new OpenTK.Mathematics.Vector3( c.R, c.G, c.B ),
						Intensity = intensity,
						HasSprite = hasSprite,
						Direction = default,
						innerCos = null,
						outerCos = null
					};
				} ) );
			lightEntities.AddRange( map.WorldSpawn.Find( q => q.ClassName == "spotlight" ).OfType<Entity>()
				.Select( x =>
				{
					float range;
					if ( !float.TryParse( x.EntityData.GetPropertyValue( "range" ), out range ) )
					{
						range = 100.0f;
					}
					float intensity;
					if ( !float.TryParse( x.EntityData.GetPropertyValue( "intensity" ), out intensity ) )
					{
						intensity = 1.0f;
					}
					bool hasSprite = parseBooleanProperty( x.EntityData.GetPropertyValue( "hassprite" ) ?? "true" );
					float innerCos = 0.5f;
					if ( float.TryParse( x.EntityData.GetPropertyValue( "innerconeangle" ), out innerCos ) )
					{
						innerCos = (float)Math.Cos( innerCos * (float)Math.PI / 180.0f );
					}
					float outerCos = 0.75f;
					if ( float.TryParse( x.EntityData.GetPropertyValue( "outerconeangle" ), out outerCos ) )
					{
						outerCos = (float)Math.Cos( outerCos * (float)Math.PI / 180.0f );
					}

					var c = x.EntityData.GetPropertyColor( "color", System.Drawing.Color.Black );

					Light light = new Light()
					{
						Origin = new OpenTK.Mathematics.Vector3( x.Origin ),
						Range = range,
						Color = new OpenTK.Mathematics.Vector3( c.R, c.G, c.B ),
						Intensity = intensity,
						HasSprite = hasSprite,
						Direction = default,
						innerCos = innerCos,
						outerCos = outerCos
					};

					OpenTK.Mathematics.Vector3 angles = x.EntityData.GetPropertyCoordinate( "angles" );

					var pitch = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( MathHelper.DegreesToRadians( angles.X ), 0, 0 ) );
					var yaw = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( 0, 0, -MathHelper.DegreesToRadians( angles.Y ) ) );
					var roll = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( 0, MathHelper.DegreesToRadians( angles.Z ), 0 ) );

					UnitMatrixMult m = new UnitMatrixMult( yaw * pitch * roll );

					light.Direction = new OpenTK.Mathematics.Vector3( m.Transform( OpenTK.Mathematics.Vector3.UnitY ) );
					//TODO: make sure this matches 3dws

					return light;
				} ) );
		}
	}
}
