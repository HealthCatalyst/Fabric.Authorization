using System.Collections.Generic;
using System.Net;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Models.Search;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class IdentitySearchMetadataModule : SearchMetadataModule
    {
        private readonly Parameter _clientIdParameter = new Parameter
        {
            Name = "client_id",
            Description = "Client ID",
            Required = true,
            Type = "string",
            In = ParameterIn.Query
        };

        private readonly Tag _searchTag = new Tag
        {
            Name = "Fabric.Identity Search",
            Description = "Operations for searching Fabric.Identity"
        };

        public IdentitySearchMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(IdentitySearchRequest),
                typeof(IdentitySearchResponse));

            RouteDescriber.DescribeRouteWithParams(
                "SearchIdentities",
                string.Empty,
                "Searches Fabric.Identity for users by 1 or more subject IDs.",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<IdentitySearchResponse>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) Nancy.HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) Nancy.HttpStatusCode.BadRequest,
                        Message = "Group already exists"
                    }
                },
                new[]
                {
                    _clientIdParameter,
                    PageNumberParameter,
                    PageSizeParameter,
                    FilterParameter,
                    SortKeyParameter,
                    SortDirectionParameter
                },
                new[]
                {
                    _searchTag
                });
        }
    }
}