import { HttpClient } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable } from 'rxjs/Observable';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthEdwAdminService extends FabricBaseService {
  private static userEdwAdminSyncUrl = '';
  private static groupEdwAdminSyncUrl = '';

  constructor(httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthEdwAdminService.userEdwAdminSyncUrl) {
      FabricAuthEdwAdminService.userEdwAdminSyncUrl =
        `${accessControlConfigService.fabricAuthApiUrl}/edw/{subjectId}/{identityProvider}/roles`;
    }

    if (!FabricAuthEdwAdminService.groupEdwAdminSyncUrl) {
      FabricAuthEdwAdminService.groupEdwAdminSyncUrl = `${accessControlConfigService.fabricAuthApiUrl}/edw/{groupName}/roles`;
    }
  }

  public syncUserWithEdwAdmin(
    identityProvider: string,
    subjectId: string): Observable<Object> {
      return this.httpClient.post(this.replaceUserIdSegment(
        FabricAuthEdwAdminService.userEdwAdminSyncUrl,
        identityProvider,
        subjectId
      ), '');
    }

  public syncGroupWithEdwAdmin(
    groupName: string): Observable<Object> {
      return this.httpClient.post(this.replaceGroupNameSegment(
        FabricAuthEdwAdminService.groupEdwAdminSyncUrl,
        groupName
      ), '');
    }

  private replaceUserIdSegment(
    tokenizedUrl: string,
    identityProvider: string,
    subjectId: string
  ): string {
    return encodeURI(
      tokenizedUrl
        .replace('{identityProvider}', identityProvider)
        .replace('{subjectId}', subjectId)
    );
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
