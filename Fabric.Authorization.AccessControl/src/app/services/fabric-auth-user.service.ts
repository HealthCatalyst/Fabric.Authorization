
import {tap} from 'rxjs/operators';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable, of } from 'rxjs';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';
import { IUser } from '../models/user.model';
import { IGroup } from '../models/group.model';
import { IRole } from '../models/role.model';
import { IUserPermissionResponse } from '../models/userPermissionResponse.model';

@Injectable()
export class FabricAuthUserService extends FabricBaseService {

  public static baseUserApiUrl = '';
  public static userRolesApiUrl = '';
  public static userGroupsApiUrl = '';
  public static currentUserPermissionsApiUrl = '';

  constructor(
    httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
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

    if (!FabricAuthUserService.currentUserPermissionsApiUrl) {
      FabricAuthUserService.currentUserPermissionsApiUrl = `${FabricAuthUserService.baseUserApiUrl}/permissions`;
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

  public getCurrentUserPermissions(
    securableItem?: string
  ): Observable<IUserPermissionResponse> {
    let params = new HttpParams();
    if (securableItem) {
      params = params.set('securableItem', securableItem);
    }

    return this.httpClient.get<IUserPermissionResponse>(FabricAuthUserService.currentUserPermissionsApiUrl, { params });
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
      return of(undefined);
    }

    const url = this.replaceUserIdSegment(
      FabricAuthUserService.userRolesApiUrl,
      identityProvider,
      subjectId
    );
    return this.httpClient.post<IUser>(
      url,
      roles
    ).pipe(tap((user) => {
      this.sendUserRoleDataChanges(roles, subjectId, 'added');
    }));
  }

  private sendUserRoleDataChanges(roles: IRole[], subjectId: string, action: string) {
    const changedData = {
      memberAffected: subjectId,
      memberType: 'user',
      action: action,
      changedDataType: 'role',
      changes: roles.map((role, index) => {
        return role.name;
      })
    };
    this.accessControlConfigService.dataChanged.next(changedData);
  }

  public removeRolesFromUser(
    identityProvider: string,
    subjectId: string,
    roles: IRole[]
  ): Observable<IUser> {
    if (!roles || roles.length === 0) {
      return of(undefined);
    }

    return this.httpClient.request<IUser>(
      'DELETE',
      this.replaceUserIdSegment(
        FabricAuthUserService.userRolesApiUrl,
        identityProvider,
        subjectId
      ),
      { body: roles }
    ).pipe(tap((user) => {
      this.sendUserRoleDataChanges(roles, subjectId, 'removed');
    }));
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
