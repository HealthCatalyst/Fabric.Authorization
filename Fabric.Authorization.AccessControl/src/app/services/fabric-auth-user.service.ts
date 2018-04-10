import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { catchError, retry } from 'rxjs/operators';

import { Exception, IGroup, IRole, IUser, IDataChanged, IChangedData } from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthUserService extends FabricBaseService {

  public static baseUserApiUrl = '';
  public static userRolesApiUrl = '';
  public static userGroupsApiUrl = '';

  constructor(
    httpClient: HttpClient,
    accessControlConfigService: AccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthUserService.baseUserApiUrl) {
      FabricAuthUserService.baseUserApiUrl = `${accessControlConfigService.fabricAuthApiUrl}/user`;
    }

    if (!FabricAuthUserService.userRolesApiUrl) {
      FabricAuthUserService.userRolesApiUrl = `${FabricAuthUserService.baseUserApiUrl}/{identityProvider}/{subjectId}/roles`;
    }

    if (!FabricAuthUserService.userGroupsApiUrl) {
      FabricAuthUserService.userGroupsApiUrl = `${FabricAuthUserService.baseUserApiUrl}/{identityProvider}/{subjectId}/groups`;
    }
  }

  public getUser(identityProvider: string, subjectId: string): Observable<IUser> {
    return this.httpClient.get<IUser>(`${FabricAuthUserService.baseUserApiUrl}/${encodeURI(identityProvider)}/${encodeURI(subjectId)}`);
  }

  public getUserGroups(
    identityProvider: string,
    subjectId: string
  ): Observable<IGroup[]> {
    return this.httpClient.get<IGroup[]>(
      this.replaceUserIdSegment(
        FabricAuthUserService.userGroupsApiUrl,
        identityProvider,
        subjectId
      )
    );
  }

  public getUserRoles(
    identityProvider: string,
    subjectId: string
  ): Observable<IRole[]> {
    return this.httpClient.get<IRole[]>(
      this.replaceUserIdSegment(
        FabricAuthUserService.userRolesApiUrl,
        identityProvider,
        subjectId
      )
    );
  }

  public addRolesToUser(
    identityProvider: string,
    subjectId: string,
    roles: IRole[]
  ): Observable<IUser> {
    if (!roles || roles.length === 0) {
      return Observable.of(undefined);
    }

    const url = this.replaceUserIdSegment(
      FabricAuthUserService.userRolesApiUrl,
      identityProvider,
      subjectId
    );
    return this.httpClient.post<IUser>(
      url,
      roles
    ).do((user) => {
      const changedData = this.getUserRoleChanges(roles, subjectId, 'added');
      this.accessControlConfigService.dataChangeEvent(changedData);
    });
  }

  private getUserRoleChanges(roles: IRole[], subjectId: string, action: string): IDataChanged {
    return {
      member: subjectId,
      actionType: action,
      changes: roles.map((role, index) => {
        return { name: role.name };
      })
    };
  }

  public removeRolesFromUser(
    identityProvider: string,
    subjectId: string,
    roles: IRole[]
  ): Observable<IUser> {
    if (!roles || roles.length === 0) {
      return Observable.of(undefined);
    }

    return this.httpClient.request<IUser>(
      'DELETE',
      this.replaceUserIdSegment(
        FabricAuthUserService.userRolesApiUrl,
        identityProvider,
        subjectId
      ),
      { body: roles }
    ).do((user) => {
      const changedData = this.getUserRoleChanges(roles, subjectId, 'removed');
      this.accessControlConfigService.dataChangeEvent(changedData);
    });
  }

  public createUser(user: IUser): Observable<IUser> {
    return this.httpClient.post<IUser>(
      FabricAuthUserService.baseUserApiUrl,
      user
    );
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
}
