import { Injectable } from '@angular/core';
import {
  Http,
  XHRBackend,
  RequestOptions,
  RequestOptionsArgs,
  Response,
  Headers,
  Request
} from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/Rx';

import { LoggingService } from './logging.service';

@Injectable()
export class HttpInterceptor extends Http { 

  constructor(
    _backend: XHRBackend, 
    _defaultOptions: RequestOptions,
    private _loggingService: LoggingService
  ){
    super(_backend, _defaultOptions);
  }

  request(url: string|Request, options?: RequestOptionsArgs): Observable<Response> {
   
    return super.request(url, options).catch(this.catchAuthError(this));
  }

  private catchAuthError (self: HttpInterceptor) {
    // we have to pass HttpInterceptor's own instance here as `self`
    return (res: Response) => {
      self._loggingService.log(res);
      // if (res.status === 401 || res.status === 403) {
      //   // if not authenticated
      //  self._loggingService.log(res);
      // }
      return Observable.throw(res);
    };
  }

}