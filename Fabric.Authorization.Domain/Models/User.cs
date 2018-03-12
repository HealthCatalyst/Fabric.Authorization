using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models.Formatters;

namespace Fabric.Authorization.Domain.Models
{
    public class User : IUser, ITrackable, IIdentifiable, ISoftDelete
    {
        public User(string subjectId, string identityProvider)
        {
            SubjectId = subjectId;
            IdentityProvider = identityProvider;
            Id = Identifier;
            Groups = new List<Group>();
            Roles = new List<Role>();
        }

        public string Id { get; set; }

        public string Identifier => new UserIdentifierFormatter().Format(this);

        public string SubjectId { get; set; }

        public string IdentityProvider { get; set; }

        public string Name { get; set; }

        public ICollection<Permission> DeniedPermissions { get; set; }

        public ICollection<Permission> Permissions { get; set; }

        public ICollection<Role> Roles { get; set; }

        public ICollection<Group> Groups { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }
    }
}