# Fabric.Authorization.AccessControl
An Angular 6 application for managing access to the Health Catalyst Data Operating System (DOS)

# Building and running
The Access Control UI depends on components of DOS to run properly, as a result having the latest version of DOS installed is a prerequisite to running the Access Control UI.

If you would like to run the Access Control UI against a development version of Fabric.Authorization which is also in this repo you need to ensure that you have .NET Core 2.1 SDK installed. The instructions below assume that you have installed DOS and will run the Access Control UI against a version of Fabric.Authorization built from the source contained in this repo.

- Run the `setup-dependencies.ps1` PowerShell script as administrator to ensure you have the correct versions of node, npm, angular and typescript installed. If you have already run this or have chocolatey intalled, this script doesn't do anything.
- In Fabric.Authorization.API/appsettings.json, update the `IdentityServerConfidentialClientSettings.Authority` setting to point at your installed version of Fabric.Identity, e.g. `https://host.domain.local/identity`. If you are debugging Fabric.Identity, then the debug version, `http://localhost/IdentityDev`. Make sure to walk through the `Readme.md` for Fabric.Identity for the correct setup, then start debugging Fabric.Identity.API in IIS mode.

- In Fabric.Authorization.API/appsettings.json, update the `DiscoveryServiceSettings.Endpoint` setting to point at your installed version of DiscoveryService, e.g. `https://host.domain.local/DiscoveryService/v1`.

- In Fabric.Authorization.API/appsettings.json, update the `IdentityProviderSearchSettings.Endpoint` setting to point at your installed version of Fabric.IdentityProviderSearchService, e.g. `https://host.domain.local/IdentityProviderSearchService/v1`. If you are debugging Fabric.IdentityProviderSearchService, then the debug version, `http://localhost/IdPSSDev`. Make sure to walk through the `Readme.md` for Fabric.IdentityProviderSearchService for the correct setup, then start debugging Fabric.IdentityProviderSearchService in Local IIS mode.

- In the `Identity` database `ClientCorsOrigins` table, there should be an `Origin` for `http://host.domain.local` that has a ClientId that coincides with `fabric-access-control` in the `Clients` table, change to `http://localhost`.
- For Client `fabric-access-control` update the following 
`Identity.ClientPostLogoutRedirectUris` table to `http://localhost/AuthorizationDev/client/logged-out` 
`Identity.ClientRedirectUris` table to `http://localhost/AuthorizationDev/client/oidc-callback.html` and `http://localhost/AuthorizationDev/client/silent.html`

- Launch PowerShell or gitbash or VS Code as administrator and in Fabric.Authorization.AccessControl folder execute `npm install` then `npm run watch` to build the angular application and watch for changes
- In VS 2017 ensure the startup project is `Fabric.Authorization.API` and the debug profile is set to `IIS`.
- In VS 2017 begin debugging by pressing `F5`
- In IIS, a new web application will be created `AuthorizationDev`.  You will need to change the app pool; I suggest changing it to the same one as Authorization.

- From time to time, the debugger set to `IIS` will remove the https binding.  If you notice things not working, check that out. Also you will need to stop and start the `Identity app pool` after changing `Identity` database settings to make sure previous values aren't cached.

This will allow you to set breakpoints in the Fabric.Authorization.API code via VS 2017. In addition you can debug the javascript via your chosen web browser using source maps. 

In chrome for example, to look at the source files and set breakpoints in angular access control, start dev tools. Then go to Sources tab then webpack:// and then the following folder hierarchy ./src/app then find the folder and file with the code change and select. You should see your latest changes are showing and are able to set breakpoints.