using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models.EDW
{
    public class EDWIdentity
    {
        private ICollection<EDWRole> roles;

        public EDWIdentity()
        {
            this.EDWRoles = new HashSet<EDWRole>();
        }

        /// <summary>
        /// identity field
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// user name
        /// </summary>
        public string Name { get; set; }

        public virtual ICollection<EDWIdentityRole> EDWIdentityRoles { get; set; }

        /// <summary>
        /// <see cref="Role"/>s that user has
        /// </summary>
        public virtual ICollection<EDWRole> EDWRoles
        {
            get
            {
                return this.roles;
            }

            set
            {
                value.CheckWhetherArgumentIsNull("value");
                this.roles = value;
            }
        }
    }
}
