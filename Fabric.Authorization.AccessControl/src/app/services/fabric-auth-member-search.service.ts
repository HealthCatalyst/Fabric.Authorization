import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { FabricAuthBaseService } from './fabric-auth-base.service';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { AuthSearchResult } from '../models/authSearchResult';

@Injectable()
export class FabricAuthMemberSearchService extends FabricAuthBaseService {

  static readonly baseMemberApiUrl = FabricAuthBaseService.authUrl + "/members";

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  public searchMembers() : Observable<AuthSearchResult> {
    return null;
  }

}
