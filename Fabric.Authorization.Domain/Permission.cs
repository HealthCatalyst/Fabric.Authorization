namespace Fabric.Authorization.Domain
{
    public class Permission
    {
        public int Id { get; set; }

        public string Grain { get; set; }

        public string Resource { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Grain}/{Resource}.{Name}";
        }
    }
}
