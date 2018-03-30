import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import { catchError, retry } from 'rxjs/operators';
import { Exception, IGroup, IRole, IUser } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';

import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthGroupService extends FabricBaseService {
  public static baseGroupApiUrl: string;
  public static groupRolesApiUrl: string;
  public static groupUsersApiUrl: string;

  constructor(
    httpClient: HttpClient,
    accessControlConfigService: AccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthGroupService.baseGroupApiUrl) {
      FabricAuthGroupService.baseGroupApiUrl = `${accessControlConfigService.getFabricAuthApiUrl()}/groups`;
    }

    if (!FabricAuthGroupService.groupRolesApiUrl) {
      FabricAuthGroupService.groupRolesApiUrl = `${
        FabricAuthGroupService.baseGroupApiUrl
      }/{groupName}/roles`;
    }

    if (!FabricAuthGroupService.groupUsersApiUrl) {
      FabricAuthGroupService.groupUsersApiUrl = `${
        FabricAuthGroupService.baseGroupApiUrl
      }/{groupName}/users`;
    }
  }

  public getGroup(groupName: string): Observable<IGroup> {
    return this.httpClient.get<IGroup>(
      encodeURI(`${FabricAuthGroupService.baseGroupApiUrl}/${groupName}`)
    );
  }

  public getGroupUsers(groupName: string): Observable<IUser[]> {
    return this.httpClient.get<IUser[]>(
      this.replaceGroupNameSegment(
        FabricAuthGroupService.groupUsersApiUrl,
        groupName
      )
    );
  }

  public addUsersToCustomGroup(
    groupName: string,
    users: IUser[]
  ): Observable<IGroup> {
    return this.httpClient.post<IGroup>(
      this.replaceGroupNameSegment(
        FabricAuthGroupService.groupUsersApiUrl,
        groupName
      ),
      users.map(function(u) {
        return {
          identityProvider: u.identityProvider,
          subjectId: u.subjectId
        };
      })
    );
  }

  public removeUserFromCustomGroup(
    groupName: string,
    user: IUser
  ): Observable<IGroup> {
    return this.httpClient.request<IGroup>(
      'DELETE',
      this.replaceGroupNameSegment(
        FabricAuthGroupService.groupUsersApiUrl,
        groupName
      ),
      { body: user }
    );
  }

  public getGroupRoles(
    groupName: string,
    grain: string,
    securableItem: string
  ): Observable<IRole[]> {
    return this.httpClient.get<IRole[]>(
      encodeURI(
        `${
          FabricAuthGroupService.baseGroupApiUrl
        }/${groupName}/${grain}/${securableItem}/roles`
      )
    );
  }

  public addRolesToGroup(
    groupName: string,
    roles: Array<IRole>
  ): Observable<IGroup> {
    return this.httpClient.post<IGroup>(
      this.replaceGroupNameSegment(
        FabricAuthGroupService.groupRolesApiUrl,
        groupName
      ),
      roles
    );
  }

  public removeRolesFromGroup(
    groupName: string,
    roles: IRole[]
  ): Observable<IGroup> {
    return this.httpClient.request<IGroup>(
      'DELETE',
      this.replaceGroupNameSegment(
        FabricAuthGroupService.groupRolesApiUrl,
        groupName
      ),
      {
        body: roles.map(function(r) {
          return {
            roleId: r.id
          };
        })
      }
    );
  }

  public createGroup(group: IGroup): Observable<IGroup> {
    return this.httpClient.post<IGroup>(
      FabricAuthGroupService.baseGroupApiUrl,
      group
    );
  }

  private replaceGroupNameSegment(
    tokenizedUrl: string,
    groupName: string
  ): string {
    return encodeURI(tokenizedUrl.replace('{groupName}', groupName));
  }
}
