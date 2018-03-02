import { Injectable } from '@angular/core';
import {Http, Response} from "@angular/http";
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { environment } from '../../environments/environment';
import { IdPSearchResult } from '../models/idpSearchResult';

@Injectable()
export class FabricExternalIdpSearchService {

  constructor(http: Http) {
  }

  public searchExternalIdP() : Observable<IdPSearchResult> {
    return null;
  }
}
