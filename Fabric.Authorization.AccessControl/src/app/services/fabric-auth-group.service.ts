import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { catchError, retry } from 'rxjs/operators';
import { Exception, Group, Role, User } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';

import { FabricBaseService } from './fabric-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthGroupService extends FabricBaseService {

  public static baseGroupApiUrl: string;
  public static groupRolesApiUrl: string;
  public static groupUsersApiUrl: string;

  constructor(httpClient: HttpClient, accessControlConfigService: AccessControlConfigService) {
    super(httpClient, accessControlConfigService);

    if (!FabricAuthGroupService.baseGroupApiUrl) {
      FabricAuthGroupService.baseGroupApiUrl = `${accessControlConfigService.getFabricAuthApiUrl()}/groups`;
    }
    
    if (!FabricAuthGroupService.groupRolesApiUrl) {
      FabricAuthGroupService.groupRolesApiUrl = `${FabricAuthGroupService.baseGroupApiUrl}/{groupName}/roles`;
    }

    if (!FabricAuthGroupService.groupUsersApiUrl) {
      FabricAuthGroupService.groupUsersApiUrl = `${FabricAuthGroupService.baseGroupApiUrl}/{groupName}/users`;
    }
  }

  public getGroupUsers(groupName: string) : Observable<User[]> {
    return this.httpClient
      .get<User[]>(this.replaceGroupNameSegment(FabricAuthGroupService.groupUsersApiUrl, groupName));
  }

  public addUsersToCustomGroup(groupName: string, users: User[]) : Observable<Group> {
    return this.httpClient
      .post<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupUsersApiUrl, groupName), users.map(function(u) {
        return {
          identityProvider: u.identityProvider,
          subjectId: u.subjectId
        };
      }));
  }

  public removeUserFromCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.httpClient.request<Group>(
      'DELETE',
      this.replaceGroupNameSegment(FabricAuthGroupService.groupUsersApiUrl, groupName),
      {body: user});
  }

  public getGroupRoles(groupName: string, grain?: string, securableItem?: string): Observable<Role[]> {
    
    let params = new HttpParams();
    if (grain) {
      params.set('grain', grain);
    }

    if (securableItem) {
      params.set('securableItem', securableItem);
    }

    return this.httpClient
      .get<Role[]>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), {params});
  }

  public addRolesToGroup(groupName: string, roles: Array<Role>): Observable<Group>{
    return this.httpClient
      .post<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), roles);
  }

  public removeRoleFromGroup(groupName: string, role: Role) : Observable<Group> {
    return this.httpClient.request<Group>(
      'DELETE',
      this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName),
      {body: role});
  }

  public createGroup(group: Group) : Observable<Group>{
    return this.httpClient
      .post<Group>(FabricAuthGroupService.baseGroupApiUrl, group);
  }

  private replaceGroupNameSegment(tokenizedUrl: string, groupName: string): string {
    return encodeURI(tokenizedUrl.replace("{groupName}", groupName));
  }
}
