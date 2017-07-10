using System.Threading.Tasks;
using Fabric.Authorization.Domain.Events;

namespace Fabric.Authorization.Domain.Services
{
    public interface IEventService
    {
        Task RaiseEventAsync(Event evnt);
    }
}
