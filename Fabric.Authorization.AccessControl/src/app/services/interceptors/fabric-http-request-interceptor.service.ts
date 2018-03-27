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

import { AccessControlConfigService } from '../../services';

@Injectable()
export class FabricHttpRequestInterceptorService implements HttpInterceptor {
  protected static readonly AcceptHeader = 'application/json';
  protected static readonly ContentTypeHeader = 'application/json';
  protected static AuthorizationHeader = `Bearer`;

  constructor(private configService: AccessControlConfigService) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const tokenObservable = Observable.fromPromise(
      this.configService.getAccessToken()
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
