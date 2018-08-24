using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels.EDW
{
    public class EDWRole
    {
        /// <summary>
        /// creates <see cref="Role"/> with default values
        /// </summary>
        public EDWRole()
        {
            this.EDWIdentityRoles = new List<EDWIdentityRole>();
            this.EDWIdentities = new List<EDWIdentity>();
        }

        /// <summary>
        /// identity field
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// role name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// role description
        /// </summary>
        public string Description { get; set; }

        public virtual ICollection<EDWIdentityRole> EDWIdentityRoles { get; set; }

        [NotMapped]
        public ICollection<EDWIdentity> EDWIdentities { get; set; }
    }
}
