namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public interface IFormattableIdentifier
    {
        string FormatId(string id);
    }
}