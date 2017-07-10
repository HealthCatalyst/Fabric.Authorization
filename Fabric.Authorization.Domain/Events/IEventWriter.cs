using System.Threading.Tasks;

namespace Fabric.Authorization.Domain.Events
{
    public interface IEventWriter
    {
        Task WriteEvent(Event evt);
    }
}
