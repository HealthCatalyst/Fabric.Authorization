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

  public static baseGroupApiUrl = '';
  public static groupRolesApiUrl = '';
  public static groupUsersApiUrl = '';

  constructor(
    httpClient: HttpClient,
    accessControlConfigService: AccessControlConfigService
  ) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthGroupService.baseGroupApiUrl) {
      FabricAuthGroupService.baseGroupApiUrl = `${accessControlConfigService.fabricAuthApiUrl}/groups`;
    }

    if (!FabricAuthGroupService.groupRolesApiUrl) {
      FabricAuthGroupService.groupRolesApiUrl = `${FabricAuthGroupService.baseGroupApiUrl}/{groupName}/roles`;
    }

    if (!FabricAuthGroupService.groupUsersApiUrl) {
      FabricAuthGroupService.groupUsersApiUrl = `${FabricAuthGroupService.baseGroupApiUrl}/{groupName}/users`;
    }
  }

  public getGroup(groupName: string): Observable<IGroup> {
    const url = `${FabricAuthGroupService.baseGroupApiUrl}/${encodeURI(groupName)}`;
    return this.httpClient.get<IGroup>(url);
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
    if (!users || users.length === 0) {
      return Observable.of(undefined);
    }

    return this.httpClient.post<IGroup>(
      this.replaceGroupNameSegment(
        FabricAuthGroupService.groupUsersApiUrl,
        groupName
      ),
      users.map(function (u) {
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
    const url = `${FabricAuthGroupService.baseGroupApiUrl}/${encodeURI(groupName)}/${encodeURI(grain)}/${encodeURI(securableItem)}/roles`;
    return this.httpClient.get<IRole[]>(url);
  }

  public addRolesToGroup(
    groupName: string,
    roles: Array<IRole>
  ): Observable<IGroup> {
    if (!roles || roles.length === 0) {
      return Observable.of(undefined);
    }

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
    if (!roles || roles.length === 0) {
      return Observable.of(undefined);
    }

    const url = this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName);
    return this.httpClient.request<IGroup>(
      'DELETE',
      url,
      {
        body: roles.map(function (r) {
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
    ).do((newGroup) => { this.accessControlConfigService.dataChangeEvent({
      groupName: newGroup.groupName
    });
  });
  }

  private replaceGroupNameSegment(
    tokenizedUrl: string,
    groupName: string
  ): string {
    return encodeURI(tokenizedUrl.replace('{groupName}', groupName));
  }
}
