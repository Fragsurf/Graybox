﻿using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Actions.MapObjects.Operations.EditOperations
{
    public class CopyPropertiesEditOperation : IEditOperation
    {
        private readonly MapObject _properties;

        public CopyPropertiesEditOperation(MapObject properties)
        {
            _properties = properties;
        }

        public void PerformOperation(MapObject mo)
        {
            mo.Unclone(_properties);
        }
    }
}