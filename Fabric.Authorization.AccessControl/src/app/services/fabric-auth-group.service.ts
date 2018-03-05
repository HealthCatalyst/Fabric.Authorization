import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { catchError, retry } from 'rxjs/operators';

import { User } from '../models/user';
import { Role } from '../models/role';
import { Group } from '../models/group';
import { Exception } from '../models/exception';
import { FabricAuthBaseService } from './fabric-auth-base.service';

@Injectable()
export class FabricAuthGroupService extends FabricAuthBaseService {

  static readonly baseGroupApiUrl = `${FabricAuthBaseService.authUrl}/groups`;
  static readonly groupRolesApiUrl = `${FabricAuthGroupService.baseGroupApiUrl}/{groupName}/roles`;

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  public addUserToCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.httpClient
      .post<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), user)
      .pipe(retry(FabricAuthBaseService.retryCount), catchError(this.handleError));
  }

  public removeUserFromCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.httpClient
      .delete<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .pipe(retry(FabricAuthBaseService.retryCount), catchError(this.handleError));
  }

  public getGroupRoles(groupName: string): Observable<Role[]> {
    return this.httpClient
      .get<Role[]>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .pipe(retry(FabricAuthBaseService.retryCount), catchError(this.handleError));
  }

  public addRoleToGroup(groupName: string, role: Role) : Observable<Group> {
    return this.httpClient
      .post<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), role)
      .pipe(retry(FabricAuthBaseService.retryCount), catchError(this.handleError));
  }

  public removeRoleFromGroup(groupName: string, role: Role) : Observable<Group> {
    return this.httpClient
      .delete<Group>(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .pipe(retry(FabricAuthBaseService.retryCount), catchError(this.handleError));
  }

  private replaceGroupNameSegment(tokenizedUrl: string, groupName: string): string {
    return tokenizedUrl
      .replace("{groupName}", groupName);
  }
}
