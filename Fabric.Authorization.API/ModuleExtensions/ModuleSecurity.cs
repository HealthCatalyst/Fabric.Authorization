using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
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

        public static void RequiresPermissionsAndClaims<T>(this NancyModule module, IEnumerable<string> permissions, string grain, string securableItem, params Predicate<Claim>[] requiredClaims)
        {
            module.RequiresClaims(requiredClaims);
            module.AddBeforeHookOrExecute(RequiresPermissions<T>(permissions, grain, securableItem));
        }
        
        public static Func<NancyContext, Response> RequiresOwnership<T>(bool doesClientOwnItem, string grain, string securableItem)
        {
            return (context) =>
            {
                Response response = null;
                var clientId = context.CurrentUser?.FindFirst(Claims.ClientId)?.Value;
                if (clientId == Domain.Defaults.Authorization.InstallerClientId)
                {
                    return null;
                }

                if (doesClientOwnItem)
                {
                    return null;
                }
                return CreateForbiddenResponse<T>(clientId, grain, securableItem, context);
            };
        }

        public static Func<NancyContext, Response> RequiresPermissions<T>(IEnumerable<string> permissions, string grain, string securableItem)
        {
            return (context) =>
            {
                var clientId = context.CurrentUser?.FindFirst(Claims.ClientId)?.Value;
                if (permissions.Contains(
                    $"{grain}/{securableItem}.{Domain.Defaults.Authorization.AuthorizationPermissionName}"))
                {
                    return null;
                }
                return CreateForbiddenResponse<T>(clientId, grain, securableItem, context);
            };
        }

        public static JsonResponse CreateForbiddenResponse<T>(string clientId, string grain, string securableItem, NancyContext context)
        {
            var error = ErrorFactory.CreateError<T>($"Client: {clientId} does not have access to the requested grain/securableItem: {grain}/{securableItem} combination", HttpStatusCode.Forbidden);
            return new JsonResponse(error, new DefaultJsonSerializer(context.Environment),
                    context.Environment)
            { StatusCode = HttpStatusCode.Forbidden };
        }
    }
}