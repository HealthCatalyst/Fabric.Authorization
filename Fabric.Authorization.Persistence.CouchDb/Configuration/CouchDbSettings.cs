namespace Fabric.Authorization.Persistence.CouchDb.Configuration
{
    public class CouchDbSettings : ICouchDbSettings
    {
        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
