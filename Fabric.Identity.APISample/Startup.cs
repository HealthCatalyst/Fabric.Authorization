using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Nancy.Owin;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Owin;
using Microsoft.Extensions.DependencyInjection;
using LibOwin;


namespace Fabric.Identity.APISample
{
    public class Startup
    {

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebEncoders();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = "http://localhost:5001",
                RequireHttpsMetadata = false,

                ApiName = "patientapi"
            });
            app.UseOwin(buildFunc =>
            {
                buildFunc(next => env =>
                {
                    var ctx = new OwinContext(env);
                    var principal = ctx.Request.User;
                    if (principal != null && principal.HasClaim("scope", "patientapi"))
                        return next(env);
                    ctx.Response.StatusCode = 403;
                    return Task.FromResult(0);
                });
                buildFunc.UseNancy();
            });
        }
    }
}
