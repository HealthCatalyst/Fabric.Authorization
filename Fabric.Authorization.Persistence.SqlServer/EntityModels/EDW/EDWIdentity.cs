using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels.EDW
{
    public class EDWIdentity
    {
        public EDWIdentity()
        {
            this.EDWIdentityRoles = new List<EDWIdentityRole>();
            this.EDWRoles = new List<EDWRole>();
        }

        /// <summary>
        /// identity field
        /// </summary>
        public int Id { get; set; }
        
        public string Name { get; set; }

        public ICollection<EDWIdentityRole> EDWIdentityRoles { get; set; }

        [NotMapped]
        public ICollection<EDWRole> EDWRoles { get; set; }
    }
}
