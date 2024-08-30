
namespace Graybox;

public struct FrameInfo
{

	public float DeltaTime { get; }
	public float ElapsedTime { get; }

	public FrameInfo( float delta, float elapsed )
	{
		DeltaTime = delta;
		ElapsedTime = elapsed;
	}

}
