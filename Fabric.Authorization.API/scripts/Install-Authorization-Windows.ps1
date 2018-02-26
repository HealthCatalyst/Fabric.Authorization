#
# Install_Authorization_Windows.ps1
#
param([switch]$noDiscoveryService)
function Get-DiscoveryServiceUrl()
{
    return "https://$env:computername.$($env:userdnsdomain.tolower())/DiscoveryService"
}

function Get-IdentityServiceUrl()
{
    return "https://$env:computername.$($env:userdnsdomain.tolower())/Identity"
}

function Add-DiscoveryRegistration($discoveryUrl, $serviceUrl, $credential)
{	
    $registrationBody = @{
        ServiceName = "AuthorizationService"
        Version = 1
        ServiceUrl = $serviceUrl
        DiscoveryType = "Service"
        IsHidden = $true
        FriendlyName = "Fabric.Authorization"
        Description = "The Fabric.Authorization service provides centralized authorization across the Fabric ecosystem."
        BuildNumber = "1.1.2017120101"
    }

	$url = "$discoveryUrl/v1/Services"
	$jsonBody = $registrationBody | ConvertTo-Json	
	try{
		Invoke-RestMethod -Method Post -Uri "$url" -Body "$jsonBody" -ContentType "application/json" -Credential $credential | Out-Null
		Write-Success "Fabric.Authorization successfully registered with DiscoveryService."
	}catch{
		Write-Error "Unable to register Fabric.Authorization with DiscoveryService. Error $($_.Exception.Message) Halting installation." -ErrorAction Stop
	}
}

function Invoke-MonitorShallow($authorizationUrl)
{
	$url = "$authorizationUrl/_monitor/shallow"
	Invoke-RestMethod -Method Get -Uri $url
}

if(!(Test-Path .\Fabric-Install-Utilities.psm1)){
	Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Force

if(!(Test-IsRunAsAdministrator))
{
    Write-Error "You must run this script as an administrator. Halting configuration." -ErrorAction Stop
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
$fabricInstallerSecret = $installSettings.fabricInstallerSecret
$hostUrl = $installSettings.hostUrl
$authorizationSerivceURL =  $installSettings.authorizationSerivce
$storedIisUser = $installSettings.iisUser
$workingDirectory = Get-CurrentScriptDirectory

if([string]::IsNullOrEmpty($installSettings.discoveryService))  
{
	$discoveryServiceUrl = Get-DiscoveryServiceUrl
} else
{
	$discoveryServiceUrl = $installSettings.discoveryService
}

if([string]::IsNullOrEmpty($installSettings.identityService))  
{
	$identityServerUrl = Get-IdentityServiceUrl
} else
{
	$identityServerUrl = $installSettings.identityService
}

if([string]::IsNullOrEmpty($installSettings.authorizationSerivce))  
{
	$authorizationSerivceURL = "https://$env:computername.$($env:userdnsdomain.tolower())/Authorization"
} else
{
	$authorizationSerivceURL = $installSettings.authorizationSerivce
}

Write-Host ""
Write-Host "Checking prerequisites..."
Write-Host ""

if(!(Test-PrerequisiteExact "*.NET Core*Windows Server Hosting*" 1.1.30503.82))
{    
    try{
        Write-Console "Windows Server Hosting Bundle version 1.1.30503.82 not installed...installing version 1.1.30503.82"        
        Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=848766 -OutFile $env:Temp\bundle.exe
        Start-Process $env:Temp\bundle.exe -Wait -ArgumentList '/quiet /install'
        net stop was /y
        net start w3svc			
    }catch{
        Write-Error "Could not install .NET Windows Server Hosting bundle. Please install the hosting bundle before proceeding. https://go.microsoft.com/fwlink/?linkid=844461" -ErrorAction Stop
    }
    try{
        Remove-Item $env:Temp\bundle.exe
    }catch{        
        $e = $_.Exception        
        Write-Warning "Unable to remove Server Hosting bundle exe" 
        Write-Warning $e.Message
    }

}else{
    Write-Success ".NET Core Windows Server Hosting Bundle installed and meets expectations."
    Write-Host ""
}

try{
    $sites = Get-ChildItem IIS:\Sites
    if($sites -is [array]){
        $sites |
            ForEach-Object {New-Object PSCustomObject -Property @{
                'Id'=$_.id;
                'Name'=$_.name;
                'Physical Path'=[System.Environment]::ExpandEnvironmentVariables($_.physicalPath);
                'Bindings'=$_.bindings;
            };} |
            Format-Table Id,Name,'Physical Path',Bindings -AutoSize

        $selectedSiteId = Read-Host "Select a web site by Id"
        Write-Host ""
        $selectedSite = $sites[$selectedSiteId - 1]
    }else{
        $selectedSite = $sites
    }

    $webroot = [System.Environment]::ExpandEnvironmentVariables($selectedSite.physicalPath)    
    $siteName = $selectedSite.name

}catch{
    Write-Error "Could not select a website." -ErrorAction Stop
}

try{
	$encryptionCert = Get-Certificate $encryptionCertificateThumbprint
}catch{
	Write-Host "Could not get encryption certificate with thumbprint $encryptionCertificateThumbprint. Please verify that the encryptionCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store. Halting installation."
	throw $_.Exception
}


$userEnteredFabricInstallerSecret = Read-Host  "Enter the Fabric Installer Secret or hit enter to accept the default [$fabricInstallerSecret]"
Write-Host ""
if(![string]::IsNullOrEmpty($userEnteredFabricInstallerSecret)){   
     $fabricInstallerSecret = $userEnteredFabricInstallerSecret
}

if((Test-Path $zipPackage))
{
	$path = [System.IO.Path]::GetDirectoryName($zipPackage)
	if(!$path)
	{
		$zipPackage = [System.IO.Path]::Combine($workingDirectory, $zipPackage)
		Write-Host "zipPackage: $zipPackage"
		Write-Host ""
	}
}else{
	Write-Host "Could not find file or directory $zipPackage, please verify that the zipPackage configuration setting in install.config is the path to a valid zip file that exists. Halting installation."
	exit 1
}


$userEnteredAuthorizationServiceURL = Read-Host  "Enter the URL for the Authorization Service or hit enter to accept the default [$authorizationSerivceURL]"
Write-Host ""
if(![string]::IsNullOrEmpty($userEnteredAuthorizationServiceURL)){   
     $authorizationSerivceURL = $userEnteredAuthorizationServiceURL
}

$userEnteredIdentityServiceURL = Read-Host  "Enter the URL for the Identity Service or hit enter to accept the default [$identityServerUrl]"
Write-Host ""
if(![string]::IsNullOrEmpty($userEnteredIdentityServiceURL)){   
     $identityServerUrl = $userEnteredIdentityServiceURL
}


if(![string]::IsNullOrEmpty($storedIisUser)){
    $userEnteredIisUser = Read-Host "Press Enter to accept the default IIS App Pool User '$($storedIisUser)' or enter a new App Pool User"
    if([string]::IsNullOrEmpty($userEnteredIisUser)){
        $userEnteredIisUser = $storedIisUser
    }
}else{
    $userEnteredIisUser = Read-Host "Please enter a user account for the App Pool"
}

if(![string]::IsNullOrEmpty($userEnteredIisUser)){
    
    $iisUser = $userEnteredIisUser
    $useSpecificUser = $true
    $userEnteredPassword = Read-Host "Enter the password for $iisUser" -AsSecureString
    $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $iisUser, $userEnteredPassword
    [System.Reflection.Assembly]::LoadWithPartialName("System.DirectoryServices.AccountManagement") | Out-Null
    $ct = [System.DirectoryServices.AccountManagement.ContextType]::Domain
    $pc = New-Object System.DirectoryServices.AccountManagement.PrincipalContext -ArgumentList $ct,$credential.GetNetworkCredential().Domain
    $isValid = $pc.ValidateCredentials($credential.GetNetworkCredential().UserName, $credential.GetNetworkCredential().Password)
    if(!$isValid){
        Write-Error "Incorrect credentials for $iisUser" -ErrorAction Stop
    }
    Write-Success "Credentials are valid for user $iisUser"
    Write-Host ""
}else{
    Write-Error "No user account was entered, please enter a valid user account." -ErrorAction Stop
}

$userEnteredAppInsightsInstrumentationKey = Read-Host  "Enter Application Insights instrumentation key or hit enter to accept the default [$appInsightsInstrumentationKey]"
Write-Host ""

if(![string]::IsNullOrEmpty($userEnteredAppInsightsInstrumentationKey)){   
     $appInsightsInstrumentationKey = $userEnteredAppInsightsInstrumentationKey
}

$userEnteredSqlServerAddress = Read-Host "Press Enter to accept the default Sql Server address '$($sqlServerAddress)' or enter a new Sql Server address" 
Write-Host ""

if(![string]::IsNullOrEmpty($userEnteredSqlServerAddress)){
    $sqlServerAddress = $userEnteredSqlServerAddress
}

$userEnteredAuthorizationDbName = Read-Host "Press Enter to accept the default Authorization DB Name '$($authorizationDbName)' or enter a new Authorization DB Name"
if(![string]::IsNullOrEmpty($userEnteredAuthorizationDbName)){
    $authorizationDbName = $userEnteredAuthorizationDbName
}

$authorizationDbConnStr = "Server=$($sqlServerAddress);Database=$($authorizationDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

Invoke-Sql $authorizationDbConnStr "SELECT TOP 1 ClientId FROM Clients" | Out-Null
Write-Success "Identity DB Connection string: $authorizationDbConnStr verified"
Write-Host ""

if(!($noDiscoveryService)){
    $userEnteredMetadataDbName = Read-Host "Press Enter to accept the default Metadata DB Name '$($metadataDbName)' or enter a new Metadata DB Name"
    if(![string]::IsNullOrEmpty($userEnteredMetadataDbName)){
        $metadataDbName = $userEnteredMetadataDbName
    }

    $metadataConnStr = "Server=$($sqlServerAddress);Database=$($metadataDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"
    Invoke-Sql $metadataConnStr "SELECT TOP 1 RoleID FROM CatalystAdmin.RoleBASE" | Out-Null
    Write-Success "Metadata DB Connection string: $metadataConnStr verified"
    Write-Host ""
}

if(!($noDiscoveryService)){
    $userEnteredDiscoveryServiceUrl = Read-Host "Press Enter to accept the default DiscoveryService URL [$discoveryServiceUrl] or enter a new URL"
    Write-Host ""
    if(![string]::IsNullOrEmpty($userEnteredDiscoveryServiceUrl)){   
         $discoveryServiceUrl = $userEnteredDiscoveryServiceUrl
    }

}

if(!($noDiscoveryService)){
    Write-Host ""
	Write-Host "Adding Service User to Discovery."
	Write-Host ""
	Add-ServiceUserToDiscovery $credential.UserName $metadataConnStr
	Write-Host ""
	Write-Host "Registering with Discovery Service."
	Write-Host ""
    Add-DiscoveryRegistration $discoveryServiceUrl $authorizationSerivceURL $credential
    Write-Host ""
}

Write-Host ""
Write-Host "Prerequisite checks complete...installing."
Write-Host ""

$appDirectory = "$webroot\$appName"
New-AppRoot $appDirectory $iisUser
Write-Host "App directory is: $appDirectory"
New-AppPool $appName $iisUser $credential
New-App $appName $siteName $appDirectory
Publish-WebSite $zipPackage $appDirectory $appName

Set-Location $workingDirectory

Write-Host "Getting access token for installer, at URL: $identityServerUrl"
$accessToken = Get-AccessToken $identityServerUrl "fabric-installer" "fabric/identity.manageresources" $fabricInstallerSecret

#Register authorization api
$body = @'
{
	"name":"authorization-api",
	"userClaims":["name","email","role","groups"],
	"scopes":[{"name":"fabric/authorization.read"}, {"name":"fabric/authorization.write"}, {"name":"fabric/authorization.manageclients"}]
}
'@

Write-Host "Registering Fabric.Authorization API."
try {
	$authorizationApiSecret = Add-ApiRegistration -authUrl $identityServerUrl -body $body -accessToken $accessToken
	Write-Host "Fabric.Authorization apiSecret: $authorizationApiSecret"
	Write-Host ""
} catch {
    $exception = $_.Exception
    if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409)
    {
	    Write-Success "Fabric.Authorization API is already registered."
        Write-Host ""
    }else{
        Write-Error "Could not register Fabric.Authorization with Fabric.Identity, halting installation."
        throw $exception
    }

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
try{
	$authorizationClientSecret = Add-ClientRegistration -authUrl $identityServerUrl -body $body -accessToken $accessToken
	Write-Host "Fabric.Authorization clientSecret: $authorizationClientSecret"
	Write-Host ""
} catch {
    $exception = $_.Exception
    if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409)
    {
	    Write-Success "Fabric.Authorization Client is already registered."
        Write-Host ""
    }else{
        Write-Error "Could not register Fabric.Authorization.Client with Fabric.Identity, halting installation."
        throw $exception
    }
}

#Write environment variables
Write-Host ""
Write-Host "Loading up environment variables..."
$environmentVariables = @{"StorageProvider" = "sqlserver"}

if($clientName){
	$environmentVariables.Add("ClientName", $clientName)
}

if ($encryptionCertificateThumbprint){
	$environmentVariables.Add("EncryptionCertificateSettings__EncryptionCertificateThumbprint", $encryptionCertificateThumbprint)
}

if($appInsightsInstrumentationKey){
	$environmentVariables.Add("ApplicationInsights__Enabled", "true")
	$environmentVariables.Add("ApplicationInsights__InstrumentationKey", $appInsightsInstrumentationKey)
}

if($authorizationClientSecret){
	$encryptedSecret = Get-EncryptedString $encryptionCert $authorizationClientSecret
	$environmentVariables.Add("IdentityServerConfidentialClientSettings__ClientSecret", $encryptedSecret)
}


$environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", $identityServerUrl)

if($authorizationDbConnStr){
    $environmentVariables.Add("ConnectionStrings__AuthorizationDatabase", $authorizationDbConnStr)
}

Set-EnvironmentVariables $appDirectory $environmentVariables | Out-Null
Write-Host ""

Set-Location $workingDirectory

if($installerClientSecret){ Add-SecureInstallationSetting "common" "fabricInstallerSecret" $installerClientSecret $signingCert | Out-Null}
if($encryptionCertificateThumbprint){ Add-InstallationSetting "common" "encryptionCertificateThumbprint" $encryptionCertificateThumbprint | Out-Null}
if($encryptionCertificateThumbprint){ Add-InstallationSetting "authorization" "encryptionCertificateThumbprint" $encryptionCertificateThumbprint | Out-Null}
if($appInsightsInstrumentationKey){ Add-InstallationSetting "authorization" "appInsightsInstrumentationKey" "$appInsightsInstrumentationKey" | Out-Null}
if($sqlServerAddress){ Add-InstallationSetting "common" "sqlServerAddress" "$sqlServerAddress" | Out-Null}
if($metadataDbName){ Add-InstallationSetting "common" "metadataDbName" "$metadataDbName" | Out-Null}
if($identityServerUrl){ Add-InstallationSetting "common" "identityService" "$identityServerUrl" | Out-Null}
if($discoveryServiceUrl) { Add-InstallationSetting "common" "discoveryService" "$discoveryServiceUrl" | Out-Null}
if($authorizationSerivceURL) { Add-InstallationSetting "authorization" "authorizationSerivce" "$authorizationSerivceURL" | Out-Null}
if($iisUser) { Add-InstallationSetting "authorization" "iisUser" "$iisUser" | Out-Null}
if($fabricInstallerSecret) {Add-InstallationSetting "common" "fabricInstallerSecret" "$fabricInstallerSecret" | Out-Null}
if($siteName) {Add-InstallationSetting "authorization" "siteName" "$siteName" | Out-Null}

Invoke-MonitorShallow "$hostUrl/$appName"

Write-Host "Installation complete, exiting."