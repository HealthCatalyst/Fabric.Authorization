using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class BadRequestException<T> : Exception
    {
        private readonly T _model;

        public T Model => _model;

        public BadRequestException()
        {
        }

        public BadRequestException(T model)
        {
            _model = model;
        }

        public BadRequestException(T model, string message) : base(message)
        {
            _model = model;
        }

        public BadRequestException(string message) : base(message)
        {
        }

        public BadRequestException(T model, string message, Exception inner) : base(message, inner)
        {
            _model = model;
        }
    }
}