using System;
using System.Security.Claims;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Clients;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses;
using Nancy.Security;

namespace Fabric.Authorization.API.ModuleExtensions
{
    public static class ModuleSecurity
    {
        public static void RequiresResourceOwnershipAndClaims<T>(this NancyModule module, IClientService clientService, string grain, string resource, params Predicate<Claim>[] requiredClaims)
        {
            module.RequiresClaims(requiredClaims);
            module.AddBeforeHookOrExecute(RequiresResourceOwnership<T>(clientService, grain, resource));
        }

        public static Func<NancyContext, Response> RequiresResourceOwnership<T>(IClientService clientService, string grain, string resource)
        {
            return (context) =>
            {
                Response response = null;
                var clientId = context.CurrentUser?.FindFirst("client_id")?.Value;
                if (!clientService.DoesClientOwnResource(clientId, grain, resource))
                {
                    var error = ErrorFactory.CreateError<T>($"Client: {clientId} does not have access to the requested grain/resource: {grain}/{resource} combination", HttpStatusCode.Forbidden);
                    response = new JsonResponse(error, new DefaultJsonSerializer(context.Environment),
                        context.Environment) {StatusCode = HttpStatusCode.Forbidden};
                }
                return response;
            };
        }
    }
}
