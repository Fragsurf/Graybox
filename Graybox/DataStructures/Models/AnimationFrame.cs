using Graybox.DataStructures.Geometric;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.DataStructures.Models
{
	public class AnimationFrame
	{
		public List<BoneAnimationFrame> Bones { get; private set; }

		public AnimationFrame()
		{
			Bones = new List<BoneAnimationFrame>();
		}

		public List<Matrix4> GetBoneTransforms( bool transformBones, bool applyDefaults )
		{
			return Bones.Select( bone => GetAnimationTransform( bone.Bone, transformBones, applyDefaults ) ).ToList();
		}

		public Matrix4 GetAnimationTransform( Bone b, bool transformBones, bool applyDefaults )
		{
			Matrix4 m = transformBones ? Matrix4.Identity : GetDefaultBoneTransform( b ).Inverted();
			while ( b != null )
			{
				Quaternion ang = Bones[b.BoneIndex].Angles;
				Vector3 pos = Bones[b.BoneIndex].Position;
				if ( applyDefaults )
				{
					ang *= Quaternion.FromEulerAngles( b.DefaultAngles );
					pos += b.DefaultPosition;
				}
				m *= Matrix4.CreateFromQuaternion( ang ).Translate( pos );
				//var test = Bones[b.BoneIndex].Angles * QuaternionF.EulerAngles(b.DefaultAngles);
				//m *= test.GetMatrix().Translate(Bones[b.BoneIndex].Position + b.DefaultPosition);
				//m *= Bones[b.BoneIndex].Angles.GetMatrix().Translate(Bones[b.BoneIndex].Position);
				b = b.Parent;
			}
			return m;
		}

		private static Matrix4 GetDefaultBoneTransform( Bone b )
		{
			Matrix4 m = Matrix4.Identity;
			while ( b != null )
			{
				m *= Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( b.DefaultAngles ) ).Translate( b.DefaultPosition );
				b = b.Parent;
			}
			return m;
		}
	}
}
