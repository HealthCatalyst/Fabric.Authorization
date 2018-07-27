namespace Catalyst.Fabric.Authorization.Client.Routes
{
    internal abstract class BaseRoute
    {
        protected abstract string CollectionType { get; }

        /// <summary>
        /// General note, do not add a forward slash to the beginning of a relative paths.  it is for reference to this article:
        /// https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working#23438417
        /// </summary>
        protected virtual string BaseRouteSegment => $"{CollectionType}";
    }
}