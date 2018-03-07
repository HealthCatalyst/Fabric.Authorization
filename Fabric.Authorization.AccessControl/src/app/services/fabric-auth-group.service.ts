import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { catchError, retry } from 'rxjs/operators';
import { Exception, Group, Role, User } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';

import { FabricBaseService } from './fabric-auth-base.service';
import { AccessControlConfigService } from './access-control-config.service';

@Injectable()
export class FabricAuthGroupService extends FabricBaseService {

  private static baseGroupApiUrl;
  private static groupRolesApiUrl;
  private static groupUsersApiUrl;

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

  public addUserToCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.httpClient
      .post<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupUsersApiUrl, groupName), user);
  }

  public removeUserFromCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.httpClient
      .delete<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupUsersApiUrl, groupName));
  }

  public getGroupRoles(groupName: string): Observable<Role[]> {
    return this.httpClient
      .get<Role[]>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName));
  }

  public addRoleToGroup(groupName: string, role: Role) : Observable<Group> {
    return this.httpClient
      .post<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), role);
  }

  public removeRoleFromGroup(groupName: string, role: Role) : Observable<Group> {
    return this.httpClient
      .delete<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName));
  }

  private replaceGroupNameSegment(tokenizedUrl: string, groupName: string): string {
    return tokenizedUrl
      .replace("{groupName}", groupName);
  }
}
