import { Injectable } from '@angular/core';
import {Http, Response} from "@angular/http";
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

  constructor(http: Http) {
    super(http);
  }

  public addUserToCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.http
      .post(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), user)
      .map(response => response.json());
  }

  public removeUserFromCustomGroup(groupName: string, user: User) : Observable<Group> {
    return this.http
      .delete(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .map(response => response.json());
  }

  public getGroupRoles(groupName: string): Observable<Role[]> {
    return this.http
      .get(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .map(response => response.json());
  }

  public addRoleToGroup(groupName: string, role: Role) {
    return this.http
      .post(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName), role)
      .map(response => response.json());
  }

  public removeRoleFromGroup(groupName: string, role: Role) {
    return this.http
      .delete(this.replaceGroupNameSegment(FabricAuthGroupService.groupRolesApiUrl, groupName))
      .map(response => response.json());
  }

  private replaceGroupNameSegment(tokenizedUrl: string, groupName: string): string {
    return tokenizedUrl
      .replace("{groupName}", groupName);
  }
}
