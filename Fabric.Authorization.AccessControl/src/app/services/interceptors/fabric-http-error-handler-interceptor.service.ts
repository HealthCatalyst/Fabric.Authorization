import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/throw';
import 'rxjs/add/operator/catch';
import { Exception } from '../../models/exception.model';

@Injectable()
export class FabricHttpErrorHandlerInterceptorService {
  /*
    Pattern below found at https://angular.io/guide/http#error-handling.
  */
  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(req)
      .catch((response: HttpErrorResponse) => {
        if (response.error instanceof ErrorEvent) {
          // A client-side or network error occurred. Handle it accordingly.
          console.error('An error occurred:', response.error.message);
        } else {
          // The backend returned an unsuccessful response code.
          // The response body may contain clues as to what went wrong,
          console.error(
            `Backend returned code ${response.status}, body was: ${JSON.stringify(
              response.error
            )}`
          );
        }

        return Observable.throw(
          new Exception(response.status, JSON.parse(response.error).message || response.error)
        );
    });
  }
}
