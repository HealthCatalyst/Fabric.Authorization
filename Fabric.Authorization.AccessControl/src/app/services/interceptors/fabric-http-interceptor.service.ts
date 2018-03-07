import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';

import { AccessControlConfigService } from '../access-control-config.service';

@Injectable()
export class FabricHttpInterceptorService implements HttpInterceptor {

  protected static readonly AcceptHeader = 'application/json';
  protected static readonly ContentTypeHeader = 'application/json';
  protected static AuthorizationHeader = `Bearer`;

  constructor(private configService: AccessControlConfigService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

    var tokenObservable = Observable.fromPromise(this.configService.getAccessToken());
    return tokenObservable.flatMap(accessToken => {
  
      const modifiedRequest = req.clone(
        {
          setHeaders: {
            'Authorization': `${FabricHttpInterceptorService.AuthorizationHeader} ${accessToken}`,
            'Accept': FabricHttpInterceptorService.AcceptHeader,
            'Content-Type': FabricHttpInterceptorService.ContentTypeHeader
          }
        }
      );
      return next.handle(modifiedRequest);
    });   
  }
}
