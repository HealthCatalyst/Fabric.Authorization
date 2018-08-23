namespace Fabric.Authorization.Domain.Models.EDW
{
    public class EDWIdentityRole
    {
        public int Id { get; set; }

        public int IdentityId { get; set; }

        public int RoleId { get; set; }

        public EDWIdentity EDWIdentity { get; set; }

        public EDWRole EDWRole { get; set; }
    }
}
