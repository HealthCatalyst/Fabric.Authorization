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
    groupName: string): Observable<Object> {
      return this.httpClient.post(this.replaceGroupNameSegment(
        FabricAuthEdwAdminService.groupEdwAdminSyncUrl,
        groupName
      ), '');
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
}
