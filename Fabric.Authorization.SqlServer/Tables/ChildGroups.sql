CREATE TABLE [dbo].[ChildGroups]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
    [ParentGroupId] UNIQUEIDENTIFIER NOT NULL, 
    [ChildGroupId] UNIQUEIDENTIFIER NOT NULL, 
	[IsDeleted] BIT NOT NULL,
    [CreatedBy] NVARCHAR(MAX) NOT NULL, 
    [CreatedDateTimeUtc] DATETIME NOT NULL, 
    [ModifiedBy] NVARCHAR(MAX) NULL, 
    [ModifiedDateTimeUtc] DATETIME NULL,
CONSTRAINT [PK_ChildGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [HCFabricAuthorizationData1]
) ON [HCFabricAuthorizationData1]
GO

CREATE NONCLUSTERED INDEX [IX_ChildGroups_ParentGroupId] ON [dbo].[ChildGroups]
(
	[ParentGroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [HCFabricAuthorizationIndex1]
GO

ALTER TABLE [dbo].[ChildGroups] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO

ALTER TABLE [dbo].[ChildGroups]  WITH CHECK ADD CONSTRAINT [FK_ChildGroups_Groups_ParentGroupId] FOREIGN KEY([ParentGroupId])
REFERENCES [dbo].[Groups] ([GroupId])
ON DELETE NO ACTION
GO

ALTER TABLE [dbo].[ChildGroups] CHECK CONSTRAINT [FK_ChildGroups_Groups_ParentGroupId]
GO

ALTER TABLE [dbo].[ChildGroups]  WITH CHECK ADD CONSTRAINT [FK_ChildGroups_Groups_ChildGroupId] FOREIGN KEY([ChildGroupId])
REFERENCES [dbo].[Groups] ([GroupId])
ON DELETE NO ACTION
GO

ALTER TABLE [dbo].[ChildGroups] CHECK CONSTRAINT [FK_ChildGroups_Groups_ChildGroupId]
GO

CREATE UNIQUE INDEX [IX_ChildGroups_ParentIdChildId] 
	ON [ChildGroups] ([ParentGroupId], [ChildGroupId])
	WHERE IsDeleted = 0
ON [HCFabricAuthorizationIndex1];