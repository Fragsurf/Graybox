using Graybox.DataStructures.Geometric;
using OpenTK.Graphics;
using System.Collections.Generic;

namespace Graybox.DataStructures.Models
{
	public class MeshVertex
	{
		public OpenTK.Mathematics.Vector3 Location { get; set; }
		public OpenTK.Mathematics.Vector3 Normal { get; set; }
		public IEnumerable<BoneWeighting> BoneWeightings { get; private set; }
		public float TextureU { get; set; }
		public float TextureV { get; set; }
		public Quaternion Color { get; set; }

		public MeshVertex( OpenTK.Mathematics.Vector3 location, OpenTK.Mathematics.Vector3 normal, IEnumerable<BoneWeighting> boneWeightings, float textureU, float textureV )
		{
			Location = location;
			Normal = normal;
			BoneWeightings = boneWeightings;
			TextureU = textureU;
			TextureV = textureV;
			Color = new Quaternion( 1, 1, 1, 1 );
		}

		public MeshVertex( OpenTK.Mathematics.Vector3 location, OpenTK.Mathematics.Vector3 normal, Bone bone, float textureU, float textureV )
		{
			Location = location;
			Normal = normal;
			BoneWeightings = new List<BoneWeighting> { new BoneWeighting( bone, 1 ) };
			TextureU = textureU;
			TextureV = textureV;
			Color = new Quaternion( 1, 1, 1, 1 );
		}
	}
}
