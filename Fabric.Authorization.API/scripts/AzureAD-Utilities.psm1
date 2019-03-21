# Import Dos Install Utilities
$minVersion = [System.Version]::new(1, 0, 248, 0)
try {
    Get-InstalledModule -Name DosInstallUtilities -MinimumVersion $minVersion -ErrorAction Stop
} catch {
    Write-Host "Installing DosInstallUtilities from Powershell Gallery"
    Install-Module DosInstallUtilities -Scope CurrentUser -MinimumVersion $minVersion -Force
}
Import-Module -Name DosInstallUtilities -Force

# Import Fabric Install Utilities
$fabricInstallUtilities = "$PSScriptRoot\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Get-WebRequestDownload -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -NoCache -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

# Import AzureAD
$minVersion = [System.Version]::new(2, 0, 2 , 4)
$azureAD = Get-Childitem -Path ./**/AzureAD.psm1 -Recurse
if ($azureAD.length -eq 0) {
    # Do not show error when AzureAD is not installed, will install instead
    $installed = Get-InstalledModule -Name AzureAD -ErrorAction "silentlycontinue"

    if (($null -eq $installed) -or ($installed.Version.CompareTo($minVersion) -lt 0)) {
        Write-DosMessage -Level Information -Message "Installing AzureAD from Powershell Gallery"
        Install-Module AzureAD -Scope CurrentUser -MinimumVersion $minVersion -Force
        Import-Module AzureAD -Force
    }
}
else {
    Write-DosMessage -Level Information -Message "Installing AzureAD at $($azureAD.FullName)"
    Import-Module -Name $azureAD.FullName
}

Add-Type -AssemblyName System.Web

function Connect-AzureADTenant {
    param(
        [Parameter(Mandatory=$true)]
        [string] $tenantId,
        [Parameter(Mandatory=$true)]
        [PSCredential] $credential
    )

    try {
        Connect-AzureAD -Credential $credential -TenantId $tenantId | Out-Null
        return $credential
    }
    catch {
        $errorMsg = "Could not sign into tenant '$tenantId' with user '$($credential.UserName)'"
        Write-DosMessage -Level "Error" -Message $errorMsg
        throw
    }
}

function Get-AzureADTenants {
    param(
        [Parameter(Mandatory=$true)]
        [string] $installConfigPath
    )
    $tenants = @()
    $scope = "identity"
    $parentSetting = "tenants"
    $tenants += Get-SettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $scope `
        -setting $parentSetting

    return $tenants
}

Export-ModuleMember Get-AzureADTenants
Export-ModuleMember Connect-AzureADTenant