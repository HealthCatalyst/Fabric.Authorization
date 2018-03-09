import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { catchError, retry } from 'rxjs/operators';
import { environment } from '../../environments/environment';

import { IdPSearchResult, IdPSearchRequest } from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricExternalIdpSearchService extends FabricBaseService {

  public static IdPServiceBaseUrl: string;

  constructor(httpClient: HttpClient, accessControlConfigService: AccessControlConfigService) {
    super(httpClient, accessControlConfigService);

    if(!FabricExternalIdpSearchService.IdPServiceBaseUrl){
      FabricExternalIdpSearchService.IdPServiceBaseUrl = `${accessControlConfigService.getFabricExternalIdpSearchApiUrl()}/principals/search`;
    }
    
  }

  public searchExternalIdP(request: IdPSearchRequest) : Observable<IdPSearchResult> {

    var params = new HttpParams();
    params.set('searchtext', request.searchText);

    if(request.type){
      params.set('type', request.type);
    }

    return this.httpClient
      .get<IdPSearchResult>(FabricExternalIdpSearchService.IdPServiceBaseUrl, {params});
  }
}
