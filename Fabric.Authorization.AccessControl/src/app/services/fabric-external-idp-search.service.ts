import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { environment } from '../../environments/environment';

import { IdPSearchResult } from '../models/idpSearchResult';

@Injectable()
export class FabricExternalIdpSearchService {

  constructor(http: HttpClient) {
  }

  public searchExternalIdP() : Observable<IdPSearchResult> {
    return null;
  }
}
