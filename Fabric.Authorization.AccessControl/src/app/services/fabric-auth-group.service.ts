
import {distinctUntilChanged, debounceTime, tap, filter, switchMap } from 'rxjs/operators';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable, of } from 'rxjs';

import { FabricBaseService } from './fabric-base.service';
import { IAccessControlConfigService } from './access-control-config.service';
import { IGroup } from '../models/group.model';
import { IUser } from '../models/user.model';
import { IRole } from '../models/role.model';

@Injectable()
export class FabricAuthGroupService extends FabricBaseService {

  public static baseGroupApiUrl = '';
  public static groupRolesApiUrl = '';
  public static groupUsersApiUrl = '';
  public static childGroupsApiUrl = '';

  constructor(
    httpClient: HttpClient,
    @Inject('IAccessControlConfigService') accessControlConfigService: IAccessControlConfigService
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

    if (!FabricAuthGroupService.childGroupsApiUrl) {
      FabricAuthGroupService.childGroupsApiUrl = `${FabricAuthGroupService.baseGroupApiUrl}/{groupName}/groups`;
    }
  }

  public getGroup(groupName: string, identityProvider?: string, tenantId?: string): Observable<IGroup> {
    let url = `${FabricAuthGroupService.baseGroupApiUrl}/${encodeURI(groupName)}`;
    url = this.setIdPAndTenant(url, identityProvider, tenantId);
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
      return of(undefined);
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
    ).pipe(tap((user) => {
      this.sendGroupUserDataChanges(users, groupName, 'added');
    }));
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
    ).pipe(tap(() => {
      this.sendGroupUserDataChanges([user], groupName, 'removed');
    }));
  }

  private sendGroupUserDataChanges(users: IUser[], groupName: string, action: string) {
    const changedData = {
      memberAffected: groupName,
      memberType: 'group',
      action: action,
      changedDataType: 'user',
      changes: users.map((user, index) => {
        return user.subjectId;
      })
    };
    this.accessControlConfigService.dataChanged.next(changedData);
  }

  public getGroupRoles(
    groupName: string,
    grain: string,
    securableItem: string,
    identityProvider?: string,
    tenantId?: string
  ): Observable<IRole[]> {
    let url = `${FabricAuthGroupService.baseGroupApiUrl}/${encodeURI(groupName)}/${encodeURI(grain)}/${encodeURI(securableItem)}/roles`;
    url = this.setIdPAndTenant(url, identityProvider, tenantId);
    return this.httpClient.get<IRole[]>(url);
  }

  public addRolesToGroup(
    groupName: string,
    roles: Array<IRole>,
    identityProvider?: string,
    tenantId?: string
  ): Observable<IGroup> {
    if (!roles || roles.length === 0) {
      return of(undefined);
    }

    let url = this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName);
    url = this.setIdPAndTenant(url, identityProvider, tenantId);
    return this.httpClient.post<IGroup>(
      url,
      roles
    ).pipe(tap((user) => {
      this.sendGroupRoleDataChanges(roles, groupName, 'added');
    }));
  }

  public removeRolesFromGroup(
    groupName: string,
    roles: IRole[],
    identityProvider?: string,
    tenantId?: string
  ): Observable<IGroup> {
    if (!roles || roles.length === 0) {
      return of(undefined);
    }

    let url = this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName);
    url = this.setIdPAndTenant(url, identityProvider, tenantId);
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
    ).pipe(tap((user) => {
      this.sendGroupRoleDataChanges(roles, groupName, 'removed');
    }));
  }

  private sendGroupRoleDataChanges(roles: IRole[], groupName: string, action: string) {
    const changedData = {
      memberAffected: groupName,
      memberType: 'group',
      action: action,
      changedDataType: 'role',
      changes: roles.map((role, index) => {
        return role.name;
      })
    };
    this.accessControlConfigService.dataChanged.next(changedData);
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

  public search(groupName: Observable<string>): Observable<IGroup[]> {
    return groupName.pipe(debounceTime(250),
      distinctUntilChanged(),
      filter((term: string) =>  term && term.length > 2),
      switchMap((term) => {
        let params = new HttpParams()
          .set('name', term);

        params = params.set('type', 'custom');

        return this.httpClient.get<IGroup[]>(
          FabricAuthGroupService.baseGroupApiUrl,
          { params }
        );
      }));
  }

  public getChildGroups(
    groupName: string,
    identityProvider?: string,
    tenantId?: string): Observable<IGroup[]> {
    let url = this.replaceGroupNameSegment(FabricAuthGroupService.childGroupsApiUrl, groupName);
    url = this.setIdPAndTenant(url, identityProvider, tenantId);
    return this.httpClient.get<IGroup[]>(url);
  }

  public addChildGroups(
    groupName: string,
    childGroups: IGroup[],
    identityProvider?: string,
    tenantId?: string
  ): Observable<IGroup> {
    if (!childGroups || childGroups.length === 0) {
      return of(undefined);
    }

    let url = this.replaceGroupNameSegment(FabricAuthGroupService.childGroupsApiUrl, groupName);
    url = this.setIdPAndTenant(url, identityProvider, tenantId);

    return this.httpClient.post<IGroup>(url,
      childGroups.map(function (g) {
        return {
          groupName: g.groupName,
          groupSource: 'directory',
          identityProvider: g.identityProvider,
          tenantId: g.tenantId
        };
      })
    );
  }

  public removeChildGroups(
    groupName: string,
    childGroups: IGroup[],
    identityProvider?: string,
    tenantId?: string
  ): Observable<IGroup> {
    let url = this.replaceGroupNameSegment(FabricAuthGroupService.childGroupsApiUrl, groupName);
    url = this.setIdPAndTenant(url, identityProvider, tenantId);

    return this.httpClient.request<IGroup>(
      'DELETE',
      url,
      { body: childGroups.map(function (g) {
          return {
            groupName: g.groupName,
            identityProvider: g.identityProvider,
            tenantId: g.tenantId
          };
        })
      }
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
