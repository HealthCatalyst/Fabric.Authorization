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
        public SecurableItemsModule(ISecurableItemService securableItemService, ILogger logger, SecurableItemValidator validator) : base("/SecurableItems", logger, validator)
        {
            Get("/", _ =>
            {
                try
                {
                    this.RequiresClaims(AuthorizationReadClaim);
                    return securableItemService.GetTopLevelSecurableItem(ClientId);
                }
                catch (ClientNotFoundException ex)
                {
                    Logger.Error(ex, ex.Message, ClientId);
                    return CreateFailureResponse($"The specified client with id: {ClientId} was not found",
                        HttpStatusCode.BadRequest);
                }
            });

            Get("/{securableItemId}", parameters =>
            {
                try
                {
                    this.RequiresClaims(AuthorizationReadClaim);
                    if (!Guid.TryParse(parameters.securableItemId, out Guid securableItemId))
                    {
                        return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                    }
                    SecurableItem securableItem = securableItemService.GetSecurableItem(ClientId, securableItemId);
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
            });

            Post("/", _ =>
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
                    SecurableItem securableItem = securableItemService.AddSecurableItem(ClientId, incomingSecurableItem);
                    return CreateSuccessfulPostResponse(securableItem.ToSecurableItemApiModel());
                }
                catch (SecurableItemAlreadyExistsException ex)
                {
                    Logger.Error(ex, "The posted securable item {@securableItemApiModel} already exists.", securableItemApiModel);
                    return CreateFailureResponse(
                        ex.Message,
                        HttpStatusCode.BadRequest);
                }
            });

            Post("/{securableItemId}", parameters =>
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
                        securableItemService.AddSecurableItem(ClientId, securableItemId, incomingSecurableItem);
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

            });
        }
    }
}
