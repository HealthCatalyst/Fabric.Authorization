import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { catchError, retry } from 'rxjs/operators';
import { environment } from '../../environments/environment';

import { IdPSearchResult } from '../models';

@Injectable()
export class FabricExternalIdpSearchService {

  constructor(http: HttpClient) {
  }

  public searchExternalIdP() : Observable<IdPSearchResult> {
    return null;
  }
}
