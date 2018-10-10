# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

# Import Dos Install Utilities
$minVersion = [System.Version]::new(1, 0, 164 , 0)
$dosInstallUtilities = Get-Childitem -Path ./**/DosInstallUtilities.psm1 -Recurse
if ($dosInstallUtilities.length -eq 0) {
    $installed = Get-Module -Name DosInstallUtilities
    if ($null -eq $installed) {
        $installed = Get-InstalledModule -Name DosInstallUtilities
    }

    if (($null -eq $installed) -or ($installed.Version.CompareTo($minVersion) -lt 0)) {
        Write-Host "Installing DosInstallUtilities from Powershell Gallery"
        Install-Module DosInstallUtilities -Scope CurrentUser -MinimumVersion 1.0.164.0 -Force
        Import-Module DosInstallUtilities -Force
    }
}
else {
    Write-Host "Installing DosInstallUtilities at $($dosInstallUtilities.FullName)"
    Import-Module -Name $dosInstallUtilities.FullName
}

$dosGrain = "dos"
$dataMartsSecurable = "datamarts"
$dosAdminRole = "dosadmin"
$dataMartAdminRole = "DataMartAdmin"
$fabricInstallerClientId = "fabric-installer"
$dosAdminGroupName = "DosAdmins"

function Get-FullyQualifiedMachineName() {
	return "https://$env:computername.$((Get-WmiObject Win32_ComputerSystem).Domain.tolower())"
}

function Get-Headers 
{
    param(
        [string] $accessToken
    )
    $headers = @{"Accept" = "application/json"}
    if ($accessToken) {
        $headers.Add("Authorization", "Bearer $accessToken")
    }
    return $headers
}

function Invoke-Get
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $url,
        [string] $accessToken
    )
    $headers = Get-Headers -accessToken $accessToken
        
    $getResponse = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    return $getResponse
}

function Invoke-Post
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $url,
        [Parameter(Mandatory=$true)]
        [object] $body,
        [string] $accessToken
    )

    $headers = Get-Headers -accessToken $accessToken

    if (!($body -is [String])) {
        $body = (ConvertTo-Json $body)
    }
    
    $postResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
    return $postResponse
}

function Get-Group{
    param(
        [Parameter(Mandatory=$true)]
        [string] $name, 
        [Parameter(Mandatory=$true)]
        [string] $authorizationServiceUrl, 
        [Parameter(Mandatory=$true)]
        [string] $accessToken
    )
    $url = "$authorizationServiceUrl/groups/$name"
    return Invoke-Get -url $url -accessToken $accessToken
}

function Get-Role 
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $name,
        [Parameter(Mandatory=$true)]
        [string] $grain,
        [Parameter(Mandatory=$true)]
        [string] $securableItem,
        [Parameter(Mandatory=$true)]
        [string] $authorizationServiceUrl,
        [Parameter(Mandatory=$true)]
        [string] $accessToken
    )
    $url = "$authorizationServiceUrl/roles/$grain/$securableItem/$name"
    $role = Invoke-Get -url $url -accessToken $accessToken
    return $role
}

function Add-Group
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $authUrl,
        [Parameter(Mandatory=$true)]
        [string] $name,
        [Parameter(Mandatory=$true)]
        [string] $source,
        [Parameter(Mandatory=$true)]
        [string] $accessToken
    )
    Write-DosMessage -Level "Information" -Message "Adding Group $($name)"
    $url = "$authUrl/groups"
    $body = @{
        groupName     = "$name"
        displayName   = "$name"
        groupSource = "$source"
    }
    return Invoke-Post $url $body $accessToken
}

function Add-User
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $authUrl,
        [Parameter(Mandatory=$true)]
        [string] $name,
        [Parameter(Mandatory=$true)]
        [string] $accessToken
    )
    Write-DosMessage -Level "Information" -Message "Adding User $($name)"
    $url = "$authUrl/user"
    $body = @{
        subjectId        = "$name"
        identityProvider = "Windows"
    }
    return Invoke-Post $url $body $accessToken
}

function Add-UserToGroup
{
    param(
        [Parameter(Mandatory=$true)]
        [object] $group,
        [Parameter(Mandatory=$true)]
        [object] $user,
        [Parameter(Mandatory=$true)]
        [string] $connString,
        [Parameter(Mandatory=$true)]
        [string] $clientId
    )
    $query = "INSERT INTO GroupUsers
             (CreatedBy, CreatedDateTimeUtc, GroupId, IdentityProvider, SubjectId, IsDeleted)
             VALUES(@clientId, GETUTCDATE(), @groupId, @identityProvider, @subjectId, 0)"

    $groupId = $group.Id
    $identityProvider = $user.identityProvider
    $subjectId = $user.subjectId
    Invoke-Sql $connString $query @{groupId = $groupId; identityProvider = $identityProvider; subjectId = $subjectId; clientId = $clientId} | Out-Null
}

function Add-ChildGroupToParentGroup
{
    param(
        [Parameter(Mandatory=$true)]
        [object] $parentGroup,
        [Parameter(Mandatory=$true)]
        [object] $childGroup,
        [Parameter(Mandatory=$true)]
        [string] $connString,
        [Parameter(Mandatory=$true)]
        [string] $clientId
    )
    $query = "INSERT INTO ChildGroups
              (ParentGroupId, ChildGroupId, CreatedBy, CreatedDateTimeUtc, IsDeleted)
              VALUES(@parentGroupId, @childGroupId, @clientId, GETUTCDATE(), 0)"

    $parentGroupId = $parentGroup.Id
    $childGroupId = $childGroup.Id
    Invoke-Sql $connString $query @{parentGroupId = $parentGroupId; childGroupId = $childGroupId; clientId = $clientId} | Out-Null
}

function Add-RoleToGroup
{
    param(
        [Parameter(Mandatory=$true)]
        [object] $role,
        [Parameter(Mandatory=$true)]
        [object] $group,
        [Parameter(Mandatory=$true)]
        [string] $connString,
        [Parameter(Mandatory=$true)]
        [string] $clientId
    )
    Write-DosMessage -Level "Information" -Message "Adding Role: $($role.Name) to Group: $($group.Name)"
    $query = "INSERT INTO GroupRoles
              (CreatedBy, CreatedDateTimeUtc, GroupId, IsDeleted, RoleId)
              VALUES(@clientId, GetUtcDate(), @groupId, 0, @roleId)"

    $roleId = $role.Id
    $groupId = $group.Id
    Invoke-Sql $connString $query @{groupId = $groupId; roleId = $roleId; ; clientId = $clientId} | Out-Null
}

function Get-PrincipalContext
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $domain
    )
    [System.Reflection.Assembly]::LoadWithPartialName("System.DirectoryServices.AccountManagement") | Out-Null
    $ct = [System.DirectoryServices.AccountManagement.ContextType]::Domain 
    $pc = New-Object System.DirectoryServices.AccountManagement.PrincipalContext -ArgumentList $ct, $domain
    return $pc
}

function Test-IsUser
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $samAccountName,
        [Parameter(Mandatory=$true)]
        [string] $domain
    )
    $isUser = $false
    $pc = Get-PrincipalContext -domain $domain
    $user = Get-UserPrincipal -pc $pc -samAccountName $samAccountName
    if ($null -ne $user) {
        $isUser = $true
    }
    return $isUser
}

function Test-IsGroup
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $samAccountName,
        [Parameter(Mandatory=$true)]
        [string] $domain
    )
    $isGroup = $false
    $pc = Get-PrincipalContext -domain $domain
    $group = Get-GroupPrincipal -pc $pc -samAccountName $samAccountName
    if ($null -ne $group) {
        $isGroup = $true
    }
    return $isGroup
}

function Get-UserPrincipal
{
    param(
        [Parameter(Mandatory=$true)]
        [object] $pc,
        [Parameter(Mandatory=$true)]
        [string] $samAccountName
    )
    return [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($pc, $samAccountName)
}

function Get-GroupPrincipal
{
    param(
        [Parameter(Mandatory=$true)]
        [object] $pc,
        [Parameter(Mandatory=$true)]
        [string] $samAccountName
    )
    return [System.DirectoryServices.AccountManagement.GroupPrincipal]::FindByIdentity($pc, $samAccountName)
}

function Get-SamAccountFromAccountName 
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $accountName
    )
    $accountNameParts = $accountName.Split('\')
    if ($accountNameParts.Count -ne 2) {
        Write-DosMessage -Level "Error" -Message "Please enter an account in the form DOMAIN\account. Halting installation." 
        throw
    }
    $samAccountName = $accountNameParts[1]
    return $samAccountName
}
 	
function Add-ListOfUsersToDosAdminGroup($edwAdminUsers, $connString, $authorizationServiceUrl, $accessToken) {	   
    # Get the group once, should be same for every user.
    $group = Get-Group -name $dosAdminGroupName -authorizationServiceUrl $authorizationServiceUrl -accessToken $accessToken

    # For each user, loop and try to add the user to the API.  
    # Do not validate it to AD like the Add-AccountToDosAdminGroup function.
    foreach ($edwAdmin in $edwAdminUsers) {	
        if ([string]::IsNullOrWhiteSpace($edwAdmin)) {
            continue
        }

        try {  
            $user = Add-User -authUrl $authorizationServiceUrl -name $edwAdmin -accessToken $accessToken
            Add-UserToGroup -group $group -user $user -connString $connString -clientId $fabricInstallerClientId
        }
        catch {
            # If there is an exception, the function will print the error. We will want to 
            # continue with the next user, so we will catch and swallow the exception.
            $exception = $_.Exception
            if ($null -ne $exception-and $null -ne $exception.Response -and $exception.Response.StatusCode.value__ -eq 409) {
                Write-DosMessage -Level "Information" -Message  "User: $accountName has already been registered as Dos Admin with Fabric.Authorization"
            }
            else {
                if ($null -ne $exception.Response) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                    Write-DosMessage -Level "Error" -Message "There was an error updating the resource: $error. "
                }
            }
        }        
    }	
}

function Add-DosAdminGroup
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $authUrl,
        [Parameter(Mandatory=$true)]
        [string] $accessToken,
        [Parameter(Mandatory=$true)]
        [string] $groupName
    )
    try {
        $group = Add-Group -authUrl $authUrl -name $groupName -source "custom" -accessToken $accessToken
        return $group
    }
    catch {
        $exception = $_.Exception
        if (Assert-WebExceptionType -exception $exception -typeCode 409) {
            $group = Get-Group -authorizationServiceUrl $authUrl -name $groupName -accessToken $accessToken
            Write-DosMessage -Level "Information" -Message "$groupName group already exists..."
            return $group;
        }
        else{
            if ($null -ne $exception.Response) {
                $error = Get-ErrorFromResponse -response $exception.Response
                Write-DosMessage -Level "Error" -Message "There was an error adding the Dos Admin group: $error. Halting installation."
            }
            throw $exception
        }
    }
}

function Add-DosAdminRoleUsersToDosAdminGroup
{
    param(
        [Parameter(Mandatory=$true)]
        [GUID] $groupId,
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [Parameter(Mandatory=$true)]
        [string] $roleName,
        [Parameter(Mandatory=$true)]
        [string] $securableName
    )
    $query = "INSERT INTO GroupUsers
              (CreatedBy, CreatedDateTimeUtc, GroupId, IdentityProvider, SubjectId, IsDeleted)
              SELECT @clientId, GETUTCDATE(), @dosAdminGroupId, u.IdentityProvider, ru.subjectid, 0 from RoleUsers ru
              INNER JOIN Roles r ON r.RoleId = ru.RoleId
              INNER JOIN Users u ON u.SubjectId = ru.SubjectId
              INNER JOIN SecurableItems s on r.SecurableItemId = s.SecurableItemId
              WHERE r.[Name] = @roleName
              AND s.[Name] = @securableName
              AND ru.IsDeleted = 0;"
    try{
        Write-DosMessage -Level "Information" -Message "Migrating $dosAdminRole role users to Dos Admin group..."
        Invoke-Sql -connectionString $connectionString -sql $query -parameters @{dosAdminGroupId=$groupId;clientId=$clientId;roleName=$roleName;securableName=$securableName} | Out-Null
    }catch{
        Write-DosMessage -Level "Error" -Message $_.Exception
        throw $_.Exception
    }
}

function Remove-UsersFromDosAdminRole
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [Parameter(Mandatory=$true)]
        [string] $roleName,
        [Parameter(Mandatory=$true)]
        [string] $securableName
    )
    $sql = "UPDATE ru 
            SET ru.IsDeleted = 1,
                ru.ModifiedBy = @clientId,
                ru.ModifiedDateTimeUtc = GETUTCDATE()
            FROM roleusers ru
            INNER JOIN roles r ON ru.RoleId = r.RoleId
            INNER JOIN SecurableItems s on r.SecurableItemId = s.SecurableItemId
            WHERE r.[Name] = @roleName
            AND s.[Name] = @securableName
            AND ru.IsDeleted = 0;"
    
    Write-DosMessage -Level "Information" -Message "Removing users from $dosAdminRole role..."
    try{
        Invoke-Sql $connectionString $sql @{clientId=$clientId;roleName=$roleName;securableName=$securableName} | Out-Null
    }catch{
        Write-DosMessage -Level "Error" -Message $_.Exception
        throw $_.Exception
    }
}

function Add-DosAdminGroupRolesToDosAdminChildGroups
{
    param(
        [Parameter(Mandatory=$true)]
        [GUID] $groupId,
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [Parameter(Mandatory=$true)]
        [string] $roleName,
        [Parameter(Mandatory=$true)]
        [string] $securableName
    )
    $query = "INSERT INTO ChildGroups
              (ParentGroupId, ChildGroupId, CreatedBy, CreatedDateTimeUtc, IsDeleted)
              SELECT @dosAdminGroupId, g.GroupId, @clientId, GETUTCDATE(), 0 
              FROM GroupRoles gr
              INNER JOIN Roles r ON r.RoleId = gr.RoleId
              INNER JOIN Groups g ON g.GroupId = gr.GroupId
              INNER JOIN SecurableItems s on r.SecurableItemId = s.SecurableItemId
              WHERE r.[Name] = @roleName
              AND s.[Name] = @securableName
              AND g.Source != 'custom'
              AND r.IsDeleted = 0;"

    try{
        Write-DosMessage -Level "Information" -Message "Migrating $dosAdminRole role groups to Dos Admin group..."
        Invoke-Sql $connectionString $query @{dosAdminGroupId = $groupId;clientId=$clientId;roleName=$roleName;securableName=$securableName} | Out-Null
    }catch{
        Write-DosMessage -Level "Error" -Message $_.Exception
        throw
    }
}

function Remove-GroupsFromDosAdminRole
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [Parameter(Mandatory=$true)]
        [string] $roleName,
        [Parameter(Mandatory=$true)]
        [string] $securableName
    )
    $sql = "UPDATE gr
            SET gr.IsDeleted = 1, 
                gr.ModifiedBy = @clientId, 
                gr.ModifiedDateTimeUtc = GETUTCDATE()
            FROM GroupRoles gr
            INNER JOIN Roles r ON gr.RoleId = r.RoleId
            INNER JOIN Groups g on g.GroupId = gr.GroupId
            INNER JOIN SecurableItems s on r.SecurableItemId = s.SecurableItemId
            WHERE r.[Name] = @roleName
            AND s.[Name] = @securableName
            AND g.Source != 'custom'
            AND gr.IsDeleted = 0;"

    Write-DosMessage -Level "Information" -Message "Removing groups from $dosAdminRole role..."
    try{
        Invoke-Sql $connectionString $sql @{clientId=$clientId;roleName=$roleName;securableName=$securableName} | Out-Null
    }catch{
        Write-DosMessage -Level "Error" -Message $_.Exception
        throw $_.Exception
    }
}

function Add-DosAdminRoleToDosAdminGroup
{
    param(
        [Parameter(Mandatory=$true)]
        [GUID] $groupId,
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [Parameter(Mandatory=$true)]
        [string] $roleName,
        [Parameter(Mandatory=$true)]
        [string] $securableName
    )
    $query = "INSERT INTO GroupRoles
              (CreatedBy, CreatedDateTimeUtc, GroupId, RoleId, IsDeleted)
              SELECT @clientId, GETUTCDATE(), @dosAdminGroupId, r.RoleId, 0
              FROM Roles r
              INNER JOIN SecurableItems s on r.SecurableItemId = s.SecurableItemId
              WHERE r.[Name] = @roleName
              AND s.[Name] = @securableName
              AND r.IsDeleted = 0;"
    try{
        Write-DosMessage -Level "Information" -Message "Associating $dosAdminRole role to Dos Admin group..."
        Invoke-Sql $connectionString $query @{dosAdminGroupId = $groupId;clientId=$clientId;roleName=$roleName;securableName=$securableName} | Out-Null
    }catch{
        Write-DosMessage -Level "Error" -Message $_.Exception
        throw
    }
}

function Update-DosAdminRoleToDataMartAdmin
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [Parameter(Mandatory=$true)]
        [string] $oldRoleName,
        [Parameter(Mandatory=$true)]
        [string] $newRoleName,
        [Parameter(Mandatory=$true)]
        [string] $securableName
    )
    $sql = "UPDATE r
            SET r.[Name]  = @newRoleName, 
                r.DisplayName = 'DataMart Admin', 
                r.ModifiedBy = @clientId, 
                r.ModifiedDateTimeUtc = GETUTCDATE()
            FROM Roles r
            INNER JOIN SecurableItems s on r.SecurableItemId = s.SecurableItemId
            WHERE r.[Name] = @oldRoleName
            AND s.[Name] = @securableName
            AND r.IsDeleted = 0;"

    Write-DosMessage -Level "Information" -Message "Renaming $dosAdminRole role to $dataMartAdminRole..."
    try{
        Invoke-Sql $connectionString $sql @{clientId=$clientId;oldRoleName=$oldRoleName;newRoleName=$newRoleName;securableName=$securableName} | Out-Null
    }catch{
        Write-DosMessage -Level "Error" -Message $_.Exception
        throw
    }
}

function Remove-DosAdminRole
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [Parameter(Mandatory=$true)]
        [string] $roleName,
        [Parameter(Mandatory=$true)]
        [string] $securableName
    )
    $sql = "UPDATE r
            SET r.IsDeleted = 1,
                r.ModifiedBy = @clientId,
                r.ModifiedDateTimeUtc = GETUTCDATE()
            FROM Roles r
            INNER JOIN SecurableItems s on r.SecurableItemId = s.SecurableItemId
            WHERE r.[Name] = @roleName
            AND s.[Name] = @securableName
            AND r.IsDeleted = 0;"

    Write-DosMessage -Level "Information" -Message "Deleting $dosAdminRole role..."
    try{
        Invoke-Sql $connectionString $sql @{clientId=$clientId;roleName=$roleName;securableName=$securableName} | Out-Null
    }catch{
        Write-DosMessage -Level "Error" -Message $_.Exception
        throw
    }
}

function Test-FabricRegistrationStepAlreadyComplete()
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $authUrl,
        [Parameter(Mandatory=$true)]
        [string] $accessToken
    )
    try{
        $dataMartAdminRole = Get-Role -name $dataMartAdminRole -grain $dosGrain -securableItem $dataMartsSecurable -authorizationServiceUrl $authUrl -accessToken $accessToken
        $dosAdminRole = Get-Role -name $dosAdminRole -grain $dosGrain -securableItem $dataMartsSecurable -authorizationServiceUrl $authUrl -accessToken $accessToken
        if($null -ne $dataMartAdminRole -and $null -ne $dosAdminRole){
            return $true
        }
        return $false
    }
    catch {
        $exception = $_.Exception
        if ($null -ne $exception.Response) {
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-DosMessage -Level "Error" -Message "There was an error getting the resource: $error. "
        }
        throw $exception
    }      
}

function Get-EdwAdminUsersAndGroups
{	
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString
    )
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)	
    $sql = "SELECT i.IdentityID, i.IdentityNM, r.RoleNM	
            FROM [CatalystAdmin].[RoleBASE] r	
            INNER JOIN [CatalystAdmin].[IdentityRoleBASE] ir	
                ON r.RoleID = ir.RoleID	
            INNER JOIN [CatalystAdmin].[IdentityBASE] i	
                ON ir.IdentityID = i.IdentityID	
            WHERE RoleNM = 'EDW Admin'";	
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)	
    
    $usersAndGroups = @();	
    try {	
        $connection.Open()    	
        $reader = $command.ExecuteReader()	
        while ($reader.Read()) {	
            $usersAndGroups += $reader['IdentityNM']	
        }	
        $connection.Close()        	
    
    }	
    catch [System.Data.SqlClient.SqlException] {	
        Write-DosMessage -Level "Error" -Message "An error ocurred while executing the command. Please ensure the connection string is correct and the metadata database has been setup. Connection String: $($connectionString). Error $($_.Exception.Message)"  -ErrorAction Stop	
    }    	
    
    return $usersAndGroups;	
}

function Get-CurrentUserDomain
{
    param(
        [Parameter(Mandatory=$true)]
        [bool] $quiet
    )
    $currentUserDomain = $env:userdnsdomain
    if(!$quiet){
        $userEnteredDomain = Read-Host "Press Enter to accept the default domain '$($currentUserDomain)' that the user/group who will administrate dos is a member or enter a new domain" 
        if (![string]::IsNullOrEmpty($userEnteredDomain)) {
            $currentUserDomain = $userEnteredDomain
        }
    }
    return $currentUserDomain
}

function Get-DefaultIdentityServiceUrl
{
    param(
        [string] $identityServiceUrl
    )
    if([string]::IsNullOrEmpty($identityServiceUrl)){
        return "$(Get-FullyQualifiedMachineName)/identity"
    }else{
        return $identityServiceUrl
    }
}

function Assert-WebExceptionType
{
    param(
        [object] $exception,
        [int] $typeCode
    )
    if ($null -ne $exception -and $exception.Response.StatusCode.value__ -eq $typeCode) {
        return $true
    }
    else {
        return $false
    }
}

function Install-UrlRewriteIfNeeded
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $version,
        [Parameter(Mandatory=$true)]
        [string] $downloadUrl
    )
    if(!(Test-Prerequisite "*IIS URL Rewrite Module 2" $version))
    {    
        try{
            Write-DosMessage -Level "Information" -Message "IIS URL Rewrite Module 2 version $version not installed...installing version $version"        
            Invoke-WebRequest -Uri $downloadUrl -OutFile $env:Temp\rewrite_amd64_en-US.msi
            Start-Process msiexec.exe -Wait -ArgumentList "/i $($env:Temp)\rewrite_amd64_en-US.msi /qn"
            Write-DosMessage -Level "Information" -Message "IIS URL Rewrite Module 2 installed successfully."
        }catch{
            Write-DosMessage -Level "Error" -Message "Could not install IIS URL Rewrite Module 2. Please install the IIS URL Rewrite Module 2 before proceeding: $downloadUrl"
            throw
        }
        try{
            Remove-Item $env:Temp\rewrite_amd64_en-US.msi
        }catch{
            $e = $_.Exception
            Write-DosMessage -Level "Warning" -Message "Unable to remove IIS Rewrite msi installer." 
            Write-DosMessage -Level "Warning" -Message  $e.Message
        }

    }else{
        Write-DosMessage -Level "Information" -Message  "IIS URL Rewrite Module 2 version (v$version) installed and meets expectations."
    }
}

function Get-AuthorizationDatabaseConnectionString
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $authorizationDbName,
        [Parameter(Mandatory=$true)]
        [string] $sqlServerAddress,
        [Parameter(Mandatory=$true)]
        [string] $installConfigPath,
        [Parameter(Mandatory=$true)]
        [bool] $quiet
    )
    if(!$quiet){
        $userEnteredAuthorizationDbName = Read-Host "Press Enter to accept the default Authorization DB Name '$($authorizationDbName)' or enter a new Authorization DB Name"
        if(![string]::IsNullOrEmpty($userEnteredAuthorizationDbName)){
            $authorizationDbName = $userEnteredAuthorizationDbName
        }
    }
    $authorizationDbConnStr = "Server=$($sqlServerAddress);Database=$($authorizationDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

    Invoke-Sql $authorizationDbConnStr "SELECT TOP 1 ClientId FROM Clients" | Out-Null
    Write-DosMessage -Level "Information" -Message "Authorization DB Connection string: $authorizationDbConnStr verified"
    if($authorizationDbName){ Add-InstallationSetting "authorization" "authorizationDbName" "$authorizationDbName" $installConfigPath | Out-Null }
    return @{DbName = $authorizationDbName; DbConnectionString = $authorizationDbConnStr}
}

function Get-IdentityServiceUrl
{
    param(
        [string]$identityServiceUrl,
        [Parameter(Mandatory=$true)]
        [string] $installConfigPath,
        [Parameter(Mandatory=$true)]
        [bool]$quiet
    )
    $defaultIdentityUrl = Get-DefaultIdentityServiceUrl -identityServiceUrl $identityServiceUrl
    if(!$quiet){
        $userEnteredIdentityServiceUrl = Read-Host "Press Enter to accept the default DiscoveryService URL [$defaultIdentityUrl] or enter a new URL"
        if(![string]::IsNullOrEmpty($userEnteredIdentityServiceUrl)){   
            $defaultIdentityUrl = $userEnteredIdentityServiceUrl
        }
    }
    if($defaultIdentityUrl){ Add-InstallationSetting "common" "identityService" "$defaultIdentityUrl" $installConfigPath | Out-Null }
    return $defaultIdentityUrl
}

function Register-AuthorizationWithDiscovery{
    param(
        [Parameter(Mandatory=$true)]
        [string] $iisUserName,
        [Parameter(Mandatory=$true)]
        [string] $metadataConnStr,
        [Parameter(Mandatory=$true)]
        [string] $version,
        [Parameter(Mandatory=$true)]
        [string] $authorizationServiceUrl
    )
    Add-ServiceUserToDiscovery $iisUserName $metadataConnStr
    $serviceUrl = "$authorizationServiceUrl/v1"
    $discoveryPostBody = @{
        buildVersion = $version;
        serviceName = "AuthorizationService";
        serviceVersion = 1;
        friendlyName = "Fabric.Authorization";
        description = "The Fabric.Authorization service provides centralized authentication across the Fabric ecosystem.";
        serviceUrl = "$serviceUrl";
        discoveryType = "Service";
    }
    Add-DiscoveryRegistrationSql -discoveryPostBody $discoveryPostBody -connectionString $metadataConnStr | Out-Null
    Write-DosMessage -Level "Information" -Message "Authorization registered URL: $serviceUrl with DiscoveryService."
}

function Register-AccessControlWithDiscovery([string] $iisUserName, [string] $metadataConnStr, [string] $version, [string] $authorizationServiceUrl){
    Add-ServiceUserToDiscovery $iisUserName $metadataConnStr

    $discoveryPostBody = @{
        buildVersion = $version;
        serviceName = "AccessControl";
        serviceVersion = 1;
        friendlyName = "Access Control";
        description = "Fabric Access Control provides a UI to manage permissions across DOS.";
        serviceUrl = $authorizationServiceUrl;
        discoveryType = "Application";
        isHidden = $false;
        iconTxt = "data:image/png;base64,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";
    }
    Add-DiscoveryRegistrationSql -discoveryPostBody $discoveryPostBody -connectionString $metadataConnStr | Out-Null
    Write-DosMessage -Level "Information" -Message "Access Control registered URL: $authorizationServiceUrl with DiscoveryService."
}

function Add-AuthorizationApiRegistration([string] $identityServiceUrl, [string] $accessToken)
{
    $body = @{
        Name = "authorization-api";
        UserClaims = @("name","email","role","groups");
        Scopes = @(@{Name = "fabric/authorization.read"}, @{Name = "fabric/authorization.write"}, @{Name = "fabric/authorization.internal"}, @{Name = "fabric/authorization.dos.write"}, @{Name = "fabric/authorization.manageclients"});
    }
    $jsonBody = ConvertTo-Json $body

    Write-DosMessage -Level "Information" -Message "Registering Fabric.Authorization API."
    $authorizationApiSecret = ([string](Add-ApiRegistration -authUrl $identityServiceUrl -body $jsonBody -accessToken $accessToken)).Trim()
    return $authorizationApiSecret
}

function Add-AuthorizationClientRegistration([string] $identityServiceUrl, [string] $accessToken)
{
    $body = @{
        ClientId = "fabric-authorization-client";
        ClientName = "Fabric Authorization Client";
        RequireConsent = $false; 
        AllowedGrantTypes = @("client_credentials"); 
        AllowedScopes = @("fabric/identity.read", "fabric/identity.searchusers");
    }
    $jsonBody = ConvertTo-Json $body

    Write-DosMessage -Level "Information" -Message "Registering Fabric.Authorization Client."
    $authorizationClientSecret = ([string](Add-ClientRegistration -authUrl $identityServiceUrl -body $jsonBody -accessToken $accessToken)).Trim()
    return $authorizationClientSecret
}

function Add-AccessControlClientRegistration([string] $identityServiceUrl, [string] $authorizationServiceUrl, [string] $accessToken)
{
    $body = @{
        clientId = "fabric-access-control";
		clientName = "Fabric Authorization Access Control Client";
		requireConsent = "false";
		allowedScopes = "openid", "profile", "fabric.profile", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.internal", "fabric/idprovider.searchusers", "fabric/authorization.dos.write";
		allowOfflineAccess = $false;
		allowAccessTokensViaBrowser = $true;
		enableLocalLogin = $false;
		accessTokenLifetime = 1200;
    }
    
    $corsOrigin = Get-FullyQualifiedMachineName

    $body.allowedGrantTypes = @("implicit")
    $body.redirectUris = "$authorizationServiceUrl/client/oidc-callback.html", "$authorizationServiceUrl/client/silent.html"
    $body.allowedCorsOrigins = @("$corsOrigin")
    $body.postLogoutRedirectUris = @("$authorizationServiceUrl/client/logged-out")

    $jsonBody = ConvertTo-Json $body

    Write-DosMessage -Level "Information" -Message "Registering Fabric.AccessControl Client."
    Add-ClientRegistration -authUrl $identityServiceUrl -body $jsonBody -accessToken $accessToken
}

function Get-AdminAccount([string] $adminAccount, [string] $currentUserDomain, [string] $installConfigPath, [bool] $quiet){
    if(!$quiet){
        if ([string]::IsNullOrEmpty($adminAccount)) {
            $userEnteredAdminAccount = Read-Host "Please enter the user/group account for dos administration in the format [DOMAIN\user]"
        }
        else {
            $userEnteredAdminAccount = Read-Host "Press Enter to accept the default admin account '$($adminAccount)' for the user/group who will administrate dos or enter a new account"
        }
            
        if (![string]::IsNullOrEmpty($userEnteredAdminAccount)) {
            $adminAccount = $userEnteredAdminAccount
        }
    }
    
    $samAccountName = Get-SamAccountFromAccountName -accountName $adminAccount
    $adminAccountIsUser = $false
    if (Test-IsUser -samAccountName $samAccountName -domain $currentUserDomain) {
        $adminAccountIsUser = $true
    }
    elseif (Test-IsGroup -samAccountName $samAccountName -domain $currentUserDomain) {
        $adminAccountIsUser = $false
    }
    else {
        Write-DosMessage -Level "Error" -Message "$samAccountName is not a valid principal in the $currentUserDomain domain. Please enter a valid account. Halting installation."
        throw
    }
    if ($adminAccount) {Add-InstallationSetting "authorization" "adminAccount" "$adminAccount" $installConfigPath | Out-Null}
    return @{AdminAccountName = $adminAccount; AdminAccountIsUser = $adminAccountIsUser}
}

function Set-AuthorizationEnvironmentVariables
{
    param(
        [string] $appDirectory,
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $encryptionCert,
        [string] $clientName,
        [string] $encryptionCertificateThumbprint,
        [string] $appInsightsInstrumentationKey,
        [string] $authorizationClientSecret,
        [string] $identityServiceUrl,
        [string] $authorizationDbConnStr,
        [string] $metadataConnStr,
        [Hashtable] $adminAccount,
        [string] $authorizationApiSecret,
        [string] $authorizationServiceUrl,
        [string] $discoveryServiceUrl
    )

    $environmentVariables = @{"StorageProvider" = "sqlserver"}

    if ($clientName) {
        $environmentVariables.Add("ClientName", $clientName)
    }

    if ($encryptionCertificateThumbprint) {
        $environmentVariables.Add("EncryptionCertificateSettings__EncryptionCertificateThumbprint", $encryptionCertificateThumbprint)
    }

    if ($appInsightsInstrumentationKey) {
        $environmentVariables.Add("ApplicationInsights__Enabled", "true")
        $environmentVariables.Add("ApplicationInsights__InstrumentationKey", $appInsightsInstrumentationKey)
    }

    if ($authorizationClientSecret) {
        $encryptedSecret = Get-EncryptedString $encryptionCert $authorizationClientSecret
        $environmentVariables.Add("IdentityServerConfidentialClientSettings__ClientSecret", $encryptedSecret)
    }

    $environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", $identityServiceUrl)

    if ($authorizationDbConnStr) {
        $environmentVariables.Add("ConnectionStrings__AuthorizationDatabase", $authorizationDbConnStr)
    }

    if($metadataConnStr){
        $environmentVariables.Add("ConnectionStrings__EDWAdminDatabase", $metadataConnStr)
    }

    if ($adminAccount.AdminAccountName) {
        $environmentVariables.Add("AdminAccount__Name", $adminAccount.AdminAccountName)
    }

    if ($adminAccount.AdminAccountIsUser) {
        $environmentVariables.Add("AdminAccount__Type", "user")
    }
    else {
        $environmentVariables.Add("AdminAccount__Type", "group")}

    if ($authorizationApiSecret) {
        $encryptedSecret = Get-EncryptedString $encryptionCert $authorizationApiSecret
        $environmentVariables.Add("IdentityServerApiSettings__ApiSecret", $encryptedSecret)
    }

    if ($authorizationServiceUrl) {
        $environmentVariables.Add("ApplicationEndpoint", $authorizationServiceUrl)
    }

    if ($discoveryServiceUrl) {
        $environmentVariables.Add("AccessControlSettings__DiscoveryServiceSettings__Value", $discoveryServiceUrl)
    }

    Set-EnvironmentVariables $appDirectory $environmentVariables | Out-Null
}

function Move-DosAdminRoleToDosAdminGroup($authUrl, $accessToken, $connectionString, $groupName)
{
    $group = Add-DosAdminGroup -authUrl $authUrl -accessToken $accessToken -groupName $groupName
    $groupId = $group.Id
    Add-DosAdminRoleUsersToDosAdminGroup -groupId $groupId -connectionString $connectionString -clientId $fabricInstallerClientId -roleName $dosAdminRole -securableName $dataMartsSecurable
    Remove-UsersFromDosAdminRole -connectionString $connectionString -clientId $fabricInstallerClientId -roleName $dosAdminRole -securableName $dataMartsSecurable
    Add-DosAdminGroupRolesToDosAdminChildGroups -groupId $groupId -connectionString $connectionString -clientId $fabricInstallerClientId -roleName $dosAdminRole -securableName $dataMartsSecurable
    Remove-GroupsFromDosAdminRole -connectionString $connectionString -clientId $fabricInstallerClientId -roleName $dosAdminRole -securableName $dataMartsSecurable
    if((Test-FabricRegistrationStepAlreadyComplete -authUrl $authUrl -accessToken $accessToken)){
        Remove-DosAdminRole -connectionString $connectionString -clientId $fabricInstallerClientId -roleName $dosAdminRole -securableName $dataMartsSecurable
        $dataMartAdminRoleModel = Get-Role -name $dataMartAdminRole -grain $dosGrain -securableItem $dataMartsSecurable -authorizationServiceUrl $authUrl -accessToken $accessToken
        Add-RoleToGroup -role $dataMartAdminRoleModel -group $group -connString $connectionString -clientId $fabricInstallerClientId
    }
    else{
        Add-DosAdminRoleToDosAdminGroup -groupId $groupId -connectionString $connectionString -clientId $fabricInstallerClientId -roleName $dosAdminRole -securableName $dataMartsSecurable
        Update-DosAdminRoleToDataMartAdmin -connectionString $connectionString -clientId $fabricInstallerClientId -oldRoleName $dosAdminRole -newRoleName $dataMartAdminRole -securableName $dataMartsSecurable
    }
}

function Add-AccountToDosAdminGroup($accountName, $domain, $authorizationServiceUrl, $accessToken, $connString) {
    $samAccountName = Get-SamAccountFromAccountName -accountName $accountName
    $group = Get-Group -name $dosAdminGroupName -authorizationServiceUrl $authorizationServiceUrl -accessToken $accessToken
    if (Test-IsUser -samAccountName $samAccountName -domain $domain) {
        try {
            $user = Add-User -authUrl $authorizationServiceUrl -name $accountName -accessToken $accessToken
            Add-UserToGroup -group $group -user $user -connString $connString -clientId $fabricInstallerClientId
        }
        catch {
            $exception = $_.Exception
            if ($null -ne $exception-and $null -ne $exception.Response -and $exception.Response.StatusCode.value__ -eq 409) {
                Write-DosMessage -Level "Information" -Message  "User: $accountName has already been registered as $dosAdminRole with Fabric.Authorization"
            }
            else {
                if ($null -ne $exception.Response) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                    Write-DosMessage -Level "Error" -Message "There was an error updating the resource: $error. Halting installation."
                }
                throw $exception
            }
        }
    }
    elseif (Test-IsGroup -samAccountName $samAccountName -domain $domain) {
        try {
            $childGroup = Add-Group -authUrl $authorizationServiceUrl -name $accountName -source "Windows" -accessToken $accessToken
            Add-ChildGroupToParentGroup -parentGroup $group -childGroup $childGroup -connString $connString -clientId $fabricInstallerClientId
        }
        catch {
            $exception = $_.Exception
            if ($null -ne $exception-and $null -ne $exception.Response -and $exception.Response.StatusCode.value__ -eq 409) {
                Write-DosMessage -Level "Information" -Message  "Group: $accountName has already been registered as $dosAdminRole with Fabric.Authorization"
            }
            else {
                if ($null -ne $exception.Response) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                    Write-DosMessage -Level "Error" -Message "There was an error updating the resource: $error. Halting installation."
                }
                throw $exception
            }
        }
    }
    else {
        Write-DosMessage -Level "Error" -Message "$samAccountName is not a valid principal in the $domain domain. Please enter a valid account. Halting installation."
        throw
    }
}

function Invoke-MonitorShallow($authorizationUrl) {
    $url = "$authorizationUrl/_monitor/shallow"
    Invoke-RestMethod -Method Get -Uri $url
}

function Add-EdwAdminUsersToDosAdminGroup([string] $metadataConnStr, [string] $authorizationDbConnStr, [string] $authorizationServiceUrl, [string] $accessToken)
{
    $edwAdminUsers = Get-EdwAdminUsersAndGroups -connectionString $metadataConnStr	
    Write-DosMessage -Level "Information" -Message "There are $($edwAdminUsers.Count) users with this role"	
    Add-ListOfUsersToDosAdminGroup -edwAdminUsers $edwAdminUsers -connString $authorizationDbConnStr -authorizationServiceUrl "$authorizationServiceUrl/v1" -accessToken $accessToken
}

function Add-AuthorizationRegistration($clientId, $clientName, $authorizationServiceUrl, $accessToken){
    $url = "$authorizationServiceUrl/clients"
    $body = @{
        id = "$clientId"
        name = $clientName
    }
    try{
        Invoke-Post -url $url -body $body -accessToken $accessToken
    }
    catch {
        $exception = $_.Exception
        if ($null -ne $exception -and $null -ne $exception.Response -and $exception.Response.StatusCode.value__ -eq 409) {
            Write-DosMessage -Level "Information" -Message  "Client: $clientId has already been registered with Fabric.Authorization"
        }
        else {
            if ($null -ne $exception.Response) {
                $error = Get-ErrorFromResponse -response $exception.Response
                Write-DosMessage -Level "Error" -Message "There was an error registering $clientId with Fabric.Authorization: $error. Halting installation."
            }
            throw $exception
        }
    }
}

Export-ModuleMember Install-UrlRewriteIfNeeded
Export-ModuleMember Get-AuthorizationDatabaseConnectionString
Export-ModuleMember Get-IdentityServiceUrl
Export-ModuleMember Register-AuthorizationWithDiscovery
Export-ModuleMember Register-AccessControlWithDiscovery
Export-ModuleMember Add-AuthorizationApiRegistration
Export-ModuleMember Add-AuthorizationClientRegistration
Export-ModuleMember Add-AccessControlClientRegistration
Export-ModuleMember Get-CurrentUserDomain
Export-ModuleMember Get-AdminAccount
Export-ModuleMember Set-AuthorizationEnvironmentVariables
Export-ModuleMember Move-DosAdminRoleToDosAdminGroup
Export-ModuleMember Add-AccountToDosAdminGroup
Export-ModuleMember Invoke-MonitorShallow
Export-ModuleMember Add-EdwAdminUsersToDosAdminGroup
Export-ModuleMember Add-AuthorizationRegistration