using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbViews
    {
        //the properties in this class need to be lowercase because couchDb design document properties are case sensitive 
        
        // ReSharper disable once InconsistentNaming
        public string id { get; set; }
        // ReSharper disable once InconsistentNaming
        public string language { get; set; } = "javascript";

        // ReSharper disable once InconsistentNaming
        public Dictionary<string, Dictionary<string, string>> views { get; set; }
    }
}