using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions;
using Graybox.Editor.Actions.MapObjects.Operations;

namespace Graybox.Editor.Problems
{
	public class TextureAxisPerpendicularToFace : IProblemCheck
	{
		public IEnumerable<Problem> Check( Map map, bool visibleOnly )
		{
			List<Face> faces = map.WorldSpawn
				.Find( x => x is Solid && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)) )
				.OfType<Solid>()
				.SelectMany( x => x.Faces )
				.ToList();
			foreach ( Face face in faces )
			{
				OpenTK.Mathematics.Vector3 normal = face.TextureRef.GetNormal();
				if ( MathF.Abs( face.Plane.Normal.Dot( normal ) ) <= 0.0001f ) yield return new Problem( GetType(), map, new[] { face }, Fix, "Texture axis perpendicular to face", "The texture axis of this face is perpendicular to the face plane. This occurs when manipulating objects with texture lock off, as well as various other operations. Re-align the texture to the face to repair. Fixing the problem will reset the textures to the face plane." );
			}
		}

		public IAction Fix( Problem problem )
		{
			return new EditFace( problem.Faces, ( d, x ) => x.AlignTextureToFace() );
		}
	}
}
