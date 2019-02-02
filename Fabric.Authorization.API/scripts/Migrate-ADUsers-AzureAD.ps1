#Requires -RunAsAdministrator
#Requires -Version 5.1
#Requires -Modules PowerShellGet, PackageManagement

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
# Install-Module ActiveDirectory

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

# Do I need this duplicate code as the #Require above
# Do I need to check powershell version depending on if #Require is only necessary
#if(!(Test-IsRunAsAdministrator))
#{
#    Write-DosMessage -Level "Error" -Message "You must run this script as an administrator. Halting configuration."
#    throw
#}

Write-DosMessage -Level "Information" -Message "Using install.config: $installConfigPath"
$installSettingsScope = "authorization"
$installSettings = Get-InstallationSettings $installSettingsScope -installConfigPath $installConfigPath

$commonSettingsScope = "common"
$commonInstallSettings = Get-InstallationSettings $commonSettingsScope -installConfigPath $installConfigPath
Set-LoggingConfiguration -commonConfig $commonInstallSettings

$tenants = Get-Tenants -installConfigPath $installConfigPath

#$selectedCerts = Get-Certificates -primarySigningCertificateThumbprint $installSettings.encryptionCertificateThumbprint -encryptionCertificateThumbprint $installSettings.encryptionCertificateThumbprint -installConfigPath $installConfigPath -scope $installSettingsScope -quiet $quiet
#$iisUser = Get-IISAppPoolUser -credential $credential -appName $installSettings.appName -storedIisUser $installSettings.iisUser -installConfigPath $installConfigPath -scope $installSettingsScope

# Adding permission to private key for file system access given to app pool user, should have been done in Authorization install
# Add-PermissionToPrivateKey $iisUser.UserName $selectedCerts.EncryptionCertificate read

$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $installSettings.sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet

$authorizationDatabase = Get-AuthorizationDatabaseConnectionString -authorizationDbName $installSettings.authorizationDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet

# Connect to the authorization database and get all the users and store them in a hash table
# Method to get authorization database users
$authUserTable = Get-AuthUsers -connectionString $authorizationDatabase.DbConnectionString

# Connect to AD to get ObjectId/ms-dsConsistenceyGuid for each user in authorization
$currentUserDomain = Get-CurrentUserDomain -quiet $quiet

$ADUserTable = Get-ADUsers -userTable $authUserTable -domain $currentUserDomain

$AADUserTable = Get-AzureADUsers -userTable $ADUserTable -tenants $tenants

# Add azureAD user objectId to the AuthorizationDB Users table
# Method to add authorization database users
$authUserTable = Add-AuthUsers -connectionString $authorizationDatabase.DbConnectionString -userTable $AADUserTable

# Database security for app pool user, should have been done in Authorization install
# Add-DatabaseSecurity $iisUser.UserName $installSettings.authorizationDatabaseRole $authorizationDatabase.DbConnectionString

#$accessToken = Get-AccessToken -authUrl $identityServiceUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret $installSettings.fabricInstallerSecret
