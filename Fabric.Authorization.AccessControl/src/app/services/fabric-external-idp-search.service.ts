import { switchMap, filter, distinctUntilChanged, debounceTime, catchError, retry, map } from 'rxjs/operators';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable } from 'rxjs';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';





import { IdPSearchResult } from '../models/idpSearchResult.model';

@Injectable()
export class FabricExternalIdpSearchService extends FabricBaseService {

  public static idPServiceBaseUrl: Observable<string> = null;

  constructor(
    httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricExternalIdpSearchService.idPServiceBaseUrl) {
      const service = accessControlConfigService;
      FabricExternalIdpSearchService.idPServiceBaseUrl = service.fabricExternalIdpSearchApiUrl.pipe(
        map(url => `${url}/api/principals/search`)
      );
    }
  }

  public search(searchText: Observable<string>, type: string): Observable<IdPSearchResult> {
    return searchText.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      filter((term: string) => term && term.length >= 2),
      switchMap(term => {
          let params: HttpParams = new HttpParams().set('searchText', term);

          if (type) {
              params = params.set('type', type);
          }

          return FabricExternalIdpSearchService.idPServiceBaseUrl.pipe(
            switchMap(url => this.httpClient.get<IdPSearchResult>(url, { params }))
          );
      })
  );
  }
}
