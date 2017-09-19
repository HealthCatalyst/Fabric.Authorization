﻿using System;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.Models.Search.Validators;
using Fabric.Authorization.API.Services;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class IdentitySearchModule : SearchModule<IdentitySearchRequest>
    {
        private readonly IdentitySearchService _identitySearchService;

        public IdentitySearchModule(
            IdentitySearchService identitySearchService,
            IdentitySearchRequestValidator validator,
            ILogger logger,
            IPropertySettings propertySettings = null) : base("/v1/search/identities", logger, validator,
            propertySettings)
        {
            _identitySearchService = identitySearchService;

            Get("/", async _ => await SearchIdentities().ConfigureAwait(false), null, "SearchIdentities");
        }

        private async Task<dynamic> SearchIdentities()
        {
            this.RequiresClaims(AuthorizationReadClaim, UserIdentityReadClaim);
            var searchRequest = this.Bind<IdentitySearchRequest>();
            Validate(searchRequest);
            var results = await _identitySearchService.Search(searchRequest);
            return results;
        }
    }

    public class IdentitySearchRequestModelBinder : IModelBinder
    {
        public object Bind(NancyContext context, Type modelType, object instance, BindingConfig configuration, params string[] blacklist)
        {
            var queryParams = context.Request.Query;

            var request = new IdentitySearchRequest
            {
                ClientId = queryParams.client_id,
                Filter = queryParams.filter,
                SortDirection = queryParams.sort_dir,
                SortKey = queryParams.sort_key,
                PageSize = queryParams.page_size,
                PageNumber = queryParams.page_number
            };

            return request;
        }

        public bool CanBind(Type modelType)
        {
            return modelType == typeof(IdentitySearchRequest);
        }
    }
}