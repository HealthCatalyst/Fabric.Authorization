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
        public ClientsModule(IClientService clientService, ClientValidator validator, ILogger logger) : base(
            "/Clients", logger, validator)
        {
            Get("/", _ =>
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationReadClaim);
                IEnumerable<Client> clients = clientService.GetClients();
                return clients.Select(c => c.ToClientApiModel());
            });

            Get("/{clientid}", parameters =>
            {
                try
                {
                    string clientIdAsString = parameters.clientid.ToString();
                    this.RequiresClaims(AuthorizationReadClaim);
                    this.RequiresAnyClaim(AuthorizationManageClientsClaim, GetClientIdPredicate(clientIdAsString));
                    Client client = clientService.GetClient(clientIdAsString);
                    return client.ToClientApiModel();
                }
                catch (ClientNotFoundException ex)
                {
                    Logger.Error(ex, ex.Message, parameters.clientid);
                    return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                        HttpStatusCode.BadRequest);
                }
            });

            Post("/", _ =>
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
                var clientApiModel = this.Bind<ClientApiModel>(model => model.CreatedBy,
                    model => model.CreatedDateTimeUtc,
                    model => model.ModifiedBy,
                    model => model.ModifiedDateTimeUtc,
                    model => model.TopLevelSecurableItem);
                var incomingClient = clientApiModel.ToClientDomainModel();
                Validate(incomingClient);
                Client client = clientService.AddClient(incomingClient);
                return CreateSuccessfulPostResponse(client.ToClientApiModel());
            });

            Delete("/{clientid}", parameters =>
            {
                try
                {
                    this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
                    Client client = clientService.GetClient(parameters.clientid);
                    clientService.DeleteClient(client);
                    return HttpStatusCode.NoContent;
                }
                catch (ClientNotFoundException ex)
                {
                    Logger.Error(ex, ex.Message, parameters.clientid);
                    return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                        HttpStatusCode.BadRequest);
                }
            });
        }
    }
}
