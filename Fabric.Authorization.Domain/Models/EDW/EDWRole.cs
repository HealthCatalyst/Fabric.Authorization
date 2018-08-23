using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models.EDW
{
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

        /// <summary>
        /// <see cref="Identity"/>s that have role
        /// </summary>
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
