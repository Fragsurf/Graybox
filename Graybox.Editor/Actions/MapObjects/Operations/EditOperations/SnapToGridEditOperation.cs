using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.Transformations;

namespace Graybox.Editor.Actions.MapObjects.Operations.EditOperations
{
	public class SnapToGridEditOperation : IEditOperation
	{
		private readonly float _gridSpacing;
		private readonly TransformFlags _transformFlags;

		public SnapToGridEditOperation( float gridSpacing, TransformFlags transformFlags )
		{
			_gridSpacing = gridSpacing;
			_transformFlags = transformFlags;
		}

		public void PerformOperation( MapObject mo )
		{
			var box = mo.BoundingBox;
			var offset = box.Start.Snap( _gridSpacing ) - box.Start;
			var transform = new UnitTranslate( offset );
			mo.Transform( transform, _transformFlags );
		}
	}
}
