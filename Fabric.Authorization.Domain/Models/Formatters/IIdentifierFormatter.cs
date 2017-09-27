namespace Fabric.Authorization.Domain.Models.Formatters
{
    public interface IIdentifierFormatter<T>
    {
        string Format(T entity);
    }
}