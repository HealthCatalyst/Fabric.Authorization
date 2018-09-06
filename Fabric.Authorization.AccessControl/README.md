# Fabric.Authorization.AccessControl
An Angular 5 routed module for managing access in Fabric.Authorization

# Installing
Run `npm i --save @healthcatalyst/fabric-access-control-ui`

This module requires [Health Catalyst Cashmere](http://cashmere.healthcatalyst.net/guides/getting-started).

# Fabric Identity Setup
You will need to set up a client in your local instance of Fabric.Identity and Fabric.Authorization. Assuming your Fabric.Identity instance is running at `http://localhost/identity`, you can run the following `curl` command to create a Fabric.Identity client:

`curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer {access_token}" -d @{identity_client.json} http://localhost/identity/api/client`

where `{access_token}` is the JWT obtained per the instructions at [Retrieving an Access Token from Fabric.Identity](https://github.com/HealthCatalyst/Fabric.Identity/wiki/Retrieving-an-Access-Token-from-Fabric.Identity)

and `{identity_client.json}` is a file containing the following payload:

```
{
    "clientId": "fabric-access-control",
    "clientName": "Fabric Access Control Sample UI",
    "allowedScopes": [
        "openid",
        "profile",
        "fabric.profile",
        "fabric/authorization.read",
        "fabric/authorization.write",
        "fabric/idprovider.searchusers",
        "fabric/authorization.dos.write"
    ],
    "allowedGrantTypes": [
        "implicit"
    ],
    "allowedCorsOrigins": [
        "http://localhost:4200"
    ],
    "redirectUris": [
        "http://localhost:4200/oidc-callback.html",
        "http://localhost:4200/silent.html"
    ],
    "postLogoutRedirectUris": [
        "http://localhost:4200"
    ],
    "allowOfflineAccess": false,
    "requireConsent": false,
    "allowAccessTokensViaBrowser": true,
    "enableLocalLogin": false,
    "accessTokenLifetime": 1200
}
```

## IIS Configuration
If you want to set up an IIS site to host the sample application, you will need to create an application pool and set the `.NET CLR version` parameter to `No Managed Code`.

## Environment Settings (environment.default.ts)
You may need to change your `environment.default.ts` file if your services are not running at the locations specified in that file.

`fabricAuthApiUri` => Fabric.Authorization  
`fabricIdentityApiUri` => Fabric.Identity  
`fabricExternalIdPSearchApiUri` => Fabric.IdentityProviderSearchService

# Configuration
The access control module is lazy loaded and should be imported into a module that will assist with loading it through the router and with providing the configuration data. A class that implements IAccessControlConfigService should be created and registered as a provider in this module. 

_You should not import the access control module directly in your root module_

Here is an example of a helper class and an implementation of the configuration class that will be used by the access control module. Notice that you can inject things via the constructor into the configuration class to take advantage of the host applications configuration service.

```
import { NgModule, Injectable } from '@angular/core';
import { AccessControlModule, IAccessControlConfigService } from '@healthcatalyst/fabric-access-control-ui';
import { IDataChangedEventArgs } from '@healthcatalyst/fabric-access-control-ui/src/app/models/changedDataEventArgs.model';
import { Exception } from '@healthcatalyst/fabric-access-control-ui/src/app/models/exception.model';
import { Subject } from 'rxjs/Subject';
import { Config } from './app.module';

@Injectable()
class AccessControlConfigService implements IAccessControlConfigService {
    dataChanged: Subject<IDataChangedEventArgs> = new Subject<IDataChangedEventArgs>();
    errorRaised: Subject<Exception> = new Subject<Exception>();

    clientId = 'atlas';
    identityProvider = 'windows';
    grain = 'dos';
    securableItem = 'datamarts';
    fabricAuthApiUrl = this.config.AuthUrl;
    fabricExternalIdpSearchApiUrl = this.config.IdpSearchUrl;

    constructor(private config: Config) {
        this.dataChanged.subscribe((eventArgs: IDataChangedEventArgs) => {          
            console.log(`Data changed: ${JSON.stringify(eventArgs)}`);
        });

        this.errorRaised.subscribe((eventArgs: Exception) => {            
            console.log(`Error: ${JSON.stringify(eventArgs)}`);
        });
    }
}

@NgModule({
  imports: [
    AccessControlModule
  ],
  providers: [
        {provide: 'IAccessControlConfigService', useClass: AccessControlConfig}
  ]
})
export class AccessControlLazyLoader { }
```

You can then wire the access control module up via the router using the lazy loader module.

_The loadChildren value must point to the location of the lazy loader module_

```
 { path: 'access-control',  loadChildren: './access-control-lazy-loader#AccessControlLazyLoader' }
```
Ensure your root module imports HttpClientModule
```
import { HttpClientModule } from '@angular/common/http';
```
# Http Interceptors
The access control module needs to be installed into an application that can get an access token with scopes to read and write to the Fabric.Authorization API. This access token needs to be provided in each http request to Fabric.Authorization. We require Http Interceptors for this. Here is an example from a sample application of an http interceptor that will add the required headers.

```
import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent
} from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/fromPromise';
import 'rxjs/add/operator/mergeMap';

import { AuthService } from '../services/auth.service'; 

@Injectable()
export class FabricHttpRequestInterceptorService implements HttpInterceptor {
  protected static readonly AcceptHeader = 'application/json';
  protected static readonly ContentTypeHeader = 'application/json';
  protected static AuthorizationHeader = `Bearer`;

  constructor(private authService: AuthService) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const tokenObservable = Observable.fromPromise(
      this.authService.getUser()
        .then((user) => {
          if(user){
            return user.access_token;
          }
        })
    );
    
    return tokenObservable.mergeMap(accessToken => {
      const modifiedRequest = req.clone({
        setHeaders: {
          Authorization: `${
            FabricHttpRequestInterceptorService.AuthorizationHeader
          } ${accessToken}`,
          Accept: FabricHttpRequestInterceptorService.AcceptHeader,
          'Content-Type': FabricHttpRequestInterceptorService.ContentTypeHeader
        }
      });
      return next.handle(modifiedRequest);
    });
  }
}

```

# To run Access-Control UI from Fabric Authorization

1.  Next, install the latest software.  Like everything: Identity, Authorization, etc through the installer.

2.  First enable CORS in Discovery service if this is not enabled:

https://healthcatalyst.slack.com/archives/GACPU031A/p1534178899000358?thread_ts=1534178899.000358

or you can do the following:

To enable CORS via DiscoveryService when running the Fabric.AccessControl UI locally, you must do the following:

a. Update the AuthenticationMessageHandler line 33 to allow OPTIONS requests to not require authorization: `if (request.Method == HttpMethod.Get) || request.Method == HttpMethod.Options) (if you are running an installed version of discovery, you will have to build and copy the dll to your deployed location.)

b. Install URL Authorization through `Turn Windows features on or off` under IIS -> WWWServices -> Security

c. Add this section to the discovery service web.config in the system.webServer section:

``` 
    <security>
        <authentication>
            <anonymousAuthentication enabled="true" />
            <windowsAuthentication enabled="true" />
        </authentication>
        <authorization>
            <add accessType="Allow" users="*" verbs="OPTIONS" />
            <add accessType="Deny" users="?" verbs="GET,HEAD,PUT,POST,DELETE,PATCH,CONNECT,TRACE" />
        </authorization>
    </security>
```

3.  Change DiscoveryServiceBASE

DiscoveryServiceBASE you need to point these apps to your http://localhost:5004 environment:

a. AuthorizationService

b. AccessControl

4.  Change the appsettings.json to point to your Domain name, like 'https://HC2282.hqcatalyst.local'.  Do this for both discovery service and Identity.

Example:

a. "Value": "https://HC2282.hqcatalyst.local/DiscoveryService/v1"

b. "Authority": "https://HC2282.hqcatalyst.local/identity"

5.  Run Fabric.Authorization service.  Should be visible in http://localhost:5004

6.  In Fabric.Authorization.AccessControl project, there is a script. Run it in Git Bash with these settings:

a. ./setup-access-control.sh <YOUR Fabric Identity Installer secret> <YOUR fully qualified domain name of Fabric.Identity>

Example:

./setup-access-control.sh znB1Hr26j/ "https://HC2282.hqcatalyst.local/identity"

# NOTE:

If you have problems, check the Fabric.Identity and Authorization logs to see if you are missing permissions.
