
import {throwError as observableThrowError,  Observable } from 'rxjs';

import {catchError} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';


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
    return next.handle(req).pipe(
      catchError((response: HttpErrorResponse) => {
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

        return observableThrowError(
          new Exception(response.status, response.error.message || JSON.stringify(response.error))
        );
    }));
  }
}
