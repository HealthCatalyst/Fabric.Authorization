import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { environment } from '../../environments/environment';

import { Exception } from '../models';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';

@Injectable()
export class FabricAuthBaseService {

  protected static readonly retryCount = 3;
  protected static readonly authUrl = `${environment.fabricAuthApiUri}/${environment.fabricAuthApiVersionSegment}`;

  constructor(protected httpClient: HttpClient) { }

  /*
    Pattern below found at https://angular.io/guide/http#error-handling.
  */
  protected handleError(response: HttpErrorResponse) {
    console.log('handling error = ' + response);
    if (response.error instanceof ErrorEvent) {
      // A client-side or network error occurred. Handle it accordingly.
      console.error('An error occurred:', response.error.message);
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong,
      console.error(
        `Backend returned code ${response.status}, body was: ${response.error}`);
    }        
    console.log('returning errorobservable');
    return Observable.throw(new Exception(response.status, response.statusText));
    //return new ErrorObservable(new Exception(response.status, response.statusText));
  }  
}
