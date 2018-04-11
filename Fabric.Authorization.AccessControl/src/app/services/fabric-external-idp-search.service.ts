import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { catchError, retry } from 'rxjs/operators';
import { environment } from '../../environments/environment';

import { IdPSearchResult } from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

import 'rxjs/add/operator/distinctUntilChanged';
import 'rxjs/add/operator/debounceTime';
import 'rxjs/add/operator/filter';
import 'rxjs/add/operator/switchMap';

@Injectable()
export class FabricExternalIdpSearchService extends FabricBaseService {

  public static idPServiceBaseUrl = '';

  constructor(
    httpClient: HttpClient,
    accessControlConfigService: AccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricExternalIdpSearchService.idPServiceBaseUrl) {
      const service = accessControlConfigService;
      FabricExternalIdpSearchService.idPServiceBaseUrl = `${service.fabricExternalIdpSearchApiUrl}/principals/search`;
    }
  }

  public search(searchText: Observable<string>, type: string): Observable<IdPSearchResult> {
    return searchText.debounceTime(250)
      .distinctUntilChanged()
      .filter((term: string) =>  term && term.length > 2)
      .switchMap((term) => {
        let params = new HttpParams()
          .set('searchText', term);

        if (type) {
          params = params.set('type', type);
        }

        return this.httpClient.get<IdPSearchResult>(
          FabricExternalIdpSearchService.idPServiceBaseUrl,
          { params }
        );
      });
  }
}
