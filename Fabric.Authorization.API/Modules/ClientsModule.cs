using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class ClientsModule : FabricModule<Client>
    {
        private readonly IClientService _clientService;

        public ClientsModule(IClientService clientService, ClientValidator validator, ILogger logger) : base(
            "/Clients", logger, validator)
        {
            //private members
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

            //routes and handlers
            Get("/", _ => GetClients());
            Get("/{clientid}", parameters => GetClientById(parameters));
            Post("/", _ => AddClient());
            Delete("/{clientid}", parameters => DeleteClient(parameters));
        }

        private dynamic GetClients()
        {
            this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationReadClaim);
            IEnumerable<Client> clients = _clientService.GetClients();
            return clients.Select(c => c.ToClientApiModel());
        }

        private dynamic GetClientById(dynamic parameters)
        {
            try
            {
                string clientIdAsString = parameters.clientid.ToString();
                this.RequiresClaims(AuthorizationReadClaim);
                this.RequiresAnyClaim(AuthorizationManageClientsClaim, GetClientIdPredicate(clientIdAsString));
                Client client = _clientService.GetClient(clientIdAsString);
                return client.ToClientApiModel();
            }
            catch (ClientNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.clientid);
                return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                    HttpStatusCode.BadRequest);
            }
        }

        private dynamic AddClient()
        {
            this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
            var clientApiModel = this.Bind<ClientApiModel>(model => model.CreatedBy,
                model => model.CreatedDateTimeUtc,
                model => model.ModifiedBy,
                model => model.ModifiedDateTimeUtc,
                model => model.TopLevelSecurableItem);
            var incomingClient = clientApiModel.ToClientDomainModel();
            Validate(incomingClient);
            Client client = _clientService.AddClient(incomingClient);
            return CreateSuccessfulPostResponse(client.ToClientApiModel());
        }

        private dynamic DeleteClient(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
                Client client = _clientService.GetClient(parameters.clientid);
                _clientService.DeleteClient(client);
                return HttpStatusCode.NoContent;
            }
            catch (ClientNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.clientid);
                return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                    HttpStatusCode.BadRequest);
            }
        }
    }
}
