using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models.Formatters;

namespace Fabric.Authorization.Domain.Models
{
    public class User : IUser, ITrackable, ISoftDelete, IIdentifiable<string>
    {
        public User(string subjectId, string identityProvider)
        {
            SubjectId = subjectId;
            IdentityProvider = identityProvider;
            Id = new UserIdentifierFormatter().Format(this);
            Groups = new List<Group>();
            Roles = new List<Role>();
        }

        public string Id { get; set; }

        public string SubjectId { get; set; }

        public string IdentityProvider { get; set; }

        public string Name { get; set; }

        public string IdentityProviderUserPrincipalName { get; set; }

        public int ParentUserId { get; set; }

        public ICollection<Permission> DeniedPermissions { get; set; }

        public ICollection<Permission> Permissions { get; set; }

        public ICollection<Role> Roles { get; set; }

        public ICollection<Group> Groups { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public override string ToString()
        {
            return $"{IdentityProvider}:{SubjectId}";
        }
    }

    public class UserComparer : IEqualityComparer<User>
    {
        public bool Equals(User p1, User p2)
        {
            if (p1 == p2)
            {
                return true;
            }

            if (p1 == null || p2 == null)
            {
                return false;
            }

            return string.Equals(p1.SubjectId, p2.SubjectId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(p1.IdentityProvider, p2.IdentityProvider, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(User user)
        {
            var hash = 13;
            hash = (hash * 7) + user.SubjectId.GetHashCode();
            hash = (hash * 7) + user.IdentityProvider.GetHashCode();
            return hash;
        }
    }
}