using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Services;
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
        private readonly PermissionService _permissionService;
        private readonly RoleService _roleService;

        public ClientsModule(ClientService clientService, ClientValidator validator, ILogger logger,
            AccessService accessService, RoleService roleService, PermissionService permissionService) : base(
            "/v1/Clients", logger, validator, accessService)
        {
            //private members
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));

            //routes and handlers
            Get("/", async _ => await GetClients().ConfigureAwait(false), null, "GetClients");
            Get("/{clientid}", async parameters => await GetClientById(parameters).ConfigureAwait(false), null,
                "GetClient");
            Post("/", async _ => await AddClient().ConfigureAwait(false), null, "AddClient");
            Delete("/{clientid}", async parameters => await DeleteClient(parameters).ConfigureAwait(false), null,
                "DeleteClient");
        }

        private async Task<dynamic> GetClients()
        {
            this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationReadClaim);
            IEnumerable<Client> clients = await _clientService.GetClients();
            return clients.Select(c => c.ToClientApiModel());
        }

        private async Task<dynamic> GetClientById(dynamic parameters)
        {
            try
            {
                string clientIdAsString = parameters.clientid.ToString();
                this.RequiresClaims(AuthorizationReadClaim);
                this.RequiresAnyClaim(AuthorizationManageClientsClaim, GetClientIdPredicate(clientIdAsString));
                var client = await _clientService.GetClient(clientIdAsString);
                return client.ToClientApiModel();
            }
            catch (NotFoundException<Client> ex)
            {
                Logger.Error(ex, ex.Message, parameters.clientid);
                return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddClient()
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
                var client = await _clientService.AddClient(incomingClient);
                await AddDefaultRoleAndPermissionAsync(client);
                return CreateSuccessfulPostResponse(client.ToClientApiModel());
            }
            catch (AlreadyExistsException<Permission> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (IncompatiblePermissionException ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        private async Task<dynamic> DeleteClient(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
                Client client = await _clientService.GetClient(parameters.clientid);
                await _clientService.DeleteClient(client);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Client> ex)
            {
                Logger.Error(ex, ex.Message, parameters.clientid);
                return CreateFailureResponse($"The specified client with id: {parameters.clientid} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task AddDefaultRoleAndPermissionAsync(Client client)
        {
            try
            {
                var newPermission = await _permissionService.AddPermission(new Permission
                {
                    Name = Domain.Defaults.Authorization.ManageAuthorizationPermissionName,
                    Grain = Domain.Defaults.Authorization.AppGrain,
                    SecurableItem = client.TopLevelSecurableItem.Name
                });
                try
                {
                    await _roleService.AddRole(new Role
                    {
                        Name = $"{client.Id}-admin",
                        Grain = Domain.Defaults.Authorization.AppGrain,
                        SecurableItem = client.TopLevelSecurableItem.Name,
                        Permissions = new List<Permission> {newPermission}
                    });
                }
                catch (Exception)
                {
                    //if we can't create the role, delete the client and the permission
                    await _clientService.DeleteClient(client);
                    await _permissionService.DeletePermission(newPermission);
                    throw;
                }
            }
            catch (Exception)
            {
                //if we can't save the permission, delete the client and rethrow the exception
                await _clientService.DeleteClient(client);
                throw;
            }
        }
    }
}
