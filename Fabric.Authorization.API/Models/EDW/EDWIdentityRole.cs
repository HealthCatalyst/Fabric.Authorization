using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabric.Authorization.API.Models.EDW
{
    [Table("IdentityRoleBASE", Schema = "CatalystAdmin")]
    public class EDWIdentityRole
    {
        [Key]
        [Column("IdentityRoleID")]
        public int Id { get; set; }

        [Column("IdentityId")]
        public int IdentityId { get; set; }

        [Column("RoleId")]
        public int RoleId { get; set; }

        public EDWIdentity EDWIdentity { get; set; }

        public EDWRole EDWRole { get; set; }
    }
}
