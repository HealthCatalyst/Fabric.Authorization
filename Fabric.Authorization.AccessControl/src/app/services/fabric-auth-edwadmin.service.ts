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
      const url = this.replaceGroupNameSegment(FabricAuthEdwAdminService.groupEdwAdminSyncUrl, groupName);
      return this.httpClient.post(url, '', {params: this.getQueryParams(identityProvider, tenantId)});
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

  private getQueryParams(identityProvider: string, tenantId: string) {
    const params = {};
    if (identityProvider) {
      params['identityProvider'] = identityProvider;
    }
    if (tenantId) {
      params['tenantId'] = tenantId;
    }
    return params;
  }
}
