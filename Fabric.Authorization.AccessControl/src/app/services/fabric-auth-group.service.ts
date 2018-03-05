import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';

import { User } from '../models/user';
import { Role } from '../models/role';
import { Group } from '../models/group';
import { Exception } from '../models/exception';
import { FabricAuthBaseService } from './fabric-auth-base.service';

@Injectable()
export class FabricAuthGroupService extends FabricAuthBaseService {

  static readonly baseGroupApiUrl = FabricAuthBaseService.authUrl + "/groups";
  static readonly groupRolesApiUrl = FabricAuthGroupService.baseGroupApiUrl + "/{groupName}/roles";

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  public addUserToCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.httpClient
      .post(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), user)
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  public removeUserFromCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.httpClient
      .delete(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  public getGroupRoles(groupName: string): Observable<Role[]> {
    return this.httpClient
      .get(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  public addRoleToGroup(groupName: string, role: Role) {
    return this.httpClient
      .post(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), role)
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  public removeRoleFromGroup(groupName: string, role: Role) : Observable<Group> {
    return this.httpClient
      .delete(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  private replaceGroupNameSegment(tokenizedUrl: string, groupName: string): string {
    return tokenizedUrl
      .replace("{groupName}", groupName);
  }
}
