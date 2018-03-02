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
export class FabricAuthUserService extends FabricAuthBaseService {

  static readonly baseUserApiUrl = FabricAuthBaseService.authUrl + "/user";
  static readonly userRolesApiUrl = FabricAuthUserService.baseUserApiUrl + "/{identityProvider}/{subjectId}/roles";

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  public getUserRoles(identityProvider: string, subjectId: string) : Observable<Role[]> {
    return this.httpClient
      .get(this.replaceUserIdSegment(FabricAuthUserService.userRolesApiUrl, identityProvider, subjectId))
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  public addRolesToUser(identityProvider: string, subjectId: string, roles: Role[]) : Observable<User> {
    return this.httpClient
      .post(this.replaceUserIdSegment(FabricAuthUserService.userRolesApiUrl, identityProvider, subjectId), roles)
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  public removeRolesFromUser(identityProvider: string, subjectId: string, roles: Role[]) : Observable<User> {
    return this.httpClient
      .delete(this.replaceUserIdSegment(FabricAuthUserService.userRolesApiUrl, identityProvider, subjectId))
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });
  }

  private replaceUserIdSegment(tokenizedUrl: string, identityProvider: string, subjectId: string): string {
    return tokenizedUrl
      .replace("{identityProvider}", identityProvider)
      .replace("{subjectId}", subjectId);
  }
}
