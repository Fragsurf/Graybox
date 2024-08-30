using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.Transformations;

namespace Graybox.Editor.Actions.MapObjects.Operations.EditOperations
{
	public class TransformEditOperation : IEditOperation
	{
		private readonly IUnitTransformation _transformation;
		private readonly TransformFlags _transformFlags;

		public TransformEditOperation( IUnitTransformation transformation, TransformFlags transformFlags )
		{
			_transformation = transformation;
			_transformFlags = transformFlags;
		}

		public void PerformOperation( MapObject mo )
		{
			mo.Transform( _transformation, _transformFlags );
		}
	}
}
