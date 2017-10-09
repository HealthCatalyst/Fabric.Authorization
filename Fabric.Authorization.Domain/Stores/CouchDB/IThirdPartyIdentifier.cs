namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public interface IThirdPartyIdentifier
    {
        string FormatId(string id);
    }
}