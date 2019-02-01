Import-Module WebAdministration
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Invoke-WaitForWebAppPoolToChangeState($name, $state){
    $currentState = Get-WebAppPoolState -Name $name
    Write-Host "Waiting for app pool '$name' to enter the '$state' state" -NoNewLine
    DO{
        Write-Host "." -NoNewLine
        Start-Sleep 1
        $currentState = Get-WebAppPoolState -Name $name
    }while($currentState.Value -ne $state)
    Write-Host ""
}

function Add-EnvironmentVariable($variableName, $variableValue, $config){
    $environmentVariablesNode = $config.configuration.'system.webServer'.aspNetCore.environmentVariables
    $existingEnvironmentVariable = $environmentVariablesNode.environmentVariable | Where-Object {$_.name -eq $variableName}
    if($existingEnvironmentVariable -eq $null){
        Write-Console "Writing $variableName to config"
        $environmentVariable = $config.CreateElement("environmentVariable")
        
        $nameAttribute = $config.CreateAttribute("name")
        $nameAttribute.Value = $variableName
        $environmentVariable.Attributes.Append($nameAttribute)
        
        $valueAttribute = $config.CreateAttribute("value")
        $valueAttribute.Value = $variableValue
        $environmentVariable.Attributes.Append($valueAttribute)

        $environmentVariablesNode.AppendChild($environmentVariable)
    }else {
        Write-Console $variableName "already exists in config, not overwriting"
    }
}

function New-AppRoot($appDirectory, $iisUser){
    # Create the necessary directories for the app
    $logDirectory = "$appDirectory\logs"

    if(!(Test-Path $appDirectory)) {
        Write-Console "Creating application directory: $appDirectory."
        mkdir $appDirectory | Out-Null
    }else{
        Write-Console "Application directory: $appDirectory exists."
    }

    
    if(!(Test-Path $logDirectory)) {
        Write-Console "Creating application log directory: $logDirectory."
        mkdir $logDirectory | Out-Null
        Write-Console "Setting Write and Read access for $iisUser on $logDirectory."
        $acl = Get-Acl $logDirectory
        $writeAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($iisUser, "Write", "ContainerInherit,ObjectInherit", "None", "Allow")
        $readAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($iisUser, "Read", "ContainerInherit,ObjectInherit", "None", "Allow")

        try {			
            $acl.AddAccessRule($writeAccessRule)
        } catch [System.InvalidOperationException]
        {
            # Attempt to fix parent identity directory before log directory
            RepairAclCanonicalOrder(Get-Acl $appDirectory)
            RepairAclCanonicalOrder($acl)
            $acl.AddAccessRule($writeAccessRule)
        }
		
        try {
            $acl.AddAccessRule($readAccessRule)
        } catch [System.InvalidOperationException]
        {
            RepairAclCanonicalOrder($acl)
            $acl.AddAccessRule($readAccessRule)
        }
		
		try {
            Set-Acl -Path $logDirectory $acl
        } catch [System.InvalidOperationException]
        {
            RepairAclCanonicalOrder($acl)
            Set-Acl -Path $logDirectory $acl
        }
    }else{
        Write-Console "Log directory: $logDirectory exists"
    }
}

function New-AppPool($appName, $userName, $credential){
    Set-Location IIS:\AppPools
    if(!(Test-Path $appName -PathType Container))
    {
        Write-Console "AppPool $appName does not exist...creating."
        $appPool = New-WebAppPool $appName
        $appPool | Set-ItemProperty -Name "managedRuntimeVersion" -Value ""
        
    }else{
        Write-Console "AppPool: $appName exists."
        $appPool = Get-Item $appName
    }

    if(![string]::IsNullOrEmpty($userName) -and $credential -ne $null)
    {
        $appPool.processModel.userName = $userName
        $appPool.processModel.password = $credential.GetNetworkCredential().Password
        $appPool.processModel.identityType = 3
        $appPool.processModel.loaduserprofile = $true
        $appPool | Set-Item
        $appPool.Stop()
        Invoke-WaitForWebAppPoolToChangeState -name $appPool.Name -state "Stopped"
    }
    $appPool.Start()
    Invoke-WaitForWebAppPoolToChangeState -name $appPool.Name -state "Started"
}

function Test-AppPoolExistsAndRunsAsUser([string]$appPoolName, [string]$userName){
    if(Test-AppPoolExists -appPoolName $appPoolName){
        $appPool = Get-AppPool -appPoolName $appPoolName
        return Test-AppPoolRunsAsUser -appPool $appPool -userName $userName
    }
    return $false
}

function Get-AppPool([string]$appPoolName){
    return Get-Item "IIS:\AppPools\$appPoolName"
}

function Test-AppPoolExists([string]$appPoolName){
    return Test-Path "IIS:\AppPools\$appPoolName" -PathType Container
}

function Test-AppPoolRunsAsUser([Microsoft.IIs.PowerShell.Framework.ConfigurationElement]$appPool, [string]$userName){
    if($null -ne $appPool -and $null -ne $appPool.processModel){
        return $userName -ieq $appPool.processModel.userName
    }
    return $false;
}

function New-Site($appName, $portNumber, $appDirectory, $hostHeader){
    cd IIS:\Sites

    if(!(Test-Path $appName -PathType Container))
    {
        Write-Console "WebSite $appName does not exist...creating."
        $webSite = New-Website -Name $appName -Port $portNumber -Ssl -PhysicalPath $appDirectory -ApplicationPool $appName -HostHeader $hostHeader
    
        Write-Console "Assigning certificate..."
        $cert = Get-Item Cert:\LocalMachine\My\$sslCertificateThumbprint
        Set-Location IIS:\SslBindings
        $sslBinding = "0.0.0.0!$portNumber"
        if(!(Test-Path $sslBinding)){
            $cert | New-Item $sslBinding
        }
    }
}

function New-App($appName, $siteName, $appDirectory){
    Set-Location IIS:\
    Write-Console "Creating web application: $webApp"
    New-WebApplication -Name $appName -Site $siteName -PhysicalPath $appDirectory -ApplicationPool $appName -Force
}

function Publish-WebSite($zipPackage, $appDirectory, $appName, $overwriteWebConfig){
    # Extract the app into the app directory
    Write-Console "Extracting $zipPackage to $appDirectory."

    try{
        Stop-WebAppPool -Name $appName
        Invoke-WaitForWebAppPoolToChangeState -name $appName -state "Stopped"
    }catch [System.InvalidOperationException]{
        Write-Console "AppPool $appName is already stopped, continuing."
    }

    Start-Sleep -Seconds 3
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zipPackage)
    foreach($item in $archive.Entries)
    {
        $itemTargetFilePath = [System.IO.Path]::Combine($appDirectory, $item.FullName)
        $itemDirectory = [System.IO.Path]::GetDirectoryName($itemTargetFilePath)
        $overwrite = $true

        if(!(Test-Path $itemDirectory)){
            New-Item -ItemType Directory -Path $itemDirectory | Out-Null
        }

        if(!(Test-IsDirectory $itemTargetFilePath)){
            try{
                Write-Console "......Extracting $itemTargetFilePath..."
                [System.IO.Compression.ZipFileExtensions]::ExtractToFile($item, $itemTargetFilePath, $overwrite)
            }catch [System.Management.Automation.MethodInvocationException]{
                Write-Console "......$itemTargetFilePath exists, not overwriting..."
                $errorId = $_.FullyQualifiedErrorId
                if($errorId -ne "IOException"){
                    throw $_.Exception
                }
            }
        }
    }
    $archive.Dispose()
    Start-WebAppPool -Name $appName
    Invoke-WaitForWebAppPoolToChangeState -name $appName -state "Started"
}

function Test-IsDirectory($path)
{
    if((Test-Path $path) -and (Get-Item $path) -is [System.IO.DirectoryInfo]){
        return $true
    }
    return $false
}

function Set-EnvironmentVariables($appDirectory, $environmentVariables){
    Write-Console "Writing environment variables to config..."
    $webConfig = [xml](Get-Content $appDirectory\web.config)
    foreach ($variable in $environmentVariables.GetEnumerator()){
        Add-EnvironmentVariable $variable.Name $variable.Value $webConfig
    }

    $webConfig.Save("$appDirectory\web.config")
}

function Get-EncryptedString($signingCert, $stringToEncrypt){
    $encryptedString = [System.Convert]::ToBase64String($signingCert.PublicKey.Key.Encrypt([System.Text.Encoding]::UTF8.GetBytes($stringToEncrypt), $true))
    return "!!enc!!:" + $encryptedString
}

function Get-InstalledApps
{
    if ([IntPtr]::Size -eq 4) {
        $regpath = 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
    }
    else {
        $regpath = @(
            'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
            'HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
        )
    }
    Get-ItemProperty $regpath | .{process{if($_.DisplayName -and $_.UninstallString) { $_ } }} | Select DisplayName, Publisher, InstallDate, DisplayVersion, UninstallString |Sort DisplayName
}

function Test-Prerequisite($appName, $minVersion)
{
    $installedAppResults = Get-InstalledApps | where {$_.DisplayName -like $appName}
    if($installedAppResults -eq $null){
        return $false;
    }

    if($minVersion -eq $null)
    {
        return $true;
    }

    $minVersionAsSystemVersion = [System.Version]$minVersion
    Foreach($version in $installedAppResults)
    {
        $installedVersion = [System.Version]$version.DisplayVersion
        if($installedVersion -ge $minVersionAsSystemVersion)
        {
            return $true;
        }
    }
}


function Test-PrerequisiteExact($appName, $supportedVersion)
{
    $installedAppResults = Get-InstalledApps | where {$_.DisplayName -like $appName}
    if($installedAppResults -eq $null){
        return $false;
    }

    if($supportedVersion -eq $null)
    {
        return $true;
    }

    $supportedVersionAsSystemVersion = [System.Version]$supportedVersion

    Foreach($version in $installedAppResults)
    {
        $installedVersion = [System.Version]$version.DisplayVersion
        if($installedVersion -eq $supportedVersionAsSystemVersion)
        {
            return $true;
        }
    }
}

function Get-CouchDbRemoteInstallationStatus($couchDbServer, $minVersion)
{
    try
    {
        $couchVersionResponse = Invoke-RestMethod -Method Get -Uri $couchDbServer 
    } catch {
        Write-Console "CouchDB not found on $couchDbServer"
    }

    if($couchVersionResponse)
    {
        $installedVersion = [System.Version]$couchVersionResponse.version
        $minVersionAsSystemVersion = [System.Version]$minVersion
        Write-Console "Found CouchDB version $installedVersion installed on $couchDbServer"
        if($installedVersion -ge $minVersionAsSystemVersion)
        {
            return "Installed"
        }else {
            return "MinVersionNotMet"
        }
    }
    return "NotInstalled"
}

function Get-AccessToken($authUrl, $clientId, $scope, $secret)
{
    $url = "$authUrl/connect/token"
    $body = @{
        client_id = "$clientId"
        grant_type = "client_credentials"
        scope = "$scope"
        client_secret = "$secret"
    }
    $accessTokenResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body
    return $accessTokenResponse.access_token
}

function Add-ApiRegistration($authUrl, $body, $accessToken)
{
    $url = "$authUrl/api/apiresource"
    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    try{
        $registrationResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
        return $registrationResponse.apiSecret
    }catch{
        $exception = $_.Exception
        $apiResourceObject = ConvertFrom-Json -InputObject $body
        if ($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409) {
            Write-Success "API Resource $($apiResourceObject.name) is already registered...updating registration settings."
            Write-Host ""
            try{
                Invoke-RestMethod -Method Put -Uri "$url/$($apiResourceObject.name)" -Body $body -ContentType "application/json" -Headers $headers

                # Reset api secret
                $apiResponse = Invoke-RestMethod -Method Post -Uri "$url/$($apiResourceObject.name)/resetPassword" -ContentType "application/json" -Headers $headers
                return $apiResponse.apiSecret
            }catch{
                $exception = $_.Exception
                $error = Get-ErrorFromResponse -response $exception.Response
                Write-Error "There was an error updating API resource $($apiResourceObject.name): $error. Halting installation."
                throw $exception
            }
        }
        else {
            $error = "Unknown error."
            $exception = $_.Exception
            if($exception -ne $null -and $exception.Response -ne $null){
                $error = Get-ErrorFromResponse -response $exception.Response
            }
            Write-Error "There was an error registering API $($apiResourceObject.name) with Fabric.Identity: $error, halting installation."
            throw $exception
        }
    }
}

function Add-ClientRegistration($authUrl, $body, $accessToken, $shouldResetSecret = $true)
{
    $url = "$authUrl/api/client"
    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }
    
    # attempt to add
    try{
        $registrationResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
        return $registrationResponse.clientSecret
    }catch{
        $exception = $_.Exception
        $clientObject = ConvertFrom-Json -InputObject $body
        if ($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409) {
            Write-Success "Client $($clientObject.clientName) is already registered...updating registration settings."
            Write-Host ""
            try{                
                Invoke-RestMethod -Method Put -Uri "$url/$($clientObject.clientId)" -Body $body -ContentType "application/json" -Headers $headers

                # Reset client secret
                if($shouldResetSecret) {
                    $apiResponse = Invoke-RestMethod -Method Post -Uri "$url/$($clientObject.clientId)/resetPassword" -ContentType "application/json" -Headers $headers
                    return $apiResponse.clientSecret
                }
                
                return "";
            }catch{
                $exception = $_.Exception
                $error = Get-ErrorFromResponse -response $exception.Response
                Write-Error "There was an error updating Client $($clientObject.clientName): $error. Halting installation."
                throw $exception
            }
        }
        else {
            $error = "Unknown error."
            $exception = $_.Exception
            if($exception -ne $null -and $exception.Response -ne $null){
                $error = Get-ErrorFromResponse -response $exception.Response
            }
            Write-Error "There was an error registering client $($clientObject.clientName) with Fabric.Identity: $error, halting installation."
            throw $exception
        }
    }
}

function Get-CurrentScriptDirectory()
{
    return Split-Path $script:MyInvocation.MyCommand.Path
}

function Get-InstallationSettings
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [ValidateScript({
            if (!(Test-Path $_)) {
                throw "Path $_ does not exist. Please enter valid path to the install.config."
            }
            if (!(Test-Path $_ -PathType Leaf)) {
                throw "Path $_ is not a file. Please enter a valid path to the install.config."
            }
            return $true
        })]  
        [string] $installConfigPath = "install.config"
    )

    $installationConfig = [xml](Get-Content $installConfigPath)
    $sectionSettings = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq $configSection}
    $installationSettings = @{}

    foreach($variable in $sectionSettings.variable){
        if($variable.name -and $variable.value){
            $installationSettings.Add($variable.name, $variable.value)
        }
    }

    $commonSettings = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq "common"}
    foreach($variable in $commonSettings.variable){
        if($variable.name -and $variable.value -and !$installationSettings.Contains($variable.name)){
            $installationSettings.Add($variable.name, $variable.value)
        }
    }

    try{
        $encryptionCertificateThumbprint = $installationSettings.encryptionCertificateThumbprint
        $encryptionCertificate = Get-EncryptionCertificate $encryptionCertificateThumbprint
    }catch{
        Write-Error "Could not get encryption certificte with thumbprint $encryptionCertificateThumbprint. Please verify that the encryptionCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store."
        throw $_.Exception
    }

    $installationSettingsDecrypted = @{}
    foreach($key in $installationSettings.Keys){
        $value = $installationSettings[$key]
        if($value.StartsWith("!!enc!!:"))
        {
            $value = Get-DecryptedString $encryptionCertificate $value
        }
        $installationSettingsDecrypted.Add($key, $value)
    }

    return $installationSettingsDecrypted
}

function Add-InstallationSetting
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [Parameter(Mandatory=$true)]
        [string] $configSetting,
        [Parameter(Mandatory=$true)]
        [string] $configValue,
        [ValidateScript({
            if (!(Test-Path $_)) {
                throw "Path $_ does not exist. Please enter valid path to the install.config."
            }
            if (!(Test-Path $_ -PathType Leaf)) {
                throw "Path $_ is not a file. Please enter a valid path to the install.config."
            }
            return $true
        })]  
        [string] $installConfigPath = "$(Get-CurrentScriptDirectory)\install.config"
    )

    $installationConfig = [xml](Get-Content $installConfigPath)
    $sectionSettings = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq $configSection}
    $existingSetting = $sectionSettings.variable | Where-Object {$_.name -eq $configSetting}
    if($null -eq $existingSetting){
        $setting = $installationConfig.CreateElement("variable")
        
        $nameAttribute = $installationConfig.CreateAttribute("name")
        $nameAttribute.Value = $configSetting
        $setting.Attributes.Append($nameAttribute)

        $valueAttribute = $installationConfig.CreateAttribute("value")
        $valueAttribute.Value = $configValue
        $setting.Attributes.Append($valueAttribute)

        $sectionSettings.AppendChild($setting)
    }else{
        $existingSetting.value = $configValue
    }
    $installationConfig.Save("$installConfigPath")
}

function Add-SecureInstallationSetting
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [Parameter(Mandatory=$true)]
        [string] $configSetting,
        [Parameter(Mandatory=$true)]
        [string] $configValue,
        [Parameter(Mandatory=$true)]
        $encryptionCertificate,
        [ValidateScript({
            if (!(Test-Path $_)) {
                throw "Path $_ does not exist. Please enter valid path to the install.config."
            }
            if (!(Test-Path $_ -PathType Leaf)) {
                throw "Path $_ is not a file. Please enter a valid path to the install.config."
            }
            return $true
        })]  
        [string] $installConfigPath = "$(Get-CurrentScriptDirectory)\install.config"
    )

    $encryptedConfigValue = Get-EncryptedString $encryptionCertificate $configValue
    Add-InstallationSetting $configSection $configSetting $encryptedConfigValue $installConfigPath
}

function Get-EncryptionCertificate($encryptionCertificateThumbprint)
{
    return Get-Certificate $encryptionCertificateThumbprint
}

function Get-Certificate($certificateThumbprint)
{
    $certificateThumbprint = $certificateThumbprint -replace '[^a-zA-Z0-9]', ''
    return Get-Item Cert:\LocalMachine\My\$certificateThumbprint -ErrorAction Stop
}

function Get-DecryptedString($encryptionCertificate, $encryptedString){
    if($encryptedString.StartsWith("!!enc!!:")){
        $cleanedEncryptedString = $encryptedString.Replace("!!enc!!:","")
        $clearTextValue = [System.Text.Encoding]::UTF8.GetString($encryptionCertificate.PrivateKey.Decrypt([System.Convert]::FromBase64String($cleanedEncryptedString), $true))
        return $clearTextValue
    }else{
        return $encryptedString
    }
}

function Get-CertsFromLocation($certLocation){
    $currentLocation = Get-Location
    Set-Location $certLocation
    $certs = Get-ChildItem
    Set-Location $currentLocation
    return $certs
}

function Get-CertThumbprint($certs, $selectionNumber){
    $selectedCert = $certs[$selectionNumber-1]
    $certThumbrint = $selectedCert.Thumbprint
    return $certThumbrint
}

function Test-IsRunAsAdministrator()
{
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Add-ServiceUserToDiscovery($userName, $connString)
{

    $query = "DECLARE @IdentityID int;
                DECLARE @DiscoveryServiceUserRoleID int;

                SELECT @IdentityID = IdentityID FROM CatalystAdmin.IdentityBASE WHERE IdentityNM = @userName;
                IF (@IdentityID IS NULL)
                BEGIN
                    print ''-- Adding Identity'';
                    INSERT INTO CatalystAdmin.IdentityBASE (IdentityNM) VALUES (@userName);
                    SELECT @IdentityID = SCOPE_IDENTITY();
                END

                SELECT @DiscoveryServiceUserRoleID = RoleID FROM CatalystAdmin.RoleBASE WHERE RoleNM = 'DiscoveryServiceUser';
                IF (NOT EXISTS (SELECT 1 FROM CatalystAdmin.IdentityRoleBASE WHERE IdentityID = @IdentityID AND RoleID = @DiscoveryServiceUserRoleID))
                BEGIN
                    print ''-- Assigning Discovery Service user'';
                    INSERT INTO CatalystAdmin.IdentityRoleBASE (IdentityID, RoleID) VALUES (@IdentityID, @DiscoveryServiceUserRoleID);
                END"
    Invoke-Sql $connString $query @{userName=$userName} | Out-Null
}

function Read-FabricInstallerSecret($defaultSecret)
{
    $fabricInstallerSecret = $defaultSecret
    $userEnteredFabricInstallerSecret = Read-Host  "Enter the Fabric Installer Secret or hit enter to accept the default [$defaultSecret]"
    Write-Host ""
    if(![string]::IsNullOrEmpty($userEnteredFabricInstallerSecret)){   
         $fabricInstallerSecret = $userEnteredFabricInstallerSecret
    }

    return $fabricInstallerSecret
}

function Invoke-ResetFabricInstallerSecret([Parameter(Mandatory=$true)] [string] $identityDbConnectionString){
    $fabricInstallerSecret = [System.Convert]::ToBase64String([guid]::NewGuid().ToByteArray()).Substring(0,16)
    Write-Host "New Installer secret: $fabricInstallerSecret"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $hashedSecret = [System.Convert]::ToBase64String($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($fabricInstallerSecret)))
    $query = "DECLARE @ClientID int;
              
              SELECT @ClientID = Id FROM Clients WHERE ClientId = 'fabric-installer';

              UPDATE ClientSecrets
              SET Value = @value
              WHERE ClientId = @ClientID"
    Invoke-Sql -connectionString $identityDbConnectionString -sql $query -parameters @{value=$hashedSecret} | Out-Null
    return $fabricInstallerSecret
}

function Get-ErrorFromResponse($response) {
    $result = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($result)
    $reader.BaseStream.Position = 0
    $reader.DiscardBufferedData()
    $responseBody = $reader.ReadToEnd();
    return $responseBody
}

function Invoke-Sql($connectionString, $sql, $parameters=@{}){    
    $connection = New-Object System.Data.SqlClient.SQLConnection($connectionString)
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    
    try {
        foreach($p in $parameters.Keys){		
          $command.Parameters.AddWithValue("@$p",$parameters[$p])
         }

        $connection.Open()    
        $command.ExecuteNonQuery()
        $connection.Close()        
    }catch [System.Data.SqlClient.SqlException] {
        Write-Error "An error ocurred while executing the command. Please ensure the connection string is correct and the identity database has been setup. Connection String: $($connectionString). Error $($_.Exception.Message)"  -ErrorAction Stop
    }    
}

function Write-Success($message){
    Write-Host $message -ForegroundColor Green
}

function Write-Console($message){
    Write-Host $message -ForegroundColor Gray
}

function Test-DiscoveryHasBuildVersion($discoveryUrl, $credential) {
    $response = [xml](Invoke-RestMethod -Method Get -Uri "$discoveryUrl/`$metadata" -Credential $credential -ContentType "application/xml")
    
    return $response.Edmx.DataServices.Schema.EntityType.Property.Name -contains 'BuildNumber'
}

function Add-DiscoveryRegistration($discoveryUrl, $credential, $discoveryPostBody) {
    $registrationBody = @{
        ServiceName   = $discoveryPostBody.serviceName
        Version       = $discoveryPostBody.serviceVersion
        ServiceUrl    = $discoveryPostBody.serviceUrl
        DiscoveryType = "Service"
        IsHidden      = $true
        FriendlyName  = $discoveryPostBody.friendlyName
        Description   = $discoveryPostBody.description
    }

    $hasVersion = Test-DiscoveryHasBuildVersion $discoveryUrl $credential
    if($hasVersion) {
        $registrationBody.BuildNumber = $discoveryPostBody.buildVersion
    }

    $url = "$discoveryUrl/Services"
    $jsonBody = $registrationBody | ConvertTo-Json
    try{
        Invoke-RestMethod -Method Post -Uri "$url" -Body "$jsonBody" -ContentType "application/json" -Credential $credential | Out-Null
        Write-Success "$($discoveryPostBody.friendlyName) successfully registered with DiscoveryService."
    }catch{
        $exception = $_.Exception
        Write-Error "Unable to register $discoveryPostBody.friendlyName with DiscoveryService. Ensure that DiscoveryService is running at $discoveryUrl, that Windows Authentication is enabled for DiscoveryService and Anonymous Authentication is disabled for DiscoveryService. Error $($_.Exception.Message) Halting installation."
        if($exception.Response -ne $null){
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-Error "    There was an error updating the resource: $error."
        }
        throw
    }
}

function Add-DiscoveryRegistrationSql($discoveryPostBody, $connectionString){
    if($null -eq $discoveryPostBody.isHidden){
        $discoveryPostBody.isHidden = $true
    }

    if($null -eq $discoveryPostBody.iconTxt){
        $discoveryPostBody.iconTxt = "data:image/png;base64,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"
    }
    $query = "DECLARE @InsertBuildNumberTXT nvarchar(100) = ''
    DECLARE @InsertListBuildNumberTXT nvarchar(50) = ''
    DECLARE @UpdateBuildNumberTXT nvarchar(100) = ''
    DECLARE @InsertOrUpdateService varchar(max) = ''
    
    IF EXISTS (SELECT * FROM sys.columns WHERE name = 'BuildNumberTXT' AND OBJECT_ID = OBJECT_ID('CatalystAdmin.DiscoveryServiceBASE'))
    BEGIN
      SET @InsertBuildNumberTXT = ',''' + @BuildNumberTXT + '''';
      SET @InsertListBuildNumberTXT = ',[BuildNumberTXT]';
      SET @UpdateBuildNumberTXT = ',BuildNumberTXT = ''' + @BuildNumberTXT + '''';
    END
    
    IF EXISTS
    (
        SELECT *
        FROM [CatalystAdmin].[DiscoveryServiceBASE]
        WHERE ServiceNM = @ServiceNM  AND ServiceVersion = @ServiceVersion
    )
    BEGIN
        SET @InsertOrUpdateService = '
           UPDATE [CatalystAdmin].[DiscoveryServiceBASE] 
           SET [ServiceUrl] = ''' + @ServiceUrl + ''',
               DiscoveryTypeCD = ''' + @DiscoveryTypeCD + ''', 
               HiddenFLG = ' + CAST(@HiddenFLG AS NVARCHAR(1)) + ', 
               FriendlyNM = ''' + @FriendlyNM + ''', 
               IconTXT = ''' + @IconTXT + ''', 
               DescriptionTXT = ''' + @DescriptionTXT + '''' +
               @UpdateBuildNumberTXT + '
           WHERE ServiceNM = ''' + @ServiceNM + ''' AND ServiceVersion =  ' + CAST(@ServiceVersion AS NVARCHAR(1)) + ';'
    END
    ELSE
    BEGIN
        SET @InsertOrUpdateService = '
        INSERT INTO [CatalystAdmin].[DiscoveryServiceBASE]
        ([ServiceNM],
        [ServiceUrl],
        [ServiceVersion],
        [DiscoveryTypeCD],
        [HiddenFLG],
        [FriendlyNM],
        [IconTXT],
        [DescriptionTXT]'
        + @InsertListBuildNumberTXT + '
        )
        VALUES
        (''' 
         + @ServiceNM + ''','''
         + @ServiceUrl + ''','
         + CAST(@ServiceVersion AS NVARCHAR(20)) + ','''
         + @DiscoveryTypeCD + ''',' 
         + CAST(@HiddenFLG AS NVARCHAR(1)) + ','''
         + @FriendlyNM + ''',''' 
         + @IconTXT + ''','''
         + @DescriptionTXT + ''''
         + @InsertBuildNumberTXT + '
        );'
    END
    
    EXEC(@InsertOrUpdateService);"

    Invoke-Sql -connectionString $connectionString -sql $query `
        -parameters @{ServiceNM=$($discoveryPostBody.serviceName);`
                      ServiceVersion=$($discoveryPostBody.serviceVersion);`
                      ServiceUrl=$($discoveryPostBody.serviceUrl);`
                      DiscoveryTypeCD=$($discoveryPostBody.discoveryType);`
                      FriendlyNM=$($discoveryPostBody.friendlyName);`
                      DescriptionTXT=$($discoveryPostBody.description);`
                      BuildNumberTXT=$($discoveryPostBody.buildVersion);`
                      HiddenFLG=$($discoveryPostBody.isHidden);`
                      IconTXT=$($discoveryPostBody.iconTxt)}
}

function RepairAclCanonicalOrder($Acl) {
    if ($Acl.AreAccessRulesCanonical) {
        return
    }

    # Convert ACL to a raw security descriptor:
    $RawSD = New-Object System.Security.AccessControl.RawSecurityDescriptor($Acl.Sddl)

    # Create a new, empty DACL
    $NewDacl = New-Object System.Security.AccessControl.RawAcl(
        [System.Security.AccessControl.RawAcl]::AclRevision,
        $RawSD.DiscretionaryAcl.Count  # Capacity of ACL
    )

    # Put in reverse canonical order and insert each ACE (I originally had a different method that
    # preserved the order as much as it could, but that order isn't preserved later when we put this
    # back into a DirectorySecurity object, so I went with this shorter command)
    $RawSD.DiscretionaryAcl | Sort-Object @{E={$_.IsInherited}; Descending=$true}, AceQualifier | ForEach-Object {
        $NewDacl.InsertAce(0, $_)
    }

    # Replace the DACL with the re-ordered one
    $RawSD.DiscretionaryAcl = $NewDacl

    # Commit those changes back to the original SD object (but not to disk yet):
    $Acl.SetSecurityDescriptorSddlForm($RawSD.GetSddlForm("Access"))

    # Commit changes
    $Acl | Set-Acl
}

Export-ModuleMember -function Add-EnvironmentVariable
Export-ModuleMember -function New-AppRoot
Export-ModuleMember -function New-AppPool
Export-ModuleMember -function New-Site
Export-ModuleMember -function New-App
Export-ModuleMember -function Publish-WebSite
Export-ModuleMember -function Set-EnvironmentVariables
Export-ModuleMember -function Get-EncryptedString
Export-ModuleMember -function Test-Prerequisite
Export-ModuleMember -function Test-PrerequisiteExact
Export-ModuleMember -function Get-CouchDbRemoteInstallationStatus
Export-ModuleMember -function Get-AccessToken
Export-ModuleMember -function Add-ApiRegistration
Export-ModuleMember -function Add-ClientRegistration
Export-ModuleMember -function Get-CurrentScriptDirectory
Export-ModuleMember -function Get-InstallationSettings
Export-ModuleMember -function Add-InstallationSetting
Export-ModuleMember -function Add-SecureInstallationSetting
Export-ModuleMember -function Get-EncryptionCertificate
Export-ModuleMember -function Get-DecryptedString
Export-ModuleMember -Function Get-Certificate
Export-ModuleMember -Function Get-CertsFromLocation
Export-ModuleMember -Function Get-CertThumbprint
Export-ModuleMember -Function Write-Success
Export-ModuleMember -Function Write-Console
Export-ModuleMember -Function Test-IsRunAsAdministrator
Export-ModuleMember -Function Add-ServiceUserToDiscovery
Export-ModuleMember -Function Invoke-Sql
Export-ModuleMember -Function Read-FabricInstallerSecret
Export-ModuleMember -Function Get-ErrorFromResponse
Export-ModuleMember -Function Invoke-ResetFabricInstallerSecret
Export-ModuleMember -Function Add-DiscoveryRegistration
Export-ModuleMember -Function Test-AppPoolExistsAndRunsAsUser
Export-ModuleMember -Function Add-DiscoveryRegistrationSql
