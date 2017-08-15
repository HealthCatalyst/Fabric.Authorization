using System;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class ClientsMetadataModule : SwaggerMetadataModule
    {
        private readonly Tag _clientsTag =
            new Tag {Name = "Clients", Description = "Operations for managing clients"};
        private readonly Parameter _clientIdParameter = new Parameter
        {
            Name = "clientId",
            Description = "ClientId to use for the request",
            In = ParameterIn.Path,
            Required = true,
            Type = "integer"
        };

        public ClientsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) 
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(SecurableItemApiModel),                
                typeof(DateTime?));

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
                });

            RouteDescriber.DescribeRouteWithParams(
                "GetClient",
                "",
                "Gets a single client",
                new[]
                {
                    new HttpResponseMetadata<ClientApiModel>
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "Client found"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
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
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddClient",
                "",
                "Registers a new client",
                new[]
                {
                    new HttpResponseMetadata<ClientApiModel>
                    {
                        Code = (int)HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Client with specified id already exists or Client object in body failed validation"
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
                });

            RouteDescriber.DescribeRouteWithParams(
                "DeleteClient",
                "",
                "Deletes a client",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NoContent,
                        Message = "Client deleted"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
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
                });
        }
    }
}
