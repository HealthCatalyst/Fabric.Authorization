#Requires -RunAsAdministrator
#Requires -Version 5.1
#Requires -Modules PowerShellGet, PackageManagement, ActiveDirectory

param(
    [ValidateScript({
        if (!(Test-Path $_)) {
            throw "Path $_ does not exist. Please enter valid path to the install.config."
        }
        if (!(Test-Path $_ -PathType Leaf)) {
            throw "Path $_ is not a file. Please enter a valid path to the install.config."
        }
        return $true
    })] 
    [string] $installConfigPath = "$PSScriptRoot\install.config", 
    [switch] $quiet
)
# Import AD
Import-Module ActiveDirectory

# Import AzureAD
$minVersion = [System.Version]::new(2, 0, 2 , 4)
$azureAD = Get-Childitem -Path ./**/AzureAD.psm1 -Recurse
if ($azureAD.length -eq 0) {
    # Do not show error when AzureAD is not installed, will install instead
    $installed = Get-InstalledModule -Name AzureAD -ErrorAction "silentlycontinue"

    if (($null -eq $installed) -or ($installed.Version.CompareTo($minVersion) -lt 0)) {
        Write-Host "Installing AzureAD from Powershell Gallery"
        Install-Module AzureAD -Scope CurrentUser -MinimumVersion $minVersion -Force
        Import-Module AzureAD -Force
    }
}
else {
    Write-Host "Installing AzureAD at $($azureAD.FullName)"
    Import-Module -Name $azureAD.FullName
}

# Need the User principal methods for talking to AD
Import-Module -Name .\Install-Authorization-Utilities.psm1 -Force

# Import Identity Install Utilities
# Used for getting access tokens
$identityInstallUtilities = ".\Install-Identity-Utilities.psm1"
if (!(Test-Path $identityInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find identity install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/Fabric.Identity/master/Fabric.Identity.API/scripts/Install-Identity-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $identityInstallUtilities
}
Import-Module -Name $identityInstallUtilities -Force

# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

function Get-AuthUsers
{	
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString
    )
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)	
    $sql = "SELECT u.Id, u.SubjectId, u.IdentityProvider, u.IdentityProviderUserPrincipalName, g.GroupId, r.RoleId, p.PermissionId, p.PermissionAction, CAST('' AS NVARCHAR(100)) AS 'sourceAnchor', CAST('' AS NVARCHAR(100)) AS 'objectId'
            FROM [dbo].[Users] u	
            LEFT OUTER JOIN [dbo].[GroupUsers] g	
                ON u.SubjectId = g.SubjectId AND u.IdentityProvider = g.IdentityProvider
		    LEFT OUTER JOIN [dbo].[RoleUsers] r
                ON u.SubjectId = r.SubjectId AND u.IdentityProvider = r.IdentityProvider
            LEFT OUTER JOIN [dbo].[UserPermissions] p	
                ON u.SubjectId = p.SubjectId AND u.IdentityProvider = p.IdentityProvider
            WHERE u.IsDeleted = 0
				AND u.IdentityProvider = 'Windows'
				AND u.IdentityProviderUserPrincipalName IS NOT NULL";	
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)	

    try {	
        $connection.Open()    	
        $result = $command.ExecuteReader()	
        $userTable = New-Object System.Data.DataTable # Create a DataTable
        $userTable.Load($result) # Load the results from the DataReader into the DataTable
        $userTable.Columns['sourceAnchor'].ReadOnly = $false
        $userTable.Columns['objectId'].ReadOnly = $false
        $connection.Close()        	
    }	
    catch [System.Data.SqlClient.SqlException] {
        Write-DosMessage -Level "Fatal" -Message "An error ocurred while executing the command. Please ensure the connection string is correct and the authorization database has been setup. Connection String: $($connectionString). Error $($_.Exception.Message)"
    }    	
    
    return $userTable;	
}

function Add-AuthUsers
{	
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [System.Object[]] $userTable
    )
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)	
  	$command = New-Object System.Data.SqlClient.SqlCommand
    $command.Connection = $connection
    $command.Parameters.Add("@subjectId", [System.Data.SqlDbType]::NVarChar)
    $command.Parameters.Add("@identityProvider", [System.Data.SqlDbType]::NVarChar)
    $command.Parameters.Add("@createdBy", [System.Data.SqlDbType]::NVarChar)
    $command.Parameters.Add("@createdDateTime", [System.Data.SqlDbType]::DateTime2)
    $command.Parameters.Add("@isDeleted", [System.Data.SqlDbType]::Bit)
    $groupCount = 0
    $roleCount = 0
    $permissionCount = 0
    $connection.Open()  
    $sqlDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    foreach($user in $userTable)
    {
      $command.CommandText = "IF NOT EXISTS 
                              (SELECT 1 FROM [dbo].[Users] u
                               WHERE SubjectId = u.SubjectId 
                               AND IdentityProvider = 'OpenIDConnect:CatalystAzureAD')
                              BEGIN
                              INSERT INTO [dbo].[Users] ([SubjectId], [ParentUserId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [IsDeleted])
                              VALUES(@subjectId, @parentId, @identityProvider, @createdBy, @createdDateTime, @isDeleted)
                              END;"
      $command.Parameters["@subjectId"].Value = $user.objectId
      $command.Parameters.Add("@parentId", [System.Data.SqlDbType]::Int)
      $command.Parameters["@parentId"].Value = $user.Id
      $command.Parameters["@identityProvider"].Value = "OpenIDConnect:CatalystAzureAD"
      $command.Parameters["@createdBy"].Value = "Migrate-ADUsers-AzureAD"
      $command.Parameters["@createdDateTime"].Value = $sqlDate
      $command.Parameters["@isDeleted"].Value = 0
      $command.ExecuteNonQuery()

      $command.Parameters.RemoveAt("@parentId")

      if(![string]::IsNullOrEmpty($user.GroupId))
      {
        $command.CommandText = "IF NOT EXISTS 
                                (SELECT 1 FROM [dbo].[GroupUsers] g
                                 WHERE SubjectId = g.SubjectId
                                 AND IdentityProvider = 'OpenIDConnect:CatalystAzureAD')
                                BEGIN
                                INSERT INTO [dbo].[GroupUsers] ([SubjectId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [GroupId], [IsDeleted])
                                VALUES(@subjectId, @identityProvider, @createdBy, @createdDateTime, @groupId, @isDeleted)
                                END;"
        $command.Parameters["@subjectId"].Value = $user.objectId
        $command.Parameters["@identityProvider"].Value = "OpenIDConnect:CatalystAzureAD"
        $command.Parameters["@createdBy"].Value = "Migrate-ADUsers-AzureAD"
        $command.Parameters["@createdDateTime"].Value = $sqlDate
        $command.Parameters.Add("@groupId", [System.Data.SqlDbType]::UniqueIdentifier)
        $command.Parameters["@groupId"].Value = $user.GroupId
        $command.Parameters["@isDeleted"].Value = 0
        $groupCount++
        $command.ExecuteNonQuery()
      }
      if($groupCount -eq 1)
      {
        $command.Parameters.RemoveAt("@groupId")
        $groupCount = 0
      }
      if(![string]::IsNullOrEmpty($user.RoleID))
      {
        $command.CommandText = "IF NOT EXISTS 
                                (SELECT 1 FROM [dbo].[RoleUsers] r
                                 WHERE SubjectId = r.SubjectId 
                                 AND IdentityProvider = 'OpenIDConnect:CatalystAzureAD')
                                BEGIN
                                INSERT INTO [dbo].[RoleUsers] ([SubjectId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [RoleId], [IsDeleted])
                                VALUES(@subjectId, @identityProvider, @createdBy, @createdDateTime, @roleId, @isDeleted)
                                END;"
        $command.Parameters["@subjectId"].Value = $user.objectId
        $command.Parameters["@identityProvider"].Value = "OpenIDConnect:CatalystAzureAD"
        $command.Parameters["@createdBy"].Value = "Migrate-ADUsers-AzureAD"
        $command.Parameters["@createdDateTime"].Value = $sqlDate
        $command.Parameters.Add("@roleId", [System.Data.SqlDbType]::UniqueIdentifier)
        $command.Parameters["@roleId"].Value = $user.RoleId
        $command.Parameters["@isDeleted"].Value = 0
        $roleCount++
        $command.ExecuteNonQuery()
      }
      if($roleCount -eq 1)
      {
        $command.Parameters.RemoveAt("@roleId")
        $roleCount = 0
      }
      if(![string]::IsNullOrEmpty($user.PermissionID))
      {
        $command.CommandText = "IF NOT EXISTS 
                                (SELECT 1 FROM [dbo].[UsersPermissions] p
                                 WHERE SubjectId = p.SubjectId
                                 AND IdentityProvider = 'OpenIDConnect:CatalystAzureAD')
                                BEGIN
                                INSERT INTO [dbo].[UserPermissions] ([SubjectId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [PermissionId], [PermissionAction], [IsDeleted])
                                VALUES(@subjectId, @identityProvider, @createdBy, @createdDateTime, @permissionId, @permissionAction, @isDeleted)
                                END;"
        $command.Parameters["@subjectId"].Value = $user.objectId
        $command.Parameters["@identityProvider"].Value = "OpenIDConnect:CatalystAzureAD"
        $command.Parameters["@createdBy"].Value = "Migrate-ADUsers-AzureAD"
        $command.Parameters["@createdDateTime"].Value = $sqlDate
        $command.Parameters.Add("@permissionId", [System.Data.SqlDbType]::UniqueIdentifier)
        $command.Parameters.Add("@permissionAction", [System.Data.SqlDbType]::Int)
        $command.Parameters["@permissionId"].Value = $user.PermissionId
        $command.Parameters["@permissionAction"].Value = $user.PermissionAction
        $command.Parameters["@isDeleted"].Value = 0
        $permissionCount++
        $command.ExecuteNonQuery()
      }
      if($permissionCount -eq 1)
      {
        $command.Parameters.RemoveAt("@permissionId")
        $command.Parameters.RemoveAt("@permissionAction")
        $permissionCount = 0
      }
    }
    $connection.Close()        	
}

function Get-ADUsers
{
    param(
        [Parameter(Mandatory=$true)]
        [System.Object[]] $userTable,
        [Parameter(Mandatory=$true)]
        [string] $domain
    )

    # Add sourceAnchor to existing auth table (userTable) passed in

    foreach($user in $userTable)
	{
       [string] $userPrincipal = $user.IdentityProviderUserPrincipalName.ToString()
	   # find the userprincipal in AD from the AuthorizationDB IdentityProviderUserPrincipalName
	   $pc = Get-PrincipalContext -domain $domain
	   $samAccountName = Get-SamAccountFromAccountName -accountName $user.IdentityProviderUserPrincipalName

       # get all properties of the user with $samAccountName
       $entry = Get-ADUser $samAccountName -Properties *

       # returns a System.Byte[], needs to be converted to a base 64 string to compare with AzureAD ImmutableId         
       # the system.Guid(, ) with the comma is interpreted by powershell as passing a literal
	   # array, instead of a list of arguments. Issue with ms-DS-ConsistencyGuid only working with
	   # the comma.
       $userMap = $entry.ObjectGUID
       if (![string]::IsNullOrEmpty($userMap))
       {
         # put the user objectGUID into base 64 string format to compare against the AzureAD ImmutableId 
         $userMapGuid = [System.Convert]::ToBase64String((new-Object system.Guid(, $userMap)).ToByteArray())
         if (![string]::IsNullOrEmpty($userMapGuid)) 
         {
           $user.sourceAnchor = $userMapGuid
		 }
         else
         {
           # cannot convert the sourceAnchor to base 64 string
           Write-DosMessage -Level "Error" -Message "Not able to convert user '$userPrincipal' objectGUID property to base 64 string"
         }
       }
       else
       {        
         #$userMap = $entry.Properties["ms-DS-ConsistencyGuid"]
         $userMap = $entry."ms-Ds-ConsistencyGuid" 
         if (![string]::IsNullOrEmpty($userMap))
         {
           # put the user ms-DS-ConsistenceyGuid into base 64 string format to compare against the AzureAD ImmutableId 
           $userMapGuid = [System.Convert]::ToBase64String((new-Object system.Guid(, $userMap)).ToByteArray())
           if (![string]::IsNullOrEmpty($userMapGuid)) 
           {
             $user.sourceAnchor = $userMapGuid
		   }
           else
           {
             # cannot convert the sourceAnchor to base 64 string
             Write-DosMessage -Level "Error" -Message "Not able to convert user '$userPrincipal' ms-DS-ConsistencyGuid property to base 64 string"
           }
         }
         else
         {
           # cannot find the sourceAnchor to map AD and AzureAD Users
           Write-DosMessage -Level "Error" -Message "Not able to map user '$userPrincipal', in AD to AzureAD"
         }
       }
    }
    return $userTable
}

function Get-AzureADUsers
{
    param(
        [Parameter(Mandatory=$true)]
        [System.Object[]] $userTable,
        [Parameter(Mandatory=$true)]
        [string[]] $tenants
    )

    # Add sourceAnchor to existing auth table (userTable) passed in

    if($null -ne $tenants) 
    {
      foreach($tenant in $tenants) 
      { 
        Write-Host "Enter credentials for tenant specified: $tenant"
        Connect-AzureADTenant -tenantId $tenant

        # get user ImmutableId from AzureAD using AD user ObjectGUID converted to base 64 string
        foreach($user in $userTable)
	    {
          [string] $userPrincipal = $user.IdentityProviderUserPrincipalName.ToString()
          $foundUserMatchId = Get-AzureADUser | Where-Object {$_.ImmutableId -eq $user.sourceAnchor} | Select-Object ObjectId
          if ($null -ne $foundUserMatchId) 
          {
            $user.objectId = $foundUserMatchId.ObjectId
	      }
          else
          {
             # cannot find the ImmutableId to map AD and AzureAD Users
             Write-DosMessage -Level "Error" -Message "Not able to map user '$userPrincipal', in AzureAD to AD"
          }
	    }
        Disconnect-AzureAD
      }
    }
    return $userTable
}

Write-DosMessage -Level "Information" -Message "Using install.config: $installConfigPath"
$installSettingsScope = "authorization"
$installSettings = Get-InstallationSettings $installSettingsScope -installConfigPath $installConfigPath

$commonSettingsScope = "common"
$commonInstallSettings = Get-InstallationSettings $commonSettingsScope -installConfigPath $installConfigPath
Set-LoggingConfiguration -commonConfig $commonInstallSettings

$tenants = Get-Tenants -installConfigPath $installConfigPath

$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $installSettings.sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet

$authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $installSettings.authorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet

# Connect to the authorization database and get all the users and store them in a data table
# Method to get authorization database users
$authUserTable = Get-AuthUsers -connectionString $authorizationDatabase.DbConnectionString

# Connect to AD to get ObjectId/ms-DS-ConsistenceyGuid for each user in authorization
$currentUserDomain = Get-CurrentUserDomain -quiet $quiet

$ADUserTable = Get-ADUsers -userTable $authUserTable -domain $currentUserDomain

$AADUserTable = Get-AzureADUsers -userTable $ADUserTable -tenants $tenants

# Add azureAD user to the AuthorizationDB Users table
$authUserTable = Add-AuthUsers -connectionString $authorizationDatabase.DbConnectionString -userTable $AADUserTable
