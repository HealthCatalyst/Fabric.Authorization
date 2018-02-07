using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Models;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses;
using Nancy.Security;

namespace Fabric.Authorization.API.ModuleExtensions
{
    public static class ModuleSecurity
    {
        public static void RequiresOwnershipAndClaims<T>(this NancyModule module, bool doesClientOwnItem, string grain, string securableItem, params Predicate<Claim>[] requiredClaims)
        {
            module.RequiresClaims(requiredClaims);
            module.AddBeforeHookOrExecute(RequiresOwnership<T>(doesClientOwnItem, grain, securableItem));
        }

        public static void RequiresSharedAccess<T>(this NancyModule module, Grain grain, string securableItem, bool isSecurableItemChildOfGrain, bool isWriteOperation, IEnumerable<string> permissions)
        {
            module.AddBeforeHookOrExecute(RequiresSharedAccess<T>(isSecurableItemChildOfGrain, grain, securableItem, isWriteOperation, permissions));
        }

        public static Func<NancyContext, Response> RequiresOwnership<T>(bool doesClientOwnItem, string grain, string securableItem)
        {
            return (context) =>
            {
                Response response = null;
                var clientId = context.CurrentUser?.FindFirst(Claims.ClientId)?.Value;
                if (!doesClientOwnItem)
                {
                    response = CreateForbiddenResponse<T>(clientId, grain, securableItem, context);
                }
                return response;
            };
        }

        public static Func<NancyContext, Response> RequiresSharedAccess<T>(bool isSecurableItemChildOfGrain, Grain grain, string securableItem, bool isWriteOperation, IEnumerable<string> permissions)
        {
            return (context) =>
            {
                var clientId = context.CurrentUser?.FindFirst(Claims.ClientId)?.Value;
                if (!isSecurableItemChildOfGrain)
                {
                    return CreateForbiddenResponse<T>(clientId, grain.Name, securableItem, context);
                }
                if (!isWriteOperation)
                {
                    return null;
                }
                if (context.CurrentUser.HasClaims(claim => claim.Type == Claims.Scope && grain.RequiredWriteScopes.Contains(claim.Value)) 
                    && (permissions.Contains("dos/datamarts.manageauthorization") || clientId == "fabric-installer"))
                {
                    return null;
                }
                return CreateForbiddenResponse<T>(clientId, grain.Name, securableItem, context);
            };
        }

        private static JsonResponse CreateForbiddenResponse<T>(string clientId, string grain, string securableItem, NancyContext context)
        {
            var error = ErrorFactory.CreateError<T>($"Client: {clientId} does not have access to the requested grain/securableItem: {grain}/{securableItem} combination", HttpStatusCode.Forbidden);
            return new JsonResponse(error, new DefaultJsonSerializer(context.Environment),
                    context.Environment)
            { StatusCode = HttpStatusCode.Forbidden };
        }
    }
}