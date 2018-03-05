import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';
import { FabricBaseInterceptorService } from './fabric-base-interceptor.service';

@Injectable()
export class FabricExternalIdpSearchInterceptorService extends FabricBaseInterceptorService implements HttpInterceptor {

  constructor() {
    super();
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const modifiedRequest = req.clone();
    return next.handle(super.setHeaders(modifiedRequest));
  }
}
