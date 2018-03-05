import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest } from '@angular/common/http';

@Injectable()
export abstract class FabricBaseInterceptorService {
  protected static readonly AcceptHeader = 'application/json';
  protected static readonly ContentTypeHeader = 'application/json';
  protected static readonly AuthorizationHeader = 'Bearer {token}';

  constructor() {}

  protected setHeaders(req: HttpRequest<any>) : HttpRequest<any> {
    req.headers.append('Authorization', FabricBaseInterceptorService.AuthorizationHeader.replace('{token}', ''));
    req.headers.append('Accept', FabricBaseInterceptorService.AcceptHeader);

    if (req.method === 'POST' || req.method === 'PUT' || req.method === 'PATCH') {
      req.headers.append('Content-Type', FabricBaseInterceptorService.ContentTypeHeader)
    }

    return req;
  }
}
