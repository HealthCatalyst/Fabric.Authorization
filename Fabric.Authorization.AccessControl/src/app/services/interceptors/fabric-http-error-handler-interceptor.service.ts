
import {throwError as observableThrowError,  Observable } from 'rxjs';

import {catchError} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';


import { Exception } from '../../models/exception.model';
import { AlertService } from '../global/alert.service';

@Injectable()
export class FabricHttpErrorHandlerInterceptorService {

  constructor(private alertService: AlertService) {}

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
          this.showAlert(response, response.error.message);
        } else {
          // The backend returned an unsuccessful response code.
          // The response body may contain clues as to what went wrong,
          console.error(
            `Backend returned code ${response.status}, body was: ${JSON.stringify(
              response.error
            )}`
          );

          this.showAlert(response, response.statusText);
        }

        return observableThrowError(
          new Exception(response.status, response.error.message || JSON.stringify(response.error))
        );
    }));
  }

  showAlert(response: HttpErrorResponse, errorMessage: string) {
    if (response.status !== 404 && response.status !== 409) {
      this.alertService.showError(errorMessage);
    }
  }
}
