using System;
using System.Security.Claims;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Services;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses;
using Nancy.Security;

namespace Fabric.Authorization.API.ModuleExtensions
{
    public static class ModuleSecurity
    {
        public static void RequiresOwnershipAndClaims<T>(this NancyModule module, IClientService clientService, string grain, string securableItem, params Predicate<Claim>[] requiredClaims)
        {
            module.RequiresClaims(requiredClaims);
            module.AddBeforeHookOrExecute(RequiresOwnership<T>(clientService, grain, securableItem));
        }

        public static Func<NancyContext, Response> RequiresOwnership<T>(IClientService clientService, string grain, string securableItem)
        {
            return (context) =>
            {
                Response response = null;
                var clientId = context.CurrentUser?.FindFirst("client_id")?.Value;
                if (!clientService.DoesClientOwnItem(clientId, grain, securableItem))
                {
                    var error = ErrorFactory.CreateError<T>($"Client: {clientId} does not have access to the requested grain/securableItem: {grain}/{securableItem} combination", HttpStatusCode.Forbidden);
                    response = new JsonResponse(error, new DefaultJsonSerializer(context.Environment),
                        context.Environment) {StatusCode = HttpStatusCode.Forbidden};
                }
                return response;
            };
        }
    }
}
