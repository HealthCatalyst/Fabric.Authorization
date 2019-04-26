# Fabric.Authorization.AccessControl
An Angular 6 application for managing access to the Health Catalyst Data Operating System (DOS)

# Building and running
The Access Control UI depends on components of DOS to run properly, as a result having the latest version of DOS installed is a prerequisite to running the Access Control UI.

If you would like to run the Access Control UI against a development version of Fabric.Authorization which is also in this repo you need to ensure that you have .NET Core 1.1 SDK installed. The instructions below assume that you have installed DOS and will run the Access Control UI against a version of Fabric.Authorization built from the source contained in this repo.

- Run the `setup-dependencies.ps1` PowerShell script as administrator to ensure you have the correct versions of node, npm, angular and typescript installed.
- In Fabric.Authorization.API/appsettings.json, update the `IdentityServerConfidentialClientSettings.Authority` setting to point at your installed version of Fabric.Identity, e.g. `https://host.domain.local/identity`
- Launch PowerShell or gitbash as administrator and in Fabric.Authorization.AccessControl folder execute `npm install' then `npm run watch` to build the angular application and watch for changes
- In VS 2017 ensure the startup project is `Fabric.Authorization.API` and the debug profile is set to `IIS`.
- In VS 2017 begin debugging by pressing `F5`
- In IIS, a new web application will be created `AuthorizationDev`.  You will need to change the app pool; I suggest changing it the the same one as Authorization.
- From time to time, the debugger set to `IIS` will remove the https binding.  If you notice things not working, check that out.

This will allow you to set breakpoints in the Fabric.Authorization.API code via VS 2017. In addition you can debug the javascript via your chosen web browser using source maps.
