using System.Collections.Generic;

namespace Fabric.Authorization.API.Services
{
    public class CouchDBViews
    {
        public string id { get; set; }
        public string language { get; set; } = "javascript";

        //public Dictionary<string, string> filters { get; set; }
        public Dictionary<string, Dictionary<string, string>> views { get; set; }
    }
}