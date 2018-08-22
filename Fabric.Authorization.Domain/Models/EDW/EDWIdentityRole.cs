using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabric.Authorization.API.Models.EDW
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
