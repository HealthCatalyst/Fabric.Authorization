param(
    [PSCredential] $credential,
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
    [switch] $noDiscoveryService, 
    [switch] $quiet
)

Import-Module -Name .\Install-Authorization-Utilities.psm1 -Force

# Import Identity Install Utilities
$identityInstallUtilities = ".\Identity-Install-Utilities.psm1"
if (!(Test-Path $identityInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find identity install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/Fabric.Identity/master/Fabric.Identity.API/scripts/Install-Identity-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name $identityInstallUtilities -Force

# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

Test-MeetsMinimumRequiredPowerShellVerion -majorVersion 5

if(!(Test-IsRunAsAdministrator))
{
    Write-DosMessage -Level "Error" -Message "You must run this script as an administrator. Halting configuration."
    throw
}

Write-DosMessage -Level "Information" -Message "Using install.config: $installConfigPath"
$installSettingsScope = "authorization"
$installSettings = Get-InstallationSettings $installSettingsScope -installConfigPath $installConfigPath
$zipPackage = Get-FullyQualifiedInstallationZipFile -zipPackage $installSettings.zipPackage -workingDirecory $PSScriptRoot
Install-DotNetCoreIfNeeded -version "1.1.30503.82" -downloadUrl "https://go.microsoft.com/fwlink/?linkid=848766"
Install-UrlRewriteIfNeeded -version "7.2.1952" -downloadUrl "http://download.microsoft.com/download/D/D/E/DDE57C26-C62C-4C59-A1BB-31D58B36ADA2/rewrite_amd64_en-US.msi"
$selectedSite = Get-IISWebSiteForInstall -selectedSiteName $installSettings.siteName -quiet $quiet -installConfigPath $installConfigPath -scope $installSettingsScope
$selectedCerts = Get-Certificates -primarySigningCertificateThumbprint $installSettings.encryptionCertificateThumbprint -encryptionCertificateThumbprint $installSettings.encryptionCertificateThumbprint -installConfigPath $installConfigPath -scope $installSettingsScope -quiet $quiet
$iisUser = Get-IISAppPoolUser -credential $credential -appName $installSettings.appName -storedIisUser $installSettings.iisUser -installConfigPath $installConfigPath -scope $installSettingsScope
$appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey $installSettings.appInsightsInstrumentationKey -installConfigPath $installConfigPath -scope $installSettingsScope -quiet $quiet
$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $installSettings.sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet
$authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $installSettings.authorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet
if(!$noDiscoveryService){
    $metadataDatabase = Get-MetadataDatabaseConnectionString -metadataDbName $installSettings.metadataDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet
    $discoveryServiceUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl $installSettings.discoveryService -installConfigPath $installConfigPath -quiet $quiet
}
$identityServiceUrl = Get-IdentityServiceUrl -identityServiceUrl $installSettings.identityService -installConfigPath $installConfigPath -quiet $quiet
$authorizationServiceUrl = Get-ApplicationEndpoint -appName $installSettings.appName -applicationEndpoint $installSettings.applicationEndPoint -installConfigPath $installConfigPath -scope $installSettingsScope -quiet $quiet
#Get domain and admin user

Add-DatabaseSecurity $iisUser.UserName $installSettings.metadataDatabaseRole $metadataDatabase.DbConnectionString
$installApplication = Publish-Application -site $selectedSite `
                 -appName $installSettings.appName `
                 -iisUser $iisUser `
                 -zipPackage $zipPackage `
                 -assembly "Fabric.Authorization.API.dll"

Add-DatabaseSecurity $iisUser.UserName $installSettings.authorizationDatabaseRole $authorizationDatabase.DbConnectionString           
if(!$noDiscoveryService){
    #Register-AuthorizationWithDiscovery -iisUserName $iisUser.UserName -metadataConnStr $metadataDatabase.DbConnectionString -version $installApplication.version -identityServerUrl "$authorizationServiceUrl/v1"
    #Register-AccessControlWithDiscovery -iisUserName $iisUser.UserName -metadataConnStr $metadataDatabase.DbConnectionString -version $installApplication.version -identityServerUrl $authorizationServiceUrl
}      

$accessToken = Get-AccessToken -authUrl $identityServiceUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret $installSettings.fabricInstallerSecret

$authorizationApiSecret = Add-AuthorizationApiRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken
$authorizationClientSecret = Add-AuthorizationClientRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken
$accessControlClientSecret = Add-AccessControlClientRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken

$accessToken = Get-AccessToken -authUrl $identityServiceUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.dos.write fabric/authorization.manageclients" $installSettings.fabricInstallerSecret
Add-AuthorizationRegistration -clientId "fabric-installer" -clientName "Fabric Installer" -authorizationServiceUrl "$authorizationServiceUrl/v1" -accessToken $accessToken | Out-Null
Add-AuthorizationRegistration -clientId "fabric-access-control" -clientName "Fabric.AccessControl" -authorizationServiceUrl "$authorizationServiceUrl/v1" -accessToken $accessToken | Out-Null

Set-AuthorizationEnvironmentVariables

Move-DosAdminRoleToDosAdminGroup -authUrl "$authorizationServiceUrl/v1" -accessToken $accessToken -connectionString $authorizationDbConnStr -groupName $dosAdminGroupName
Add-AccountToDosAdminGroup -accountName $adminAccount -domain $currentUserDomain -authorizationServiceUrl "$authorizationServiceUrl/v1" -accessToken $accessToken -connString $authorizationDbConnStr

$edwAdminUsers = Get-EdwAdminUsersAndGroups -connectionString $metadataConnStr	
Write-Host "There are $($edwAdminUsers.Count) users with this role"	
Write-Host ""	
Add-ListOfUsersToDosAdminGroup -edwAdminUsers $edwAdminUsers -connString $authorizationDbConnStr -authorizationServiceUrl "$authorizationServiceUrl/v1" -accessToken $accessToken


Invoke-MonitorShallow "$authorizationServiceUrl"
