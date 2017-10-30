using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Validators
{
    public class ValidationEnums
    {
        public enum ValidationState
        {
            Duplicate,
            MissingRequiredField,
            InvalidFieldValue
        }
    }
}
