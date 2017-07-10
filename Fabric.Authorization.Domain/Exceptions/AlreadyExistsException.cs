using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class AlreadyExistsException<T> : Exception
    {
        private T Model { get; set; }

        public AlreadyExistsException()
        {
        }

        public AlreadyExistsException(T model)
        {
            this.Model = model;
        }

        public AlreadyExistsException(string message) : base(message)
        {
        }

        public AlreadyExistsException(T model, string message) : base(message)
        {
            this.Model = model;
        }

        public AlreadyExistsException(T model, string message, Exception inner) : base(message, inner)
        {
            this.Model = model;
        }
    }
}