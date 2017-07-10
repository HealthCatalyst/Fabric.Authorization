using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class NotFoundException<T> : Exception
    {
        private T Model { get; set; }

        public NotFoundException()
        {
        }

        public NotFoundException(T model)
        {
            this.Model = model;
        }

        public NotFoundException(T model, string message) : base(message)
        {
            this.Model = model;
        }

        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(T model, string message, Exception inner) : base(message, inner)
        {
            this.Model = model;
        }
    }
}