using System;
using System.IO;
using Nancy;
using Nancy.Responses;

namespace Fabric.Authorization.API.Configuration
{
    public class AngularConventionBuilder
    {
        public static Func<NancyContext, string, Response> AddAngularRoot(string angularRootDirectory, string indexFile)
        {
            return (ctx, appRoot) =>
            {
                if (!ctx.Request.Path.StartsWith($"/{angularRootDirectory}/")) return null;
                var file = Path.Combine(appRoot, angularRootDirectory, indexFile);
                return new GenericFileResponse(file, ctx);
            };
        }
    }
}
