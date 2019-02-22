# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

# Import CatalystDosIdentity
$minVersion = [System.Version]::new(1, 4, 18200, 12)
try {
    Get-InstalledModule -Name CatalystDosIdentity -MinimumVersion $minVersion -ErrorAction Stop
} catch {
    Write-Host "Installing CatalystDosIdentity from Powershell Gallery"
    Install-Module CatalystDosIdentity -Scope CurrentUser -MinimumVersion $minVersion -Force
}
Import-Module -Name CatalystDosIdentity -MinimumVersion $minVersion -Force

function Add-AccessControlRegistration($discoveryServiceUrl, $identityServiceUrl, $fabricInstallerSecret){
    Write-Host "Getting access token for installer"
    $accessToken = Get-FabricInstallerAccessToken -identityUrl $identityServiceUrl -secret $fabricInstallerSecret
    Write-Host ""

    Write-Host "Getting current client registration"
    $accessControlClient = Get-ClientRegistration -identityUrl $identityServiceUrl -clientId "fabric-access-control" -accessToken $accessToken
    Write-Host "Current RedirectUris: $($accessControlClient.redirectUris)"
    Write-Host "Current CorsOrigins: $($accessControlClient.allowedCorsOrigins)"
    Write-Host ""

    $redirectUris = {$accessControlClient.redirectUris}.Invoke()
    if(!($redirectUris.Contains("http://localhost/AuthorizationDev/client/oidc-callback.html"))){
        $redirectUris.Add("http://localhost/AuthorizationDev/client/oidc-callback.html")
    }
    if(!($redirectUris.Contains("http://localhost/AuthorizationDev/client/silent.html"))){
        $redirectUris.Add("http://localhost/AuthorizationDev/client/silent.html")
    }
    $accessControlClient.redirectUris = $redirectUris

    $corsOrigins = {$accessControlClient.allowedCorsOrigins}.Invoke()
    if(!($corsOrigins.Contains("http://localhost"))){
        $corsOrigins.Add("http://localhost")
    }
    $accessControlClient.allowedCorsOrigins = $corsOrigins

    Write-Host "Updating client registration"
    Edit-ClientRegistration -identityUrl $identityServiceUrl -body $accessControlClient -accessToken $accessToken
    Write-Host ""

    Write-Host "Getting updated client registration"
    $updatedAccessControlClient = Get-ClientRegistration -identityUrl $identityServiceUrl -clientId "fabric-access-control" -accessToken $accessToken
    Write-Host "Updated RedirectUris: $($updatedAccessControlClient.redirectUris)"
    Write-Host "Updated CorsOrigins: $($updatedAccessControlClient.allowedCorsOrigins)"
    Write-Host ""

}

$installSettingsScope = "authorization"
$installConfigPath = "C:\Program Files\Health Catalyst\install.config"
if(!(Test-Path $installConfigPath)){
    throw "install.config is not in the default directory, please ensure you have installed DOS."
}
$installSettings = Get-InstallationSettings $installSettingsScope -installConfigPath $installConfigPath
Add-AccessControlRegistration -discoveryServiceUrl $installSettings.discoveryService -identityServiceUrl $installSettings.identityService -fabricInstallerSecret $installSettings.fabricInstallerSecret



