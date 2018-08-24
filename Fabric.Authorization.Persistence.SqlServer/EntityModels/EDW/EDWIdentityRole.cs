namespace Fabric.Authorization.Persistence.SqlServer.EntityModels.EDW
{
    public class EDWIdentityRole
    {
        public int IdentityRoleID { get; set; }

        public int IdentityID { get; set; }

        public int RoleID { get; set; }

        public EDWIdentity EDWIdentity { get; set; }

        public EDWRole EDWRole { get; set; }
    }
}
