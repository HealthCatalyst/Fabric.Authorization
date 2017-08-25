using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class User : ITrackable
    {
        public User()
        {
            Groups = new List<Group>();
        }

        public string Id { get; set; }

        public string ProviderSubjectId { get; set; }

        public string Name { get; set; }

        public ICollection<Permission> DeniedPermissions { get; set; }

        public ICollection<Permission> Permissions { get; set; }

        public ICollection<Group> Groups { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }
    }
}