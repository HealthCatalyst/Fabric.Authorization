namespace Fabric.Authorization.Client.Routes
{
    internal abstract class BaseRoute
    {
        protected abstract string CollectionType { get; }

        protected virtual string BaseRouteSegment => $"/{CollectionType}";
    }
}