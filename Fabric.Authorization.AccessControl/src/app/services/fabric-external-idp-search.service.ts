import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { catchError, retry } from 'rxjs/operators';
import { environment } from '../../environments/environment';

import { IdPSearchResult, IdPSearchRequest } from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

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
      FabricExternalIdpSearchService.idPServiceBaseUrl = `${service.getFabricExternalIdpSearchApiUrl()}/principals/search`;
    }
  }

  public searchExternalIdP(
    request: IdPSearchRequest
  ): Observable<IdPSearchResult> {
    let params = new HttpParams().set('searchText', request.searchText);

    if (request.type) {
      params = params.set('type', request.type);
    }

    return this.httpClient.get<IdPSearchResult>(
      FabricExternalIdpSearchService.idPServiceBaseUrl,
      { params }
    );
  }
}
