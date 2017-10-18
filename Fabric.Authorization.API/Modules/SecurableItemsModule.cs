using System;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class SecurableItemsModule : FabricModule<SecurableItem>
    {
        private readonly SecurableItemService _securableItemService;

        public SecurableItemsModule(SecurableItemService securableItemService,
            SecurableItemValidator validator,
            ILogger logger) : base("/v1/SecurableItems", logger, validator)
        {
            _securableItemService = securableItemService ??
                                    throw new ArgumentNullException(nameof(securableItemService));
            Get("/",
                async _ => await GetSecurableItem().ConfigureAwait(false),
                null,
                "GetSecurableItem");

            Get("/{securableItemId}",
                async parameters => await this.GetSecurableItem(parameters).ConfigureAwait(false),
                null,
                "GetSecurableItemById");

            Post("/",
                async _ => await AddSecurableItem().ConfigureAwait(false),
                null,
                "AddSecurableItem");

            Post("/{securableItemId}",
                async parameters => await this.AddSecurableItem(parameters).ConfigureAwait(false),
                null,
                "AddSecurableItemById");
        }

        private async Task<dynamic> GetSecurableItem()
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                return (await _securableItemService.GetTopLevelSecurableItem(ClientId)).ToSecurableItemApiModel();
            }
            catch (NotFoundException<Client> ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> GetSecurableItem(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                if (!Guid.TryParse(parameters.securableItemId, out Guid securableItemId))
                {
                    return CreateFailureResponse("securableItemId must be a guid.", HttpStatusCode.BadRequest);
                }
                var securableItem = await _securableItemService.GetSecurableItem(ClientId, securableItemId);
                return securableItem.ToSecurableItemApiModel();
            }
            catch (NotFoundException<Client> ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.NotFound);
            }
            catch (NotFoundException<SecurableItem> ex)
            {
                Logger.Error(ex, ex.Message, parameters.securableItemId);
                return CreateFailureResponse(
                    $"The specified securableItem with id: {parameters.securableItemId} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddSecurableItem()
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            var securableItemApiModel = SecureBind();
            var incomingSecurableItem = securableItemApiModel.ToSecurableItemDomainModel();
            Validate(incomingSecurableItem);

            try
            {
                var securableItem = await _securableItemService.AddSecurableItem(ClientId, incomingSecurableItem);
                return CreateSuccessfulPostResponse(securableItem.ToSecurableItemApiModel());
            }
            catch (NotFoundException<Client> ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.Forbidden);
            }
            catch (AlreadyExistsException<SecurableItem> ex)
            {
                Logger.Error(ex, "The posted securable item {@securableItemApiModel} already exists.",
                    securableItemApiModel);
                return CreateFailureResponse(
                    ex.Message,
                    HttpStatusCode.BadRequest);
            }
        }

        private async Task<dynamic> AddSecurableItem(dynamic parameters)
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            if (!Guid.TryParse(parameters.securableItemId, out Guid securableItemId))
            {
                return CreateFailureResponse("securableItemId must be a guid.", HttpStatusCode.BadRequest);
            }

            var securableItemApiModel = SecureBind();
            var incomingSecurableItem = securableItemApiModel.ToSecurableItemDomainModel();
            Validate(incomingSecurableItem);

            try
            {
                var securableItem =
                    await _securableItemService.AddSecurableItem(ClientId, securableItemId, incomingSecurableItem);
                return CreateSuccessfulPostResponse(securableItem.ToSecurableItemApiModel());
            }
            catch (NotFoundException<Client> ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.Forbidden);
            }
            catch (AlreadyExistsException<SecurableItem> ex)
            {
                Logger.Error(ex, "The posted securable item {@securableItemApiModel} already exists.",
                    securableItemApiModel);
                return CreateFailureResponse(
                    ex.Message,
                    HttpStatusCode.BadRequest);
            }
            catch (NotFoundException<SecurableItem> ex)
            {
                Logger.Error(ex, ex.Message, parameters.securableItemId);
                return CreateFailureResponse(
                    $"The specified securableItem with id: {parameters.securableItemId} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private SecurableItemApiModel SecureBind()
        {
            return this.Bind<SecurableItemApiModel>(binderIgnore => binderIgnore.Id,
                binderIgnore => binderIgnore.CreatedBy,
                binderIgnore => binderIgnore.CreatedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedBy,
                binderIgnore => binderIgnore.SecurableItems);
        }
    }
}