# Fabric.Authorization.AccessControl
An Angular 6 application for managing access to the Health Catalyst Data Operating System (DOS)

# Building and running
The Access Control UI depends on components of DOS to run properly, as a result having the latest version of DOS installed is a prerequisite to running the Access Control UI.

If you would like to run the Access Control UI against a development version of Fabric.Authorization which is also in this repo you need to ensure that you have .NET Core 2.2 SDK installed. The instructions below assume that you have installed DOS and will run the Access Control UI against a version of Fabric.Authorization built from the source contained in this repo.

- Run the `setup-dependencies.ps1` PowerShell script as administrator to ensure you have the correct versions of node, npm, angular and typescript installed. If you have already run this or have chocolatey intalled, this script doesn't do anything.
- In Fabric.Authorization.API/appsettings.json, update the `IdentityServerConfidentialClientSettings.Authority` setting to point at your installed version of Fabric.Identity, e.g. `https://host.domain.local/identity`. If you are debugging Fabric.Identity, then the debug version, `http://localhost/IdentityDev`. Make sure to walk through the `Readme.md` for Fabric.Identity for the correct setup, then start debugging Fabric.Identity.API in IIS mode.

- In Fabric.Authorization.API/appsettings.json, update the `DiscoveryServiceSettings.Endpoint` setting to point at your installed version of DiscoveryService, e.g. `https://host.domain.local/DiscoveryService/v1`.

- In Fabric.Authorization.API/appsettings.json, update the `IdentityProviderSearchSettings.Endpoint` setting to point at your installed version of Fabric.IdentityProviderSearchService, e.g. `https://host.domain.local/IdentityProviderSearchService/v1`. If you are debugging Fabric.IdentityProviderSearchService, then the debug version, `http://localhost/IdPSSDev`. Make sure to walk through the `Readme.md` for Fabric.IdentityProviderSearchService for the correct setup, then start debugging Fabric.IdentityProviderSearchService in Local IIS mode.
- Update `IdentityServerConfidentialClientSettings.ClientSecret` to be `secret`
- The `IdentityServerConfidentialClientSettings.ClientID` shows `authorization-api`. That will be the name of the `ApiResource` that you will find the `Id` of in the `Identity` database `ApiResources` table. Now go to the `ApiSecrets` table and find the column with that `ApiResourceId`. Using the ApiResource Id, find the correct row and  `ApiSecrets.Value` to update. Uncomment and copy the following code, then run in powershell. Copy the returned secret value. Use a sql update script or right-click and edit the `ApiSecrets` table `Value` column, and paste in the secret value.
- Update the `Identity` database `ClientSecrets` table `Value` column. Look for `fabric-authorization-client` ClientId, in Clients table.
  Using the Client Id find the correct row and `ClientSecrets.Value` to update. Uncomment and copy the following code, then run in powershell. Copy the returned secret value. Use a sql update script or right-click and edit the `ClientSecrets` table and paste in the secret value.

```powershell
$fabricInstallerSecret = "secret"
#$fabricInstallerSecret = [System.Convert]::ToBase64String([guid]::NewGuid().ToByteArray()).Substring(0,16)
Write-Host "New Installer secret: $fabricInstallerSecret"
$sha = [System.Security.Cryptography.SHA256]::Create()
$hashedSecret = [System.Convert]::ToBase64String($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($fabricInstallerSecret)))
#Invoke-Sql -connectionString $identityDbConnectionString -sql $query -parameters @{value=$hashedSecret} | Out-Null
$fabricInstallerSecret
$hashedSecret
```

- In the `Identity` database `ClientCorsOrigins` table, there should be an `Origin` for `http://host.domain.local` that has a ClientId that coincides with `fabric-access-control` in the `Clients` table. For this same ClientId, add another row with this Origin: `http://localhost`. 
- For Client `fabric-access-control` update the following 
`Identity.ClientPostLogoutRedirectUris` table to `http://localhost/AuthorizationDev/client/logged-out` 
`Identity.ClientRedirectUris` table to `http://localhost/AuthorizationDev/client/oidc-callback.html` and `http://localhost/AuthorizationDev/client/silent.html`

- Launch PowerShell or gitbash or VS Code as administrator and in Fabric.Authorization.AccessControl folder execute `npm install` then `npm run watch` to build the angular application and watch for changes
- In VS 2017 ensure the startup project is `Fabric.Authorization.API` and the debug profile is set to `IIS`.
- In VS 2017 begin debugging by pressing `F5`
- In IIS, a new web application will be created `AuthorizationDev`.  You will need to change the app pool; I suggest changing it to the same one as Authorization.
In the new web application `AuthorizationDev` add the following `environmentVariable` to the web.config 
`<environmentVariable name="IdentityServerApiSettings__ApiSecret" value="secret" />`

- From time to time, the debugger set to `IIS` will remove the https binding.  If you notice things not working, check that out. Also, you will need to stop and start the `App Pools` for Identity and Authorization after changing settings in `Identity` database tables, to make sure previous values aren't cached.

This will allow you to set breakpoints in the Fabric.Authorization.API code via VS 2017. In addition you can debug the javascript via your chosen web browser using source maps. 

In `chrome` for example, to look at the source map files and set breakpoints in access control, `start dev tools`. Then go to Sources tab then `webpack://` and the following folder hierarchy `./src/app`. Find the folder and file with the code change and select. You should see your latest changes showing and are able to set breakpoints. While running `npm run watch` in `VS Code`, make a small change to the typescript, wait for it to update, then `refresh the browser` to make sure changes are getting updated. If they aren't updating, undo your changes, `terminate` the `npm run watch` and re-run, make your change, then `refresh the browser` again.

# Debugging Fabric.Identity, Fabric.Authorization and Fabric.IdentityProviderSearchService in Fabric.Authorization.AccessControl
After running the `Register-Identity-IdPSS.ps1` script and re-running the `Install-Identity.ps1` script, web.config values will be generated for AAD.
To make sure you have all the necessary settings you can `copy` the web.config appSettings from the Identity service installation in inetpub to the `IdentityDev` instance in the solution folder.
This can be done for the Authorization service installation and `AuthorizationDev` instance, and the Identity Provider Search service installation and `IdPSSDev` instance.
The `alternative` is to add these same settings to the `appsettings.json`. If these settings are in the web.config and appsettings.json, the `web.config will override the appsettings.json` file.
There were some `appsettings.json` file changes involving setting the ClientSecret to 'secret'. Make sure these are reflected in the `web.config` files if using the web.config over the appsettings.json.
The `AzureActiveDirectorySettings_ClientId and ClientSecret` should reflect the application clientid and clientsecret on `portal.azure.com` Azure Active Directory `App Registrations`.




