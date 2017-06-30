using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class CouldNotCompleteOperationException : Exception
    {
        public CouldNotCompleteOperationException()
        {
        }

        public CouldNotCompleteOperationException(string message) : base(message)
        {
        }

        public CouldNotCompleteOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
