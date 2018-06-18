namespace Fabric.Authorization.Models
{
    public interface IIdentifiable<out T>
    {
        T Id { get; }
    }
}
