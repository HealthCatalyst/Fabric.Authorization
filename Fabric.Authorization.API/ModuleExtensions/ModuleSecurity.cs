using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Services;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses;
using Nancy.Security;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.ModuleExtensions
{
    public static class ModuleSecurity
    {
        public static void RequiresOwnershipAndClaims<T>(this NancyModule module, ClientService clientService, string grain, string securableItem, params Predicate<Claim>[] requiredClaims)
        {
            module.RequiresClaims(requiredClaims);
            module.AddBeforeHookOrExecute(RequiresOwnership<T>(clientService, grain, securableItem));
        }

        public static Func<NancyContext, Response> RequiresOwnership<T>(ClientService clientService, string grain, string securableItem)
        {
            return (context) =>
            {
                Response response = null;
                var clientId = context.CurrentUser?.FindFirst(Claims.ClientId)?.Value;
                try
                {
                    if (!clientService.DoesClientOwnItem(clientId, grain, securableItem).Result)
                    {
                        response = CreateForbiddenResponse<T>(clientId, grain, securableItem, context);
                    }
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is NotFoundException<Client>)
                    {
                        response = CreateForbiddenResponse<T>(clientId, grain, securableItem, context);
                    }
                    else
                    {
                        throw;
                    }
                    
                }

                return response;
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
