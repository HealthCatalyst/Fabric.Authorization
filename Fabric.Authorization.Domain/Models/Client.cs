namespace Fabric.Authorization.Domain.Models
{
    public class Client
    {
        public string Id { get; set; }
        public Resource TopLevelResource { get; set; }
    }
}
