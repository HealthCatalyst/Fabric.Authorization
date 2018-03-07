import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Response } from "@angular/http";
import { Observable } from 'rxjs/Rx';
import { catchError, retry } from 'rxjs/operators';

import { Exception, Group, Role, User } from '../models';
import { FabricAuthBaseService } from '../services';

@Injectable()
export class FabricAuthUserService extends FabricAuthBaseService {

  static readonly baseUserApiUrl = `${FabricAuthBaseService.authUrl}/user`;
  static readonly userRolesApiUrl = `${FabricAuthUserService.baseUserApiUrl}/{identityProvider}/{subjectId}/roles`;

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  public getUserRoles(identityProvider: string, subjectId: string) : Observable<Role[]> {
    return this.httpClient
      .get<Role[]>(this.replaceUserIdSegment(FabricAuthUserService.userRolesApiUrl, identityProvider, subjectId));
  }

  public addRolesToUser(identityProvider: string, subjectId: string, roles: Role[]) : Observable<User> {
    return this.httpClient
      .post<User>(this.replaceUserIdSegment(FabricAuthUserService.userRolesApiUrl, identityProvider, subjectId), roles);
  }

  public removeRolesFromUser(identityProvider: string, subjectId: string, roles: Role[]) : Observable<User> {
    return this.httpClient
      .delete<User>(this.replaceUserIdSegment(FabricAuthUserService.userRolesApiUrl, identityProvider, subjectId));
  }

  private replaceUserIdSegment(tokenizedUrl: string, identityProvider: string, subjectId: string): string {
    return tokenizedUrl
      .replace("{identityProvider}", identityProvider)
      .replace("{subjectId}", subjectId);
  }
}
