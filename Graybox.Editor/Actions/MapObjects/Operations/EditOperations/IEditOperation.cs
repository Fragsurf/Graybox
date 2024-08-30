using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Actions.MapObjects.Operations.EditOperations
{
    public interface IEditOperation
    {
        void PerformOperation(MapObject mo);
    }
}
