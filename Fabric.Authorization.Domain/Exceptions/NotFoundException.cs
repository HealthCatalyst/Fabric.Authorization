using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class NotFoundException<T> : Exception
    {
        private readonly T _model;

        public NotFoundException()
        {
        }

        public NotFoundException(T model)
        {
            _model = model;
        }

        public NotFoundException(T model, string message) : base(message)
        {
            _model = model;
        }

        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(T model, string message, Exception inner) : base(message, inner)
        {
            _model = model;
        }
    }
}