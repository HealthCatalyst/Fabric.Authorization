#
# Install_Authorization_Windows.ps1
#
param(
	[String]$zipPackage, 
	[String]$webroot = "C:\inetpub\wwwroot", 
	[String]$appName = "authorization", 
	[String]$iisUser = "IIS_IUSRS", 
	[String]$sslCertificateThumbprint,
	[String]$couchDbServer,
	[String]$couchDbUsername,
	[String]$couchDbPassword,
	[String]$appInsightsInstrumentationKey,
	[String]$siteName = "Default Web Site",
	[String]$identityServerUrl)

Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -OutFile Fabric-Install-Utilities.psm1
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Verbose

$appDirectory = "$webroot\$appName"
New-AppRoot $appDirectory $iisUser
Write-Host "App directory is: $appDirectory"
New-AppPool $appName
New-App $appName $siteName $appDirectory
Publish-WebSite $zipPackage $appDirectory

#Write environment variables
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