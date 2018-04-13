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
  fabricAuthApiUrl: string
  fabricExternalIdpSearchApiUrl: string
}
```

This configuration object should be passed to the access control module in the forRoot function when it is loaded. The access control module is lazy loaded and should be imported into a module that will assist with loading it through the router and providing the configuration data. Here is an example:

```
import { NgModule } from "@angular/core";
import { AccessControlModule, IAccessControlConfigService } from '@healthcatalyst/fabric-access-control-ui';

// You can inject needed classes through the constructor
@Injectable()
class AccessControlConfig implements IAccessControlConfigService {
    dataChanged: Subject<IDataChangedEventArgs> = new Subject<IDataChangedEventArgs>();
    errorRaised: Subject<Exception> = new Subject<Exception>();

    constructor() {
        this.dataChanged.subscribe((eventArgs: IDataChangedEventArgs) => {
            // tslint:disable-next-line:no-console
            console.log(`Data changed: ${JSON.stringify(eventArgs)}`);
        });

        this.errorRaised.subscribe((eventArgs: Exception) => {
            // tslint:disable-next-line:no-console
            console.log(`Error: ${JSON.stringify(eventArgs)}`);
        });
    }

    clientId: string = 'atlas';
    identityProvider: string = 'windows';
    grain: string = 'dos';
    securableItem: string = 'datamarts';
    fabricAuthApiUrl = 'http://localhost/authorization/v1';
    fabricExternalIdpSearchApiUrl = 'http://localhost:5009/v1';
}

@NgModule({
  imports: [
    AccessControlModule.forRoot(AccessControlConfig)
  ]
})
export class AccessControlLazyLoader {}
```

You can then wire the access control module up via the router using the lazy loader module.

```
 { path: 'accesscontrol',  loadChildren: './access-control-lazy-loader#AccessControlLazyLoader' }
```
# Http Interceptors

The access control module needs to be installed into an application that can get an access token with scopes to read and write to the Fabric.Authorization API. This access token needs to be provided in each http request to Fabric.Authorization. We recommend Http Interceptors for this. Here is an example of an http interceptor taken from a sample application.

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