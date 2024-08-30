
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;

namespace Graybox.Editor.Problems
{
	public class TextureNotFound : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			var faces = map.WorldSpawn
				.Find( x => x is Solid && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
				.OfType<Solid>()
				.SelectMany( x => x.Faces )
				.Where( x => x.TextureRef.Texture == null )
				.ToList();
			foreach ( string name in faces.Select( x => x.TextureRef.AssetPath ).Distinct() )
			{
				yield return new Problem( GetType(), map, faces.Where( x => x.TextureRef.AssetPath == name ).ToList(), Fix, "Texture not found: " + name, "This texture was not found in the currently loaded texture folders. Ensure that the correct texture folders are loaded. Fixing the problems will reset the face textures to the default texture." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new EditFace( problem.Faces, ( d, x ) =>
			{
				char[] ignored = "{#!~+-0123456789".ToCharArray();
				var def = d.AssetSystem.FindAssetsOfType<TextureAsset>()
					.OrderBy( i => new string( i.Name.Where( c => !ignored.Contains( c ) ).ToArray() ) + "Z" )
					.FirstOrDefault();
				if ( def != null )
				{
					x.TextureRef.AssetPath = def.Name;
					x.TextureRef.Texture = def;
					x.CalculateTextureCoordinates( true );
				}
			} );
		}
	}
}
