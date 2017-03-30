using Microsoft.AspNetCore.Builder;
using Nancy.Owin;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using LibOwin;


namespace Fabric.Identity.APISample
{
    public class Startup
    {

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebEncoders();
            services.AddCors(options =>
            {
                // this defines a CORS policy called "default"
                options.AddPolicy("default", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseCors("default");
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
