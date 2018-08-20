﻿using System.Collections.Generic;
using System.Net;
using Catalyst.Fabric.Authorization.Models;
using Catalyst.Fabric.Authorization.Models.Search;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class MemberSearchMetadataModule : SearchMetadataModule
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

        public MemberSearchMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(MemberSearchRequest),
                typeof(MemberSearchResponse));

            RouteDescriber.DescribeRouteWithParams(
                "GetMembers",
                string.Empty,
                "Searches for users and groups by client ID and other optional parameters.",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<IEnumerable<MemberSearchResponse>>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata<IEnumerable<MemberSearchResponse>>
                    {
                        Code = (int) Nancy.HttpStatusCode.PartialContent,
                        Message = "Partial success (e.g., results were found in Fabric.Authorization but the call out to Fabric.Identity failed). Properties populated by Fabric.Identity data are FirstName, MiddleName, LastName, and LastLoginDateTimeUtc."
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) Nancy.HttpStatusCode.Forbidden,
                        Message = "Client does not have the required scopes to read data in Fabric.Authorization (fabric/authorization.read)."
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
                }).SecurityRequirement(OAuth2ReadScopeBuilder);
        }
    }
}