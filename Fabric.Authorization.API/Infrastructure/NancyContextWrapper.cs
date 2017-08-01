using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;

namespace Fabric.Authorization.API.Infrastructure
{
    public class NancyContextWrapper
    {
        public NancyContextWrapper(NancyContext context)
        {
            Context = context;
        }
        public NancyContext Context { get; internal set; }
    }
}
