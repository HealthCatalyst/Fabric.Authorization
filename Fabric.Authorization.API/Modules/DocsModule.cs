using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Nancy.Routing;
using Nancy.Metadata.Modules;


namespace Fabric.Authorization.API.Modules
{
    public class DocsModule : NancyModule
    {
        public DocsModule(): base("/v1/docs")
        {
            this.Get("/", _ => this.GetSwaggerUrl());

            Get("/swagger-ui", _ =>
            {
                var url = $"{Request.Url.BasePath}/api-docs";
                return View["doc", url];
            });
        }

        private Response GetSwaggerUrl()
        {
            var hostName = this.Request.Url.HostName;
            var port = this.Request.Url.Port ?? 80;
            return this.Response.AsRedirect($"http://{hostName}:{port}/swagger/index.html?url=http://{hostName}:{port}/api-docs");
        }
    }
}
