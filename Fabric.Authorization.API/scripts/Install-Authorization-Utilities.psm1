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

function Install-UrlRewriteIfNeeded([string] $version, [string] $downloadUrl){
    if(!(Test-PrerequisiteExact "*IIS URL Rewrite Module 2*" $version))
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

function Get-AuthorizationDatabaseConnectionString([string] $authorizationDbName, [string] $sqlServerAddress, [string] $installConfigPath, [bool] $quiet){
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

function Get-IdentityServiceUrl([string]$identityServiceUrl, [string] $installConfigPath, [bool]$quiet){
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

function Get-DefaultIdentityServiceUrl([string] $identityServiceUrl)
{
    if([string]::IsNullOrEmpty($identityServiceUrl)){
        return "$(Get-FullyQualifiedMachineName)/$identity"
    }else{
        return $identityServiceUrl
    }
}

Export-ModuleMember Install-UrlRewriteIfNeeded
Export-ModuleMember Get-AuthorizationDatabaseConnectionString
Export-ModuleMember Get-IdentityServiceUrl