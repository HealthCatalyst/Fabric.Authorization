namespace Fabric.Authorization.Persistence.SqlServer.Configuration
{
    public class ConnectionStrings : IConnectionStrings
    {
        public string AuthorizationDatabase { get; set; }

        public string EDWAdminDatabase { get; set; }
    }
}
