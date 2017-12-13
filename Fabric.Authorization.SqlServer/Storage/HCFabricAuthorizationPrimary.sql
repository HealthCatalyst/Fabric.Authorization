/*
Do not change the database path or name variables.
Any sqlcmd variables will be properly substituted during 
build and deployment.
*/
ALTER DATABASE [$(DatabaseName)]
	ADD FILE
	(
		NAME = [HCFabricAuthorizationPrimary],
		FILENAME = '$(FabricAuthorizationDataMountPoint)\HC$(DatabaseName)Primary.mdf'
	)