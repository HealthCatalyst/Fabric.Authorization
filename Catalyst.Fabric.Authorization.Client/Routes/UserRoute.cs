using System.Collections.Generic;

namespace Catalyst.Fabric.Authorization.Client.Routes
{
    internal class UserRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.UserRoute;

        public string IdentityProvider { get; set; }
        public string SubjectId { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(IdentityProvider) && string.IsNullOrEmpty(SubjectId))
            {
                return BaseRouteSegment;
            }

            return $"{BaseRouteSegment}/{IdentityProvider}/{SubjectId}";
        }
    }

    internal class UserRouteBuilder
    {
        private readonly UserRoute _userRoute;
        private readonly IDictionary<string, string> _queryParameters;

        public UserRouteBuilder()
        {
            _userRoute = new UserRoute();
            _queryParameters = new Dictionary<string, string>();
        }

        public UserRouteBuilder IdentityProvider(string identityProvider)
        {
            _userRoute.IdentityProvider = identityProvider;
            return this;
        }

        public UserRouteBuilder SubjectId(string subjectId)
        {
            _userRoute.SubjectId = subjectId;
            return this;
        }

        public UserRouteBuilder Grain(string grain)
        {
            this._queryParameters.Add(ClientConstants.Grain, grain);
            return this;
        }

        public UserRouteBuilder SecurableItem(string securableItem)
        {
            this._queryParameters.Add(ClientConstants.SecurableItem, securableItem);
            return this;
        }

        public string Route => _userRoute.ToString();
        public string UserPermissionsRoute => AppendQueryParameters($"{Route}/{RouteConstants.PermissionCollectionRoute}");
        public string UserRolesRoute => $"{Route}/{RouteConstants.RoleCollectionRoute}";
        public string UserGroupsRoute => $"{Route}/{RouteConstants.GroupCollectionRoute}";


        protected string AppendQueryParameters(string url)
        {
            return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, _queryParameters);
        }
    }
}