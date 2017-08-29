using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class User : ITrackable, IIdentifiable, ISoftDelete
    {
        public User()
        {
            Groups = new List<string>();
        }

        public string Id { get; set; }

        // TODO: should this be SubjectId?
        public string Identifier => Id;

        public string SubjectId { get; set; }

        public string Name { get; set; }

        public ICollection<Permission> DeniedPermissions { get; set; }

        public ICollection<Permission> Permissions { get; set; }

        public ICollection<string> Groups { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }
    }
}