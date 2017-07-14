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
        private readonly SecurableItemService _securableItemService;

        public SecurableItemsModule(SecurableItemService securableItemService,
            SecurableItemValidator validator,
            ILogger logger) : base("/SecurableItems", logger, validator)
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
                return _securableItemService.GetTopLevelSecurableItem(ClientId).Result.ToSecurableItemApiModel();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Client>)
                {
                    Logger.Error(ex, ex.Message, ClientId);
                    return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                        HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
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
                SecurableItem securableItem = _securableItemService.GetSecurableItem(ClientId, securableItemId).Result;
                return securableItem.ToSecurableItemApiModel();
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                if (ex.InnerException is NotFoundException<Client>)
                {
                    Logger.Error(ex, ex.Message, ClientId);
                    return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                        HttpStatusCode.NotFound);
                }
                else if (ex.InnerException is NotFoundException<SecurableItem>)
                {
                    Logger.Error(ex, ex.Message, parameters.securableItemId);
                    return CreateFailureResponse($"The specified securableItem with id: {parameters.securableItemId} was not found",
                        HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
            }
        }

        private dynamic AddSecurableItem()
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            var securableItemApiModel = SecureBind();
            var incomingSecurableItem = securableItemApiModel.ToSecurableItemDomainModel();
            Validate(incomingSecurableItem);

            try
            {
                SecurableItem securableItem = _securableItemService.AddSecurableItem(ClientId, incomingSecurableItem).Result;
                return CreateSuccessfulPostResponse(securableItem.ToSecurableItemApiModel());
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Client>)
                {
                    Logger.Error(ex, ex.Message, ClientId);
                    return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                        HttpStatusCode.Forbidden);
                }
                else if (ex.InnerException is AlreadyExistsException<SecurableItem>)
                {
                    Logger.Error(ex, "The posted securable item {@securableItemApiModel} already exists.", securableItemApiModel);
                    return CreateFailureResponse(
                        ex.Message,
                        HttpStatusCode.BadRequest);
                }
                else
                {
                    throw;
                }
            }
        }

        private dynamic AddSecurableItem(dynamic parameters)
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            if (!Guid.TryParse(parameters.securableItemId, out Guid securableItemId))
            {
                return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
            }

            var securableItemApiModel = SecureBind();
            var incomingSecurableItem = securableItemApiModel.ToSecurableItemDomainModel();
            Validate(incomingSecurableItem);

            try
            {
                SecurableItem securableItem =
                    _securableItemService.AddSecurableItem(ClientId, securableItemId, incomingSecurableItem).Result;
                return CreateSuccessfulPostResponse(securableItem.ToSecurableItemApiModel());
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Client>)
                {
                    Logger.Error(ex, ex.Message, ClientId);
                    return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                        HttpStatusCode.Forbidden);
                }
                else if (ex.InnerException is AlreadyExistsException<SecurableItem>)
                {
                    Logger.Error(ex, "The posted securable item {@securableItemApiModel} already exists.", securableItemApiModel);
                    return CreateFailureResponse(
                        ex.Message,
                        HttpStatusCode.BadRequest);
                }
                else if (ex.InnerException is NotFoundException<SecurableItem>)
                {
                    Logger.Error(ex, ex.Message, parameters.securableItemId);
                    return CreateFailureResponse($"The specified securableItem with id: {parameters.securableItemId} was not found",
                        HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
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