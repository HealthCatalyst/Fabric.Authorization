import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { catchError, retry } from 'rxjs/operators';

import {
  IAuthMemberSearchRequest,
  IAuthMemberSearchResult,
  IAuthMemberSearchResponse
} from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthMemberSearchService extends FabricBaseService {
  public static baseMemberSearchApiUrl = '';

  constructor(
    httpClient: HttpClient,
    accessControlConfigService: AccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthMemberSearchService.baseMemberSearchApiUrl) {
      FabricAuthMemberSearchService.baseMemberSearchApiUrl = `${accessControlConfigService.getFabricAuthApiUrl()}/members`;
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
