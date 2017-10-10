namespace Fabric.Authorization.Domain.Stores
{
    public interface IIdentifierFormatter
    {
        string Format(string id);
    }
}