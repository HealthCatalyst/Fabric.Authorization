import { Injectable } from '@angular/core';
import {Http, Response} from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { Exception } from '../models/exception';
import { environment } from '../../environments/environment';

@Injectable()
export class FabricAuthBaseService {

  protected static readonly authUrl = environment.fabricAuthApiUri + "/" + environment.fabricAuthApiVersionSegment;

  constructor(protected http: Http) { }

  protected handleError(response: Response) {
    if (response.status >= 400) {
      Observable.throw(new Exception(response.status, response.statusText));
    }
  }  
}
