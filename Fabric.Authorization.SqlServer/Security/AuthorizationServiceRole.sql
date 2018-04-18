
CREATE ROLE [AuthorizationServiceRole];
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Clients] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Grains] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[GroupRoles] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Groups] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[GroupUsers] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Permissions] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[RolePermissions] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Roles] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[RoleUsers] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[SecurableItems] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[UserPermissions] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Users] TO AuthorizationServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[EventLogs] to AuthorizationServiceRole;
GO