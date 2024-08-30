using Graybox.DataStructures.Geometric;

namespace Graybox.DataStructures.Models
{
	public class Bone
	{
		public int BoneIndex { get; private set; }
		public int ParentIndex { get; private set; }
		public Bone Parent { get; private set; }
		public string Name { get; private set; }
		public Vector3 DefaultPosition { get; private set; }
		public Vector3 DefaultAngles { get; private set; }
		public Vector3 DefaultPositionScale { get; private set; }
		public Vector3 DefaultAnglesScale { get; private set; }
		public Matrix4 Transform { get; private set; }

		public Bone( int boneIndex, int parentIndex, Bone parent, string name,
					OpenTK.Mathematics.Vector3 defaultPosition, OpenTK.Mathematics.Vector3 defaultAngles,
					OpenTK.Mathematics.Vector3 defaultPositionScale, OpenTK.Mathematics.Vector3 defaultAnglesScale )
		{
			BoneIndex = boneIndex;
			ParentIndex = parentIndex;
			Parent = parent;
			Name = name;
			DefaultPosition = defaultPosition;
			DefaultAngles = defaultAngles;
			DefaultPositionScale = defaultPositionScale;
			DefaultAnglesScale = defaultAnglesScale;
			Transform = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( DefaultAngles ) ).Translate( defaultPosition );
			if ( parent != null ) Transform *= parent.Transform;
		}
	}
}
