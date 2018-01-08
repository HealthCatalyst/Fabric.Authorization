CREATE TABLE [dbo].[GroupRoles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDateTimeUtc] [datetime] NOT NULL,
	[GroupName] [nvarchar](200) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[ModifiedDateTimeUtc] [datetime] NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_GroupRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [HCFabricAuthorizationData1],
 CONSTRAINT [AK_GroupRoles_GroupName_RoleId] UNIQUE NONCLUSTERED 
(
	[GroupName] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [HCFabricAuthorizationData1]
) ON [HCFabricAuthorizationData1]
GO

CREATE NONCLUSTERED INDEX [IX_GroupRoles_RoleId] ON [dbo].[GroupRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [HCFabricAuthorizationIndex1]
GO

ALTER TABLE [dbo].[GroupRoles] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO

ALTER TABLE [dbo].[GroupRoles]  WITH CHECK ADD  CONSTRAINT [FK_GroupRoles_Groups_GroupName] FOREIGN KEY([GroupName])
REFERENCES [dbo].[Groups] ([Name])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[GroupRoles] CHECK CONSTRAINT [FK_GroupRoles_Groups_GroupName]
GO

ALTER TABLE [dbo].[GroupRoles]  WITH CHECK ADD  CONSTRAINT [FK_GroupRoles_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([RoleId])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[GroupRoles] CHECK CONSTRAINT [FK_GroupRoles_Roles_RoleId]
GO
