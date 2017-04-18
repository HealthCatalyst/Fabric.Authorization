# Fabric.Identity

The Fabric.Identity service is planned to provide central authentication and authorization across the Fabric ecosystem. An overvie of our thinking can be found in [this presentation](https://healthcatalyst.box.com/s/alac73mlvo1ojm1jrnzm37zma282lc9b).

## Platform
The Fabric.Identity service is built using:

+ ASP .NET Core 1.1
+ [IdentityServer4](http://identityserver.io/)

## How to build and run
+ [Install .NET Core 1.1](https://www.microsoft.com/net/core#windowsvs2017)
+ Clone or download the repo
+ Launch a command prompt or powershell window and change directory to the Fabric.Identity.API directory and execute the following commands
  + `dotnet restore`
  + `dotnet run`

Fabric.Identity service will start up and listen on port 5001.

You can run the following curl commands to ensure the service is up and working properly:

```curl -G http://localhost:5001/.well-known/openid-configuration```

Which will return a json document representing the discovery information for the service:

```
{
  "issuer": "http://localhost:5001",
  "jwks_uri": "http://localhost:5001/.well-known/openid-configuration/jwks",
  "authorization_endpoint": "http://localhost:5001/connect/authorize",
  ...
}
```

You can then run the following curl to ensure the service is issuing access tokens properly:

```
curl http://localhost:5001/connect/token --data "client_id=fabric-sampleapi-client&client_secret=secret&grant_type=client_credentials&scope=patientapi"
```

You should get a response back that looks something like this:

```
{
	"access_token": "[base64 encoded string]",
	"expires_in": 3600,
	"token_type": "Bearer"
}
```
