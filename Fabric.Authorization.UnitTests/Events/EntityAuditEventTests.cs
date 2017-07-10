using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Models;
using Xunit;

namespace Fabric.Authorization.UnitTests.Events
{
    public class EntityAuditEventTests
    {
        [Fact]
        public void CreateEntityAudit_WithEntity_Succeeds()
        {
            var permission = CreateTestPermission();

            var evt = new EntityAuditEvent<Permission>(EventTypes.EntityCreatedEvent, permission.Id.ToString(), permission);

            AssertBaseEntity(permission, evt);
            Assert.Equal(permission.Id, evt.Entity.Id);
        }

        [Fact]
        public void CreateEntityAudit_WithoutPermission_Succeeds()
        {
            var permission = CreateTestPermission();

            var evt = new EntityAuditEvent<Permission>(EventTypes.EntityCreatedEvent, permission.Id.ToString());

            AssertBaseEntity(permission, evt);
            Assert.Null(evt.Entity);
        }

        private void AssertBaseEntity(Permission permission, EntityAuditEvent<Permission> evt)
        {
            Assert.Equal(permission.Id.ToString(), evt.EntityId);
            Assert.Equal(permission.GetType().FullName, evt.EntityType);
        }

        private Permission CreateTestPermission()
        {
            return new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            };
        }
    }
}
