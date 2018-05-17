namespace Fabric.Authorization.Domain.Models
{
    public interface IIdentifiable<out T>
    {
        T Id { get; }
    }
}
