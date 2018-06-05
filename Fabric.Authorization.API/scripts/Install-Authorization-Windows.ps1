#
# Install_Authorization_Windows.ps1
#
param([switch]$noDiscoveryService)
function Get-DiscoveryServiceUrl() {
    return "https://$env:computername.$($env:userdnsdomain.tolower())/DiscoveryService"
}

function Get-IdentityServiceUrl() {
    return "https://$env:computername.$($env:userdnsdomain.tolower())/Identity"
}

function Get-Headers($accessToken) {
    $headers = @{"Accept" = "application/json"}
    if ($accessToken) {
        $headers.Add("Authorization", "Bearer $accessToken")
    }
    return $headers
}

function Add-DiscoveryRegistration($discoveryUrl, $serviceUrl, $credential) {	
    $registrationBody = @{
        ServiceName   = "AuthorizationService"
        Version       = 1
        ServiceUrl    = $serviceUrl
        DiscoveryType = "Service"
        IsHidden      = $true
        FriendlyName  = "Fabric.Authorization"
        Description   = "The Fabric.Authorization service provides centralized authorization across the Fabric ecosystem."
    }

    $url = "$discoveryUrl/v1/Services"
    $jsonBody = $registrationBody | ConvertTo-Json	
    try {
        Invoke-RestMethod -Method Post -Uri "$url" -Body "$jsonBody" -ContentType "application/json" -Credential $credential | Out-Null
        Write-Success "Fabric.Authorization successfully registered with DiscoveryService."
    }
    catch {
        $exception = $_.Exception
        Write-Error "Unable to register Fabric.Authorization with DiscoveryService. Ensure that DiscoveryService is running at $discoveryUrl, that Windows Authentication is enabled for DiscoveryService and Anonymous Authentication is disabled for DiscoveryService. Error $($_.Exception.Message) Halting installation."
        if ($exception.Response -ne $null) {
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-Error "    There was an error updating the resource: $error."
        }
        throw
    }
}

function Add-DatabaseLogin($userName, $connString) {
    $query = "USE master
            If Not exists (SELECT * FROM sys.server_principals
                WHERE sid = suser_sid(@userName))
            BEGIN
                print '-- creating database login'
                DECLARE @sql nvarchar(4000)
                set @sql = 'CREATE LOGIN ' + QUOTENAME('$userName') + ' FROM WINDOWS'
                EXEC sp_executesql @sql
            END"
    Invoke-Sql $connString $query @{userName = $userName} | Out-Null
}

function Add-DatabaseUser($userName, $connString) {
    $query = "IF( NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = @userName))
            BEGIN
                print '-- Creating user';
                DECLARE @sql nvarchar(4000)
                set @sql = 'CREATE USER ' + QUOTENAME('$userName') + ' FOR LOGIN ' + QUOTENAME('$userName')
                EXEC sp_executesql @sql
            END"
    Invoke-Sql $connString $query @{userName = $userName} | Out-Null
}

function Add-DatabaseUserToRole($userName, $connString, $role) {
    $query = "DECLARE @exists int
            SELECT @exists = IS_ROLEMEMBER(@role, @userName) 
            IF (@exists IS NULL OR @exists = 0)
            BEGIN
                print '-- Adding @role to @userName';
                EXEC sp_addrolemember @role, @userName;
            END"
    Invoke-Sql $connString $query @{userName = $userName; role = $role} | Out-Null
}

function Add-DatabaseSecurity($userName, $role, $connString) {
    Add-DatabaseLogin $userName $connString
    Add-DatabaseUser $userName $connString
    Add-DatabaseUserToRole $userName $connString $role
    Write-Success "Database security applied successfully"
}

function Invoke-Get($url, $accessToken) {
    $headers = Get-Headers -accessToken $accessToken
        
    $getResponse = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    return $getResponse
}

function Invoke-Post($url, $body, $accessToken) {
    $headers = Get-Headers -accessToken $accessToken

    if (!($body -is [String])) {
        $body = (ConvertTo-Json $body)
    }
    
    $postResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
    Write-Success "    Success."
    Write-Host ""
    return $postResponse
}

function Get-Role($name, $grain, $securableItem, $authorizationServiceUrl, $accessToken) {
    $url = "$authorizationServiceUrl/roles/$grain/$securableItem/$name"
    $role = Invoke-Get -url $url -accessToken $accessToken
    return $role
}

function Add-Group($authUrl, $name, $source, $accessToken) {
    Write-Host "Adding Group $($name)"
    $url = "$authUrl/groups"
    $body = @{
        id          = "$name"
        groupName   = "$name"
        groupSource = "$source"
    }
    return Invoke-Post $url $body $accessToken
}

function Add-User($authUrl, $name, $accessToken) {
    Write-Host "Adding User $($name)"
    $url = "$authUrl/user"
    $body = @{
        subjectId        = "$name"
        identityProvider = "Windows"
    }
    return Invoke-Post $url $body $accessToken
}

function Add-RoleToUser($role, $user, $connString) {
    $query = "INSERT INTO RoleUsers
              (CreatedBy, CreatedDateTimeUtc, RoleId, IdentityProvider, IsDeleted, SubjectId)
              VALUES('fabric-installer', GetUtcDate(), @roleId, @identityProvider, 0, @subjectId)"

    $roleId = $role.Id
    $identityProvider = $user.identityProvider
    $subjectId = $user.subjectId
    Invoke-Sql $connString $query @{roleId = $roleId; identityProvider = $identityProvider; subjectId = $subjectId} | Out-Null
}

function Add-RoleToGroup($role, $group, $connString) {
    $query = "INSERT INTO GroupRoles
              (CreatedBy, CreatedDateTimeUtc, GroupId, IsDeleted, RoleId)
              VALUES('fabric-installer', GetUtcDate(), @groupId, 0, @roleId)"

    $roleId = $role.Id
    $groupId = $group.Id
    Invoke-Sql $connString $query @{groupId = $groupId; roleId = $roleId; } | Out-Null
}

function Get-PrincipalContext($domain) {
    [System.Reflection.Assembly]::LoadWithPartialName("System.DirectoryServices.AccountManagement") | Out-Null
    $ct = [System.DirectoryServices.AccountManagement.ContextType]::Domain 
    $pc = New-Object System.DirectoryServices.AccountManagement.PrincipalContext -ArgumentList $ct, $domain
    return $pc
}

function Test-IsUser($samAccountName, $domain) {
    $isUser = $false
    $pc = Get-PrincipalContext -domain $domain
    $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($pc, $samAccountName)
    if ($user -ne $null) {
        $isUser = $true
    }
    return $isUser
}

function Test-IsGroup($samAccountName, $domain) {
    $isGroup = $false
    $pc = Get-PrincipalContext -domain $domain
    $group = [System.DirectoryServices.AccountManagement.GroupPrincipal]::FindByIdentity($pc, $samAccountName)
    if ($group -ne $null) {
        $isGroup = $true
    }
    return $isGroup
}

function Get-SamAccountFromAccountName($accountName) {
    $accountNameParts = $accountName.Split('\')
    if ($accountNameParts.Count -ne 2) {
        Write-Error "Please enter an account in the form DOMAIN\account. Halting installation." 
        throw
    }
    $samAccountName = $accountNameParts[1]
    return $samAccountName
}

function Add-AccountToDosAdminRole($accountName, $domain, $authorizationServiceUrl, $accessToken, $connString) {
    $samAccountName = Get-SamAccountFromAccountName -accountName $accountName
    $role = Get-Role -name "dosadmin" -grain "dos" -securableItem "datamarts" -authorizationServiceUrl $authorizationServiceUrl -accessToken $accessToken
    if (Test-IsUser -samAccountName $samAccountName -domain $domain) {
        try {
            $user = Add-User -authUrl $authorizationServiceUrl -name $accountName -accessToken $accessToken
            Add-RoleToUser -role $role -user $user -connString $connString
        }
        catch {
            $exception = $_.Exception
            if ($exception -ne $null -and $exception.Response -ne $null -and $exception.Response.StatusCode.value__ -eq 409) {
                Write-Success "    User: $accountName has already been registered as dosadmin with Fabric.Authorization"
                Write-Host ""
            }
            else {
                if ($exception.Response -ne $null) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                    Write-Error "    There was an error updating the resource: $error. Halting installation."
                }
                throw $exception
            }
        }
    }
    elseif (Test-IsGroup -samAccountName $samAccountName -domain $domain) {
        try {
            $group = Add-Group -authUrl $authorizationServiceUrl -name $accountName -source "Windows" -accessToken $accessToken
            Add-RoleToGroup -role $role -group $group -connString $connString
        }
        catch {
            $exception = $_.Exception
            if ($exception -ne $null -and $exception.Response -ne $null -and $exception.Response.StatusCode.value__ -eq 409) {
                Write-Success "    Group: $accountName has already been registered as dosadmin with Fabric.Authorization"
                Write-Host ""
            }
            else {
                if ($exception.Response -ne $null) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                    Write-Error "    There was an error updating the resource: $error. Halting installation."
                }
                throw $exception
            }
        }
    }
    else {
        Write-Error "$samAccountName is not a valid principal in the $domain domain. Please enter a valid account. Halting installation."
        throw
    }
}

function Invoke-MonitorShallow($authorizationUrl) {
    $url = "$authorizationUrl/_monitor/shallow"
    Invoke-RestMethod -Method Get -Uri $url
}

if (!(Test-Path .\Fabric-Install-Utilities.psm1)) {
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Force

if (!(Test-IsRunAsAdministrator)) {
    Write-Error "You must run this script as an administrator. Halting configuration."
    throw
}

$installSettings = Get-InstallationSettings "authorization"
$zipPackage = $installSettings.zipPackage
$webroot = $installSettings.webroot 
$appName = $installSettings.appName
$iisUser = $installSettings.iisUser
$encryptionCertificateThumbprint = $installSettings.encryptionCertificateThumbprint -replace '[^a-zA-Z0-9]', ''
$appInsightsInstrumentationKey = $installSettings.appInsightsInstrumentationKey
$siteName = $installSettings.siteName
$sqlServerAddress = $installSettings.sqlServerAddress
$identityServerUrl = $installSettings.identityService
$metadataDbName = $installSettings.metadataDbName
$authorizationDbName = $installSettings.authorizationDbName
$authorizationDatabaseRole = $installSettings.authorizationDatabaseRole
$fabricInstallerSecret = $installSettings.fabricInstallerSecret
$hostUrl = $installSettings.hostUrl
$authorizationServiceUrl = $installSettings.authorizationSerivce
$storedIisUser = $installSettings.iisUser
$adminAccount = $installSettings.adminAccount
$currentUserDomain = $env:userdnsdomain
$workingDirectory = Get-CurrentScriptDirectory

if ([string]::IsNullOrEmpty($installSettings.discoveryService)) {
    $discoveryServiceUrl = Get-DiscoveryServiceUrl
}
else {
    $discoveryServiceUrl = $installSettings.discoveryService
}

if ([string]::IsNullOrEmpty($installSettings.identityService)) {
    $identityServerUrl = Get-IdentityServiceUrl
}
else {
    $identityServerUrl = $installSettings.identityService
}

if ([string]::IsNullOrEmpty($installSettings.authorizationSerivce)) {
    $authorizationServiceUrl = "https://$env:computername.$($env:userdnsdomain.tolower())/Authorization"
}
else {
    $authorizationServiceUrl = $installSettings.authorizationSerivce
}

Write-Host ""
Write-Host "Checking prerequisites..."
Write-Host ""

if (!(Test-PrerequisiteExact "*.NET Core*Windows Server Hosting*" 1.1.30503.82)) {    
    try {
        Write-Console "Windows Server Hosting Bundle version 1.1.30503.82 not installed...installing version 1.1.30503.82"        
        Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=848766 -OutFile $env:Temp\bundle.exe
        Start-Process $env:Temp\bundle.exe -Wait -ArgumentList '/quiet /install'
        net stop was /y
        net start w3svc
        Write-Console "Windows Server Hosting Bundle installed successfully."
    }
    catch {
        Write-Error "Could not install .NET Windows Server Hosting bundle. Please install the hosting bundle before proceeding. https://go.microsoft.com/fwlink/?linkid=844461."
        throw
    }
    try {
        Remove-Item $env:Temp\bundle.exe
    }
    catch {        
        $e = $_.Exception        
        Write-Warning "Unable to remove Server Hosting bundle exe" 
        Write-Warning $e.Message
    }

}
else {
    Write-Success ".NET Core Windows Server Hosting Bundle installed and meets expectations."
    Write-Host ""
}

if (!(Test-Prerequisite "*IIS URL Rewrite Module 2" 7.2.1952)) {
    try{
        Write-Console "IIS URL Rewrite Module 2 not installed...installing latest version."
        Invoke-WebRequest -Uri "http://download.microsoft.com/download/D/D/E/DDE57C26-C62C-4C59-A1BB-31D58B36ADA2/rewrite_amd64_en-US.msi" -OutFile $env:Temp\rewrite_amd64_en-US.msi
        Start-Process msiexec.exe -Wait -ArgumentList "/i $($env:Temp)\rewrite_amd64_en-US.msi /qn"
        Write-Console "IIS URL Rewrite Module 2 installed successfully."
    }catch{
        Write-Error "Could not install IIS URL Rewrite Module 2. Please install the IIS URL Rewrite Module 2 before proceeding: https://www.iis.net/downloads/microsoft/url-rewrite."
        throw
    }
    try {
        Remove-Item $env:Temp\rewrite_amd64_en-US.msi
    }
    catch {        
        $e = $_.Exception        
        Write-Warning "Unable to remove IIS Rewrite msi installer." 
        Write-Warning $e.Message
    }
}

try {
    $sites = Get-ChildItem IIS:\Sites
    if ($sites -is [array]) {
        $sites |
            ForEach-Object {New-Object PSCustomObject -Property @{
                'Id'            = $_.id;
                'Name'          = $_.name;
                'Physical Path' = [System.Environment]::ExpandEnvironmentVariables($_.physicalPath);
                'Bindings'      = $_.bindings;
            }; } |
            Format-Table Id, Name, 'Physical Path', Bindings -AutoSize

        $selectedSiteId = Read-Host "Select a web site by Id"
        Write-Host ""
        $selectedSite = $sites[$selectedSiteId - 1]
    }
    else {
        $selectedSite = $sites
    }

    $webroot = [System.Environment]::ExpandEnvironmentVariables($selectedSite.physicalPath)    
    $siteName = $selectedSite.name

}
catch {
    Write-Error "Could not select a website."
    throw
}

if ([string]::IsNullOrEmpty($encryptionCertificateThumbprint)) {
    try {

        $allCerts = Get-CertsFromLocation Cert:\LocalMachine\My
        $index = 1
        $allCerts |
            ForEach-Object {New-Object PSCustomObject -Property @{
                'Index'      = $index;
                'Subject'    = $_.Subject; 
                'Name'       = $_.FriendlyName; 
                'Thumbprint' = $_.Thumbprint; 
                'Expiration' = $_.NotAfter
            };
            $index ++} |
            Format-Table Index, Name, Subject, Expiration, Thumbprint  -AutoSize

        $selectionNumber = Read-Host  "Select an encryption certificate by Index"
        Write-Host ""
        if ([string]::IsNullOrEmpty($selectionNumber)) {
            Write-Error "You must select a certificate so Fabric.Identity can sign access and identity tokens."
            throw
        }
        $selectionNumberAsInt = [convert]::ToInt32($selectionNumber, 10)
        if (($selectionNumberAsInt -gt $allCerts.Count) -or ($selectionNumberAsInt -le 0)) {
            Write-Error "Please select a certificate with index between 1 and $($allCerts.Count)." 
            throw
        }
        $certThumbprint = Get-CertThumbprint $allCerts $selectionNumberAsInt       
        $encryptionCertificateThumbprint = $certThumbprint -replace '[^a-zA-Z0-9]', ''
    }
    catch {
        $scriptDirectory = Get-CurrentScriptDirectory
        Set-Location $scriptDirectory
        Write-Error "Could not set the certificate thumbprint. Error $($_.Exception.Message)"
        throw
    }

}

try {
    $encryptionCert = Get-Certificate $encryptionCertificateThumbprint
}
catch {
    Write-Host "Could not get encryption certificate with thumbprint $encryptionCertificateThumbprint. Please verify that the encryptionCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store. Halting installation."
    throw $_.Exception
}


$userEnteredFabricInstallerSecret = Read-Host  "Enter the Fabric Installer Secret or hit enter to accept the default [$fabricInstallerSecret]"
Write-Host ""
if (![string]::IsNullOrEmpty($userEnteredFabricInstallerSecret)) {   
    $fabricInstallerSecret = $userEnteredFabricInstallerSecret
}

if ((Test-Path $zipPackage)) {
    $path = [System.IO.Path]::GetDirectoryName($zipPackage)
    if (!$path) {
        $zipPackage = [System.IO.Path]::Combine($workingDirectory, $zipPackage)
        Write-Host "zipPackage: $zipPackage"
        Write-Host ""
    }
}
else {
    Write-Host "Could not find file or directory $zipPackage, please verify that the zipPackage configuration setting in install.config is the path to a valid zip file that exists. Halting installation."
    exit 1
}


$userEnteredAuthorizationServiceUrl = Read-Host  "Enter the URL for the Authorization Service or hit enter to accept the default [$authorizationServiceUrl]"
Write-Host ""
if (![string]::IsNullOrEmpty($userEnteredAuthorizationServiceUrl)) {   
    $authorizationServiceUrl = $userEnteredAuthorizationServiceUrl
}

$userEnteredIdentityServiceUrl = Read-Host  "Enter the URL for the Identity Service or hit enter to accept the default [$identityServerUrl]"
Write-Host ""
if (![string]::IsNullOrEmpty($userEnteredIdentityServiceUrl)) {   
    $identityServerUrl = $userEnteredIdentityServiceUrl
}

if (!($noDiscoveryService)) {
    $userEnteredDiscoveryServiceUrl = Read-Host "Press Enter to accept the default DiscoveryService URL [$discoveryServiceUrl] or enter a new URL"
    Write-Host ""
    if (![string]::IsNullOrEmpty($userEnteredDiscoveryServiceUrl)) {   
        $discoveryServiceUrl = $userEnteredDiscoveryServiceUrl
    }

}


if (![string]::IsNullOrEmpty($storedIisUser)) {
    $userEnteredIisUser = Read-Host "Press Enter to accept the default IIS App Pool User '$($storedIisUser)' or enter a new App Pool User"
    if ([string]::IsNullOrEmpty($userEnteredIisUser)) {
        $userEnteredIisUser = $storedIisUser
    }
}
else {
    $userEnteredIisUser = Read-Host "Please enter a user account for the App Pool"
}

if (![string]::IsNullOrEmpty($userEnteredIisUser)) {
    
    $iisUser = $userEnteredIisUser
    $useSpecificUser = $true
    $userEnteredPassword = Read-Host "Enter the password for $iisUser" -AsSecureString
    $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $iisUser, $userEnteredPassword
    [System.Reflection.Assembly]::LoadWithPartialName("System.DirectoryServices.AccountManagement") | Out-Null
    $ct = [System.DirectoryServices.AccountManagement.ContextType]::Domain
    $pc = New-Object System.DirectoryServices.AccountManagement.PrincipalContext -ArgumentList $ct, $credential.GetNetworkCredential().Domain
    $isValid = $pc.ValidateCredentials($credential.GetNetworkCredential().UserName, $credential.GetNetworkCredential().Password)
    if (!$isValid) {
        Write-Error "Incorrect credentials for $iisUser"
        throw
    }
    Write-Success "Credentials are valid for user $iisUser"
    Write-Host ""
}
else {
    Write-Error "No user account was entered, please enter a valid user account."
    throw
}

$userEnteredAppInsightsInstrumentationKey = Read-Host  "Enter Application Insights instrumentation key or hit enter to accept the default [$appInsightsInstrumentationKey]"
Write-Host ""

if (![string]::IsNullOrEmpty($userEnteredAppInsightsInstrumentationKey)) {   
    $appInsightsInstrumentationKey = $userEnteredAppInsightsInstrumentationKey
}

$userEnteredSqlServerAddress = Read-Host "Press Enter to accept the default Sql Server address '$($sqlServerAddress)' or enter a new Sql Server address" 
Write-Host ""

if (![string]::IsNullOrEmpty($userEnteredSqlServerAddress)) {
    $sqlServerAddress = $userEnteredSqlServerAddress
}

$userEnteredAuthorizationDbName = Read-Host "Press Enter to accept the default Authorization DB Name '$($authorizationDbName)' or enter a new Authorization DB Name"
if (![string]::IsNullOrEmpty($userEnteredAuthorizationDbName)) {
    $authorizationDbName = $userEnteredAuthorizationDbName
}

$authorizationDbConnStr = "Server=$($sqlServerAddress);Database=$($authorizationDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

Invoke-Sql $authorizationDbConnStr "SELECT TOP 1 ClientId FROM Clients" | Out-Null
Write-Success "Identity DB Connection string: $authorizationDbConnStr verified"
Write-Host ""

if (!($noDiscoveryService)) {
    $userEnteredMetadataDbName = Read-Host "Press Enter to accept the default Metadata DB Name '$($metadataDbName)' or enter a new Metadata DB Name"
    if (![string]::IsNullOrEmpty($userEnteredMetadataDbName)) {
        $metadataDbName = $userEnteredMetadataDbName
    }

    $metadataConnStr = "Server=$($sqlServerAddress);Database=$($metadataDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"
    Invoke-Sql $metadataConnStr "SELECT TOP 1 RoleID FROM CatalystAdmin.RoleBASE" | Out-Null
    Write-Success "Metadata DB Connection string: $metadataConnStr verified"
    Write-Host ""
}

$userEnteredDomain = Read-Host "Press Enter to accept the default domain '$($currentUserDomain)' that the user/group who will administrate dos is a member or enter a new domain" 
Write-Host ""

if (![string]::IsNullOrEmpty($userEnteredDomain)) {
    $currentUserDomain = $userEnteredDomain
}

if ([string]::IsNullOrEmpty($adminAccount)) {
    $userEnteredAdminAccount = Read-Host "Please enter the user/group account for dos administration in the format [DOMAIN\user]"
}
else {
    $userEnteredAdminAccount = Read-Host "Press Enter to accept the default admin account '$($adminAccount)' for the user/group who will administrate dos or enter a new account"
}

Write-Host ""

if (![string]::IsNullOrEmpty($userEnteredAdminAccount)) {
    $adminAccount = $userEnteredAdminAccount
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
    Write-Error "$samAccountName is not a valid principal in the $currentUserDomain domain. Please enter a valid account. Halting installation."
    throw
}


Write-Host ""
Write-Host "Prerequisite checks complete...installing."
Write-Host ""


if (!($noDiscoveryService)) {
    Write-Host ""
    Write-Host "Adding Service User to Discovery."
    Write-Host ""
    Add-ServiceUserToDiscovery $credential.UserName $metadataConnStr
    Write-Host ""
    Write-Host "Registering with Discovery Service."
    Write-Host ""
    Add-DiscoveryRegistration $discoveryServiceUrl "$authorizationServiceUrl/v1" $credential
    Write-Host ""
}

$appDirectory = [System.IO.Path]::Combine($webroot, $appName)
New-AppRoot $appDirectory $iisUser
Write-Host "App directory is: $appDirectory"
New-AppPool $appName $iisUser $credential
New-App $appName $siteName $appDirectory
Publish-WebSite $zipPackage $appDirectory $appName
Add-DatabaseSecurity $iisUser $authorizationDatabaseRole $authorizationDbConnStr

Set-Location $workingDirectory

Write-Host ""
Write-Host "Getting access token for installer, at URL: $identityServerUrl"
$accessToken = Get-AccessToken $identityServerUrl "fabric-installer" "fabric/identity.manageresources" $fabricInstallerSecret

#Register authorization api
$body = @'
{
    "name":"authorization-api",
    "userClaims":["name","email","role","groups"],
    "scopes":[{"name":"fabric/authorization.read"}, {"name":"fabric/authorization.write"}, {"name":"fabric/authorization.dos.write"}, {"name":"fabric/authorization.manageclients"}]
}
'@

Write-Host "Registering Fabric.Authorization API."
$authorizationApiSecret = ([string](Add-ApiRegistration -authUrl $identityServerUrl -body $body -accessToken $accessToken)).Trim()

if ($authorizationApiSecret) {
    Write-Success "Fabric.Authorization apiSecret: $authorizationApiSecret"
    Write-Host ""
}

#Register Fabric.Authorization client
$body = @'
{
    "clientId":"fabric-authorization-client", 
    "clientName":"Fabric Authorization Client", 
    "requireConsent":"false", 
    "allowedGrantTypes": ["client_credentials"], 
    "allowedScopes": ["fabric/identity.read", "fabric/identity.searchusers"]
}
'@

Write-Host "Registering Fabric.Authorization Client."
$authorizationClientSecret = ([string](Add-ClientRegistration -authUrl $identityServerUrl -body $body -accessToken $accessToken)).Trim()

if ($authorizationClientSecret) {
    Write-Success "Fabric.Authorization clientSecret: $authorizationClientSecret"
    Write-Host ""
}

#Write environment variables
Write-Host "Loading up environment variables..."
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

$environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", $identityServerUrl)

if ($authorizationDbConnStr) {
    $environmentVariables.Add("ConnectionStrings__AuthorizationDatabase", $authorizationDbConnStr)
}

if ($adminAccount) {
    $environmentVariables.Add("AdminAccount__Name", $adminAccount)
}

if ($adminAccountIsUser) {
    $environmentVariables.Add("AdminAccount__Type", "user")
}
else {
    $environmentVariables.Add("AdminAccount__Type", "group")
}

if ($authorizationApiSecret) {
    $encryptedSecret = Get-EncryptedString $encryptionCert $authorizationApiSecret
    $environmentVariables.Add("IdentityServerApiSettings__ApiSecret", $encryptedSecret)
}

Set-EnvironmentVariables $appDirectory $environmentVariables | Out-Null
Write-Host ""

Write-Host "Setting up Admin account."
$accessToken = Get-AccessToken $identityServerUrl "fabric-installer" "fabric/authorization.read fabric/authorization.write fabric/authorization.dos.write fabric/authorization.manageclients" $fabricInstallerSecret
Add-AccountToDosAdminRole -accountName $adminAccount -domain $currentUserDomain -authorizationServiceUrl "$authorizationServiceUrl/v1" -accessToken $accessToken -connString $authorizationDbConnStr

Set-Location $workingDirectory

if ($fabricInstallerSecret) { Add-SecureInstallationSetting "common" "fabricInstallerSecret" $fabricInstallerSecret $encryptionCert | Out-Null}
if ($encryptionCertificateThumbprint) { Add-InstallationSetting "common" "encryptionCertificateThumbprint" $encryptionCertificateThumbprint | Out-Null}
if ($encryptionCertificateThumbprint) { Add-InstallationSetting "authorization" "encryptionCertificateThumbprint" $encryptionCertificateThumbprint | Out-Null}
if ($appInsightsInstrumentationKey) { Add-InstallationSetting "authorization" "appInsightsInstrumentationKey" "$appInsightsInstrumentationKey" | Out-Null}
if ($sqlServerAddress) { Add-InstallationSetting "common" "sqlServerAddress" "$sqlServerAddress" | Out-Null}
if ($metadataDbName) { Add-InstallationSetting "common" "metadataDbName" "$metadataDbName" | Out-Null}
if ($identityServerUrl) { Add-InstallationSetting "common" "identityService" "$identityServerUrl" | Out-Null}
if ($discoveryServiceUrl) { Add-InstallationSetting "common" "discoveryService" "$discoveryServiceUrl" | Out-Null}
if ($authorizationServiceUrl) { Add-InstallationSetting "authorization" "authorizationService" "$authorizationServiceUrl" | Out-Null}
if ($authorizationServiceUrl) { Add-InstallationSetting "common" "authorizationService" "$authorizationServiceUrl" | Out-Null}
if ($iisUser) { Add-InstallationSetting "authorization" "iisUser" "$iisUser" | Out-Null}
if ($siteName) {Add-InstallationSetting "authorization" "siteName" "$siteName" | Out-Null}
if ($adminAccount) {Add-InstallationSetting "authorization" "adminAccount" "$adminAccount" | Out-Null}

Invoke-MonitorShallow "$authorizationServiceUrl"

Read-Host -Prompt "Installation complete, press Enter to exit"
