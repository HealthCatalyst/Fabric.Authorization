using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabric.Authorization.API.Models.EDW
{
    [Table("RoleBASE", Schema = "CatalystAdmin")]
    public class EDWRole
    {
        private ICollection<EDWIdentity> identities;

        /// <summary>
        /// creates <see cref="Role"/> with default values
        /// </summary>
        public EDWRole()
        {
            this.EDWIdentities = new HashSet<EDWIdentity>();
        }

        /// <summary>
        /// identity field
        /// </summary>
        [Key]
        [Column("RoleID")]
        public int Id { get; set; }

        /// <summary>
        /// role name
        /// </summary>
        [Required]
        [Column("RoleNM")]
        public string Name { get; set; }

        /// <summary>
        /// role description
        /// </summary>
        [Column("RoleDSC")]
        public string Description { get; set; }

        public virtual ICollection<EDWIdentityRole> EDWIdentityRoles { get; set; }

        /// <summary>
        /// <see cref="Identity"/>s that have role
        /// </summary>
        [NotNull]
        public virtual ICollection<EDWIdentity> EDWIdentities
        {
            get
            {
                return this.identities;
            }

            set
            {
                value.CheckWhetherArgumentIsNull("value");
                this.identities = value;
            }
        }
    }
}
