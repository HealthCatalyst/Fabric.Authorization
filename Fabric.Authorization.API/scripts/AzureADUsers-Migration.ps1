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
Import-Module -Name "$PSScriptRoot\AzureAD-Utilities.psm1" -Force

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
$credentials = @{}

function Get-AuthUsers
{	
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString
    )
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)	
    $sql = "WITH Users AS
			(
				SELECT u.Id, u.SubjectId, u.IdentityProvider, CAST('' AS NVARCHAR(100)) AS 'sourceAnchor', CAST('' AS NVARCHAR(100)) AS 'objectId'
				FROM [dbo].[Users] u	
				WHERE u.IsDeleted = 0  
				AND u.IdentityProvider = 'Windows'
				GROUP BY u.Id, u.SubjectId, u.IdentityProvider
			)
			SELECT * FROM Users;

			WITH Groups AS
			(
				SELECT u.Id, u.SubjectId, u.IdentityProvider, g.GroupId
				FROM [dbo].[Users] u	
				LEFT OUTER JOIN [dbo].[GroupUsers] g	
					ON u.SubjectId = g.SubjectId AND u.IdentityProvider = g.IdentityProvider AND g.IsDeleted = 0
				WHERE u.IsDeleted = 0  
				AND u.IdentityProvider = 'Windows'
				GROUP BY u.Id, u.SubjectId, u.IdentityProvider, g.GroupId
			)
			SELECT * FROM Groups;

			WITH Roles AS
			(
				SELECT u.Id, u.SubjectId, u.IdentityProvider, r.RoleId
				FROM [dbo].[Users] u	
				LEFT OUTER JOIN [dbo].[RoleUsers] r
					ON u.SubjectId = r.SubjectId AND u.IdentityProvider = r.IdentityProvider AND r.IsDeleted = 0
				WHERE u.IsDeleted = 0  
				AND u.IdentityProvider = 'Windows'
				GROUP BY u.Id, u.SubjectId, u.IdentityProvider, r.RoleId
			)
			SELECT * FROM Roles;

			WITH UserPermissions AS
			(
				SELECT u.Id, u.SubjectId, u.IdentityProvider, p.PermissionId, p.PermissionAction
				FROM [dbo].[Users] u	
				LEFT OUTER JOIN [dbo].[UserPermissions] p	
					ON u.SubjectId = p.SubjectId AND u.IdentityProvider = p.IdentityProvider AND p.IsDeleted = 0
				WHERE u.IsDeleted = 0  
				AND u.IdentityProvider = 'Windows'
				GROUP BY u.Id, u.SubjectId, u.IdentityProvider, p.PermissionId, p.PermissionAction
			)
			SELECT * FROM UserPermissions;";	
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)	

    try {	
          $connection.Open()    	
          $result = $command.ExecuteReader()

          # Create new Data Set
          $DataSet = New-Object System.Data.DataSet
                    
          # Load the users result set
          $userTable = New-Object System.Data.DataTable # Create a DataTable
          $userTable.Load($result) # Load the results from the DataReader into the DataTable
          $userTable.Columns['sourceAnchor'].ReadOnly = $false
          $userTable.Columns['objectId'].ReadOnly = $false

          $DataSet.Tables.Add($userTable)
             
          # Load the groups result set
          $groupTable = New-Object System.Data.DataTable # Create a DataTable
          $groupTable.Load($result) # Load the results from the DataReader into the DataTable

          $DataSet.Tables.Add($groupTable)
      
          # Load the roles result set
          $roleTable = New-Object System.Data.DataTable # Create a DataTable
          $roleTable.Load($result) # Load the results from the DataReader into the DataTable

          $DataSet.Tables.Add($roleTable)
     
          # Load the permissions result set
          $permissionTable = New-Object System.Data.DataTable # Create a DataTable
          $permissionTable.Load($result) # Load the results from the DataReader into the DataTable

          $DataSet.Tables.Add($permissionTable)
      
        $connection.Close()        	
    }	
    catch [System.Data.SqlClient.SqlException] {
        Write-DosMessage -Level "Fatal" -Message "An error ocurred while executing the command. Please ensure the connection string is correct and the authorization database has been setup. Connection String: $($connectionString). Error $($_.Exception)"
    }    	
    
    return $DataSet;	
}

function Add-AuthUsers
{	
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [System.Data.DataSet] $authDataSet
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
	$sqlDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
	$totalUsersAdded = 0
	$totalGroupUsersAdded = 0
    $totalRoleUsersAdded = 0
    $totalUserPermissionsAdded = 0

	try{
        $connection.Open()  

        $transaction = $connection.BeginTransaction("MergeAADUsers")

        $command.Transaction = $transaction;
        
        $userTable = $authDataSet.Tables[0]
		foreach($user in $userTable)
		{
          $resultUsers = 0
          $resultGroupUsers = 0
          $resultRoleUsers = 0
          $resultUserPermissions = 0

		  if (![string]::IsNullOrEmpty($user.objectId))
		  {
			  $command.CommandText = "IF NOT EXISTS 
									  (SELECT 1 FROM [dbo].[Users] u
									   WHERE SubjectId = @subjectId 
									   AND IdentityProvider = 'AzureActiveDirectory')
									  BEGIN
                             		  INSERT INTO [dbo].[Users] ([SubjectId], [ParentUserId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [IsDeleted])
									  VALUES(@subjectId, @parentId, @identityProvider, @createdBy, @createdDateTime, @isDeleted)
                         			  END;"
			  $command.Parameters["@subjectId"].Value = $user.objectId
			  $command.Parameters.Add("@parentId", [System.Data.SqlDbType]::Int)
			  $command.Parameters["@parentId"].Value = $user.Id
			  $command.Parameters["@identityProvider"].Value = "AzureActiveDirectory"
			  $command.Parameters["@createdBy"].Value = "AzureADUsers-Migration"
			  $command.Parameters["@createdDateTime"].Value = $sqlDate
			  $command.Parameters["@isDeleted"].Value = 0
              # if you run the script twice or more and some users have already been added
              # they show as -1 since they aren't inserted more than once
              $resultUsers = $command.ExecuteNonQuery()
              if($resultUsers -gt 0)
              {              
                $totalUsersAdded += $resultUsers
			  }
			  $command.Parameters.RemoveAt("@parentId")

            $groupTable = $authDataSet.Tables[1]
		    foreach ($group in $groupTable)
		    {
			    if(![string]::IsNullOrEmpty($group.GroupId) -and $user.Id -eq $group.Id)
			    {
			    $command.CommandText = "IF NOT EXISTS 
									    (SELECT 1 FROM [dbo].[GroupUsers] g
										    WHERE SubjectId = @subjectId
                                            AND GroupId = @groupId
										    AND IdentityProvider = 'AzureActiveDirectory')
									    BEGIN
									    INSERT INTO [dbo].[GroupUsers] ([SubjectId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [GroupId], [IsDeleted])
									    VALUES(@subjectId, @identityProvider, @createdBy, @createdDateTime, @groupId, @isDeleted)
									    END;"
			    $command.Parameters["@subjectId"].Value = $user.objectId
			    $command.Parameters["@identityProvider"].Value = "AzureActiveDirectory"
			    $command.Parameters["@createdBy"].Value = "AzureADUsers-Migration"
			    $command.Parameters["@createdDateTime"].Value = $sqlDate
			    $command.Parameters.Add("@groupId", [System.Data.SqlDbType]::UniqueIdentifier)
			    $command.Parameters["@groupId"].Value = $group.GroupId
			    $command.Parameters["@isDeleted"].Value = 0
			    $groupCount++

                $resultGroupUsers = $command.ExecuteNonQuery()
                if($resultGroupUsers -gt 0)
                {   
			        $totalGroupUsersAdded += $resultGroupUsers
	    	    }

			    if($groupCount -eq 1)
			    {
			        $command.Parameters.RemoveAt("@groupId")
				    $groupCount = 0
			    }
		      }
		    }
            $roleTable = $authDataSet.Tables[2]
		    foreach ($role in $roleTable)
		    {
			    if(![string]::IsNullOrEmpty($role.RoleID) -and $user.Id -eq $role.Id)
			    {
			    $command.CommandText = "IF NOT EXISTS 
									    (SELECT 1 FROM [dbo].[RoleUsers] r
										    WHERE SubjectId = @subjectId
                                            AND RoleId = @roleId
										    AND IdentityProvider = 'AzureActiveDirectory')
									    BEGIN
									    INSERT INTO [dbo].[RoleUsers] ([SubjectId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [RoleId], [IsDeleted])
									    VALUES(@subjectId, @identityProvider, @createdBy, @createdDateTime, @roleId, @isDeleted)
									    END;"
			    $command.Parameters["@subjectId"].Value = $user.objectId
			    $command.Parameters["@identityProvider"].Value = "AzureActiveDirectory"
			    $command.Parameters["@createdBy"].Value = "AzureADUsers-Migration"
			    $command.Parameters["@createdDateTime"].Value = $sqlDate
			    $command.Parameters.Add("@roleId", [System.Data.SqlDbType]::UniqueIdentifier)
			    $command.Parameters["@roleId"].Value = $role.RoleId
			    $command.Parameters["@isDeleted"].Value = 0
			    $roleCount++

                $resultRoleUsers = $command.ExecuteNonQuery()
                if($resultRoleUsers -gt 0)
                {   
			    $totalRoleUsersAdded += $resultRoleUsers
                }

			    if($roleCount -eq 1)
			    {
				    $command.Parameters.RemoveAt("@roleId")
				    $roleCount = 0
			    }
		      }
		    }
            $permissionTable = $authDataSet.Tables[3]
		    foreach ($permission in $permissionTable)
		    {
			    if(![string]::IsNullOrEmpty($permission.PermissionID) -and $user.Id -eq $permission.Id)
			    {
			    $command.CommandText = "IF NOT EXISTS 
									    (SELECT 1 FROM [dbo].[UserPermissions] p
										    WHERE SubjectId = @subjectId
                                            AND PermissionId = @permissionId
										    AND IdentityProvider = 'AzureActiveDirectory')
									    BEGIN
									    INSERT INTO [dbo].[UserPermissions] ([SubjectId], [IdentityProvider], [CreatedBy], [CreatedDateTimeUtc], [PermissionId], [PermissionAction], [IsDeleted])
									    VALUES(@subjectId, @identityProvider, @createdBy, @createdDateTime, @permissionId, @permissionAction, @isDeleted)
									    END;"
			    $command.Parameters["@subjectId"].Value = $user.objectId
			    $command.Parameters["@identityProvider"].Value = "AzureActiveDirectory"
			    $command.Parameters["@createdBy"].Value = "AzureADUsers-Migration"
			    $command.Parameters["@createdDateTime"].Value = $sqlDate
			    $command.Parameters.Add("@permissionId", [System.Data.SqlDbType]::UniqueIdentifier)
			    $command.Parameters.Add("@permissionAction", [System.Data.SqlDbType]::Int)
			    $command.Parameters["@permissionId"].Value = $permission.PermissionId
			    $command.Parameters["@permissionAction"].Value = $permission.PermissionAction
			    $command.Parameters["@isDeleted"].Value = 0
			    $permissionCount++

                $resultUserPermissions = $command.ExecuteNonQuery()
                if($resultUserPermissions -gt 0)
                {  
				    $totalUserPermissionsAdded += $resultUserPermissions
			    }
			    if($permissionCount -eq 1)
			    {
				    $command.Parameters.RemoveAt("@permissionId")
				    $command.Parameters.RemoveAt("@permissionAction")
				    $permissionCount = 0
			    }
		      }
		    }
          }
        }
        $transaction.Commit()
		$connection.Close() 
        if ($totalUsersAdded -lt 1){$totalUsersAdded = 0} 
        if ($totalGroupUsersAdded -lt 1){$totalGroupUsersAdded = 0} 
        if ($totalRoleUsersAdded -lt 1){$totalRoleUsersAdded = 0} 
        if ($totalUserPermissionsAdded -lt 1){$totalUserPermissionsAdded = 0} 
        $userTableCount = $userTable.Rows.Count
		Write-DosMessage -Level Information "$totalUsersAdded Azure AD user(s) added out of $userTableCount AD user(s) from the Users table, in the Authorization database"
		Write-DosMessage -Level Information "$totalGroupUsersAdded group user(s) added"
		Write-DosMessage -Level Information "$totalRoleUsersAdded role user(s) added"
		Write-DosMessage -Level Information "$totalUserPermissionsAdded user permission(s) added"
	}
	catch [System.Data.SqlClient.SqlException] {
        Write-DosMessage -Level "Fatal" -Message "An error ocurred while executing the command. Please ensure the connection string is correct and the authorization database has been setup. Connection String: $($connectionString). Error $($_.Exception)"
        try
        {
          $transaction.Rollback()
        }
        catch
        {
          # This catch block will handle any errors that may have occurred
          # on the server that would cause the rollback to fail, such as
          # a closed connection.
          Console.WriteLine("Rollback Exception Type: {0}", $Error[0].Exception.GetType());
          Console.WriteLine("  Message: {0}", $Error[0].Exception.Message());
        }
    } 
}

function Get-ADUsers
{
    param(
        [Parameter(Mandatory=$true)]
        [System.Data.DataSet] $authDataSet,
        [Parameter(Mandatory=$true)]
        [string] $domain
    )

    # Add sourceAnchor to existing auth table (userTable) passed in
    $userTable = $authDataSet.Tables[0]
    foreach($user in $userTable)
	{
       # cleanup variables 
       # if AD User is mispelled the values from the previous user will be entered for the next user
       $pc = $null
       $samAccountName = $null
       $entry = $null
       $userMap = $null
       $userMapGuid = $null

       [string] $subjectId = $user.SubjectId.ToString()
	   # find the samAccountName in AD from the AuthorizationDB SubjectId
	   $pc = Get-PrincipalContext -domain $domain
	   $samAccountName = Get-SamAccountFromAccountName -accountName $subjectId

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
           Write-DosMessage -Level "Error" -Message "Not able to convert user '$subjectId' objectGUID property to base 64 string"
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
             Write-DosMessage -Level "Error" -Message "Not able to convert user '$subjectId' ms-DS-ConsistencyGuid property to base 64 string"
           }
         }
         else
         {
           # cannot find the sourceAnchor to map AD and AzureAD Users
           Write-DosMessage -Level "Error" -Message "Not able to map user '$subjectId', in AD to AzureAD"
         }
       }
    }
    return $authDataSet
}

function Get-AzureADUsers
{
    param(
        [Parameter(Mandatory=$true)]
        [System.Data.DataSet] $authDataSet,
        [Parameter(Mandatory=$true)]
        [string[]] $tenants
    )

    # Add sourceAnchor to existing auth table (userTable) passed in

    if($null -ne $tenants) 
    {
      foreach($tenantId in $tenants) 
      { 
       # Prompting user for credentials to connect to tenant"
        # $credentials = Connect-AzureADTenant -tenantId $tenant
        if ($credentials.ContainsKey($tenantId)) {
            Write-DosMessage -Level Information "Using cached credentials to connect to tenant $($tenantId)"
            Connect-AzureADTenant -tenantId $tenantId -credential $credentials[$tenantId] | Out-Null
        } else {
            Write-DosMessage -Level Information "Credentials not cached - prompting user for credentials to connect to tenant $($tenantId)"
            $authenticationFailed = $true
            $maxRetryAttempts = 3

            do {
                try {
                    $credential = Connect-AzureADTenant -tenantId $tenantId
                    if (!$credentials.ContainsKey($tenantId)) {
                        Write-DosMessage -Level Information -Message "Caching credentials for tenant $($tenantId)."
                        $credentials.Add($tenantId, $credential)
                        $authenticationFailed = $false
                    }
                }
                catch [Microsoft.Open.Azure.AD.CommonLibrary.AadAuthenticationFailedException], [Microsoft.Open.Azure.AD.CommonLibrary.AadNeedAuthenticationException] {
                    Write-DosMessage -Level Error -Message "Error authenticating user with Azure AD tenant $($tenantId). Please try again."
                    $authenticationFailed = $true
                }
                catch {
                    $errorMsg = "Unexpected error while connecting to Azure AD: $($_.Exception)"
                    Write-DosMessage -Level Error -Message $errorMsg
                    throw $errorMsg
                }
                $maxRetryAttempts--
                if ($maxRetryAttempts -eq 0) {
                    Write-DosMessage -Level Information -Message "You have reached 3 login attempts. Exiting login prompts."
                }
            } while ($authenticationFailed -eq $true -and $maxRetryAttempts -gt 0)

        # get user ImmutableId from AzureAD using AD user ObjectGUID converted to base 64 string
        $userTable = $authDataSet.Tables[0]
        foreach($user in $userTable)
	    {
		  $userPrincipal = $null
		  $foundUserMatchId = $null

          [string] $userPrincipal = $user.SubjectId.ToString()
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
  }
    return $authDataSet
}

Write-DosMessage -Level "Information" -Message "Using install.config: $installConfigPath"
$installSettingsScope = "authorization"
$installSettings = Get-InstallationSettings $installSettingsScope -installConfigPath $installConfigPath

$commonSettingsScope = "common"
$commonInstallSettings = Get-InstallationSettings $commonSettingsScope -installConfigPath $installConfigPath
Set-LoggingConfiguration -commonConfig $commonInstallSettings

$tenants = Get-AzureADTenants -installConfigPath $installConfigPath

$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $installSettings.sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet

$authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $installSettings.authorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet

# Connect to the authorization database and get all the users and store them in a data table
# Method to get authorization database users
$authDataSet = Get-AuthUsers -connectionString $authorizationDatabase.DbConnectionString

# Connect to AD to get ObjectId/ms-DS-ConsistenceyGuid for each user in authorization
$currentUserDomain = Get-CurrentUserDomain -quiet $quiet

$authADDataSet = Get-ADUsers -authDataSet $authDataSet -domain $currentUserDomain

$authAADDataSet = Get-AzureADUsers -authDataSet $authADDataSet -tenants $tenants

# Add azureAD user to the AuthorizationDB Users table
$authUserTable = Add-AuthUsers -connectionString $authorizationDatabase.DbConnectionString -authDataSet $authAADDataSet