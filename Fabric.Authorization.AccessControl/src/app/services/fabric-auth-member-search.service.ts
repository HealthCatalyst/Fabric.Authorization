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
import { AuthMemberSearchResult } from '../models/authMemberSearchResult';
import { AuthMemberSearchRequest } from '../models/authMemberSearchRequest';

@Injectable()
export class FabricAuthMemberSearchService extends FabricAuthBaseService {

  static readonly baseMemberApiUrl = FabricAuthBaseService.authUrl + "/members";

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  public searchMembers(request: AuthMemberSearchRequest) : Observable<AuthMemberSearchResult[]> {
    return this.httpClient
      .get(FabricAuthMemberSearchService.baseMemberApiUrl)
      .map((response: Response) => {
        this.handleError(response);
        return response.json();
      });      
  }
}
