using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class NotFoundExceptionDetail
    {
        public string Identifier { get; set; }
    }

    public class NotFoundException<T> : Exception
    {
        private readonly T _model;

        public ICollection<NotFoundExceptionDetail> ExceptionDetails { get; } = new List<NotFoundExceptionDetail>();

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

        public NotFoundException(string message, ICollection<NotFoundExceptionDetail> exceptionDetails) : base(message)
        {
            ExceptionDetails = exceptionDetails;
        }

        public NotFoundException(T model, string message, Exception inner) : base(message, inner)
        {
            _model = model;
        }
    }
}