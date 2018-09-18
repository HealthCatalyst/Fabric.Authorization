import { Injectable, Injector } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class StandardHeaderInterceptor implements HttpInterceptor {
    constructor(private injector: Injector) {}

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // IE requires Pragma, Expires, and If-Modified-Since headers to NOT cache results, so we need to add these headers on requests
        const genericReq: HttpRequest<any> = req.clone({
            headers: req.headers
                .set('Pragma', 'no-cache')
                .set('Expires', 'Sat, 01 Jan 2000 00:00:00 GMT')
                .set('If-Modified-Since', '0')
        });

        return next.handle(genericReq);
    }
}
