
namespace Graybox.DataStructures.Transformations;

public class UnitTranslate : IUnitTransformation
{
	public Vector3 Translation { get; set; }

	public UnitTranslate( Vector3 translation )
	{
		Translation = translation;
	}

	public Vector3 Transform( Vector3 c )
	{
		return c + Translation;
	}

}
