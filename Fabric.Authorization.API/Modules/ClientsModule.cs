using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ClientService _clientService;

        public ClientsModule(ClientService clientService, ClientValidator validator, ILogger logger) : base(
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
            IEnumerable<Client> clients = _clientService.GetClients().Result;
            return clients.Select(c => c.ToClientApiModel());
        }

        private dynamic GetClientById(dynamic parameters)
        {
            try
            {
                string clientIdAsString = parameters.clientid.ToString();
                this.RequiresClaims(AuthorizationReadClaim);
                this.RequiresAnyClaim(AuthorizationManageClientsClaim, GetClientIdPredicate(clientIdAsString));
                Client client = _clientService.GetClient(clientIdAsString).Result;
                return client.ToClientApiModel();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Client>)
                {
                    Logger.Error(ex, ex.Message, parameters.clientid);
                    return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                        HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
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
            try
            {
                Client client = _clientService.AddClient(incomingClient).Result;
                return CreateSuccessfulPostResponse(client.ToClientApiModel());
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is AlreadyExistsException<Client>)
                {
                    Logger.Error(ex, ex.Message, incomingClient.Id);
                    return CreateFailureResponse($"The specified client with id: {incomingClient.Id} already exists.",
                        HttpStatusCode.BadRequest);
                }
                else
                {
                    throw;
                }
            }
        }

        private dynamic DeleteClient(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
                Client client = _clientService.GetClient(parameters.clientid).Result;
                _clientService.DeleteClient(client).Wait();
                return HttpStatusCode.NoContent;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Client>)
                {
                    Logger.Error(ex, ex.Message, parameters.clientid);
                    return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                        HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
