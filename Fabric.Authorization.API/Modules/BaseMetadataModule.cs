using System.Collections.Generic;
using Fabric.Authorization.API.Constants;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Swagger.ObjectModel;
using Swagger.ObjectModel.Builders;

namespace Fabric.Authorization.API.Modules
{
    public abstract class BaseMetadataModule : SwaggerMetadataModule
    {
        protected SecurityRequirementBuilder OAuth2ReadScopeBuilder = new SecurityRequirementBuilder()
            .SecurityScheme(SecuritySchemes.Oauth2)
            .SecurityScheme(new List<string>() {Scopes.ReadScope});

        protected SecurityRequirementBuilder OAuth2WriteScopeBuilder = new SecurityRequirementBuilder()
            .SecurityScheme(SecuritySchemes.Oauth2)
            .SecurityScheme(new List<string>() { Scopes.WriteScope });

        protected SecurityRequirementBuilder OAuth2ReadWriteScopeBuilder = new SecurityRequirementBuilder()
            .SecurityScheme(SecuritySchemes.Oauth2)
            .SecurityScheme(new List<string>() { Scopes.ReadScope, Scopes.WriteScope });

        protected SecurityRequirementBuilder OAuth2ManageClientsScopeBuilder = new SecurityRequirementBuilder()
            .SecurityScheme(SecuritySchemes.Oauth2)
            .SecurityScheme(new List<string>() { Scopes.ManageClientsScope });

        protected SecurityRequirementBuilder OAuth2ManageClientsAndReadScopeBuilder = new SecurityRequirementBuilder()
            .SecurityScheme(SecuritySchemes.Oauth2)
            .SecurityScheme(new List<string>() { Scopes.ReadScope, Scopes.ManageClientsScope });

        protected SecurityRequirementBuilder OAuth2ManageClientsAndWriteScopeBuilder = new SecurityRequirementBuilder()
            .SecurityScheme(SecuritySchemes.Oauth2)
            .SecurityScheme(new List<string>() { Scopes.WriteScope, Scopes.ManageClientsScope });


        protected BaseMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) : base(modelCatalog, tagCatalog)
        {
            
        }
    }
}