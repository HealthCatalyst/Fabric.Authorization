namespace Fabric.Authorization.Persistence.CouchDB.Stores
{
    public interface IFormattableIdentifier
    {
        string FormatId(string id);
    }
}