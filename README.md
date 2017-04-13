# Fabric.Identity

The Fabric.Identity service is planned to provide central authentication and authorization across the Fabric ecosystem. An overvie of our thinking can be found in [this presentation](https://healthcatalyst.box.com/s/alac73mlvo1ojm1jrnzm37zma282lc9b).

## Platform
The Fabric.Identity service is built using:

+ ASP .NET Core 1.1
+ [IdentityServer4](http://identityserver.io/)

## How to build and run
+ [Install .NET Core 1.1](https://www.microsoft.com/net/core#windowsvs2017)
+ Update your NugGet config to include the Fabric dev nuget feed
  + [Map a network drive for box](https://bbcrm.edusupportcenter.com/link/portal/8197/8382/Article/4747/Can-I-map-a-network-drive-to-my-Box-storage-Windows-PC-only)
  + Edit your NuGet.config to include `<add key="Fabric.Nuget" value="{your mapped drive}:\Fabric.Nuget\" />`
+ Clone or download the repo
+ Launch a command prompt or powershell window and change directory to the Fabric.Identity.API directory and execute the following commands
  + `dotnet restore`
  + `dotnet run`

Fabric.Identity service will start up and listen on port 5001.