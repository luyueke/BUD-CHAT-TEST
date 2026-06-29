using System;

namespace UI.EditOperation
{
    public class OperationTypeAttribute : Attribute
    {
        public OperationType OperationType { get; private set; }
        
        public OperationTypeAttribute(OperationType type)
        {
            OperationType = type;
        }
    }
}