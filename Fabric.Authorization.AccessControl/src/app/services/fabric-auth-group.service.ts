import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, Inject } from '@angular/core';
import { Observable } from 'rxjs/Observable';

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
    ).do((user) => {
      this.sendGroupUserDataChanges(users, groupName, 'added');
    });
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
    ).do(() => {
      this.sendGroupUserDataChanges([user], groupName, 'removed');
    });
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
    ).do((user) => {
      this.sendGroupRoleDataChanges(roles, groupName, 'added');
    });
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
    ).do((user) => {
      this.sendGroupRoleDataChanges(roles, groupName, 'removed');
    });
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
    return groupName.debounceTime(250)
      .distinctUntilChanged()
      .filter((term: string) =>  term && term.length > 2)
      .switchMap((term) => {
        let params = new HttpParams()
          .set('name', term);

        params = params.set('type', 'custom');

        return this.httpClient.get<IGroup[]>(
          FabricAuthGroupService.baseGroupApiUrl,
          { params }
        );
      });
  }

  public getChildGroups(groupName: string): Observable<IGroup[]> {
    return this.httpClient.get<IGroup[]>(
      this.replaceGroupNameSegment(
        FabricAuthGroupService.childGroupsApiUrl,
        groupName
      )
    );
  }

  public addChildGroups(
    groupName: string,
    childGroups: IGroup[]
  ): Observable<IGroup> {
    if (!childGroups || childGroups.length === 0) {
      return Observable.of(undefined);
    }

    return this.httpClient.post<IGroup>(
      this.replaceGroupNameSegment(
        FabricAuthGroupService.childGroupsApiUrl,
        groupName
      ),
      childGroups.map(function (g) {
        return {
          groupName: g
        };
      })
    );
  }

  public removeChildGroups(
    groupName: string,
    childGroups: string[]
  ): Observable<IGroup> {
    return this.httpClient.request<IGroup>(
      'DELETE',
      this.replaceGroupNameSegment(
        FabricAuthGroupService.childGroupsApiUrl,
        groupName
      ),
      { body: childGroups.map(function (g) {
          return {
            groupName: g
          };
        })
      }
    );
  }
}
