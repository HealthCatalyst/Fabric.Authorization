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

        public string IdentityProvider { get; set; }

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
                                  || incomingGroup.TenantIdEquals(TenantId);

            var idpMatches = IdentityProvider == incomingGroup.IdentityProvider
                             || incomingGroup.IdentityProviderEquals(TenantId);

            return nameMatches && tenantIdMatches && idpMatches;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool SourceEquals(string source)
        {
            return Source == source
                   || string.Equals(Source, source, StringComparison.OrdinalIgnoreCase);
        }

        public bool IdentityProviderEquals(string identityProvider)
        {
            return IdentityProvider == identityProvider
                   || string.Equals(IdentityProvider, identityProvider, StringComparison.OrdinalIgnoreCase);
        }

        public bool TenantIdEquals(string tenantId)
        {
            return TenantId == tenantId
                   || string.Equals(TenantId, tenantId, StringComparison.OrdinalIgnoreCase);
        }

        public bool NameEquals(string name)
        {
            return Name == name
                   || string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class GroupIdentifier
    {
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; }

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

            return string.Equals(g1.IdentityProvider, g2.IdentityProvider, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(g1.TenantId, g2.TenantId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(g1.GroupName, g2.GroupName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(GroupIdentifier groupIdentifier)
        {
            var hash = 13;
            if (!string.IsNullOrWhiteSpace(groupIdentifier.IdentityProvider))
            {
                hash = (hash * 7) + groupIdentifier.IdentityProvider.ToLower().GetHashCode();
            }

            if (!string.IsNullOrWhiteSpace(groupIdentifier.TenantId))
            {
                hash = (hash * 7) + groupIdentifier.TenantId.ToLower().GetHashCode();
            }

            hash = (hash * 7) + groupIdentifier.GroupName.ToLower().GetHashCode();
            return hash;
        }
    }
}