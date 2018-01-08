CREATE TABLE [dbo].[Groups](
	[Name] [nvarchar](200) NOT NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDateTimeUtc] [datetime] NOT NULL,
	[GroupId] [nvarchar](max) NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[ModifiedDateTimeUtc] [datetime] NULL,
	[Source] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_Groups] PRIMARY KEY NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [HCFabricAuthorizationData1]
) ON [HCFabricAuthorizationData1] TEXTIMAGE_ON [HCFabricAuthorizationData1]
GO

CREATE UNIQUE CLUSTERED INDEX [IX_Groups_Id] ON [dbo].[Groups]
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [HCFabricAuthorizationData1]
GO

ALTER TABLE [dbo].[Groups] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
