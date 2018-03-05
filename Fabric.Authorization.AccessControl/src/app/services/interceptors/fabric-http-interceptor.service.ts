import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';

@Injectable()
export class FabricHttpInterceptorService implements HttpInterceptor {

  protected static readonly AccessTokenToken = '{token}';
  protected static readonly AcceptHeader = 'application/json';
  protected static readonly ContentTypeHeader = 'application/json';
  protected static readonly AuthorizationHeader = 'Bearer ' + FabricHttpInterceptorService.AccessTokenToken;

  constructor() {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const modifiedRequest = req.clone();
    modifiedRequest.headers.append('Authorization', FabricHttpInterceptorService.AuthorizationHeader.replace(FabricHttpInterceptorService.AccessTokenToken, ''));
    modifiedRequest.headers.append('Accept', FabricHttpInterceptorService.AcceptHeader);

    if (modifiedRequest.method === 'POST' || modifiedRequest.method === 'PUT' || modifiedRequest.method === 'PATCH') {
      modifiedRequest.headers.append('Content-Type', FabricHttpInterceptorService.ContentTypeHeader)
    }

    return next.handle(modifiedRequest);
  }
}
