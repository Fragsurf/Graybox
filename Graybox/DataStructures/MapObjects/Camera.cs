
namespace Graybox.DataStructures.MapObjects;

public class Camera
{
	public Vector3 EyePosition { get; set; }
	public Vector3 LookPosition { get; set; }

	public float Length
	{
		get { return (LookPosition - EyePosition).Length; }
		set { LookPosition = EyePosition + Direction * value; }
	}

	public Vector3 Direction
	{
		get { return (LookPosition - EyePosition).Normalized(); }
		set { LookPosition = EyePosition + value.Normalized() * Length; }
	}
}
