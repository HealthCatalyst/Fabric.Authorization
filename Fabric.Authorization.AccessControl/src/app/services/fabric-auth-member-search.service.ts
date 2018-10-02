import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable } from 'rxjs';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';
import { IAuthMemberSearchRequest } from '../models/authMemberSearchRequest.model';
import { IAuthMemberSearchResponse } from '../models/authMemberSearchResult.model';

@Injectable()
export class FabricAuthMemberSearchService extends FabricBaseService {
  public static baseMemberSearchApiUrl = '';

  constructor(
    httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthMemberSearchService.baseMemberSearchApiUrl) {
      FabricAuthMemberSearchService.baseMemberSearchApiUrl = `${accessControlConfigService.fabricAuthApiUrl}/members`;
    }
  }

  public searchMembers(
    request: IAuthMemberSearchRequest
  ): Observable<IAuthMemberSearchResponse> {
    let params = new HttpParams();

    if (request.clientId) {
      params = params.set('clientId', request.clientId);
    }

    if (request.grain) {
      params = params.set('grain', request.grain);
    }

    if (request.securableItem) {
      params = params.set('securableItem', request.securableItem);
    }

    if (request.pageSize) {
      params = params.set('pageSize', request.pageSize.toString());
    }

    if (request.pageNumber) {
      params = params.set('pageNumber', request.pageNumber.toString());
    }

    if (request.sortKey) {
      params = params.set('sortKey', request.sortKey);
    }

    if (request.sortDirection) {
      params = params.set('sortDirection', request.sortDirection);
    }

    if (request.filter) {
      params = params.set('filter', request.filter);
    }

    return this.httpClient.get<IAuthMemberSearchResponse>(
      FabricAuthMemberSearchService.baseMemberSearchApiUrl,
      { params }
    );
  }
}
