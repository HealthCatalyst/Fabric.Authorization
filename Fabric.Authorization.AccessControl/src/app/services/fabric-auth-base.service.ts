import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { environment } from '../../environments/environment';

import { Exception } from '../models/exception';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class FabricAuthBaseService {

  protected static readonly authUrl = environment.fabricAuthApiUri + "/" + environment.fabricAuthApiVersionSegment;

  constructor(protected httpClient: HttpClient) { }

  protected handleError(response: Response) {
    if (response.status >= 400) {
      Observable.throw(new Exception(response.status, response.statusText));
    }
  }  
}
