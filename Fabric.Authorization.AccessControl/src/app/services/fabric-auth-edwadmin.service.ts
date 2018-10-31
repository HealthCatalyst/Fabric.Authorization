import { HttpClient } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable } from 'rxjs';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';
import { IUser } from '../models/user.model';

@Injectable()
export class FabricAuthEdwAdminService extends FabricBaseService {
  private static userEdwAdminSyncUrl = '';
  private static groupEdwAdminSyncUrl = '';

  constructor(httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthEdwAdminService.userEdwAdminSyncUrl) {
      FabricAuthEdwAdminService.userEdwAdminSyncUrl = `${accessControlConfigService.fabricAuthApiUrl}/edw/roles`;
    }

    if (!FabricAuthEdwAdminService.groupEdwAdminSyncUrl) {
      FabricAuthEdwAdminService.groupEdwAdminSyncUrl = `${accessControlConfigService.fabricAuthApiUrl}/edw/{groupName}/roles`;
    }
  }

  public syncUsersWithEdwAdmin(
    users: IUser[]): Observable<Object> {
      const userArray = [];
      for (let i = 0; i < users.length; i++) {
        userArray.push({ subjectId: users[i].subjectId, identityProvider: users[i].identityProvider });
      }

      return this.httpClient.post(FabricAuthEdwAdminService.userEdwAdminSyncUrl, userArray);
    }

  public syncGroupWithEdwAdmin(
    groupName: string,
    identityProvider?: string,
    tenantId?: string): Observable<Object> {
      let url = this.replaceGroupNameSegment(FabricAuthEdwAdminService.groupEdwAdminSyncUrl, groupName);
      url = this.setIdPAndTenant(url, identityProvider, tenantId);
      return this.httpClient.post(url, '');
    }

  private replaceGroupNameSegment(
    tokenizedUrl: string,
    groupName: string
  ): string {
    return encodeURI(
      tokenizedUrl
        .replace('{groupName}', groupName)
    );
  }

  private setIdPAndTenant(url: string, identityProvider: string, tenantId: string) {
    url = this.setQueryParameters(url, 'identityProvider', identityProvider);
    url = this.setQueryParameters(url, 'tenantId', tenantId);
    return url;
  }

  private setQueryParameters(url: string, key: string, val: string) {
    if (val) {
      if (url.indexOf('?') < 0) {
        url = `${url}?${key}=${val}`;
      } else {
        url = `${url}&${key}=${val}`;
      }
    }

    return url;
  }
}
