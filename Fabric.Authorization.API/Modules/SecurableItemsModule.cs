using System;
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
    public class SecurableItemsModule : FabricModule<SecurableItem>
    {
        private readonly ISecurableItemService _securableItemService;
        public SecurableItemsModule(ISecurableItemService securableItemService, 
            ILogger logger, 
            SecurableItemValidator validator) : base("/SecurableItems", logger, validator)
        {
            _securableItemService = securableItemService ?? throw new ArgumentNullException(nameof(securableItemService));

            Get("/", _ => GetSecurableItem());

            Get("/{securableItemId}", parameters => GetSecurableItem(parameters));

            Post("/", _ => AddSecurableItem());

            Post("/{securableItemId}", parameters => AddSecurableItem(parameters));
            
        }

        private dynamic GetSecurableItem()
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                return _securableItemService.GetTopLevelSecurableItem(ClientId);
            }
            catch (ClientNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.BadRequest);
            }
        }

        private dynamic GetSecurableItem(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                if (!Guid.TryParse(parameters.securableItemId, out Guid securableItemId))
                {
                    return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                }
                SecurableItem securableItem = _securableItemService.GetSecurableItem(ClientId, securableItemId);
                return securableItem.ToSecurableItemApiModel();
            }
            catch (ClientNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.BadRequest);
            }
            catch (SecurableItemNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.securableItemId);
                return CreateFailureResponse($"The specified securableItem with id: {parameters.securableItemId} was not found",
                    HttpStatusCode.BadRequest);
            }
        }

        private dynamic AddSecurableItem()
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            var securableItemApiModel = this.Bind<SecurableItemApiModel>(binderIgnore => binderIgnore.Id,
                binderIgnore => binderIgnore.CreatedBy,
                binderIgnore => binderIgnore.CreatedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedBy,
                binderIgnore => binderIgnore.SecurableItems);
            var incomingSecurableItem = securableItemApiModel.ToSecurableItemDomainModel();
            Validate(incomingSecurableItem);

            try
            {
                SecurableItem securableItem = _securableItemService.AddSecurableItem(ClientId, incomingSecurableItem);
                return CreateSuccessfulPostResponse(securableItem.ToSecurableItemApiModel());
            }
            catch (SecurableItemAlreadyExistsException ex)
            {
                Logger.Error(ex, "The posted securable item {@securableItemApiModel} already exists.", securableItemApiModel);
                return CreateFailureResponse(
                    ex.Message,
                    HttpStatusCode.BadRequest);
            }
            catch (ClientNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.Forbidden);
            }
        }

        private dynamic AddSecurableItem(dynamic parameters)
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            if (!Guid.TryParse(parameters.securableItemId, out Guid securableItemId))
            {
                return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
            }
            var securableItemApiModel = this.Bind<SecurableItemApiModel>(binderIgnore => binderIgnore.Id,
                binderIgnore => binderIgnore.CreatedBy,
                binderIgnore => binderIgnore.CreatedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedBy,
                binderIgnore => binderIgnore.SecurableItems);
            var incomingSecurableItem = securableItemApiModel.ToSecurableItemDomainModel();
            Validate(incomingSecurableItem);

            try
            {
                SecurableItem securableItem =
                    _securableItemService.AddSecurableItem(ClientId, securableItemId, incomingSecurableItem);
                return CreateSuccessfulPostResponse(securableItem.ToSecurableItemApiModel());
            }
            catch (SecurableItemNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.securableItemId);
                return CreateFailureResponse(
                    $"The specified securableItem with id: {parameters.securableItemId} was not found",
                    HttpStatusCode.BadRequest);
            }
            catch (SecurableItemAlreadyExistsException ex)
            {
                Logger.Error(ex, "The posted securable item {@securableItemApiModel} already exists.", securableItemApiModel);
                return CreateFailureResponse(
                    ex.Message,
                    HttpStatusCode.BadRequest);
            }
            catch (ClientNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, ClientId);
                return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                    HttpStatusCode.Forbidden);
            }
        }
    }
}
