using Graybox.Graphics;
using Graybox.FileSystem;
using Graybox.Graphics.Helpers;

namespace Graybox.Providers.Model
{
	public abstract class ModelProvider
	{
		private static readonly List<ModelProvider> RegisteredProviders;
		private static readonly List<ModelReference> References;
		private static readonly Dictionary<string, DataStructures.Models.Model> Models;

		static ModelProvider()
		{
			RegisteredProviders = new List<ModelProvider>();
			References = new List<ModelReference>();
			Models = new Dictionary<string, DataStructures.Models.Model>();
		}

		public static void Register( ModelProvider provider )
		{
			RegisteredProviders.Add( provider );
		}

		public static void Deregister( ModelProvider provider )
		{
			RegisteredProviders.Remove( provider );
		}

		public static void DeregisterAll()
		{
			RegisteredProviders.Clear();
		}

		public static ModelReference CreateModelReference( IFile file )
		{
			var model = LoadModel( file );
			var reference = new ModelReference( file.FullPathName, model );
			References.Add( reference );

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			return reference;
		}

		public static void DeleteModelReference( ModelReference reference )
		{
			References.Remove( reference );
			if ( References.All( x => x.Model != reference.Model ) )
			{
				UnloadModel( reference.Model );
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		public static bool CanLoad( IFile file )
		{
			return RegisteredProviders.Any( p => p.IsValidForFile( file ) );
		}

		private static DataStructures.Models.Model LoadModel( IFile file )
		{
			var path = file.FullPathName;

			if ( Models.ContainsKey( path ) ) 
				return Models[path];

			if ( !file.Exists ) throw new ProviderException( "The supplied file doesn't exist." );
			var provider = RegisteredProviders.FirstOrDefault( p => p.IsValidForFile( file ) );
			if ( provider != null )
			{
				var model = provider.LoadFromFile( file );
				model.PreprocessModel();
				for ( int i = 0; i < model.Textures.Count; i++ )
				{
					var t = model.Textures[i];
					Debug.LogError( "Unfinished: create the model's texture" );
					//t.TextureObject = TextureHelper.Create( String.Format( "ModelProvider: {0}/{1}/{2}", path, t.Name, i ), t.Image, t.Image.Width, t.Image.Height, TextureFlags.None );
				}
				Models[path] = model;
				return model;
			}
			throw new ProviderNotFoundException( "No model provider was found for this file." );
		}

		private static void UnloadModel( DataStructures.Models.Model model )
		{
			model.Dispose();
			List<KeyValuePair<string, DataStructures.Models.Model>> kvs = Models.Where( x => x.Value == model ).ToList();
			foreach ( KeyValuePair<string, DataStructures.Models.Model> kv in kvs ) Models.Remove( kv.Key );
		}

		protected abstract bool IsValidForFile( IFile file );
		protected abstract DataStructures.Models.Model LoadFromFile( IFile file );
	}
}
