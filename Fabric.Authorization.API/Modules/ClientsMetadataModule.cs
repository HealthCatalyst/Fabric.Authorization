using System;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

using Catalyst.Fabric.Authorization.Models;

namespace Fabric.Authorization.API.Modules
{
    public class ClientsMetadataModule : BaseMetadataModule
    {
        private readonly Parameter _clientIdParameter = new Parameter
        {
            Name = "clientid",
            Description = "ClientId to use for the request",
            In = ParameterIn.Path,
            Required = true,
            Type = "integer"
        };

        private readonly Tag _clientsTag =
            new Tag {Name = "Clients", Description = "Operations for managing clients"};

        public ClientsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(Guid),
                typeof(Guid?),
                typeof(SecurableItemApiModel),
                typeof(DateTime?),
                typeof(InnerError));

            RouteDescriber.DescribeRoute(
                "GetClients",
                "",
                "Gets all registered clients",
                new[]
                {
                    new HttpResponseMetadata<ClientApiModel> {Code = (int) HttpStatusCode.OK, Message = "OK"},
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    _clientsTag
                }).SecurityRequirement(OAuth2ManageClientsAndReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetClient",
                "",
                "Gets a single client",
                new[]
                {
                    new HttpResponseMetadata<ClientApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "Client found"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Client with specified id was not found"
                    }
                },
                new[]
                {
                    _clientIdParameter
                },
                new[]
                {
                    _clientsTag
                }).SecurityRequirement(OAuth2ManageClientsAndReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddClient",
                "",
                "Registers a new client",
                new[]
                {
                    new HttpResponseMetadata<ClientApiModel>
                    {
                        Code = (int) HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "Client object in body failed validation"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.Conflict,
                        Message = "Client with specified id already exists"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.UnsupportedMediaType,
                        Message = "Content-Type header was not included in request"
                    }
                },
                new[]
                {
                    new BodyParameter<ClientApiModel>(modelCatalog)
                    {
                        Name = "Client",
                        Description = "The client to register"
                    }
                },
                new[]
                {
                    _clientsTag
                }).SecurityRequirement(OAuth2ManageClientsAndWriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "DeleteClient",
                "",
                "Deletes a client",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NoContent,
                        Message = "Client deleted"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Client with specified id was not found"
                    }
                },
                new[]
                {
                    _clientIdParameter
                },
                new[]
                {
                    _clientsTag
                }).SecurityRequirement(OAuth2ManageClientsAndWriteScopeBuilder);
        }
    }
}