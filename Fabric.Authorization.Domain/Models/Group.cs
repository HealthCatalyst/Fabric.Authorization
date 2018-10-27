using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class Group : ITrackable, IIdentifiable<Guid>, ISoftDelete
    {
        private GroupIdentifier _groupIdentifier;

        public Group()
        {
            Roles = new List<Role>();
            Users = new List<User>();
            Children = new List<Group>();
            Parents = new List<Group>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string IdentityProvider { get; set; } = "Windows";

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public ICollection<Role> Roles { get; set; }

        public ICollection<User> Users { get; set; }

        public ICollection<Group> Children { get; set; }

        public ICollection<Group> Parents { get; set; }

        public string Source { get; set; }
        
        public string ExternalIdentifier { get; set; }

        public string TenantId { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public GroupIdentifier GroupIdentifier
        {
            get
            {
                if (_groupIdentifier != null) return _groupIdentifier;

                _groupIdentifier = new GroupIdentifier
                {
                    GroupName = Name,
                    IdentityProvider = IdentityProvider,
                    TenantId = TenantId
                };

                return _groupIdentifier;
            }
        } 

        public override string ToString()
        {
            return $"Id = {Id}, Name = {Name}, DisplayName = {DisplayName}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this == obj)
            {
                return true;
            }

            var incomingGroup = obj as Group;

            if (incomingGroup == null)
            {
                return false;
            }

            var nameMatches = Name == incomingGroup.Name
                   || incomingGroup.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);

            var tenantIdMatches = TenantId == incomingGroup.TenantId
                                  || incomingGroup.TenantId.Equals(TenantId, StringComparison.OrdinalIgnoreCase);

            var idpMatches = IdentityProvider == incomingGroup.IdentityProvider
                             || incomingGroup.IdentityProvider.Equals(TenantId, StringComparison.OrdinalIgnoreCase);

            return nameMatches && tenantIdMatches && idpMatches;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }

    public class GroupIdentifier
    {
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; } = "Windows";

        public override string ToString()
        {
            return $"IdP = {IdentityProvider}, TenantId = {TenantId}, GroupName = {GroupName}";
        }


        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }

    public class GroupIdentifierComparer : IEqualityComparer<GroupIdentifier>
    {
        public bool Equals(GroupIdentifier g1, GroupIdentifier g2)
        {
            if (g1 == g2)
            {
                return true;
            }

            if (g1 == null || g2 == null)
            {
                return false;
            }

            return string.Equals(g1.IdentityProvider, g2.IdentityProvider)
                   && string.Equals(g1.TenantId, g2.TenantId)
                   && string.Equals(g1.GroupName, g2.GroupName);
        }

        public int GetHashCode(GroupIdentifier groupIdentifier)
        {
            var hash = 13;
            hash = (hash * 7) + groupIdentifier.IdentityProvider.GetHashCode();
            hash = (hash * 7) + groupIdentifier.TenantId.GetHashCode();
            hash = (hash * 7) + groupIdentifier.GroupName.GetHashCode();
            return hash;
        }
    }
}