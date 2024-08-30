
using Graybox.DataStructures.MapObjects;

namespace Graybox.Scenes
{
	/// <summary>
	/// Helper that keeps track of objects that need to be synced with the GPU
	/// or physics engine.
	/// </summary>
	public partial class Scene
	{

		HashSet<MapObject> easySyncList = new( 10000 );
		HashSet<MapObject> easySyncRemoved = new( 10000 );
		Dictionary<MapObject, int> easySyncHashes = new( 10000 );

		public void UpdateObjects( MapObject root )
		{
			easySyncRemoved.Clear();

			foreach ( var obj in easySyncList )
				easySyncRemoved.Add( obj );

			ProcessObjects( root );

			foreach ( var obj in easySyncRemoved )
			{
				easySyncList.Remove( obj );
				easySyncHashes.Remove( obj );
				ObjectRemoved( obj );
			}

			void ProcessObjects( MapObject obj )
			{
				easySyncRemoved.Remove( obj );

				if ( easySyncList.Contains( obj ) )
				{
					var hash = obj.UpdateCounter;
					if ( easySyncHashes[obj] != hash )
					{
						easySyncHashes[obj] = hash;
						ObjectChanged( obj );
					}
				}
				else
				{
					easySyncList.Add( obj );
					easySyncHashes[obj] = obj.UpdateCounter;
					ObjectAdded( obj );
				}

				foreach ( var child in obj.Children )
				{
					ProcessObjects( child );
				}
			}

			void ObjectRemoved( MapObject obj )
			{
				if ( obj is World w ) return;

				_objects.Remove( obj );

				if ( obj is Solid s )
				{
					_solidRenderer.Remove( s );
					_solidWireframeRenderer.Remove( s );
				}
				Physics.Remove( obj );
			}

			void ObjectAdded( MapObject obj )
			{
				if ( obj is World w ) return;

				_objects.Add( obj );

				if ( obj is Solid s )
				{
					var isTrigger = s.IsTrigger();
					var triggerTexture = (TextureAsset)null;

					if ( isTrigger )
					{
						triggerTexture = AssetSystem.FindAsset<TextureAsset>( "textures/tools/tool_trigger" );
					}

					foreach ( var face in s.Faces )
					{
						face.TextureRef.Texture = triggerTexture ?? AssetSystem.FindAsset<TextureAsset>( face.TextureRef.AssetPath );
						face.CalculateTextureCoordinates( true );
					}
					_solidRenderer.Add( s );
					_solidWireframeRenderer.Add( s );
				}
				Physics.Add( obj );
			}

			void ObjectChanged( MapObject obj )
			{
				if ( obj is World w ) return;

				if ( obj is Solid s )
				{
					_solidRenderer.Update( s );
					_solidWireframeRenderer.Update( s );
				}
				Physics.Add( obj );
			}
		}

		public void RefreshAllObjects()
		{
			foreach ( var obj in this._objects )
			{
				obj.IncrementUpdateCounter();
			}
		}

	}
}
