namespace Fabric.Authorization.Domain.Models
{
    public class Client
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public SecurableItem TopLevelSecurableItem { get; set; }
    }
}
