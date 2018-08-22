using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabric.Authorization.API.Models.EDW
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
