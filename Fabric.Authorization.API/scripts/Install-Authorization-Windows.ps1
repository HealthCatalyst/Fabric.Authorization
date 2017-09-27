#
# Install_Authorization_Windows.ps1
#
function Test-IdentityInstalled($identityServerUrl)
{
	$url = "$identityServerUrl/.well-known/openid-configuration"
	$headers = @{"Accept" = "application/json"}
    
    try {
        $response = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    } catch {
        $exception = $_.Exception
    }

    if($response.issuer -eq "https://fabric.identity")
    {
        return $true
    }
	
    return $false
}

function Invoke-MonitorShallow($authorizationUrl)
{
	$url = "$authorizationUrl/_monitor/shallow"
	Invoke-RestMethod -Method Get -Uri $url
}

if(!(Test-Path .\Fabric-Install-Utilities.psm1)){
	Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1

$installSettings = Get-InstallationSettings "authorization"
$zipPackage = $installSettings.zipPackage
$webroot = $installSettings.webroot 
$appName = $installSettings.appName
$iisUser = $installSettings.iisUser
$sslCertificateThumbprint = $installSettings.sslCertificateThumbprint
$couchDbServer = $installSettings.couchDbServer
$couchDbUsername = $installSettings.couchDbUsername
$couchDbPassword = $installSettings.couchDbPassword
$appInsightsInstrumentationKey = $installSettings.appInsightsInstrumentationKey
$siteName = $installSettings.siteName
$identityServerUrl = $installSettings.identityServerUrl
$fabricInstallerSecret = $installSettings.fabricInstallerSecret
$hostUrl = $installSettings.hostUrl

$workingDirectory = Get-CurrentScriptDirectory

if(!(Test-Prerequisite '*.NET Core*Windows Server Hosting*' 1.1.30327.81))
{
    Write-Host ".NET Core Windows Server Hosting Bundle minimum version 1.1.30327.81 not installed...download and install from https://go.microsoft.com/fwlink/?linkid=844461. Halting installation."
    exit 1
}else{
    Write-Host ".NET Core Windows Server Hosting Bundle installed and meets expectations."
}

if(!(Test-Prerequisite '*CouchDB*'))
{
    Write-Host "CouchDB not installed locally, testing to see if is installed on a remote server using $couchDbServer"
    $remoteInstallationStatus = Get-CouchDbRemoteInstallationStatus $couchDbServer 2.0.0
    if($remoteInstallationStatus -eq "NotInstalled")
    {
        Write-Host "CouchDB not installed, download and install from https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi. Halting installation."
		exit 1
    }elseif($remoteInstallationStatus -eq "MinVersionNotMet"){
        Write-Host "CouchDB is installed on $couchDbServer but does not meet the minimum version requirements, you must have CouchDB 2.0.0.1 or greater installed: https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi. Halting installation."
        exit 1
    }else{
        Write-Host "CouchDB installed and meets specifications"
    }
}elseif (!(Test-Prerequisite '*CouchDB*' 2.0.0.1)) {
    Write-Host "CouchDB is installed but does not meet the minimum version requirements, you must have CouchDB 2.0.0.1 or greater installed: https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi. Halting installation."
    exit 1
}else{
    Write-Host "CouchDB installed and meets specifications"
}

if(!(Test-IdentityInstalled $identityServerUrl)){
	Write-Host "Fabric.Identity is not installed, please first install Fabric.Identity then install Fabric.Authorization. Halting installation."
	exit 1
}

$appDirectory = "$webroot\$appName"
New-AppRoot $appDirectory $iisUser
Write-Host "App directory is: $appDirectory"
New-AppPool $appName
New-App $appName $siteName $appDirectory
Publish-WebSite $zipPackage $appDirectory $appName

#Write environment variables
Write-Host ""
Write-Host "Loading up environment variables..."
$environmentVariables = @{"HostingOptions__UseInMemoryStores" = "false"}
$signingCert = Get-Item Cert:\LocalMachine\My\$sslCertificateThumbprint

if($clientName){
	$environmentVariables.Add("ClientName", $clientName)
}

if ($sslCertificateThumbprint){
	$environmentVariables.Add("EncryptionCertificateSettings__EncryptionCertificateThumbprint", $sslCertificateThumbprint)
}

if ($couchDbServer){
	$environmentVariables.Add("CouchDbSettings__Server", $couchDbServer)
}

if ($couchDbUsername){
	$environmentVariables.Add("CouchDbSettings__Username", $couchDbUsername)
}

if ($couchDbPassword){
	$encryptedCouchDbPassword = Get-EncryptedString $signingCert $couchDbPassword
	$environmentVariables.Add("CouchDbSettings__Password", $encryptedCouchDbPassword)
}

if($appInsightsInstrumentationKey){
	$environmentVariables.Add("ApplicationInsights__Enabled", "true")
	$environmentVariables.Add("ApplicationInsights__InstrumentationKey", $appInsightsInstrumentationKey)
}

$environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", $identityServerUrl)

Set-EnvironmentVariables $appDirectory $environmentVariables
Write-Host ""

Set-Location $workingDirectory

$accessToken = Get-AccessToken -authUrl $identityServerUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret $fabricInstallerSecret

#Register authorization api
$body = @'
{
	"name":"authorization-api",
	"userClaims":["name","email","role","groups"],
	"scopes":[{"name":"fabric/authorization.read"}, {"name":"fabric/authorization.write"}, {"name":"fabric/authorization.manageclients"}]
}
'@

Write-Host "Registering Fabric.Authorization."
try {
	$authorizationApiSecret = Add-ApiRegistration -authUrl $identityServerUrl -body $body -accessToken $accessToken
	Write-Host "Fabric.Authorization apiSecret: $authorizationApiSecret"
	Write-Host ""
} catch {
	Write-Host "Fabric.Authorization is already registered."
	Write-Host ""
}


#Register group fetcher client

$body = @'
{
    "clientId":"fabric-group-fetcher", 
    "clientName":"Fabric Group Fetcher", 
    "requireConsent":"false", 
    "allowedGrantTypes": ["client_credentials"], 
    "allowedScopes": ["fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients"]
}
'@

Write-Host "Registering Fabric.GroupFetcher."
try{
	$groupFetcherSecret = Add-ClientRegistration -authUrl $identityServerUrl -body $body -accessToken $accessToken
	Write-Host "Fabric.GroupFetcher clientSecret: $groupFetcherSecret"
	Write-Host ""
} catch {
	Write-Host "Fabric.GroupFetcher is already registered."
	Write-Host ""
}

Invoke-MonitorShallow "$hostUrl/$appName"
Write-Host "Installation complete, exiting."