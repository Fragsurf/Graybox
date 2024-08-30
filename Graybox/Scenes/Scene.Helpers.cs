
using Graybox.Graphics.Helpers;
using Graybox.Scenes.Physics;
using OpenTK;

namespace Graybox.Scenes
{
	public partial class Scene
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="worldPos"></param>
		/// <returns>Z will return 0 if the position is behind the camera</returns>
		public Vector3 WorldToScreen( float x, float y, float z ) => WorldToScreen( new Vector3( x, y, z ) );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="worldPos"></param>
		/// <returns>Z will return 0 if the position is behind the camera</returns>
		public Vector3 WorldToScreen( Vector3 worldPos )
		{
			var projectionMatrix = Camera.GetProjectionMatrix();
			var viewMatrix = Camera.GetViewMatrix();
			var viewProjectionMatrix = viewMatrix * projectionMatrix;

			var clipCoords = Vector4.TransformRow( new Vector4( worldPos, 1.0f ), viewProjectionMatrix );

			bool isBehind = clipCoords.W < 0.0f;

			if ( clipCoords.W != 0.0f )
			{
				clipCoords /= clipCoords.W;
			}

			var ndc = new Vector3( clipCoords.X, clipCoords.Y, clipCoords.Z );
			var scale = isBehind ? 0.0f : 1.0f;

			var screenX = (ndc.X + 1.0f) * 0.5f * Width;
			var screenY = (1.0f - ndc.Y) * 0.5f * Height;

			return new Vector3( screenX, screenY, scale );
		}

		public Vector3 ScreenToWorld( int x, int y ) => ScreenToWorld( new Vector2( x, y ) );
		public Vector3 ScreenToWorld( Vector2 screenPos ) => ScreenToRay( screenPos ).Origin;
		public Ray ScreenToRay( float x, float y ) => ScreenToRay( new Vector2( x, y ) );
		public Ray ScreenToRay( int x, int y ) => ScreenToRay( new Vector2( x, y ) );
		public Ray ScreenToRay( Vector2 screenPos )
		{
			var ndcX = (2.0f * screenPos.X) / Width - 1.0f;
			var ndcY = 1.0f - (2.0f * screenPos.Y) / Height;
			var ndc = new Vector3( ndcX, ndcY, -1.0f ); 

			var projectionMatrix = Camera.GetProjectionMatrix();
			var viewMatrix = Camera.GetViewMatrix();
			var inverseViewProjection = Matrix4.Invert( viewMatrix * projectionMatrix );

			var clipCoords = new Vector4( ndc, 1.0f );
			var worldCoords = Vector4.TransformRow( clipCoords, inverseViewProjection );
			if ( worldCoords.W != 0.0f )
			{
				worldCoords /= worldCoords.W;
			}

			var rayOrigin = Camera.Position;
			var rayDirection = Vector3.Normalize( new Vector3( worldCoords.X, worldCoords.Y, worldCoords.Z ) - Camera.Position );

			if ( Camera.Orthographic )
			{
				rayOrigin = new Vector3( worldCoords.X, worldCoords.Y, worldCoords.Z );
				rayDirection = Camera.Forward;
			}

			var result = new Ray()
			{
				Origin = rayOrigin,
				Direction = rayDirection
			};

			//Gizmos.Sphere( result.Origin, 48f, new Vector3( 1, 0, 0 ) );
			//Gizmos.Line( result.Origin, result.Origin + result.Direction * 100000, new Vector3( 1, 0, 0 ), 1.5f, 0 );

			return result;
		}

		public float CalculateScaleForSomething( Vector3 origin )
		{
			if ( Camera.Orthographic )
			{
				return 0.55f * Camera.OrthographicZoom;
			}

			var distance = Vector3.Distance( origin, Camera.Position );
			const float scaleFactor = 800f;
			return distance / scaleFactor;
		}

	}
}
