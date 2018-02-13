using System.Collections;

namespace Fabric.Authorization.API.Models
{
    public class RequestContext
    {
        public string RequestedGrain { get; set; }
        public string RequestedSecurableItem { get; set; }
    }
}