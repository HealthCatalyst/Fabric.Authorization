# Fabric.Authorization.AccessControl

An Angular 5 routed module for managing access in Fabric.Authorization

# Installing

Run `npm i @healthcatalyst/fabric-access-control-ui`

# Configuration

A configuration object should be provided to the module

```
{
  clientId: string;
  identityProvider: string;
  grain: string;
  securableItem: string;
  getFabricAuthApiUrl(): string
  getFabricExternalIdpSearchApiUrl(): string
}
```

This configuration object should be passed to the access control module in the forRoot function when it is loaded. The access control module is lazy loaded and should be imported into a module that will assist with loading it through the router and providing the configuration data. Here is an example:

```
import { NgModule } from "@angular/core";
import { AccessControlModule } from '@healthcatalyst/fabric-access-control-ui';

const accesscontrolConfig = {
  clientId: 'fabric-angularsample',
  identityProvider: 'windows',
  grain: 'dos',
  securableItem: 'datamarts',
  
  getFabricAuthApiUrl(): string {
    return 'http://localhost/authorization/v1';
  },
  getFabricExternalIdpSearchApiUrl(): string {
    return 'http://localhost:5009/v1';
  }
};

@NgModule({
  imports: [
    AccessControlModule.forRoot(accesscontrolConfig)
  ]
})
export class AccessControlLazyLoader {}
```

You can then wire the access control module up via the router.

```
 { path: 'accesscontrol',  loadChildren: './access-control-lazy-loader#AccessControlLazyLoader' }
```
# Http Interceptors

The access control module needs to be installed into an application that can get an access token with scopes to read and write to the Fabric.Authorization API. This access token needs to be provided in each http request to Fabric.Authorization. We recommend Http Interceptors for this. Here is an example of an http interceptor that will set the necessary headers for each http request. A service that can provide an access token should be injected via the constructor.

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