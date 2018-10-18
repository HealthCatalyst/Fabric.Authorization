using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fabric.Authorization.API.RemoteServices.Discovery.Models
{
    public class DiscoveryServiceResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryServiceApiModel"/> class.
        /// </summary>
        public DiscoveryServiceResponse()
        {
            Value = new List<DiscoveryServiceApiModel>();
        }

        /// <summary>
        /// Gets or sets the OData context.
        /// </summary>
        [JsonProperty("@odata.context")]
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets the Value
        /// </summary>
        public List<DiscoveryServiceApiModel> Value { get; set; }
    }
}
