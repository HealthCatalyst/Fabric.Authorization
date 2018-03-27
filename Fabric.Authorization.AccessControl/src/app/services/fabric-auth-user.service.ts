import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { catchError, retry } from 'rxjs/operators';

import { Exception, Group, Role, User } from '../models';
import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthUserService extends FabricBaseService {
  public static baseUserApiUrl: string;
  public static userRolesApiUrl: string;
  public static userGroupsApiUrl: string;

  constructor(
    httpClient: HttpClient,
    accessControlConfigService: AccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthUserService.baseUserApiUrl) {
      FabricAuthUserService.baseUserApiUrl = `${accessControlConfigService.getFabricAuthApiUrl()}/user`;
    }

    if (!FabricAuthUserService.userRolesApiUrl) {
      FabricAuthUserService.userRolesApiUrl = `${
        FabricAuthUserService.baseUserApiUrl
      }/{identityProvider}/{subjectId}/roles`;
    }

    if (!FabricAuthUserService.userGroupsApiUrl) {
      FabricAuthUserService.userGroupsApiUrl = `${
        FabricAuthUserService.baseUserApiUrl
      }/{identityProvider}/{subjectId}/groups`;
    }
  }

  public getUser(
    identityProvider: string,
    subjectId: string
  ): Observable<User> {
    return this.httpClient.get<User>(
      encodeURI(
        `${
          FabricAuthUserService.baseUserApiUrl
        }/${identityProvider}/${subjectId}`
      )
    );
  }

  public getUserGroups(
    identityProvider: string,
    subjectId: string
  ): Observable<Group[]> {
    return this.httpClient.get<Group[]>(
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
  ): Observable<Role[]> {
    return this.httpClient.get<Role[]>(
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
    roles: Role[]
  ): Observable<User> {
    return this.httpClient.post<User>(
      this.replaceUserIdSegment(
        FabricAuthUserService.userRolesApiUrl,
        identityProvider,
        subjectId
      ),
      roles
    );
  }

  public removeRolesFromUser(
    identityProvider: string,
    subjectId: string,
    roles: Role[]
  ): Observable<User> {
    return this.httpClient.request<User>(
      'DELETE',
      this.replaceUserIdSegment(
        FabricAuthUserService.userRolesApiUrl,
        identityProvider,
        subjectId
      ),
      { body: roles }
    );
  }

  public createUser(user: User): Observable<User> {
    return this.httpClient.post<User>(
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
